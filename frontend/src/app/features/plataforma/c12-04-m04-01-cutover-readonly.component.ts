import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

/** Contractual checkpoints, not results from MySQL, Aiven or a cutover provider. */
const CONTROLES = Object.freeze([
  { id: 'exportacion', nombre: 'Exportación aislada', evidencia: 'SIN_VERIFICAR' },
  { id: 'restauracion', nombre: 'Restauración de ensayo', evidencia: 'SIN_VERIFICAR' },
  { id: 'comparacion', nombre: 'Checksums y conteos', evidencia: 'NO_MEDIDO' },
  { id: 'autorizacion', nombre: 'Ventana, rollback y aprobación explícita', evidencia: 'NO_AUTORIZADO' }
] as const);

/**
 * C12-04.M04.01 — Private read-only cutover reference.
 * The browser cannot choose a tenant, DSN, migration, backup or deployment.
 */
@Component({
  selector: 'app-c1204-cutover-readonly',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="cutover" aria-labelledby="cutover-title">
      <header>
        <p class="eyebrow">SOLQARYN · Preparación MySQL privado</p>
        <h1 id="cutover-title">Controles de preparación para cutover</h1>
        <p>Referencia de lectura protegida; no muestra el estado de bases de datos reales.</p>
        <a routerLink="/plataforma" aria-label="Volver a plataforma">Volver a plataforma</a>
      </header>
      @if (autorizado()) {
        <section class="aviso" role="note" aria-label="Límite de operación">
          <strong>NO OPERATIVO · Sin acceso a MySQL ni ejecución de migraciones</strong>
          <p>Ninguna restauración, exportación, comparación ni aprobación se ejecuta desde esta vista.</p>
        </section>
        <section class="panel" data-testid="c1204-cutover-private" aria-labelledby="pasos-title">
          <h2 id="pasos-title">Evidencia necesaria antes de operar</h2>
          <ol class="controles">
            @for (control of controles; track control.id) {
              <li>
                <h3>{{ control.nombre }}</h3>
                <p>Estado de evidencia: <strong>{{ control.evidencia }}</strong></p>
              </li>
            }
          </ol>
          <p role="note">Los estados son límites de evidencia, no resultados de pruebas ni autorización técnica.</p>
        </section>
      } @else {
        <p class="denegado" role="alert">Acceso de plataforma no confirmado. Detalles privados ocultos.</p>
      }
    </main>
  `,
  styles: [`
    :host { display:block; color:var(--mat-sys-on-surface,#17212d); }
    .cutover { box-sizing:border-box; max-width:72rem; margin-inline:auto; padding:clamp(1rem,3vw,2.5rem); }
    h1 { font-size:clamp(1.5rem,4vw,2.2rem); line-height:1.2; }
    h2 { font-size:1.3rem; } h3 { font-size:1rem; margin:0; }
    p, li { line-height:1.55; overflow-wrap:anywhere; }
    .eyebrow { font-weight:700; color:var(--mat-sys-primary,#2157a3); }
    a { display:inline-block; padding:.7rem .9rem; border:1px solid currentColor; border-radius:.55rem; color:inherit; }
    a:focus-visible { outline:3px solid var(--mat-sys-primary,#2157a3); outline-offset:3px; }
    .aviso, .panel, .denegado { border:1px solid var(--mat-sys-outline-variant,#aeb8c1); border-radius:.75rem; padding:1rem; margin-top:1rem; }
    .aviso { background:var(--mat-sys-surface-container-low,#f5f7fa); }
    .controles { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:1rem; padding:0; list-style:none; }
    .controles li { min-width:0; border:1px solid var(--mat-sys-outline-variant,#aeb8c1); border-radius:.65rem; padding:1rem; }
    @media (max-width:45rem) { .controles { grid-template-columns:1fr; } }
    @media (prefers-contrast:more) { a:focus-visible { outline-color:CanvasText; outline-width:4px; } }
  `]
})
export class C1204CutoverReadonlyComponent {
  private readonly plataforma = inject(PlataformaSesionService);
  readonly controles = CONTROLES;
  readonly autorizado = computed(() => {
    const sesion = this.plataforma.sesion();
    return sesion?.ambito === 'Plataforma' && sesion.esAdministrador === true;
  });
}
