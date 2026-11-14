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
  catalogRules,
  cartService,
  cartTs,
  cartHtml,
  cartScss,
  productHtml,
  homeTs,
  homeHtml,
  homeScss,
  homeResponsiveScss,
  productsTs,
  productTs,
  categoriesTs,
  categoryTs
] = await Promise.all([
  readFile(path.join(frontendDir, 'src/app/app.routes.ts'), 'utf8'),
  readFeature('storefront.paths.ts'),
  readFeature('storefront.catalog.ts'),
  readFeature('storefront-carrito.service.ts'),
  readFeature('storefront-carrito.component.ts'),
  readFeature('storefront-carrito.component.html'),
  readFeature('storefront-carrito.component.scss'),
  readFeature('storefront-producto.component.html'),
  readFeature('storefront.component.ts'),
  readFeature('storefront.component.html'),
  readFeature('storefront.component.scss'),
  readFeature('storefront.responsive.scss'),
  readFeature('storefront-productos.component.ts'),
  readFeature('storefront-producto.component.ts'),
  readFeature('storefront-categorias.component.ts'),
  readFeature('storefront-categoria.component.ts')
]);

const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };

const cartRoute = routes.split('\n').find(line => line.includes("path: 'tienda/carrito'")) || '';
expect(Boolean(cartRoute), 'Debe existir /tienda/carrito.');
expect(cartRoute.includes('StorefrontCarritoComponent'), 'La ruta /tienda/carrito debe cargar su página pública.');
expect(!cartRoute.includes('authGuard') && !cartRoute.includes('permisoGuard'), 'El carrito público no debe usar guards administrativos.');
expect(paths.includes("carrito: '/tienda/carrito'"), 'STOREFRONT_PATHS debe conservar la ruta canónica del carrito.');
expect(!catalogRules.includes('urlCheckoutSegura'), 'Las reglas puras de Fase 5 no deben conservar utilidades muertas de redirección de pago.');
expect(!catalogRules.includes('export function cambiarCantidad(items:'), 'Las reglas puras no deben conservar el helper de carrito sustituido por el store global.');

for (const required of [
  "@Injectable({ providedIn: 'root' })",
  'readonly items = this._items.asReadonly()',
  'readonly totalUnidades = computed',
  'readonly subtotal = computed',
  'readonly total = computed',
  'restaurarCarrito(originales, productos)',
  'referenciasCarrito(restaurados)',
  'storefront:carrito:v2:',
  'incrementar(clave: string)',
  'disminuir(clave: string)',
  'establecerCantidad(clave: string, unidades: number)',
  'quitar(clave: string)',
  'vaciar(): void',
  'Math.max(1, Math.min(item.stock',
  'this.document.defaultView?.localStorage.setItem'
]) {
  expect(cartService.includes(required), `El store global debe contener ${required}.`);
}

expect(!cartService.includes('limpiarAviso(): void'), 'El store no debe exponer API de avisos sin consumidores.');
expect(!cartService.includes('referencias(): ReferenciaCarrito[]'), 'El store no debe exponer referencias públicas; el checkout deriva referencias desde items rehidratados.');
expect(!/precio\s*:\s*item\.precio/.test(cartService), 'La persistencia no debe serializar precios como autoridad.');
expect(!cartService.includes('JSON.stringify(this._items'), 'localStorage nunca debe guardar ItemCarrito completo.');
expect(cartService.includes('JSON.stringify(referencias)'), 'localStorage debe guardar referencias mínimas.');

for (const [name, source] of [
  ['home', homeTs],
  ['catálogo', productsTs],
  ['detalle', productTs],
  ['categorías', categoriesTs],
  ['categoría', categoryTs],
  ['carrito', cartTs]
]) {
  expect(source.includes('StorefrontCarritoService'), `${name} debe consumir el carrito global.`);
  expect(!source.includes('localStorage'), `${name} no debe mantener un localStorage de carrito paralelo.`);
}

for (const source of [productsTs, productTs, categoriesTs, categoryTs]) {
  expect(source.includes('STOREFRONT_PATHS.carrito'), 'Las páginas públicas independientes deben navegar al carrito canónico.');
}

