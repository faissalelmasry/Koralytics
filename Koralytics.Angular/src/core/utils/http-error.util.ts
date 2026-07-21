import { HttpErrorResponse } from '@angular/common/http';

/**
 * Extracts a user-facing message from a failed HTTP response.
 *
 * The backend's GlobalExceptionHandler / AddProblemDetails() typically returns
 * a body shaped like { title, detail, status, ... } (RFC 7807 ProblemDetails),
 * but plain-string or { message } bodies are also handled defensively.
 */
export function extractErrorMessage(err: HttpErrorResponse, fallback: string): string {
  const body = err?.error;

  if (typeof body === 'string' && body.trim().length > 0) {
    return body;
  }

  if (body && typeof body === 'object') {
    if (typeof body.detail === 'string' && body.detail.trim().length > 0) {
      return body.detail;
    }
    if (typeof body.message === 'string' && body.message.trim().length > 0) {
      return body.message;
    }
    if (typeof body.title === 'string' && body.title.trim().length > 0) {
      return body.title;
    }
  }

  return fallback;
}