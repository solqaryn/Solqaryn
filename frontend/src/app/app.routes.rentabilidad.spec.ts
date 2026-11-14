import { Route } from '@angular/router';
import { permisoGuard } from './core/guards/permiso.guard';
import { RentabilidadComponent } from './features/rentabilidad/rentabilidad.component';
import { routes } from './app.routes';

describe('N5.4.E.6 profitability route', () => {
  const centroReportes = routes.find(route => route.path === 'centro-reportes');
  const rentabilidad = centroReportes?.children?.find(route => route.path === 'rentabilidad') as Route | undefined;

  it('is exposed only through the authenticated report center with Ventas/Ver permission', () => {
    expect(centroReportes).toBeDefined();
    expect(rentabilidad).toBeDefined();
    expect(rentabilidad?.canActivate).toEqual([permisoGuard]);
    expect(rentabilidad?.data).toEqual({ modulo: 'Ventas', accion: 'Ver' });
  });

  it('lazy-loads the profitability page component', async () => {
    const loader = rentabilidad?.loadComponent;
    expect(typeof loader).toBe('function');
    const component = await (loader as () => Promise<unknown>)();
    expect(component).toBe(RentabilidadComponent);
  });
});
