import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, Input, computed, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { environment } from '../../../environments/environment';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { Factura } from '../../core/models/factura.model';

interface DocumentoFiscalEmisionResponse {
  registroId: number;
  estado: 'Pendiente' | 'Confirmado' | 'Rechazado' | string;
  referenciaExterna?: string | null;
  codigoProveedor?: string | null;
  mensaje?: string | null;
  esTransitorio: boolean;
  idempotente: boolean;
  reintentoAceptado: boolean;
}

type AmbitoSucursal = 'empresa' | 'sucursal';

@Component({
  selector: 'app-factura-fiscal-emision',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule
  ],
  template: `
    @if (puedeEmitir()) {
      <section class="fiscal-card no-print" aria-labelledby="fiscal-title">
        <div class="fiscal-heading">
          <div>
            <p class="eyebrow">Facturación fiscal/electrónica</p>
            <h2 id="fiscal-title">Emitir mediante proveedor fiscal</h2>
            <p>
              Solqaryn envía un contrato técnico neutral. La jurisdicción, el proveedor y el tipo de documento
              son códigos de la configuración aplicable; la interfaz no presume una legislación universal.
            </p>
          </div>
          @if (!mostrarFormulario()) {
            <button
              mat-flat-button
              color="primary"
              type="button"
              [disabled]="factura.estado === 'Anulada'"
              (click)="abrirFormulario()">
              <mat-icon>receipt_long</mat-icon>
              Preparar emisión fiscal
            </button>
          }
        </div>

        @if (factura.estado === 'Anulada') {
          <div class="status status-error" role="alert">
            <mat-icon>block</mat-icon>
            <span>Una factura anulada no puede iniciar una nueva emisión fiscal.</span>
          </div>
        } @else if (!empresaIdVerificada()) {
          <div class="status status-empty" role="status">
            <mat-icon>domain_disabled</mat-icon>
            <span>Selecciona y verifica una empresa antes de emitir. El identificador local no se usa como autoridad.</span>
          </div>
        }

        @if (mostrarFormulario() && factura.estado !== 'Anulada') {
          <div class="fiscal-form" aria-busy="{{ emitiendo() }}">
            <div class="form-grid">
              <mat-form-field appearance="outline">
                <mat-label>Jurisdicción</mat-label>
                <input
                  matInput
                  maxlength="80"
                  [(ngModel)]="jurisdiccion"
                  (ngModelChange)="configuracionCambio()"
                  placeholder="Código configurado">
                <mat-hint>Máximo 80 caracteres; no se asume país o autoridad.</mat-hint>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Proveedor fiscal</mat-label>
                <input
                  matInput
                  maxlength="80"
                  [(ngModel)]="proveedor"
                  (ngModelChange)="configuracionCambio()"
                  placeholder="Código del adaptador">
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Tipo de documento</mat-label>
                <input
                  matInput
                  maxlength="80"
                  [(ngModel)]="tipoDocumento"
                  (ngModelChange)="configuracionCambio()"
                  placeholder="Código configurado">
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Ámbito de sucursal</mat-label>
                <mat-select [(ngModel)]="ambitoSucursal" (selectionChange)="configuracionCambio()">
                  <mat-option value="empresa">Empresa verificada</mat-option>
                  <mat-option value="sucursal">Sucursal específica</mat-option>
                </mat-select>
              </mat-form-field>

              @if (ambitoSucursal === 'sucursal') {
                <mat-form-field appearance="outline">
                  <mat-label>ID de sucursal</mat-label>
                  <input
                    matInput
                    type="number"
                    min="1"
                    step="1"
                    [(ngModel)]="sucursalId"
                    (ngModelChange)="configuracionCambio()">
                  <mat-hint>El backend vuelve a validar que pertenezca al tenant.</mat-hint>
                </mat-form-field>
              }
            </div>

            <div class="invoice-context">
              <div><span>Factura</span><strong>{{ factura.numeroFactura }}</strong></div>
              <div><span>Empresa verificada</span><strong>{{ empresaIdVerificada() || 'Sin contexto' }}</strong></div>
              <div><span>Moneda</span><strong>{{ factura.moneda }}</strong></div>
              <div><span>Total</span><strong>{{ factura.total | number:'1.2-2' }}</strong></div>
            </div>

            @if (error()) {
              <div class="status status-error" role="alert">
                <mat-icon>error</mat-icon>
                <span>{{ error() }}</span>
              </div>
            }

            @if (resultado(); as r) {
              <div class="result-shell" aria-live="polite">
                <div class="status" [class.status-ok]="r.estado === 'Confirmado'" [class.status-error]="r.estado === 'Rechazado'" [class.status-pending]="r.estado !== 'Confirmado' && r.estado !== 'Rechazado'">
                  <mat-icon>{{ r.estado === 'Confirmado' ? 'check_circle' : (r.estado === 'Rechazado' ? 'cancel' : 'schedule') }}</mat-icon>
                  <span>{{ r.mensaje || ('Estado técnico: ' + r.estado) }}</span>
                </div>
                <div class="table-scroll">
                  <table class="result-table">
                    <thead>
                      <tr><th>Registro</th><th>Estado</th><th>Referencia</th><th>Código proveedor</th><th>Idempotente</th></tr>
                    </thead>
                    <tbody>
                      <tr>
                        <td>{{ r.registroId }}</td>
                        <td>{{ r.estado }}</td>
                        <td>{{ r.referenciaExterna || '—' }}</td>
                        <td>{{ r.codigoProveedor || '—' }}</td>
                        <td>{{ r.idempotente ? 'Sí' : 'No' }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            }

            <div class="actions">
              <button
                mat-flat-button
                color="primary"
                type="button"
                [disabled]="!formularioValido() || emitiendo()"
                (click)="emitir()">
                @if (emitiendo()) {
                  <mat-spinner diameter="18"></mat-spinner>
                } @else {
                  <mat-icon>send</mat-icon>
                }
                {{ resultado()?.esTransitorio ? 'Reintentar con la misma clave' : 'Emitir' }}
              </button>
              <button mat-button type="button" [disabled]="emitiendo()" (click)="cerrarFormulario()">Cerrar</button>
            </div>

            <p class="footnote">
              El servidor revalida membresía, identidad fiscal de la empresa, ownership de factura y sucursal.
              Los estados de proveedor son técnicos y no sustituyen una conclusión jurídica universal.
            </p>
          </div>
        }
      </section>
    }
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .fiscal-card{margin:18px 0;padding:20px;border:1px solid #d8dee8;border-radius:14px;background:#fff;box-shadow:0 8px 24px rgba(15,23,42,.06)}
    .fiscal-heading{display:flex;gap:18px;justify-content:space-between;align-items:flex-start}.fiscal-heading h2{margin:2px 0 6px;font-size:20px}.fiscal-heading p{margin:0;color:#526071;max-width:760px}.eyebrow{font-size:12px!important;font-weight:700;text-transform:uppercase;letter-spacing:.08em;color:#0f5f91!important}
    .fiscal-form{margin-top:18px}.form-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px}.form-grid mat-form-field{width:100%}
    .invoice-context{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:10px;margin:2px 0 16px}.invoice-context div{padding:10px 12px;background:#f7f9fc;border-radius:9px}.invoice-context span{display:block;font-size:12px;color:#607085}.invoice-context strong{display:block;margin-top:3px;overflow-wrap:anywhere}
    .status{display:flex;align-items:flex-start;gap:9px;padding:11px 13px;border-radius:9px;margin:12px 0;background:#eef4fa;color:#314154}.status mat-icon{flex:0 0 auto}.status-ok{background:#e9f7ef;color:#17663b}.status-error{background:#fff0f0;color:#9b2525}.status-pending{background:#fff7e5;color:#725214}.status-empty{background:#f4f5f7;color:#4b5563}
    .table-scroll{overflow-x:auto}.result-table{width:100%;border-collapse:collapse;margin-top:8px;min-width:620px}.result-table th,.result-table td{text-align:left;padding:9px 10px;border-bottom:1px solid #e5e9ef}.result-table th{font-size:12px;text-transform:uppercase;color:#607085;background:#f7f9fc}
    .actions{display:flex;gap:8px;align-items:center;margin-top:16px}.actions button mat-spinner{display:inline-block;margin-right:8px}.footnote{font-size:12px;color:#68778a;margin:14px 0 0}
    @media (max-width:760px){.fiscal-card{padding:15px}.fiscal-heading{flex-direction:column}.form-grid,.invoice-context{grid-template-columns:1fr}.fiscal-heading button{width:100%}.actions{align-items:stretch;flex-direction:column}.actions button{width:100%}}
  `]
})
export class FacturaFiscalEmisionComponent {
  @Input({ required: true }) factura!: Factura;

