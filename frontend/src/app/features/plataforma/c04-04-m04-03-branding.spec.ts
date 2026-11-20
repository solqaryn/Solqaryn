import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { C0404PaymentLinkPreviewComponent } from './c04-04-m04-01-payment-link-preview.component';

describe('C04-04.M04.03 - branding local permitido sin proveedor de pagos', () => {
  const admin: PlataformaSesion = { nombreUsuario: 'QA', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true };

  async function montar(initial: PlataformaSesion | null = admin) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(initial);
    await TestBed.configureTestingModule({
      imports: [C0404PaymentLinkPreviewComponent],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(C0404PaymentLinkPreviewComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    return { fixture, sesion, root: fixture.nativeElement as HTMLElement };
  }

  it('usa preset base seguro sin logotipos ni acciones financieras', async () => {
    const { root } = await montar();
    const main = root.querySelector<HTMLElement>('[data-testid="c0404-payment-preview"]');
    expect(main?.getAttribute('data-tema')).toBe('base');
    expect(root.querySelector('[data-testid="c0404-branding-preset"]')?.textContent).toContain('SOLQARYN');
    expect(root.querySelectorAll('[data-testid^="c0404-step-"]')).toHaveLength(3);
    expect(root.querySelectorAll('form,input,button,iframe,script')).toHaveLength(0);
  });

  it('permite el unico preset alternativo de contraste por Signal Input', async () => {
    const { fixture, root } = await montar();
    fixture.componentRef.setInput('temaSolicitado', 'contraste');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(root.querySelector('main')?.getAttribute('data-tema')).toBe('contraste');
    expect(root.querySelector('[data-testid="c0404-branding-preset"]')?.textContent).toContain('alto contraste');
    expect(root.querySelectorAll('[data-testid^="c0404-step-"]')).toHaveLength(3);
  });

  it('rechaza opciones desconocidas y vuelve al preset base de forma determinista', async () => {
    const { fixture, root } = await montar();
    fixture.componentRef.setInput('temaSolicitado', 'contraste');
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.componentRef.setInput('temaSolicitado', 'empresa-administrador');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(root.querySelector('main')?.getAttribute('data-tema')).toBe('base');
    expect(root.textContent).not.toContain('empresa-administrador');
  });

  it('no refleja HTML hostil ni permite atributos visuales arbitrarios', async () => {
    const { fixture, root } = await montar();
    const payload = '<img src=x onerror=alert(1)>';
    fixture.componentRef.setInput('temaSolicitado', payload);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(root.querySelector('main')?.getAttribute('data-tema')).toBe('base');
    expect(root.querySelector('img')).toBeNull();
    expect(root.innerHTML).not.toContain('onerror');
    expect(root.textContent).not.toContain(payload);
  });

  it('no muestra branding privado cuando se revoca la sesion', async () => {
    const { fixture, sesion, root } = await montar();
    fixture.componentRef.setInput('temaSolicitado', 'contraste');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(root.querySelector('[data-testid="c0404-branding-preset"]')).not.toBeNull();
    sesion.set(null);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(root.querySelector('[data-testid="c0404-branding-preset"]')).toBeNull();
    expect(root.querySelector('[data-testid="c0404-payment-private"]')).toBeNull();
    expect(root.querySelector('[data-testid="c0404-payment-denied"]')).not.toBeNull();
  });

  it('limita entradas largas y conserva el estado de error sin datos del proveedor', async () => {
    const { fixture, root } = await montar();
    const oversized = 'x'.repeat(4096);
    fixture.componentRef.setInput('temaSolicitado', oversized);
    fixture.componentRef.setInput('estadoVista', 'error');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(root.querySelector('main')?.getAttribute('data-tema')).toBe('base');
    expect(root.textContent).not.toContain(oversized);
    expect(root.querySelector('[data-testid="c0404-state-error"]')?.textContent).toContain('Ningún pago');
    expect(root.querySelectorAll('form,input,button,iframe')).toHaveLength(0);
  });
});
