import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptsDir = path.dirname(fileURLToPath(import.meta.url));
const frontendDir = path.resolve(scriptsDir, '..');
const featureDir = path.join(frontendDir, 'src/app/features/storefront');

const read = (name) => readFile(path.join(featureDir, name), 'utf8');
const [
  headerTs,
  headerHtml,
  headerScss,
  storefrontTs,
  storefrontHtml,
  storefrontScss,
  storefrontResponsiveScss,
  appRoutes
] = await Promise.all([
  read('storefront-header.component.ts'),
  read('storefront-header.component.html'),
  read('storefront-header.component.scss'),
  read('storefront.component.ts'),
  read('storefront.component.html'),
  read('storefront.component.scss'),
  read('storefront.responsive.scss'),
  readFile(path.join(frontendDir, 'src/app/app.routes.ts'), 'utf8')
]);

const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };

expect(headerTs.includes('STOREFRONT_PATHS'), 'El header debe construir navegación desde STOREFRONT_PATHS.');
expect(headerTs.includes('StorefrontIdentidadService'), 'El header debe consumir la identidad comercial pública separada del shell.');
expect(headerTs.includes('showModal()'), 'El menú móvil debe usar un diálogo modal nativo para contener el foco.');
expect(headerTs.includes('telefonoWhatsapp'), 'WhatsApp debe normalizarse con la regla compartida del escaparate.');
expect(headerHtml.includes('role="search"'), 'El buscador debe conservar semántica role=search.');
expect(headerHtml.includes('aria-controls="storefront-menu-movil"'), 'El disparador móvil debe declarar aria-controls.');
expect(headerHtml.includes('[attr.aria-expanded]="menuAbierto()"'), 'El disparador móvil debe exponer aria-expanded real.');
expect(headerHtml.includes('[totalUnidades]') === false, 'El header no debe intentar enlazar inputs a sí mismo.');
expect(headerHtml.includes('{{ totalUnidades }}'), 'El contador visual del carrito debe usar el total real recibido.');
expect(headerHtml.includes('enlaceWhatsapp()'), 'Debe existir una acción secundaria de WhatsApp cuando esté configurada.');
expect(!headerHtml.includes('\\n'), 'El header público no debe renderizar escapes literales \\n en navegación o utilidades.');
expect(headerScss.includes('min-height: 44px'), 'Los controles móviles deben conservar objetivos táctiles de al menos 44px.');
expect(!/#[0-9a-f]{3,8}\b/i.test(headerScss), 'El header no debe introducir colores hexadecimales fuera del tema.');

const searchButtonRule = headerScss.match(/\.search-box button\s*\{([^}]*)\}/s)?.[1] ?? '';
expect(searchButtonRule.includes('width: var(--target-min, 44px)'), 'El botón de búsqueda debe garantizar al menos 44px de ancho.');
expect(searchButtonRule.includes('height: var(--target-min, 44px)'), 'El botón de búsqueda debe garantizar al menos 44px de alto.');

const publicStoreRoute = appRoutes.match(/\{\s*path:\s*'tienda'\s*,[\s\S]*?\},/)?.[0] ?? '';
expect(Boolean(publicStoreRoute), 'Debe existir la ruta pública /tienda.');
expect(publicStoreRoute.includes('StorefrontComponent'), 'La ruta /tienda debe cargar el escaparate público.');
expect(!publicStoreRoute.includes('canActivate'), 'La ruta pública /tienda no debe incorporar guards administrativos.');
expect(!publicStoreRoute.includes('authGuard'), 'La ruta pública /tienda no debe depender de authGuard.');
expect(!publicStoreRoute.includes('permisoGuard'), 'La ruta pública /tienda no debe depender de permisoGuard.');
expect(storefrontTs.includes('StorefrontHeaderComponent'), 'El escaparate debe importar el header público reutilizable.');
expect(storefrontHtml.includes('<app-storefront-header'), 'El escaparate debe delegar su cabecera al componente público.');
expect(!storefrontHtml.includes('<header class="store-header">'), 'La cabecera monolítica anterior debe dejar de vivir en el escaparate.');
expect(storefrontHtml.includes('(busquedaActualizada)="actualizarBusqueda($event)"'), 'El home debe recibir la búsqueda desde el header compartido.');
expect(storefrontHtml.includes('(buscarSolicitado)="buscarCatalogo()"'), 'El home comercial debe enviar la búsqueda al catálogo independiente.');
expect(storefrontTs.includes('STOREFRONT_PATHS.productos'), 'La búsqueda del home debe usar la ruta canónica del catálogo.');
expect(storefrontHtml.includes('(carritoSolicitado)="abrirCarrito()"'), 'El evento de carrito del header debe conservar un manejador explícito.');
expect(storefrontTs.includes('navigateByUrl(STOREFRONT_PATHS.carrito'), 'El manejador de carrito del home debe navegar a la ruta canónica global.');
expect(!storefrontHtml.includes('#carritoDialog') && !storefrontHtml.includes('class="cart-dialog"'), 'El home no debe reintroducir un drawer de carrito paralelo.');

const staleHeaderSelectors = [
  '.skip-link',
  '.utility-bar',
  '.utility-inner',
  '.store-header',
  '.header-main',
  '.brand-mark',
  '.search-box',
  '.cart-trigger',
  '.cart-icon',
  '.store-nav'
];

for (const [name, content] of [
  ['storefront.component.scss', storefrontScss],
  ['storefront.responsive.scss', storefrontResponsiveScss]
]) {
  for (const selector of staleHeaderSelectors) {
    expect(
      !content.includes(selector),
      `${name} no debe conservar estilos residuales del header extraído (${selector}).`
    );
  }
}

for (const selector of ['.cart-dialog', '.cart-panel', '.detail-layout', '.detail-media', '.detail-body']) {
  expect(!storefrontScss.includes(selector) && !storefrontResponsiveScss.includes(selector), `El home no debe conservar CSS legado ${selector}.`);
}

for (const [name, content] of [['header.ts', headerTs], ['header.html', headerHtml]]) {
  expect(!content.includes('authGuard'), `${name} no debe depender de authGuard.`);
  expect(!content.includes('permisoGuard'), `${name} no debe depender de permisoGuard.`);
  expect(!content.includes('ProductosListComponent'), `${name} no debe reutilizar componentes administrativos de productos.`);
  expect(!content.includes('CategoriasListComponent'), `${name} no debe reutilizar componentes administrativos de categorías.`);
}

if (failures.length) {
  console.error('Fase 1 — validación de navegación/header FALLÓ:');
  failures.forEach((failure) => console.error(`- ${failure}`));
  process.exit(1);
}

console.info('Fase 1 — navegación/header: ruta pública, targets táctiles, carrito canónico y extracción completa de estilos aprobados.');
