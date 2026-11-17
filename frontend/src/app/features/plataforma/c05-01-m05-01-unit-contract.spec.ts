import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';
import { DesignSystemV5Component } from './design-system-v5.component';

// C05-01.M05.01: contrato de catálogo real; sesión inyectada de prueba, sin JWT/HTTP/MySQL reales.
const global = { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true };

describe('C05-01.M05.01 contrato unitario reproducible de catálogo privado', () => {
  async function montar(autorizacion: unknown) {
    TestBed.resetTestingModule();
    const sesion = signal<unknown>(autorizacion);
    await TestBed.configureTestingModule({
      imports: [DesignSystemV5Component],
      providers: [{ provide: PlataformaSesionService, useValue: { sesion } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(DesignSystemV5Component);
    fixture.detectChanges();
    return { fixture, sesion, component: fixture.componentInstance, root: fixture.nativeElement as HTMLElement };
  }

  it('deniega catálogo y presets sin sesión, por empresa y por falta de administrador global', async () => {
    for (const credencial of [null, { ...global, ambito: 'Empresa' }, { ...global, esAdministrador: false }]) {
      const { root, component } = await montar(credencial);
      expect(component.accesoVerificado()).toBe(false);
      expect(root.querySelectorAll('.token-item, .state-example, .brand-choice')).toHaveLength(0);
      expect(root.querySelector('[role="alert"]')?.textContent).toContain('permanece oculto');
      expect(component.seleccionarPreset('contraste')).toBe(false);
    }
  });

  it('con autoridad global expone exactamente cuatro tokens CSS de lista cerrada sin entradas operativas', async () => {
    const { root, component } = await montar(global);
    expect(component.tokens.map(token => token.css)).toEqual([
      '--color-primary', '--color-accent', '--color-success', '--color-danger'
    ]);
    expect(component.tokens.map(token => token.clase)).toEqual([
      'swatch-primary', 'swatch-accent', 'swatch-success', 'swatch-danger'
    ]);
    expect(root.querySelectorAll('.token-item')).toHaveLength(4);
    expect(root.querySelectorAll('form, input, textarea, select')).toHaveLength(0);
    expect(root.textContent).not.toContain('javascript:');
  });

  it('limita estados a cinco muestras descriptivas sin botones ni formularios de ejecución', async () => {
    const { root, component } = await montar(global);
    expect(component.estadosMuestra.map(estado => estado.id)).toEqual([
      'carga', 'vacio', 'error', 'exito', 'permiso'
    ]);
    const items = root.querySelectorAll('.state-example');
    expect(items).toHaveLength(5);
    for (const item of Array.from(items)) {
      expect(item.querySelector('h4')?.textContent?.trim().length).toBeGreaterThan(0);
      expect(item.querySelector('p')?.textContent?.trim().length).toBeGreaterThan(0);
      expect(item.querySelector('button, input, form, textarea')).toBeNull();
    }
    expect(root.querySelector('.state-catalog')?.getAttribute('aria-labelledby')).toBe('design-states-title');
  });

  it('rechaza IDs adulterados y repetición de preset sin alterar selección ni interpolar CSS arbitrario', async () => {
    const { root, component, fixture } = await montar(global);
    expect(component.presetSeleccionado()).toBe('institucional');
    for (const id of ['constructor', '__proto__', 'javascript:alert(1)', 'contraste ', 'CONTRASTE', '']) {
      expect(component.seleccionarPreset(id)).toBe(false);
      expect(component.presetSeleccionado()).toBe('institucional');
    }
    expect(component.seleccionarPreset('contraste')).toBe(true);
    expect(component.seleccionarPreset('contraste')).toBe(false);
    fixture.detectChanges();
    expect(root.querySelectorAll('.brand-choice[aria-pressed="true"]')).toHaveLength(1);
    expect(root.querySelector('.brand-preview-contrast')).not.toBeNull();
    expect(root.textContent).not.toContain('javascript:alert(1)');
  });

  it('revoca inmediatamente el catálogo y niega nuevas selecciones tras retirar sesión', async () => {
    const { root, component, sesion, fixture } = await montar(global);
    expect(root.querySelectorAll('.token-item')).toHaveLength(4);
    expect(component.seleccionarPreset('contraste')).toBe(true);
    sesion.set(null);
    fixture.detectChanges();
    expect(component.accesoVerificado()).toBe(false);
    expect(component.presetVista()).toBeNull();
    expect(component.seleccionarPreset('institucional')).toBe(false);
    expect(root.querySelectorAll('.token-item, .state-example, .brand-choice, .brand-preview')).toHaveLength(0);
    expect(root.querySelector('[role="alert"]')?.textContent).toContain('permanece oculto');
  });
});
