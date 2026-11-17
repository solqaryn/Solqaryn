import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { GobiernoCambiosV5Component } from './gobierno-cambios-v5.component';
import { COBERTURA_PRESENTACION_V5, CoberturaTrazabilidadV5Component,
  normalizarCoberturaPresentacionV5 } from './cobertura-trazabilidad-v5.component';

// C15-04.M04.03: configuración visual local, no contrato de JWT HTTP ni tenant live.
const admin: PlataformaSesion = {
  nombreUsuario: 'Synthetic', rolNombre: 'Global', ambito: 'Plataforma', esAdministrador: true
};

describe('C15-04.M04.03 identidad visual y componentes allowlisted', () => {
  async function governance(value: PlataformaSesion | null, marca: unknown) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(value);
    await TestBed.configureTestingModule({
      imports: [GobiernoCambiosV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(GobiernoCambiosV5Component);
    fixture.componentRef.setInput('modoMarca', marca);
    fixture.detectChanges();
    return fixture;
  }

  async function coverage(value: PlataformaSesion | null, apariencia: unknown) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(value);
    await TestBed.configureTestingModule({
      imports: [CoberturaTrazabilidadV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion } },
        { provide: COBERTURA_PRESENTACION_V5, useValue: apariencia }]
    }).compileComponents();
    const fixture = TestBed.createComponent(CoberturaTrazabilidadV5Component);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('marca global/neutra en gobierno solo cambia presentación; no crea operaciones', async () => {
    const normal = await governance(admin, 'plataforma');
    expect(normal.nativeElement.querySelector('.brand-context strong')?.textContent)
      .toContain('SOLQARYN');
    expect(normal.nativeElement.querySelector('.brand-emblem')?.textContent).toContain('S');
    expect(normal.nativeElement.querySelectorAll('.steps li')).toHaveLength(4);
    expect(normal.nativeElement.querySelectorAll('form,input,textarea,iframe')).toHaveLength(0);
    const neutral = await governance(admin, 'neutra');
    expect(neutral.nativeElement.querySelector('.brand-context--neutral')).not.toBeNull();
    expect(neutral.nativeElement.querySelector('.brand-context strong')?.textContent)
      .toContain('Interfaz neutra');
    expect(neutral.nativeElement.textContent).toContain('sin autoridad operativa');
    expect(neutral.nativeElement.querySelectorAll('form,input,textarea,iframe')).toHaveLength(0);
  });

  it('marca arbitraria inyectada no activa HTML, CSS ni texto de otra empresa', async () => {
    const payload = '<svg onload=alert("SECRET_TENANT_B")>';
    const fixture = await governance(admin, payload);
    expect(fixture.nativeElement.querySelector('.brand-context--neutral')).not.toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain(payload);
    expect(fixture.nativeElement.textContent).not.toContain('SECRET_TENANT_B');
    expect(fixture.nativeElement.querySelectorAll('svg,script,iframe,form')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('.brand-context')?.getAttribute('style')).toBeNull();
  });

  it('configuración dinámica de cobertura limita dos valores y congela resultado', async () => {
    const variant = normalizarCoberturaPresentacionV5({ marca: 'neutra', densidad: 'compacta' });
    expect(variant).toEqual({ marca: 'neutra', densidad: 'compacta' });
    expect(Object.isFrozen(variant)).toBe(true);
    const fixture = await coverage(admin, variant);
    const root = fixture.nativeElement.querySelector('.coverage-shell') as HTMLElement;
    expect(root.getAttribute('data-presentacion-marca')).toBe('neutra');
    expect(root.getAttribute('data-presentacion-densidad')).toBe('compacta');
    expect(root.classList.contains('coverage-neutral')).toBe(true);
    expect(root.classList.contains('coverage-compacta')).toBe(true);
    expect(root.textContent).toContain('NO_MEDIDO');
    expect(root.querySelectorAll('.unverified')).toHaveLength(4);
    expect(root.querySelectorAll('form,input,iframe')).toHaveLength(0);
  });

  it('getter malicioso y revocación no cambian marca en autoridad ni filtran datos', async () => {
    const malformed = {
      get marca(): string { throw new Error('SECRET_INTERNAL_PROVIDER'); },
      densidad: 'compacta'
    };
    const variant = normalizarCoberturaPresentacionV5(malformed);
    expect(variant).toEqual({ marca: 'neutra', densidad: 'regular' });
    const fixture = await coverage(null, malformed);
    const root = fixture.nativeElement.querySelector('.coverage-shell') as HTMLElement;
    expect(root.getAttribute('data-presentacion-marca')).toBe('neutra');
    expect(root.getAttribute('data-presentacion-densidad')).toBe('regular');
    expect(root.querySelectorAll('.coverage-list li')).toHaveLength(0);
    expect(root.querySelector('[role="alert"]')).not.toBeNull();
    expect(root.textContent).not.toContain('SECRET_INTERNAL_PROVIDER');
    expect(root.querySelectorAll('script,svg,iframe,form')).toHaveLength(0);
  });
});
