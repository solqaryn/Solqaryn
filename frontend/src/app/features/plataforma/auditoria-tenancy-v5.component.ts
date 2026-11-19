import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, InjectionToken, computed, inject, signal, viewChild, type Signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { defer } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

export type EstadoAuditoriaTenancyV5 = 'vacio' | 'cargando' | 'exito' | 'sin_permiso' | 'error';
export type MarcaAuditoriaTenancyV5 = 'solqaryn' | 'institucional';
export type DensidadAuditoriaTenancyV5 = 'normal' | 'compacta';
export interface PresentacionAuditoriaTenancyV5 {
  readonly marca: MarcaAuditoriaTenancyV5;
  readonly densidad: DensidadAuditoriaTenancyV5;
}
const PRESENTACION_SEGURA: PresentacionAuditoriaTenancyV5 = Object.freeze({
  marca: 'solqaryn', densidad: 'normal'
});

/** C15-03.M04.03: solo presentación reactiva. No recibe ni concede autoridad de tenant/JWT/RBAC. */
export const AUDITORIA_TENANCY_PRESENTACION_V5 = new InjectionToken<Signal<unknown>>(
  'AUDITORIA_TENANCY_PRESENTACION_V5',
  { providedIn: 'root', factory: () => signal<unknown>(PRESENTACION_SEGURA) }
);

/** No acepta CSS, HTML, URLs, datos empresariales ni propiedades no reconocidas. */
export function normalizarPresentacionAuditoriaTenancyV5(valor: unknown): PresentacionAuditoriaTenancyV5 {
  if (valor === null || typeof valor !== 'object' || Array.isArray(valor)) return PRESENTACION_SEGURA;
  const datos = valor as Record<string, unknown>;
  if (Object.keys(datos).length !== 2 ||
      !Object.prototype.hasOwnProperty.call(datos, 'marca') ||
      !Object.prototype.hasOwnProperty.call(datos, 'densidad')) return PRESENTACION_SEGURA;
  const marca = datos['marca'];
  const densidad = datos['densidad'];
  if ((marca !== 'solqaryn' && marca !== 'institucional') ||
      (densidad !== 'normal' && densidad !== 'compacta')) return PRESENTACION_SEGURA;
  return Object.freeze({ marca, densidad });
}

