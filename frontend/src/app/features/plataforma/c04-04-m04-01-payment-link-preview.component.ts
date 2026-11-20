import { Component, ChangeDetectionStrategy, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

const PASOS = Object.freeze([
  Object.freeze({ id: 'enlace', titulo: 'Solicitud de enlace', estado: 'SIN_CONEXION' }),
  Object.freeze({ id: 'confirmacion', titulo: 'Confirmacion y conciliacion', estado: 'SIN_CONEXION' }),
  Object.freeze({ id: 'reversion', titulo: 'Reversion y trazabilidad', estado: 'SIN_CONEXION' })
] as const);

/** C04-04.M04.01: reference surface; no payment provider, HTTP, or DI registration. */
@Component({
  selector: 'app-c0404-payment-link-preview',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="c0404" aria-labelledby="c0404-title" data-testid="c0404-payment-preview">
      <header>
        <p class="eyebrow">SOLQARYN · Referencia de desarrollo</p>
        <h1 id="c0404-title">Enlaces de cobro</h1>
        <p>Vista informativa. No emite enlaces, no contacta proveedores ni procesa pagos.</p>
        <a routerLink="/plataforma" aria-label="Volver a plataforma">Volver a plataforma</a>
      </header>
      @if (autorizado()) {
        <section class="panel" data-testid="c0404-payment-private" aria-labelledby="c0404-steps" [attr.aria-busy]="estadoVista() === 'cargando' ? 'true' : 'false'">
          <h2 id="c0404-steps">Etapas previstas</h2>
          @switch (estadoVista()) {
            @case ('cargando') {
              <p role="status" aria-live="polite" aria-atomic="true" data-testid="c0404-state-loading">Preparando vista informativa local. No se consulta ningún proveedor de pagos.</p>
            }
            @case ('vacio') {
              <p role="status" aria-live="polite" aria-atomic="true" data-testid="c0404-state-empty">SIN_CONEXION: no hay cobros ni webhooks reales.</p>
            }
            @case ('error') {
              <p role="alert" aria-atomic="true" data-testid="c0404-state-error">No se pudo preparar la vista informativa. Ningún pago se ha procesado.</p>
            }
            @case ('listo') {
              <p role="status" aria-live="polite" aria-atomic="true" data-testid="c0404-state-ready">Vista informativa preparada correctamente; SIN_CONEXION al proveedor. Esto no confirma pagos ni cobros.</p>
            }
            @default {
              <p role="alert" data-testid="c0404-state-invalid">Vista no disponible. Estado no reconocido, sin operaciones financieras.</p>
            }
          }
          <ol>
            @for (paso of pasos; track paso.id) {
              <li [attr.data-testid]="'c0404-step-' + paso.id">
                <h3>{{ paso.titulo }}</h3>
                <p>Integracion: <strong>{{ paso.estado }}</strong></p>
              </li>
            }
          </ol>
          <p role="note">Nunca se muestran enlaces cobrables, identificadores, secretos, montos ni PII.</p>
          <p role="note">El guard de Angular no sustituye autorizacion JWT/RBAC del servidor.</p>
        </section>
      } @else {
        <p role="alert" data-testid="c0404-payment-denied">Acceso de plataforma no confirmado. Se oculta la referencia privada.</p>
      }
    </main>
  `,
  styles: [`
    :host { display:block; color:var(--mat-sys-on-surface,#202631); }
    .c0404 { box-sizing:border-box; max-width:74rem; margin:0 auto; padding:clamp(1rem,3vw,2.5rem); }
    h1 { font-size:clamp(1.5rem,3vw,2.25rem); line-height:1.2; }
    h2 { font-size:1.22rem; } h3 { font-size:1rem; margin:0; }
    p,li { line-height:1.55; overflow-wrap:anywhere; }
    .eyebrow { color:var(--mat-sys-primary,#2157a3); font-weight:700; }
    .panel { margin-top:1.5rem; border:1px solid var(--mat-sys-outline-variant,#aeb8c1); border-radius:.75rem; padding:1rem; }
    ol { display:grid; grid-template-columns:repeat(auto-fit,minmax(min(100%,260px),1fr)); gap:1rem; list-style:none; padding:0; margin:1rem 0; }
    li { min-width:0; padding:1rem; border:1px solid var(--mat-sys-outline-variant,#aeb8c1); border-radius:.65rem; }
    a { display:inline-block; min-height:44px; box-sizing:border-box; padding:.75rem 1rem; border:1px solid currentColor; border-radius:.5rem; color:inherit; }
    a:focus-visible { outline:3px solid var(--mat-sys-primary,#2157a3); outline-offset:3px; }
  `]
})
export class C0404PaymentLinkPreviewComponent {
  private readonly plataforma = inject(PlataformaSesionService);
  readonly pasos = PASOS;
  /** C04-04.M04.02: estados visuales informativos; no es estado de un pago real. */
  readonly estadoVista = input<'cargando' | 'vacio' | 'error' | 'listo'>('vacio');
  readonly autorizado = computed(() => {
    const sesion = this.plataforma.sesion();
    return sesion?.ambito === 'Plataforma' && sesion.esAdministrador === true;
  });
}
