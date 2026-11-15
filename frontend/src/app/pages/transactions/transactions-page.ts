import { CurrencyPipe, DatePipe, formatDate } from '@angular/common';
import { Component, LOCALE_ID, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { TuiTable } from '@taiga-ui/addon-table';
import { TuiButton, TuiIcon, TuiInput, TuiLoader } from '@taiga-ui/core';
import {
  TuiBadge,
  TuiChevron,
  TuiChip,
  TuiDataListWrapper,
  TuiSegmented,
  TuiSelect,
} from '@taiga-ui/kit';
import { AppRoutes } from '../../auth/routes';
import { sumAmounts } from '../../finance/finance.calc';
import { currentMonth } from '../../finance/finance.dates';
import { CATEGORIES, Category, Origin } from '../../finance/finance.models';
import { FinanceService } from '../../finance/finance.service';

type OriginFilter = 'all' | Origin;
type CategoryFilter = 'all' | Category;

const ORIGIN_FILTERS: readonly OriginFilter[] = ['all', 'card', 'account', 'pix'];
const CATEGORY_FILTERS: readonly CategoryFilter[] = ['all', ...CATEGORIES];

@Component({
  selector: 'app-transactions-page',
  imports: [
    CurrencyPipe,
    DatePipe,
    FormsModule,
    RouterLink,
    TranslocoDirective,
    TuiBadge,
    TuiButton,
    TuiChevron,
    TuiChip,
    TuiDataListWrapper,
    TuiIcon,
    TuiInput,
    TuiLoader,
    TuiSegmented,
    TuiSelect,
    TuiTable,
  ],
  templateUrl: './transactions-page.html',
  styleUrl: './transactions-page.scss',
})
export class TransactionsPage {
  private readonly transloco = inject(TranslocoService);
  private readonly locale = inject(LOCALE_ID);

  protected readonly routes = AppRoutes;
  protected readonly originFilters = ORIGIN_FILTERS;
  protected readonly categoryFilters = CATEGORY_FILTERS;
  protected readonly monthLabel = this.formatMonth(currentMonth());

  protected readonly transactions = toSignal(inject(FinanceService).transactions(currentMonth()));
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

  protected clearFilters(): void {
    this.origin.set('all');
    this.search.set('');
    this.category.set('all');
  }

  private formatMonth(month: string): string {
    const label = formatDate(`${month}-01`, "MMMM 'de' y", this.locale, 'UTC');
    return label.charAt(0).toLocaleUpperCase(this.locale) + label.slice(1);
  }
}
