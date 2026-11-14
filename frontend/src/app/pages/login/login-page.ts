import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatButtonToggle, MatButtonToggleGroup } from '@angular/material/button-toggle';
import { MatError, MatFormField, MatLabel, MatSuffix } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { catchError, map, of } from 'rxjs';
import { ignoreApiError } from '../../api/api-error';
import { AuthService } from '../../auth/auth.service';
import { AuthStore } from '../../auth/auth.store';
import { passwordRule } from '../../auth/password-rules';
import { AppRoutes } from '../../auth/routes';
import { AppBrand } from '../../components/app-brand';
import { AppPasswordToggle } from '../../components/app-password-toggle';

type Mode = 'login' | 'register';

@Component({
  selector: 'app-login-page',
  imports: [
    AppBrand,
    AppPasswordToggle,
    MatButton,
    MatButtonToggle,
    MatButtonToggleGroup,
    MatError,
    MatFormField,
    MatInput,
    MatLabel,
    MatSuffix,
    ReactiveFormsModule,
    TranslocoDirective,
  ],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
})
export class LoginPage {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly forms = inject(NonNullableFormBuilder);

  protected readonly mode = signal<Mode>('login');
  protected readonly submitting = signal(false);
  protected readonly registrationEnabled = toSignal(
    inject(AuthService)
      .registrationStatus()
      .pipe(
        map((status) => status.enabled),
        catchError(() => of(false)),
      ),
    { initialValue: false },
  );

  protected readonly loginForm = this.forms.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  protected readonly registerForm = this.forms.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, passwordRule]],
  });

  protected async submitLogin(): Promise<void> {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }
    const { email, password } = this.loginForm.getRawValue();
    await this.enter(() => this.auth.login(email, password));
  }

  protected async submitRegister(): Promise<void> {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }
    const { name, email, password } = this.registerForm.getRawValue();
    await this.enter(() => this.auth.register(name, email, password));
  }

  private async enter(action: () => Promise<void>): Promise<void> {
    this.submitting.set(true);
    try {
      await action();
      await this.router.navigateByUrl(this.returnUrl());
    } catch (error) {
      ignoreApiError(error);
    } finally {
      this.submitting.set(false);
    }
  }

  private returnUrl(): string {
    const target = this.route.snapshot.queryParamMap.get('voltar');
    return target && target.startsWith('/') && !target.startsWith('//') ? target : AppRoutes.home;
  }
}
