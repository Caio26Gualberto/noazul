import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { API_BASE_URL } from './api-config';
import { envelopeCode } from './api-error';
import { TokenRefresher } from './token-refresher';
import { TokenStorage } from './token-storage';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith(API_BASE_URL)) return next(request);

  const storage = inject(TokenStorage);
  const refresher = inject(TokenRefresher);
  const token = storage.accessToken;

  return next(token ? withBearer(request, token) : request).pipe(
    catchError((error: unknown) => {
      if (!storage.hasSession || !isBearerRejection(error)) return throwError(() => error);
      return refresher.refresh().pipe(switchMap((fresh) => next(withBearer(request, fresh))));
    }),
  );
};

function withBearer(request: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return request.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

function isBearerRejection(error: unknown): boolean {
  return (
    error instanceof HttpErrorResponse && error.status === 401 && envelopeCode(error.error) === null
  );
}
