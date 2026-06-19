import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // If a token exists in local storage, they are logged in. Let them pass.
  if (authService.getToken()) {
    return true;
  }

  // Otherwise, kick them back to the login screen
  router.navigate(['/login']);
  return false;
};
