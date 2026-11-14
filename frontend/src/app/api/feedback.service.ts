import { Injectable, inject } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { TuiNotificationService } from '@taiga-ui/core';
import { ApiCodes } from './api-codes';

@Injectable({ providedIn: 'root' })
export class FeedbackService {
  private readonly notifications = inject(TuiNotificationService);
  private readonly transloco = inject(TranslocoService);

  show(code: string): void {
    const key = `api.${code}`;
    const translated = this.transloco.translate(key);
    const message =
      translated === key ? this.transloco.translate(`api.${ApiCodes.unknownError}`) : translated;
    this.notifications.open(message, { autoClose: 5000, closable: true }).subscribe();
  }
}
