import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { IconoTiendaComponent } from './storefront.visual';

@Component({
  selector: 'section[appStorefrontPreviewSource]',
  standalone: true,
  imports: [IconoTiendaComponent],
  templateUrl: './storefront-preview-source.component.html',
  styleUrl: './storefront-preview-source.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StorefrontPreviewSourceComponent {
  readonly titulo = input.required<string>();
  readonly descripcion = input.required<string>();
  readonly baseDatos = input.required<boolean>();
  readonly fuenteSeleccionada = output<boolean>();
}
