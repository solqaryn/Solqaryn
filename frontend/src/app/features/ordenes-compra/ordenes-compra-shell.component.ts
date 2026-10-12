import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize, Subscription } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { EstadoOrdenCompra, EstadoOrdenCompraNombre, OrdenCompra } from '../../core/models/orden-compra.model';
import { Proveedor } from '../../core/models/proveedor.model';
import { OrdenCompraService } from '../../services/orden-compra.service';
import { ProveedorService } from '../../services/proveedor.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

@Component({
  selector: 'app-ordenes-compra-shell',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule
  ],
  template: `
    <section class="page-shell" aria-labelledby="ordenes-title">
      <header class="page-header">
        <div>
          <p class="eyebrow">Compras empresariales</p>
          <h1 id="ordenes-title">Órdenes de compra</h1>
          <p>Consulta y controla documentos de compra sin afectar inventario hasta la recepción autorizada.</p>
        </div>
        @if (permisosRuntime.puede('Compras', 'Crear')) {
          <button mat-flat-button type="button" (click)="nuevaOrden()">
            <mat-icon>add_shopping_cart</mat-icon>
            Nueva orden
          </button>
        }
      </header>

      <form class="filters" (ngSubmit)="aplicarFiltros()" aria-label="Filtros de órdenes de compra">
        <mat-form-field appearance="outline">
          <mat-label>Número</mat-label>
          <input matInput [(ngModel)]="numero" name="numero" autocomplete="off">
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Estado</mat-label>
          <mat-select [(ngModel)]="estado" name="estado">
            <mat-option value="">Todos</mat-option>
            @for (item of estados; track item) {
              <mat-option [value]="item">{{ etiquetaEstado(item) }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Proveedor</mat-label>
          <mat-select [(ngModel)]="proveedorId" name="proveedorId">
            <mat-option [value]="null">Todos</mat-option>
            @for (proveedor of proveedores(); track proveedor.id) {
              <mat-option [value]="proveedor.id">{{ proveedor.nombre }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Desde</mat-label>
          <input matInput type="date" [(ngModel)]="desde" name="desde">
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Hasta</mat-label>
          <input matInput type="date" [(ngModel)]="hasta" name="hasta">
        </mat-form-field>
        <div class="filter-actions">
          <button mat-flat-button type="submit"><mat-icon>search</mat-icon> Filtrar</button>
          <button mat-button type="button" (click)="limpiarFiltros()">Limpiar</button>
        </div>
      </form>

      @if (loading()) {
        <div class="state-panel" role="status"><mat-spinner diameter="36"></mat-spinner><span>Cargando órdenes…</span></div>
      } @else if (error()) {
        <div class="state-panel error" role="alert">
          <mat-icon>error_outline</mat-icon>
          <span>{{ error() }}</span>
          <button mat-stroked-button type="button" (click)="cargar()">Reintentar</button>
        </div>
      } @else if (ordenes().length === 0) {
        <div class="state-panel" role="status"><mat-icon>inbox</mat-icon><span>No hay órdenes que coincidan con los filtros.</span></div>
      } @else {
        <div class="table-wrap">
          <table>
            <thead>
              <tr><th>Número</th><th>Proveedor</th><th>Estado</th><th>Fecha esperada</th><th class="numeric">Total</th><th>Acciones</th></tr>
            </thead>
            <tbody>
              @for (orden of ordenes(); track orden.id) {
                <tr [class.selected]="detalle()?.id === orden.id">
                  <td><strong>{{ orden.numeroOrden }}</strong></td>
                  <td>{{ orden.proveedorNombre }}</td>
                  <td><span class="status" [attr.data-status]="orden.estado">{{ etiquetaEstado(orden.estado) }}</span></td>
                  <td>{{ orden.fechaEsperadaUtc ? (orden.fechaEsperadaUtc | date:'mediumDate') : '—' }}</td>
                  <td class="numeric">{{ orden.total | currency:orden.moneda:'symbol-narrow':'1.2-2' }}</td>
                  <td class="row-actions">
                    <button mat-stroked-button type="button" (click)="verDetalle(orden.id)"><mat-icon>visibility</mat-icon> Ver</button>
                    @if (esBorrador(orden) && permisosRuntime.puede('Compras', 'Editar')) {
                      <button mat-stroked-button type="button" (click)="editarOrden(orden.id)"><mat-icon>edit</mat-icon> Editar</button>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
        <mat-paginator
          [length]="totalCount()"
          [pageIndex]="page - 1"
          [pageSize]="pageSize"
          [pageSizeOptions]="[10, 25, 50, 100]"
          (page)="cambiarPagina($event)"
          aria-label="Paginación de órdenes de compra">
        </mat-paginator>
      }

      @if (detalleLoading()) {
        <div class="detail-panel state-panel" role="status"><mat-spinner diameter="32"></mat-spinner><span>Cargando detalle…</span></div>
      } @else if (detalleError()) {
        <div class="detail-panel state-panel error" role="alert">{{ detalleError() }}</div>
      } @else if (detalle(); as orden) {
        <article class="detail-panel" aria-labelledby="detail-title">
          <div class="detail-heading">
            <div><p class="eyebrow">Detalle</p><h2 id="detail-title">{{ orden.numeroOrden }}</h2></div>
            <div class="row-actions">
              @if (esBorrador(orden) && permisosRuntime.puede('Compras', 'Editar')) {
                <button mat-stroked-button type="button" (click)="editarOrden(orden.id)" [disabled]="accionLoading()"><mat-icon>edit</mat-icon> Editar</button>
              }
              <button mat-icon-button type="button" (click)="cerrarDetalle()" aria-label="Cerrar detalle" [disabled]="accionLoading()"><mat-icon>close</mat-icon></button>
            </div>
          </div>

          @if (accionError()) {
            <div class="action-feedback error" role="alert"><mat-icon>error_outline</mat-icon><span>{{ accionError() }}</span></div>
          } @else if (accionMensaje()) {
            <div class="action-feedback success" role="status"><mat-icon>check_circle</mat-icon><span>{{ accionMensaje() }}</span></div>
          }

          <dl class="summary-grid">
            <div><dt>Proveedor</dt><dd>{{ orden.proveedorNombre }}</dd></div>
            <div><dt>Estado</dt><dd>{{ etiquetaEstado(orden.estado) }}</dd></div>
            <div><dt>Moneda</dt><dd>{{ orden.moneda }}</dd></div>
            <div><dt>Total</dt><dd>{{ orden.total | currency:orden.moneda:'symbol-narrow':'1.2-2' }}</dd></div>
            <div><dt>Solicitud origen</dt><dd>{{ orden.solicitudCompraId || 'Sin vínculo' }}</dd></div>
            <div><dt>Fecha esperada</dt><dd>{{ orden.fechaEsperadaUtc ? (orden.fechaEsperadaUtc | date:'mediumDate') : 'Sin definir' }}</dd></div>
          </dl>

          <div class="lifecycle-actions" aria-label="Acciones de ciclo de vida de la orden">
            @if (esBorrador(orden) && permisosRuntime.puede('Compras', 'Confirmar')) {
              <button mat-flat-button type="button" (click)="enviarAprobacion(orden)" [disabled]="accionLoading()">
                <mat-icon>forward_to_inbox</mat-icon>
                Enviar a aprobación
              </button>
            }
            @if (esPendienteAprobacion(orden) && permisosRuntime.puede('Compras', 'Aprobar')) {
              <button mat-flat-button type="button" (click)="aprobar(orden)" [disabled]="accionLoading()">
                <mat-icon>verified</mat-icon>
                Aprobar
              </button>
            }
            @if (esCancelable(orden) && permisosRuntime.puede('Compras', 'Anular')) {
              <button mat-stroked-button type="button" (click)="cancelar(orden)" [disabled]="accionLoading()">
                <mat-icon>cancel</mat-icon>
                Cancelar
              </button>
            }
            @if (accionLoading()) {
              <span class="action-progress" role="status"><mat-spinner diameter="22"></mat-spinner> Procesando…</span>
            }
          </div>

          <div class="detail-lines" role="region" aria-label="Líneas de la orden">
            @for (linea of orden.detalles; track linea.id) {
              <div class="line-card">
                <div>
                  <strong>{{ linea.productoNombreSnapshot || ('Producto #' + linea.productoId) }}</strong>
                  <span>{{ descripcionVariante(linea) }}</span>
                </div>
                <div class="line-totals"><span>{{ linea.cantidadOrdenada }} × {{ linea.precioUnitario | number:'1.2-2' }}</span><strong>{{ linea.total | currency:orden.moneda:'symbol-narrow':'1.2-2' }}</strong></div>
              </div>
            }
          </div>
        </article>
      }
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    :host { display: block; }
    .page-shell { display: grid; gap: 1.25rem; }
    .page-header, .detail-heading { display: flex; justify-content: space-between; gap: 1rem; align-items: flex-start; }
    .page-header h1, .detail-heading h2 { margin: .1rem 0 .35rem; }
    .page-header p { margin: 0; max-width: 70ch; color: var(--text-secondary, #5f6368); }
    .eyebrow { margin: 0; font-size: .75rem; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color: var(--primary-color, #3157d5); }
    .filters { display: grid; grid-template-columns: repeat(5, minmax(140px, 1fr)); gap: .75rem; align-items: start; }
    .filter-actions, .row-actions, .lifecycle-actions { display: flex; gap: .5rem; align-items: center; flex-wrap: wrap; }
    .filter-actions { grid-column: 1 / -1; }
    .state-panel { min-height: 9rem; display: flex; align-items: center; justify-content: center; gap: .75rem; border: 1px dashed var(--border-color, #d6dae3); border-radius: 1rem; padding: 1rem; }
    .state-panel.error { color: var(--warn-color, #b3261e); }
    .table-wrap { overflow-x: auto; border: 1px solid var(--border-color, #e1e4ea); border-radius: 1rem; }
    table { width: 100%; border-collapse: collapse; min-width: 900px; }
    th, td { padding: .85rem 1rem; text-align: left; border-bottom: 1px solid var(--border-color, #eceff3); }
    th { font-size: .78rem; text-transform: uppercase; letter-spacing: .04em; }
    tr:last-child td { border-bottom: 0; }
    tr.selected { background: var(--surface-variant, #f5f7ff); }
    .numeric { text-align: right; }
    .status { display: inline-flex; border-radius: 999px; padding: .25rem .6rem; background: var(--surface-variant, #eef1f5); font-size: .8rem; font-weight: 700; }
    .status[data-status='Aprobada'], .status[data-status='3'] { background: #e6f4ea; color: #176b36; }
    .status[data-status='Cancelada'], .status[data-status='4'] { background: #fde8e7; color: #9b1c16; }
    .status[data-status='PendienteAprobacion'], .status[data-status='2'] { background: #fff4d6; color: #805900; }
    .detail-panel { border: 1px solid var(--border-color, #dfe3ea); border-radius: 1rem; padding: 1rem; background: var(--surface-color, #fff); }
    .summary-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: .75rem; margin: 1rem 0; }
    .summary-grid div { padding: .75rem; border-radius: .75rem; background: var(--surface-variant, #f6f7f9); }
    dt { font-size: .75rem; color: var(--text-secondary, #62666d); }
    dd { margin: .25rem 0 0; font-weight: 600; }
    .action-feedback { display: flex; align-items: center; gap: .5rem; margin-top: .85rem; padding: .7rem .8rem; border-radius: .75rem; }
    .action-feedback.error { color: #9b1c16; background: #fde8e7; }
    .action-feedback.success { color: #176b36; background: #e6f4ea; }
    .lifecycle-actions { padding: .85rem 0 1rem; border-top: 1px solid var(--border-color, #eceff3); }
    .action-progress { display: inline-flex; gap: .45rem; align-items: center; color: var(--text-secondary, #62666d); }
    .detail-lines { display: grid; gap: .5rem; }
    .line-card { display: flex; justify-content: space-between; gap: 1rem; padding: .75rem 0; border-top: 1px solid var(--border-color, #eceff3); }
    .line-card span { display: block; margin-top: .25rem; color: var(--text-secondary, #62666d); }
    .line-totals { text-align: right; }
    @media (max-width: 900px) { .filters { grid-template-columns: repeat(2, minmax(0, 1fr)); } .summary-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); } }
    @media (max-width: 600px) { .page-header, .detail-heading, .line-card { flex-direction: column; } .filters, .summary-grid { grid-template-columns: 1fr; } .row-actions { flex-wrap: wrap; } .line-totals { text-align: left; } }
  `]
})
export class OrdenesCompraShellComponent implements OnInit, OnDestroy {
  readonly ordenes = signal<OrdenCompra[]>([]);
  readonly proveedores = signal<Proveedor[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly detalle = signal<OrdenCompra | null>(null);
  readonly detalleLoading = signal(false);
  readonly detalleError = signal<string | null>(null);
  readonly accionLoading = signal(false);
  readonly accionError = signal<string | null>(null);
  readonly accionMensaje = signal<string | null>(null);

  readonly estados: EstadoOrdenCompraNombre[] = ['Borrador', 'PendienteAprobacion', 'Aprobada', 'Cancelada'];
  page = 1;
  pageSize = 10;
  numero = '';
  estado: EstadoOrdenCompraNombre | '' = '';
  proveedorId: number | null = null;
  desde = '';
  hasta = '';

  private listaSubscription?: Subscription;
  private detalleSubscription?: Subscription;
  private proveedoresSubscription?: Subscription;
  private accionSubscription?: Subscription;
  private secuenciaLista = 0;
  private secuenciaDetalle = 0;

  constructor(
    private readonly ordenService: OrdenCompraService,
    private readonly proveedorService: ProveedorService,
    private readonly router: Router,
    public readonly permisosRuntime: PermisosRuntimeService,
    private readonly alerts: AppAlertService
  ) {}

  ngOnInit(): void {
    this.proveedoresSubscription = this.proveedorService.getActivos().subscribe({
      next: response => this.proveedores.set(response.success ? (response.data ?? []) : []),
      error: () => this.proveedores.set([])
    });
    this.cargar();
  }

  ngOnDestroy(): void {
    this.listaSubscription?.unsubscribe();
    this.detalleSubscription?.unsubscribe();
    this.proveedoresSubscription?.unsubscribe();
    this.accionSubscription?.unsubscribe();
  }

  cargar(): void {
    const secuencia = ++this.secuenciaLista;
    this.listaSubscription?.unsubscribe();
    this.loading.set(true);
    this.error.set(null);
    this.listaSubscription = this.ordenService.getPaged({
      page: this.page,
      pageSize: this.pageSize,
      numero: this.numero || null,
      estado: this.estado || null,
      proveedorId: this.proveedorId,
      desde: this.desde || null,
      hasta: this.hasta || null,
      sortBy: 'FechaCreacion',
      sortDirection: 'desc'
    }).pipe(finalize(() => {
      if (secuencia === this.secuenciaLista) this.loading.set(false);
    })).subscribe({
      next: response => {
        if (secuencia !== this.secuenciaLista) return;
        if (!response.success || !response.data) {
          this.ordenes.set([]);
          this.totalCount.set(0);
          this.error.set(response.message || 'No fue posible cargar las órdenes de compra.');
          return;
        }
        this.ordenes.set(response.data.items ?? []);
        this.totalCount.set(response.data.totalCount ?? 0);
      },
      error: () => {
        if (secuencia !== this.secuenciaLista) return;
        this.ordenes.set([]);
        this.totalCount.set(0);
        this.error.set('No fue posible cargar las órdenes de compra. Intenta nuevamente.');
      }
    });
  }

  aplicarFiltros(): void {
    this.page = 1;
    this.cerrarDetalle();
    this.cargar();
  }

  limpiarFiltros(): void {
    this.numero = '';
    this.estado = '';
    this.proveedorId = null;
    this.desde = '';
    this.hasta = '';
    this.aplicarFiltros();
  }

  cambiarPagina(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.cerrarDetalle();
    this.cargar();
  }

  nuevaOrden(): void { void this.router.navigate(['/ordenes-compra/nueva']); }

  editarOrden(id: number): void {
    if (!Number.isInteger(id) || id <= 0) return;
    void this.router.navigate(['/ordenes-compra', id, 'editar']);
  }

  esBorrador(orden: OrdenCompra): boolean { return orden.estado === 1 || orden.estado === 'Borrador'; }

  esPendienteAprobacion(orden: OrdenCompra): boolean {
    return orden.estado === 2 || orden.estado === 'PendienteAprobacion';
  }

  esCancelable(orden: OrdenCompra): boolean {
    return this.esBorrador(orden) || this.esPendienteAprobacion(orden);
  }

  verDetalle(id: number): void {
    if (!Number.isInteger(id) || id <= 0) return;
    const secuencia = ++this.secuenciaDetalle;
    this.detalleSubscription?.unsubscribe();
    this.limpiarFeedbackAccion();
    this.detalleLoading.set(true);
    this.detalleError.set(null);
    this.detalleSubscription = this.ordenService.getById(id).pipe(finalize(() => {
      if (secuencia === this.secuenciaDetalle) this.detalleLoading.set(false);
    })).subscribe({
      next: response => {
        if (secuencia !== this.secuenciaDetalle) return;
        if (!response.success || !response.data) {
          this.detalle.set(null);
          this.detalleError.set(response.message || 'No fue posible cargar el detalle de la orden.');
          return;
        }
        this.detalle.set(response.data);
      },
      error: () => {
        if (secuencia !== this.secuenciaDetalle) return;
        this.detalle.set(null);
        this.detalleError.set('No fue posible cargar el detalle de la orden.');
      }
    });
  }

  async enviarAprobacion(orden: OrdenCompra): Promise<void> {
    if (!this.esBorrador(orden) || !this.permisosRuntime.puede('Compras', 'Confirmar') || this.accionLoading()) return;
    const confirmado = await this.alerts.confirmar({
      titulo: 'Enviar orden a aprobación',
      mensaje: `La orden ${orden.numeroOrden} dejará de ser editable como borrador.`,
      detalle: 'La operación conserva sus líneas y totales, pero inicia el flujo de aprobación.',
      tipo: 'advertencia',
      confirmarTexto: 'Enviar a aprobación'
    });
    if (!confirmado) return;
    this.ejecutarAccion(
      () => this.ordenService.enviarAprobacion(orden.id),
      'Orden enviada a aprobación correctamente.'
    );
  }

  async aprobar(orden: OrdenCompra): Promise<void> {
    if (!this.esPendienteAprobacion(orden) || !this.permisosRuntime.puede('Compras', 'Aprobar') || this.accionLoading()) return;
    const confirmado = await this.alerts.confirmar({
      titulo: 'Aprobar orden de compra',
      mensaje: `¿Aprobar la orden ${orden.numeroOrden}?`,
      detalle: 'La aprobación no recibe mercancía ni modifica inventario; el impacto ocurre en la recepción autorizada.',
      confirmarTexto: 'Aprobar orden'
    });
    if (!confirmado) return;
    this.ejecutarAccion(
      () => this.ordenService.aprobar(orden.id),
      'Orden de compra aprobada correctamente.'
    );
  }

  async cancelar(orden: OrdenCompra): Promise<void> {
    if (!this.esCancelable(orden) || !this.permisosRuntime.puede('Compras', 'Anular') || this.accionLoading()) return;
    const motivo = await this.alerts.solicitarTexto({
      titulo: 'Cancelar orden de compra',
      mensaje: `La orden ${orden.numeroOrden} se marcará como cancelada y no podrá continuar su ciclo.`,
      detalle: 'Indica un motivo trazable para la auditoría.',
      tipo: 'peligro',
      entrada: { etiqueta: 'Motivo de cancelación', requerida: true },
      confirmarTexto: 'Cancelar orden'
    });
    if (!motivo) {
      this.accionMensaje.set(null);
      this.accionError.set('La cancelación exige un motivo.');
      return;
    }
    this.ejecutarAccion(
      () => this.ordenService.cancelar(orden.id, motivo),
      'Orden de compra cancelada correctamente.'
    );
  }

  cerrarDetalle(): void {
    this.secuenciaDetalle++;
    this.detalleSubscription?.unsubscribe();
    this.accionSubscription?.unsubscribe();
    this.detalle.set(null);
    this.detalleLoading.set(false);
    this.detalleError.set(null);
    this.accionLoading.set(false);
    this.limpiarFeedbackAccion();
  }

  etiquetaEstado(estado: EstadoOrdenCompra): string {
    switch (estado) {
      case 1:
      case 'Borrador':
        return 'Borrador';
      case 2:
      case 'PendienteAprobacion':
        return 'Pendiente de aprobación';
      case 3:
      case 'Aprobada':
        return 'Aprobada';
      case 4:
      case 'Cancelada':
        return 'Cancelada';
    }
  }

  descripcionVariante(linea: OrdenCompra['detalles'][number]): string {
    return [linea.productoSkuSnapshot, linea.productoMarcaSnapshot, linea.productoModeloSnapshot, linea.productoColorSnapshot, linea.productoTallaSnapshot]
      .filter((valor): valor is string => Boolean(valor?.trim()))
      .join(' · ') || 'Sin atributos de variante';
  }

  private ejecutarAccion(
    accion: () => ReturnType<OrdenCompraService['aprobar']>,
    mensajeExito: string
  ): void {
    this.accionSubscription?.unsubscribe();
    this.accionLoading.set(true);
    this.limpiarFeedbackAccion();
    this.accionSubscription = accion().pipe(finalize(() => this.accionLoading.set(false))).subscribe({
      next: response => {
        if (!response.success || !response.data) {
          this.accionError.set(response.message || 'La operación fue rechazada por el servidor.');
          return;
        }
        this.detalle.set(response.data);
        this.reemplazarOrdenEnLista(response.data);
        this.accionMensaje.set(response.message || mensajeExito);
      },
      error: error => {
        const mensaje = typeof error?.error?.detail === 'string'
          ? error.error.detail
          : typeof error?.error?.message === 'string'
            ? error.error.message
            : 'No fue posible completar la operación. Intenta nuevamente.';
        this.accionError.set(mensaje);
      }
    });
  }

  private reemplazarOrdenEnLista(actualizada: OrdenCompra): void {
    this.ordenes.update(items => items.map(item => item.id === actualizada.id ? actualizada : item));
  }

  private limpiarFeedbackAccion(): void {
    this.accionError.set(null);
    this.accionMensaje.set(null);
  }
}
