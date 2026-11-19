import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { defer } from 'rxjs';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

/** C14-02.M04.01: guía privada de amenazas; nunca concede permisos ni ejecuta pentest. */
@Component({
  selector: 'app-revision-amenazas-v5',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="threat-guide" [class.visual-neutral]="temaPresentacion() === 'neutro'" aria-labelledby="threat-title">
      <header>
        <p class="eyebrow">SOLQARYN · Evaluación de amenazas</p>
        <h1 id="threat-title">Verificaciones de acceso y abuso</h1>
        <p>Guía privada sin peticiones a registros, proveedores ni datos productivos.</p>
        <a routerLink="/plataforma" aria-label="Volver a administración de plataforma">Volver a plataforma</a>
      </header>
      @if (estadoVista() === 'cargando') {
        <section role="status" aria-live="polite" class="notice">Comprobando nuevamente la sesión; la guía de amenazas permanece oculta.</section>
      } @else if (estadoVista() === 'error') {
        <p role="alert" class="notice">No se pudo comprobar la sesión. La guía sigue oculta para proteger la información.</p>
      } @else if (estadoVista() === 'sin_datos' || estadoVista() === 'exito') {
        <section class="notice" aria-label="Estado de acceso">
          <p role="status" aria-live="polite">
            @if (estadoVista() === 'exito') {
              Sesión de plataforma confirmada; ninguna revisión de amenazas fue ejecutada.
            } @else {
              Sesión autorizada para consultar la guía.
            }
          </p>
          <button type="button" (click)="revalidarSesion()">Revalidar sesión de plataforma</button>
        </section>
        <section class="notice" aria-label="Presentación visual local">
          <h2 id="threat-presentation-title">Presentación de seguridad</h2>
          <p>Preferencia local autorizada: institucional o neutra. No modifica sesión, permisos, marca del tenant ni políticas.</p>
          <div class="visual-options" role="group" aria-labelledby="threat-presentation-title">
            <button type="button" [attr.aria-pressed]="temaPresentacion() === 'institucional'" (click)="cambiarTemaPresentacion('institucional')">Presentación institucional</button>
            <button type="button" [attr.aria-pressed]="temaPresentacion() === 'neutro'" (click)="cambiarTemaPresentacion('neutro')">Presentación neutra</button>
          </div>
        </section>
        <section class="notice" aria-label="Estado vacío de amenazas">
          <h2>Sin hallazgos conectados</h2>
          <p>Esta guía no consulta un registro de amenazas ni ejecuta pruebas de penetración. No se presumen hallazgos inexistentes.</p>
        </section>
        <section class="notice" aria-label="Límites de ejecución">
          <strong>Solo consulta · no realiza pentest ni modifica políticas</strong>
          <p>Las mitigaciones requieren revisión y pruebas reales del backend.</p>
        </section>
        <section aria-labelledby="amenazas-title">
          <h2 id="amenazas-title">Cuatro superficies críticas a comprobar</h2>
          <ol class="threat-list">
            <li><h3>BOLA/IDOR y pertenencia</h3><p>Rechazar identificadores de otro tenant, sucursal, dueño o rol, aun si son válidos sintácticamente.</p></li>
            <li><h3>Suplantación y sesión</h3><p>Validar identidad servidor, MFA, revocación, reenvíos y denegaciones ante tokens obsoletos.</p></li>
            <li><h3>Archivos, jobs y caché</h3><p>Aislar rutas, colas, recibos, firmas, claves y errores sin enumeración ni filtración.</p></li>
            <li><h3>Host, backup y recuperación</h3><p>Exigir validación del host, precondiciones de restore, rollback y ensayos aislados antes de actuar.</p></li>
          </ol>
        </section>
        <p role="note">Pruebas HTTP JWT, tenant A/B y pentest en vivo: NO_MEDIDO. QA, main y PROD no autorizados.</p>
      } @else {
        <p class="notice" role="alert">Acceso de plataforma no confirmado. Guía privada oculta.</p>
      }
    </main>
  `,
  styles: [`
    :host { display:block; color:var(--mat-sys-on-surface, #17212d); }
    .threat-guide { box-sizing:border-box; max-width:70rem; margin:auto; padding:clamp(1rem,3vw,2.5rem); }
    h1 { line-height:1.2; font-size:clamp(1.6rem,4vw,2.35rem); }
    h2 { font-size:1.3rem; margin-top:1.5rem; }
    h3 { font-size:1rem; margin:0 0 .35rem; }
    p { line-height:1.55; overflow-wrap:anywhere; }
    .eyebrow { color:var(--mat-sys-primary, #2157a3); font-weight:700; }
    header a { display:inline-block; border:1px solid currentColor; border-radius:.55rem; padding:.7rem .85rem; color:inherit; text-decoration:underline; }
    header a:focus-visible { outline:3px solid var(--mat-sys-primary, #2157a3); outline-offset:3px; }
    button { min-height:44px; padding:.7rem .85rem; border:1px solid currentColor; border-radius:.55rem; color:inherit; background:var(--mat-sys-surface, #fff); cursor:pointer; }
    button:focus-visible { outline:3px solid var(--mat-sys-primary, #2157a3); outline-offset:3px; }
    .notice { border:1px solid var(--mat-sys-outline-variant, #aeb8c1); border-radius:.7rem; padding:1rem; margin:1rem 0; }
    .threat-guide.visual-neutral { background:#f3f6f8; color:#25313d; }
    .threat-guide.visual-neutral .eyebrow { color:#2c4b69; }
    .visual-options { display:flex; flex-wrap:wrap; gap:.6rem; }
    .visual-options button[aria-pressed="true"] { font-weight:700; box-shadow:inset 0 0 0 1px currentColor; }
    .threat-list { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:.85rem; padding:0; list-style:none; }
    .threat-list li { min-width:0; border:1px solid var(--mat-sys-outline-variant, #aeb8c1); border-radius:.7rem; padding:.9rem; }
    @media(max-width:45rem) { .threat-list { grid-template-columns:1fr; } }
  `]
})
export class RevisionAmenazasV5Component {
  private readonly plataforma = inject(PlataformaSesionService);
  private readonly destroyRef = inject(DestroyRef);
  readonly accesoVerificado = computed(() => {
    const s = this.plataforma.sesion();
    return s?.ambito === 'Plataforma' && s.esAdministrador === true;
  });
  /** C14-02.M04.02: estado de autorización a la guía, nunca resultado de pentest. */
  readonly verificacion = signal<'inicial' | 'cargando' | 'exito' | 'error' | 'sin_permiso'>('inicial');
  readonly estadoVista = computed(() => {
    const actual = this.verificacion();
    if (actual === 'cargando' || actual === 'error' || actual === 'sin_permiso') return actual;
    return this.accesoVerificado() ? (actual === 'exito' ? 'exito' : 'sin_datos') : 'sin_permiso';
  });
  /** C14-02.M04.03: dos apariencias locales permitidas, sin inyección CSS ni configuración remota. */
  readonly temaPresentacion = signal<'institucional' | 'neutro'>('institucional');
  cambiarTemaPresentacion(valor: string): void {
    if (!this.accesoVerificado() || (valor !== 'institucional' && valor !== 'neutro')) {
      this.temaPresentacion.set('institucional');
      return;
    }
    this.temaPresentacion.set(valor);
  }
  /** GET revalidación existente, single-flight, sin alterar grants ni política de backend. */
  revalidarSesion(): void {
    if (!this.accesoVerificado() || this.verificacion() === 'cargando') return;
    this.verificacion.set('cargando');
    defer(() => this.plataforma.obtener()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.verificacion.set(this.accesoVerificado() ? 'exito' : 'sin_permiso'),
      error: (error: unknown) => {
        const denied = error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403);
        this.verificacion.set(denied ? 'sin_permiso' : 'error');
      }
    });
  }

}
