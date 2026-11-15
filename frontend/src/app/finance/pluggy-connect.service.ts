import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

export type PluggyConnectOutcome =
  { kind: 'connected'; itemId: string } | { kind: 'closed' } | { kind: 'failed'; message: string };

@Injectable({ providedIn: 'root' })
export class PluggyConnectService {
  async open(connectToken: string): Promise<PluggyConnectOutcome> {
    const { PluggyConnect } = await import('pluggy-connect-sdk');

    return new Promise<PluggyConnectOutcome>((resolve) => {
      let outcome: PluggyConnectOutcome = { kind: 'closed' };

      const widget = new PluggyConnect({
        connectToken,
        includeSandbox: environment.pluggyIncludeSandbox,
        language: 'pt',
        theme: 'light',
        onSuccess: ({ item }) => {
          outcome = { kind: 'connected', itemId: item.id };
          resolve(outcome);
        },
        onError: ({ message }) => {
          outcome = { kind: 'failed', message };
        },
        onClose: () => {
          void widget.destroy();
          resolve(outcome);
        },
      });

      widget.init().catch((error: unknown) => {
        resolve({ kind: 'failed', message: String(error) });
      });
    });
  }
}
