import { HttpBackend, HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, Subject, catchError, finalize, map, shareReplay, throwError } from 'rxjs';
import { ApiCodes } from './api-codes';
import { API_BASE_URL } from './api-config';
import { ApiError } from './api-error';
import { TokenStorage } from './token-storage';
import { ApiEnvelope, TokensDto } from './types';

@Injectable({ providedIn: 'root' })
export class TokenRefresher {
  private readonly http = new HttpClient(inject(HttpBackend));
  private readonly storage = inject(TokenStorage);
  private readonly expired = new Subject<void>();
  private pending: Observable<string> | null = null;

  readonly sessionExpired$ = this.expired.asObservable();

  refresh(): Observable<string> {
    if (this.pending) return this.pending;

    const refreshToken = this.storage.refreshToken;
    if (!refreshToken) return this.expire();

    this.pending = this.http
      .post<ApiEnvelope<TokensDto>>(`${API_BASE_URL}/api/auth/refresh-token`, { refreshToken })
      .pipe(
        map((envelope) => {
          if (!envelope.isSuccess || !envelope.data) throw new ApiError(envelope.code, 401);
          this.storage.save(envelope.data);
          return envelope.data.token;
        }),
        catchError((error: unknown) =>
          error instanceof HttpErrorResponse && error.status === 0
            ? throwError(() => new ApiError(ApiCodes.networkError, 0))
            : this.expire(),
        ),
        finalize(() => (this.pending = null)),
        shareReplay({ bufferSize: 1, refCount: false }),
      );

    return this.pending;
  }

  private expire(): Observable<never> {
    this.storage.clear();
    this.expired.next();
    return throwError(() => new ApiError(ApiCodes.sessionExpired, 401));
  }
}
