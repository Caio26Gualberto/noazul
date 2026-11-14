import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslocoService } from '@jsverse/transloco';
import { ApiCodes } from './api-codes';

@Injectable({ providedIn: 'root' })
export class FeedbackService {
  private readonly snackBar = inject(MatSnackBar);
  private readonly transloco = inject(TranslocoService);

  show(code: string): void {
    const key = `api.${code}`;
    const translated = this.transloco.translate(key);
    const message =
      translated === key ? this.transloco.translate(`api.${ApiCodes.unknownError}`) : translated;
    this.snackBar.open(message, this.transloco.translate('common.actions.close'), {
      duration: 5000,
    });
  }
}
