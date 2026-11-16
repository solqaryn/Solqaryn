import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

/**
 * C02-02.M04.01: review-only contract for shared/dedicated placement.
 * No client DSN, database routing, plan mutation or migration can run in the browser.
 */
@Component({
  selector: 'app-placement-review-v5',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="placement-review" aria-labelledby="placement-title">
      <header>
        <p class="eyebrow">SOLQARYN · Almacenamiento multitenant</p>
        <h1 id="placement-title">Placement compartido y dedicado</h1>
        <p>Contrato de revisión privada. Ningún destino, credencial o migración se determina desde el navegador.</p>
        <a routerLink="/plataforma" aria-label="Regresar a administración de plataforma">Volver a plataforma</a>
      </header>
      @if (accesoVerificado()) {
        <section class="notice" aria-label="Estado de integración">
          <strong>Solo lectura · sin operaciones de base de datos</strong>
          <p>Placement, router del servidor y migraciones reales: NO_MEDIDO / NO_EJECUTADO.</p>
        </section>
        <section aria-labelledby="modos-title">
          <h2 id="modos-title">Modalidades previstas</h2>
          <div class="modalidades">
            @for (modo of modos; track modo.nombre) {
              <article>
                <h3>{{ modo.nombre }}</h3>
                <p>{{ modo.responsabilidad }}</p>
                <p class="limitacion">{{ modo.limite }}</p>
              </article>
            }
          </div>
        </section>
        <section aria-labelledby="fases-title">
          <h2 id="fases-title">Controles previos a un cambio real</h2>
          <ol>
            <li>Identidad actual y autorización de plataforma confirmadas por backend.</li>
            <li>Ownership de tenant y políticas de placement validadas server-side.</li>
            <li>Plan explícito de migración, compatibilidad y rollback reversible.</li>
            <li>Pruebas MySQL aisladas, auditabilidad y aprobación técnica independiente.</li>
          </ol>
        </section>
        <p role="note">No se muestran DSN, cadenas de conexión, secretos, claves de clientes ni controles de cutover.</p>
      } @else {
        <p role="alert" class="denied">Sesión de plataforma no confirmada. Placement privado oculto.</p>
      }
    </main>
  `,
  styles: [`
    :host { display:block; color:var(--mat-sys-on-surface,#17212d); }
    .placement-review { max-width:70rem; width:100%; box-sizing:border-box; margin:auto; padding:clamp(1rem,3vw,2.5rem); }
    h1 { font-size:clamp(1.6rem,4vw,2.2rem); line-height:1.2; }
    h2 { margin-top:1.5rem; font-size:1.3rem; }
    h3 { margin:0 0 .35rem; font-size:1.1rem; }
    p,li { line-height:1.55; overflow-wrap:anywhere; }
    .eyebrow { font-weight:700; color:var(--mat-sys-primary,#2157a3); }
    header a { display:inline-block; margin-top:.3rem; border:2px solid currentColor; padding:.65rem .85rem; border-radius:.5rem; text-decoration:underline; color:inherit; }
    header a:focus-visible { outline:3px solid var(--mat-sys-primary,#2157a3); outline-offset:3px; }
    .notice,.denied { margin-top:1rem; padding:1rem; border-radius:.65rem; border:1px solid var(--mat-sys-outline-variant,#aeb8c1); background:var(--mat-sys-surface-container-low,#f5f7fa); }
    .modalidades { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:1rem; }
    .modalidades article { min-width:0; padding:1rem; border:1px solid var(--mat-sys-outline-variant,#aeb8c1); border-radius:.65rem; }
    .limitacion { font-weight:600; }
    @media(max-width:45rem) { .modalidades { grid-template-columns:1fr; } }
  `]
})
export class PlacementReviewV5Component {
  private readonly plataforma = inject(PlataformaSesionService);
  readonly accesoVerificado = computed(() => {
    const sesion = this.plataforma.sesion();
    return sesion?.ambito === 'Plataforma' && sesion.esAdministrador === true;
  });
  readonly modos = [
    { nombre: 'Shared', responsabilidad: 'Placement central evaluado en el backend para cada tenant.',
      limite: 'Sin selección del cliente, DSN visible ni acceso cruzado desde esta pantalla.' },
    { nombre: 'Dedicated', responsabilidad: 'Separación dedicada sujeta a contratos de autorización y auditoría.',
      limite: 'No activa un pool, migración, conexión ni cutover por visualizarse.' }
  ] as const;
}
