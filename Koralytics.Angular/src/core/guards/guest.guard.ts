import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth/auth.service';
import { TokenStorageService } from '../services/auth/token-storage.service';
import { ToastService } from '../services/Toast/toast';

export const guestGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const tokenStorage = inject(TokenStorageService);
  const toast = inject(ToastService);

  if (!authService.isLoggedIn()) {
    return true;
  }

  if (!tokenStorage.getUser()) {
    if (state.url.includes('/auth/complete-profile')) {
      return true;
    }
    toast.show('Please complete your profile first.', 'warning');
    return router.createUrlTree(['/auth/complete-profile']);
  }

  // Navigate to dashboard if already logged in
  return router.createUrlTree(['/dashboard']);
};
