import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';
import { PlacementReviewV5Component } from './placement-review-v5.component';
import { PLACEMENT_PRESENTACION_V5, normalizarPresentacionPlacementV5 } from './placement-presentacion-v5';

describe('C02-02.M04.03 - branding tipado y reactivo sin autoridad de placement', () => {
  const admin = {
    nombreUsuario: 'Plataforma', rolNombre: 'Admin',
    ambito: 'Plataforma' as const, esAdministrador: true as const
  };
  async function montar(value: unknown, autorizada = true) {
    TestBed.resetTestingModule();
    const config = signal<unknown>(value);
    const sesion = signal(autorizada ? admin : null);
    await TestBed.configureTestingModule({
      imports: [PlacementReviewV5Component],
      providers: [
        provideRouter([]),
        { provide: PlataformaSesionService, useValue: { sesion, obtener: () => of(admin) } },
        { provide: PLACEMENT_PRESENTACION_V5, useValue: config }
      ]
    }).compileComponents();
    const fixture = TestBed.createComponent(PlacementReviewV5Component);
    fixture.detectChanges();
    return { fixture, config, sesion };
  }

  it('presenta variantes institucional y SOLQARYN, densidad reactiva sin recargar', async () => {
    const { fixture, config } = await montar({ marca: 'institucional', densidad: 'compacta' });
    const main = fixture.nativeElement.querySelector('main');
    expect(main.getAttribute('data-densidad')).toBe('compacta');
    expect(main.querySelector('.eyebrow').textContent).toContain('Plataforma SOLQARYN');
    config.set({ marca: 'solqaryn', densidad: 'normal' });
    fixture.detectChanges();
    expect(main.getAttribute('data-densidad')).toBe('normal');
    expect(main.querySelector('.eyebrow').textContent).toContain('SOLQARYN · Almacenamiento');
    expect(main.querySelectorAll('.modalidades article')).toHaveLength(2);
  });

  it('rechaza HTML/CSS no confiables sin reflejar payload en el DOM', async () => {
    const malicious = '<img src=x onerror=alert(1)>';
    const { fixture } = await montar({ marca: malicious, densidad: 'compacta' });
    const main = fixture.nativeElement.querySelector('main');
    expect(main.getAttribute('data-densidad')).toBe('normal');
    expect(main.querySelector('.eyebrow').textContent).toContain('SOLQARYN');
    expect(fixture.nativeElement.innerHTML).not.toContain(malicious);
  });

  it('niega objetos extra, arrays, null, strings, mezclas y densidad arbitraria', () => {
    for (const x of [
      null, [], 'dedicated', { marca: 'institucional' },
      { marca: 'institucional', densidad: 'compacta', tenantId: 'tenant-B' },
      { marca: 'institucional', densidad: 'gigante' },
      { marca: 'SOLQARYN', densidad: 'normal' },
      { marca: 'solqaryn', densidad: 'compacta', dsn: 'mysql://no' }
    ]) {
      expect(normalizarPresentacionPlacementV5(x)).toEqual({
        marca: 'solqaryn', densidad: 'normal'
      });
    }
  });

  it('branding no concede acceso a empresa ni usuario sin sesión y sigue sin formularios', async () => {
    const { fixture, sesion } = await montar({ marca: 'institucional', densidad: 'compacta' }, false);
    expect(fixture.nativeElement.querySelectorAll('.modalidades article')).toHaveLength(0);
    sesion.set(admin);
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.modalidades article')).toHaveLength(0);
    expect(fixture.nativeElement.querySelectorAll('form, input, select, textarea')).toHaveLength(0);
  });

  it('no hay DSN, migración, cutover ni acción sobre bases de datos por aplicar presentación', async () => {
    const { fixture } = await montar({ marca: 'institucional', densidad: 'normal' });
    const html = fixture.nativeElement.innerHTML as string;
    expect(fixture.nativeElement.querySelectorAll('button')).toHaveLength(1);
    expect(fixture.nativeElement.querySelectorAll('form, input, select, textarea')).toHaveLength(0);
    expect(html).not.toContain('mysql://');
    expect(html).not.toContain('tenant-B');
    expect(fixture.nativeElement.textContent).toContain('NO_EJECUTADO');
  });
});
