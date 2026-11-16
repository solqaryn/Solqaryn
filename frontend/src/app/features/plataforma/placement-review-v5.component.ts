import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { PLACEMENT_PRESENTACION_V5, normalizarPresentacionPlacementV5 } from './placement-presentacion-v5';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

export type EstadoPlacementV5 = 'vacio' | 'cargando' | 'exito' | 'sin_permiso' | 'error';

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
    <main class="placement-review" [attr.data-densidad]="presentacion().densidad" aria-labelledby="placement-title">
      <header>
        <p class="eyebrow">{{ marcaVisible() }} · Almacenamiento multitenant</p>
        <h1 id="placement-title">Placement compartido y dedicado</h1>
        <p>Contrato de revisión privada. Ningún destino, credencial o migración se determina desde el navegador.</p>
        <a routerLink="/plataforma" aria-label="Regresar a administración de plataforma">Volver a plataforma</a>
        <button type="button" (click)="revalidar()" [disabled]="estado() === 'cargando'" aria-label="Revalidar sesión de placement">Revalidar sesión</button>
      </header>
      <section class="estado" aria-label="Estado de autorización de placement" aria-live="polite" aria-atomic="true" [attr.aria-busy]="estado() === 'cargando'">
        @switch (estadoVisible()) {
          @case ('cargando') { <p role="status">Revalidando autorización de plataforma con el servidor…</p> }
          @case ('sin_permiso') { <p role="alert">Sesión administrativa no confirmada. Placement privado oculto.</p> }
          @case ('error') { <p role="alert">No fue posible revalidar la sesión. La información permanece restringida.</p> }
          @case ('vacio') { <p role="status">Sesión verificada. Sin datos de placement conectados; solo guía.</p> }
          @case ('exito') { <p role="status">Sesión revalidada. Solo consulta documental, sin migraciones.</p> }
        }
      </section>
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
    header :is(a, button) { display:inline-block; margin-top:.3rem; margin-right:.5rem; border:2px solid currentColor; padding:.65rem .85rem; border-radius:.5rem; text-decoration:underline; color:inherit; font:inherit; background:transparent; cursor:pointer; }
    header button[disabled] { opacity:.55; cursor:progress; }
    header :is(a, button):focus-visible { outline:3px solid var(--mat-sys-primary,#2157a3); outline-offset:3px; }
.estado,.notice,.denied { margin-top:1rem; padding:1rem; border-radius:.65rem; border:1px solid var(--mat-sys-outline-variant,#aeb8c1); background:var(--mat-sys-surface-container-low,#f5f7fa); }
    .modalidades { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:1rem; }
    .placement-review[data-densidad="compacta"] .modalidades { gap:.5rem; }
    .placement-review[data-densidad="compacta"] .modalidades article { padding:.65rem; }
    .modalidades article { min-width:0; padding:1rem; border:1px solid var(--mat-sys-outline-variant,#aeb8c1); border-radius:.65rem; }
    .limitacion { font-weight:600; }
    @media(max-width:45rem) { .modalidades { grid-template-columns:1fr; } }
  `]
})
export class PlacementReviewV5Component {
  private readonly plataforma = inject(PlataformaSesionService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly presentacionFuente = inject(PLACEMENT_PRESENTACION_V5);
  readonly presentacion = computed(() => normalizarPresentacionPlacementV5(this.presentacionFuente()));
  readonly marcaVisible = computed(() => this.presentacion().marca === 'institucional' ? 'Plataforma SOLQARYN' : 'SOLQARYN');

  /** M04.02: a browser label never grants platform authority. */
  readonly estado = signal<EstadoPlacementV5>(this.plataforma.sesion() ? 'vacio' : 'sin_permiso');
  readonly accesoVerificado = computed(() => {
    const sesion = this.plataforma.sesion();
    const actual = this.estado();
    return (actual === 'vacio' || actual === 'exito') &&
      sesion?.ambito === 'Plataforma' && sesion.esAdministrador === true;
  });
  readonly estadoVisible = computed<EstadoPlacementV5>(() => {
    if (this.estado() === 'cargando') return 'cargando';
    if (this.estado() === 'error') return 'error';
    if (!this.accesoVerificado()) return 'sin_permiso';
    return this.estado();
  });

  readonly modos = [
    { nombre: 'Shared', responsabilidad: 'Placement central evaluado en el backend para cada tenant.',
      limite: 'Sin selección del cliente, DSN visible ni acceso cruzado desde esta pantalla.' },
    { nombre: 'Dedicated', responsabilidad: 'Separación dedicada sujeta a contratos de autorización y auditoría.',
      limite: 'No activa un pool, migración, conexión ni cutover por visualizarse.' }
  ] as const;

  revalidar(): void {
    if (this.estado() === 'cargando') return; // single-flight, no duplicate request.
    this.estado.set('cargando'); // hide all protected guidance before external I/O.
    this.plataforma.obtener().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (sesion) => {
        const actual = this.plataforma.sesion();
        const confirmado = sesion?.ambito === 'Plataforma' &&
          sesion?.esAdministrador === true &&
          actual?.ambito === 'Plataforma' && actual.esAdministrador === true;
        this.estado.set(confirmado ? 'exito' : 'sin_permiso');
      },
      complete: () => {
        if (this.estado() === 'cargando') this.estado.set('error'); // empty observable is not PASS.
      },
      error: (error: unknown) => {
        const status = error instanceof HttpErrorResponse ? error.status : 0;
        this.estado.set(status === 401 || status === 403 ? 'sin_permiso' : 'error');
      }
    });
  }
}
