import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { TuiButton, TuiError, TuiInput } from '@taiga-ui/core';
import { TuiButtonLoading } from '@taiga-ui/kit';
import { ignoreApiError } from '../../api/api-error';
import { AuthStore } from '../../auth/auth.store';

@Component({
  selector: 'app-profile-page',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    TranslocoDirective,
    TuiButton,
    TuiButtonLoading,
    TuiError,
    TuiInput,
  ],
  templateUrl: './profile-page.html',
  styleUrl: './profile-page.scss',
})
export class ProfilePage {
  protected readonly auth = inject(AuthStore);

  protected readonly submitting = signal(false);
  protected readonly form = inject(NonNullableFormBuilder).group({
    name: [
      this.auth.profile()?.name ?? '',
      [Validators.required, Validators.minLength(2), Validators.maxLength(100)],
    ],
  });

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    try {
      await this.auth.updateName(this.form.getRawValue().name.trim());
      this.form.markAsPristine();
    } catch (error) {
      ignoreApiError(error);
    } finally {
      this.submitting.set(false);
    }
  }
}
