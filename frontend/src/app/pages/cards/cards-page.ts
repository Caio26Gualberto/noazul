import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoDirective } from '@jsverse/transloco';
import { TuiThumbnailCard } from '@taiga-ui/addon-commerce';
import { TuiButton, TuiHint, TuiLoader, TuiNotification, TuiTitle } from '@taiga-ui/core';
import { TuiAvatar, TuiButtonLoading, TuiProgress } from '@taiga-ui/kit';
import { firstValueFrom } from 'rxjs';
import { ApiCodes } from '../../api/api-codes';
import { ignoreApiError } from '../../api/api-error';
import { FeedbackService } from '../../api/feedback.service';
import { BankConnection } from '../../finance/finance.models';
import { bankClass } from '../../finance/bank-connection.mapper';
import { FinanceService } from '../../finance/finance.service';
import { PluggyConnectService } from '../../finance/pluggy-connect.service';

const PRODUCT_TIME_ZONE = 'America/Sao_Paulo';
const FIRST_SYNC_RECHECK_MS = 8000;
const FIRST_SYNC_MAX_RECHECKS = 8;

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

interface ConnectionView {
  connection: BankConnection;
  initial: string;
  bankClass: string;
  syncedToday: boolean;
  syncDate: string;
  syncTime: string;
}

@Component({
  selector: 'app-cards-page',
  imports: [
    CurrencyPipe,
    DatePipe,
    TranslocoDirective,
    TuiAvatar,
    TuiButton,
    TuiButtonLoading,
    TuiHint,
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
  private readonly pluggyConnect = inject(PluggyConnectService);
  private readonly feedback = inject(FeedbackService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly rawConnections = signal<BankConnection[] | undefined>(undefined);
  private recheckTimer: ReturnType<typeof setTimeout> | undefined;
  private rechecksLeft = FIRST_SYNC_MAX_RECHECKS;

  protected readonly connecting = signal(false);

  protected readonly loading = computed(() => this.rawConnections() === undefined);

  protected readonly connections = computed<ConnectionView[]>(() =>
    (this.rawConnections() ?? []).map(toConnectionView),
  );

  constructor() {
    this.destroyRef.onDestroy(() => clearTimeout(this.recheckTimer));
    this.load();
  }

  protected async connectBank(): Promise<void> {
    if (this.connecting()) return;
    this.connecting.set(true);

    try {
      const token = await firstValueFrom(this.finance.createConnectToken());
      const outcome = await this.pluggyConnect.open(token);

      if (outcome.kind === 'failed') {
        this.feedback.show(ApiCodes.pluggyUnavailable);
        return;
      }

      if (outcome.kind === 'connected') {
        await firstValueFrom(this.finance.registerConnection(outcome.itemId));
        this.rechecksLeft = FIRST_SYNC_MAX_RECHECKS;
        this.load();
      }
    } catch (error) {
      ignoreApiError(error);
    } finally {
      this.connecting.set(false);
    }
  }

  private load(): void {
    clearTimeout(this.recheckTimer);

    this.finance
      .connections()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (connections) => {
          this.rawConnections.set(connections);
          const stillUpdating = connections.some((connection) => connection.status === 'updating');
          if (stillUpdating && this.rechecksLeft > 0) {
            this.rechecksLeft--;
            this.recheckTimer = setTimeout(() => this.load(), FIRST_SYNC_RECHECK_MS);
          }
        },
        error: (error: unknown) => {
          ignoreApiError(error);
          this.rawConnections.set([]);
        },
      });
  }
}

function toConnectionView(connection: BankConnection): ConnectionView {
  const syncedAt = connection.lastSyncAt ? new Date(connection.lastSyncAt) : null;
  return {
    connection,
    initial: connection.bank.charAt(0).toUpperCase(),
    bankClass: bankClass(connection),
    syncedToday:
      syncedAt !== null && isoDayFormat.format(syncedAt) === isoDayFormat.format(new Date()),
    syncDate: syncedAt ? dayMonthFormat.format(syncedAt) : '',
    syncTime: syncedAt ? timeFormat.format(syncedAt) : '',
  };
}
