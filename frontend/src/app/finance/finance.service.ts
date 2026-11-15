import { Injectable, inject } from '@angular/core';
import { Observable, delay, map, of } from 'rxjs';
import { ApiClient } from '../api/api-client';
import { BankConnectionDto, ConnectTokenDto, PagedResultDto } from '../api/types';
import { toBankConnection } from './bank-connection.mapper';
import { INITIAL_TRANSACTIONS, MONTHLY_HISTORY } from './finance.mock';
import { BankConnection, MonthSummary, NewPix, Transaction } from './finance.models';

const FAKE_LATENCY_MS = 300;
const CONNECTIONS_PAGE_SIZE = 100;

@Injectable({ providedIn: 'root' })
export class FinanceService {
  private readonly api = inject(ApiClient);

  private transactionsInMemory = [...INITIAL_TRANSACTIONS];

  transactions(month: string): Observable<Transaction[]> {
    const ofMonth = this.transactionsInMemory
      .filter((transaction) => transaction.date.startsWith(month))
      .sort((a, b) => b.date.localeCompare(a.date) || b.id - a.id);
    return of(ofMonth).pipe(delay(FAKE_LATENCY_MS));
  }

  createPix(pix: NewPix): Observable<Transaction> {
    const created: Transaction = {
      ...pix,
      id: Math.max(...this.transactionsInMemory.map((transaction) => transaction.id)) + 1,
      origin: 'pix',
      bank: null,
      pending: false,
      installment: null,
    };
    this.transactionsInMemory = [...this.transactionsInMemory, created];
    return of(created).pipe(delay(FAKE_LATENCY_MS));
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

  history(): Observable<MonthSummary[]> {
    return of(MONTHLY_HISTORY).pipe(delay(FAKE_LATENCY_MS));
  }
}
