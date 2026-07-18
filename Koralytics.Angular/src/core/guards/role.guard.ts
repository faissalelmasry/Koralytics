import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth/auth.service';
import { TokenStorageService } from '../services/auth/token-storage.service';
import { ToastService } from '../services/Toast/toast';

export const roleGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const tokenStorage = inject(TokenStorageService);
  const toast = inject(ToastService);

  if (!authService.isLoggedIn()) {
    return router.createUrlTree(['/auth/login']);
  }

  if (!tokenStorage.getUser()) {
    toast.show('Please complete your profile first.', 'warning');
    return router.createUrlTree(['/auth/complete-profile']);
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
