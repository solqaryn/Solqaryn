import { authGuard } from './core/guards/auth.guard';
import { permisoGuard } from './core/guards/permiso.guard';
import { routes } from './app.routes';

describe('centro-reportes/ventas route', () => {
  it('keeps the sales report behind authentication and Ventas/Ver permission', () => {
    const centroReportes = routes.find(route => route.path === 'centro-reportes');
    const ventas = centroReportes?.children?.find(route => route.path === 'ventas');

    expect(centroReportes).toBeDefined();
    expect(centroReportes?.canActivate).toContain(authGuard);
    expect(ventas).toBeDefined();
    expect(ventas?.canActivate).toContain(permisoGuard);
    expect(ventas?.data).toEqual(expect.objectContaining({ modulo: 'Ventas', accion: 'Ver' }));
    expect(typeof ventas?.loadComponent).toBe('function');
  });
});
