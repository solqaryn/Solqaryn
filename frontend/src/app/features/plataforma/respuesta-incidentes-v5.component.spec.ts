import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { routes } from '../../app.routes';
import { RespuestaIncidentesV5Component } from './respuesta-incidentes-v5.component';

describe('C03-04.M04.01 - respuesta a incidentes privada', () => {
  const autorizado: PlataformaSesion = {
    nombreUsuario: 'Plataforma', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true
  };
  async function montar(valor: PlataformaSesion | null) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(valor);
    await TestBed.configureTestingModule({
      imports: [RespuestaIncidentesV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(RespuestaIncidentesV5Component);
    fixture.detectChanges();
    return { fixture, sesion };
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
    expect(fixture.nativeElement.querySelectorAll('button, form, input, textarea')).toHaveLength(0);
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
});
