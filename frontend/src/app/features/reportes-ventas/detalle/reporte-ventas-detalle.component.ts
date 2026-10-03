import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, ChangeDetectionStrategy } from '@angular/core';
import { PagedResult } from '../../../core/models/api-response.model';
import { ReporteVentasDetalleDto } from '../../../core/models/reporte-ventas.models';

@Component({
  selector: 'app-reporte-ventas-detalle',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="detail-card" aria-labelledby="reporte-ventas-detalle-title">
      <h2 id="reporte-ventas-detalle-title">Detalle de ventas</h2>

      @if (cargando) {
        <p class="status" role="status" aria-live="polite">Cargando detalle de ventas…</p>
      } @else if (error) {
        <p class="error" role="alert">{{ error }}</p>
      } @else if (!detalle || detalle.items.length === 0) {
        <p class="empty" role="status">No hay ventas para los filtros seleccionados.</p>
      } @else {
        <div class="table-scroll" tabindex="0" aria-label="Tabla desplazable de detalle de ventas">
          <table>
            <caption>
              {{ detalle.totalCount }} registro{{ detalle.totalCount === 1 ? '' : 's' }} de ventas
            </caption>
            <thead>
              <tr>
                <th scope="col">Fecha</th>
                <th scope="col">Venta</th>
                <th scope="col">Cliente</th>
                <th scope="col">Vendedor</th>
                <th scope="col">Producto</th>
                <th scope="col">SKU</th>
                <th scope="col">Marca</th>
                <th scope="col">Modelo</th>
                <th scope="col">Color</th>
                <th scope="col">Talla</th>
                <th scope="col" class="numeric">Cantidad</th>
                <th scope="col" class="numeric">Precio</th>
                <th scope="col" class="numeric">Costo</th>
                <th scope="col" class="numeric">Subtotal</th>
                <th scope="col" class="numeric">Utilidad</th>
              </tr>
            </thead>
            <tbody>
              @for (fila of detalle.items; track fila.ventaId + ':' + (fila.productoVarianteId ?? fila.productoId)) {
                <tr>
                  <td>{{ fila.fecha | date:'short' }}</td>
                  <td>{{ fila.numeroVenta }}</td>
                  <td>{{ fila.clienteNombre || '—' }}</td>
                  <td>{{ fila.vendedorNombre || '—' }}</td>
                  <td>{{ fila.productoNombre }}</td>
                  <td>{{ fila.productoSku || '—' }}</td>
                  <td>{{ fila.productoMarca || '—' }}</td>
                  <td>{{ fila.productoModelo || '—' }}</td>
                  <td>{{ fila.productoColor || '—' }}</td>
                  <td>{{ fila.productoTalla || '—' }}</td>
                  <td class="numeric">{{ fila.cantidad | number:'1.0-2' }}</td>
                  <td class="numeric">{{ fila.precioUnitario | currency:'HNL':'symbol-narrow':'1.2-2' }}</td>
                  <td class="numeric">{{ fila.costoUnitario | currency:'HNL':'symbol-narrow':'1.2-2' }}</td>
                  <td class="numeric">{{ fila.subtotal | currency:'HNL':'symbol-narrow':'1.2-2' }}</td>
                  <td class="numeric">{{ fila.utilidadBruta | currency:'HNL':'symbol-narrow':'1.2-2' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <nav class="pagination" aria-label="Paginación del detalle de ventas">
          <button
            type="button"
            (click)="irPagina(detalle.page - 1)"
            [disabled]="detalle.page <= 1"
            aria-label="Página anterior">
            Anterior
          </button>
          <span aria-live="polite">Página {{ detalle.page }} de {{ detalle.totalPages }}</span>
          <button
            type="button"
            (click)="irPagina(detalle.page + 1)"
            [disabled]="detalle.page >= detalle.totalPages"
            aria-label="Página siguiente">
            Siguiente
          </button>
        </nav>
      }
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .detail-card {
      background: #fff;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 1rem;
    }
    h2 { margin: 0 0 1rem; font-size: 1.125rem; }
    .table-scroll { overflow-x: auto; max-width: 100%; }
    table { width: 100%; border-collapse: collapse; min-width: 1180px; }
    caption { text-align: left; padding: 0 0 .75rem; color: #475569; }
    th, td { padding: .65rem .75rem; border-bottom: 1px solid #e2e8f0; text-align: left; white-space: nowrap; }
    th { color: #334155; font-size: .8rem; text-transform: uppercase; letter-spacing: .02em; }
    td { color: #0f172a; }
    .numeric { text-align: right; font-variant-numeric: tabular-nums; }
    .status { color: #0369a1; }
    .error { color: #b91c1c; }
    .empty { color: #64748b; }
    .pagination { display: flex; gap: 1rem; align-items: center; justify-content: flex-end; margin-top: 1rem; }
    .pagination button { padding: .5rem .75rem; }
    @media (max-width: 720px) {
      .detail-card { padding: .75rem; }
      .pagination { justify-content: space-between; gap: .5rem; }
    }
  `]
})
export class ReporteVentasDetalleComponent {
  @Input() detalle: PagedResult<ReporteVentasDetalleDto> | null = null;
  @Input() cargando = false;
  @Input() error = '';
  @Output() paginaChange = new EventEmitter<number>();

  irPagina(pagina: number): void {
    if (!this.detalle || pagina < 1 || pagina > this.detalle.totalPages || pagina === this.detalle.page) {
      return;
    }
    this.paginaChange.emit(pagina);
  }
}
