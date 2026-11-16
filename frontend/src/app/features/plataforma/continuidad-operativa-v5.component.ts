import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { CONTINUIDAD_PRESENTACION_V5, normalizarPresentacionV5 } from './continuidad-presentacion-v5';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { defer } from 'rxjs';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

export type EstadoContinuidadV5 = 'vacio' | 'cargando' | 'exito' | 'sin_permiso' | 'error';
/** C10-04.M04.01: informacion privada, NO una restauracion, cola ni permiso. */
@Component({
  selector: 'app-continuidad-operativa-v5',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: "\n<main class=\"continuidad\" [attr.data-densidad]=\"presentacion().densidad\" aria-labelledby=\"continuidad-title\">\n  <header>\n    <p class=\"eyebrow\">{{ marcaVisible() }} · Continuidad operativa</p>\n    <h1 id=\"continuidad-title\">Controles de recuperación y aislamiento</h1>\n    <p>Guía privada de consulta. No hay incidentes, backups ni colas en vivo.</p>\n    <div class=\"actions\">\n      <button type=\"button\" (click)=\"revalidar()\" [disabled]=\"estado() === 'cargando'\" aria-label=\"Revalidar sesión de continuidad\">Revalidar sesión</button>\n      <a routerLink=\"/plataforma\" aria-label=\"Regresar a plataforma\">Volver a plataforma</a>\n    </div>\n  </header>\n  <section class=\"estado\" [attr.aria-busy]=\"estado() === 'cargando'\" aria-live=\"polite\" aria-atomic=\"true\" aria-label=\"Estado de acceso\">\n    @switch (estadoVisible()) {\n      @case ('cargando') { <p role=\"status\">Revalidando la sesión con el servidor…</p> }\n      @case ('sin_permiso') { <p role=\"alert\">Acceso de plataforma no confirmado. Contenido protegido oculto.</p> }\n      @case ('error') { <p role=\"alert\">No fue posible validar la sesión. El acceso sigue restringido.</p> }\n      @case ('vacio') { <p role=\"status\">Sesión verificada. Sin datos institucionales conectados.</p> }\n      @case ('exito') { <p role=\"status\">Sesión revalidada. Solo consulta documental.</p> }\n    }\n  </section>\n  @if (estadoVisible() === 'vacio' || estadoVisible() === 'exito') {\n    <section class=\"aviso\" aria-label=\"Límites de autoridad\">\n      <strong>Solo lectura · restauración deshabilitada</strong>\n      <p>Esta pantalla no ejecuta restauraciones, no autoriza permisos y no accede a bases de datos ni proveedores.</p>\n    </section>\n    <section aria-labelledby=\"controles-title\">\n      <h2 id=\"controles-title\">Controles antes de una recuperación real</h2>\n      <ol class=\"controles\" aria-labelledby=\"controles-title\">\n        <li><h3>Identidad y tenancy</h3><p>Verificar actor, empresa, roles y denegación de accesos cruzados.</p></li>\n        <li><h3>Dependencias e idempotencia</h3><p>Validar SHA, predecesores, propietario exclusivo y ausencia de duplicados.</p></li>\n        <li><h3>Respaldo y reversión</h3><p>Exigir pruebas de restauración aislada, evidencias y rollback antes de operar.</p></li>\n        <li><h3>Revisión independiente</h3><p>Requerir CI pertinente terminada y A5 de una Task distinta de la autora.</p></li>\n      </ol>\n    </section>\n    <p class=\"limite\" role=\"note\">Sin métricas, restauraciones ni aprobaciones verificadas desde esta interfaz.</p>\n  }\n</main>",
  styles: ["\n:host { display: block; color: var(--mat-sys-on-surface, #17212d); }\n.continuidad { max-width: 72rem; margin-inline: auto; box-sizing: border-box; padding: clamp(1rem,3vw,2.5rem); }\nh1 { font-size: clamp(1.6rem,4vw,2.3rem); line-height: 1.2; }\nh2 { margin-top: 1.5rem; font-size: 1.3rem; }\nh3 { font-size: 1rem; margin: 0 0 .3rem; }\np { line-height: 1.55; overflow-wrap: anywhere; }\n.eyebrow { font-weight: 700; color: var(--mat-sys-primary, #2157a3); }\n.actions { display: flex; gap: .6rem; flex-wrap: wrap; }\n.actions :is(button,a) { border: 1px solid currentColor; border-radius: .55rem; font: inherit; color: inherit; background: transparent; padding: .65rem .85rem; text-decoration: none; }\n.actions :is(button,a):focus-visible { outline: 3px solid var(--mat-sys-primary, #2157a3); outline-offset: 3px; }\n.actions button[disabled] { opacity: .55; cursor: progress; }\n.estado,.aviso { border: 1px solid var(--mat-sys-outline-variant, #aeb8c1); border-radius: .8rem; padding: .85rem 1rem; margin-top: 1rem; }\n.aviso { background: var(--mat-sys-surface-container-low, #f5f7fa); }\n.continuidad[data-densidad=\"compacta\"] .controles { gap: .5rem; }\n.continuidad[data-densidad=\"compacta\"] .controles li { padding: .75rem; }\n.controles { display: grid; grid-template-columns: repeat(2,minmax(0,1fr)); gap: .8rem; padding: 0; list-style: none; }\n.controles li { min-width: 0; border: 1px solid var(--mat-sys-outline-variant, #aeb8c1); border-radius: .8rem; padding: 1rem; }\n.controles p { margin: 0; }\n.limite { font-size: .9rem; }\n@media (max-width: 45rem) { .controles { grid-template-columns: 1fr; } }\n@media (prefers-contrast: more) { .actions :is(button,a):focus-visible { outline: 4px solid CanvasText !important; outline-offset: 4px; background: Canvas; color: CanvasText; } }\n@media (prefers-reduced-motion: reduce) { .actions :is(button,a) { transition: none; } }"]
})
export class ContinuidadOperativaV5Component {
  private readonly plataforma = inject(PlataformaSesionService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly presentacionFuente = inject(CONTINUIDAD_PRESENTACION_V5);
  readonly presentacion = computed(() => normalizarPresentacionV5(this.presentacionFuente()));
  readonly marcaVisible = computed(() => this.presentacion().marca === 'institucional' ? 'Plataforma SOLQARYN' : 'SOLQARYN');
  readonly estado = signal<EstadoContinuidadV5>(this.plataforma.sesion() ? 'vacio' : 'sin_permiso');
  /** M04.02: revocacion externa oculta inmediatamente la guia, sin conceder autoridad. */
  readonly estadoVisible = computed<EstadoContinuidadV5>(() => {
    const actual = this.estado();
    const sesion = this.plataforma.sesion();
    if ((actual === 'vacio' || actual === 'exito') &&
        (sesion?.ambito !== 'Plataforma' || sesion.esAdministrador !== true)) {
      return 'sin_permiso';
    }
    return actual;
  });
  revalidar(): void {
    if (this.estado() === 'cargando') return;
    this.estado.set('cargando');
    // Defer catches synchronous provider failures and restores the safe retry state.
    defer(() => this.plataforma.obtener()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (sesion) => {
        const vigente = this.plataforma.sesion();
        const confirmada = sesion?.ambito === 'Plataforma' &&
          sesion?.esAdministrador === true &&
          vigente?.ambito === 'Plataforma' &&
          vigente.esAdministrador === true;
        this.estado.set(confirmada ? 'exito' : 'sin_permiso');
      },
      complete: () => {
        // Un Observable que finaliza sin emision no confirma autoridad.
        if (this.estado() === 'cargando') this.estado.set('error');
      },
      error: (error: unknown) => {
        const status = error instanceof HttpErrorResponse ? error.status : 0;
        this.estado.set(status === 401 || status === 403 ? 'sin_permiso' : 'error');
      }
    });
  }
}
