import { signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Subject } from 'rxjs';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { routes } from '../../app.routes';
import { RevisionAmenazasV5Component } from './revision-amenazas-v5.component';

describe('C14-02.M04.01 - pantalla privada de amenazas', () => {
  const autorizada: PlataformaSesion = {
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
        const result = new Subject<PlataformaSesion>();
        solicitudes.push(result);
        return result.asObservable();
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
  it('ruta única lazy con plataformaGuard', async () => {
    const matches = routes.filter(r => r.path === 'plataforma/revision-amenazas-v5');
    expect(matches).toHaveLength(1);
    expect(matches[0].canActivate).toEqual([plataformaGuard]);
    expect(await matches[0].loadComponent?.()).toBe(RevisionAmenazasV5Component);
  });
  it('anónimo no conoce detalles de amenazas', async () => {
    const { fixture } = await montar(null);
    expect(fixture.componentInstance.accesoVerificado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.threat-list li')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).toContain('Guía privada oculta');
  });
  it('administrador sólo ve cuatro comprobaciones no operativas', async () => {
    const { fixture } = await montar(autorizada);
    expect(fixture.componentInstance.accesoVerificado()).toBe(true);
    expect(fixture.nativeElement.querySelectorAll('.threat-list li')).toHaveLength(4);
    expect(fixture.nativeElement.querySelectorAll('input, form, textarea')).toHaveLength(0);
    expect(fixture.nativeElement.querySelectorAll('button')).toHaveLength(3);
    expect(fixture.nativeElement.textContent).toContain('NO_MEDIDO');
  });
  it('rol empresarial falso se rechaza', async () => {
    const { fixture } = await montar({ ...autorizada, ambito: 'Empresa' } as unknown as PlataformaSesion);
    expect(fixture.componentInstance.accesoVerificado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.threat-list li')).toHaveLength(0);
  });
  it('revocación de la señal elimina inmediatamente la guía', async () => {
    const { fixture, sesion } = await montar(autorizada);
    expect(fixture.nativeElement.querySelectorAll('.threat-list li')).toHaveLength(4);
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.componentInstance.accesoVerificado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.threat-list li')).toHaveLength(0);
  });
  it('C14-02.M04.02 vacío no inventa hallazgos de seguridad', async () => {
    const { fixture } = await montar(autorizada);
    expect(fixture.componentInstance.estadoVista()).toBe('sin_datos');
    expect(fixture.nativeElement.textContent).toContain('Sin hallazgos conectados');
  });
  it('C14-02.M04.02 single-flight bloquea contenido durante carga y confirma sesión', async () => {
    const { fixture, sesion, solicitudes } = await montar(autorizada);
    fixture.componentInstance.revalidarSesion();
    fixture.componentInstance.revalidarSesion();
    fixture.detectChanges();
    expect(solicitudes).toHaveLength(1);
    expect(fixture.componentInstance.estadoVista()).toBe('cargando');
    expect(fixture.nativeElement.querySelectorAll('.threat-list li')).toHaveLength(0);
    sesion.set(autorizada);
    solicitudes[0].next(autorizada);
    solicitudes[0].complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVista()).toBe('exito');
    expect(fixture.nativeElement.querySelectorAll('.threat-list li')).toHaveLength(4);
  });
  it('C14-02.M04.02 error 503 oculta detalles y contenido', async () => {
    const { fixture, solicitudes } = await montar(autorizada);
    fixture.componentInstance.revalidarSesion();
    solicitudes[0].error(new HttpErrorResponse({ status: 503, statusText: 'INTERNAL_SECRET_DIAGNOSTIC' }));
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVista()).toBe('error');
    expect(fixture.nativeElement.querySelectorAll('.threat-list li')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).not.toContain('INTERNAL_SECRET_DIAGNOSTIC');
  });
  it('C14-02.M04.02 403 y revocación mantienen denegación reactiva', async () => {
    const { fixture, sesion, solicitudes } = await montar(autorizada);
    fixture.componentInstance.revalidarSesion();
    solicitudes[0].error(new HttpErrorResponse({ status: 403 }));
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVista()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.threat-list li')).toHaveLength(0);
    sesion.set(autorizada);
    fixture.componentInstance.verificacion.set('inicial');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.threat-list li')).toHaveLength(4);
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVista()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.threat-list li')).toHaveLength(0);
  });

  it('C14-02.M04.03 cambia entre dos temas tipados sin consultas ni permisos nuevos', async () => {
    const { fixture, solicitudes } = await montar(autorizada);
    expect(fixture.componentInstance.temaPresentacion()).toBe('institucional');
    fixture.componentInstance.cambiarTemaPresentacion('neutro');
    fixture.detectChanges();
    expect(fixture.componentInstance.temaPresentacion()).toBe('neutro');
    expect(fixture.nativeElement.querySelector('.threat-guide.visual-neutral')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[aria-label="Presentación visual local"] button[aria-pressed="true"]')?.textContent).toContain('neutra');
    expect(solicitudes).toHaveLength(0);
  });
  it('C14-02.M04.03 valor arbitrario restaura presentación segura sin CSS inyectado', async () => {
    const { fixture } = await montar(autorizada);
    fixture.componentInstance.cambiarTemaPresentacion('neutro');
    fixture.componentInstance.cambiarTemaPresentacion('url(javascript:alert(1))');
    fixture.detectChanges();
    expect(fixture.componentInstance.temaPresentacion()).toBe('institucional');
    expect(fixture.nativeElement.querySelector('.threat-guide.visual-neutral')).toBeNull();
    expect(fixture.nativeElement.outerHTML).not.toContain('javascript:alert');
  });
  it('C14-02.M04.03 revocación y ámbito empresa deniegan cambio y controles visuales', async () => {
    const { fixture, sesion } = await montar(autorizada);
    sesion.set(null);
    fixture.componentInstance.cambiarTemaPresentacion('neutro');
    fixture.detectChanges();
    expect(fixture.componentInstance.temaPresentacion()).toBe('institucional');
    expect(fixture.nativeElement.querySelectorAll('[aria-label="Presentación visual local"] button')).toHaveLength(0);
    sesion.set({ ...autorizada, ambito: 'Empresa' } as unknown as PlataformaSesion);
    fixture.componentInstance.cambiarTemaPresentacion('neutro');
    fixture.detectChanges();
    expect(fixture.componentInstance.temaPresentacion()).toBe('institucional');
    expect(fixture.nativeElement.querySelectorAll('.threat-list li')).toHaveLength(0);
  });

});
