import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { environment } from '../../../environments/environment';
import { CategoriaTienda, ProductoTienda } from './varistorehn.models';
import { VARISTOREHN_PATHS } from './varistorehn.paths';

interface SeoPage {
  title: string;
  description: string;
  path: string;
  siteName: string;
  indexable: boolean;
  type?: 'website' | 'product';
  image?: string;
  imageAlt?: string;
  jsonLd?: Record<string, unknown>;
}

@Injectable({ providedIn: 'root' })
export class VaristorehnSeoService {
  private readonly document = inject(DOCUMENT);
  private readonly meta = inject(Meta);
  private readonly title = inject(Title);
  private readonly productionOrigin = 'https://solqaryn-prod.vercel.app';

  aplicarRuta(url: string, nombreMarca = 'Tienda'): void {
    const path = this.normalizarPath(url);
    const marca = this.nombreMarca(nombreMarca);

    if (path === '/' || path === VARISTOREHN_PATHS.inicio) {
      this.aplicar({
        title: `${marca} | Tecnología y compras en línea`,
        description: `Compra tecnología, accesorios y productos seleccionados en ${marca}. Explora categorías, ofertas y disponibilidad en línea.`,
        path: VARISTOREHN_PATHS.inicio,
        siteName: marca,
        indexable: true
      });
      return;
    }

    if (path === VARISTOREHN_PATHS.productos) {
      this.aplicar({
        title: `Productos | ${marca}`,
        description: `Explora el catálogo público de ${marca}, consulta precios, disponibilidad, categorías y modelos.`,
        path,
        siteName: marca,
        indexable: true
      });
      return;
    }

    if (path === VARISTOREHN_PATHS.ofertas) {
      this.aplicar({
        title: `Ofertas vigentes | ${marca}`,
        description: `Descubre promociones vigentes de ${marca} con precios y disponibilidad actualizados.`,
        path,
        siteName: marca,
        indexable: true
      });
      return;
    }

    if (path === VARISTOREHN_PATHS.categorias) {
      this.aplicar({
        title: `Categorías | ${marca}`,
        description: `Explora las categorías públicas de ${marca} y encuentra productos por tipo de compra.`,
        path,
        siteName: marca,
        indexable: true
      });
      return;
    }

    if (/^\/varistorehn\/producto\/[^/]+$/.test(path)) {
      this.aplicar({
        title: `Producto | ${marca}`,
        description: `Consulta información, precio y disponibilidad de este producto en ${marca}.`,
        path,
        siteName: marca,
        indexable: true,
        type: 'product'
      });
      return;
    }

    if (/^\/varistorehn\/categoria\/[^/]+$/.test(path)) {
      this.aplicar({
        title: `Categoría | ${marca}`,
        description: `Explora productos de esta categoría en ${marca}.`,
        path,
        siteName: marca,
        indexable: true
      });
      return;
    }

    this.aplicarNoIndex(marca);
  }

  aplicarProducto(
    producto: ProductoTienda,
    nombreMarca: string,
    imagen?: string,
    precio?: number,
    disponible?: boolean,
    moneda = 'HNL'
  ): void {
    const marca = this.nombreMarca(nombreMarca);
    const descripcion = this.descripcion(
      producto.descripcion,
      `Compra ${producto.nombre} en ${marca}. Consulta precio, disponibilidad y opciones del producto.`
    );
    const image = this.urlAbsoluta(imagen || producto.imagenes[0] || '');
    const precioSeo = Number.isFinite(precio) && (precio || 0) > 0 ? Number(precio) : producto.precio;
    const disponibilidad = disponible ?? producto.disponible;

    this.aplicar({
      title: `${producto.nombre} | ${marca}`,
      description: descripcion,
      path: VARISTOREHN_PATHS.producto(producto.slug),
      siteName: marca,
      indexable: true,
      type: 'product',
      image,
      imageAlt: producto.nombre,
      jsonLd: {
        '@context': 'https://schema.org',
        '@type': 'Product',
        name: producto.nombre,
        description: descripcion,
        sku: producto.sku || undefined,
        brand: producto.marca ? { '@type': 'Brand', name: producto.marca } : undefined,
        category: producto.categoria || undefined,
        image: image || undefined,
        url: this.urlCanonica(VARISTOREHN_PATHS.producto(producto.slug)),
        offers: precioSeo > 0 ? {
          '@type': 'Offer',
          priceCurrency: moneda || 'HNL',
          price: precioSeo.toFixed(2),
          availability: disponibilidad ? 'https://schema.org/InStock' : 'https://schema.org/OutOfStock',
          url: this.urlCanonica(VARISTOREHN_PATHS.producto(producto.slug))
        } : undefined
      }
    });
  }

  aplicarCategoria(categoria: CategoriaTienda, nombreMarca: string): void {
    const marca = this.nombreMarca(nombreMarca);
    this.aplicar({
      title: `${categoria.nombre} | ${marca}`,
      description: this.descripcion(
        categoria.descripcion,
        `Explora productos de ${categoria.nombre} en ${marca}. Consulta disponibilidad y opciones del catálogo público.`
      ),
      path: VARISTOREHN_PATHS.categoria(categoria.slug),
      siteName: marca,
      indexable: true
    });
  }

