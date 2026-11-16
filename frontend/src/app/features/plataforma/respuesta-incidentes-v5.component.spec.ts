import { signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Subject } from 'rxjs';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { routes } from '../../app.routes';
import { RespuestaIncidentesV5Component, normalizarPresentacionIncidentesV5 } from './respuesta-incidentes-v5.component';

describe('C03-04.M04.01 - respuesta a incidentes privada', () => {
  const autorizado: PlataformaSesion = {
    nombreUsuario: 'Plataforma', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true
  };
  async function montar(valor: PlataformaSesion | null) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(valor);
    const solicitudes: Subject<PlataformaSesion>[] = [];
    const servicio = {
      sesion,
      obtener: () => {
        sesion.set(null);
        const pendiente = new Subject<PlataformaSesion>();
        solicitudes.push(pendiente);
        return pendiente.asObservable();
      }
    };
    await TestBed.configureTestingModule({
      imports: [RespuestaIncidentesV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: servicio }]
    }).compileComponents();
    const fixture = TestBed.createComponent(RespuestaIncidentesV5Component);
    fixture.detectChanges();
    return { fixture, sesion, solicitudes };
  }
  it('existe solo una ruta lazy protegida por plataformaGuard', async () => {
    const coincidencias = routes.filter(r => r.path === 'plataforma/respuesta-incidentes-v5');
    expect(coincidencias).toHaveLength(1);
    expect(coincidencias[0].canActivate).toEqual([plataformaGuard]);
    expect(await coincidencias[0].loadComponent?.()).toBe(RespuestaIncidentesV5Component);
  });
  it('anónimo nunca muestra controles protegidos ni operaciones', async () => {
    const { fixture } = await montar(null);
    expect(fixture.componentInstance.autorizado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.incident-review li')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).toContain('Guía protegida oculta');
  });
  it('sesión administradora permite únicamente cuatro decisiones estáticas', async () => {
    const { fixture } = await montar(autorizado);
    expect(fixture.componentInstance.autorizado()).toBe(true);
    expect(fixture.nativeElement.querySelectorAll('.incident-review li')).toHaveLength(4);
    expect(fixture.nativeElement.querySelectorAll('form, input, textarea')).toHaveLength(0);
    expect(fixture.nativeElement.querySelectorAll('button')).toHaveLength(2);
    expect(fixture.nativeElement.textContent).toContain('NO_MEDIDO');
  });
  it('sesión de empresa no puede elevarse a plataforma', async () => {
    const falsa = { ...autorizado, ambito: 'Empresa' } as unknown as PlataformaSesion;
    const { fixture } = await montar(falsa);
    expect(fixture.componentInstance.autorizado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.incident-review li')).toHaveLength(0);
  });
  it('revocación reactiva oculta la guía inmediatamente', async () => {
    const { fixture, sesion } = await montar(autorizado);
    expect(fixture.nativeElement.querySelectorAll('.incident-review li')).toHaveLength(4);
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.componentInstance.autorizado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.incident-review li')).toHaveLength(0);
  });
  it('C03-04.M04.02 vacío de fuente sin inventar incidentes', async () => {
    const { fixture } = await montar(autorizado);
    expect(fixture.componentInstance.estadoVista()).toBe('sin_datos');
    expect(fixture.nativeElement.textContent).toContain('Sin incidentes conectados');
  });
  it('C03-04.M04.02 carga oculta datos y bloquea consulta duplicada; luego confirma sesión', async () => {
    const { fixture, sesion, solicitudes } = await montar(autorizado);
    fixture.componentInstance.revalidarSesion();
    fixture.componentInstance.revalidarSesion();
    fixture.detectChanges();
    expect(solicitudes).toHaveLength(1);
    expect(fixture.componentInstance.estadoVista()).toBe('cargando');
    expect(fixture.nativeElement.querySelectorAll('.incident-review li')).toHaveLength(0);
    sesion.set(autorizado);
    solicitudes[0].next(autorizado);
    solicitudes[0].complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVista()).toBe('exito');
    expect(fixture.nativeElement.querySelectorAll('.incident-review li')).toHaveLength(4);
  });
  it('C03-04.M04.02 error 503 falla cerrado y oculta diagnóstico', async () => {
    const { fixture, solicitudes } = await montar(autorizado);
    fixture.componentInstance.revalidarSesion();
    solicitudes[0].error(new HttpErrorResponse({ status: 503, statusText: 'UPSTREAM_SECRET_DIAGNOSTIC' }));
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVista()).toBe('error');
    expect(fixture.nativeElement.querySelectorAll('.incident-review li')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).toContain('No se pudo comprobar la sesión');
    expect(fixture.nativeElement.textContent).not.toContain('UPSTREAM_SECRET_DIAGNOSTIC');
  });
  it('C03-04.M04.02 403 y revocación niegan la guía', async () => {
    const { fixture, sesion, solicitudes } = await montar(autorizado);
    fixture.componentInstance.revalidarSesion();
    solicitudes[0].error(new HttpErrorResponse({ status: 403 }));
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVista()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.incident-review li')).toHaveLength(0);
    sesion.set(autorizado);
    fixture.componentInstance.verificacion.set('inicial');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.incident-review li')).toHaveLength(4);
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVista()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.incident-review li')).toHaveLength(0);
  });


  it('C03-04.M04.03 acepta solo marca densidad contraste conocidos', () => {
    expect(normalizarPresentacionIncidentesV5(null)).toEqual({ marca: 'SOLQARYN', densidad: 'comoda', contraste: 'normal' });
    expect(normalizarPresentacionIncidentesV5({ marca: 'SOLQARYN Plataforma', densidad: 'compacta', contraste: 'alto' }))
      .toEqual({ marca: 'SOLQARYN Plataforma', densidad: 'compacta', contraste: 'alto' });
    expect(normalizarPresentacionIncidentesV5({ marca: '<img onerror=alert(1)>', densidad: 'color:red', contraste: 'url(javascript:evil)' }))
      .toEqual({ marca: 'SOLQARYN', densidad: 'comoda', contraste: 'normal' });
    expect(normalizarPresentacionIncidentesV5([])).toEqual({ marca: 'SOLQARYN', densidad: 'comoda', contraste: 'normal' });
  });
  it('C03-04.M04.03 actualiza densidad y contraste sin HTML arbitrario', async () => {
    const { fixture } = await montar(autorizado);
    const root = fixture.nativeElement.querySelector('.incident-guide') as HTMLElement;
    expect(root.getAttribute('data-densidad')).toBe('comoda');
    fixture.componentInstance.alternarDensidad();
    fixture.detectChanges();
    expect(root.getAttribute('data-densidad')).toBe('compacta');
    fixture.componentInstance.aplicarPresentacion({ marca: '<img src=x>', densidad: 'evil', contraste: 'red' });
    fixture.detectChanges();
    expect(root.getAttribute('data-densidad')).toBe('comoda');
    expect(root.getAttribute('data-contraste')).toBe('normal');
    expect(fixture.nativeElement.querySelectorAll('.incident-guide img')).toHaveLength(0);
    fixture.componentInstance.aplicarPresentacion({ marca: 'SOLQARYN Plataforma', densidad: 'compacta', contraste: 'alto' });
    fixture.detectChanges();
    expect(root.getAttribute('data-contraste')).toBe('alto');
    expect(fixture.nativeElement.textContent).toContain('SOLQARYN Plataforma');
  });
  it('C03-04.M04.03 anónimo no cambia configuración ni abre la guía', async () => {
    const { fixture, sesion } = await montar(null);
    fixture.componentInstance.aplicarPresentacion({ densidad: 'compacta' });
    expect(fixture.componentInstance.presentacion().densidad).toBe('comoda');
    sesion.set(autorizado);
    fixture.detectChanges();
    fixture.componentInstance.aplicarPresentacion({ densidad: 'compacta' });
    sesion.set(null);
    fixture.detectChanges();
    fixture.componentInstance.alternarDensidad();
    expect(fixture.componentInstance.presentacion().densidad).toBe('compacta');
    expect(fixture.nativeElement.querySelectorAll('.incident-review li')).toHaveLength(0);
  });
});
