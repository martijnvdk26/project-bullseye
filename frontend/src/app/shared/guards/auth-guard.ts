import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth';

// Route guard for pages that should only be reachable by a logged-in
// registered player (e.g. /dashboard). Not used on the live game board -
// that route is reached exclusively through the guest-lobby flow, which
// never produces a token, so attaching this guard there would lock every
// guest player out.
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
