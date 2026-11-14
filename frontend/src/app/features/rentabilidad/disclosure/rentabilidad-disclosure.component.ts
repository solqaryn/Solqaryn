import { Component, Input, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-rentabilidad-disclosure',
  standalone: true,
  template: `
    <aside class="disclosure-container" role="note" aria-labelledby="rentabilidad-disclosure-title">
      <h3 id="rentabilidad-disclosure-title" class="sr-only">Semántica del cálculo de rentabilidad</h3>
      <p class="disclosure-text"><strong>Semántica:</strong> {{ semantica }}</p>
      <p class="disclosure-text">
        El cálculo
        <strong>{{ incluyeDescuentoEncabezadoEnUtilidad ? 'incluye' : 'no incluye' }}</strong>
        el descuento global de cabecera en la utilidad bruta.
      </p>
    </aside>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .disclosure-container {
      border-inline-start: 4px solid currentColor;
      padding: 1rem;
      margin-block: 1rem;
      background: rgba(127, 127, 127, 0.08);
      border-radius: 0.25rem;
    }
    .disclosure-text { margin: 0; line-height: 1.5; }
    .disclosure-text + .disclosure-text { margin-top: 0.5rem; }
    .sr-only {
      position: absolute;
      width: 1px;
      height: 1px;
      padding: 0;
      margin: -1px;
      overflow: hidden;
      clip: rect(0, 0, 0, 0);
      white-space: nowrap;
      border: 0;
    }
  `],
})
export class RentabilidadDisclosureComponent {
  @Input({ required: true }) semantica = '';
  @Input({ required: true }) incluyeDescuentoEncabezadoEnUtilidad = false;
}
