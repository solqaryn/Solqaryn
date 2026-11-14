import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptsDir = path.dirname(fileURLToPath(import.meta.url));
const frontendDir = path.resolve(scriptsDir, '..');
const repoDir = path.resolve(frontendDir, '..');
const featureDir = path.join(frontendDir, 'src/app/features/storefront');
const readFeature = name => readFile(path.join(featureDir, name), 'utf8');

const [
  appRoutes,
  paths,
  service,
  catalog,
  models,
  productTs,
  productHtml,
  productScss,
  productsHtml,
  cartService,
  homeTs,
  homeHtml,
  backendController
] = await Promise.all([
  readFile(path.join(frontendDir, 'src/app/app.routes.ts'), 'utf8'),
  readFeature('storefront.paths.ts'),
  readFeature('storefront.service.ts'),
  readFeature('storefront.catalog.ts'),
  readFeature('storefront.models.ts'),
  readFeature('storefront-producto.component.ts'),
  readFeature('storefront-producto.component.html'),
  readFeature('storefront-producto.component.scss'),
  readFeature('storefront-productos.component.html'),
  readFeature('storefront-carrito.service.ts'),
  readFeature('storefront.component.ts'),
  readFeature('storefront.component.html'),
  readFile(path.join(repoDir, 'backend/src/API/Controllers/TiendaController.cs'), 'utf8')
]);

const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };

const productRouteLine = appRoutes.split('\n').find(line => line.includes("path: 'tienda/producto/:slug'")) || '';
expect(Boolean(productRouteLine), 'Debe existir la ruta pública /tienda/producto/:slug.');
expect(productRouteLine.includes('StorefrontProductoComponent'), 'La ruta de detalle debe cargar StorefrontProductoComponent.');
expect(!productRouteLine.includes('authGuard') && !productRouteLine.includes('permisoGuard'), 'El detalle público no debe usar guards administrativos.');
expect(paths.includes("producto: (slug: string) => `/tienda/producto/${encodeURIComponent(slug)}`"), 'El mapa canónico debe definir el detalle por slug.');
expect(service.includes('obtenerProductoPorSlug(slug: string)'), 'El detalle debe usar el endpoint público de producto por slug.');

for (const required of [
  'this.servicio.obtenerProductoPorSlug(slug)',
  'mapearProducto',
  'STOREFRONT_PATHS.producto(producto.slug)',
  'replaceUrl: true',
  'precioVenta',
  'StorefrontCarritoService',
  'this.carritoStore.agregar(producto, modelo, this.cantidad())',
  'this.carritoStore.hidratar(productos',
  'telefonoWhatsapp',
  'crearCatalogoEjemplo',
  'this.servicio.obtenerProductosContexto(idsPersistidos)',
  'this.servicio.obtenerCategorias()',
  'stockRestante',
  'reiniciarCantidad()',
  'cancelarSwipe()'
]) {
  expect(productTs.includes(required), `El detalle público debe integrar ${required}.`);
}

expect(productTs.includes('EstadoRecursoPublico') && models.includes("EstadoRecursoPublico = EstadoConsultaPublica | 'not-found'"), 'El detalle debe usar el contrato compartido loading/error/empty/success + not-found.');
expect(productTs.includes("this.error.set('No pudimos cargar este producto"), 'Una falla real debe quedar visible y no sustituirse por demo.');
expect(productTs.includes("if (!this.utilizarDatosBaseDatos())"), 'Demo y fuente real deben estar separados de forma explícita.');
expect(productTs.includes('this.stockSeleccionado()'), 'La cantidad debe depender del stock de la variante seleccionada.');
expect(productTs.includes('Math.min(this.stockRestante()'), 'La cantidad debe quedar acotada al stock que resta después de considerar el carrito.');
expect(productTs.includes('this.cantidad() <= this.stockRestante()'), 'El CTA debe bloquear cantidades superiores al stock restante.');
expect(!productTs.includes('localStorage'), 'El detalle no debe mantener una persistencia de carrito paralela al store global.');
expect(!productTs.includes('ProductosListComponent'), 'El detalle público no debe reutilizar el CRUD administrativo.');
expect(!productTs.includes('authGuard') && !productTs.includes('permisoGuard'), 'El componente público no debe importar guards administrativos.');
expect(!productTs.includes('crearCheckoutTarjeta('), 'Fase 4 no debe adelantar el checkout de Fase 6.');

for (const required of [
  'Migas de pan',
  'Galería de imágenes del producto',
  'Miniaturas del producto',
  'pointerdown',
  'pointerup',
  'image-position',
  'lightbox',
  'Cerrar imagen ampliada',
  'SKU',
  'Precio del producto',
  'Cantidad a agregar',
  '[max]="stockRestante()"',
  'Agregar al carrito',
  'Comprar por WhatsApp',
  'Descripción',
  'Características',
  'Productos relacionados',
  'mobile-buy-bar',
  "estado() === 'loading'",
  "estado() === 'error'",
  "estado() === 'not-found'"
]) {
  expect(productHtml.includes(required), `La plantilla de detalle debe contener ${required}.`);
}

