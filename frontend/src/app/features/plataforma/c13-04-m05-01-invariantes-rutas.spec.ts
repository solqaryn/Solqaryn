import type { Routes } from '@angular/router';
import { inventariarRutasV19, PROGRAMAS_PLAN_V19 } from './c13-04-m04-01-catalogo-rutas';

describe('C13-04.M05.01 reglas reproducibles del catálogo de rutas', () => {
  it('conserva los 16 IDs comerciales únicos sin inferir módulos implementados', () => {
    expect(PROGRAMAS_PLAN_V19.map(p => p.id)).toEqual(
      Array.from({ length: 16 }, (_, i) => 'C' + String(i).padStart(2, '0'))
    );
    expect(new Set(PROGRAMAS_PLAN_V19.map(p => p.id)).size).toBe(16);
    expect(PROGRAMAS_PLAN_V19.every(p => p.nombre.trim().length > 0)).toBe(true);
  });

  it('hereda los guards por hijos, identifica edición y sólo enlaza rutas estáticas', () => {
    const rutas: Routes = [
      { path: 'plataforma', canMatch: [() => true], children: [
        { path: 'clientes', children: [{ path: ':id/editar' }, { path: 'nuevo' }] }
      ] },
      { path: 'tienda', children: [{ path: 'productos' }] }
    ];
    const inventario = inventariarRutasV19(rutas);
    expect(inventario.map(r => r.path)).toEqual([
      'plataforma', 'plataforma/clientes', 'plataforma/clientes/:id/editar',
      'plataforma/clientes/nuevo', 'tienda', 'tienda/productos'
    ]);
    expect(inventario.filter(r => r.path.startsWith('plataforma')).every(r => r.protegidaPorRuta)).toBe(true);
    expect(inventario.find(r => r.path.endsWith(':id/editar'))).toMatchObject({
      tipo: 'edicion', rutaNavegable: null, superficie: 'plataforma'
    });
    expect(inventario.find(r => r.path === 'plataforma/clientes/nuevo')).toMatchObject({
      tipo: 'creacion', rutaNavegable: '/plataforma/clientes/nuevo'
    });
    expect(inventario.find(r => r.path === 'tienda/productos')).toMatchObject({
      superficie: 'publica', rutaNavegable: '/tienda/productos'
    });
  });

  it('deniega vínculos dinámicos, remotos, scripts, redirecciones y comodines', () => {
    const inventario = inventariarRutasV19([
      { path: 'clientes/:id' }, { path: 'javascript:alert(1)' },
      { path: 'https://evil.example' }, { path: 'legacy', redirectTo: '/login' },
      { path: '**', redirectTo: '/login' }, { path: 'seguros' }
    ]);
    expect(inventario.slice(0, 5).every(r => r.rutaNavegable === null)).toBe(true);
    expect(inventario[3].tipo).toBe('redirect');
    expect(inventario[4].tipo).toBe('comodin');
    expect(inventario[5].rutaNavegable).toBe('/seguros');
  });

  it('no ejecuta proveedores lazy: su cobertura profunda permanece desconocida', () => {
    let invoked = 0;
    const inventario = inventariarRutasV19([
      { path: 'lazy', loadChildren: () => { invoked++; throw new Error('lazy no debe ejecutarse'); } },
      { path: 'privado', canActivate: [() => false] }
    ]);
    expect(invoked).toBe(0);
    expect(inventario[0]).toMatchObject({ hijosLazySinResolver: true, rutaNavegable: '/lazy' });
    expect(inventario[1]).toMatchObject({ protegidaPorRuta: true, superficie: 'erp' });
  });

  it('cubre raíz, matcher y objetos congelados sin inventar navegación', () => {
    const inventario = inventariarRutasV19([{ path: '' }, { matcher: () => null }]);
    expect(inventario).toHaveLength(2);
    expect(inventario[0]).toMatchObject({ path: '(raíz)', rutaNavegable: null });
    expect(inventario[1]).toMatchObject({ path: '[matcher]', tipo: 'matcher', rutaNavegable: null });
    expect(Object.isFrozen(inventario)).toBe(true);
    expect(inventario.every(item => Object.isFrozen(item))).toBe(true);
    expect(inventario.map(item => item.id)).toEqual(['1', '2']);
  });
});
