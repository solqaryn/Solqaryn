import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { StorefrontIdentidadService } from './storefront-identidad.service';
import { STOREFRONT_PATHS } from './storefront.paths';

@Component({
  selector: 'footer[app-storefront-catalog-footer]',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class.catalogo]': "variante() === 'catalogo'" },
  template: `
    <div class="container footer-grid">
      <div><strong>{{ identidad.config().nombreComercial || 'Tienda' }}</strong><p>{{ identidad.config().piePaginaTexto || identidad.config().eslogan }}</p></div>
      <div><b>Tienda</b><a [href]="enlaces.inicio">Inicio</a><a [href]="enlaces.productos">Productos</a><a [href]="enlaces.ofertas">Ofertas</a><a [href]="enlaces.categorias">Categorías</a></div>
      <div><b>Ayuda</b><a [href]="enlaces.contacto">Contacto</a>@if (permiteWhatsapp()) { <span>{{ variante() === 'catalogo' ? 'Pedidos por WhatsApp' : 'Compra alternativa por WhatsApp' }}</span> }</div>
    </div>
    <div class="container footer-bottom">@if (identidad.mostrarCopyright()) { <small>{{ identidad.copyright() }}</small> }</div>
  `,
  styleUrl: './storefront-catalog-footer.component.scss'
})
export class StorefrontCatalogFooterComponent {
  readonly variante = input.required<'catalogo' | 'detalle'>();
  readonly permiteWhatsapp = input.required<boolean>();
  readonly identidad = inject(StorefrontIdentidadService);
  readonly enlaces = {
    ...STOREFRONT_PATHS,
    contacto: `${STOREFRONT_PATHS.inicio}#contacto`
  } as const;
}
