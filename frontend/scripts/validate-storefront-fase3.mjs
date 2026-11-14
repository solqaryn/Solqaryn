import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptsDir = path.dirname(fileURLToPath(import.meta.url));
const frontendDir = path.resolve(scriptsDir, '..');
const featureDir = path.join(frontendDir, 'src/app/features/storefront');
const readFeature = name => readFile(path.join(featureDir, name), 'utf8');

const [
  appRoutes,
  paths,
  models,
  service,
  catalog,
  productsTs,
  productsHtml,
  productsScss,
  headerTs,
  categoriesTs,
  categoryTs,
  cartStore
] = await Promise.all([
  readFile(path.join(frontendDir, 'src/app/app.routes.ts'), 'utf8'),
  readFeature('storefront.paths.ts'),
  readFeature('storefront.models.ts'),
  readFeature('storefront.service.ts'),
  readFeature('storefront.catalog.ts'),
  readFeature('storefront-productos.component.ts'),
  readFeature('storefront-productos.component.html'),
  readFeature('storefront-productos.component.scss'),
  readFeature('storefront-header.component.ts'),
  readFeature('storefront-categorias.component.ts'),
  readFeature('storefront-categoria.component.ts'),
  readFeature('storefront-carrito.service.ts')
]);

const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };

const productsRouteLine = appRoutes.split('\n').find(line => line.includes("path: 'tienda/productos'")) || '';
expect(Boolean(productsRouteLine), 'Debe existir la ruta pública /tienda/productos.');
expect(productsRouteLine.includes('StorefrontProductosComponent'), 'La ruta /tienda/productos debe cargar su componente público independiente.');
expect(!productsRouteLine.includes('authGuard') && !productsRouteLine.includes('permisoGuard'), 'La ruta pública de productos no debe usar guards administrativos.');

expect(paths.includes("productos: '/tienda/productos'"), 'El mapa canónico debe conservar STOREFRONT_PATHS.productos.');
expect(models.includes('export interface ProductoTienda'), 'Fase 3 debe usar ProductoTienda como modelo visual canónico.');
expect(models.includes('export interface ProductoCatalogoResumenPublico'), 'El listado debe tener un contrato HTTP ligero separado del detalle.');
expect(models.includes('export interface ModeloCatalogoResumenPublico'), 'El listado debe exponer solo el resumen mínimo de variantes.');
expect(models.includes("export type OrdenCatalogo = 'destacados' | 'relevancia' | 'precio-asc' | 'precio-desc' | 'recientes' | 'nombre'"), 'El orden canónico del catálogo debe conservar compatibilidad y añadir relevancia/recientes.');
expect(service.includes('obtenerProductos('), 'La página de productos debe consumir la frontera pública paginada del catálogo.');
expect(service.includes('PagedResult<ProductoCatalogoResumenPublico>'), 'El listado HTTP debe tiparse con el resumen ligero y no con el DTO rico de detalle.');
expect(service.includes('obtenerProductosContexto(productoIds: number[])'), 'El carrito debe rehidratar únicamente los productos persistidos, no descargar el catálogo completo.');
expect(!service.includes('obtenerCatalogo()'), 'El cliente público no debe conservar una API que descargue todas las páginas del catálogo.');
expect(catalog.includes('export function mapearProductoResumen'), 'Las tarjetas deben mapear el contrato ligero sin depender de galerías del detalle.');
expect(catalog.includes('export function filtrarProductos'), 'Los filtros deben reutilizar la regla pura canónica.');
expect(catalog.includes('export function referenciasCarrito'), 'La persistencia debe conservar el formato canónico de referencias.');
expect(catalog.includes('export function restaurarCarrito'), 'El carrito debe restaurarse contra catálogo/precio/stock actuales.');
expect(catalog.includes('export function precioVenta'), 'Catálogo, detalle y carrito deben compartir la regla de precio efectivo cuando existe una oferta válida.');

for (const required of [
  'ProductoTienda',
  'EstadoConsultaPublica',
  'filtrarProductos',
  'this.servicio.obtenerProductos(this.pagina(), this.tamanoPagina',
  'datos.items.map(mapearProductoResumen)',
  'this.servicio.obtenerCategorias()',
  'mapearCategoriaTienda',
  'StorefrontCarritoService',
  'this.carritoStore.hidratar(productos',
  'this.servicio.obtenerProductosContexto(idsPersistidos)',
  'STOREFRONT_PATHS.productos',
  'STOREFRONT_PATHS.carrito',
  'this.route.queryParamMap'
]) {
  expect(productsTs.includes(required), `La página independiente de productos debe integrar ${required}.`);
}

