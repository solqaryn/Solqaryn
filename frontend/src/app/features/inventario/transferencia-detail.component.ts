import { CommonModule } from '@angular/common';
import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { TransferenciaInventario } from '../../core/models/transferencia-inventario.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { TransferenciaInventarioService } from '../../services/transferencia-inventario.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

type RecepcionLinea = {
  recibida: number;
  faltante: number;
  danada: number;
  sobrante: number;
};

@Component({
  selector: 'app-transferencia-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <section class="page" aria-labelledby="transferencia-title">
      <header class="header">
        <div>
          <p class="eyebrow">Inventario empresarial</p>
          <h1 id="transferencia-title">{{ item?.numero || 'Transferencia' }}</h1>
          @if (item) {
            <p>{{ item.almacenOrigenNombre || ('Almacén #' + item.almacenOrigenId) }} → {{ item.almacenDestinoNombre || ('Almacén #' + item.almacenDestinoId) }}</p>
          }
        </div>
        <div class="header-actions">
          <button mat-stroked-button type="button" (click)="volver()"><mat-icon>arrow_back</mat-icon>Volver</button>
          @if (item?.estado === 'Borrador' && puedeEditar) {
            <button mat-stroked-button type="button" (click)="editar()"><mat-icon>edit</mat-icon>Editar</button>
          }
        </div>
      </header>
    
      @if (loading) {
        <div class="state"><mat-spinner diameter="36"></mat-spinner><span>Cargando transferencia…</span></div>
      }
      @if (!loading && error) {
        <div class="state error">{{ error }}</div>
      }
    
      @if (!loading && item; as transferencia) {
        <section class="summary">
          <div><span>Estado</span><strong>{{ transferencia.estado }}</strong></div>
          <div><span>Origen</span><strong>{{ transferencia.almacenOrigenNombre || ('#' + transferencia.almacenOrigenId) }}</strong></div>
          <div><span>Destino</span><strong>{{ transferencia.almacenDestinoNombre || ('#' + transferencia.almacenDestinoId) }}</strong></div>
          <div><span>Líneas</span><strong>{{ transferencia.detalles.length }}</strong></div>
        </section>
        <section class="timeline">
          <h2>Trazabilidad</h2>
          <div class="dates">
            @if (transferencia.fechaSolicitud) {
              <span>Solicitada: {{ transferencia.fechaSolicitud | date:'short' }}</span>
            }
            @if (transferencia.fechaAprobacion) {
              <span>Aprobada: {{ transferencia.fechaAprobacion | date:'short' }}</span>
            }
            @if (transferencia.fechaDespacho) {
              <span>Despachada: {{ transferencia.fechaDespacho | date:'short' }}</span>
            }
            @if (transferencia.fechaRecepcion) {
              <span>Recibida: {{ transferencia.fechaRecepcion | date:'short' }}</span>
            }
            @if (transferencia.fechaCancelacion) {
              <span>Cancelada: {{ transferencia.fechaCancelacion | date:'short' }}</span>
            }
          </div>
          @if (transferencia.observaciones) {
            <p><strong>Observaciones:</strong> {{ transferencia.observaciones }}</p>
          }
          @if (transferencia.motivoCancelacion) {
            <p class="error"><strong>Motivo de cancelación:</strong> {{ transferencia.motivoCancelacion }}</p>
          }
        </section>
        <section class="details">
          <h2>Detalle físico</h2>
          <div class="table-wrap">
            <table>
              <thead><tr><th>Variante</th><th>SKU</th><th>Solicitada</th><th>Aprobada</th><th>Despachada</th><th>Recibida</th><th>Faltante</th><th>Dañada</th><th>Sobrante</th></tr></thead>
              <tbody>
                @for (detalle of transferencia.detalles; track detalle) {
                  <tr>
                    <td>#{{ detalle.productoVarianteId }}</td>
                    <td>{{ detalle.productoSkuSnapshot || '—' }}</td>
                    <td>{{ detalle.cantidadSolicitada }}</td>
                    <td>{{ detalle.cantidadAprobada }}</td>
                    <td>{{ detalle.cantidadDespachada }}</td>
                    <td>{{ detalle.cantidadRecibida }}</td>
                    <td>{{ detalle.cantidadFaltante }}</td>
                    <td>{{ detalle.cantidadDanada }}</td>
                    <td>{{ detalle.cantidadSobrante }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </section>
        @if (transferencia.estado === 'EnTransito' && puedeConfirmar) {
          <section class="recepcion">
            <div class="section-heading">
              <div>
                <h2>Recepción y discrepancias</h2>
                <p>Recibida + faltante + dañada debe cerrar exactamente lo despachado. El sobrante se registra aparte.</p>
              </div>
            </div>
            <div class="table-wrap">
              <table>
                <thead><tr><th>Variante</th><th>Despachada</th><th>Recibida</th><th>Faltante</th><th>Dañada</th><th>Sobrante</th><th>Control</th></tr></thead>
                <tbody>
                  @for (detalle of transferencia.detalles; track detalle) {
                    <tr>
                      <td>#{{ detalle.productoVarianteId }}</td>
                      <td>{{ detalle.cantidadDespachada }}</td>
                      <td><input class="quantity" type="number" min="0" [name]="'recibida-' + detalle.id" [(ngModel)]="recepcion[detalle.id].recibida" /></td>
                      <td><input class="quantity" type="number" min="0" [name]="'faltante-' + detalle.id" [(ngModel)]="recepcion[detalle.id].faltante" /></td>
                      <td><input class="quantity" type="number" min="0" [name]="'danada-' + detalle.id" [(ngModel)]="recepcion[detalle.id].danada" /></td>
                      <td><input class="quantity" type="number" min="0" [name]="'sobrante-' + detalle.id" [(ngModel)]="recepcion[detalle.id].sobrante" /></td>
                      <td>
                        @if (lineaRecepcionValida(detalle.id, detalle.cantidadDespachada)) {
                          <span class="ok">Cuadra</span>
                        }
                        @if (!lineaRecepcionValida(detalle.id, detalle.cantidadDespachada)) {
                          <span class="error">Debe sumar {{ detalle.cantidadDespachada }}</span>
                        }
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </section>
        }
        <section class="lifecycle">
          <h2>Acciones de lifecycle</h2>
          <div class="actions">
            @if (transferencia.estado === 'Borrador' && puedeCambiarEstado) {
              <button mat-flat-button color="primary" type="button" (click)="solicitar()" [disabled]="busy">Solicitar</button>
            }
            @if (transferencia.estado === 'Solicitada' && puedeAprobar) {
              <button mat-flat-button color="primary" type="button" (click)="aprobar()" [disabled]="busy">Aprobar</button>
            }
            @if (transferencia.estado === 'Aprobada' && puedeConfirmar) {
              <button mat-flat-button color="primary" type="button" (click)="despachar()" [disabled]="busy">Despachar</button>
            }
            @if (transferencia.estado === 'EnTransito' && puedeConfirmar) {
              <button mat-flat-button color="primary" type="button" (click)="recibir()" [disabled]="busy || !recepcionValida">Registrar recepción</button>
            }
            @if (transferencia.estado !== 'Recibida' && transferencia.estado !== 'Cancelada' && puedeAnular) {
              <button mat-stroked-button color="warn" type="button" (click)="cancelar()" [disabled]="busy">Cancelar</button>
            }
            @if (busy) {
              <mat-spinner diameter="24"></mat-spinner>
            }
          </div>
          @if (actionError) {
            <p class="error">{{ actionError }}</p>
          }
        </section>
      }
    </section>
    `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .page{padding:24px;display:grid;gap:20px}.header{display:flex;justify-content:space-between;gap:16px}.header-actions,.actions{display:flex;gap:10px;flex-wrap:wrap;align-items:center}.eyebrow{text-transform:uppercase;letter-spacing:.08em;font-size:12px;font-weight:700;margin:0}.header h1{margin:4px 0}.header p,.section-heading p{margin:0;color:var(--text-secondary,#667085)}.summary{display:grid;grid-template-columns:repeat(4,1fr);gap:12px}.summary div,.timeline,.details,.recepcion,.lifecycle{padding:18px;border:1px solid rgba(0,0,0,.12);border-radius:12px}.summary span{display:block;color:var(--text-secondary,#667085);font-size:12px}.summary strong{display:block;margin-top:6px}.dates{display:flex;gap:14px;flex-wrap:wrap}.table-wrap{overflow:auto}table{width:100%;border-collapse:collapse}th,td{padding:12px;text-align:left;border-bottom:1px solid rgba(0,0,0,.08);white-space:nowrap}.quantity{width:76px;padding:8px;border:1px solid rgba(0,0,0,.2);border-radius:8px}.state{min-height:160px;display:flex;justify-content:center;align-items:center;gap:12px}.error{color:#b42318}.ok{color:#027a48;font-weight:700}@media(max-width:800px){.summary{grid-template-columns:1fr 1fr}.header{flex-direction:column}}@media(max-width:520px){.page{padding:16px}.summary{grid-template-columns:1fr}}
  `]
})
export class TransferenciaDetailComponent implements OnInit {
  readonly id: number;
  item: TransferenciaInventario | null = null;
  recepcion: Record<number, RecepcionLinea> = {};
  loading = false;
  busy = false;
  error = '';
  actionError = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly service: TransferenciaInventarioService,
    private readonly permisos: PermisosRuntimeService,
    private readonly alerts: AppAlertService
  ) {
    this.id = Number(this.route.snapshot.paramMap.get('id')) || 0;
  }

  ngOnInit(): void { this.cargar(); }

  get puedeEditar(): boolean { return this.permisos.puede('MovimientosInventario', 'Editar'); }
  get puedeCambiarEstado(): boolean { return this.permisos.puede('MovimientosInventario', 'CambiarEstado'); }
  get puedeAprobar(): boolean { return this.permisos.puede('MovimientosInventario', 'Aprobar'); }
  get puedeConfirmar(): boolean { return this.permisos.puede('MovimientosInventario', 'Confirmar'); }
  get puedeAnular(): boolean { return this.permisos.puede('MovimientosInventario', 'Anular'); }
  get recepcionValida(): boolean {
    const transferencia = this.item;
    return !!transferencia && transferencia.detalles.length > 0 && transferencia.detalles.every(d => this.lineaRecepcionValida(d.id, d.cantidadDespachada));
  }

  cargar(): void {
    this.loading = true;
    this.error = '';
    this.service.getById(this.id).pipe(finalize(() => this.loading = false)).subscribe({
      next: response => {
        if (!response.success) { this.error = response.message || 'No se pudo cargar la transferencia.'; return; }
        this.item = response.data;
        this.prepararRecepcion(response.data);
      },
      error: () => this.error = 'No se pudo cargar la transferencia.'
    });
  }

  lineaRecepcionValida(detalleId: number, despachada: number): boolean {
    const linea = this.recepcion[detalleId];
    if (!linea) return false;
    const cantidades = [linea.recibida, linea.faltante, linea.danada, linea.sobrante];
    if (cantidades.some(valor => !Number.isFinite(valor) || valor < 0)) return false;
    return linea.recibida + linea.faltante + linea.danada === despachada;
  }

  async solicitar(): Promise<void> {
    const confirmado = await this.alerts.confirmar({
      titulo: 'Solicitar transferencia',
      mensaje: '¿Solicitar esta transferencia? Después de solicitarla ya no podrá editarse como borrador.',
      tipo: 'advertencia',
      confirmarTexto: 'Solicitar'
    });
    if (!confirmado) return;
    this.runAction(() => this.service.solicitar(this.id));
  }

  async aprobar(): Promise<void> {
    const transferencia = this.item;
    if (!transferencia) return;
    const confirmado = await this.alerts.confirmar({
      titulo: 'Aprobar transferencia',
      mensaje: '¿Aprobar las cantidades solicitadas de esta transferencia?',
      tipo: 'advertencia',
      confirmarTexto: 'Aprobar'
    });
    if (!confirmado) return;
    this.runAction(() => this.service.aprobar(this.id, {
      detalles: transferencia.detalles.map(d => ({ detalleId: d.id, cantidadAprobada: d.cantidadSolicitada }))
    }));
  }

  async despachar(): Promise<void> {
    const transferencia = this.item;
    if (!transferencia) return;
    const confirmado = await this.alerts.confirmar({
      titulo: 'Despachar transferencia',
      mensaje: '¿Despachar las cantidades aprobadas? Esta acción afecta stock físico.',
      tipo: 'advertencia',
      confirmarTexto: 'Despachar'
    });
    if (!confirmado) return;
    this.runAction(() => this.service.despachar(this.id, {
      detalles: transferencia.detalles.map(d => ({ detalleId: d.id, cantidadDespachada: d.cantidadAprobada }))
    }));
  }

  async recibir(): Promise<void> {
    const transferencia = this.item;
    if (!transferencia || !this.recepcionValida) return;
    const confirmado = await this.alerts.confirmar({
      titulo: 'Registrar recepción',
      mensaje: '¿Registrar la recepción y sus discrepancias?',
      tipo: 'advertencia',
      confirmarTexto: 'Registrar recepción'
    });
    if (!confirmado) return;
    this.runAction(() => this.service.recibir(this.id, {
      detalles: transferencia.detalles.map(d => {
        const linea = this.recepcion[d.id];
        return {
          detalleId: d.id,
          cantidadRecibida: linea.recibida,
          cantidadFaltante: linea.faltante,
          cantidadDanada: linea.danada,
          cantidadSobrante: linea.sobrante
        };
      })
    }));
  }

  async cancelar(): Promise<void> {
    const motivo = await this.alerts.solicitarTexto({
      titulo: 'Cancelar transferencia',
      mensaje: 'Indica el motivo obligatorio de cancelación. Esta acción no se puede deshacer.',
      tipo: 'peligro',
      confirmarTexto: 'Cancelar transferencia',
      entrada: { etiqueta: 'Motivo de cancelación', requerida: true }
    });
    if (!motivo) return;
    this.runAction(() => this.service.cancelar(this.id, { motivo }));
  }

  editar(): void { void this.router.navigate(['/inventario/transferencias', this.id, 'editar']); }
  volver(): void { void this.router.navigate(['/inventario/transferencias']); }

  private prepararRecepcion(transferencia: TransferenciaInventario): void {
    this.recepcion = Object.fromEntries(transferencia.detalles.map(detalle => {
      const tieneRecepcion = detalle.cantidadRecibida > 0 || detalle.cantidadFaltante > 0 || detalle.cantidadDanada > 0 || detalle.cantidadSobrante > 0;
      return [detalle.id, {
        recibida: tieneRecepcion ? detalle.cantidadRecibida : detalle.cantidadDespachada,
        faltante: detalle.cantidadFaltante,
        danada: detalle.cantidadDanada,
        sobrante: detalle.cantidadSobrante
      }];
    }));
  }

  private runAction(operationFactory: () => ReturnType<TransferenciaInventarioService['solicitar']>): void {
    this.busy = true;
    this.actionError = '';
    operationFactory().pipe(finalize(() => this.busy = false)).subscribe({
      next: response => {
        if (!response.success) { this.actionError = response.message || 'La operación no pudo completarse.'; return; }
        this.item = response.data;
        this.prepararRecepcion(response.data);
      },
      error: error => this.actionError = error?.error?.message || 'La operación no pudo completarse.'
    });
  }
}
