import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, computed, inject, signal, viewChild } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

/** C03-04.M04.01: guía de incidentes privada, sin consultar ni operar restauraciones. */
@Component({
  selector: 'app-respuesta-incidentes-v5',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="incident-guide" aria-labelledby="incident-title">
      <a class="skip-link" href="#incident-title" (click)="irAlContenido($event)">Ir al contenido de incidentes</a>
      <header>
        <p class="eyebrow">SOLQARYN · Respuesta a incidentes</p>
        <h1 #incidentTitle id="incident-title" tabindex="-1">Respuesta, revocación y recuperación segura</h1>
        <p>Guía de comprobación futura, sin incidentes ni respaldos conectados.</p>
        <a routerLink="/plataforma" aria-label="Regresar a plataforma">Regresar a plataforma</a>
      </header>
      @if (estadoVista() === 'cargando') {
        <section class="notice" role="status" aria-live="polite">Comprobando la sesión de plataforma. La guía permanece oculta durante la verificación.</section>
      } @else if (estadoVista() === 'error') {
        <p class="notice" role="alert">No se pudo comprobar la sesión. La guía permanece oculta; vuelve a entrar para reintentar.</p>
      } @else if (autorizado()) {
        <section class="notice" aria-label="Estado de la verificación">
          <p role="status" aria-live="polite">
            @if (estadoVista() === 'exito') {
              Sesión administradora confirmada por la API; ninguna operación de respuesta a incidentes fue ejecutada.
            } @else {
              Guía disponible con la sesión comprobada en la navegación protegida.
            }
          </p>
          <button type="button" (click)="revalidarSesion()">Revalidar sesión de plataforma</button>
        </section>
        <section class="notice" aria-label="Estado vacío del registro">
          <h2>Sin incidentes conectados</h2>
          <p>Esta guía no consume un feed de incidentes ni consulta respaldos. Sin registros confirmados no se muestran eventos inventados.</p>
        </section>
        <section aria-label="Límite de autoridad" class="notice">
          <strong>Solo lectura · ninguna restauración ni revocación ejecutada</strong>
          <p>Requiere evidencia de identidad, alcance, pruebas aisladas y aprobación antes de operar.</p>
        </section>
        <section aria-labelledby="incident-checks">
          <h2 id="incident-checks">Cuatro decisiones de respuesta</h2>
          <ol class="incident-review">
            <li><h3>Contención y pertenencia</h3><p>Confirmar actor, tenant, sucursal, propiedad y revocación. Denegar acceso cruzado antes de cualquier acción.</p></li>
            <li><h3>Integridad y alcance</h3><p>Correlacionar incidentes, versiones, SHA, dependencias y trazas sin publicar secretos ni identificadores privados.</p></li>
            <li><h3>Respaldo y ensayo</h3><p>Exigir snapshot válido y restauración probada en entorno aislado; no invocar proveedores ni bases de datos desde esta página.</p></li>
            <li><h3>Reversión y revisión</h3><p>Conservar bitácora inmutable, criterios de rollback y revisión independiente antes de cerrar un incidente.</p></li>
          </ol>
        </section>
        <p role="note">Evidencia real de incidentes, recuperación, tenant A/B y RTO/RPO: NO_MEDIDO. Sin autorización QA/main/PROD.</p>
      } @else {
        <p role="alert" class="notice">Sesión administradora de plataforma no confirmada. Guía protegida oculta.</p>
      }
    </main>
  `,
  styles: [`
    :host { display:block; color:var(--mat-sys-on-surface, #17212d); }
    .incident-guide { box-sizing:border-box; max-width:70rem; min-width:0; margin-inline:auto; padding:clamp(1rem,3vw,2.5rem); background:var(--mat-sys-surface, #fff); color:var(--mat-sys-on-surface, #17212d); }
    .skip-link { position:fixed; left:1rem; top:.5rem; z-index:1000; padding:.75rem 1rem; border:2px solid currentColor; border-radius:.4rem; background:var(--mat-sys-surface, #fff); color:var(--mat-sys-on-surface, #17212d); transform:translateY(-200%); }
    .skip-link:focus-visible { transform:none; outline:3px solid currentColor; outline-offset:3px; }
    #incident-title:focus-visible { outline:3px solid currentColor; outline-offset:4px; }
    h1 { font-size:clamp(1.6rem,4vw,2.3rem); line-height:1.2; }
    h2 { font-size:1.3rem; margin-top:1.5rem; }
    h3 { font-size:1rem; margin:0 0 .4rem; }
    p { line-height:1.55; overflow-wrap:anywhere; }
    .eyebrow { color:var(--mat-sys-primary, #2157a3); font-weight:700; }
    header a { display:inline-flex; box-sizing:border-box; min-height:44px; align-items:center; border:1px solid currentColor; border-radius:.55rem; padding:.65rem .85rem; color:inherit; }
    header a:focus-visible { outline:3px solid var(--mat-sys-primary, #2157a3); outline-offset:3px; }
    button { min-height:44px; padding:.65rem .9rem; border:1px solid currentColor; border-radius:.55rem; color:inherit; background:var(--mat-sys-surface, #fff); cursor:pointer; }
    button:focus-visible { outline:3px solid var(--mat-sys-primary, #2157a3); outline-offset:3px; }
    .notice { border:1px solid var(--mat-sys-outline-variant, #aeb8c1); border-radius:.7rem; padding:1rem; margin-top:1rem; }
    .incident-review { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); list-style:none; gap:.8rem; padding:0; }
    .incident-review li { min-width:0; overflow-wrap:anywhere; border:1px solid var(--mat-sys-outline-variant, #aeb8c1); border-radius:.7rem; padding:.9rem; }
    @media(max-width:45rem) { .incident-review { grid-template-columns:minmax(0,1fr); } }
    @media(forced-colors: active) {
      .incident-guide { background:Canvas; color:CanvasText; }
      .skip-link { background:Canvas; color:CanvasText; border-color:CanvasText; }
      .notice, .incident-review li, header a { border-color:CanvasText; }
    }
    @media(prefers-contrast: more) { .notice, .incident-review li, header a { border-width:2px; } }
  `]
})
export class RespuestaIncidentesV5Component {
  private readonly plataforma = inject(PlataformaSesionService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly titulo = viewChild.required<ElementRef<HTMLHeadingElement>>('incidentTitle');

  /** El <base href="/"> del SPA no debe enviar el salto a /#incident-title. */
  irAlContenido(event: MouseEvent): void {
    event.preventDefault();
    this.titulo().nativeElement.focus();
  }
  /** Solo la sesión backend vigente es fuente del grant. No confiar en JWT local ni EmpresaId. */
  readonly autorizado = computed(() => {
    const sesion = this.plataforma.sesion();
    return sesion?.ambito === 'Plataforma' && sesion.esAdministrador === true;
  });
  /** C03-04.M04.02: estado de comprobación de sesión, nunca incidentes inventados. */
  readonly verificacion = signal<'inicial' | 'cargando' | 'exito' | 'error' | 'sin_permiso'>('inicial');
  readonly estadoVista = computed(() => {
    const actual = this.verificacion();
    if (actual === 'cargando' || actual === 'error' || actual === 'sin_permiso') return actual;
    return this.autorizado() ? (actual === 'exito' ? 'exito' : 'sin_datos') : 'sin_permiso';
  });

  /** Solo GET de sesión existente, sin restore, revocación ni jobs. */
  revalidarSesion(): void {
    if (!this.autorizado() || this.verificacion() === 'cargando') return;
    this.verificacion.set('cargando');
    this.plataforma.obtener().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.verificacion.set(this.autorizado() ? 'exito' : 'sin_permiso'),
      error: (error: unknown) => {
        const denegado = error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403);
        this.verificacion.set(denegado ? 'sin_permiso' : 'error');
      }
    });
  }

}
