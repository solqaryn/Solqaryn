import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <main class="not-found" aria-labelledby="not-found-title">
      <section class="not-found__card">
        <p class="not-found__eyebrow">Ruta no disponible</p>
        <h1 id="not-found-title">Página no encontrada</h1>
        <p>La página solicitada no existe o ya no está disponible. Puedes regresar al acceso o visitar la tienda pública.</p>
        <nav class="not-found__actions" aria-label="Opciones de navegación">
          <a class="not-found__primary" routerLink="/login">Ir a iniciar sesión</a>
          <a class="not-found__secondary" routerLink="/tienda">Ver tienda pública</a>
        </nav>
      </section>
    </main>
  `,
  styles: [`
    :host { display: block; }
    .not-found {
      min-height: 60vh;
      display: grid;
      place-items: center;
      padding: clamp(1.5rem, 5vw, 4rem);
      color: var(--color-text, #192231);
    }
    .not-found__card {
      box-sizing: border-box;
      width: min(100%, 36rem);
      padding: clamp(1.5rem, 4vw, 2.5rem);
      background: var(--color-surface, #fff);
      border: 1px solid var(--color-border, #d7dce4);
      border-radius: 1rem;
    }
    .not-found__eyebrow {
      margin: 0 0 .75rem;
      font-weight: 600;
      font-size: .875rem;
      letter-spacing: .025em;
    }
    h1 { margin: 0 0 1rem; line-height: 1.2; font-size: clamp(1.6rem, 4vw, 2.2rem); }
    p { line-height: 1.6; }
    .not-found__actions { display: flex; flex-wrap: wrap; gap: .75rem; margin-top: 1.5rem; }
    .not-found__actions a {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      box-sizing: border-box;
      min-height: 44px;
      padding: .65rem 1rem;
      border-radius: .55rem;
      text-decoration: none;
      font-weight: 600;
    }
    .not-found__primary { background: var(--color-primary, #253e82); color: #fff; }
    .not-found__secondary { border: 1px solid currentColor; color: inherit; }
    .not-found__actions a:focus-visible { outline: 3px solid var(--color-primary, #253e82); outline-offset: 3px; }
    @media (max-width: 420px) {
      .not-found__actions { flex-direction: column; }
      .not-found__actions a { width: 100%; }
    }
  `]
})
export class NotFoundComponent {}
