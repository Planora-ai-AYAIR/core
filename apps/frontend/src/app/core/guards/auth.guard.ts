import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { ROUTES } from '../../shared/config/constants';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated() || auth.hasPersistedSession()) {
    router.navigate([ROUTES.dashboard]);
    return false;
  }
  return true;
};
