import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { routes } from '../../app.routes';
import { C1304InventarioGlobalComponent } from './c13-04-m04-01-catalogo-rutas.component';
import { inventariarRutasV19, PROGRAMAS_PLAN_V19 } from './c13-04-m04-01-catalogo-rutas';

describe('C13-04.M04.01 inventario global del plan y Router real', () => {
  const admin: PlataformaSesion = { nombreUsuario: 'Admin', rolNombre: 'Administrador', ambito: 'Plataforma', esAdministrador: true };
  async function montar(inicial: PlataformaSesion | null) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(inicial);
    await TestBed.configureTestingModule({
      imports: [C1304InventarioGlobalComponent],
      providers: [provideRouter(routes), { provide: PlataformaSesionService, useValue: { sesion } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(C1304InventarioGlobalComponent);
    fixture.detectChanges();
    return { fixture, sesion };
  }

  it('16/16 programas planificados únicos, sin atribuir cobertura o CERTIFICACION', () => {
    expect(PROGRAMAS_PLAN_V19).toHaveLength(16);
    expect(new Set(PROGRAMAS_PLAN_V19.map(m => m.id)).size).toBe(16);
    expect(PROGRAMAS_PLAN_V19.map(m => m.id)).toEqual(
      Array.from({ length: 16 }, (_, i) => 'C' + String(i).padStart(2, '0'))
    );
  });

  it('recorre el Router real: registro, hijos, detalle/edición y enlaces sólo estáticos', () => {
    const inventario = inventariarRutasV19(routes);
    expect(inventario.length).toBeGreaterThanOrEqual(108);
    expect(inventario.some(r => r.path === 'clientes/:id/editar' && r.tipo === 'edicion' && r.rutaNavegable === null)).toBe(true);
    expect(inventario.some(r => r.path === 'usuarios/:id' && r.tipo === 'detalle' && r.rutaNavegable === null)).toBe(true);
    expect(inventario.some(r => r.path === 'centro-reportes/financieros' && r.protegidaPorRuta)).toBe(true);
    expect(inventario.some(r => r.path === 'plataforma/inventario-rutas-v19' && r.protegidaPorRuta)).toBe(true);
    expect(inventario.find(r => r.path === 'tienda/productos')?.rutaNavegable).toBe('/tienda/productos');
    expect(inventario.some(r => r.tipo === 'comodin' && r.rutaNavegable !== null)).toBe(false);
  });

  it('rutas construidas de entrada hostil nunca generan enlace remoto o paramétrico', () => {
    const inventario = inventariarRutasV19([
      { path: 'clientes/:id/editar', component: C1304InventarioGlobalComponent },
      { path: 'javascript:alert(1)', component: C1304InventarioGlobalComponent },
      { path: 'centro', children: [{ path: 'detalle/:id', component: C1304InventarioGlobalComponent }] },
      { path: 'lazy', loadChildren: async () => [] }
    ]);
    expect(inventario).toHaveLength(5);
    expect(inventario.filter(r => r.rutaNavegable !== null).map(r => r.rutaNavegable)).toEqual(['/centro', '/lazy']);
    expect(inventario.find(r => r.path === 'lazy')?.hijosLazySinResolver).toBe(true);
    expect(inventario.find(r => r.path === 'centro/detalle/:id')?.tipo).toBe('detalle');
  });

  it('ruta global protegida y vista 16 + Router completo sólo para admin de Plataforma', async () => {
    const entrada = routes.filter(r => r.path === 'plataforma/inventario-rutas-v19');
    expect(entrada).toHaveLength(1);
    expect(entrada[0].canActivate).toEqual([plataformaGuard]);
    expect(await entrada[0].loadComponent?.()).toBe(C1304InventarioGlobalComponent);
    const { fixture } = await montar(admin);
    expect(fixture.nativeElement.querySelectorAll('.programas li')).toHaveLength(16);
    expect(fixture.nativeElement.querySelectorAll('.rutas li').length).toBeGreaterThanOrEqual(108);
    expect(fixture.nativeElement.textContent).toContain('SIN_VERIFICAR');
  });

  it('anonimo, Empresa y revocacion ocultan programas, rutas y enlaces', async () => {
    for (const initial of [null, { ...admin, ambito: 'Empresa' } as unknown as PlataformaSesion]) {
      const { fixture } = await montar(initial);
      expect(fixture.nativeElement.querySelectorAll('.programas li,.rutas li')).toHaveLength(0);
      expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    }
    const { fixture, sesion } = await montar(admin);
    expect(fixture.nativeElement.querySelectorAll('.programas li')).toHaveLength(16);
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.programas li,.rutas li')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).not.toContain('C15 —');
  });
});
