import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { routes } from '../../app.routes';
import { CoberturaTrazabilidadV5Component } from './cobertura-trazabilidad-v5.component';

describe('C15-01.M04.01 cobertura privada de solo lectura', () => {
  const auth: PlataformaSesion = { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true };
  async function montar(value: PlataformaSesion | null) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(value);
    await TestBed.configureTestingModule({
      imports: [CoberturaTrazabilidadV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(CoberturaTrazabilidadV5Component);
    fixture.detectChanges();
    return { fixture, sesion };
  }

  it('ruta privada lazy requiere exactamente plataformaGuard', async () => {
    const matches = routes.filter(route => route.path === 'plataforma/trazabilidad-cobertura-v5');
    expect(matches).toHaveLength(1);
    expect(matches[0].canActivate).toEqual([plataformaGuard]);
    expect(await matches[0].loadComponent?.()).toBe(CoberturaTrazabilidadV5Component);
  });
  it('autorizado ve cuatro dimensiones sin inventar resultados ni acciones', async () => {
    const { fixture } = await montar(auth);
    expect(fixture.nativeElement.querySelectorAll('.coverage-list li')).toHaveLength(4);
    expect(fixture.nativeElement.textContent).toContain('NO_MEDIDO');
    expect(fixture.nativeElement.querySelectorAll('.unverified')).toHaveLength(4);
    expect(fixture.nativeElement.querySelectorAll('button,form,input,textarea')).toHaveLength(0);
  });
  it('anónimo y ambito Empresa no reciben datos internos', async () => {
    for (const value of [null, { ...auth, ambito: 'Empresa' } as unknown as PlataformaSesion]) {
      const { fixture } = await montar(value);
      expect(fixture.nativeElement.querySelectorAll('.coverage-list li')).toHaveLength(0);
      expect(fixture.nativeElement.querySelector('[role="alert"]')?.textContent).toContain('ocultos');
    }
  });
  it('revocación elimina inmediatamente tabla de dimensiones', async () => {
    const { fixture, sesion } = await montar(auth);
    expect(fixture.nativeElement.querySelectorAll('.coverage-list li')).toHaveLength(4);
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.coverage-list li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });
});
