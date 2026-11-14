import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiCodes } from '../api/api-codes';
import { ignoreApiError } from '../api/api-error';
import { FeedbackService } from '../api/feedback.service';
import { TokenRefresher } from '../api/token-refresher';
import { TokenStorage } from '../api/token-storage';
import { ProfileDto } from '../api/types';
import { AuthService } from './auth.service';
import { AppRoutes } from './routes';

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly service = inject(AuthService);
  private readonly storage = inject(TokenStorage);
  private readonly feedback = inject(FeedbackService);
  private readonly router = inject(Router);
  private readonly profileState = signal<ProfileDto | null>(null);

  readonly profile = this.profileState.asReadonly();
  readonly isAuthenticated = computed(() => this.profileState() !== null);
  readonly needsPasswordChange = computed(
    () => this.profileState()?.isNeededChangePassword ?? false,
  );

  constructor() {
    inject(TokenRefresher)
      .sessionExpired$.pipe(takeUntilDestroyed())
      .subscribe(() => {
        const wasAuthenticated = this.isAuthenticated();
        this.profileState.set(null);
        if (!wasAuthenticated) return;
        this.feedback.show(ApiCodes.sessionExpired);
        void this.router.navigateByUrl(AppRoutes.login);
      });
  }

  async restore(): Promise<void> {
    if (!this.storage.hasSession) return;
    try {
      await this.loadProfile();
    } catch (error) {
      ignoreApiError(error);
    }
  }

  async login(email: string, password: string): Promise<void> {
    this.storage.clear();
    const response = await firstValueFrom(this.service.login(email, password));
    this.storage.save(response.tokens);
    await this.loadProfile();
  }

  async register(name: string, email: string, password: string): Promise<void> {
    this.storage.clear();
    const response = await firstValueFrom(this.service.register(name, email, password));
    this.storage.save(response.tokens);
    await this.loadProfile();
  }

  async logout(): Promise<void> {
    try {
      await firstValueFrom(this.service.logout());
    } catch (error) {
      ignoreApiError(error);
    }
    this.storage.clear();
    this.profileState.set(null);
    await this.router.navigateByUrl(AppRoutes.login);
  }

  async updateName(name: string): Promise<void> {
    this.profileState.set(await firstValueFrom(this.service.updateName(name)));
  }

  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    this.storage.save(
      await firstValueFrom(this.service.changePassword(currentPassword, newPassword)),
    );
  }

  async setInitialPassword(newPassword: string): Promise<void> {
    this.storage.save(await firstValueFrom(this.service.setInitialPassword(newPassword)));
    await this.loadProfile();
  }

  private async loadProfile(): Promise<void> {
    this.profileState.set(await firstValueFrom(this.service.profile()));
  }
}
