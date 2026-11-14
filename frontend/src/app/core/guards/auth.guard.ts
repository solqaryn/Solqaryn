import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { SessionActivityService } from '../auth/session-activity.service';
import { TenantContextService } from '../auth/tenant-context.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const sessionActivity = inject(SessionActivityService);
  const tenantContext = inject(TenantContextService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    // Autenticación por sí sola no autoriza el shell ERP. Cada navegación
    // protegida exige un contexto tenant materializado por el backend.
    if (tenantContext.tieneContextoVerificado()) return true;

    // Tras una recarga completa el contexto verificado vive únicamente en memoria,
    // pero la empresa solicitada puede persistir como una intención no autoritativa.
    // Revalidarla aquí conserva la ruta solicitada sin convertir localStorage en
    // autoridad: sólo se permite continuar si el backend vuelve a confirmar la
    // membresía activa para el usuario autenticado.
    const empresaSolicitadaId = tenantContext.empresaSolicitadaId();
    if (empresaSolicitadaId) {
      return tenantContext.seleccionarEmpresa(empresaSolicitadaId).pipe(
        map(() => tenantContext.tieneContextoVerificado()
          ? true
          : router.parseUrl('/login')),
        catchError(() => of(router.parseUrl('/login')))
      );
    }

    return router.parseUrl('/login');
  }

  if (authService.getToken() && authService.isTokenExpired()) {
    tenantContext.limpiar();
    sessionActivity.cerrarPor401();
    return false;
  }

  tenantContext.limpiar();
  return router.parseUrl('/login');
};
