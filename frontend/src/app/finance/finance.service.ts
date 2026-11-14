import { Injectable } from '@angular/core';
import { Observable, delay, of } from 'rxjs';
import { BANK_CONNECTIONS, INITIAL_TRANSACTIONS, MONTHLY_HISTORY } from './finance.mock';
import { BankConnection, MonthSummary, NewPix, Transaction } from './finance.models';

const FAKE_LATENCY_MS = 300;

@Injectable({ providedIn: 'root' })
export class FinanceService {
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
    return of(BANK_CONNECTIONS).pipe(delay(FAKE_LATENCY_MS));
  }

  history(): Observable<MonthSummary[]> {
    return of(MONTHLY_HISTORY).pipe(delay(FAKE_LATENCY_MS));
  }
}
