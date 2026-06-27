import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { ROUTES } from '../../shared/config/constants';

export const roleGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const token = auth.accessToken;
  if (!token) {
    router.navigate([ROUTES.signIn]);
    return false;
  }

  try {
    // Decode JWT payload
    const payloadBase64 = token.split('.')[1];
    const decodedPayload = JSON.parse(atob(payloadBase64));

    // Look up Microsoft/WS-Federation format role URI used by your API response
    const userRole = decodedPayload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
    const allowedRoles = route.data?.['roles'] as Array<string>;

    if (!allowedRoles || allowedRoles.includes(userRole)) {
      return true;
    }

    // Redirect to unauthorized / fallback if they don't have the right clearance
    router.navigate([ROUTES.dashboard]);
    return false;
  } catch (e) {
    router.navigate([ROUTES.signIn]);
    return false;
  }
};
