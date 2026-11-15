import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';
import { RouterLink } from '@angular/router';

export type EstadoGobiernoV5 = 'cargando' | 'vacio' | 'error' | 'exito' | 'sin_permiso';

/**
 * C00-04.M04.01: contractual, protected read-only surface.
 * The plataformaGuard verifies the platform session with the backend.
 * No plan data is fetched and this view does not approve or execute changes.
 */
@Component({
  selector: 'app-gobierno-cambios-v5',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="governance" aria-labelledby="governance-title">
      <header class="governance-header">
        <div>
          <p class="eyebrow">SOLQARYN · Gobierno de cambios</p>
          <h1 id="governance-title">Decisiones y controles V5</h1>
          <p class="intro">Referencia de consulta para cambios de la plataforma. Esta pantalla no accede al plan institucional ni muestra estados vigentes.</p>
        </div>
        <button class="return-link" type="button" (click)="revalidar()" [disabled]="estado() === 'cargando'" aria-label="Revalidar sesión de plataforma">Revalidar sesión</button>
        <a class="return-link" routerLink="/plataforma" aria-label="Volver a administración de plataforma">Volver a plataforma</a>
      </header>

      <section class="state-banner" aria-live="polite" aria-atomic="true" aria-label="Estado de acceso y consulta">
        @if (estado() === 'cargando') {
          <p role="status">Verificando permisos con el servidor…</p>
        } @else if (estado() === 'sin_permiso') {
          <p role="alert">Sin permiso de plataforma confirmado. No se muestra contenido protegido.</p>
        } @else if (estado() === 'error') {
          <p role="alert">No fue posible verificar la sesión. El acceso permanece restringido; reintenta la comprobación.</p>
        } @else if (estado() === 'vacio') {
          <p role="status">Sesión verificada. Sin datos institucionales conectados; solo está disponible el contrato de consulta.</p>
        } @else {
          <p role="status">Sesión de plataforma revalidada correctamente. La vista sigue siendo solo lectura.</p>
          <p>Sin datos institucionales conectados: no hay estados reales de cambios para mostrar.</p>
        }
      </section>

      @if (estado() === 'exito' || estado() === 'vacio') {
      <section class="notice" aria-label="Límites de esta pantalla">
        <strong>Solo lectura · sin autoridad operativa</strong>
        <p>Una sesión de plataforma verificada permite consultar esta explicación, pero no autoriza migraciones, permisos, publicaciones ni cambios de empresa.</p>
      </section>

      <section aria-labelledby="controls-title">
        <h2 id="controls-title">Secuencia de control</h2>
        <ol class="steps">
          <li><span class="step-index" aria-hidden="true">01</span><div><h3>Alcance y dependencias</h3><p>Identificar el ID exacto, el propietario exclusivo y la validación de PLAN, COLA y DAG.</p></div></li>
          <li><span class="step-index" aria-hidden="true">02</span><div><h3>Pruebas dirigidas</h3><p>Evaluar el cambio y su aislamiento con evidencia positiva, negativa y de seguridad acorde al componente.</p></div></li>
          <li><span class="step-index" aria-hidden="true">03</span><div><h3>Verificación DEV</h3><p>Confirmar el SHA, Scope Lock y controles pertinentes terminados satisfactoriamente. La ejecución pendiente no es aprobación.</p></div></li>
          <li><span class="step-index" aria-hidden="true">04</span><div><h3>Revisión independiente A5</h3><p>Una Task distinta de la autora revisa la evidencia antes de liberar dependencias que la exijan.</p></div></li>
        </ol>
      </section>

      <p class="footnote" role="note">Contenido estático: no se consultan microtareas, JWT, permisos, bases de datos o auditorías en vivo. No existen acciones para aprobar, ejecutar o desplegar desde esta vista.</p>
      }
    </main>
  `,
  styles: [`
    :host { display: block; min-height: 100%; color: var(--mat-sys-on-surface, #17212d); }
    .governance { box-sizing: border-box; max-width: 70rem; margin: 0 auto; padding: clamp(1rem, 4vw, 2.5rem); }
    .governance-header { display: flex; justify-content: space-between; gap: 1.5rem; align-items: flex-start; }
    .eyebrow { color: var(--mat-sys-primary, #2157a3); letter-spacing: .04em; font-size: .85rem; font-weight: 700; }
    h1 { margin: .35rem 0 .75rem; font-size: clamp(1.6rem, 4vw, 2.4rem); line-height: 1.2; }
    h2 { margin: 1.8rem 0 1rem; font-size: 1.35rem; }
    h3 { margin: 0 0 .3rem; font-size: 1.06rem; }
    p { line-height: 1.55; }
    .intro { max-width: 49rem; }
    .return-link { flex-shrink: 0; padding: .65rem .85rem; border: 1px solid currentColor; border-radius: .6rem; color: inherit; text-decoration: none; font-weight: 600; }
    .return-link:focus-visible { outline: 3px solid var(--mat-sys-primary, #2157a3); outline-offset: 4px; }
    .state-banner { margin: 1.25rem 0; padding: .8rem 1rem; border: 1px solid var(--mat-sys-outline-variant, #c0c7cf); border-radius: .75rem; }
    .state-banner p { margin: .25rem 0; }
    .return-link[disabled] { opacity: .65; cursor: progress; }
    .notice { margin-top: 1.25rem; padding: 1rem 1.15rem; border-radius: .8rem; border: 1px solid var(--mat-sys-outline, #6e7782); background: var(--mat-sys-surface-container-low, #f5f7fa); }
    .notice p { margin-bottom: 0; }
    .steps { padding: 0; list-style: none; display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: .85rem; }
    .steps li { border: 1px solid var(--mat-sys-outline-variant, #c0c7cf); border-radius: .75rem; padding: 1rem; display: flex; gap: .85rem; min-width: 0; }
    .steps p { margin: 0; }
    .step-index { font-weight: 800; font-variant-numeric: tabular-nums; color: var(--mat-sys-primary, #2157a3); }
    .footnote { margin-top: 1.7rem; font-size: .9rem; }
    @media (max-width: 45rem) { .governance-header { flex-direction: column; } .steps { grid-template-columns: minmax(0, 1fr); } }
    @media (prefers-reduced-motion: reduce) { .return-link { transition: none; } }
  `]
})
export class GobiernoCambiosV5Component {
  private readonly plataforma = inject(PlataformaSesionService);
  private readonly destroyRef = inject(DestroyRef);
  readonly estado = signal<EstadoGobiernoV5>(this.plataforma.sesion() ? 'vacio' : 'sin_permiso');

  /** Revalidation is single-flight and never infers authority from JWT claims. */
  revalidar(): void {
    if (this.estado() === 'cargando') return;
    this.estado.set('cargando');
    this.plataforma.obtener().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.estado.set('exito'),
      error: (error: unknown) => {
        const status = error instanceof HttpErrorResponse ? error.status : 0;
        this.estado.set(status === 401 || status === 403 ? 'sin_permiso' : 'error');
      }
    });
  }
}