  private readonly http = inject(HttpClient);
  private readonly permisosRuntime = inject(PermisosRuntimeService);
  private readonly tenantContext = inject(TenantContextService);
  private readonly apiUrl = `${environment.apiUrl}/facturacion-fiscal/emisiones`;

  readonly mostrarFormulario = signal(false);
  readonly emitiendo = signal(false);
  readonly resultado = signal<DocumentoFiscalEmisionResponse | null>(null);
  readonly error = signal('');
  readonly puedeEmitir = computed(() => this.permisosRuntime.puede('Facturacion', 'Crear'));
  readonly empresaIdVerificada = this.tenantContext.empresaIdVerificada;

  jurisdiccion = '';
  proveedor = '';
  tipoDocumento = '';
  ambitoSucursal: AmbitoSucursal = 'empresa';
  sucursalId: number | null = null;
  private claveIdempotencia = '';

  abrirFormulario(): void {
    if (!this.puedeEmitir() || this.factura.estado === 'Anulada') return;
    this.mostrarFormulario.set(true);
    this.error.set('');
  }

  cerrarFormulario(): void {
    if (this.emitiendo()) return;
    this.mostrarFormulario.set(false);
    this.error.set('');
  }

  configuracionCambio(): void {
    this.claveIdempotencia = '';
    this.resultado.set(null);
    this.error.set('');
    if (this.ambitoSucursal === 'empresa') this.sucursalId = null;
  }

