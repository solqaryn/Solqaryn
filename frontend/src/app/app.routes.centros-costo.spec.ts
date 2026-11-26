import { TestBed } from '@angular/core/testing';
import { Router, Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { permisoGuard } from './core/guards/permiso.guard';
import { appConfig } from './app.config';

describe('N4.11.G CentroCosto application route registration', () => {
  it('registers the protected centros-costo route in the application router', () => {
    TestBed.configureTestingModule({ providers: appConfig.providers });

    const router = TestBed.inject(Router);
    const route = router.config.find(candidate => candidate.path === 'centros-costo');

    expect(route).toBeDefined();
    expect(route?.canActivate).toHaveLength(2);
    expect(route?.data).toEqual({ modulo: 'Finanzas', accion: 'Ver' });
    expect(route?.loadComponent).toBeTypeOf('function');
  });
});

// Comprueba la configuración efectiva, incluidos los arrays agregados en appConfig.
describe('QA — Registro único de rutas institucionales', () => {
  function flatten(routes: Routes, prefix = ''): string[] {
    return routes.flatMap(route => {
      const fullPath = [prefix, route.path ?? ''].filter(Boolean).join('/');
      return [fullPath, ...flatten(route.children ?? [], fullPath)];
    });
  }

  it('registra cada ruta una sola vez, incluidas las rutas hijas de reportes', () => {
    TestBed.configureTestingModule({ providers: appConfig.providers });
    const router = TestBed.inject(Router);
    const paths = flatten(router.config);
    const duplicates = paths.filter((path, index) => paths.indexOf(path) !== index);
    expect(duplicates).toEqual([]);
    expect(router.config.at(-1)?.path).toBe('**');
    expect(paths).toContain('centro-reportes/ventas');
    expect(paths).toContain('centro-reportes/compras');
  });

  it('conserva los permisos de los documentos, transferencias y cuentas por cobrar', () => {
    TestBed.configureTestingModule({ providers: appConfig.providers });
    const routes = TestBed.inject(Router).config;
    const contracts = [
      ['inventario/transferencias', 'MovimientosInventario', 'Ver'],
      ['inventario/transferencias/nueva', 'MovimientosInventario', 'Crear'],
      ['inventario/transferencias/:id/editar', 'MovimientosInventario', 'Editar'],
      ['inventario/transferencias/:id', 'MovimientosInventario', 'Ver'],
      ['facturas-proveedor', 'Compras', 'Ver'],
      ['facturas-proveedor/nueva', 'Compras', 'Crear'],
      ['facturas-proveedor/:id/editar', 'Compras', 'Editar'],
      ['facturas-proveedor/:id', 'Compras', 'Ver'],
      ['cuentas-por-cobrar', 'Facturacion', 'Ver'],
      ['cuentas-por-pagar', 'Finanzas', 'Ver'],
      ['compras/:id/three-way-match', 'Compras', 'Ver']
    ];
    for (const [path, modulo, accion] of contracts) {
      const matching = routes.filter(route => route.path === path);
      expect(matching).toHaveLength(1);
      expect(matching[0].canActivate).toEqual([authGuard, permisoGuard]);
      expect(matching[0].data).toMatchObject({ modulo, accion });
      expect(matching[0].loadComponent).toBeTypeOf('function');
    }
  });

  it('mantiene el componente activo de cuentas por cobrar y su acceso a facturas', async () => {
    TestBed.configureTestingModule({ providers: appConfig.providers });
    const route = TestBed.inject(Router).config.find(item => item.path === 'cuentas-por-cobrar');
    const { CuentasPorCobrarComponent } = await import('./features/cuentas-por-cobrar/cuentas-por-cobrar.component');
    expect(await route?.loadComponent?.()).toBe(CuentasPorCobrarComponent);
  });
});
