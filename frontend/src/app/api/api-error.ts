import { HttpErrorResponse } from '@angular/common/http';
import { ApiCodes } from './api-codes';

export class ApiError extends Error {
  constructor(
    readonly code: string,
    readonly status: number,
  ) {
    super(code);
    this.name = 'ApiError';
  }
}

export function envelopeCode(body: unknown): string | null {
  if (typeof body !== 'object' || body === null) return null;
  const code = (body as { code?: unknown }).code;
  return typeof code === 'string' && code.length > 0 ? code : null;
}

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error;
  if (!(error instanceof HttpErrorResponse)) return new ApiError(ApiCodes.unknownError, 0);
  if (error.status === 0) return new ApiError(ApiCodes.networkError, 0);
  const code = envelopeCode(error.error);
  if (code) return new ApiError(code, error.status);
  if (error.status === 401) return new ApiError(ApiCodes.sessionExpired, 401);
  return new ApiError(ApiCodes.unknownError, error.status);
}

export function ignoreApiError(error: unknown): void {
  if (!(error instanceof ApiError)) throw error;
}
