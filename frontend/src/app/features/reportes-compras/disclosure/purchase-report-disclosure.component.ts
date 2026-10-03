import { Component, Input } from '@angular/core';

import { FeedbackStateComponent } from '../../../shared/feedback-state/feedback-state.component';

export type PurchaseReportState = 'loading' | 'empty' | 'error' | 'loaded';

@Component({
  selector: 'app-purchase-report-disclosure',
  standalone: true,
  imports: [FeedbackStateComponent],
  template: `
    <div class="purchase-report-disclosure">
      @if (state === 'loading') {
        <app-feedback-state type="loading" title="Cargando reporte de compras..." message="Obteniendo la información de compras y facturación."></app-feedback-state>
      }

      @if (state === 'empty') {
        <app-feedback-state type="empty" title="No hay datos" message="No se encontraron registros de compras para los criterios seleccionados."></app-feedback-state>
      }

      @if (state === 'error') {
        <app-feedback-state type="error" title="Error al cargar el reporte" [message]="errorMessage || 'Ocurrió un problema al obtener los datos del reporte.'"></app-feedback-state>
      }

      @if (state === 'loaded') {
        <section class="interpretation-notes" aria-labelledby="notes-title">
          <h3 id="notes-title">Notas de Interpretación</h3>
          <ul>
            <li>
              <strong>Moneda de Orden vs Facturación:</strong> Los valores reflejan la moneda en la que se generó la orden frente a la moneda de la factura. No se calculan variaciones de precio cuando estas monedas son incompatibles.
            </li>
            <li>
              <strong>Valores Persistidos:</strong> Las cantidades de recepción, devoluciones y la evaluación de calidad se presentan exactamente como hechos persistidos en el sistema en el momento de su registro.
            </li>
          </ul>
        </section>
      }
    </div>
  `,
  styles: [`
    .purchase-report-disclosure {
      margin-top: 1rem;
    }
    .interpretation-notes {
      background-color: #f9fafb;
      border-left: 4px solid #3b82f6;
      padding: 1rem;
      border-radius: 0.25rem;
    }
    .interpretation-notes h3 {
      margin-top: 0;
      font-size: 1.125rem;
      font-weight: 600;
      color: #1f2937;
    }
    .interpretation-notes ul {
      margin: 0;
      padding-left: 1.5rem;
      color: #4b5563;
    }
    .interpretation-notes li {
      margin-bottom: 0.5rem;
    }
    .interpretation-notes li:last-child {
      margin-bottom: 0;
    }
  `]
})
export class PurchaseReportDisclosureComponent {
  @Input({ required: true }) state: PurchaseReportState = 'loading';
  @Input() errorMessage = '';
}
