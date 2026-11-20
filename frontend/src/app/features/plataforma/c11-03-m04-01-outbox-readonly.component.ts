import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

/** C11-03.M04.01: UI-only, opt-in reference; never a live event feed. */
const ETAPAS = Object.freeze([
  Object.freeze({ id: 'outbox', nombre: 'Registro durable', evidencia: 'SIN_FUENTE_CONECTADA' }),
  Object.freeze({ id: 'relay', nombre: 'Reintentos y entrega', evidencia: 'SIN_FUENTE_CONECTADA' }),
  Object.freeze({ id: 'auditoria', nombre: 'Auditoría y correlación', evidencia: 'SIN_FUENTE_CONECTADA' })
] as const);

@Component({
  selector: 'app-c1103-outbox-readonly',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="c1103" aria-labelledby="c1103-title" data-testid="c1103-outbox-view">
      <header>
        <p class="eyebrow">SOLQARYN · Eventos internos · Referencia de desarrollo</p>
        <h1 id="c1103-title">Observabilidad del outbox</h1>
        <p>Superficie privada de solo lectura. La plataforma aún no conecta datos reales a esta vista.</p>
        <a routerLink="/plataforma" aria-label="Volver a plataforma">Volver a plataforma</a>
      </header>
      @if (autorizado()) {
        <section class="panel" data-testid="c1103-outbox-private" aria-labelledby="c1103-checkpoints" [attr.aria-busy]="estadoVista() === 'cargando' ? 'true' : 'false'">
          <h2 id="c1103-checkpoints">Etapas del procesamiento</h2>
          @switch (estadoVista()) {
            @case ('cargando') {
              <p role="status" aria-live="polite" aria-atomic="true" data-testid="c1103-state-loading">Preparando vista local; no se consulta un relay real.</p>
            }
            @case ('vacio') {
              <p role="status" aria-live="polite" aria-atomic="true" data-testid="c1103-state-empty">SIN_FUENTE_CONECTADA: los estados siguientes son referencias, no eventos observados.</p>
            }
            @case ('error') {
              <p role="alert" aria-atomic="true" data-testid="c1103-state-error">No fue posible preparar la vista informativa. Ningún evento fue transmitido.</p>
            }
            @case ('listo') {
              <p role="status" aria-live="polite" aria-atomic="true" data-testid="c1103-state-ready">Vista informativa preparada; SIN_FUENTE_CONECTADA, no confirma entregas de eventos.</p>
            }
            @default {
              <p role="alert" data-testid="c1103-state-invalid">Estado de vista no reconocido. No se muestran datos privados.</p>
            }
          }
          <ol>
            @for (paso of etapas; track paso.id) {
              <li [attr.data-testid]="'c1103-outbox-step-' + paso.id">
                <h3>{{ paso.nombre }}</h3>
                <p>Verificación: <strong>{{ paso.evidencia }}</strong></p>
              </li>
            }
          </ol>
          <p role="note">No se muestran payloads, tenant IDs, claves de idempotencia, errores de proveedor ni datos personales.</p>
          <p role="note">Esta pantalla no confirma JWT del servidor, eventos enviados o resultados de MySQL.</p>
        </section>
      } @else {
        <p role="alert" data-testid="c1103-outbox-denied">Acceso de plataforma no confirmado. La información privada permanece oculta.</p>
      }
    </section>
  `,
  styles: [`
    :host { display:block; color:var(--mat-sys-on-surface,#17212d); }
    .c1103 { box-sizing:border-box; max-width:74rem; margin:auto; padding:clamp(1rem,3vw,2.5rem); }
    h1 { font-size:clamp(1.5rem,3vw,2.2rem); line-height:1.2; }
    h2 { font-size:1.22rem; } h3 { font-size:1rem; margin:0; }
    p,li { line-height:1.55; overflow-wrap:anywhere; }
    .eyebrow { font-weight:700; color:var(--mat-sys-primary,#2157a3); }
    a { display:inline-block; box-sizing:border-box; min-height:44px; padding:.7rem .9rem;
        border:1px solid currentColor; border-radius:.5rem; color:inherit; }
    a:focus-visible { outline:3px solid var(--mat-sys-primary,#2157a3); outline-offset:3px; }
    .panel { margin-top:1rem; border:1px solid var(--mat-sys-outline-variant,#aeb8c1);
        border-radius:.75rem; padding:1rem; }
    ol { display:grid; grid-template-columns:repeat(auto-fit,minmax(min(100%,260px),1fr));
        list-style:none; gap:.8rem; margin:1rem 0; padding:0; }
    li { min-width:0; border:1px solid var(--mat-sys-outline-variant,#aeb8c1);
        border-radius:.65rem; padding:1rem; }
  `]
})
export class C1103OutboxReadonlyComponent {
  private readonly plataforma = inject(PlataformaSesionService);
  readonly estadoVista = input<string>('vacio');
  readonly etapas = ETAPAS;
  readonly autorizado = computed(() => {
    const sesion = this.plataforma.sesion();
    return sesion?.ambito === 'Plataforma' && sesion.esAdministrador === true;
  });
}
