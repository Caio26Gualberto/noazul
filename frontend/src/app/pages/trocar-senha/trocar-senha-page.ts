import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormGroupDirective,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { TuiButton, TuiError, TuiIcon, TuiInput } from '@taiga-ui/core';
import { TuiButtonLoading, TuiPassword } from '@taiga-ui/kit';
import { ignoreApiError } from '../../api/api-error';
import { AuthStore } from '../../auth/auth.store';
import { matchesControl, passwordRule } from '../../auth/password-rules';

@Component({
  selector: 'app-trocar-senha-page',
  imports: [
    ReactiveFormsModule,
    TranslocoDirective,
    TuiButton,
    TuiButtonLoading,
    TuiError,
    TuiIcon,
    TuiInput,
    TuiPassword,
  ],
  templateUrl: './trocar-senha-page.html',
})
export class TrocarSenhaPage {
  private readonly auth = inject(AuthStore);

  protected readonly submitting = signal(false);
  protected readonly form = inject(NonNullableFormBuilder).group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, passwordRule]],
    confirmPassword: ['', [Validators.required, matchesControl('newPassword')]],
  });

  constructor() {
    this.form.controls.newPassword.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.form.controls.confirmPassword.updateValueAndValidity());
  }

  protected async submit(formDirective: FormGroupDirective): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    try {
      const { currentPassword, newPassword } = this.form.getRawValue();
      await this.auth.changePassword(currentPassword, newPassword);
      formDirective.resetForm();
    } catch (error) {
      ignoreApiError(error);
    } finally {
      this.submitting.set(false);
    }
  }
}
