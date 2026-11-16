import {
  CurrencyPipe,
  DatePipe,
  DecimalPipe,
  TitleCasePipe,
  formatCurrency,
  formatDate,
  getCurrencySymbol,
} from '@angular/common';
import { Component, DEFAULT_CURRENCY_CODE, LOCALE_ID, computed, inject } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { TuiTable } from '@taiga-ui/addon-table';
import { TuiLoader } from '@taiga-ui/core';
import { TuiProgress } from '@taiga-ui/kit';
import { forkJoin, startWith, switchMap } from 'rxjs';
import { sumAmounts, totalsByCategory, totalsByOrigin } from '../../finance/finance.calc';
import { currentMonth, today } from '../../finance/finance.dates';
import { Category, Origin } from '../../finance/finance.models';
import { CategoryNamePipe } from '../../finance/category-name.pipe';
import { CategoryStore } from '../../finance/category.store';
import { FinanceService } from '../../finance/finance.service';
import { ScopeStore } from '../../finance/scope.store';

interface MonthBar {
  month: string;
  label: string;
  total: number;
  heightRatio: number;
  inProgress: boolean;
}

interface OriginSlice {
  origin: Origin;
  amount: number;
  percent: number;
}

interface CategoryComparison {
  category: Category;
  previous: number;
  current: number;
  percent: number;
  exceeded: boolean;
}

const TOP_EXPENSES = 5;
const ORIGINS: Origin[] = ['card', 'account'];

function parseMonth(month: string): Date {
  const [year, monthNumber] = month.split('-').map(Number);
  return new Date(year, monthNumber - 1, 1);
}

function parseDay(date: string): Date {
  const [year, month, day] = date.split('-').map(Number);
  return new Date(year, month - 1, day);
}

function daysInMonth(month: string): number {
  const [year, monthNumber] = month.split('-').map(Number);
  return new Date(year, monthNumber, 0).getDate();
}

function percentOf(part: number, whole: number): number {
  return whole > 0 ? (part / whole) * 100 : 0;
}

@Component({
  selector: 'app-metrics-page',
  imports: [
    CurrencyPipe,
    DatePipe,
    DecimalPipe,
    TitleCasePipe,
    TranslocoDirective,
    TuiLoader,
    TuiProgress,
    TuiTable,
    CategoryNamePipe,
  ],
  templateUrl: './metrics-page.html',
  styleUrl: './metrics-page.scss',
})
export class MetricsPage {
  private readonly finance = inject(FinanceService);
  private readonly categoryStore = inject(CategoryStore);
  private readonly scope = inject(ScopeStore);

  constructor() {
    this.categoryStore.refresh();
  }
  private readonly transloco = inject(TranslocoService);
  private readonly locale = inject(LOCALE_ID);
  private readonly currencyCode = inject(DEFAULT_CURRENCY_CODE);

  private readonly data = toSignal(
    toObservable(this.scope.selected).pipe(
      switchMap((connection) =>
        forkJoin({
          history: this.finance.history(connection),
          transactions: this.finance.transactions(currentMonth(), connection),
        }).pipe(startWith(undefined)),
      ),
    ),
  );

  protected readonly loading = computed(() => this.data() === undefined);

  private readonly history = computed(() => this.data()?.history ?? []);
  private readonly transactions = computed(() =>
    (this.data()?.transactions ?? []).filter((transaction) => !transaction.ignored),
  );
  private readonly lastClosed = computed(() => this.history().at(-1));

  protected readonly today = parseDay(today());
  protected readonly currentDay = this.today.getDate();
  protected readonly currentMonthDays = daysInMonth(currentMonth());
  protected readonly currentMonthName = this.monthName(currentMonth());

  protected readonly currentTotal = computed(() => sumAmounts(this.transactions()));
  protected readonly currentDailyAverage = computed(() => this.currentTotal() / this.currentDay);

  protected readonly closedMonthsCount = computed(() => this.history().length);
  protected readonly closedRange = computed(() => {
    const history = this.history();
    if (history.length === 0) {
      return '';
    }
    return `${this.shortMonthName(history[0].month)}–${this.shortMonthName(history[history.length - 1].month)}`;
  });
  protected readonly closedAverage = computed(() => {
    const history = this.history();
    return history.length > 0
      ? history.reduce((total, month) => total + month.total, 0) / history.length
      : 0;
  });

  protected readonly mostExpensive = computed(() => {
    const top = this.history().reduce<{ month: string; total: number } | null>(
      (best, month) => (best === null || month.total > best.total ? month : best),
      null,
    );
    return top ? { name: this.monthName(top.month), total: top.total } : null;
  });

  protected readonly lastClosedName = computed(() => {
    const month = this.lastClosed()?.month;
    return month ? this.monthName(month) : '';
  });
  protected readonly lastClosedDailyAverage = computed(() => {
    const month = this.lastClosed();
    return month ? month.total / daysInMonth(month.month) : 0;
  });

  protected readonly monthBars = computed<MonthBar[]>(() => {
    const months = [
      ...this.history().map((month) => ({
        month: month.month,
        total: month.total,
        inProgress: false,
      })),
      { month: currentMonth(), total: this.currentTotal(), inProgress: true },
    ];
    const highest = Math.max(...months.map((month) => month.total));
    return months.map((month) => ({
      ...month,
      label: this.shortMonthName(month.month),
      heightRatio: highest > 0 ? month.total / highest : 0,
    }));
  });

  protected readonly monthBarsDescription = computed(() => {
    const values = this.monthBars()
      .map((bar) =>
        this.transloco.translate(
          bar.inProgress ? 'metrics.byMonth.valueInProgress' : 'metrics.byMonth.value',
          {
            month: this.monthName(bar.month),
            value: this.money(bar.total),
          },
        ),
      )
      .join(', ');
    return this.transloco.translate('metrics.byMonth.description', { values });
  });

  protected readonly originSlices = computed<OriginSlice[]>(() => {
    const totals = totalsByOrigin(this.transactions());
    const total = this.currentTotal();
    return ORIGINS.map((origin) => ({
      origin,
      amount: totals[origin],
      percent: percentOf(totals[origin], total),
    }));
  });

  protected readonly topExpenses = computed(() =>
    [...this.transactions()].sort((a, b) => b.amount - a.amount).slice(0, TOP_EXPENSES),
  );

  protected readonly categories = computed<CategoryComparison[]>(() => {
    const previous = this.lastClosed()?.byCategory;
    const current = totalsByCategory(this.transactions());
    return this.categoryStore.all().map((category) => {
      const previousAmount = previous?.[category] ?? 0;
      const currentAmount = current[category] ?? 0;
      const percent = percentOf(currentAmount, previousAmount);
      return {
        category,
        previous: previousAmount,
        current: currentAmount,
        percent,
        exceeded: percent >= 100,
      };
    });
  });

  private monthName(month: string): string {
    return formatDate(parseMonth(month), 'LLLL', this.locale);
  }

  private shortMonthName(month: string): string {
    return formatDate(parseMonth(month), 'LLL', this.locale).replace('.', '');
  }

  private money(value: number): string {
    return formatCurrency(
      value,
      this.locale,
      getCurrencySymbol(this.currencyCode, 'narrow', this.locale),
      this.currencyCode,
    );
  }
}
