import { CurrencyPipe, DatePipe, formatDate } from '@angular/common';
import { Component, LOCALE_ID, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { TuiTable } from '@taiga-ui/addon-table';
import {
  TuiButton,
  TuiDataList,
  TuiDropdown,
  TuiIcon,
  TuiInput,
  TuiLoader,
  TuiTextfieldOptionsDirective,
} from '@taiga-ui/core';
import {
  TuiBadge,
  TuiChevron,
  TuiChip,
  TuiDataListWrapper,
  TuiSegmented,
  TuiSelect,
} from '@taiga-ui/kit';
import { sumAmounts } from '../../finance/finance.calc';
import { Observable, firstValueFrom, startWith, switchMap } from 'rxjs';
import { ignoreApiError } from '../../api/api-error';
import { currentMonth, recentMonths } from '../../finance/finance.dates';
import { CATEGORIES, Category, Origin, Transaction } from '../../finance/finance.models';
import { FinanceService } from '../../finance/finance.service';

type OriginFilter = 'all' | Origin;
type CategoryFilter = 'all' | Category;

const ORIGIN_FILTERS: readonly OriginFilter[] = ['all', 'card', 'account'];
const CATEGORY_FILTERS: readonly CategoryFilter[] = ['all', ...CATEGORIES];
const MONTHS_AVAILABLE = 12;

@Component({
  selector: 'app-transactions-page',
  imports: [
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

  protected readonly originFilters = ORIGIN_FILTERS;
  protected readonly categoryFilters = CATEGORY_FILTERS;
  protected readonly months = recentMonths(MONTHS_AVAILABLE);
  protected readonly month = signal(currentMonth());
  protected readonly categories = CATEGORIES;
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
        transaction.description.toLocaleLowerCase(this.locale).includes(search),
    );
  });

  protected readonly total = computed(() => sumAmounts(this.filtered()));

  protected readonly stringifyCategory = (category: CategoryFilter): string =>
    category === 'all'
      ? this.transloco.translate('transactions.filters.allCategories')
      : this.transloco.translate(`finance.categories.${category}`);

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
