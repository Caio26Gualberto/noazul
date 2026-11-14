import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { TuiButton, TuiError, TuiIcon, TuiInput } from '@taiga-ui/core';
import { TuiButtonLoading, TuiPassword } from '@taiga-ui/kit';
import { ignoreApiError } from '../../api/api-error';
import { AuthStore } from '../../auth/auth.store';
import { matchesControl, passwordRule } from '../../auth/password-rules';
import { AppRoutes } from '../../auth/routes';
import { AppBrand } from '../../components/app-brand';

@Component({
  selector: 'app-definir-senha-page',
  imports: [
    AppBrand,
    ReactiveFormsModule,
    TranslocoDirective,
    TuiButton,
    TuiButtonLoading,
    TuiError,
    TuiIcon,
    TuiInput,
    TuiPassword,
  ],
  templateUrl: './definir-senha-page.html',
})
export class DefinirSenhaPage {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);

  protected readonly submitting = signal(false);
  protected readonly form = inject(NonNullableFormBuilder).group({
    newPassword: ['', [Validators.required, passwordRule]],
    confirmPassword: ['', [Validators.required, matchesControl('newPassword')]],
  });

  constructor() {
    this.form.controls.newPassword.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.form.controls.confirmPassword.updateValueAndValidity());
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    try {
      await this.auth.setInitialPassword(this.form.getRawValue().newPassword);
      await this.router.navigateByUrl(AppRoutes.home);
    } catch (error) {
      ignoreApiError(error);
    } finally {
      this.submitting.set(false);
    }
  }

  protected logout(): void {
    void this.auth.logout();
  }
}
