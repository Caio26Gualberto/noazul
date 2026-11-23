import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { startWith, switchMap } from 'rxjs';
import { TranslocoDirective } from '@jsverse/transloco';
import { TuiThumbnailCard } from '@taiga-ui/addon-commerce';
import { TuiTable } from '@taiga-ui/addon-table';
import { TuiLink, TuiLoader, TuiTitle } from '@taiga-ui/core';
import { TuiChip, TuiProgress } from '@taiga-ui/kit';
import { TuiCardLarge, TuiHeader } from '@taiga-ui/layout';
import { AuthStore } from '../../auth/auth.store';
import { AppRoutes } from '../../auth/routes';
import { sumAmounts, totalsByCategory, totalsByOrigin } from '../../finance/finance.calc';
import { currentMonth, today } from '../../finance/finance.dates';
import {
  BankConnection,
  Card,
  Category,
  MonthSummary,
  Transaction,
} from '../../finance/finance.models';
import { bankClass } from '../../finance/bank-connection.mapper';
import { CategoryNamePipe } from '../../finance/category-name.pipe';
import { CategoryStore } from '../../finance/category.store';
import { FinanceService } from '../../finance/finance.service';
import { ScopeStore } from '../../finance/scope.store';

const RECENT_TRANSACTIONS_COUNT = 6;
const PRODUCT_TIME_ZONE_OFFSET = '-0300';

interface CategoryRow {
  category: Category;
  amount: number;
  percent: number;
}

interface OpenBill {
  bank: string;
  bankClass: string;
  card: Card;
  usedPercent: number;
}

interface Dashboard {
  total: number;
  byOrigin: Record<Transaction['origin'], number>;
  categories: CategoryRow[];
  largestCategory: number;
  bills: OpenBill[];
  recent: Transaction[];
  previousMonth: MonthSummary | null;
  lastSyncAt: string | null;
}

@Component({
  selector: 'app-home-page',
  imports: [
    TranslocoDirective,
    RouterLink,
    CurrencyPipe,
    DatePipe,
    TuiTitle,
    TuiLink,
    TuiLoader,
    TuiCardLarge,
    TuiHeader,
    TuiProgress,
    TuiChip,
    TuiTable,
    TuiThumbnailCard,
    CategoryNamePipe,
  ],
  templateUrl: './home-page.html',
  styleUrl: './home-page.scss',
})
export class HomePage {
  private readonly finance = inject(FinanceService);
  private readonly categoryStore = inject(CategoryStore);
  private readonly scope = inject(ScopeStore);

  constructor() {
    this.categoryStore.refresh();
  }

  protected readonly auth = inject(AuthStore);
  protected readonly routes = AppRoutes;
  protected readonly timeZone = PRODUCT_TIME_ZONE_OFFSET;
  protected readonly monthStart = `${currentMonth()}-01`;

  private readonly transactions = toSignal(
    toObservable(this.scope.selected).pipe(
      switchMap((connection) =>
        this.finance.transactions(currentMonth(), connection).pipe(startWith(undefined)),
      ),
    ),
  );
  private readonly connections = toSignal(this.finance.connections());
  private readonly history = toSignal(
    toObservable(this.scope.selected).pipe(
      switchMap((connection) => this.finance.history(connection).pipe(startWith(undefined))),
    ),
  );

  protected readonly dashboard = computed<Dashboard | null>(() => {
    const all = this.transactions();
    const connections = this.connections();
    const history = this.history();
    if (!all || !connections || !history) {
      return null;
    }
    const selected = this.scope.selected();
    const scoped =
      selected === null ? connections : connections.filter((connection) => connection.id === selected);
    const transactions = all.filter((transaction) => !transaction.ignored);
    const total = sumAmounts(transactions);
    const categories = this.categoryRows(transactions, total);
    return {
      total,
      byOrigin: totalsByOrigin(transactions),
      categories,
      largestCategory: categories[0]?.amount ?? 0,
      bills: this.openBills(scoped),
      recent: transactions.slice(0, RECENT_TRANSACTIONS_COUNT),
      previousMonth: this.previousMonth(history),
      lastSyncAt: this.lastSyncAt(scoped),
    };
  });

  protected plural(count: number): 'one' | 'other' {
    return count === 1 ? 'one' : 'other';
  }

  protected isToday(dateTime: string): boolean {
    return dateTime.startsWith(today());
  }

  private categoryRows(transactions: Transaction[], total: number): CategoryRow[] {
    const totals = totalsByCategory(transactions);
    return this.categoryStore
      .all()
      .map((category) => {
        const amount = totals[category] ?? 0;
        return {
          category,
          amount,
          percent: total > 0 ? Math.round((amount / total) * 100) : 0,
        };
      })
      .sort((a, b) => b.amount - a.amount);
  }

  private openBills(connections: BankConnection[]): OpenBill[] {
    return connections.flatMap((connection) =>
      connection.cards.map((card) => ({
        bank: connection.bank,
        bankClass: bankClass(connection),
        card,
        usedPercent: card.limit > 0 ? Math.round((card.openBill / card.limit) * 100) : 0,
      })),
    );
  }

  private previousMonth(history: MonthSummary[]): MonthSummary | null {
    const before = history.filter((summary) => summary.month < currentMonth());
    return before.at(-1) ?? null;
  }

  private lastSyncAt(connections: BankConnection[]): string | null {
    const syncs = connections
      .filter((connection) => connection.status === 'ok')
      .map((connection) => connection.lastSyncAt)
      .filter((lastSyncAt): lastSyncAt is string => lastSyncAt !== null)
      .sort();
    return syncs.at(-1) ?? null;
  }
}
