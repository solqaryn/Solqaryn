import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { take } from 'rxjs';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

/** Contractual checkpoints, not results from MySQL, Aiven or a cutover provider. */
const CONTROLES = Object.freeze([
  Object.freeze({ id: 'exportacion', nombre: 'Exportación aislada', evidencia: 'SIN_VERIFICAR' }),
  Object.freeze({ id: 'restauracion', nombre: 'Restauración de ensayo', evidencia: 'SIN_VERIFICAR' }),
  Object.freeze({ id: 'comparacion', nombre: 'Checksums y conteos', evidencia: 'NO_MEDIDO' }),
  Object.freeze({ id: 'autorizacion', nombre: 'Ventana, rollback y aprobación explícita', evidencia: 'NO_AUTORIZADO' })
] as const);

const TEMAS_PERMITIDOS = Object.freeze([
  { id: 'institucional', etiqueta: 'Institucional' },
  { id: 'alto-contraste', etiqueta: 'Alto contraste' }
] as const);
type TemaPermitido = typeof TEMAS_PERMITIDOS[number]['id'];

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
    <main class="cutover" aria-labelledby="cutover-title"
      [class.contraste-alto]="puedeVer() && tema() === 'alto-contraste'">
      <header>
        <p class="eyebrow">SOLQARYN · Preparación MySQL privado</p>
        <h1 id="cutover-title">Controles de preparación para cutover</h1>
        <p>Referencia de lectura protegida; no muestra el estado de bases de datos reales.</p>
        <a routerLink="/plataforma" aria-label="Volver a plataforma">Volver a plataforma</a>
      </header>
      <section class="panel" data-testid="c1204-m0402-panel" aria-labelledby="c1204-session-title"
        [attr.aria-busy]="estadoVisible() === 'cargando'">
        <h2 id="c1204-session-title">Estado de comprobación de sesión</h2>
        @switch (estadoVisible()) {
          @case ('cargando') {
            <p role="status" data-testid="c1204-m0402-state">Cargando revalidación de sesión de plataforma…</p>
          }
          @case ('vacio') {
            <p role="status" data-testid="c1204-m0402-state">Vacío: sin evidencia de bases de datos conectadas ni restauraciones verificadas.</p>
          }
          @case ('exito') {
            <p role="status" data-testid="c1204-m0402-state">Éxito: sesión global revalidada; cutover sigue SIN_VERIFICAR.</p>
          }
          @case ('error') {
            <p role="alert" data-testid="c1204-m0402-state">Error al revalidar la sesión. No se muestran diagnósticos privados.</p>
          }
          @case ('sin_permiso') {
            <p role="alert" data-testid="c1204-m0402-state">Sin permiso de plataforma confirmado.</p>
          }
        }
        @if (puedeVer()) {
          <button type="button" data-testid="c1204-m0402-revalidate" (click)="revalidar()">
            Revalidar sesión de plataforma
          </button>
        }
        <p role="note">El estado de sesión no acredita exportación, restore, checksums ni autorización operativa.</p>
      </section>
      @if (puedeVer()) {
        <section class="panel" data-testid="c1204-m0403-branding" aria-labelledby="c1204-branding-title">
          <h2 id="c1204-branding-title">Presentación permitida</h2>
          <p>Los temas cambian solo la lectura local. No alteran permisos ni estado de MySQL.</p>
          <div class="tema-opciones" role="group" aria-label="Tema visual permitido">
            @for (opcion of temasPermitidos; track opcion.id) {
              <button type="button" [attr.data-testid]="'c1204-m0403-theme-' + opcion.id"
                [attr.aria-pressed]="tema() === opcion.id" (click)="seleccionarTema(opcion.id)">
                {{ opcion.etiqueta }}
              </button>
            }
          </div>
        </section>
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
    a:focus-visible, button:focus-visible { outline:3px solid var(--mat-sys-primary,#2157a3); outline-offset:3px; }
    button { min-height:44px; padding:.7rem 1rem; border:1px solid currentColor; border-radius:.55rem; cursor:pointer; background:transparent; color:inherit; }
    .aviso, .panel, .denegado { border:1px solid var(--mat-sys-outline-variant,#aeb8c1); border-radius:.75rem; padding:1rem; margin-top:1rem; }
    .aviso { background:var(--mat-sys-surface-container-low,#f5f7fa); }
    .tema-opciones { display:flex; flex-wrap:wrap; gap:.75rem; }
    .tema-opciones button[aria-pressed="true"] { font-weight:700; border-width:2px; }
    .cutover.contraste-alto { color:#fff; background:#101927; }
    .cutover.contraste-alto .panel, .cutover.contraste-alto .aviso,
    .cutover.contraste-alto .controles li { color:#fff; background:#101927; border-color:#fff; }
    .cutover.contraste-alto button, .cutover.contraste-alto a { color:#fff; border-color:#fff; }
    .cutover.contraste-alto :is(a,button):focus-visible { outline-color:#ffdf5f; }
    .controles { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:1rem; padding:0; list-style:none; }
    .controles li { min-width:0; border:1px solid var(--mat-sys-outline-variant,#aeb8c1); border-radius:.65rem; padding:1rem; }
    @media (max-width:45rem) { .controles { grid-template-columns:1fr; } }
    @media (prefers-contrast:more) { a:focus-visible { outline-color:CanvasText; outline-width:4px; } }
  `]
})
export class C1204CutoverReadonlyComponent {
  private readonly plataforma = inject(PlataformaSesionService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly estadoSolicitud = signal<'vacio' | 'cargando' | 'exito' | 'error' | 'sin_permiso'>('vacio');
  private revalidando = false;
  readonly controles = CONTROLES;
  readonly temasPermitidos = TEMAS_PERMITIDOS;
  readonly tema = signal<TemaPermitido>('institucional');

  /** Allowlist local estricta: no acepta URLs, CSS, objetos ni valores externos. */
  seleccionarTema(value: unknown): boolean {
    if (!this.puedeVer() || (value !== 'institucional' && value !== 'alto-contraste')) return false;
    this.tema.set(value);
    return true;
  }
  readonly autorizado = computed(() => {
    const sesion = this.plataforma.sesion();
    return sesion?.ambito === 'Plataforma' && sesion.esAdministrador === true;
  });
  readonly estadoVisible = computed(() => {
    const estado = this.estadoSolicitud();
    if (estado === 'cargando' || estado === 'error') return estado;
    return this.autorizado() ? estado : 'sin_permiso';
  });
  readonly puedeVer = computed(() => this.autorizado() &&
    (this.estadoSolicitud() === 'vacio' || this.estadoSolicitud() === 'exito'));

  /** GET de sesión global, nunca comando de MySQL, backup ni migración. */
  revalidar(): void {
    if (this.revalidando || !this.autorizado()) return;
    this.revalidando = true;
    this.estadoSolicitud.set('cargando');
    this.plataforma.obtener().pipe(take(1), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: sesion => {
        if (sesion?.ambito !== 'Plataforma' || sesion?.esAdministrador !== true) {
          this.plataforma.limpiar();
          this.estadoSolicitud.set('sin_permiso');
        } else {
          this.estadoSolicitud.set('exito');
        }
      },
      error: (error: unknown) => {
        const status = error instanceof HttpErrorResponse ? error.status : 0;
        this.estadoSolicitud.set(status === 401 || status === 403 ? 'sin_permiso' : 'error');
        this.revalidando = false;
      },
      complete: () => {
        if (this.estadoSolicitud() === 'cargando') this.estadoSolicitud.set('error');
        this.revalidando = false;
      }
    });
  }
}
