import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

export type EstadoAuditoriaOperativaV5 = 'cargando' | 'vacio' | 'error' | 'exito' | 'sin_permiso';

/**
 * C15-05.M04.01 — superficie privada de consulta de auditoria operativa.
 * La ruta es protegida por plataformaGuard (verificacion backend de sesion).
 * Esta superficie NO consulta Sheets/GitHub ni concede autoridad de CLAIM/A5.
 */
@Component({
  selector: 'app-auditoria-operativa-v5',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="audit-view" aria-labelledby="audit-view-title">
      <header>
        <p class="eyebrow">SOLQARYN · Auditoría operativa</p>
        <h1 id="audit-view-title">Controles de ejecución y evidencia</h1>
        <p>Referencia de lectura para el ciclo DEV. No presenta telemetría ni microtareas en vivo.</p>
        <button class="refresh-button" type="button" (click)="revalidar()" [disabled]="estado() === 'cargando'" aria-label="Revalidar sesión de auditoría">Revalidar sesión</button>
      </header>

      <section class="audit-state" aria-live="polite" aria-atomic="true" aria-label="Estado de acceso y consulta">
        @switch (estado()) {
          @case ('cargando') { <p role="status">Verificando permisos con el servidor…</p> }
          @case ('sin_permiso') { <p role="alert">Sin permiso de plataforma confirmado. La guía privada permanece oculta.</p> }
          @case ('error') { <p role="alert">No fue posible verificar la sesión. El acceso permanece restringido; vuelve a intentarlo.</p> }
          @case ('vacio') { <p role="status">Sesión verificada. No hay datos institucionales conectados; esta guía es de solo lectura.</p> }
          @case ('exito') { <p role="status">Sesión de plataforma revalidada correctamente. No se consultan registros operativos en vivo.</p> }
        }
      </section>

      @if (accesoPlataforma() && (estado() === 'vacio' || estado() === 'exito')) {
        <section class="notice" aria-label="Alcance y fuente de esta pantalla">
          <strong>Guía estática · sin operaciones institucionales</strong>
          <p>Los estados, propietarios, SHA y revisiones deben verificarse en sus fuentes autorizadas. Esta pantalla no sustituye PLAN, COLA, DAG, BITACORA ni CI.</p>
        </section>

        <section aria-labelledby="audit-controls-title">
          <h2 id="audit-controls-title">Puntos de control</h2>
          <ol class="controls">
            <li><h3>01 · Propiedad exclusiva</h3><p>Verificar el ID, dueño, dependencia y candado de fila antes de trabajar. Nunca dos escritores sobre la misma unidad.</p></li>
            <li><h3>02 · Evidencia A0–A3</h3><p>Revisar requisitos, pruebas positivas y negativas, aislamiento tenant y la integración pertinente al riesgo.</p></li>
            <li><h3>03 · Publicación A4</h3><p>Comparar el SHA exacto, Scope Lock y checks requeridos finalizados con éxito; pendiente o cancelado no equivale a aprobado.</p></li>
            <li><h3>04 · Revisión A5 y continuidad</h3><p>La revisión independiente corresponde a otra Task autorizada. Recalcular dependencias solo después de acreditar la transición.</p></li>
          </ol>
        </section>
        <p role="note">Sin resultados operativos conectados: 0 métricas verificadas desde esta vista. No hay acciones para reclamar, aprobar, modificar o desplegar.</p>
      } @else {
        <p class="notice" role="alert">Acceso de plataforma no confirmado. El contenido protegido permanece oculto.</p>
      }

      <a class="return-link" routerLink="/plataforma" aria-label="Regresar a administración de plataforma">Regresar a plataforma</a>
    </main>
  `,
  styles: [`
    :host { display: block; color: var(--mat-sys-on-surface, #17212d); }
    .audit-view { box-sizing: border-box; max-width: 70rem; margin-inline: auto; padding: clamp(1rem, 3vw, 2.5rem); }
    .eyebrow { color: var(--mat-sys-primary, #2157a3); font-weight: 700; letter-spacing: .035em; }
    h1 { font-size: clamp(1.65rem, 4vw, 2.4rem); line-height: 1.2; margin: .3rem 0 1rem; }
    h2 { font-size: 1.35rem; margin-top: 1.6rem; }
    h3 { font-size: 1.05rem; margin: 0 0 .45rem; }
    p { line-height: 1.55; }
    .audit-state { border: 1px solid var(--mat-sys-outline-variant, #a7b2bd); border-radius: .75rem; padding: .65rem 1rem; margin: 1rem 0; }
    .audit-state p { margin: .25rem 0; }
    .refresh-button { border: 1px solid currentColor; border-radius: .5rem; padding: .65rem 1rem; font: inherit; font-weight: 600; color: inherit; background: transparent; cursor: pointer; }
    .refresh-button[disabled] { opacity: .65; cursor: progress; }
    .refresh-button:focus-visible { outline: 3px solid var(--mat-sys-primary, #2157a3); outline-offset: 3px; }
    .notice { padding: 1rem; margin: 1.25rem 0; border: 1px solid var(--mat-sys-outline-variant, #a7b2bd); border-radius: .75rem; background: var(--mat-sys-surface-container-low, #f5f7fa); }
    .notice p { margin-bottom: 0; }
    .controls { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 1rem; padding: 0; list-style: none; }
    .controls li { border: 1px solid var(--mat-sys-outline-variant, #a7b2bd); border-radius: .75rem; padding: 1rem; min-width: 0; }
    .controls li p { margin: 0; }
    .return-link { display: inline-block; padding: .75rem 1rem; border-radius: .5rem; border: 1px solid currentColor; color: inherit; text-decoration: none; font-weight: 600; margin-top: 1rem; }
    .return-link:focus-visible { outline: 3px solid var(--mat-sys-primary, #2157a3); outline-offset: 3px; }
    @media (max-width: 45rem) { .controls { grid-template-columns: minmax(0, 1fr); } }
    @media (prefers-reduced-motion: reduce) { .return-link { transition: none; } }
  `]
})
export class AuditoriaOperativaV5Component {
  private readonly plataforma = inject(PlataformaSesionService);
  private readonly destroyRef = inject(DestroyRef);
  readonly accesoPlataforma = computed(() => {
    const sesion = this.plataforma.sesion();
    return sesion?.ambito === 'Plataforma' && sesion.esAdministrador === true;
  });
  readonly estado = signal<EstadoAuditoriaOperativaV5>(this.accesoPlataforma() ? 'vacio' : 'sin_permiso');

  /** A new check always comes from the server; browser tokens never grant permissions. */
  revalidar(): void {
    if (this.estado() === 'cargando') return;
    this.estado.set('cargando');
    this.plataforma.obtener().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.estado.set(this.accesoPlataforma() ? 'exito' : 'sin_permiso'),
      error: (error: unknown) => {
        const status = error instanceof HttpErrorResponse ? error.status : 0;
        this.estado.set(status === 401 || status === 403 ? 'sin_permiso' : 'error');
      }
    });
  }
}
