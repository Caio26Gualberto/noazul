import { Injectable, effect, inject, signal } from '@angular/core';
import { AuthStore } from '../auth/auth.store';

const STORAGE_PREFIX = 'noazul:scope:';

@Injectable({ providedIn: 'root' })
export class ScopeStore {
  private readonly auth = inject(AuthStore);
  private readonly selectedState = signal<number | null>(null);

  readonly selected = this.selectedState.asReadonly();

  constructor() {
    effect(() => {
      const userId = this.auth.profile()?.id ?? null;
      this.selectedState.set(userId === null ? null : read(userId));
    });
  }

  select(connectionId: number | null): void {
    this.selectedState.set(connectionId);
    const userId = this.auth.profile()?.id ?? null;
    if (userId !== null) write(userId, connectionId);
  }

  reconcile(connectionIds: readonly number[]): void {
    const current = this.selectedState();
    if (current !== null && !connectionIds.includes(current)) this.select(null);
  }
}

function read(userId: number): number | null {
  try {
    const raw = localStorage.getItem(STORAGE_PREFIX + userId);
    if (raw === null) return null;
    const value = Number(raw);
    return Number.isInteger(value) ? value : null;
  } catch {
    return null;
  }
}

function write(userId: number, connectionId: number | null): void {
  try {
    if (connectionId === null) localStorage.removeItem(STORAGE_PREFIX + userId);
    else localStorage.setItem(STORAGE_PREFIX + userId, String(connectionId));
  } catch {
    return;
  }
}
