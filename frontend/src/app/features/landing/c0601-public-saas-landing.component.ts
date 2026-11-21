import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * C06-01.M04.01. Public, read-only DEV SaaS landing entry.
 * Deliberately no tenant selector, pricing, checkout, private API or JWT state.
 */
@Component({
  selector: 'app-c0601-public-saas-landing',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a class="skip-link" href="#c0601-content">Ir al contenido</a>
    <div class="landing-shell" data-testid="c0601-public-saas-landing">
      <header class="site-header">
        <a class="wordmark" href="/conoce-solqaryn" aria-label="SOLQARYN, inicio de presentación">SOLQARYN<span class="wordmark-mark" aria-hidden="true">.</span></a>
        <nav aria-label="Navegación de presentación">
          <a href="#beneficios">Qué resuelve</a>
          <a href="#alcance">Alcance</a>
          <a routerLink="/login" class="header-action">Acceder</a>
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
              <a href="#beneficios" class="button-secondary">Conocer la propuesta</a>
            </div>
          </div>
          <aside class="hero-aside" aria-label="Principios del producto">
            <span class="aside-index">01 / 03</span>
            <p class="aside-lead">Una misma vista de negocio. Cada empresa, su propio ámbito.</p>
            <div class="aside-divider"></div>
            <p class="aside-note">Los permisos y el acceso a los datos no dependen de esta página pública.</p>
          </aside>
        </section>
        <section id="beneficios" class="section" aria-labelledby="benefits-heading">
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
        <section id="alcance" class="closing" aria-labelledby="scope-heading">
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
    .landing-shell { min-height: 100vh; }
    .site-header { max-width: 1260px; margin: auto; padding: 1.35rem clamp(1.1rem, 5vw, 4rem); display: flex; align-items: center; justify-content: space-between; gap: 1.3rem; border-bottom: 1px solid #c8d3cb; }
    .wordmark { color: #163f40; font-weight: 900; font-size: 1.12rem; text-decoration: none; letter-spacing: .09em; }
    .wordmark-mark { color: #c2713d; }
    nav { display: flex; flex-wrap: wrap; align-items: center; justify-content: flex-end; gap: clamp(.8rem, 3vw, 2.4rem); }
    nav a { color: #2e4a4b; text-decoration: none; font-size: .88rem; font-weight: 600; }
    nav .header-action { border: 1px solid #284f48; padding: .7rem 1rem; border-radius: .2rem; }
    .hero { max-width: 1260px; margin: auto; min-height: 34rem; padding: clamp(3.3rem, 7vw, 7rem) clamp(1.1rem, 5vw, 4rem); display: grid; grid-template-columns: minmax(0, 1.7fr) minmax(250px, .7fr); gap: clamp(2rem, 8vw, 7rem); align-items: center; }
    .eyebrow, .section-caption { color: #35665e; letter-spacing: .15em; font-size: .75rem; font-weight: 800; }
    h1, h2, h3, p { overflow-wrap: anywhere; }
    h1, h2 { font-family: Georgia, 'Times New Roman', serif; font-weight: 400; }
    h1 { max-width: 820px; font-size: clamp(2.8rem, 6.2vw, 5.7rem); letter-spacing: -.055em; line-height: 1.05; margin: 1.25rem 0; }
    h1 em { color: #366f61; font-weight: 400; font-style: normal; }
    .lede { max-width: 41rem; font-size: clamp(1rem, 1.6vw, 1.14rem); line-height: 1.7; color: #3e5558; }
    .hero-actions { display: flex; flex-wrap: wrap; gap: 1rem; margin-top: 2.1rem; }
    .button-primary, .button-secondary { min-height: 44px; display: inline-flex; align-items: center; justify-content: center; gap: 1.1rem; text-decoration: none; padding: .85rem 1.3rem; font-weight: 700; border-radius: .2rem; }
    .button-primary { background: #1a5048; color: #fff; }
    .button-secondary { color: #1a5048; border: 1px solid #a9bab2; }
    .hero-aside { background: #e5e8db; padding: clamp(1.7rem, 3vw, 2.8rem); border-top: 4px solid #bd7849; align-self: stretch; display: flex; flex-direction: column; justify-content: center; }
    .aside-index, .item-number { font-size: .78rem; font-weight: 800; letter-spacing: .12em; color: #3a7164; }
    .aside-lead { font-family: Georgia, 'Times New Roman', serif; font-size: clamp(1.5rem, 2vw, 2.25rem); line-height: 1.28; }
    .aside-divider { border-top: 1px solid #a4b7aa; margin: .8rem 0; }
    .aside-note { line-height: 1.6; color: #42595a; }
    .section { background: #e9ede7; padding: clamp(3rem, 7vw, 6rem) clamp(1.1rem, 5vw, 4rem); display: grid; grid-template-columns: minmax(150px, .45fr) minmax(0, 2fr); gap: 2rem; }
    .section > * { width: 100%; max-width: 1060px; }
    .section h2 { font-size: clamp(2rem, 4vw, 3.4rem); line-height: 1.1; margin: .3rem 0 2rem; }
    .benefit-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 1.3rem; }
    .benefit-grid article { padding-top: 1.2rem; border-top: 1px solid #93aea2; }
    h3 { font-size: 1.06rem; margin: 1rem 0 .6rem; }
    .benefit-grid p, .closing p { line-height: 1.65; color: #435a58; }
    .closing { max-width: 1260px; margin: auto; padding: clamp(3.5rem, 7vw, 6.5rem) clamp(1.1rem, 5vw, 4rem); }
    .closing h2 { max-width: 750px; font-size: clamp(2rem, 4.2vw, 3.6rem); margin: .8rem 0; }
    .closing p { max-width: 700px; }
    .closing-link { display: inline-block; padding: .85rem 0; font-weight: 700; color: #194f45; }
    footer { border-top: 1px solid #bdc9c0; padding: 1.6rem clamp(1.1rem, 5vw, 4rem); display: flex; flex-wrap: wrap; justify-content: space-between; gap: 1rem; color: #56706b; font-size: .82rem; }
    a:focus-visible { outline: 3px solid #bb6835; outline-offset: 4px; }
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
export class C0601PublicSaasLandingComponent {}
