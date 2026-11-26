import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { IconoTiendaComponent } from './storefront.visual';

@Component({
  selector: 'section[app-storefront-preview]',
  standalone: true,
  imports: [IconoTiendaComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class.catalogo]': "variante() === 'catalogo'" },
  template: `
    <div class="preview-copy">
      <app-store-icon nombre="sol" />
      <span><strong>{{ variante() === 'catalogo' ? 'Vista previa de catálogo' : 'Vista previa de detalle' }}</strong><small>{{ variante() === 'catalogo' ? 'La fuente puede alternarse solo fuera de producción.' : 'La fuente real y los fixtures permanecen separados.' }}</small></span>
    </div>
    <div class="segmented" role="group" aria-label="Origen de datos">
      <button type="button" [class.active]="!usaDatosReales()" [attr.aria-pressed]="!usaDatosReales()" (click)="fuenteCambiada.emit(false)">Datos de ejemplo</button>
      <button type="button" [class.active]="usaDatosReales()" [attr.aria-pressed]="usaDatosReales()" (click)="fuenteCambiada.emit(true)">Base de datos</button>
    </div>
  `,
  styleUrl: './storefront-preview.component.scss'
})
export class StorefrontPreviewComponent {
  readonly variante = input.required<'catalogo' | 'detalle'>();
  readonly usaDatosReales = input.required<boolean>();
  readonly fuenteCambiada = output<boolean>();
}
