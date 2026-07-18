import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TokenStorageService } from '../services/auth/token-storage.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenStorage = inject(TokenStorageService);
  const accessToken = tokenStorage.getAccessToken();

  if (accessToken) {
    const authReq = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${accessToken}`),
      withCredentials: true // Important for sending/receiving HTTP-only cookies
    });
    return next(authReq);
  }

  const reqWithCreds = req.clone({
    withCredentials: true
  });

  return next(reqWithCreds);
};
