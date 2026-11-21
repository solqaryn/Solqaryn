import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { environment } from '../../../environments/environment';

type PublicHealthState = 'idle' | 'loading' | 'ready' | 'empty' | 'denied'
  | 'not-found' | 'rate-limited' | 'failure';

/**
 * C06-01.M04.02: Public, user-initiated, read-only health HTTP.
 * It does not validate private tenant access, JWT, RBAC, payments or uptime.
 */
@Component({
  selector: 'app-c0601-public-status',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="status-panel" aria-labelledby="c0601-status-heading" data-testid="c0601-public-status">
      <div class="status-copy">
        <p class="eyebrow">COMPROBACIÓN PÚBLICA</p>
        <h2 id="c0601-status-heading">Estado de conexión</h2>
        <p>Consulta el endpoint público de salud del servicio. No verifica tu acceso a empresas,
          permisos privados ni la disponibilidad de todos los módulos.</p>
      </div>
      <div class="status-control">
        <button type="button" data-testid="c0601-status-check"
          [disabled]="estado() === 'loading'" (click)="comprobar()">
          {{ estado() === 'loading' ? 'Consultando…' : 'Consultar estado' }}
        </button>
        <div class="status-response" data-testid="c0601-status-response"
          role="status" aria-live="polite" aria-atomic="true">
          @switch (estado()) {
            @case ('idle') {
              <p>Sin consulta todavía. La página no solicita información al servidor automáticamente.</p>
            }
            @case ('loading') {
              <p>Consultando el servicio público…</p>
            }
            @case ('ready') {
              <p>El servicio público respondió correctamente. No se han consultado datos privados.</p>
            }
            @case ('empty') {
              <p>El servidor respondió sin datos de estado. No se puede confirmar su disponibilidad.</p>
            }
            @case ('denied') {
              <p>Acceso a la comprobación denegado (401/403). No significa que se haya verificado tu sesión.</p>
            }
            @case ('not-found') {
              <p>La comprobación no está disponible en esta dirección (404).</p>
            }
            @case ('rate-limited') {
              <p>Demasiadas solicitudes (429). Vuelve a intentarlo más adelante.</p>
            }
            @case ('failure') {
              <p>No fue posible comprobar el servicio. Puedes volver a intentarlo.</p>
            }
          }
        </div>
      </div>
    </section>
  `,
  styles: [`
    :host { display: block; }
    * { box-sizing: border-box; }
    .status-panel {
      display: grid; grid-template-columns: minmax(0, 1.4fr) minmax(0, 1fr);
      gap: clamp(1.2rem, 4vw, 3rem); align-items: start;
      max-width: 1260px; margin: 0 auto; padding: clamp(2.3rem, 5vw, 4rem)
      clamp(1.1rem, 5vw, 4rem);
      border-block: 1px solid var(--sq-border); color: var(--sq-ink);
    }
    .eyebrow { color: var(--sq-emphasis); letter-spacing: .12em; font-size: .75rem; font-weight: 800; }
    h2 { font: 400 clamp(2rem, 3.7vw, 3rem)/1.15 Georgia, 'Times New Roman', serif; margin: .4rem 0 .9rem; }
    p { margin: .5rem 0; line-height: 1.7; overflow-wrap: anywhere; }
    .status-copy > p:last-child { color: var(--sq-muted); max-width: 48ch; }
    .status-control { min-width: 0; background: var(--sq-panel); padding: clamp(1.2rem, 2vw, 1.8rem); border-top: 3px solid var(--sq-accent); }
    button {
      display: inline-flex; min-height: 44px; min-width: 10rem; align-items: center;
      justify-content: center; border: 1px solid var(--sq-border);
      border-radius: .2rem; padding: .7rem 1.1rem;
      background: var(--sq-action); color: var(--sq-action-text);
      font: inherit; font-weight: 700; cursor: pointer;
    }
    button:disabled { opacity: .65; cursor: wait; }
    button:focus-visible { outline: 3px solid var(--sq-focus); outline-offset: 4px; }
    .status-response { margin-top: 1rem; min-height: 3.3rem; color: var(--sq-ink); }
    @media (max-width: 760px) { .status-panel { grid-template-columns: minmax(0, 1fr); } }
    @media (prefers-reduced-motion: reduce) { * { scroll-behavior: auto !important; } }
  `]
})
export class C0601PublicStatusComponent {
  readonly estado = signal<PublicHealthState>('idle');
  private readonly destroyRef = inject(DestroyRef);
  private activeController: AbortController | null = null;
  private destroyed = false;

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.destroyed = true;
      this.activeController?.abort();
    });
  }

  async comprobar(): Promise<void> {
    if (this.estado() === 'loading' || this.destroyed) return;
    const controller = new AbortController();
    this.activeController = controller;
    this.estado.set('loading');
    const timeout = setTimeout(() => controller.abort(), 8_000);
    try {
      const url = environment.apiUrl.replace(/\/$/, '') + '/health';
      const response = await fetch(url, {
        method: 'GET', mode: 'cors', credentials: 'omit', cache: 'no-store',
        redirect: 'error', headers: { Accept: 'application/json' },
        signal: controller.signal
      });
      if (this.destroyed || controller.signal.aborted) return;
      if (response.status === 204) { this.estado.set('empty'); return; }
      if (response.status === 401 || response.status === 403) { this.estado.set('denied'); return; }
      if (response.status === 404) { this.estado.set('not-found'); return; }
      if (response.status === 429) { this.estado.set('rate-limited'); return; }
      if (!response.ok) { this.estado.set('failure'); return; }

      const body: unknown = await response.json();
      if (this.destroyed || controller.signal.aborted) return;
      if (!body || typeof body !== 'object' || !('status' in body)) {
        this.estado.set('empty');
      } else {
        this.estado.set(body.status === 'ok' ? 'ready' : 'failure');
      }
    } catch {
      // Never expose provider message, response body, endpoint metadata or tokens.
      if (!this.destroyed) this.estado.set('failure');
    } finally {
      clearTimeout(timeout);
      if (this.activeController === controller) this.activeController = null;
    }
  }
}
