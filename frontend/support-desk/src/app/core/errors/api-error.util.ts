import { HttpErrorResponse } from '@angular/common/http';
import { ApiError } from './api-error.model';

function toCamelCase(key: string): string {
  if (!key) {
    return key;
  }
  return key.charAt(0).toLowerCase() + key.slice(1);
}

function normalizeFieldErrors(
  errors: Record<string, string[]> | undefined,
): Record<string, string[]> | undefined {
  if (!errors) {
    return undefined;
  }

  const normalized: Record<string, string[]> = {};
  for (const [key, messages] of Object.entries(errors)) {
    const camel = toCamelCase(key);
    normalized[camel] = messages;
    // Keep original key too so either casing can be looked up.
    if (camel !== key) {
      normalized[key] = messages;
    }
  }
  return normalized;
}

/**
 * Extracts a stable ApiError from ASP.NET ProblemDetails / ValidationProblemDetails.
 * Extensions such as code, currentStatus, requestedStatus, reason are flattened to the root.
 */
export function extractApiError(error: unknown): ApiError {
  if (!(error instanceof HttpErrorResponse)) {
    return {
      status: 0,
      detail: 'An unexpected error occurred.',
      code: 'UNEXPECTED_ERROR',
    };
  }

  const body = error.error as Record<string, unknown> | string | null | undefined;

  if (!body || typeof body !== 'object') {
    return {
      status: error.status,
      detail: error.message || 'Request failed.',
      code: error.status === 0 ? 'NETWORK_ERROR' : undefined,
    };
  }

  const fieldErrors = normalizeFieldErrors(
    body['errors'] as Record<string, string[]> | undefined,
  );

  const code =
    (typeof body['code'] === 'string' ? body['code'] : undefined) ??
    (typeof body['title'] === 'string' ? body['title'] : undefined);

  const context: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(body)) {
    if (
      [
        'type',
        'title',
        'status',
        'detail',
        'instance',
        'errors',
        'code',
        'traceId',
        'currentStatus',
        'requestedStatus',
        'reason',
        'debugMessage',
      ].includes(key)
    ) {
      continue;
    }
    context[key] = value;
  }

  return {
    status: error.status,
    title: typeof body['title'] === 'string' ? body['title'] : undefined,
    detail:
      typeof body['detail'] === 'string'
        ? body['detail']
        : 'Request failed.',
    code,
    fieldErrors,
    currentStatus:
      typeof body['currentStatus'] === 'string'
        ? body['currentStatus']
        : undefined,
    requestedStatus:
      typeof body['requestedStatus'] === 'string'
        ? body['requestedStatus']
        : undefined,
    reason: typeof body['reason'] === 'string' ? body['reason'] : undefined,
    context: Object.keys(context).length > 0 ? context : undefined,
  };
}

/** Best user-facing message from an ApiError (never stack traces). */
export function apiErrorMessage(error: ApiError): string {
  if (error.detail) {
    return error.detail;
  }
  if (error.code) {
    return error.code;
  }
  return 'Request failed.';
}

/** Apply field errors onto a Reactive Forms group by control name (camelCase). */
export function applyFieldErrors(
  setError: (controlName: string, message: string) => void,
  fieldErrors: Record<string, string[]> | undefined,
): void {
  if (!fieldErrors) {
    return;
  }
  for (const [key, messages] of Object.entries(fieldErrors)) {
    const camel = toCamelCase(key);
    const message = messages?.[0];
    if (message) {
      setError(camel, message);
    }
  }
}
