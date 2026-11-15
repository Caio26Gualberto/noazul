import { CurrencyPipe, DatePipe, formatDate } from '@angular/common';
import {
  Component,
  LOCALE_ID,
  TemplateRef,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { TuiTable } from '@taiga-ui/addon-table';
import {
  TuiButton,
  TuiDataList,
  TuiDialogService,
  TuiDropdown,
  TuiIcon,
  TuiInput,
  TuiLoader,
  TuiTextfieldOptionsDirective,
} from '@taiga-ui/core';
import {
  TUI_CONFIRM,
  TuiConfirmData,
  TuiBadge,
  TuiChevron,
  TuiChip,
  TuiDataListWrapper,
  TuiSegmented,
  TuiSelect,
} from '@taiga-ui/kit';
import { sumAmounts } from '../../finance/finance.calc';
import { Observable, Observer, firstValueFrom, startWith, switchMap } from 'rxjs';
import { ignoreApiError } from '../../api/api-error';
import { CategoryNamePipe } from '../../finance/category-name.pipe';
import { CategoryStore } from '../../finance/category.store';
import { currentMonth, recentMonths } from '../../finance/finance.dates';
import { Category, CustomCategory, Origin, Transaction } from '../../finance/finance.models';
import { FinanceService } from '../../finance/finance.service';

type OriginFilter = 'all' | Origin;
type CategoryFilter = 'all' | Category;

const ORIGIN_FILTERS: readonly OriginFilter[] = ['all', 'card', 'account'];
const MONTHS_AVAILABLE = 12;
const CATEGORY_NAME_MAX_LENGTH = 40;
const DISPLAY_NAME_MAX_LENGTH = 60;

@Component({
  selector: 'app-transactions-page',
  imports: [
    CategoryNamePipe,
    CurrencyPipe,
    DatePipe,
    FormsModule,
    TranslocoDirective,
    TuiBadge,
    TuiButton,
    TuiDataList,
    TuiDropdown,
    TuiChevron,
    TuiChip,
    TuiDataListWrapper,
    TuiIcon,
    TuiInput,
    TuiLoader,
    TuiSegmented,
    TuiSelect,
    TuiTable,
    TuiTextfieldOptionsDirective,
  ],
  templateUrl: './transactions-page.html',
  styleUrl: './transactions-page.scss',
})
export class TransactionsPage {
  private readonly transloco = inject(TranslocoService);
  private readonly locale = inject(LOCALE_ID);

  private readonly categoryStore = inject(CategoryStore);
  private readonly dialogs = inject(TuiDialogService);
  private readonly nameDialog = viewChild.required<TemplateRef<unknown>>('nameDialog');
  private readonly managerDialog = viewChild.required<TemplateRef<unknown>>('managerDialog');
  private readonly renameDialog = viewChild.required<TemplateRef<unknown>>('renameDialog');
  protected readonly renaming = signal<Transaction | null>(null);
  protected readonly displayNameMaxLength = DISPLAY_NAME_MAX_LENGTH;

  protected readonly originFilters = ORIGIN_FILTERS;
  protected readonly categories = this.categoryStore.all;
  protected readonly customCategories = this.categoryStore.custom;
  protected readonly categoryFilters = computed<CategoryFilter[]>(() => [
    'all',
    ...this.categories(),
  ]);
  protected readonly months = recentMonths(MONTHS_AVAILABLE);
  protected readonly month = signal(currentMonth());
  protected readonly nameMaxLength = CATEGORY_NAME_MAX_LENGTH;
  protected readonly draftName = signal('');
  protected readonly menuFor = signal<number | null>(null);
  protected readonly saving = signal(false);
  private readonly version = signal(0);
  protected readonly monthLabel = computed(() => this.formatMonth(this.month()));

  private readonly finance = inject(FinanceService);

  protected readonly transactions = toSignal(
    toObservable(computed(() => ({ month: this.month(), version: this.version() }))).pipe(
      switchMap(({ month }) => this.finance.transactions(month).pipe(startWith(undefined))),
    ),
  );
  protected readonly loading = computed(() => this.transactions() === undefined);

  protected readonly origin = signal<OriginFilter>('all');
  protected readonly search = signal('');
  protected readonly category = signal<CategoryFilter>('all');

  protected readonly filtered = computed(() => {
    const origin = this.origin();
    const category = this.category();
    const search = this.search().trim().toLocaleLowerCase(this.locale);
    return (this.transactions() ?? []).filter(
      (transaction) =>
        (origin === 'all' || transaction.origin === origin) &&
        (category === 'all' || transaction.category === category) &&
        (transaction.name.toLocaleLowerCase(this.locale).includes(search) ||
          transaction.description.toLocaleLowerCase(this.locale).includes(search)),
    );
  });

  protected readonly total = computed(() => sumAmounts(this.filtered()));

  constructor() {
    this.categoryStore.refresh();
  }

  protected readonly stringifyCategory = (category: CategoryFilter): string =>
    category === 'all'
      ? this.transloco.translate('transactions.filters.allCategories')
      : this.categoryStore.label(category);

  protected async renameTransaction(transaction: Transaction): Promise<void> {
    if (this.saving()) return;
    this.renaming.set(transaction);
    this.draftName.set(transaction.name);
    const name = await firstValueFrom(
      this.dialogs.open<string>(this.renameDialog(), {
        label: this.transloco.translate('transactions.rename.title'),
        size: 's',
      }),
      { defaultValue: undefined },
    );
    this.renaming.set(null);
    if (name === undefined || name === transaction.name) return;
    if (name === '' && !transaction.renamed) return;
    await this.runSaving(this.finance.updateDisplayName(transaction.id, name), true);
  }

  protected submitRename(observer: Observer<string>): void {
    const name = this.draftName().trim();
    if (name.length === 0) return;
    observer.next(name);
    observer.complete();
  }

  protected submitName(observer: Observer<string>): void {
    const name = this.draftName().trim();
    if (name.length === 0) return;
    observer.next(name);
    observer.complete();
  }

  protected async createAndApply(transaction: Transaction): Promise<void> {
    this.menuFor.set(null);
    const name = await this.askName('transactions.categoryDialog.createTitle', '');
    if (!name) return;
    if (this.saving()) return;
    this.saving.set(true);
    try {
      const created = await firstValueFrom(this.categoryStore.create(name));
      await firstValueFrom(this.finance.updateCategory(transaction.id, created.key));
      this.version.update((value) => value + 1);
    } catch (error) {
      ignoreApiError(error);
    } finally {
      this.saving.set(false);
    }
  }

  protected openManager(): void {
    this.menuFor.set(null);
    this.dialogs
      .open(this.managerDialog(), {
        label: this.transloco.translate('transactions.categoryManager.title'),
        size: 's',
      })
      .subscribe();
  }

  protected async renameCategory(category: CustomCategory): Promise<void> {
    const name = await this.askName('transactions.categoryDialog.renameTitle', category.name);
    if (!name || name === category.name) return;
    await this.runSaving(this.categoryStore.rename(category, name), false);
  }

  protected async deleteCategory(category: CustomCategory): Promise<void> {
    const confirmed = await firstValueFrom(
      this.dialogs.open<boolean>(TUI_CONFIRM, {
        label: this.transloco.translate('transactions.categoryManager.deleteTitle', {
          name: category.name,
        }),
        size: 's',
        data: {
          content: this.transloco.translate('transactions.categoryManager.deleteText'),
          yes: this.transloco.translate('transactions.categoryManager.deleteConfirm'),
          no: this.transloco.translate('transactions.categoryDialog.cancel'),
          appearance: ['primary-destructive', 'secondary'],
        } satisfies TuiConfirmData,
      }),
      { defaultValue: false },
    );
    if (!confirmed) return;
    if (this.category() === category.key) this.category.set('all');
    await this.runSaving(this.categoryStore.remove(category), true);
  }

  private async askName(labelKey: string, initial: string): Promise<string | undefined> {
    this.draftName.set(initial);
    return firstValueFrom(
      this.dialogs.open<string>(this.nameDialog(), {
        label: this.transloco.translate(labelKey),
        size: 's',
      }),
      { defaultValue: undefined },
    );
  }

  private async runSaving(request: Observable<unknown>, reload: boolean): Promise<void> {
    if (this.saving()) return;
    this.saving.set(true);
    try {
      await firstValueFrom(request, { defaultValue: undefined });
      if (reload) this.version.update((value) => value + 1);
    } catch (error) {
      ignoreApiError(error);
    } finally {
      this.saving.set(false);
    }
  }

  protected readonly stringifyMonth = (month: string): string => this.formatMonth(month);

  protected changeCategory(transaction: Transaction, category: Category): Promise<void> {
    if (!transaction.ignored && transaction.category === category) {
      this.menuFor.set(null);
      return Promise.resolve();
    }
    return this.save(this.finance.updateCategory(transaction.id, category));
  }

  protected markNotSpending(transaction: Transaction): Promise<void> {
    if (transaction.ignored) {
      this.menuFor.set(null);
      return Promise.resolve();
    }
    return this.save(this.finance.markNotSpending(transaction.id));
  }

  protected clearFilters(): void {
    this.origin.set('all');
    this.search.set('');
    this.category.set('all');
  }

  private async save(request: Observable<unknown>): Promise<void> {
    if (this.saving()) return;
    this.saving.set(true);
    try {
      await firstValueFrom(request);
      this.menuFor.set(null);
      this.version.update((value) => value + 1);
    } catch (error) {
      ignoreApiError(error);
    } finally {
      this.saving.set(false);
    }
  }

  private formatMonth(month: string): string {
    const label = formatDate(`${month}-01`, "MMMM 'de' y", this.locale, 'UTC');
    return label.charAt(0).toLocaleUpperCase(this.locale) + label.slice(1);
  }
}
