import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';
import { ContinuidadOperativaV5Component } from './continuidad-operativa-v5.component';
import { CONTINUIDAD_PRESENTACION_V5, normalizarPresentacionV5 } from './continuidad-presentacion-v5';

describe('C10-04.M04.03 - presentacion de continuidad permitida', () => {
  const sesionValida = { nombreUsuario: 'Plataforma', rolNombre: 'Admin', ambito: 'Plataforma' as const, esAdministrador: true as const };
  async function montar(valor: unknown, autorizada = true) {
    TestBed.resetTestingModule();
    const config = signal<unknown>(valor);
    const sesion = signal(autorizada ? sesionValida : null);
    await TestBed.configureTestingModule({
      imports: [ContinuidadOperativaV5Component],
      providers: [
        provideRouter([]),
        { provide: PlataformaSesionService, useValue: { sesion, obtener: () => of(sesionValida) } },
        { provide: CONTINUIDAD_PRESENTACION_V5, useValue: config }
      ]
    }).compileComponents();
    const fixture = TestBed.createComponent(ContinuidadOperativaV5Component);
    fixture.detectChanges();
    return { fixture, config, sesion };
  }
  it('acepta las dos variantes tipadas y actualiza branding/densidad al cambiar el Signal', async () => {
    const { fixture, config } = await montar({ marca: 'institucional', densidad: 'compacta' });
    const surface = fixture.nativeElement.querySelector('section.continuidad');
    expect(surface).not.toBeNull();
    expect(fixture.nativeElement.querySelectorAll('main')).toHaveLength(0);
    expect(surface.getAttribute('data-densidad')).toBe('compacta');
    expect(surface.querySelector('.eyebrow').textContent).toContain('Plataforma SOLQARYN');
    config.set({ marca: 'solqaryn', densidad: 'normal' });
    fixture.detectChanges();
    expect(surface.getAttribute('data-densidad')).toBe('normal');
    expect(surface.querySelector('.eyebrow').textContent).toContain('SOLQARYN · Continuidad');
  });
  it('niega inyeccion de HTML/CSS desde configuracion externa y revierte a defaults', async () => {
    const payload = '<img src=x onerror=alert(1)>';
    const { fixture } = await montar({ marca: payload, densidad: 'compacta' });
    const surface = fixture.nativeElement.querySelector('section.continuidad');
    expect(surface).not.toBeNull();
    expect(fixture.nativeElement.querySelectorAll('main')).toHaveLength(0);
    expect(surface.getAttribute('data-densidad')).toBe('normal');
    expect(surface.querySelector('.eyebrow').textContent).toContain('SOLQARYN');
    expect(fixture.nativeElement.innerHTML).not.toContain(payload);
  });
  it('configuraciones extra, nulas, arrays y tipos desconocidos son fail-closed', () => {
    for (const valor of [null, [], 'institucional', { marca: 'institucional', densidad: 'compacta', habilitarRestauracion: true }, { marca: 'institucional', densidad: 'gigante' }]) {
      expect(normalizarPresentacionV5(valor)).toEqual({ marca: 'solqaryn', densidad: 'normal' });
    }
  });
  it('el branding permitido nunca concede acceso si se pierde la sesion', async () => {
    const { fixture, sesion } = await montar({ marca: 'institucional', densidad: 'compacta' }, false);
    expect(fixture.nativeElement.querySelectorAll('.controles li')).toHaveLength(0);
    sesion.set(sesionValida);
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.controles li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelectorAll('form, input')).toHaveLength(0);
  });
});
