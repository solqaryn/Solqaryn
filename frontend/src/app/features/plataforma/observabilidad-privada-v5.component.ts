import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { defer } from 'rxjs';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

export type EstadoObservabilidadV5 = 'vacio' | 'cargando' | 'exito' | 'sin_permiso' | 'error';
/** C11-04.M04.02: revalidación de sesión, sin consultas de métricas ni exporter. */
@Component({
  selector: 'app-observabilidad-privada-v5',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="observability-guide" aria-labelledby="obs-title">
      <header>
        <p class="eyebrow">SOLQARYN · Observabilidad privada</p>
        <h1 id="obs-title">Diagnóstico y señales seguras</h1>
        <p>Esta guía describe requisitos de evidencia; no lee métricas ni inicia un worker.</p>
        <nav class="obs-navigation" aria-label="Acciones de observabilidad">
          <a routerLink="/plataforma" aria-label="Regresar a plataforma">Volver a plataforma</a>
          <button type="button" aria-label="Revalidar acceso de observabilidad"
            aria-describedby="obs-status" (click)="revalidar()"
            [disabled]="estado() === 'cargando'">Revalidar sesión</button>
        </nav>
      </header>
      <section id="obs-status" class="notice" aria-label="Estado de consulta" aria-live="polite" aria-atomic="true"
        [attr.aria-busy]="estado() === 'cargando'">
        @switch (estadoVisible()) {
          @case ('cargando') { <p role="status">Revalidando autorización con el servidor…</p> }
          @case ('sin_permiso') { <p role="alert">Sesión de plataforma no confirmada. Observabilidad privada oculta.</p> }
          @case ('error') { <p role="alert">No se pudo revalidar la sesión. Observabilidad privada oculta.</p> }
          @case ('vacio') { <p role="status">Sesión verificada. No hay métricas ni diagnósticos en vivo conectados.</p> }
          @case ('exito') { <p role="status">Sesión revalidada. Información solo de lectura, sin datos operativos.</p> }
        }
      </section>
      @if (verificado()) {
        <section class="obs-branding" [class.obs-branding--neutral]="modoIdentidad() !== 'institucional'"
          aria-label="Identidad visual de observabilidad">
          <span class="obs-branding-mark" aria-hidden="true">{{ modoIdentidad() === 'institucional' ? 'S' : '·' }}</span>
          <div><strong>{{ identidadVisual().nombre }}</strong><p>{{ identidadVisual().descripcion }}</p></div>
        </section>
        <section class="notice" aria-label="Límites de medición">
          <strong>Solo lectura · sin SLO ni costos verificados</strong>
          <p>Probar las fuentes reales en un entorno aislado antes de publicar alertas o atribuir cumplimiento.</p>
        </section>
        <section aria-labelledby="obs-checks">
          <h2 id="obs-checks">Cuatro frentes de observabilidad</h2>
          <ol class="obs-points">
            <li><h3>Logs y confidencialidad</h3><p>Permitir sólo códigos estructurados, sin JWT, actor, tenant ni rutas privadas.</p></li>
            <li><h3>Métricas y costos</h3><p>Validar etiquetas de baja cardinalidad, presupuesto y deduplicación antes del exporter.</p></li>
            <li><h3>Trazas y latencia</h3><p>Conservar contexto de autoridad servidor, límites, correlación segura y muestreo acotado.</p></li>
            <li><h3>Alertas y diagnóstico</h3><p>Comprobar revocación, errores, reintentos, falso positivo y rollback sin divulgar secretos.</p></li>
          </ol>
        </section>
        <p role="note">SLO, RTO/RPO, costos, latencia y proveedor de telemetría: NO_MEDIDO. Sin autorizaciones QA/main/PROD.</p>
      }
    </section>
  `,
  styles: [`
    :host { display:block; color:var(--mat-sys-on-surface, #17212d); }
    .observability-guide { box-sizing:border-box; max-width:70rem; margin:auto; padding:clamp(1rem,3vw,2.5rem); }
    h1 { line-height:1.2; font-size:clamp(1.6rem,4vw,2.3rem); }
    h2 { font-size:1.3rem; margin-top:1.5rem; }
    h3 { font-size:1rem; margin:0 0 .35rem; }
    p { line-height:1.55; overflow-wrap:anywhere; }
    .eyebrow { font-weight:700; color:var(--mat-sys-primary, #2157a3); }
    .obs-navigation { display:flex; flex-wrap:wrap; align-items:center; gap:.55rem; }
    header :is(a,button) { display:inline-block; box-sizing:border-box; max-width:100%; min-height:44px; overflow-wrap:anywhere; margin-right:.55rem; margin-bottom:.4rem; border:1px solid currentColor; border-radius:.55rem; padding:.7rem .9rem; color:inherit; font:inherit; background:transparent; text-decoration:underline; cursor:pointer; }
    header button[disabled] { opacity:.55; cursor:progress; }
    header :is(a,button):focus-visible { outline:3px solid var(--mat-sys-primary, #2157a3); outline-offset:3px; }
    .notice { border:1px solid var(--mat-sys-outline-variant, #aeb8c1); border-radius:.7rem; padding:.9rem; margin:1rem 0; }
    .obs-branding { display:flex; flex-wrap:wrap; align-items:center; gap:.85rem; padding:.85rem 1rem; border:1px solid var(--mat-sys-outline-variant, #aeb8c1); border-radius:.7rem; background:var(--mat-sys-surface-container-low, #f5f7fa); }
    .obs-branding-mark { flex:none; display:grid; place-items:center; width:2.4rem; height:2.4rem; border-radius:.55rem; background:var(--mat-sys-primary, #2157a3); color:var(--mat-sys-on-primary, #fff); font-weight:800; }
    .obs-branding--neutral .obs-branding-mark { background:var(--mat-sys-outline, #66727e); color:#fff; }
    .obs-branding p { margin:.1rem 0 0; }
    .obs-points { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); list-style:none; padding:0; gap:.85rem; }
    .obs-points li { border:1px solid var(--mat-sys-outline-variant, #aeb8c1); border-radius:.7rem; min-width:0; padding:clamp(.8rem,1.6vw,1rem); content-visibility:auto; contain-intrinsic-size:auto 10rem; }
    @media(max-width:45rem) { .obs-points { grid-template-columns:1fr; } }
  `]
})
export class ObservabilidadPrivadaV5Component {
  /** C11-04.M04.03: solo identidad visual local tipada; nunca fuente de autorización. */
  readonly modoIdentidad = input<'institucional' | 'neutra'>('institucional');
  readonly identidadVisual = computed(() => this.modoIdentidad() === 'institucional'
    ? { nombre: 'SOLQARYN · Observabilidad', descripcion: 'Identidad institucional · guía de lectura sin métricas conectadas' }
    : { nombre: 'Identidad visual neutra', descripcion: 'Sin marca empresarial ni autoridad sobre datos o exporter' });
  private readonly plataforma = inject(PlataformaSesionService);
  private readonly destroyRef = inject(DestroyRef);
  readonly estado = signal<EstadoObservabilidadV5>(this.plataforma.sesion() ? 'vacio' : 'sin_permiso');
  /** La sesion confirmada por backend, nunca un texto de navegador, habilita la vista. */
  readonly verificado = computed(() => {
    const sesion = this.plataforma.sesion();
    const estado = this.estado();
    return (estado === 'vacio' || estado === 'exito') &&
      sesion?.ambito === 'Plataforma' && sesion.esAdministrador === true;
  });
  readonly estadoVisible = computed<EstadoObservabilidadV5>(() => {
    if (this.estado() === 'cargando') return 'cargando';
    if (this.estado() === 'error') return 'error';
    return this.verificado() ? this.estado() : 'sin_permiso';
  });

  revalidar(): void {
    if (this.estado() === 'cargando') return; // No duplicar consultas ni permitir doble accion.
    this.estado.set('cargando'); // Oculta el contenido antes de cualquier I/O.
    defer(() => this.plataforma.obtener()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (respuesta) => {
        const actual = this.plataforma.sesion();
        const autorizado = respuesta?.ambito === 'Plataforma' &&
          respuesta.esAdministrador === true &&
          actual?.ambito === 'Plataforma' && actual.esAdministrador === true;
        this.estado.set(autorizado ? 'exito' : 'sin_permiso');
      },
      complete: () => {
        if (this.estado() === 'cargando') this.estado.set('error'); // Observable vacio NO es PASS.
      },
      error: (error: unknown) => {
        const codigo = error instanceof HttpErrorResponse ? error.status : 0;
        this.estado.set(codigo === 401 || codigo === 403 ? 'sin_permiso' : 'error');
        // Nunca incluir la respuesta o el mensaje del servidor en la UI.
      }
    });
  }
}