expect(homeTs.includes('navigateByUrl(STOREFRONT_PATHS.carrito'), 'El home debe abrir la ruta canónica del carrito, no una segunda superficie.');
expect(homeTs.includes("query.get('carrito') === '1'"), 'El puente legado ?carrito=1 debe migrar a la ruta canónica.');
expect(homeTs.includes('{ replaceUrl: true }'), 'La migración del puente legado debe reemplazar la URL temporal.');
for (const forbidden of [
  "@ViewChild('carritoDialog')",
  'carritoDialog?.nativeElement.showModal',
  'realizarPedido()',
  'pagarConTarjeta()',
  'crearCheckoutTarjeta(',
  'urlCheckoutSegura',
  'referenciasCarrito(this.carrito'
]) {
  expect(!homeTs.includes(forbidden), `El home no debe conservar lógica de carrito o checkout legada: ${forbidden}.`);
}
expect(!homeHtml.includes('#carritoDialog') && !homeHtml.includes('class="cart-dialog"'), 'El DOM del home no debe contener el drawer de carrito legado.');
expect(!homeHtml.includes('Continuar con tarjeta') && !homeHtml.includes('Pedir por WhatsApp'), 'El home no debe ejecutar el cierre del carrito; esa responsabilidad pertenece al checkout.');
expect(!homeHtml.includes('Fase 6'), 'La UI pública del home no debe exponer lenguaje interno del roadmap.');
for (const selector of ['.cart-dialog', '.cart-panel', '.cart-items', '.cart-item', '.cart-footer', '.checkout-preview', '.detail-layout', '.detail-media', '.detail-body', '.button.whatsapp', '.button.full']) {
  expect(!homeScss.includes(selector) && !homeResponsiveScss.includes(selector), `El home no debe conservar CSS legado o sin uso ${selector}.`);
}

for (const required of [
  'Mi carrito',
  'TU CARRITO ESTÁ VACÍO',
  'Explorar productos',
  'Vaciar carrito',
  'Precio unitario',
  'Cantidad',
  'Total de línea',
  'Subtotal',
  'Total del carrito',
  'carrito.total()',
  'min="1"',
  '[max]="item.stock"',
  '[disabled]="item.unidades <= 1"',
  '[disabled]="item.unidades >= item.stock"',
  'Agrega al menos un producto antes de continuar con tu compra',
  'El carrito no reserva inventario'
]) {
  expect(cartHtml.includes(required), `La página de carrito debe incluir ${required}.`);
}
expect(!cartHtml.includes('Fase 6'), 'La UI pública del carrito no debe exponer lenguaje interno del roadmap.');
expect(!cartTs.includes('puedeContinuarCheckout'), 'El carrito no debe duplicar estado de checkout; la página de checkout es la autoridad del cierre.');
expect(!cartTs.includes('crearCheckoutTarjeta'), 'La página de carrito no debe ejecutar lógica de pago.');
expect(!cartTs.includes('authGuard') && !cartTs.includes('permisoGuard'), 'El carrito público no debe importar guards administrativos.');

expect(cartScss.includes('var(--color-bg)') && cartScss.includes('var(--color-surface)') && cartScss.includes('var(--color-primary)'), 'El carrito debe usar tokens canónicos del tema.');
expect(!/#[0-9a-f]{3,8}\b/i.test(cartScss), 'Fase 5 no debe introducir colores hexadecimales propios.');
expect(!/\brgb(?:a)?\s*\(/i.test(cartScss), 'Fase 5 no debe introducir colores RGB propios.');
expect(!/\bhsl(?:a)?\s*\(/i.test(cartScss), 'Fase 5 no debe introducir colores HSL propios.');
expect(/min-height\s*:\s*44px/.test(cartScss), 'Los controles del carrito deben conservar touch targets de al menos 44 px.');
expect(cartScss.includes('@media(min-width:391px)') && !/@media\s*\(\s*max-width/i.test(cartScss), 'El carrito debe usar teléfono estrecho como baseline y expandirse con min-width.');

expect(productTs.includes('stockRestante'), 'El detalle debe descontar lo ya agregado al validar una nueva cantidad.');
expect(productTs.includes('this.carritoStore.unidadesDe'), 'El stock restante del detalle debe salir del carrito global.');
expect(productHtml.includes('[max]="stockRestante()"'), 'El input del detalle debe mostrar el stock realmente restante, no el stock total.');
expect(productHtml.includes('cantidad() >= stockRestante()'), 'El botón + del detalle debe bloquearse en el stock restante.');
expect(homeTs.includes('navigateByUrl(STOREFRONT_PATHS.producto(producto.slug))'), 'El home debe llevar productos destacados al detalle canónico por slug.');
expect(!homeTs.includes('detalleDialog?.nativeElement.showModal'), 'El home no debe abrir un modal como detalle principal.');

if (failures.length) {
  console.error('Fase 5 — validación de carrito global FALLÓ:');
  failures.forEach(failure => console.error(`- ${failure}`));
  process.exit(1);
}

console.info('Fase 5 — carrito global: ruta única, store único, persistencia mínima, stock, subtotal/total, tema y ausencia de deuda heredada aprobados.');
