import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { C0404PaymentLinkPreviewComponent } from './c04-04-m04-01-payment-link-preview.component';

describe('C04-04.M04.02 - estados accesibles de vista desconectada', () => {
  const admin: PlataformaSesion = { nombreUsuario: 'QA', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true };
  async function montar(inicial: PlataformaSesion | null = admin) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(inicial);
    await TestBed.configureTestingModule({
      imports: [C0404PaymentLinkPreviewComponent],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(C0404PaymentLinkPreviewComponent);
    fixture.detectChanges();
    return { fixture, sesion, root: fixture.nativeElement as HTMLElement };
  }

  it('parte vacío sin proveedores ni identificadores de cobro', async () => {
    const { root } = await montar();
    expect(root.querySelector('[data-testid="c0404-state-empty"]')?.getAttribute('role')).toBe('status');
    expect(root.textContent).toContain('SIN_CONEXION');
    expect(root.querySelectorAll('[data-testid^="c0404-step-"]')).toHaveLength(3);
    expect(root.querySelectorAll('button,form,input,iframe')).toHaveLength(0);
  });

  it('cargando expone estado accesible, aria-busy y no inventa pago', async () => {
    const { fixture, root } = await montar();
    fixture.componentRef.setInput('estadoVista', 'cargando'); fixture.detectChanges();
    expect(root.querySelector('[data-testid="c0404-payment-private"]')?.getAttribute('aria-busy')).toBe('true');
    expect(root.querySelector('[data-testid="c0404-state-loading"]')?.getAttribute('aria-live')).toBe('polite');
    expect(root.textContent).toContain('No se consulta ningún proveedor');
    expect(root.querySelector('[data-testid="c0404-state-ready"]')).toBeNull();
  });

  it('error presenta mensaje seguro sin interpolar detalles de backend', async () => {
    const { fixture, root } = await montar();
    fixture.componentRef.setInput('estadoVista', 'error'); fixture.detectChanges();
    expect(root.querySelector('[data-testid="c0404-state-error"]')?.getAttribute('role')).toBe('alert');
    expect(root.textContent).toContain('Ningún pago se ha procesado');
    expect(root.querySelector('script,img,iframe')).toBeNull();
  });

  it('listo solo significa renderizado informativo, nunca cobro aceptado', async () => {
    const { fixture, root } = await montar();
    fixture.componentRef.setInput('estadoVista', 'listo'); fixture.detectChanges();
    expect(root.querySelector('[data-testid="c0404-state-ready"]')?.getAttribute('role')).toBe('status');
    expect(root.textContent).toContain('no confirma pagos');
    expect(root.querySelector('[data-testid="c0404-payment-private"]')?.getAttribute('aria-busy')).toBe('false');
    expect(root.querySelectorAll('button,form,input,iframe')).toHaveLength(0);
  });

  it('deniega cualquier estado privado con sesión nula o rol no administrador', async () => {
    const { fixture, root, sesion } = await montar(null);
    fixture.componentRef.setInput('estadoVista', 'listo'); fixture.detectChanges();
    expect(root.querySelector('[data-testid="c0404-payment-private"]')).toBeNull();
    expect(root.querySelector('[data-testid="c0404-payment-denied"]')?.getAttribute('role')).toBe('alert');
    sesion.set({ ...admin, esAdministrador: false } as unknown as PlataformaSesion); fixture.detectChanges();
    expect(root.querySelector('[data-testid="c0404-state-ready"]')).toBeNull();
    sesion.set(admin); fixture.detectChanges();
    expect(root.querySelector('[data-testid="c0404-state-ready"]')).not.toBeNull();
    sesion.set(null); fixture.detectChanges();
    expect(root.querySelector('[data-testid="c0404-state-ready"]')).toBeNull();
  });

  it('estado desconocido falla cerrado con texto fijo', async () => {
    const { fixture, root } = await montar();
    fixture.componentRef.setInput('estadoVista', '<img src=x onerror=alert(1)>'); fixture.detectChanges();
    expect(root.querySelector('[data-testid="c0404-state-invalid"]')?.getAttribute('role')).toBe('alert');
    expect(root.querySelector('img')).toBeNull();
    expect(root.innerHTML).not.toContain('onerror');
  });
});
