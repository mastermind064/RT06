import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { UserRole } from '../models/user.model';

export const RoleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const roles = route.data?.['roles'] as UserRole[] | undefined;
  if (!roles) {
    return true;
  }
  if (authService.isAuthenticated() && authService.validateToken() && roles.includes(authService.currentUser()?.role ?? 'WARGA')) {
    return true;
  }
  router.navigate(['/login']);
  return false;
};
