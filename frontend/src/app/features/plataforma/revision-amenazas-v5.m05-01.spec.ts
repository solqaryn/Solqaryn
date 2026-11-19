import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { RevisionAmenazasV5Component } from './revision-amenazas-v5.component';

/** C14-02.M05.01: contratos de regla reales del componente; servicio sintético, sin HTTP JWT. */
describe('C14-02.M05.01 - unidad fail-closed de guía privada de amenazas', () => {
  const admin: PlataformaSesion = {
    nombreUsuario: 'Plataforma', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true
  };
  async function montar(inicial: PlataformaSesion | null) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(inicial);
    const solicitudes: Subject<PlataformaSesion>[] = [];
    const servicio = {
      sesion,
      obtener: () => {
        // La solicitud nunca hereda autoridad del caché anterior.
        sesion.set(null);
        const siguiente = new Subject<PlataformaSesion>();
        solicitudes.push(siguiente);
        return siguiente.asObservable();
      }
    };
    await TestBed.configureTestingModule({
      imports: [RevisionAmenazasV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: servicio }]
    }).compileComponents();
    const fixture = TestBed.createComponent(RevisionAmenazasV5Component);
    fixture.detectChanges();
    return { fixture, sesion, solicitudes };
  }

  it('positivo: administrador de Plataforma recibe cuatro criterios estáticos, nunca acciones', async () => {
    const { fixture, solicitudes } = await montar(admin);
    expect(fixture.componentInstance.estadoVista()).toBe('sin_datos');
    expect(fixture.nativeElement.querySelectorAll('.threat-list li')).toHaveLength(4);
    expect(fixture.nativeElement.textContent).toContain('NO_MEDIDO');
    expect(fixture.nativeElement.querySelectorAll('form, input, textarea, iframe')).toHaveLength(0);
    expect(solicitudes).toHaveLength(0);
  });

  it('negativos de frontera: anónimo, empresa y rol no administrador quedan ocultos', async () => {
    for (const identidad of [
      null,
      { ...admin, ambito: 'Empresa' } as unknown as PlataformaSesion,
      { ...admin, esAdministrador: false } as unknown as PlataformaSesion
    ]) {
      const { fixture } = await montar(identidad);
      expect(fixture.componentInstance.estadoVista()).toBe('sin_permiso');
      expect(fixture.nativeElement.querySelectorAll('.threat-list li')).toHaveLength(0);
      expect(fixture.nativeElement.textContent).toContain('Guía privada oculta');
    }
  });

  it('adversarial: una respuesta positiva obsoleta sin sesión vigente no reabre la guía', async () => {
    const { fixture, sesion, solicitudes } = await montar(admin);
    fixture.componentInstance.revalidarSesion();
    fixture.detectChanges();
    expect(solicitudes).toHaveLength(1);
    expect(fixture.componentInstance.estadoVista()).toBe('cargando');
    expect(fixture.nativeElement.querySelectorAll('.threat-list li')).toHaveLength(0);
    // El backend simulado responde 200, pero la señal de sesión fue invalidada.
    solicitudes[0].next(admin);
    solicitudes[0].complete();
    fixture.detectChanges();
    expect(sesion()).toBeNull();
    expect(fixture.componentInstance.estadoVista()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.threat-list li')).toHaveLength(0);
    sesion.set(admin);
    fixture.detectChanges();
    // Un caché restablecido después del 200 obsoleto no limpia la denegación.
    expect(fixture.componentInstance.estadoVista()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.threat-list li')).toHaveLength(0);
  });

  it('single-flight: doble revalidación en vuelo produce una única solicitud y recuperación causal', async () => {
    const { fixture, sesion, solicitudes } = await montar(admin);
    fixture.componentInstance.revalidarSesion();
    fixture.componentInstance.revalidarSesion();
    fixture.detectChanges();
    expect(solicitudes).toHaveLength(1);
    expect(fixture.nativeElement.querySelectorAll('.threat-list li')).toHaveLength(0);
    sesion.set(admin);
    solicitudes[0].next(admin);
    solicitudes[0].complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVista()).toBe('exito');
    expect(fixture.nativeElement.querySelectorAll('.threat-list li')).toHaveLength(4);
  });

  it('error 401 y 503 se deniegan sin publicar diagnósticos privados', async () => {
    for (const [estadoHttp, esperado] of [[401, 'sin_permiso'], [503, 'error']] as const) {
      const { fixture, solicitudes } = await montar(admin);
      fixture.componentInstance.revalidarSesion();
      solicitudes[0].error(new HttpErrorResponse({
        status: estadoHttp, statusText: 'INTERNAL_SECRET_C14_02_NOT_FOR_UI'
      }));
      fixture.detectChanges();
      expect(fixture.componentInstance.estadoVista()).toBe(esperado);
      expect(fixture.nativeElement.querySelectorAll('.threat-list li')).toHaveLength(0);
      expect(fixture.nativeElement.textContent).not.toContain('INTERNAL_SECRET_C14_02_NOT_FOR_UI');
    }
  });
});
