import { CommonModule } from '@angular/common';
import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { ReporteVentasResumenDto } from '../../../core/models/reporte-ventas.models';

@Component({
  selector: 'app-reporte-ventas-resumen',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="summary-card" aria-label="Resumen de ventas">
      @if (cargando) {
        <p class="status" role="status">Cargando resumen…</p>
      } @else if (error) {
        <p class="error" role="alert">{{ error }}</p>
      } @else if (resumen) {
        <div class="grid">
          <div class="kpi">
            <span class="label">Importe Bruto</span>
            <span class="value">{{ resumen.importeBruto | currency:'HNL':'symbol-narrow':'1.2-2' }}</span>
          </div>
          <div class="kpi">
            <span class="label">Subtotal</span>
            <span class="value">{{ resumen.subtotal | currency:'HNL':'symbol-narrow':'1.2-2' }}</span>
          </div>
          <div class="kpi">
            <span class="label">Descuento</span>
            <span class="value text-danger">{{ resumen.descuento | currency:'HNL':'symbol-narrow':'1.2-2' }}</span>
          </div>
          <div class="kpi">
            <span class="label">Impuesto</span>
            <span class="value">{{ resumen.impuesto | currency:'HNL':'symbol-narrow':'1.2-2' }}</span>
          </div>
          <div class="kpi highlight">
            <span class="label">Total</span>
            <span class="value">{{ resumen.total | currency:'HNL':'symbol-narrow':'1.2-2' }}</span>
          </div>
          <div class="kpi">
            <span class="label">Costo Total</span>
            <span class="value">{{ resumen.costoTotal | currency:'HNL':'symbol-narrow':'1.2-2' }}</span>
          </div>
          <div class="kpi success">
            <span class="label">Utilidad Bruta</span>
            <span class="value">{{ resumen.utilidadBruta | currency:'HNL':'symbol-narrow':'1.2-2' }}</span>
          </div>
        </div>
      } @else {
        <p class="empty" role="status">No hay datos de resumen disponibles.</p>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [
    `
      .summary-card {
        background: #fff;
        border-radius: 8px;
        padding: 1.5rem;
        box-shadow: 0 1px 3px rgba(0,0,0,0.1);
        margin-bottom: 1.5rem;
      }
      .grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
        gap: 1rem;
      }
      .kpi {
        display: flex;
        flex-direction: column;
        padding: 1rem;
        background: #f8fafc;
        border-radius: 6px;
        border: 1px solid #e2e8f0;
      }
      .kpi.highlight { background: #eff6ff; border-color: #bfdbfe; }
      .kpi.success { background: #f0fdf4; border-color: #bbf7d0; }
      .label { font-size: 0.875rem; color: #64748b; margin-bottom: 0.5rem; }
      .value { font-size: 1.25rem; font-weight: 600; color: #0f172a; }
      .text-danger { color: #ef4444; }
      .status { color: #0369a1; }
      .error { color: #b91c1c; }
      .empty { color: #64748b; font-style: italic; }
    `
  ]
})
export class ReporteVentasResumenComponent {
  @Input() resumen: ReporteVentasResumenDto | null = null;
  @Input() cargando = false;
  @Input() error = '';
}
