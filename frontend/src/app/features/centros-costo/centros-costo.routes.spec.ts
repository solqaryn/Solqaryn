import { authGuard } from '../../core/guards/auth.guard';
import { permisoGuard } from '../../core/guards/permiso.guard';
import { CENTROS_COSTO_ROUTES } from './centros-costo.routes';

describe('N4.11.E CentroCosto route contract', () => {
  it('publishes exactly one protected centros-costo route', () => {
    expect(CENTROS_COSTO_ROUTES).toHaveLength(1);
    const route = CENTROS_COSTO_ROUTES[0];

    expect(route.path).toBe('centros-costo');
    expect(route.canActivate).toEqual([authGuard, permisoGuard]);
    expect(route.data).toEqual({ modulo: 'Finanzas', accion: 'Ver' });
    expect(route.loadComponent).toBeTypeOf('function');
  });
});