  formularioValido(): boolean {
    const empresaId = this.empresaIdVerificada();
    const codigosValidos = [this.jurisdiccion, this.proveedor, this.tipoDocumento]
      .every((value) => value.trim().length > 0 && value.trim().length <= 80);
    const sucursalValida = this.ambitoSucursal === 'empresa' ||
      (Number.isInteger(Number(this.sucursalId)) && Number(this.sucursalId) > 0);

    return this.puedeEmitir() && !!empresaId && empresaId > 0 && codigosValidos && sucursalValida && !this.emitiendo();
  }

  async emitir(): Promise<void> {
    if (!this.formularioValido() || this.factura.estado === 'Anulada') return;

    const empresaId = this.empresaIdVerificada();
    if (!empresaId) {
      this.error.set('El contexto de empresa dejó de estar verificado. Selecciónalo nuevamente.');
      return;
    }

    if (!this.claveIdempotencia) this.claveIdempotencia = this.crearClaveIdempotencia();

    this.emitiendo.set(true);
    this.error.set('');

    const hashSnapshot = await this.crearHashSnapshot();
    const request = {
      empresaId,
      sucursalId: this.ambitoSucursal === 'sucursal' ? Number(this.sucursalId) : null,
      facturaId: this.factura.id,
      jurisdiccion: this.jurisdiccion.trim(),
      proveedor: this.proveedor.trim(),
      tipoDocumento: this.tipoDocumento.trim(),
      claveIdempotencia: this.claveIdempotencia,
      hashSnapshot
    };

    this.http.post<DocumentoFiscalEmisionResponse>(this.apiUrl, request).subscribe({
      next: (res) => {
        this.emitiendo.set(false);
        this.resultado.set(res);
        if (!res.esTransitorio) this.claveIdempotencia = '';
      },
      error: (err: HttpErrorResponse) => {
        this.emitiendo.set(false);
        const body = err.error as Partial<DocumentoFiscalEmisionResponse> & { mensaje?: string; message?: string; error?: string };
        if (err.status === 422 && body && typeof body.registroId === 'number' && typeof body.estado === 'string') {
          this.resultado.set(body as DocumentoFiscalEmisionResponse);
          this.claveIdempotencia = '';
          return;
        }

        const mensaje = body?.mensaje || body?.message || body?.error || 'No se pudo completar la emisión fiscal.';
        this.error.set(mensaje);
        if (err.status !== 503) this.claveIdempotencia = '';
      }
    });
  }

  private async crearHashSnapshot(): Promise<string> {
    const snapshot = JSON.stringify({
      version: 1,
      facturaId: this.factura.id,
      numeroFactura: this.factura.numeroFactura,
      fechaEmision: this.factura.fechaEmision,
      estado: this.factura.estado,
      moneda: this.factura.moneda,
      empresaRTN: this.factura.empresaRTN ?? null,
      clienteIdentidadORTN: this.factura.clienteIdentidadORTN ?? null,
      subtotal: this.factura.subtotal,
      descuento: this.factura.descuento,
      impuesto: this.factura.impuesto,
      total: this.factura.total,
      detalles: this.factura.detalles.map((detalle) => ({
        productoId: detalle.productoId,
        productoVarianteId: detalle.productoVarianteId ?? null,
        cantidad: detalle.cantidad,
        precioUnitario: detalle.precioUnitario,
        descuento: detalle.descuento,
        impuesto: detalle.impuesto,
        totalLinea: detalle.totalLinea
      }))
    });

    try {
      const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(snapshot));
      return `sha256:${Array.from(new Uint8Array(digest)).map((b) => b.toString(16).padStart(2, '0')).join('')}`;
    } catch {
      let hash = 2166136261;
      for (let i = 0; i < snapshot.length; i += 1) {
        hash ^= snapshot.charCodeAt(i);
        hash = Math.imul(hash, 16777619);
      }
      return `fnv1a32:${(hash >>> 0).toString(16).padStart(8, '0')}`;
    }
  }

  private crearClaveIdempotencia(): string {
    try {
      return crypto.randomUUID();
    } catch {
      return `fiscal-${Date.now()}-${Math.random().toString(36).slice(2)}-${Math.random().toString(36).slice(2)}`;
    }
  }
}
