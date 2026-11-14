import { HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { PlataformaSesionService } from '../auth/plataforma-sesion.service';

/**
 * El shell ERP exige un tenant, pero la plataforma no debe exigir EmpresaId.
 * El backend vuelve a decidir en cada GET; este guard es solo UX fail-closed.
 */
export const plataformaGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const plataforma = inject(PlataformaSesionService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    plataforma.limpiar();
    return router.createUrlTree(['/login']);
  }

  return plataforma.obtener().pipe(
    map(() => true),
    catchError((error: HttpErrorResponse) => of(router.createUrlTree(['/login'], {
      queryParams: { plataforma: error?.status === 403 ? 'denegado' : 'no-disponible' }
    })))
  );
};
