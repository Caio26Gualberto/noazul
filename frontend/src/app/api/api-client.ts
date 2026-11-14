import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, throwError } from 'rxjs';
import { ApiCodes } from './api-codes';
import { API_BASE_URL } from './api-config';
import { ApiError, toApiError } from './api-error';
import { FeedbackService } from './feedback.service';
import { ApiEnvelope } from './types';

export interface RequestOptions {
  silent?: boolean;
  announce?: boolean;
}

@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);
  private readonly feedback = inject(FeedbackService);

  get<T>(path: string, options: RequestOptions = {}): Observable<T> {
    return this.send<T>('GET', path, undefined, options);
  }

  post<T>(path: string, body?: unknown, options: RequestOptions = {}): Observable<T> {
    return this.send<T>('POST', path, body, options);
  }

  patch<T>(path: string, body?: unknown, options: RequestOptions = {}): Observable<T> {
    return this.send<T>('PATCH', path, body, options);
  }

  private send<T>(
    method: string,
    path: string,
    body: unknown,
    options: RequestOptions,
  ): Observable<T> {
    return this.http.request<ApiEnvelope<T>>(method, `${API_BASE_URL}${path}`, { body }).pipe(
      map((envelope) => {
        if (!envelope.isSuccess) throw new ApiError(envelope.code, 200);
        if (options.announce) this.feedback.show(envelope.code);
        return envelope.data as T;
      }),
      catchError((error: unknown) => {
        const apiError = toApiError(error);
        if (!options.silent && apiError.code !== ApiCodes.sessionExpired) {
          this.feedback.show(apiError.code);
        }
        return throwError(() => apiError);
      }),
    );
  }
}
