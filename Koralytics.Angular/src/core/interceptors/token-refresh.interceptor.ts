import { HttpInterceptorFn, HttpErrorResponse, HttpEvent, HttpRequest, HttpHandlerFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { throwError, BehaviorSubject, Observable } from 'rxjs';
import { AuthService } from '../services/auth/auth.service';

let isRefreshing = false;
const refreshTokenSubject = new BehaviorSubject<any>(null);

export const tokenRefreshInterceptor: HttpInterceptorFn = (req, next): Observable<HttpEvent<unknown>> => {
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // 401 Unauthorized
      // Skip token refresh for:
      // - login endpoint (would cause infinite loop)
      // - complete-profile endpoints (they use a temporary token, no real refresh token exists yet)
      const isLoginUrl = req.url.includes('/Auth/login');
      const isCompleteProfileUrl = req.url.includes('/complete-profile/');
      if (error.status === 401 && !isLoginUrl && !isCompleteProfileUrl) {
        return handle401Error(req, next, authService);
      }
      return throwError(() => error);
    })
  );
};

function handle401Error(request: HttpRequest<unknown>, next: HttpHandlerFn, authService: AuthService): Observable<HttpEvent<unknown>> {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshTokenSubject.next(null);

    return authService.refreshToken().pipe(
      switchMap((response) => {
        isRefreshing = false;
        
        if (response.isSuccess && response.data) {
          refreshTokenSubject.next(response.data.accessToken);
          return next(request.clone({
            headers: request.headers.set('Authorization', `Bearer ${response.data.accessToken}`),
            withCredentials: true
          }));
        }

        // If refresh fails or returns bad data, logout
        authService.logout().subscribe();
        return throwError(() => new Error('Refresh failed'));
      }),
      catchError((err) => {
        isRefreshing = false;
        authService.logout().subscribe();
        return throwError(() => err);
      })
    );
  } else {
    // Wait for the refreshing to complete
    return refreshTokenSubject.pipe(
      filter(token => token != null),
      take(1),
      switchMap(jwt => {
        return next(request.clone({
          headers: request.headers.set('Authorization', `Bearer ${jwt}`),
          withCredentials: true
        }));
      })
    );
  }
}
