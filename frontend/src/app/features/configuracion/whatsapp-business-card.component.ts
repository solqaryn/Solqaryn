
import { HttpClient } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';

interface WhatsAppSessionStatus {
  status: string;
  qr: string | null;
  numeroTelefonoE164: string;
  requiereProveedorSesion: boolean;
}

@Component({
  selector: 'app-whatsapp-business-card',
  standalone: true,
  imports: [],
  template: `
    <section class="card whatsapp-card" aria-labelledby="whatsapp-business-title">
      <div class="header-row">
        <div>
          <h2 id="whatsapp-business-title">WhatsApp Business</h2>
          <p class="subtitle">
            Consulta el estado seguro de la integración para la empresa activa. Solqaryn nunca inventa un QR ni expone credenciales del proveedor.
          </p>
        </div>
        <span class="status-chip" [attr.data-state]="estado()?.status || 'SIN_CONSULTAR'">
          {{ etiquetaEstado() }}
        </span>
      </div>

      @if (!empresaId()) {
        <div class="notice warning" role="status">
          Selecciona una empresa activa para consultar WhatsApp Business. La integración usa el mismo tenant validado por el resto de Solqaryn.
        </div>
      } @else {
        <div class="summary-grid" aria-label="Resumen de WhatsApp Business">
          <div><span class="label">Empresa activa</span><strong>#{{ empresaId() }}</strong></div>
          <div><span class="label">Número</span><strong>{{ estado()?.numeroTelefonoE164 || 'Pendiente de consulta' }}</strong></div>
          <div><span class="label">Proveedor de sesión</span><strong>{{ etiquetaProveedor() }}</strong></div>
        </div>

        @if (loading()) {
          <div class="notice" role="status" aria-live="polite">Consultando el estado de WhatsApp Business…</div>
        }

        @if (error()) {
          <div class="notice error" role="alert">{{ error() }}</div>
        }

        @if (estado(); as actual) {
          <div class="connection-panel">
            @if (qrSeguro()) {
              <img class="qr" [src]="qrSeguro()" alt="Código QR para vincular la sesión de WhatsApp Business">
            } @else {
              <div class="qr-placeholder" role="img" aria-label="QR no disponible">
                <span aria-hidden="true">▦</span>
                <strong>QR no disponible</strong>
                <small>
                  @if (actual.requiereProveedorSesion) {
                    La configuración del tenant existe, pero el proveedor de sesión todavía no ha entregado un QR verificable.
                  } @else {
                    La sesión no requiere un QR en este momento.
                  }
                </small>
              </div>
            }

            <dl>
              <div><dt>Estado</dt><dd>{{ actual.status }}</dd></div>
              <div><dt>Teléfono</dt><dd>{{ actual.numeroTelefonoE164 }}</dd></div>
              <div><dt>Aislamiento</dt><dd>Tenant activo verificado en backend</dd></div>
            </dl>
          </div>
        } @else if (!loading()) {
          <div class="empty-state">
            Aún no se ha consultado la integración. La consulta no almacena secretos en el navegador.
          </div>
        }

        <div class="actions">
          <button
            type="button"
            class="primary-button"
            [disabled]="loading() || !puedeEditar()"
            (click)="consultarEstado()">
            {{ loading() ? 'Consultando…' : (estado() ? 'Actualizar estado' : 'Consultar / iniciar conexión') }}
          </button>
          @if (!puedeEditar()) {
            <span class="permission-hint">Necesitas “Configuración: Editar” para iniciar o consultar la sesión.</span>
          }
        </div>
      }
    </section>
  `,
  styles: [`
    .whatsapp-card { margin-top: 1rem; padding: 1.25rem; }
    .header-row { display: flex; justify-content: space-between; gap: 1rem; align-items: flex-start; }
    h2 { margin: 0 0 .35rem; }
    .subtitle { margin: 0; color: var(--text-secondary, #5f6368); max-width: 70ch; }
    .status-chip { border: 1px solid currentColor; border-radius: 999px; padding: .35rem .65rem; font-size: .78rem; font-weight: 700; white-space: nowrap; }
    .summary-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: .75rem; margin-top: 1rem; }
    .summary-grid > div { border: 1px solid rgba(0,0,0,.12); border-radius: .65rem; padding: .8rem; display: grid; gap: .25rem; }
    .label { font-size: .78rem; color: var(--text-secondary, #5f6368); }
    .notice, .empty-state { margin-top: 1rem; border-radius: .65rem; padding: .85rem 1rem; background: rgba(0,0,0,.04); }
    .notice.warning { border-left: 4px solid #b26a00; }
    .notice.error { border-left: 4px solid #b3261e; }
    .connection-panel { display: grid; grid-template-columns: minmax(160px, 220px) 1fr; gap: 1rem; margin-top: 1rem; align-items: stretch; }
    .qr { width: 100%; max-width: 220px; aspect-ratio: 1; object-fit: contain; border: 1px solid rgba(0,0,0,.12); border-radius: .65rem; }
    .qr-placeholder { min-height: 160px; border: 1px dashed rgba(0,0,0,.25); border-radius: .65rem; display: grid; place-items: center; align-content: center; gap: .35rem; text-align: center; padding: 1rem; }
    .qr-placeholder > span { font-size: 2rem; }
    .qr-placeholder small { color: var(--text-secondary, #5f6368); }
    dl { margin: 0; border: 1px solid rgba(0,0,0,.12); border-radius: .65rem; overflow: hidden; }
    dl > div { display: grid; grid-template-columns: 150px 1fr; gap: .75rem; padding: .75rem; border-bottom: 1px solid rgba(0,0,0,.08); }
    dl > div:last-child { border-bottom: 0; }
    dt { color: var(--text-secondary, #5f6368); }
    dd { margin: 0; font-weight: 600; overflow-wrap: anywhere; }
    .actions { display: flex; align-items: center; gap: .75rem; flex-wrap: wrap; margin-top: 1rem; }
    .primary-button { border: 0; border-radius: .55rem; padding: .7rem 1rem; cursor: pointer; background: var(--primary-color, #2563eb); color: #fff; font: inherit; font-weight: 600; }
    .primary-button:disabled { opacity: .55; cursor: not-allowed; }
    .permission-hint { font-size: .85rem; color: var(--text-secondary, #5f6368); }
    @media (max-width: 720px) {
      .header-row { display: grid; }
      .status-chip { justify-self: start; }
      .summary-grid, .connection-panel { grid-template-columns: 1fr; }
      dl > div { grid-template-columns: 1fr; gap: .25rem; }
    }
  `]
})
export class WhatsappBusinessCardComponent {
  private readonly http = inject(HttpClient);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly tenantContext = inject(TenantContextService);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly estado = signal<WhatsAppSessionStatus | null>(null);
  readonly empresaId = computed(() => this.tenantContext.empresaIdVerificada());
  readonly puedeEditar = computed(() => this.permisos.puede('Configuracion', 'Editar'));
  readonly etiquetaEstado = computed(() => this.estado()?.status || 'SIN CONSULTAR');
  readonly etiquetaProveedor = computed(() => {
    const actual = this.estado();
    if (!actual) return 'Pendiente de consulta';
    return actual.requiereProveedorSesion ? 'Requerido' : 'Listo';
  });
  readonly qrSeguro = computed(() => {
    const qr = this.estado()?.qr?.trim();
    return qr?.startsWith('data:image/') ? qr : null;
  });

  consultarEstado(): void {
    const empresaId = this.empresaId();
    this.error.set(null);

    if (!empresaId) {
      this.estado.set(null);
      this.error.set('No hay una empresa activa válida para consultar.');
      return;
    }

    if (!this.puedeEditar()) {
      this.error.set('No tienes permiso para iniciar o consultar la sesión de WhatsApp Business.');
      return;
    }

    this.loading.set(true);
    this.http.post<WhatsAppSessionStatus>(
      `${environment.apiUrl}/whatsapp/iniciar-whatsapp`,
      { empresaId }
    ).pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: (respuesta) => this.estado.set(respuesta),
      error: (err) => {
        this.estado.set(null);
        const detail = err?.error?.detail ?? err?.error?.message;
        this.error.set(detail || 'No se pudo consultar el estado de WhatsApp Business.');
      }
    });
  }
}
