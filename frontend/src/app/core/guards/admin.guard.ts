import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';

/// Permite el acceso solo a usuarios con rol Administrador.
export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated() && authService.esAdministrador()) {
    return true;
  }

  router.navigate(['/dashboard']);
  return false;
};
