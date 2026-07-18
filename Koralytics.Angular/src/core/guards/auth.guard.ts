import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth/auth.service';
import { TokenStorageService } from '../services/auth/token-storage.service';
import { ToastService } from '../services/Toast/toast';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const tokenStorage = inject(TokenStorageService);
  const toast = inject(ToastService);

  if (authService.isLoggedIn()) {
    if (!tokenStorage.getUser()) {
      toast.show('Please complete your profile first.', 'warning');
      return router.createUrlTree(['/auth/complete-profile']);
    }
    return true;
  }

  // Navigate to login page
  return router.createUrlTree(['/auth/login'], { queryParams: { returnUrl: state.url }});
};
