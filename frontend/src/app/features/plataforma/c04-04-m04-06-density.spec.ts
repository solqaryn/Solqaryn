import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { C0404PaymentLinkPreviewComponent } from './c04-04-m04-01-payment-link-preview.component';

describe('C04-04.M04.06 - carga progresiva y densidad segura UI', () => {
  const admin: PlataformaSesion = { nombreUsuario: 'QA', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true };
  async function montar() {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(admin);
    await TestBed.configureTestingModule({
      imports: [C0404PaymentLinkPreviewComponent],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(C0404PaymentLinkPreviewComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    return { fixture, root: fixture.nativeElement as HTMLElement, sesion };
  }

  it('cargando -> vacio libera aria-busy sin mutaciones ni cobros', async () => {
    const { fixture, root } = await montar();
    fixture.componentRef.setInput('estadoVista', 'cargando');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(root.querySelector('[data-testid="c0404-payment-private"]')?.getAttribute('aria-busy')).toBe('true');
    expect(root.querySelector('[data-testid="c0404-state-loading"]')?.getAttribute('aria-live')).toBe('polite');
    expect(root.textContent).toContain('No se consulta ningún proveedor');
    fixture.componentRef.setInput('estadoVista', 'vacio');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(root.querySelector('[data-testid="c0404-payment-private"]')?.getAttribute('aria-busy')).toBe('false');
    expect(root.querySelector('[data-testid="c0404-state-empty"]')).not.toBeNull();
    expect(root.querySelector('[data-testid="c0404-state-loading"]')).toBeNull();
    expect(root.querySelectorAll('[data-testid^="c0404-step-"]')).toHaveLength(3);
    expect(root.querySelectorAll('form,button,input,iframe')).toHaveLength(0);
  });

  it('error -> listo no convierte vista informativa en pago aprobado', async () => {
    const { fixture, root } = await montar();
    fixture.componentRef.setInput('estadoVista', 'error');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(root.querySelector('[data-testid="c0404-state-error"]')?.getAttribute('role')).toBe('alert');
    expect(root.textContent).toContain('Ningún pago se ha procesado');
    fixture.componentRef.setInput('estadoVista', 'listo');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(root.querySelector('[data-testid="c0404-state-error"]')).toBeNull();
    expect(root.querySelector('[data-testid="c0404-state-ready"]')?.getAttribute('role')).toBe('status');
    expect(root.textContent).toContain('no confirma pagos ni cobros');
    expect(root.querySelectorAll('form,button,input,iframe')).toHaveLength(0);
  });

  it('revocacion mientras cargando oculta la vista y nunca expone datos', async () => {
    const { fixture, root, sesion } = await montar();
    fixture.componentRef.setInput('estadoVista', 'cargando');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(root.querySelector('[data-testid="c0404-payment-private"]')).not.toBeNull();
    sesion.set(null);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(root.querySelector('[data-testid="c0404-payment-private"]')).toBeNull();
    expect(root.querySelector('[data-testid="c0404-payment-denied"]')?.getAttribute('role')).toBe('alert');
    expect(root.textContent).not.toContain('PRIVATE_PSP_SECRET');
    expect(root.querySelectorAll('iframe,form,input,button')).toHaveLength(0);
  });
});
