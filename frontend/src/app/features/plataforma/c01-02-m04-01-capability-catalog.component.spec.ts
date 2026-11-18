import { signal } from '@angular/core';
import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { routes } from '../../app.routes';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { authGuard } from '../../core/guards/auth.guard';
import { permisoGuard } from '../../core/guards/permiso.guard';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { C0102CapabilityCatalogComponent, c0102CatalogoDeRutas } from './c01-02-m04-01-capability-catalog.component';

describe('C01-02.M04.01 - catalogo read-only de capacidades', () => {
  it('declara ruta privada unica y guarda exacta', async () => {
    const ruta = routes.find(x => x.path === 'plataforma/capacidades-v19');
    expect(routes.filter(x => x.path === 'plataforma/capacidades-v19')).toHaveLength(1);
    expect(ruta?.canActivate).toEqual([plataformaGuard]);
    expect(await (ruta?.loadComponent as () => Promise<unknown>)()).toBe(C0102CapabilityCatalogComponent);
    const protegidas = routes.filter(x => x.canActivate?.includes(authGuard) && x.canActivate?.includes(permisoGuard) && x.data?.['modulo'] && x.data?.['accion'])
      .map(x => ({ ruta: '/' + x.path, modulo: x.data?.['modulo'], accion: x.data?.['accion'] }));
    expect(c0102CatalogoDeRutas(routes)).toEqual(protegidas);
  });
  it('oculta sin plataforma y falla cerrado sin tenant, permisos o ruta', async () => {
    const sesion = signal<PlataformaSesion | null>(null);
    const tenant = signal(false), cargado = signal(false), permiso = signal(false), ruta = signal(false);
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [C0102CapabilityCatalogComponent],
      providers: [
        provideRouter(routes),
        { provide: PlataformaSesionService, useValue: { sesion: sesion.asReadonly() } },
        { provide: TenantContextService, useValue: { tieneContextoVerificado: tenant.asReadonly() } },
        { provide: PermisosRuntimeService, useValue: { cargado: cargado.asReadonly(), puede: () => permiso(), rutaHabilitada: () => ruta() } }
      ]
    }).compileComponents();
    const fixture = TestBed.createComponent(C0102CapabilityCatalogComponent);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[data-testid="c0102-oculto"]')).not.toBeNull();
    expect(root.querySelectorAll('li')).toHaveLength(0);
    sesion.set({ nombreUsuario: 'QA', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true });
    fixture.detectChanges();
    expect(root.querySelectorAll('li')).toHaveLength(c0102CatalogoDeRutas(routes).length);
    expect(root.textContent).toContain('SIN_TENANT_VERIFICADO');
    tenant.set(true); fixture.detectChanges();
    expect(root.textContent).toContain('PERMISOS_NO_CONFIRMADOS');
    cargado.set(true); fixture.detectChanges();
    expect(root.textContent).toContain('PERMISO_O_CAPACIDAD_NO_CONFIRMADA');
    permiso.set(true); fixture.detectChanges();
    expect(root.textContent).toContain('RUTA_RESTRINGIDA');
    ruta.set(true); fixture.detectChanges();
    expect(root.textContent).toContain('SENAL_CLIENTE_NO_AUTORITATIVA');
    expect(root.querySelectorAll('form,button,input,textarea,select')).toHaveLength(0);
  });
  it('renderiza rutas protegidas duplicadas sin colision de claves Angular NG0955', async () => {
    const rutaExistente = routes.find(x => x.path === 'dashboard');
    expect(rutaExistente).toBeDefined();
    const rutasConDuplicado = [...routes, { ...rutaExistente! }];
    const sesion = signal<PlataformaSesion | null>({
      nombreUsuario: 'QA', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true
    });
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [C0102CapabilityCatalogComponent],
      providers: [
        provideRouter(rutasConDuplicado),
        { provide: PlataformaSesionService, useValue: { sesion: sesion.asReadonly() } },
        { provide: TenantContextService, useValue: { tieneContextoVerificado: () => false } },
        { provide: PermisosRuntimeService, useValue: {
          cargado: () => false, puede: () => false, rutaHabilitada: () => false
        } }
      ]
    }).compileComponents();
    const warning = vi.spyOn(console, 'warn').mockImplementation(() => {});
    try {
      const fixture = TestBed.createComponent(C0102CapabilityCatalogComponent);
      fixture.detectChanges();
      const listado = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('li'));
      expect(listado).toHaveLength(c0102CatalogoDeRutas(rutasConDuplicado).length);
      expect(listado.filter(li => li.textContent?.includes('Ruta: /dashboard'))).toHaveLength(2);
      expect(warning.mock.calls.flat().join(' ')).not.toContain('NG0955');
    } finally {
      warning.mockRestore();
    }
  });

});