expect(!productsTs.includes('localStorage'), 'El catálogo independiente no debe mantener persistencia de carrito paralela.');
expect(productsTs.includes('this.carritoStore.agregar(producto, modelo, 1)'), 'Agregar desde catálogo debe usar el carrito central.');
expect(productsTs.includes("params.get('q')"), 'La búsqueda profunda debe hidratarse desde el query param q.');
expect(productsTs.includes("params.get('categoria')"), 'El filtro de categoría debe hidratarse desde el slug público del query param categoria.');
expect(productsTs.includes("No se sustituyeron los datos reales por ejemplos"), 'Una falla del catálogo real no debe caer silenciosamente a fixtures.');
expect(productsTs.includes("const fuente: Observable<{ productos: ProductoTienda[]; total: number }> = this.utilizarDatosBaseDatos()"), 'Demo y fuente real deben permanecer separados explícitamente aun con paginación server-side.');
expect(productsTs.includes('cargarCatalogo(): void'), 'La acción de reintento del template debe ser pública y comprobable por Angular.');
expect(!productsTs.includes('private cargarCatalogo(): void'), 'El template no debe depender de un método privado.');
expect(!productsTs.includes('ProductosListComponent'), 'El catálogo público no debe reutilizar el CRUD administrativo de productos.');
expect(!productsTs.includes('authGuard') && !productsTs.includes('permisoGuard'), 'El componente público no debe depender de guards administrativos.');
expect(!productsTs.includes('obtenerProductoPorSlug('), 'La carga individual del detalle debe permanecer fuera del componente de catálogo.');
expect(!productsTs.includes('crearCheckoutTarjeta('), 'Fase 3 no debe adelantar checkout de Fase 6.');

for (const state of ['loading', 'error', 'empty']) {
  expect(productsHtml.includes(`estadoCatalogo() === '${state}'`), `La plantilla de catálogo debe representar el estado ${state}.`);
}
expect(productsHtml.includes('[attr.aria-busy]="estadoCatalogo() === \'loading\'"'), 'El catálogo debe exponer la carga mediante aria-busy.');
expect(productsHtml.includes('No encontramos coincidencias'), 'Debe distinguir catálogo vacío de filtros sin coincidencias.');
expect(productsHtml.includes('Solo disponibles'), 'Debe existir filtro de disponibilidad.');
expect(productsHtml.includes('Precio mínimo'), 'Debe existir filtro de precio mínimo.');
expect(productsHtml.includes('Precio máximo'), 'Debe existir filtro de precio máximo.');
for (const order of ['relevancia', 'precio-asc', 'precio-desc', 'recientes', 'nombre']) {
  expect(productsHtml.includes(`value="${order}"`), `Debe existir la opción de orden ${order}.`);
}
expect(productsHtml.includes('Paginación del catálogo'), 'La página independiente debe exponer paginación accesible.');
expect(productsHtml.includes("[attr.aria-label]=\"'Modelo de ' + producto.nombre\""), 'Las variantes deben poder seleccionarse con control etiquetado.');
expect(productsHtml.includes("[attr.aria-label]=\"'Agregar ' + producto.nombre\""), 'Cada tarjeta disponible debe ofrecer una acción explícita de agregar.');
expect(productsHtml.includes('destinoSaltar="#catalogo-productos"'), 'El header compartido debe saltar al contenido real de Fase 3.');
expect(productsHtml.includes("[href]=\"'/tienda/producto/' + producto.slug\""), 'Ver producto debe navegar por slug al detalle público cuando Fase 4 está activa.');
expect(productsHtml.includes('Ver producto'), 'La tarjeta debe conservar una acción explícita Ver producto.');
expect(productsHtml.includes('tieneOferta(modelo)') && productsHtml.includes('precioActual(producto, modelo)'), 'La tarjeta debe diferenciar el precio promocional autoritativo por variante cuando aplique.');

expect(!/#[0-9a-f]{3,8}\b/i.test(productsScss), 'Fase 3 no debe introducir colores hexadecimales fuera del tema global.');
expect(!/\brgb(?:a)?\s*\(/i.test(productsScss), 'Fase 3 no debe introducir colores RGB paralelos al tema.');
expect(!/\bhsl(?:a)?\s*\(/i.test(productsScss), 'Fase 3 no debe introducir colores HSL paralelos al tema.');
expect(productsScss.includes('var(--color-bg)'), 'La página debe heredar el fondo canónico del tema.');
expect(productsScss.includes('var(--color-text-muted)'), 'La página debe usar el texto secundario canónico del tema.');
expect(productsScss.includes('min-height: 44px'), 'Los controles principales deben conservar objetivos táctiles de al menos 44px.');
expect(!productsScss.includes('--color-background') && !productsScss.includes('--color-text-secondary'), 'Fase 3 no debe reintroducir aliases de tema inexistentes.');

expect(cartStore.includes('restaurarCarrito') && cartStore.includes('referenciasCarrito'), 'La persistencia/revalidación de catálogo debe vivir en el carrito central.');
expect(headerTs.includes('productos: STOREFRONT_PATHS.productos'), 'El enlace Productos del header debe usar la ruta canónica independiente.');
expect(categoriesTs.includes("this.router.navigate(['/tienda/productos']"), 'La búsqueda desde el listado de categorías debe continuar al catálogo independiente.');
expect(categoriesTs.includes('productos: STOREFRONT_PATHS.productos'), 'El footer/listado de categorías debe enlazar el catálogo independiente.');
expect(categoryTs.includes('`${STOREFRONT_PATHS.productos}?categoria=${encodeURIComponent(slug)}`'), 'La categoría individual debe continuar al catálogo con su slug público.');
expect(categoryTs.includes("this.router.navigate(['/tienda/productos']"), 'La búsqueda desde categoría individual debe continuar al catálogo independiente.');
expect(categoryTs.includes('productos: STOREFRONT_PATHS.productos'), 'La categoría individual debe exponer Productos como ruta canónica.');

if (failures.length) {
  console.error('Fase 3 — validación de catálogo público FALLÓ:');
  failures.forEach(failure => console.error(`- ${failure}`));
  process.exit(1);
}

console.info('Fase 3 — catálogo independiente: ruta, datos, filtros, carrito central, navegación, tema y límites aprobados.');
