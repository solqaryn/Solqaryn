import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of, switchMap } from 'rxjs';
import { PermisosRuntimeService } from '../auth/permisos-runtime.service';
import { TenantContextService } from '../auth/tenant-context.service';

export const permisoGuard: CanActivateFn = (route) => {
  const permisosRuntime = inject(PermisosRuntimeService);
  const tenantContext = inject(TenantContextService);
  const router = inject(Router);

  const modulo = route.data?.['modulo'] as string | undefined;
  const accion = route.data?.['accion'] as string | undefined;

  if (!modulo || !accion) return true;

  const resolverPermiso = () => permisosRuntime.cargar().pipe(
    map((ok) => ok &&
      permisosRuntime.puede(modulo, accion) &&
      permisosRuntime.rutaHabilitada(route.routeConfig?.path)
        ? true
        : router.createUrlTree([permisosRuntime.rutaInicialPermitida() ?? '/login']))
  );

  // authGuard y permisoGuard pertenecen a la misma navegación protegida. Tras una
  // recarga completa el contexto tenant verificado vuelve a memoria mediante una
  // petición asíncrona; permisoGuard no puede interpretar ese intervalo como una
  // denegación y redirigir al dashboard/login antes de que termine la revalidación.
  // La empresa persistida sigue siendo sólo una solicitud: se vuelve a confirmar
  // server-side antes de consultar permisos. Cualquier fallo permanece fail-closed.
  if (tenantContext.tieneContextoVerificado()) {
    return resolverPermiso();
  }

  const empresaSolicitadaId = tenantContext.empresaSolicitadaId();
  if (!empresaSolicitadaId) {
    return router.createUrlTree(['/login']);
  }

  return tenantContext.seleccionarEmpresa(empresaSolicitadaId).pipe(
    switchMap(() => resolverPermiso()),
    catchError(() => of(router.createUrlTree(['/login'])))
  );
};
