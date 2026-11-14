import { Route } from '@angular/router';
import { routes } from './app.routes';

describe('ruta del reporte de compras', () => {
  it('queda protegida por Compras/Ver dentro del centro de reportes', () => {
    const centroReportes = routes.find(route => route.path === 'centro-reportes');
    const reporteCompras = centroReportes?.children?.find((route: Route) => route.path === 'compras');

    expect(reporteCompras).toBeDefined();
    expect(reporteCompras?.data?.['modulo']).toBe('Compras');
    expect(reporteCompras?.data?.['accion']).toBe('Ver');
    expect(reporteCompras?.canActivate?.length).toBeGreaterThan(0);
    expect(reporteCompras?.loadComponent).toBeDefined();
  });
});
