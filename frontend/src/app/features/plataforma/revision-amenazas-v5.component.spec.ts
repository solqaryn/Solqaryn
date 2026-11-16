import { signal } from '@angular/core';
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
    await TestBed.configureTestingModule({
      imports: [RevisionAmenazasV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(RevisionAmenazasV5Component);
    fixture.detectChanges();
    return { fixture, sesion };
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
    expect(fixture.nativeElement.querySelectorAll('button, input, form, textarea')).toHaveLength(0);
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
});
