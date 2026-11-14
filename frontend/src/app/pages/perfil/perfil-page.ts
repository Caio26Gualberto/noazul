import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatError, MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { TranslocoDirective } from '@jsverse/transloco';
import { ignoreApiError } from '../../api/api-error';
import { AuthStore } from '../../auth/auth.store';

@Component({
  selector: 'app-perfil-page',
  imports: [
    DatePipe,
    MatButton,
    MatError,
    MatFormField,
    MatInput,
    MatLabel,
    ReactiveFormsModule,
    TranslocoDirective,
  ],
  templateUrl: './perfil-page.html',
  styleUrl: './perfil-page.scss',
})
export class PerfilPage {
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
