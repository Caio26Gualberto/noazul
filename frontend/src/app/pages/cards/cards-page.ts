import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoDirective } from '@jsverse/transloco';
import { TuiThumbnailCard } from '@taiga-ui/addon-commerce';
import { TuiButton, TuiHint, TuiIcon, TuiLoader, TuiNotification, TuiTitle } from '@taiga-ui/core';
import { TuiAvatar, TuiProgress } from '@taiga-ui/kit';
import { sumAmounts } from '../../finance/finance.calc';
import { CURRENT_MONTH, TODAY } from '../../finance/finance.mock';
import { Account, BankConnection, Transaction } from '../../finance/finance.models';
import { FinanceService } from '../../finance/finance.service';

const PRODUCT_TIME_ZONE = 'America/Sao_Paulo';

const timeFormat = new Intl.DateTimeFormat('pt-BR', {
  timeZone: PRODUCT_TIME_ZONE,
  hour: '2-digit',
  minute: '2-digit',
});

const dayMonthFormat = new Intl.DateTimeFormat('pt-BR', {
  timeZone: PRODUCT_TIME_ZONE,
  day: '2-digit',
  month: '2-digit',
});

const isoDayFormat = new Intl.DateTimeFormat('en-CA', { timeZone: PRODUCT_TIME_ZONE });

interface AccountView {
  account: Account;
  monthOutflow: number;
  monthCount: number;
}

interface ConnectionView {
  connection: BankConnection;
  initial: string;
  syncedToday: boolean;
  syncDate: string;
  syncTime: string;
  accounts: AccountView[];
}

@Component({
  selector: 'app-cards-page',
  imports: [
    CurrencyPipe,
    DatePipe,
    TranslocoDirective,
    TuiAvatar,
    TuiButton,
    TuiHint,
    TuiIcon,
    TuiLoader,
    TuiNotification,
    TuiProgress,
    TuiThumbnailCard,
    TuiTitle,
  ],
  templateUrl: './cards-page.html',
  styleUrl: './cards-page.scss',
})
export class CardsPage {
  private readonly finance = inject(FinanceService);

  private readonly rawConnections = toSignal(this.finance.connections());
  private readonly monthTransactions = toSignal(this.finance.transactions(CURRENT_MONTH));

  protected readonly loading = computed(
    () => this.rawConnections() === undefined || this.monthTransactions() === undefined,
  );

  protected readonly connections = computed<ConnectionView[]>(() => {
    const transactions = this.monthTransactions() ?? [];
    return (this.rawConnections() ?? []).map((connection) =>
      toConnectionView(connection, transactions),
    );
  });
}

function toConnectionView(
  connection: BankConnection,
  monthTransactions: readonly Transaction[],
): ConnectionView {
  const syncedAt = new Date(connection.lastSyncAt);
  const accountOutflows = monthTransactions.filter(
    (transaction) => transaction.origin === 'account' && transaction.bank === connection.bank,
  );
  return {
    connection,
    initial: connection.bank.charAt(0).toUpperCase(),
    syncedToday: isoDayFormat.format(syncedAt) === TODAY,
    syncDate: dayMonthFormat.format(syncedAt),
    syncTime: timeFormat.format(syncedAt),
    accounts: connection.accounts.map((account) => ({
      account,
      monthOutflow: sumAmounts(accountOutflows),
      monthCount: accountOutflows.length,
    })),
  };
}
