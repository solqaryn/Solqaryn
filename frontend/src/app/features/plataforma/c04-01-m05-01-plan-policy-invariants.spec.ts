import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { routes } from '../../app.routes';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { C0401PlanesReadonlyComponent } from './c04-01-m04-01-planes-readonly.component';

// C04-01.M05.01 is a UNIT boundary around an existing unapproved plans reference.
// It is NOT a pricing engine, signed JWT test, tenant SQL test, upgrade or payment test.
describe('C04-01.M05.01 | reglas inmutables de referencia de planes', () => {
  const global: PlataformaSesion = {
    nombreUsuario: 'QA', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true
  };
  async function montar(inicial: PlataformaSesion | null = global) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(inicial);
    const obtener = vi.fn();
    const limpiar = vi.fn(() => sesion.set(null));
    await TestBed.configureTestingModule({
      imports: [C0401PlanesReadonlyComponent],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion, obtener, limpiar } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(C0401PlanesReadonlyComponent);
    fixture.detectChanges();
    return { fixture, sesion, obtener, limpiar, root: fixture.nativeElement as HTMLElement };
  }

  it('congela lista y entradas: la UI no puede reescribir la política documental como aprobación', async () => {
    const { fixture, root } = await montar();
    const sections = fixture.componentInstance.secciones;
    expect(Object.isFrozen(sections)).toBe(true);
    expect(sections).toHaveLength(3);
    expect(sections.map(section => section.clave)).toEqual(['versiones', 'capacidades', 'transiciones']);
    expect(new Set(sections.map(section => section.clave)).size).toBe(3);
    for (const section of sections) {
      expect(Object.isFrozen(section)).toBe(true);
      expect(Reflect.set(section, 'detalle', 'PRECIO_APROBADO_PAGO_AUTORIZADO')).toBe(false);
    }
    expect(Reflect.set(sections, 3, { clave: 'cobros', titulo: 'Cobrar', detalle: 'APROBADO' })).toBe(false);
    fixture.detectChanges();
    expect(root.textContent).not.toContain('PRECIO_APROBADO_PAGO_AUTORIZADO');
    expect(root.querySelectorAll('[data-testid^="c0401-section-"]')).toHaveLength(3);
  });

  it('autoriza solo la referencia descriptiva, sin controles de comercialización ni validación de precio', async () => {
    const { root, obtener, limpiar } = await montar();
    expect(root.querySelector('[data-testid="c0401-unapproved"]')?.textContent).toContain('NO_CERTIFICADO');
    expect(root.querySelectorAll('[data-testid^="c0401-section-"]')).toHaveLength(3);
    expect(root.querySelectorAll('form,button,input,textarea,select,iframe,[data-precio],[data-cobro],[data-checkout]')).toHaveLength(0);
    expect(root.querySelectorAll('a')).toHaveLength(1);
    expect(root.querySelector('a')?.getAttribute('href')).toBe('/plataforma');
    expect(obtener).not.toHaveBeenCalled();
    expect(limpiar).not.toHaveBeenCalled();
  });

  it('rechaza anónimo, empresa y falso administrador aun con permisos comerciales inyectados', async () => {
    for (const value of [
      null,
      { ...global, ambito: 'Empresa', precioAprobado: true, permisos: ['Plan:Upgrade'] },
      { ...global, esAdministrador: false, precioAprobado: true, tenantId: 71 }
    ]) {
      const { root } = await montar(value as PlataformaSesion | null);
      expect(root.querySelector('[data-testid="c0401-private"]')).toBeNull();
      expect(root.querySelector('[data-testid="c0401-denied"]')?.textContent).toContain('no confirmada');
      expect(root.querySelectorAll('[data-testid^="c0401-section-"],button,form,input')).toHaveLength(0);
    }
  });

  it('revocación después de render impide usar el panel aun si antes fue visible', async () => {
    const { root, sesion, fixture } = await montar();
    expect(root.querySelectorAll('[data-testid^="c0401-section-"]')).toHaveLength(3);
    sesion.set({ ...global, ambito: 'Empresa' } as unknown as PlataformaSesion);
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="c0401-private"]')).toBeNull();
    sesion.set({ ...global, esAdministrador: false } as unknown as PlataformaSesion);
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="c0401-private"]')).toBeNull();
    sesion.set(null);
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="c0401-private"]')).toBeNull();
  });

  it('rechaza sesión hostil sin crear tarifas, HTML ejecutable o acciones upgrade', async () => {
    const hostile = {
      ...global, nombreUsuario: '<img src=x onerror=alert(1)>',
      precio: '0', moneda: 'HNL', pagoAutorizado: true, plan: 'SIN_LIMITE',
      metadata: { upgradeUrl: 'javascript:alert(1)' }
    } as unknown as PlataformaSesion;
    const { root, fixture } = await montar(hostile);
    expect(fixture.componentInstance.secciones.map(section => section.clave)).toEqual([
      'versiones', 'capacidades', 'transiciones'
    ]);
    expect(root.querySelector('img,script,iframe,button,form,[data-precio]')).toBeNull();
    expect(root.innerHTML).not.toContain('onerror');
    expect(root.textContent).not.toContain('SIN_LIMITE');
    expect(root.textContent).not.toContain('javascript:alert(1)');
    expect(root.querySelector('[data-testid="c0401-unapproved"]')?.textContent).toContain('NO_CERTIFICADO');
  });

  it('enruta únicamente bajo plataformaGuard lazy: el test unitario nunca convierte UI en permiso de backend', async () => {
    const matches = routes.filter(route => route.path === 'plataforma/catalogo-planes-v22');
    expect(matches).toHaveLength(1);
    expect(matches[0].canActivate).toEqual([plataformaGuard]);
    expect(matches[0].data).toBeUndefined();
    expect(await matches[0].loadComponent?.()).toBe(C0401PlanesReadonlyComponent);
  });
});
