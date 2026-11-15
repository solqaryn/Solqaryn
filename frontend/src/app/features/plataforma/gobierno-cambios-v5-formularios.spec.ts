import { signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Subject } from 'rxjs';
import { vi } from 'vitest';
import { PlataformaSesionService, PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { PlataformaGestionMvpService } from '../../core/auth/plataforma-gestion-mvp.service';
import { PlataformaGestionMvpComponent } from './plataforma-gestion-mvp.component';
import { GobiernoCambiosV5Component } from './gobierno-cambios-v5.component';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { routes } from '../../app.routes';

const ADMIN: PlataformaSesion = {
  nombreUsuario: 'platform-test', rolNombre: 'Super Admin',
  ambito: 'Plataforma', esAdministrador: true
};
describe('C00-04.M04.04 - formularios navegacion mensajes y doble accion', () => {
  it('conserva ruta protegida y enlace de retorno, sin formularios de escritura', async () => {
    const ruta = routes.find(r => r.path === 'plataforma/gobierno-cambios-v5');
    expect(ruta?.canActivate).toEqual([plataformaGuard]);
    await TestBed.configureTestingModule({
      imports: [GobiernoCambiosV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: {
        sesion: signal(ADMIN), obtener: vi.fn()
      } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(GobiernoCambiosV5Component);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelectorAll('form, input')).toHaveLength(0);
    expect(root.querySelector('a[aria-label="Volver a administración de plataforma"]')?.getAttribute('href')).toBe('/plataforma');
  });
  it('limita a una sola comprobacion mientras el servidor no responde', async () => {
    TestBed.resetTestingModule();
    const pending = new Subject<PlataformaSesion>();
    const obtener = vi.fn(() => pending.asObservable());
    await TestBed.configureTestingModule({
      imports: [GobiernoCambiosV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: {
        sesion: signal(ADMIN), obtener
      } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(GobiernoCambiosV5Component);
    fixture.detectChanges();
    fixture.componentInstance.revalidar();
    fixture.componentInstance.revalidar();
    fixture.detectChanges();
    expect(obtener).toHaveBeenCalledOnce();
    expect(fixture.componentInstance.estado()).toBe('cargando');
    expect((fixture.nativeElement as HTMLElement).querySelector('button[aria-label="Revalidar sesión de plataforma"]')?.hasAttribute('disabled')).toBe(true);
    pending.next(ADMIN); pending.complete(); fixture.detectChanges();
    expect(fixture.componentInstance.estado()).toBe('exito');
    expect(fixture.nativeElement.textContent).toContain('revalidada correctamente');
  });
  it('denegacion HTTP 403 oculta contenido protegido y muestra error seguro', async () => {
    TestBed.resetTestingModule();
    const pending = new Subject<PlataformaSesion>();
    await TestBed.configureTestingModule({
      imports: [GobiernoCambiosV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: {
        sesion: signal(ADMIN), obtener: vi.fn(() => pending.asObservable())
      } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(GobiernoCambiosV5Component);
    fixture.componentInstance.revalidar();
    pending.error(new HttpErrorResponse({status:403}));
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(fixture.componentInstance.estado()).toBe('sin_permiso');
    expect(root.querySelector('section.notice')).toBeNull();
    expect(root.querySelector('[role="alert"]')?.textContent).toContain('Sin permiso');
  });
  it('formulario empresarial no duplica alta y no adelanta el mensaje de exito', async () => {
    TestBed.resetTestingModule();
    const pending = new Subject<{ empresaId:number; administradorUsuarioId:number }>();
    const altaControlada = vi.fn(() => pending.asObservable());
    await TestBed.configureTestingModule({
      imports: [PlataformaGestionMvpComponent, NoopAnimationsModule],
      providers: [
        { provide: PlataformaGestionMvpService, useValue: { altaControlada } },
        { provide: PlataformaSesionService, useValue: { limpiar: vi.fn() } },
        { provide: Router, useValue: { navigate: vi.fn() } },
        { provide: ActivatedRoute, useValue: { snapshot: {} } }
      ]
    }).compileComponents();
    const fixture = TestBed.createComponent(PlataformaGestionMvpComponent);
    fixture.componentRef.setInput('directorio', {items:[],puedeCrear:true});
    fixture.detectChanges();
    const ui = fixture.componentInstance;
    ui.empresaNueva.set('Empresa M04 Unica'); ui.avanzarAlta();
    ui.buscado.set({ id:7, nombreUsuario:'admin', nombreCompleto:'Admin',
      activo:true, bloqueado:false, pendienteAsignacion:false });
    ui.avanzarAlta();
    ui.crearEmpresa(); ui.crearEmpresa();
    fixture.detectChanges();
    expect(altaControlada).toHaveBeenCalledOnce();
    expect(ui.ocupado()).toBe(true);
    expect(ui.exito()).toBeNull();
    pending.next({empresaId:104,administradorUsuarioId:7});
    pending.complete(); fixture.detectChanges();
    expect(ui.ocupado()).toBe(false);
    expect(ui.exito()).toContain('Empresa creada con administrador existente');
  });
});