expect(productHtml.includes('<dialog #lightbox'), 'La única ampliación modal permitida en el detalle independiente debe ser el lightbox de imágenes.');
expect(productHtml.indexOf('class="product-layout"') < productHtml.indexOf('<dialog #lightbox'), 'El contenido comercial debe vivir en la página antes del lightbox, no dentro del diálogo.');
expect(!productHtml.includes('class="detail-dialog"'), 'El detalle independiente no puede reutilizar el modal legado del home.');
expect(productsHtml.includes('Ver producto'), 'Las tarjetas del catálogo deben ofrecer Ver producto.');
expect(productsHtml.includes("'/tienda/producto/' + producto.slug"), 'Ver producto debe navegar por slug a la página independiente.');
expect(productsHtml.includes('tieneOferta(modelo)') && productsHtml.includes('precioActual(producto, modelo)'), 'El catálogo debe mostrar la misma oferta vigente por variante que el detalle.');
expect(
  homeTs.includes('abrirDetalle(producto: ProductoTienda)')
    && homeTs.includes('STOREFRONT_PATHS.producto(producto.slug)'),
  'Si el home expone un producto destacado, debe navegar al detalle canónico por slug.'
);
expect(!homeTs.includes('detalleDialog?.nativeElement.showModal'), 'El home no debe abrir un modal como experiencia principal de detalle de producto.');
expect(!homeTs.includes("@ViewChild('detalleDialog')"), 'El home no debe conservar el ViewChild del modal legado de detalle.');
expect(!homeTs.includes('productoDetalle = signal'), 'El home no debe conservar estado muerto del modal legado de detalle.');
expect(!homeHtml.includes('#detalleDialog') && !homeHtml.includes('class="detail-dialog"'), 'El DOM del home no debe contener el modal legado de detalle.');

expect(catalog.includes('export function precioVenta'), 'Debe existir una regla única de precio efectivo para detalle y carrito.');
expect(catalog.includes('precio: precioVenta(producto, modelo)'), 'El carrito debe reconstruir el precio efectivo centralizado, no un precio divergente.');
expect(cartService.includes('restaurarCarrito') && cartService.includes('referenciasCarrito') && cartService.includes('productoIdsPersistidos'), 'El carrito central debe rehidratar solo sus productos persistidos contra la fuente pública sin confiar en precios ni catálogo completo.');

expect(/object-fit\s*:\s*contain/.test(productScss), 'Las imágenes deben preservar proporción con object-fit: contain.');
expect(/touch-action\s*:\s*pan-y/.test(productScss), 'La galería móvil debe permitir swipe horizontal sin romper el scroll vertical.');
expect(productScss.includes('env(safe-area-inset-bottom)'), 'El CTA móvil fijo debe respetar el safe area inferior.');
expect(productScss.includes('.mobile-buy-bar'), 'Debe existir el CTA móvil fijo de compra.');
expect(/(?:min-)?height\s*:\s*44px/.test(productScss), 'Los controles principales deben conservar objetivos táctiles de al menos 44px.');
expect(productScss.includes('var(--color-bg)') && productScss.includes('var(--color-surface)') && productScss.includes('var(--color-primary)'), 'El detalle debe heredar los tokens canónicos del tema.');
expect(!/#[0-9a-f]{3,8}\b/i.test(productScss), 'Fase 4 no debe introducir colores hexadecimales paralelos al tema.');
expect(!/\brgb(?:a)?\s*\(/i.test(productScss), 'Fase 4 no debe introducir colores RGB paralelos al tema.');
expect(!/\bhsl(?:a)?\s*\(/i.test(productScss), 'Fase 4 no debe introducir colores HSL paralelos al tema.');
expect(!productScss.includes('--color-background') && !productScss.includes('--color-text-secondary'), 'Fase 4 no debe usar aliases de tema inexistentes.');

expect(backendController.includes('[AllowAnonymous]'), 'El controlador público de tienda debe mantenerse anónimo.');
expect(/\[HttpGet\("productos\/\{slug\}"\)\][\s\S]{0,180}GetProducto\(string slug\)/.test(backendController), 'El backend debe exponer producto público por slug.');
expect(/producto\s+is\s+null\s+\|\|\s+!producto\.Activo/.test(backendController), 'Producto inexistente o inactivo debe resolverse como no disponible desde la fuente pública.');

if (failures.length) {
  console.error('Fase 4 — validación de detalle público FALLÓ:');
  failures.forEach(failure => console.error(`- ${failure}`));
  process.exit(1);
}

console.info('Fase 4 — detalle público: URL canónica única, galería, stock restante, precio, carrito central, WhatsApp, tema y límites aprobados.');