/** C15-03.M04.01: superficie de guia privada; nunca ejecuta pentest ni otorga permisos. */
@Component({
  selector: 'app-auditoria-tenancy-v5',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="tenancy-audit" aria-labelledby="tenancy-audit-title" [attr.data-densidad]="presentacion().densidad">
      <header>
        <p class="eyebrow">{{ presentacion().marca === 'institucional' ? 'Plataforma SOLQARYN · Auditoría de seguridad y tenancy' : 'SOLQARYN · Auditoría de seguridad y tenancy' }}</p>
        <h1 id="tenancy-audit-title">Fronteras de aislamiento y evidencia</h1>
        <p>Guía privada, sin consultas de datos ni pruebas de seguridad en vivo.</p>
        <a routerLink="/plataforma" aria-label="Regresar a administración de plataforma">Volver a plataforma</a>
        <button type="button" (click)="revalidar()" [disabled]="estado() === 'cargando'" aria-label="Revalidar sesión de auditoría de tenancy">Revalidar sesión</button>
      </header>
      <section class="estado" aria-live="polite" aria-atomic="true" aria-label="Estado de autorización de auditoría de tenancy" [attr.aria-busy]="estado() === 'cargando'">
        @switch (estadoVisible()) {
          @case ('cargando') { <p role="status">Revalidando autorización privada con el servidor…</p> }
          @case ('sin_permiso') { <p role="alert">Sesión de plataforma no confirmada. Guía protegida oculta.</p> }
          @case ('error') { <p role="alert">No fue posible revalidar la sesión. La guía permanece restringida.</p> }
          @case ('vacio') { <p role="status">Sesión verificada. Sin mediciones institucionales conectadas.</p> }
          @case ('exito') { <p role="status">Sesión revalidada. Solo guía de revisión, no pentest real.</p> }
        }
      </section>
      @if (accesoVerificado()) {
        <section class="notice" aria-label="Alcance de solo lectura">
          <strong>Solo lectura · ninguna prueba o autorización ejecutada</strong>
          <p>Los criterios siguientes son un contrato de revisión futura, no resultados certificados.</p>
        </section>
        <a class="skip-frames" href="#fronteras-title" (click)="irAFronteras($event)">Saltar a los cuatro frentes de revisión</a>
        <section aria-labelledby="fronteras-title">
          <h2 #fronterasTitle id="fronteras-title" tabindex="-1">Cuatro frentes de comprobación futura</h2>
          <ol class="fronteras" aria-labelledby="fronteras-title">
            <li><h3>Tenant A/B y sucursal</h3><p>Rechazar cruce de EmpresaId, SucursalId, consultas, índices y exportaciones, incluso con identificadores repetidos.</p></li>
            <li><h3>Propiedad y pertenencia</h3><p>Revalidar usuario, rol, membresía, revocación y denegación por defecto en backend.</p></li>
            <li><h3>Archivos, almacenamiento y jobs</h3><p>Verificar ownership, rutas, claves, idempotencia y doble ejecución; ningún restore o proveedor se invoca aquí.</p></li>
            <li><h3>Control plane y secretos</h3><p>Probar fallos seguros, límites, registros mínimos y ausencia de credenciales en respuestas; no conceder privilegios.</p></li>
          </ol>
        </section>
        <p class="limites" role="note">Estado de pentest, resultados A/B y métricas: NO_MEDIDO. QA/PROD no autorizados.</p>
      } @else {
        <p class="denied" role="alert">Sesión de plataforma no confirmada. Guía protegida oculta.</p>
      }
    </section>
  `,
  styles: [`
    :host { display: block; color: var(--mat-sys-on-surface, #17212d); }
    .tenancy-audit { max-width: 72rem; box-sizing: border-box; margin: auto; padding: clamp(1rem,3vw,2.5rem); }
    h1 { line-height: 1.2; font-size: clamp(1.6rem,4vw,2.3rem); }
    h2 { margin-top: 1.5rem; font-size: 1.3rem; }
    h3 { font-size: 1rem; margin: 0 0 .3rem; }
    p { line-height: 1.55; overflow-wrap: anywhere; }
    .eyebrow { font-weight: 700; color: var(--mat-sys-primary, #2157a3); }
    header :is(a,button) { display: inline-block; border: 1px solid currentColor; border-radius: .55rem; margin-right:.5rem; padding: .7rem .9rem; color: inherit; background:transparent; font:inherit; text-decoration: underline; cursor:pointer; }
    header button[disabled] { opacity:.55; cursor:progress; }
    header :is(a,button):focus-visible { outline: 3px solid var(--mat-sys-primary, #2157a3); outline-offset: 3px; }
    .skip-frames { display:inline-flex; margin-top:.75rem; padding:.65rem .85rem; border:2px solid currentColor; border-radius:.5rem; color:inherit; text-decoration:underline; }
    .skip-frames:focus-visible { outline:3px solid var(--mat-sys-primary, #2157a3); outline-offset:3px; }
    @media (prefers-contrast:more) { .skip-frames:focus-visible, header a:focus-visible { outline-width:4px; } }
.estado,.notice,.denied { border: 1px solid var(--mat-sys-outline-variant, #aeb8c1); border-radius: .75rem; padding: 1rem; margin-top: 1rem; }
    .notice { background: var(--mat-sys-surface-container-low, #f5f7fa); }
    .fronteras { display: grid; grid-template-columns: repeat(2,minmax(0,1fr)); gap: .85rem; list-style: none; padding: 0; }
    .fronteras li { min-width: 0; border: 1px solid var(--mat-sys-outline-variant, #aeb8c1); border-radius: .75rem; padding: 1rem; }
    .fronteras p { margin: 0; }
    .tenancy-audit[data-densidad='compacta'] :is(.estado,.notice,.denied,.fronteras li) { padding: .75rem; }
    .limites { font-weight: 600; }
    @media(max-width: 45rem) { .fronteras { grid-template-columns: 1fr; } }
    @media(forced-colors: active) {
      .tenancy-audit { background: Canvas; color: CanvasText; }
      .skip-frames { background: Canvas; color: CanvasText; }
      .skip-frames:focus-visible { outline-color: CanvasText; background: Canvas; color: CanvasText; }
    }
  `]
})
export class AuditoriaTenancyV5Component {
  private readonly tituloFrentes = viewChild<ElementRef<HTMLHeadingElement>>('fronterasTitle');
  /** Evita que base href del SPA convierta el fragmento en navegación a /. */
  irAFronteras(evento: MouseEvent): void {
    evento.preventDefault();
    this.tituloFrentes()?.nativeElement.focus();
  }
  private readonly plataforma = inject(PlataformaSesionService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly configuracionPresentacion = inject(AUDITORIA_TENANCY_PRESENTACION_V5);
  readonly presentacion = computed(() => normalizarPresentacionAuditoriaTenancyV5(this.configuracionPresentacion()));
  /** M04.02: no se infiere autoridad desde JWT, empresa, cache ni un estado visual. */
  readonly estado = signal<EstadoAuditoriaTenancyV5>(this.plataforma.sesion() ? 'vacio' : 'sin_permiso');
  readonly accesoVerificado = computed(() => {
    const sesion = this.plataforma.sesion();
    const actual = this.estado();
    return (actual === 'vacio' || actual === 'exito') &&
      sesion?.ambito === 'Plataforma' && sesion.esAdministrador === true;
  });
  readonly estadoVisible = computed<EstadoAuditoriaTenancyV5>(() => {
    if (this.estado() === 'cargando') return 'cargando';
    if (this.estado() === 'error') return 'error';
    if (!this.accesoVerificado()) return 'sin_permiso';
    return this.estado();
  });
  revalidar(): void {
    if (this.estado() === 'cargando') return; // single-flight, no doble llamada.
    this.estado.set('cargando');
    defer(() => this.plataforma.obtener()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (sesion) => {
        const actual = this.plataforma.sesion();
        const confirmado = sesion?.ambito === 'Plataforma' &&
          sesion?.esAdministrador === true &&
          actual?.ambito === 'Plataforma' && actual.esAdministrador === true;
        this.estado.set(confirmado ? 'exito' : 'sin_permiso');
      },
      complete: () => {
        if (this.estado() === 'cargando') this.estado.set('error'); // never treat empty response as grant.
      },
      error: (error: unknown) => {
        const status = error instanceof HttpErrorResponse ? error.status : 0;
        this.estado.set(status === 401 || status === 403 ? 'sin_permiso' : 'error');
      }
    });
  }
}
