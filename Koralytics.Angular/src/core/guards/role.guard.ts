import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth/auth.service';

export const roleGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isLoggedIn()) {
    return router.createUrlTree(['/auth/login']);
  }

  const expectedRoles: string[] = route.data['roles'];
  if (!expectedRoles || expectedRoles.length === 0) {
    return true;
  }

  const userRoles = authService.getUserRoles();
  const hasRole = expectedRoles.some(role => userRoles.includes(role));

  if (hasRole) {
    return true;
  }

  // Navigate to dashboard if role is not allowed
  return router.createUrlTree(['/dashboard']);
};
