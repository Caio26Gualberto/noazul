import { Injectable, inject } from '@angular/core';
import { EMPTY, Observable, expand, map, reduce } from 'rxjs';
import { ApiClient } from '../api/api-client';
import {
  BankConnectionDto,
  CategoryRuleResultDto,
  ConnectTokenDto,
  MonthSummaryDto,
  PagedResultDto,
  TransactionDto,
} from '../api/types';
import { toBankConnection } from './bank-connection.mapper';
import { currentMonth } from './finance.dates';
import { BankConnection, Category, MonthSummary, Transaction } from './finance.models';
import { toMonthSummary, toTransaction } from './transaction.mapper';

const CONNECTIONS_PAGE_SIZE = 100;
const TRANSACTIONS_PAGE_SIZE = 100;
const HISTORY_MONTHS = 6;

@Injectable({ providedIn: 'root' })
export class FinanceService {
  private readonly api = inject(ApiClient);

  transactions(month: string): Observable<Transaction[]> {
    return this.transactionsPage(month, 1).pipe(
      expand((page) => (page.hasNext ? this.transactionsPage(month, page.page + 1) : EMPTY)),
      reduce((all, page) => [...all, ...page.items.map(toTransaction)], [] as Transaction[]),
    );
  }

  connections(): Observable<BankConnection[]> {
    return this.api
      .get<PagedResultDto<BankConnectionDto>>(
        `/api/bank-connections?pageSize=${CONNECTIONS_PAGE_SIZE}`,
      )
      .pipe(map((page) => page.items.map(toBankConnection)));
  }

  createConnectToken(): Observable<string> {
    return this.api
      .post<ConnectTokenDto>('/api/bank-connections/connect-token')
      .pipe(map((dto) => dto.accessToken));
  }

  registerConnection(itemId: string): Observable<BankConnection> {
    return this.api
      .post<BankConnectionDto>('/api/bank-connections', { itemId }, { announce: true })
      .pipe(map(toBankConnection));
  }

  updateCategory(id: number, category: Category): Observable<CategoryRuleResultDto> {
    return this.api.put<CategoryRuleResultDto>(
      `/api/transactions/${id}/category`,
      { category, ignored: false },
      { announce: true },
    );
  }

  markNotSpending(id: number): Observable<CategoryRuleResultDto> {
    return this.api.put<CategoryRuleResultDto>(
      `/api/transactions/${id}/category`,
      { ignored: true },
      { announce: true },
    );
  }

  history(): Observable<MonthSummary[]> {
    const month = currentMonth();
    return this.api
      .get<MonthSummaryDto[]>(`/api/transactions/monthly-summary?months=${HISTORY_MONTHS}`)
      .pipe(
        map((summaries) =>
          summaries.filter((summary) => summary.month < month).map(toMonthSummary),
        ),
      );
  }

  private transactionsPage(
    month: string,
    page: number,
  ): Observable<PagedResultDto<TransactionDto>> {
    return this.api.get<PagedResultDto<TransactionDto>>(
      `/api/transactions?month=${month}&page=${page}&pageSize=${TRANSACTIONS_PAGE_SIZE}`,
    );
  }
}