  aplicarNoIndex(nombreMarca = 'Tienda'): void {
    const marca = this.nombreMarca(nombreMarca);
    this.title.setTitle(`${marca} | Acceso privado`);
    this.meta.updateTag({ name: 'robots', content: 'noindex,nofollow,noarchive' });
    this.meta.updateTag({ name: 'googlebot', content: 'noindex,nofollow,noarchive' });
    this.quitarMeta("name='description'");
    this.limpiarSocial();
    this.establecerCanonica('');
    this.establecerJsonLd(undefined);
  }

  private aplicar(page: SeoPage): void {
    const indexable = page.indexable && this.entornoIndexable();
    const canonical = this.urlCanonica(page.path);
    const title = this.limitar(page.title, 70);
    const description = this.descripcion(page.description, 'Tienda');

    this.title.setTitle(title);
    this.meta.updateTag({ name: 'description', content: description });
    this.meta.updateTag({
      name: 'robots',
      content: indexable ? 'index,follow,max-image-preview:large,max-snippet:-1,max-video-preview:-1' : 'noindex,nofollow,noarchive'
    });
    this.meta.updateTag({
      name: 'googlebot',
      content: indexable ? 'index,follow,max-image-preview:large' : 'noindex,nofollow,noarchive'
    });

    this.meta.updateTag({ property: 'og:locale', content: 'es_HN' }, "property='og:locale'");
    this.meta.updateTag({ property: 'og:type', content: page.type === 'product' ? 'product' : 'website' }, "property='og:type'");
    this.meta.updateTag({ property: 'og:title', content: title }, "property='og:title'");
    this.meta.updateTag({ property: 'og:description', content: description }, "property='og:description'");
    this.meta.updateTag({ property: 'og:url', content: canonical }, "property='og:url'");
    this.meta.updateTag({ property: 'og:site_name', content: page.siteName }, "property='og:site_name'");

    this.meta.updateTag({ name: 'twitter:card', content: page.image ? 'summary_large_image' : 'summary' });
    this.meta.updateTag({ name: 'twitter:title', content: title });
    this.meta.updateTag({ name: 'twitter:description', content: description });

    if (page.image) {
      this.meta.updateTag({ property: 'og:image', content: page.image }, "property='og:image'");
      this.meta.updateTag({ property: 'og:image:alt', content: page.imageAlt || title }, "property='og:image:alt'");
      this.meta.updateTag({ name: 'twitter:image', content: page.image });
      this.meta.updateTag({ name: 'twitter:image:alt', content: page.imageAlt || title });
    } else {
      this.quitarMeta("property='og:image'");
      this.quitarMeta("property='og:image:alt'");
      this.quitarMeta("name='twitter:image'");
      this.quitarMeta("name='twitter:image:alt'");
    }

    this.establecerCanonica(canonical);
    this.establecerJsonLd(page.jsonLd);
  }

  private entornoIndexable(): boolean {
    if (!environment.production) return false;
    const host = this.document.defaultView?.location.hostname.toLowerCase() || '';
    return host === 'solqaryn-prod.vercel.app';
  }

  private urlCanonica(path: string): string {
    return path ? `${this.productionOrigin}${path.startsWith('/') ? path : `/${path}`}` : '';
  }

  private urlAbsoluta(value: string): string {
    const url = value.trim();
    if (!url) return '';
    if (/^https?:\/\//i.test(url)) return url;
    const base = this.urlCanonica('/');
    try { return new URL(url, base).toString(); } catch { return ''; }
  }

  private establecerCanonica(url: string): void {
    this.document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]')?.remove();
    if (!url) return;
    const link = this.document.createElement('link');
    link.rel = 'canonical';
    link.href = url;
    this.document.head.appendChild(link);
  }

  private establecerJsonLd(data?: Record<string, unknown>): void {
    this.document.head.querySelector<HTMLScriptElement>('script[data-varistorehn-seo="jsonld"]')?.remove();
    if (!data) return;
    const script = this.document.createElement('script');
    script.type = 'application/ld+json';
    script.dataset['varistorehnSeo'] = 'jsonld';
    script.textContent = JSON.stringify(data, (_key, value) => value === undefined ? undefined : value);
    this.document.head.appendChild(script);
  }

  private limpiarSocial(): void {
    [
      "property='og:title'", "property='og:description'", "property='og:url'", "property='og:type'",
      "property='og:site_name'", "property='og:locale'", "property='og:image'", "property='og:image:alt'",
      "name='twitter:card'", "name='twitter:title'",
      "name='twitter:description'", "name='twitter:image'", "name='twitter:image:alt'"
    ].forEach(selector => this.quitarMeta(selector));
  }

  private quitarMeta(selector: string): void {
    this.meta.removeTag(selector);
  }

  private normalizarPath(url: string): string {
    const value = (url || '/').split(/[?#]/, 1)[0] || '/';
    return value.length > 1 ? value.replace(/\/+$/, '') : value;
  }

  private nombreMarca(nombre: string): string {
    const limpio = nombre.trim();
    if (!limpio || /administrativ[oa]/i.test(limpio)) return 'Tienda';
    return this.limitar(limpio, 50);
  }

  private descripcion(valor: string | undefined | null, fallback: string): string {
    const limpio = (valor || '').replace(/\s+/g, ' ').trim();
    return this.limitar(limpio || fallback, 160);
  }

  private limitar(valor: string, maximo: number): string {
    const limpio = valor.replace(/\s+/g, ' ').trim();
    if (limpio.length <= maximo) return limpio;
    return `${limpio.slice(0, Math.max(1, maximo - 1)).trimEnd()}…`;
  }
}
