import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { defer } from 'rxjs';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

/**
 * C02-04.M04.01: superficie privada, informativa y fail-closed del control plane.
 * No concede grants, no simula MFA y no invoca operaciones empresariales.
 * La ruta y cada revalidación consultan la autoridad real en el servidor.
 */
@Component({
  selector: 'app-consola-global-auditada-v5',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="control-plane" aria-labelledby="control-plane-title">
      <header>
        <p class="eyebrow">SOLQARYN · Plataforma</p>
        <h1 id="control-plane-title">Consola global auditada</h1>
        <p>Consulta privada de controles del plano global, separada de la administración ERP.</p>
        <nav aria-label="Navegación de consola global">
          <a routerLink="/plataforma" aria-label="Volver a administración de plataforma">Volver a plataforma</a>
          @if (accesoVerificado() || verificacion() === 'error' || verificacion() === 'cargando') {
            <button type="button" (click)="revalidarSesion()"
              [disabled]="estadoVisible() === 'cargando'"
              aria-describedby="control-plane-estado">{{ verificacion() === 'error' ? 'Reintentar verificación' : 'Revalidar sesión global' }}</button>
          }
          <a routerLink="/plataforma/inventario-rutas-v19" aria-label="Consultar inventario global de programas y rutas">Inventario de módulos y rutas</a>
        </nav>
      </header>

      <section id="control-plane-estado" class="notice" aria-live="polite" aria-atomic="true"
        [attr.aria-busy]="estadoVisible() === 'cargando'">
        @switch (estadoVisible()) {
          @case ('cargando') { <p role="status">Revalidando autorización global con el servidor…</p> }
          @case ('sin_permiso') { <p role="alert">Permiso global no confirmado. La consola privada permanece oculta.</p> }
          @case ('error') { <p role="alert">No se pudo verificar la sesión. La consola privada permanece oculta.</p> }
          @case ('vacio') { <p role="status">Sesión global confirmada. No hay operaciones excepcionales conectadas.</p> }
          @case ('exito') { <p role="status">Sesión global revalidada. Solo consulta de controles, sin ejecutar acciones.</p> }
        }
      </section>

      @if (estadoVisible() === 'vacio' || estadoVisible() === 'exito') {
        <section class="control-brand" role="group" aria-label="Marca visual autorizada de consola global"
          data-testid="c0204-branding" [class.control-brand--contrast]="marcaConsola() === 'contraste'">
          <h2>Presentación visual permitida</h2>
          <p>Opciones locales de apariencia. Ninguna selección concede permisos ni modifica una empresa.</p>
          <div class="control-brand__actions">
            @for (opcion of marcasPermitidas; track opcion.id) {
              <button type="button" [attr.data-testid]="'c0204-brand-' + opcion.id"
                [attr.aria-pressed]="marcaConsola() === opcion.id"
                (click)="seleccionarMarcaConsola(opcion.id)">{{ opcion.nombre }}</button>
            }
          </div>
          <p role="status" data-testid="c0204-brand-state">{{ marcaConsola() === 'contraste' ? 'Contraste alto seleccionado' : 'Institucional seleccionado' }}</p>
        </section>
        <section aria-labelledby="controles-title">
          <h2 id="controles-title">Fronteras de control plane</h2>
          <ol class="control-cards">
            <li><h3>Separación plataforma / ERP</h3><p>La identidad global no hereda permisos de una empresa. El servidor debe autorizar cada operación.</p></li>
            <li><h3>MFA y reautenticación</h3><p>Las acciones críticas exigirán desafío fresco verificado en backend. Esta pantalla no ejecuta MFA.</p></li>
            <li><h3>Justificación y auditoría</h3><p>Los cambios excepcionales requieren motivo validado, alcance mínimo y evidencia durable antes de habilitarse.</p></li>
            <li><h3>Excepción temporal</h3><p>Grants de plazo limitado, revocación y denegación entre tenants deberán probarse en infraestructura aislada.</p></li>
          </ol>
        </section>
        <section class="notice" aria-label="Operaciones no habilitadas">
          <strong>Solo lectura · ninguna concesión de acceso</strong>
          <p>No se exponen usuarios, tenants, tokens, secretos, grants ni botones de escalamiento.</p>
          <p role="note">MFA, tenant A/B, JWT HTTP y auditoría durable: NO_MEDIDO. Sin acceso a QA, main o producción.</p>
        </section>
      }
    </section>
  `,
  styles: [`
    :host { display:block; color:var(--mat-sys-on-surface, #17212d); }
    .control-plane { box-sizing:border-box; max-width:72rem; padding:clamp(1rem,3vw,2.5rem); margin:0 auto; }
    h1 { font-size:clamp(1.6rem,4vw,2.4rem); line-height:1.2; }
    h2 { font-size:1.3rem; margin:1.5rem 0 .8rem; }
    h3 { margin:0 0 .4rem; font-size:1rem; }
    p { line-height:1.55; overflow-wrap:anywhere; }
    .eyebrow { color:var(--mat-sys-primary,#2157a3); font-weight:700; }
    nav { display:flex; gap:.7rem; flex-wrap:wrap; align-items:center; }
    nav :is(a,button) { display:inline-block; box-sizing:border-box; min-height:44px; padding:.7rem .9rem;
      border:1px solid currentColor; border-radius:.55rem; background:transparent; color:inherit;
      font:inherit; text-decoration:underline; cursor:pointer; }
    nav button:disabled { opacity:.6; cursor:progress; }
    nav :is(a,button):focus-visible { outline:3px solid var(--mat-sys-primary,#2157a3); outline-offset:3px; }
    .notice { border:1px solid var(--mat-sys-outline-variant,#aeb8c1); border-radius:.65rem; padding:1rem; margin:1.1rem 0; }
    .control-brand { box-sizing:border-box; margin:1.25rem 0; padding:1rem; border:1px solid #8b9eb0; border-radius:.75rem; background:#f4f7fb; color:#18324d; overflow-wrap:anywhere; }
    .control-brand h2 { margin:.25rem 0 .65rem; }
    .control-brand__actions { display:flex; flex-wrap:wrap; gap:.75rem; margin:1rem 0; }
    .control-brand__actions button { min-height:44px; padding:.65rem 1rem; border:1px solid currentColor; border-radius:.5rem; background:transparent; color:inherit; font:inherit; cursor:pointer; }
    .control-brand__actions button:focus-visible { outline:3px solid currentColor; outline-offset:3px; }
    .control-brand--contrast { background:#0b1829; color:#fff; border-color:#b8e2ff; }
    .control-brand--contrast .control-brand__actions button { border-color:#fff; color:#fff; }
    @media(max-width:375px) { .control-brand__actions { flex-direction:column; align-items:stretch; } }
    .control-cards { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:.9rem;
      list-style:none; padding:0; margin:0; }
    .control-cards li { box-sizing:border-box; min-width:0; border:1px solid var(--mat-sys-outline-variant,#aeb8c1);
      border-radius:.6rem; padding:1rem; background:var(--mat-sys-surface-container-low,#f5f7fa); }
    @media (max-width:44rem) { .control-cards { grid-template-columns:1fr; } }
  `]
})
export class ConsolaGlobalAuditadaV5Component {
  private readonly plataforma = inject(PlataformaSesionService);
  private readonly destroyRef = inject(DestroyRef);
  readonly accesoVerificado = computed(() => {
    const sesion = this.plataforma.sesion();
    return sesion?.ambito === 'Plataforma' && sesion.esAdministrador === true;
  });
  readonly verificacion = signal<'inicial' | 'cargando' | 'confirmada' | 'denegada' | 'error'>('inicial');
  // C02-04.M04.03: sólo tokens locales allowlisted, no CSS remoto, grants ni estado persistido.
  readonly marcasPermitidas = [
    { id: 'institucional', nombre: 'Institucional' },
    { id: 'contraste', nombre: 'Contraste alto' }
  ] as const;
  readonly marcaConsola = signal<'institucional' | 'contraste'>('institucional');

  seleccionarMarcaConsola(id: string): boolean {
    const estado = this.estadoVisible();
    if (!this.accesoVerificado() || (estado !== 'vacio' && estado !== 'exito')) return false;
    const opcion = this.marcasPermitidas.find(item => item.id === id);
    if (!opcion || this.marcaConsola() === opcion.id) return false;
    this.marcaConsola.set(opcion.id);
    return true;
  }
  readonly estadoVisible = computed(() => {
    const estado = this.verificacion();
    if (estado === 'cargando' || estado === 'error') return estado;
    if (estado === 'denegada' || !this.accesoVerificado()) return 'sin_permiso';
    return estado === 'confirmada' ? 'exito' : 'vacio';
  });

  revalidarSesion(): void {
    if ((!this.accesoVerificado() && this.verificacion() !== 'error') || this.verificacion() === 'cargando') return;
    this.verificacion.set('cargando');
    defer(() => this.plataforma.obtener()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.verificacion.set(this.accesoVerificado() ? 'confirmada' : 'denegada'),
      error: (error: unknown) => {
        const denegada = error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403);
        this.verificacion.set(denegada ? 'denegada' : 'error');
      },
      complete: () => {
        if (this.verificacion() === 'cargando') this.verificacion.set('error');
      }
    });
  }
}
