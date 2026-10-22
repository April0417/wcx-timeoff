import { HttpErrorResponse } from '@angular/common/http';
import { ApiErrorBody } from './models';

export function readApiError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as ApiErrorBody | string | null;
    if (body && typeof body === 'object' && body.message) {
      return body.message;
    }
    if (error.status === 0) {
      return 'The API could not be reached. Confirm the backend and MongoDB are running.';
    }
    if (error.status === 401) {
      return 'Your session has expired. Sign in again.';
    }
    return `Request failed (${error.status}).`;
  }
  return 'Something went wrong.';
}
