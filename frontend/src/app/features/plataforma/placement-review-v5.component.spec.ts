import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { routes } from '../../app.routes';
import { PlacementReviewV5Component } from './placement-review-v5.component';

describe('C02-02.M04.01 - placement privado no operativo', () => {
  const verified: PlataformaSesion = {
    nombreUsuario: 'Plataforma', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true
  };
  async function render(value: PlataformaSesion | null) {
    TestBed.resetTestingModule();
    const sesion = signal(value);
    await TestBed.configureTestingModule({
      imports: [PlacementReviewV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(PlacementReviewV5Component);
    fixture.detectChanges();
    return { fixture, sesion };
  }
  it('registra una sola ruta lazy protegida exclusivamente por plataformaGuard', async () => {
    const match = routes.filter(r => r.path === 'plataforma/placement-v5');
    expect(match).toHaveLength(1);
    expect(match[0].canActivate).toEqual([plataformaGuard]);
    expect(await match[0].loadComponent?.()).toBe(PlacementReviewV5Component);
  });
  it('sin sesión no muestra modalidades ni operación', async () => {
    const { fixture } = await render(null);
    expect(fixture.componentInstance.accesoVerificado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.modalidades article')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).toContain('Placement privado oculto');
  });
  it('acceso plataforma muestra Shared/Dedicated solo lectura y NO_EJECUTADO', async () => {
    const { fixture } = await render(verified);
    expect(fixture.componentInstance.accesoVerificado()).toBe(true);
    expect(fixture.nativeElement.querySelectorAll('.modalidades article')).toHaveLength(2);
    expect(fixture.nativeElement.textContent).toContain('NO_EJECUTADO');
    expect(fixture.nativeElement.querySelectorAll('button, form, input, textarea')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).not.toContain('mysql://');
  });
  it('rol empresarial simulado no concede privilegios de plataforma', async () => {
    const fake = { ...verified, ambito: 'Empresa' } as unknown as PlataformaSesion;
    const { fixture } = await render(fake);
    expect(fixture.componentInstance.accesoVerificado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.modalidades article')).toHaveLength(0);
  });
  it('revocación de sesión reactiva es fail-closed', async () => {
    const { fixture, sesion } = await render(verified);
    expect(fixture.nativeElement.querySelectorAll('.modalidades article')).toHaveLength(2);
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.modalidades article')).toHaveLength(0);
  });
});
