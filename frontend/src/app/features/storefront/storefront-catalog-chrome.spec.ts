import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { StorefrontCatalogFooterComponent } from './storefront-catalog-footer.component';
import { StorefrontPreviewComponent } from './storefront-preview.component';
import { StorefrontFeedbackComponent } from './storefront-feedback.component';
import { StorefrontIdentidadService } from './storefront-identidad.service';

describe('Componentes compartidos de catálogo y detalle', () => {
  it('conserva identidad, enlaces y visibilidad del copyright y WhatsApp', () => {
    const config = signal({ nombreComercial: 'Empresa QA', piePaginaTexto: 'Identidad de la empresa', eslogan: 'Compras seguras' });
    const mostrarCopyright = signal(true);
    TestBed.configureTestingModule({
      imports: [StorefrontCatalogFooterComponent],
      providers: [{ provide: StorefrontIdentidadService, useValue: { config, mostrarCopyright, copyright: signal('2026 Empresa QA') } }]
    });
    const fixture = TestBed.createComponent(StorefrontCatalogFooterComponent);
    fixture.componentRef.setInput('variante', 'detalle');
    fixture.componentRef.setInput('permiteWhatsapp', true);
    fixture.detectChanges();
    const host: HTMLElement = fixture.nativeElement;
    expect(host.querySelector('.footer-grid strong')?.textContent).toContain('Empresa QA');
    expect(host.querySelector('.footer-grid p')?.textContent).toContain('Identidad de la empresa');
    expect([...host.querySelectorAll('a')].map(a => a.getAttribute('href'))).toEqual([
      '/tienda', '/tienda/productos', '/tienda/ofertas', '/tienda/categorias', '/tienda#contacto'
    ]);
    expect(host.textContent).toContain('Compra alternativa por WhatsApp');
    expect(host.querySelector('.footer-bottom small')?.textContent).toContain('2026 Empresa QA');

    fixture.componentRef.setInput('variante', 'catalogo');
    fixture.detectChanges();
    expect(host.classList.contains('catalogo')).toBe(true);
    expect(host.textContent).toContain('Pedidos por WhatsApp');
    fixture.componentRef.setInput('permiteWhatsapp', false);
    mostrarCopyright.set(false);
    config.set({ nombreComercial: 'Otra empresa QA', piePaginaTexto: '', eslogan: 'Compras seguras' });
    fixture.detectChanges();
    expect(host.textContent).toContain('Otra empresa QA');
    expect(host.textContent).toContain('Compras seguras');
    expect(host.textContent).not.toContain('Pedidos por WhatsApp');
    expect(host.querySelector('.footer-bottom small')).toBeNull();
  });

  it('conserva el estado accesible y las dos acciones de selección de fuente', () => {
    TestBed.configureTestingModule({ imports: [StorefrontPreviewComponent] });
    const fixture = TestBed.createComponent(StorefrontPreviewComponent);
    fixture.componentRef.setInput('variante', 'detalle');
    fixture.componentRef.setInput('usaDatosReales', false);
    const evento = vi.fn();
    fixture.componentInstance.fuenteCambiada.subscribe(evento);
    fixture.detectChanges();
    const host: HTMLElement = fixture.nativeElement;
    const botones = host.querySelectorAll<HTMLButtonElement>('button');
    expect(botones).toHaveLength(2);
    expect(botones[0].getAttribute('aria-pressed')).toBe('true');
    expect(botones[1].getAttribute('aria-pressed')).toBe('false');
    expect(host.textContent).toContain('Vista previa de detalle');
    botones[1].click();
    expect(evento).toHaveBeenLastCalledWith(true);
    fixture.componentRef.setInput('usaDatosReales', true);
    fixture.componentRef.setInput('variante', 'catalogo');
    fixture.detectChanges();
    expect(botones[1].getAttribute('aria-pressed')).toBe('true');
    expect(botones[0].getAttribute('aria-pressed')).toBe('false');
    expect(host.textContent).toContain('Vista previa de catálogo');
    botones[0].click();
    expect(evento).toHaveBeenLastCalledWith(false);
  });
});

describe('Avisos accesibles de Storefront', () => {
  it('mantiene la región viva al cambiar el mensaje y conserva los modos visual y sólo lector', async () => {
    TestBed.configureTestingModule({ imports: [StorefrontFeedbackComponent] });
    const fixture = TestBed.createComponent(StorefrontFeedbackComponent);
    fixture.componentRef.setInput('mensaje', '');
    fixture.componentRef.setInput('mostrarAviso', true);
    fixture.detectChanges();
    await fixture.whenStable();
    const host: HTMLElement = fixture.nativeElement;
    expect(host.getAttribute('role')).toBe('status');
    expect(host.getAttribute('aria-live')).toBe('polite');
    expect(host.getAttribute('aria-atomic')).toBe('true');
    expect(host.classList.contains('sr-only')).toBe(true);
    expect(host.classList.contains('feedback-toast')).toBe(false);
    fixture.componentRef.setInput('mensaje', '1 unidad agregada al carrito.');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.nativeElement).toBe(host);
    expect(host.textContent).toContain('1 unidad agregada al carrito.');
    expect(host.classList.contains('feedback-toast')).toBe(true);
    expect(host.classList.contains('sr-only')).toBe(false);
    fixture.componentRef.setInput('mostrarAviso', false);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(host.textContent).toContain('1 unidad agregada al carrito.');
    expect(host.classList.contains('sr-only')).toBe(true);
    expect(host.classList.contains('feedback-toast')).toBe(false);
    fixture.componentRef.setInput('mensaje', '');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(host.textContent?.trim()).toBe('');
  });
});
