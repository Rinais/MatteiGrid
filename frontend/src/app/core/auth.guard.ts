import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  // Redirect to login page and preserve requested destination
  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url }
  });
};

export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return router.createUrlTree(['/dashboard']);
  }

  return true;
};

/**
 * Permission guard checking if current user has the required permission key.
 * NOTE: Frontend guards provide UX guidance and navigation control.
 * The backend remains the authoritative, immutable enforcement layer for all resources.
 */
export const permissionGuard = (permissionKey: string): CanActivateFn => {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isAuthenticated()) {
      return router.createUrlTree(['/login']);
    }

    if (auth.hasPermission(permissionKey)) {
      return true;
    }

    // Access Denied - redirect to dashboard with unauthorized notification
    return router.createUrlTree(['/dashboard'], { queryParams: { unauthorized: permissionKey } });
  };
};
