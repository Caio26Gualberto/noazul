import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoDirective } from '@jsverse/transloco';
import { TuiDataList, TuiDropdown, TuiIcon } from '@taiga-ui/core';
import { bankClass } from '../finance/bank-connection.mapper';
import { BankConnection } from '../finance/finance.models';
import { FinanceService } from '../finance/finance.service';
import { ScopeStore } from '../finance/scope.store';

@Component({
  selector: 'app-scope-select',
  imports: [TranslocoDirective, TuiDataList, TuiDropdown, TuiIcon],
  templateUrl: './app-scope-select.html',
  styleUrl: './app-scope-select.scss',
})
export class AppScopeSelect {
  private readonly finance = inject(FinanceService);
  private readonly scope = inject(ScopeStore);

  protected readonly connections = toSignal(this.finance.connections(), { initialValue: [] });
  protected readonly open = signal(false);
  protected readonly selected = this.scope.selected;

  protected readonly visible = computed(() => this.connections().length >= 2);
  protected readonly current = computed(
    () => this.connections().find((connection) => connection.id === this.selected()) ?? null,
  );

  constructor() {
    effect(() => this.scope.reconcile(this.connections().map((connection) => connection.id)));
  }

  protected bankClass(connection: BankConnection): string {
    return bankClass(connection);
  }

  protected choose(connectionId: number | null): void {
    this.scope.select(connectionId);
    this.open.set(false);
  }
}
