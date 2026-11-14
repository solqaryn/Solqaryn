import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptsDir = path.dirname(fileURLToPath(import.meta.url));
const frontendDir = path.resolve(scriptsDir, '..');
const featureDir = path.join(frontendDir, 'src/app/features/storefront');
const readFeature = name => readFile(path.join(featureDir, name), 'utf8');

const [
  routes,
  paths,
  headerHtml,
  categoriesHtml,
  productsHtml,
  productHtml,
  cartService,
  cartHtml,
  checkoutTs,
  checkoutHtml,
  config,
  homeResponsive,
  cartScss,
  checkoutScss
] = await Promise.all([
  readFile(path.join(frontendDir, 'src/app/app.routes.ts'), 'utf8'),
  readFeature('storefront.paths.ts'),
  readFeature('storefront-header.component.html'),
  readFeature('storefront-categorias.component.html'),
  readFeature('storefront-productos.component.html'),
  readFeature('storefront-producto.component.html'),
  readFeature('storefront-carrito.service.ts'),
  readFeature('storefront-carrito.component.html'),
  readFeature('storefront-checkout.component.ts'),
  readFeature('storefront-checkout.component.html'),
  readFeature('storefront.config.ts'),
  readFeature('storefront.responsive.scss'),
  readFeature('storefront-carrito.component.scss'),
  readFeature('storefront-checkout.component.scss')
]);

const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };
const routeLine = route => routes.split('\n').find(line => line.includes(`path: '${route}'`)) || '';

const publicRoutes = [
  ['tienda', 'escaparate'],
  ['tienda/categorias', 'categorías'],
  ['tienda/categoria/:slug', 'categoría'],
  ['tienda/productos', 'catálogo'],
  ['tienda/producto/:slug', 'detalle'],
  ['tienda/carrito', 'carrito'],
  ['tienda/checkout', 'checkout'],
  ['tienda/pedido/:id', 'cierre de pedido']
];

for (const [route, label] of publicRoutes) {
  const line = routeLine(route);
  expect(Boolean(line), `MVP debe conservar la ruta pública de ${label}: /${route}.`);
  expect(!line.includes('authGuard') && !line.includes('permisoGuard') && !line.includes('canActivate'),
    `La ruta MVP de ${label} no debe exigir login administrativo.`);
}

for (const token of [
  "inicio: '/tienda'",
  "productos: '/tienda/productos'",
  "categorias: '/tienda/categorias'",
  "carrito: '/tienda/carrito'",
  "checkout: '/tienda/checkout'",
  'producto: (slug: string)',
  'categoria: (slug: string)',
  'pedido: (id: string | number)'
]) {
  expect(paths.includes(token), `Rutas canónicas MVP deben conservar: ${token}.`);
}

expect(headerHtml.includes('Navegación de tienda'), 'MVP debe conservar navegación pública en el header.');
expect(headerHtml.includes('Productos') && headerHtml.includes('Categorías') && headerHtml.includes('Mi carrito'),
  'Header MVP debe exponer catálogo, categorías y carrito.');
expect(!headerHtml.includes('\\n'), 'Header MVP no debe renderizar escapes literales \\n.');

expect(categoriesHtml.includes('categories-grid') && categoriesHtml.includes('categoria.nombre'),
  'MVP debe conservar listado público de categorías.');
expect(productsHtml.includes('CATÁLOGO PÚBLICO') && productsHtml.includes('product-grid'),
  'MVP debe conservar catálogo público.');
expect(productsHtml.includes('rutaProducto') || productsHtml.includes('/tienda/producto'),
  'Catálogo debe enlazar al detalle de producto.');

expect(productHtml.includes('price-block') && productHtml.includes('precioActual()'),
  'Detalle MVP debe mostrar precio vigente.');
expect(productHtml.includes('availability') && productHtml.includes('textoDisponibilidad()'),
  'Detalle MVP debe mostrar disponibilidad/stock.');
expect(productHtml.includes('Agregar al carrito') && productHtml.includes('[disabled]="!puedeAgregar()"'),
  'Detalle MVP debe permitir agregar al carrito y bloquear cuando no hay disponibilidad.');

expect(cartService.includes('localStorage.setItem') && cartService.includes('localStorage.getItem'),
  'Carrito MVP debe persistir entre recargas.');
expect(cartService.includes('referenciasCarrito') && cartService.includes('restaurarCarrito'),
  'Carrito persistente debe rehidratar referencias contra catálogo vigente.');
expect(cartHtml.includes('Continuar al checkout'), 'Carrito MVP debe conectar con checkout.');

expect(checkoutHtml.includes('Confirma tus datos y tu forma de compra'), 'MVP debe conservar checkout público.');
expect(checkoutTs.includes('prepararWhatsapp()') && checkoutTs.includes('confirmarSalidaWhatsapp'),
  'MVP debe permitir cierre por WhatsApp.');
expect(checkoutTs.includes("this.identidad.config().nombreComercial || 'Tienda'"),
  'WhatsApp MVP debe usar marca comercial pública.');
expect(!checkoutTs.includes('StorefrontCuentaService'), 'Cuenta de cliente no debe ser requisito del checkout MVP.');
expect(config.includes('endpointCheckoutTarjeta: null'), 'Pasarela debe seguir siendo opcional/fail-closed por defecto.');
expect(config.includes("export type ModoCarrito = 'whatsapp' | 'tarjeta' | 'ambos'"),
  'La operación debe poder cerrar inicialmente por WhatsApp sin exigir pasarela.');

for (const [name, scss] of [
  ['home', homeResponsive],
  ['carrito', cartScss],
  ['checkout', checkoutScss]
]) {
  expect(scss.includes('@media'), `Responsive básico MVP debe cubrir ${name}.`);
}
expect(homeResponsive.includes('min-width: 601px') && !/@media\s*\(\s*max-width/i.test(homeResponsive),
  'Home MVP debe usar móvil como baseline y expandirse desde 601px.');
expect(cartScss.includes('min-width:681px') && !/@media\s*\(\s*max-width/i.test(cartScss),
  'Carrito MVP debe usar móvil como baseline y expandirse desde 681px.');
expect(checkoutScss.includes('min-width: 601px') && !/@media\s*\(\s*max-width/i.test(checkoutScss),
  'Checkout MVP debe usar móvil como baseline y expandirse desde 601px.');

if (failures.length) {
  console.error('MVP Fases 0–6 — auditoría de alcance FALLÓ:');
  failures.forEach(failure => console.error(`- ${failure}`));
  process.exit(1);
}

console.info('MVP Fases 0–6 — header, categorías, catálogo, detalle, precio/stock, carrito persistente, checkout, WhatsApp y responsive aprobados.');
