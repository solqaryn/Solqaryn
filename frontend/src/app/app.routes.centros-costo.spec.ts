import { TestBed } from '@angular/core/testing';
import { Router, Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { permisoGuard } from './core/guards/permiso.guard';
import { appConfig } from './app.config';
import { canonicalRoute, REQUIRED_QA_WIDTHS, RESPONSIVE_ROUTES, RESPONSIVE_VIEWPORTS } from './testing/qa-responsive-catalog';

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

// Informe de alcance de QA derivado del Router real, no de una lista manual de módulos.
describe('QA — Inventario de pantallas y permisos', () => {
  function recorrer(routes: Routes, prefix = '', authPadre = false, permisoPadre = false): {
    ruta: string; tipo: 'pantalla' | 'redireccion' | 'contenedor'; modulo: string | null; accion: string | null;
    autenticacion: boolean; permiso: boolean; parametros: string[];
  }[] {
    return routes.flatMap(route => {
      const fullPath = [prefix, route.path ?? ''].filter(Boolean).join('/');
      const auth = authPadre || Boolean(route.canActivate?.includes(authGuard));
      const permiso = permisoPadre || Boolean(route.canActivate?.includes(permisoGuard));
      const tipo = route.redirectTo !== undefined ? 'redireccion' : route.component || route.loadComponent ? 'pantalla' : 'contenedor';
      const entrada = {
        ruta: '/' + fullPath,
        tipo,
        modulo: typeof route.data?.['modulo'] === 'string' ? route.data['modulo'] as string : null,
        accion: typeof route.data?.['accion'] === 'string' ? route.data['accion'] as string : null,
        autenticacion: auth,
        permiso,
        parametros: fullPath.split('/').filter(segment => segment.startsWith(':')).map(segment => segment.slice(1))
      } as const;
      return [{ ...entrada, parametros: [...entrada.parametros] }, ...recorrer(route.children ?? [], fullPath, auth, permiso)];
    });
  }

  it('enumera las 151 inscripciones efectivas y conserva autenticación y permiso en pantallas con contrato RBAC', () => {
    TestBed.configureTestingModule({ providers: appConfig.providers });
    const inventario = recorrer(TestBed.inject(Router).config);
    expect(inventario).toHaveLength(151);
    expect(inventario.filter(item => item.tipo === 'pantalla')).toHaveLength(149);
    expect(inventario.filter(item => item.tipo === 'redireccion')).toHaveLength(2);
    expect(new Set(inventario.map(item => item.ruta)).size).toBe(inventario.length);
    expect(inventario.filter(item => item.modulo !== null && (!item.autenticacion || !item.permiso || !item.accion))).toEqual([]);
    // El barrido de navegador comparte el catálogo con esta prueba; no se
    // aceptan omisiones ni URLs inventadas por un escaneo regex de fuentes.
    const esperadas = inventario
      .filter(item => item.tipo === 'pantalla' || item.ruta === '/')
      .map(item => canonicalRoute(item.ruta)).sort();
    expect(new Set(esperadas).size).toBe(esperadas.length);
    expect([...RESPONSIVE_ROUTES].sort()).toEqual(esperadas);
    const anchos = RESPONSIVE_VIEWPORTS.map(viewport => Number(viewport.width));
    expect(new Set(anchos).size).toBe(anchos.length);
    expect(REQUIRED_QA_WIDTHS.filter(ancho => !anchos.includes(ancho))).toEqual([]);
    for (const item of inventario) {
      console.info('[QA-RUTA] ' + JSON.stringify(item));
    }
    console.info('[QA-RUTAS] 149 pantallas y 2 redirecciones. Este inventario no certifica renderizado ni API real.');
  });
});
