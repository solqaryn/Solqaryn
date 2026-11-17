import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { routes } from '../../app.routes';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { C1504AuditoriaUxV19Component } from './c15-04-auditoria-ux-v19.component';

// C15-04.M05.01: prueba real del contrato Angular de la superficie de auditoría.
// No equivale a auditoría de las cinco aplicaciones, WCAG ni Web Vitals reales.
describe('C15-04.M05.01 — contrato unitario de auditoría UX privada', () => {
  async function montar() {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [C1504AuditoriaUxV19Component],
      providers: [provideRouter([])]
    }).compileComponents();
    const fixture = TestBed.createComponent(C1504AuditoriaUxV19Component);
    fixture.detectChanges();
    return { fixture, root: fixture.nativeElement as HTMLElement };
  }

  it('sólo expone la ruta lazy protegida por plataformaGuard', async () => {
    const matching = routes.filter(r => r.path === 'plataforma/auditoria-ux-v5');
    expect(matching).toHaveLength(1);
    expect(matching[0].canActivate).toEqual([plataformaGuard]);
    expect(await matching[0].loadComponent?.()).toBe(C1504AuditoriaUxV19Component);
  });

  it('mantiene cinco superficies únicas con encabezados legibles y alcance explícito', async () => {
    const { fixture, root } = await montar();
    const surfaces = fixture.componentInstance.superficies;
    expect(surfaces.map(s => s.id)).toEqual(['landing', 'portal', 'erp', 'builder', 'global']);
    expect(new Set(surfaces.map(s => s.id)).size).toBe(5);
    const cards = [...root.querySelectorAll<HTMLElement>('[data-testid="ux-surface"]')];
    expect(cards).toHaveLength(5);
    for (const [i, card] of cards.entries()) {
      expect(card.querySelector('h3')?.textContent?.trim()).toBe(surfaces[i].titulo);
      expect(card.textContent).toContain(surfaces[i].objetivo);
    }
  });

  it('no presenta objetivos como cobertura WCAG, rendimiento ni defectos certificados', async () => {
    const { root } = await montar();
    const cards = root.querySelectorAll('[data-testid="ux-surface"]');
    expect(cards).toHaveLength(5);
    for (const card of cards) {
      expect(card.getAttribute('data-state')).toBe('NO_MEDIDO');
      expect(card.textContent).toContain('NO_MEDIDO');
    }
    expect(root.textContent).toContain('WCAG 2.2 AA');
    expect(root.textContent).toContain('SIN_VERIFICAR');
    expect(root.textContent).toContain('Web Vitals');
    expect(root.textContent).toContain('NO_MEDIDO');
    expect(root.textContent).not.toContain('100% aprobado');
  });

  it('es semántica, de lectura únicamente y regresa por enlace, sin mutaciones', async () => {
    const { root } = await montar();
    const main = root.querySelector<HTMLElement>('main[data-testid="ux-audit-v19"]');
    expect(main).not.toBeNull();
    const titleId = main?.getAttribute('aria-labelledby');
    expect(titleId).toBe('ux-audit-title');
    expect(root.querySelector('#ux-audit-title')?.textContent).toContain('Auditoría UX');
    expect(root.querySelectorAll('form, input, select, textarea, button')).toHaveLength(0);
    const links = root.querySelectorAll<HTMLAnchorElement>('a[href]');
    expect(links).toHaveLength(1);
    expect(links[0].getAttribute('href')).toBe('/plataforma');
    expect(links[0].getAttribute('aria-label')).toContain('Volver');
  });

  it('declara los cuatro límites móviles/escritorio sin inventar mediciones', async () => {
    const { fixture, root } = await montar();
    const widths = [...fixture.componentInstance.anchosObjetivo];
    expect(widths).toEqual([320, 375, 768, 1280]);
    expect(new Set(widths).size).toBe(4);
    expect(Math.min(...widths)).toBe(320);
    expect(Math.max(...widths)).toBe(1280);
    const labels = [...root.querySelectorAll<HTMLElement>('.ux-width')].map(el => el.textContent?.trim());
    expect(labels).toEqual(['320 px', '375 px', '768 px', '1280 px']);
    expect(root.textContent).toContain('Objetivos, no resultados');
  });
});
