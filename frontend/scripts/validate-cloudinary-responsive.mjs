import fs from 'node:fs';
import path from 'node:path';

const root = process.cwd();
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const expect = (condition, message) => {
  if (!condition) {
    console.error(`[cloudinary-responsive] FAIL: ${message}`);
    process.exitCode = 1;
  }
};

const helper = read('src/app/shared/cloudinary-image.util.ts');
const shared = read('src/app/shared/producto-imagen/producto-imagen.component.ts');
const listado = read('src/app/features/storefront/storefront-productos.component.html');
const detalle = read('src/app/features/storefront/storefront-producto.component.html');
const home = read('src/app/features/storefront/storefront.component.html');
const categoria = read('src/app/features/storefront/storefront-categoria.component.html');
const carrito = read('src/app/features/storefront/storefront-carrito.component.html');
const catalogoService = read('../backend/src/Application/Services/CatalogoPublicoService.cs');

for (const width of [320, 480, 640, 800]) {
  expect(helper.includes(String(width)), `helper debe conservar el ancho responsive ${width}`);
}
expect(helper.includes('f_auto,q_auto,c_limit,w_'), 'helper debe aplicar f_auto,q_auto,c_limit,w_* a Cloudinary');
expect(helper.includes("parsed.hostname === 'res.cloudinary.com'"), 'URLs no Cloudinary no deben reescribirse');
expect(shared.includes('[attr.srcset]="responsiveSrcset"'), 'componente compartido debe emitir srcset');
expect(shared.includes('[attr.sizes]="responsiveSizes"'), 'componente compartido debe emitir sizes');
expect(shared.includes(`[attr.fetchpriority]="priority ? 'high' : null"`), 'fetchpriority high debe depender de prioridad explícita');

expect(listado.includes('[attr.srcset]="srcsetCloudinary(modelo.imagenes[0])"'), 'tarjetas de catálogo deben usar srcset');
expect(listado.includes('loading="lazy"') && listado.includes('decoding="async"'), 'tarjetas deben conservar lazy + async');

expect(detalle.includes('fetchpriority="high"'), 'imagen LCP de detalle debe conservar prioridad alta');
expect(detalle.includes('[attr.srcset]="srcsetCloudinary(imagenActual())"'), 'imagen principal de detalle debe usar srcset');
expect(detalle.includes('sizes="96px"'), 'miniaturas deben declarar sizes');
expect(detalle.includes('loading="lazy"') && detalle.includes('decoding="async"'), 'contenido no crítico debe conservar lazy + async');
expect(home.includes('[attr.srcset]="srcsetCloudinary(fotos(destacado)[0])"'), 'home LCP debe usar srcset');
expect(home.includes('fetchpriority="high"'), 'home debe conservar prioridad alta sólo en su LCP');
expect(categoria.includes('[attr.srcset]="srcsetCloudinary(imagen)"'), 'categoría debe usar srcset');
expect(carrito.includes('[attr.srcset]="srcsetCloudinary(item.imagen)"'), 'carrito debe usar srcset');

expect(catalogoService.includes('Task<PagedResult<TiendaProductoResumenDto>> BuscarAsync'), 'listado público debe usar DTO resumen');
expect(catalogoService.includes('ImagenPrincipalUrl = producto.ImagenPrincipalUrl'), 'DTO resumen debe incluir solo imagen principal');
expect(catalogoService.includes('includeGalleries: true'), 'detalle debe mantener galería completa');
expect(catalogoService.includes('includeGalleries: false'), 'contextos/listados no deben requerir galerías completas');

if (!process.exitCode) console.log('[cloudinary-responsive] PASS');
