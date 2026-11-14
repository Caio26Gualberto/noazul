import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClient } from '../api/api-client';
import { LoginResponseDto, ProfileDto, RegistrationStatusDto, TokensDto } from '../api/types';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiClient);

  registrationStatus(): Observable<RegistrationStatusDto> {
    return this.api.get<RegistrationStatusDto>('/api/auth/registration', { silent: true });
  }

  login(email: string, password: string): Observable<LoginResponseDto> {
    return this.api.post<LoginResponseDto>('/api/auth/login', { email, password });
  }

  register(name: string, email: string, password: string): Observable<LoginResponseDto> {
    return this.api.post<LoginResponseDto>('/api/auth/register', { name, email, password });
  }

  logout(): Observable<boolean> {
    return this.api.post<boolean>('/api/auth/logout', undefined, { silent: true });
  }

  profile(): Observable<ProfileDto> {
    return this.api.get<ProfileDto>('/api/profile', { silent: true });
  }

  updateName(name: string): Observable<ProfileDto> {
    return this.api.patch<ProfileDto>('/api/profile/name', { name }, { announce: true });
  }

  changePassword(currentPassword: string, newPassword: string): Observable<TokensDto> {
    return this.api.post<TokensDto>(
      '/api/profile/change-password',
      { currentPassword, newPassword },
      { announce: true },
    );
  }

  setInitialPassword(newPassword: string): Observable<TokensDto> {
    return this.api.post<TokensDto>(
      '/api/profile/set-initial-password',
      { newPassword },
      { announce: true },
    );
  }
}
