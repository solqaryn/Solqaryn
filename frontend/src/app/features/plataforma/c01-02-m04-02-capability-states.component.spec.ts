import { signal } from '@angular/core';
import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { routes } from '../../app.routes';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { C0102CapabilityStatesComponent, resolverEstadoCapacidad } from './c01-02-m04-02-capability-states.component';

describe('C01-02.M04.02 - estados Angular de capacidades', () => {
  const autorizado: PlataformaSesion = { nombreUsuario: 'QA', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true };
  it('tiene ruta privada y cubre todas las ramas de estados', async () => {
    const route = routes.find(x => x.path === 'plataforma/capacidades-estados-v19');
    expect(routes.filter(x => x.path === 'plataforma/capacidades-estados-v19')).toHaveLength(1);
    expect(route?.canActivate).toEqual([plataformaGuard]);
    expect(await (route?.loadComponent as () => Promise<unknown>)()).toBe(C0102CapabilityStatesComponent);
    const x = { autorizado: true, tenant: true, carga: 'lista' as const, permisos: true, total: 2, visibles: 1 };
    expect(resolverEstadoCapacidad({ ...x, autorizado: false })).toBe('DENEGADO');
    expect(resolverEstadoCapacidad({ ...x, tenant: false })).toBe('SIN_TENANT');
    expect(resolverEstadoCapacidad({ ...x, carga: 'cargando' })).toBe('CARGANDO');
    expect(resolverEstadoCapacidad({ ...x, carga: 'error' })).toBe('ERROR');
    expect(resolverEstadoCapacidad({ ...x, permisos: false })).toBe('ERROR');
    expect(resolverEstadoCapacidad({ ...x, total: 0, visibles: 0 })).toBe('VACIO');
    expect(resolverEstadoCapacidad({ ...x, visibles: 0 })).toBe('SIN_PERMISOS');
    expect(resolverEstadoCapacidad(x)).toBe('EXITO');
  });
  it('deniega sin sesion o tenant y nunca consulta permisos', async () => {
    const sesion = signal<PlataformaSesion | null>(null), tenant = signal(false);
    let llamadas = 0;
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({ imports: [C0102CapabilityStatesComponent], providers: [
      provideRouter(routes),
      { provide: PlataformaSesionService, useValue: { sesion: sesion.asReadonly() } },
      { provide: TenantContextService, useValue: { tieneContextoVerificado: tenant.asReadonly(), empresaIdVerificada: () => null } },
      { provide: PermisosRuntimeService, useValue: { cargado: () => false, puede: () => false, rutaHabilitada: () => false,
        cargar: () => { llamadas++; throw new Error('sin autorizacion'); } } }
    ] }).compileComponents();
    const f = TestBed.createComponent(C0102CapabilityStatesComponent);
    f.detectChanges();
    const node: HTMLElement = f.nativeElement;
    expect(node.querySelector('[data-testid="estado-denegado"]')).not.toBeNull();
    sesion.set(autorizado); f.detectChanges();
    expect(node.querySelector('[data-testid="estado-sin-tenant"]')).not.toBeNull();
    expect(node.querySelectorAll('li, button, form')).toHaveLength(0);
    expect(llamadas).toBe(0);
  });
  it('muestra carga y rechazo cuando el servicio no confirma', async () => {
    const respuesta = new Subject<boolean>();
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({ imports: [C0102CapabilityStatesComponent], providers: [
      provideRouter(routes),
      { provide: PlataformaSesionService, useValue: { sesion: signal<PlataformaSesion | null>(autorizado).asReadonly() } },
      { provide: TenantContextService, useValue: { tieneContextoVerificado: () => true, empresaIdVerificada: () => 41 } },
      { provide: PermisosRuntimeService, useValue: { cargado: () => false, puede: () => false, rutaHabilitada: () => false, cargar: () => respuesta.asObservable() } }
    ] }).compileComponents();
    const f = TestBed.createComponent(C0102CapabilityStatesComponent);
    f.detectChanges();
    const node: HTMLElement = f.nativeElement;
    expect(node.querySelector('[data-testid="estado-cargando"]')).not.toBeNull();
    respuesta.next(false); f.detectChanges();
    expect(node.querySelector('[data-testid="estado-error"]')).not.toBeNull();
    expect(node.querySelectorAll('li')).toHaveLength(0);
  });
  it('oculta rutas al revocar permiso o sesion', async () => {
    const sesion = signal<PlataformaSesion | null>(autorizado), permiso = signal(true);
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({ imports: [C0102CapabilityStatesComponent], providers: [
      provideRouter(routes),
      { provide: PlataformaSesionService, useValue: { sesion: sesion.asReadonly() } },
      { provide: TenantContextService, useValue: { tieneContextoVerificado: () => true, empresaIdVerificada: () => 41 } },
      { provide: PermisosRuntimeService, useValue: { cargado: () => true, puede: () => permiso(), rutaHabilitada: () => permiso(),
        cargar: () => { throw new Error('carga duplicada'); } } }
    ] }).compileComponents();
    const f = TestBed.createComponent(C0102CapabilityStatesComponent);
    f.detectChanges();
    const node: HTMLElement = f.nativeElement;
    expect(node.querySelector('[data-testid="estado-exito"]')).not.toBeNull();
    expect(node.querySelectorAll('li').length).toBeGreaterThan(0);
    permiso.set(false); f.detectChanges();
    expect(node.querySelector('[data-testid="estado-sin-permisos"]')).not.toBeNull();
    expect(node.querySelectorAll('li')).toHaveLength(0);
    permiso.set(true); sesion.set(null); f.detectChanges();
    expect(node.querySelector('[data-testid="estado-denegado"]')).not.toBeNull();
    expect(node.querySelectorAll('li')).toHaveLength(0);
  });
  it('mantiene claves distintas aun con rutas protegidas duplicadas (NG0955)', async () => {
    const dashboard = routes.find(x => x.path === 'dashboard');
    expect(dashboard).toBeDefined();
    const duplicadas = [...routes, { ...dashboard! }];
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({ imports: [C0102CapabilityStatesComponent], providers: [
      provideRouter(duplicadas),
      { provide: PlataformaSesionService, useValue: { sesion: signal<PlataformaSesion | null>(autorizado).asReadonly() } },
      { provide: TenantContextService, useValue: { tieneContextoVerificado: () => true, empresaIdVerificada: () => 41 } },
      { provide: PermisosRuntimeService, useValue: { cargado: () => true, puede: () => true,
        rutaHabilitada: () => true, cargar: () => { throw new Error('carga indebida'); } } }
    ] }).compileComponents();
    const warning = vi.spyOn(console, 'warn').mockImplementation(() => {});
    try {
      const f = TestBed.createComponent(C0102CapabilityStatesComponent);
      f.detectChanges();
      const filas = Array.from((f.nativeElement as HTMLElement).querySelectorAll('li'));
      expect(filas.filter(li => li.textContent?.includes('/dashboard'))).toHaveLength(2);
      expect(warning.mock.calls.flat().join(' ')).not.toContain('NG0955');
    } finally { warning.mockRestore(); }
  });

  it('cambio de tenant A a B cancela permiso A tardio y exige B', async () => {
    const empresa = signal(41), empresaCargada = signal<number | null>(null);
    const pendientes: Subject<boolean>[] = [];
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({ imports: [C0102CapabilityStatesComponent], providers: [
      provideRouter(routes),
      { provide: PlataformaSesionService, useValue: { sesion: signal<PlataformaSesion | null>(autorizado).asReadonly() } },
      { provide: TenantContextService, useValue: {
        tieneContextoVerificado: () => true, empresaIdVerificada: empresa.asReadonly()
      } },
      { provide: PermisosRuntimeService, useValue: {
        cargado: () => empresaCargada() === empresa(), puede: () => true, rutaHabilitada: () => true,
        cargar: () => { const p = new Subject<boolean>(); pendientes.push(p); return p.asObservable(); }
      } }
    ] }).compileComponents();
    const f = TestBed.createComponent(C0102CapabilityStatesComponent);
    f.detectChanges();
    expect(pendientes).toHaveLength(1);
    empresa.set(42); f.detectChanges();
    expect(pendientes).toHaveLength(2);
    expect(f.componentInstance.estado()).toBe('CARGANDO');
    pendientes[0].next(true); f.detectChanges();
    expect((f.nativeElement as HTMLElement).querySelectorAll('li')).toHaveLength(0);
    empresaCargada.set(42);
    pendientes[1].next(true); f.detectChanges();
    expect(f.componentInstance.estado()).toBe('EXITO');
    expect((f.nativeElement as HTMLElement).querySelectorAll('li').length).toBeGreaterThan(0);
    pendientes[0].error(new Error('A tardio')); f.detectChanges();
    expect(f.componentInstance.estado()).toBe('EXITO');
  });

  it('cambio A a B con rechazo no reexpone filas de A y finaliza ERROR', async () => {
    const empresa = signal(41), empresaCargada = signal<number | null>(41);
    const pendientes: Subject<boolean>[] = [];
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({ imports: [C0102CapabilityStatesComponent], providers: [
      provideRouter(routes),
      { provide: PlataformaSesionService, useValue: { sesion: signal<PlataformaSesion | null>(autorizado).asReadonly() } },
      { provide: TenantContextService, useValue: {
        tieneContextoVerificado: () => true, empresaIdVerificada: empresa.asReadonly()
      } },
      { provide: PermisosRuntimeService, useValue: {
        cargado: () => empresaCargada() === empresa(), puede: () => true, rutaHabilitada: () => true,
        cargar: () => { const p = new Subject<boolean>(); pendientes.push(p); return p.asObservable(); }
      } }
    ] }).compileComponents();
    const f = TestBed.createComponent(C0102CapabilityStatesComponent);
    f.detectChanges();
    expect(f.componentInstance.estado()).toBe('EXITO');
    empresa.set(42); f.detectChanges();
    expect(pendientes).toHaveLength(1);
    expect((f.nativeElement as HTMLElement).querySelectorAll('li')).toHaveLength(0);
    pendientes[0].next(false); f.detectChanges();
    expect(f.componentInstance.estado()).toBe('ERROR');
    expect((f.nativeElement as HTMLElement).querySelectorAll('li')).toHaveLength(0);
  });

});
