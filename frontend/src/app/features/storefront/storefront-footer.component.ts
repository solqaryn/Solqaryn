import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { STOREFRONT_PATHS } from './storefront.paths';

@Component({
  selector: 'footer[appStorefrontFooter]',
  standalone: true,
  templateUrl: './storefront-footer.component.html',
  styleUrl: './storefront-footer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StorefrontFooterComponent {
  readonly nombre = input.required<string>();
  readonly texto = input.required<string>();
  readonly mostrarCopyright = input.required<boolean>();
  readonly copyrightTexto = input.required<string>();
  readonly ayuda = input('');
  readonly enlaces = { ...STOREFRONT_PATHS, contacto: STOREFRONT_PATHS.inicio + '#contacto' };
}
