import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { C0601PublicStatusComponent } from './c0601-public-status.component';

/**
 * C06-01.M04.01. Public, read-only DEV SaaS landing entry.
 * Deliberately no tenant selector, pricing, checkout, private API or JWT state.
 */
@Component({
  selector: 'app-c0601-public-saas-landing',
  standalone: true,
  imports: [RouterLink, C0601PublicStatusComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a class="skip-link" href="#c0601-content" (click)="enfocarContenido($event)">Ir al contenido</a>
    <div class="landing-shell" [attr.data-brand-theme]="altoContraste() ? 'contraste' : 'institucional'" data-testid="c0601-public-saas-landing">
      <header class="site-header">
        <a class="wordmark" href="/conoce-solqaryn" aria-label="SOLQARYN, inicio de presentación">SOLQARYN<span class="wordmark-mark" aria-hidden="true">.</span></a>
        <nav aria-label="Navegación de presentación">
          <a href="#beneficios" (click)="enfocarSeccion($event, 'beneficios')">Qué resuelve</a>
          <a href="#alcance" (click)="enfocarSeccion($event, 'alcance')">Alcance</a>
          <a routerLink="/login" class="header-action">Acceder</a>
          <button type="button" class="theme-toggle" data-testid="c0601-brand-toggle" [attr.aria-pressed]="altoContraste()" (click)="alternarContraste()">Contraste alto</button>
        </nav>
      </header>
      <main id="c0601-content" tabindex="-1">
        <section class="hero" aria-labelledby="c0601-heading">
          <div class="hero-copy">
            <p class="eyebrow">Plataforma de gestión empresarial</p>
            <h1 id="c0601-heading">Tu operación merece <em>un lugar claro.</em></h1>
            <p class="lede">SOLQARYN reúne herramientas para organizar procesos de negocio. El acceso a cada empresa y a sus datos exige una sesión y permisos verificados por el servidor.</p>
            <div class="hero-actions">
              <a routerLink="/login" class="button-primary">Ir a mi cuenta <span aria-hidden="true">→</span></a>
              <a href="#beneficios" class="button-secondary" (click)="enfocarSeccion($event, 'beneficios')">Conocer la propuesta</a>
            </div>
          </div>
          <aside class="hero-aside" aria-label="Principios del producto">
            <span class="aside-index">01 / 03</span>
            <p class="aside-lead">Una misma vista de negocio. Cada empresa, su propio ámbito.</p>
            <div class="aside-divider"></div>
            <p class="aside-note">Los permisos y el acceso a los datos no dependen de esta página pública.</p>
          </aside>
        </section>
        <section id="beneficios" class="section" tabindex="-1" aria-labelledby="benefits-heading">
          <div class="section-caption">UNA PLATAFORMA, DIFERENTES OPERACIONES</div>
          <div class="section-content">
            <h2 id="benefits-heading">Más contexto, menos saltos.</h2>
            <div class="benefit-grid">
              <article><span class="item-number">01</span><h3>Operación ordenada</h3><p>Un punto de partida para consultar y organizar los procesos que correspondan a tu empresa.</p></article>
              <article><span class="item-number">02</span><h3>Acceso responsable</h3><p>La identidad, los roles y los permisos deben confirmarse antes de mostrar información privada.</p></article>
              <article><span class="item-number">03</span><h3>Trabajo con trazabilidad</h3><p>Las operaciones empresariales requieren controles y registros que faciliten su seguimiento.</p></article>
            </div>
          </div>
        </section>
        <app-c0601-public-status />
        <section id="alcance" class="closing" tabindex="-1" aria-labelledby="scope-heading">
          <p class="eyebrow">ACCESO SEGURO</p>
          <h2 id="scope-heading">La presentación es pública. Tus datos, no.</h2>
          <p>Esta página no consulta datos de clientes, empresas, inventario ni cuentas; tampoco publica precios, planes aprobados o enlaces de pago.</p>
          <a routerLink="/login" class="closing-link">Abrir inicio de sesión <span aria-hidden="true">↗</span></a>
        </section>
      </main>
      <footer><strong>SOLQARYN</strong><span>Vista informativa de desarrollo · Sin contratación desde esta pantalla.</span></footer>
    </div>
  `,
  styles: [`
    :host { display: block; color: #172d34; background: #f4f2ec; font-family: Arial, Helvetica, sans-serif; }
    * { box-sizing: border-box; }
    .skip-link { position: absolute; top: -60px; left: 1rem; z-index: 10; padding: .8rem; color: #11292f; background: white; }
    .skip-link:focus { top: .5rem; }
    .landing-shell {
      --sq-bg: #f4f2ec; --sq-ink: #172d34; --sq-nav: #2e4a4b;
      --sq-muted: #3e5558; --sq-accent: #bd7849; --sq-emphasis: #35665e;
      --sq-panel: #e5e8db; --sq-section: #e9ede7; --sq-border: #a9bab2;
      --sq-action: #1a5048; --sq-action-text: #ffffff; --sq-focus: #bb6835;
      min-height: 100vh; background: var(--sq-bg); color: var(--sq-ink);
    }
    .landing-shell[data-brand-theme='contraste'] {
      --sq-bg: #092028; --sq-ink: #fffdf5; --sq-nav: #fffdf5;
      --sq-muted: #f0f8f5; --sq-accent: #fddcaa; --sq-emphasis: #bdf2da;
      --sq-panel: #15343d; --sq-section: #12303a; --sq-border: #9dd2c5;
      --sq-action: #bdf2da; --sq-action-text: #092028; --sq-focus: #ffe49e;
    }
    .site-header { max-width: 1260px; margin: auto; padding: 1.35rem clamp(1.1rem, 5vw, 4rem); display: flex; align-items: center; justify-content: space-between; gap: 1.3rem; border-bottom: 1px solid var(--sq-border); }
    .wordmark { color: var(--sq-nav); font-weight: 900; font-size: 1.12rem; text-decoration: none; letter-spacing: .09em; }
    .wordmark-mark { color: var(--sq-accent); }
    .theme-toggle {
      border: 1px solid var(--sq-border); color: var(--sq-nav); background: transparent;
      min-height: 44px; padding: .65rem .9rem; border-radius: .2rem;
      cursor: pointer; font: inherit; font-size: .86rem; font-weight: 700;
    }
    .theme-toggle[aria-pressed='true'] { background: var(--sq-action); color: var(--sq-action-text); }
    .theme-toggle:hover { text-decoration: underline; text-underline-offset: .15em; }
    nav { display: flex; flex-wrap: wrap; align-items: center; justify-content: flex-end; gap: clamp(.8rem, 3vw, 2.4rem); }
    nav a { color: var(--sq-nav); text-decoration: none; font-size: .88rem; font-weight: 600; }
    nav .header-action { border: 1px solid var(--sq-border); padding: .7rem 1rem; border-radius: .2rem; }
    .hero { max-width: 1260px; margin: auto; min-height: 34rem; padding: clamp(3.3rem, 7vw, 7rem) clamp(1.1rem, 5vw, 4rem); display: grid; grid-template-columns: minmax(0, 1.7fr) minmax(250px, .7fr); gap: clamp(2rem, 8vw, 7rem); align-items: center; }
    .eyebrow, .section-caption { color: var(--sq-emphasis); letter-spacing: .15em; font-size: .75rem; font-weight: 800; }
    h1, h2, h3, p { overflow-wrap: anywhere; }
    /* LANDING_CONTRAST_HEADINGS_V23: global h1/h2/h3 color tokens must not override local brand palette. */
    .landing-shell h1, .landing-shell h2, .landing-shell h3 { color: var(--sq-ink); }
    h1, h2 { font-family: Georgia, 'Times New Roman', serif; font-weight: 400; }
    h1 { max-width: 820px; font-size: clamp(2.8rem, 6.2vw, 5.7rem); letter-spacing: -.055em; line-height: 1.05; margin: 1.25rem 0; }
    h1 em { color: var(--sq-emphasis); font-weight: 400; font-style: normal; }
    .lede { max-width: 41rem; font-size: clamp(1rem, 1.6vw, 1.14rem); line-height: 1.7; color: var(--sq-muted); }
    .hero-actions { display: flex; flex-wrap: wrap; gap: 1rem; margin-top: 2.1rem; }
    .button-primary, .button-secondary { min-height: 44px; display: inline-flex; align-items: center; justify-content: center; gap: 1.1rem; text-decoration: none; padding: .85rem 1.3rem; font-weight: 700; border-radius: .2rem; }
    .button-primary { background: var(--sq-action); color: var(--sq-action-text); }
    .button-secondary { color: var(--sq-nav); border: 1px solid var(--sq-border); }
    .hero-aside { background: var(--sq-panel); padding: clamp(1.7rem, 3vw, 2.8rem); border-top: 4px solid var(--sq-accent); align-self: stretch; display: flex; flex-direction: column; justify-content: center; }
    .aside-index, .item-number { font-size: .78rem; font-weight: 800; letter-spacing: .12em; color: var(--sq-emphasis); }
    .aside-lead { font-family: Georgia, 'Times New Roman', serif; font-size: clamp(1.5rem, 2vw, 2.25rem); line-height: 1.28; }
    .aside-divider { border-top: 1px solid var(--sq-border); margin: .8rem 0; }
    .aside-note { line-height: 1.6; color: var(--sq-muted); }
    .section { background: var(--sq-section); padding: clamp(3rem, 7vw, 6rem) clamp(1.1rem, 5vw, 4rem); display: grid; grid-template-columns: minmax(150px, .45fr) minmax(0, 2fr); gap: 2rem; }
    .section > * { width: 100%; max-width: 1060px; }
    .section h2 { font-size: clamp(2rem, 4vw, 3.4rem); line-height: 1.1; margin: .3rem 0 2rem; }
    .benefit-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 1.3rem; }
    .benefit-grid article { padding-top: 1.2rem; border-top: 1px solid var(--sq-border); }
    h3 { font-size: 1.06rem; margin: 1rem 0 .6rem; }
    .benefit-grid p, .closing p { line-height: 1.65; color: var(--sq-muted); }
    .closing { max-width: 1260px; margin: auto; padding: clamp(3.5rem, 7vw, 6.5rem) clamp(1.1rem, 5vw, 4rem); }
    .closing h2 { max-width: 750px; font-size: clamp(2rem, 4.2vw, 3.6rem); line-height: 1.18; margin: .8rem 0; }
    .closing p { max-width: 700px; }
    .closing-link { display: inline-block; padding: .85rem 0; font-weight: 700; color: var(--sq-nav); }
    footer { border-top: 1px solid var(--sq-border); padding: 1.6rem clamp(1.1rem, 5vw, 4rem); display: flex; flex-wrap: wrap; justify-content: space-between; gap: 1rem; color: var(--sq-muted); font-size: .82rem; }
    a:focus-visible, button:focus-visible { outline: 3px solid var(--sq-focus); outline-offset: 4px; }
    @media (max-width: 760px) {
      .site-header { align-items: flex-start; flex-direction: column; }
      .hero { grid-template-columns: minmax(0, 1fr); min-height: 0; gap: 2rem; }
      .hero-aside { min-height: 13rem; }
      .section { grid-template-columns: 1fr; }
      .benefit-grid { grid-template-columns: 1fr; }
    }
    @media (prefers-reduced-motion: reduce) { * { scroll-behavior: auto !important; } }
  `]
})
export class C0601PublicSaasLandingComponent {
  private readonly document = inject(DOCUMENT);
  // Only trusted institutional tokens; never derive brand or tenant from a URL or CSS input.
  readonly altoContraste = signal(false);

  alternarContraste(): void {
    this.altoContraste.update(current => !current);
  }

  enfocarContenido(event: Event): void {
    event.preventDefault();
    this.document.getElementById('c0601-content')?.focus({ preventScroll: false });
  }

  /** Preserve the Angular public route when navigating within its sections. */
  enfocarSeccion(event: Event, id: 'beneficios' | 'alcance'): void {
    event.preventDefault();
    this.document.getElementById(id)?.focus({ preventScroll: false });
  }
}
