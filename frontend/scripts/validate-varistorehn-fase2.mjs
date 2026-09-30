import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptsDir = path.dirname(fileURLToPath(import.meta.url));
const frontendDir = path.resolve(scriptsDir, '..');
const featureDir = path.join(frontendDir, 'src/app/features/varistorehn');
const readFeature = name => readFile(path.join(featureDir, name), 'utf8');

const [
  appRoutes,
  models,
  service,
  categoryRules,
  categoriesTs,
  categoriesHtml,
  categoriesScss,
  categoryTs,
  categoryHtml,
  categoryScss,
  storefrontTs,
  storefrontHtml,
  headerTs,
  headerHtml,
  cartStore
] = await Promise.all([
  readFile(path.join(frontendDir, 'src/app/app.routes.ts'), 'utf8'),
  readFeature('varistorehn.models.ts'),
  readFeature('varistorehn.service.ts'),
  readFeature('varistorehn-categorias.catalog.ts'),
  readFeature('varistorehn-categorias.component.ts'),
  readFeature('varistorehn-categorias.component.html'),
  readFeature('varistorehn-categorias.component.scss'),
  readFeature('varistorehn-categoria.component.ts'),
  readFeature('varistorehn-categoria.component.html'),
  readFeature('varistorehn-categoria.component.scss'),
  readFeature('varistorehn.component.ts'),
  readFeature('varistorehn.component.html'),
  readFeature('varistorehn-header.component.ts'),
  readFeature('varistorehn-header.component.html'),
  readFeature('varistorehn-carrito.service.ts')
]);

const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };

const listRouteLine = appRoutes.split('\n').find(line => line.includes("path: 'varistorehn/categorias'")) || '';
expect(Boolean(listRouteLine), 'Debe existir la ruta pública /varistorehn/categorias.');
expect(listRouteLine.includes('VaristorehnCategoriasComponent'), 'La ruta de categorías debe cargar su página pública independiente.');
expect(!listRouteLine.includes('authGuard') && !listRouteLine.includes('permisoGuard'), 'La ruta pública de categorías no debe requerir guards administrativos.');

const categoryRouteLine = appRoutes.split('\n').find(line => line.includes("path: 'varistorehn/categoria/:slug'")) || '';
expect(Boolean(categoryRouteLine), 'Debe existir la ruta pública canónica /varistorehn/categoria/:slug.');
expect(categoryRouteLine.includes('VaristorehnCategoriaComponent'), 'La ruta canónica debe cargar VaristorehnCategoriaComponent.');
expect(!categoryRouteLine.includes('authGuard') && !categoryRouteLine.includes('permisoGuard'), 'La ruta pública por slug no debe requerir guards administrativos.');

expect(models.includes('export interface CategoriaTienda'), 'Fase 2 debe conservar CategoriaTienda como modelo visual canónico.');
expect(models.includes('cantidadProductos: number | null'), 'El conteo de CategoriaTienda debe preservar null como desconocido.');
expect(service.includes('obtenerCategorias(force = false)'), 'La página pública debe usar la frontera HTTP compartida de categorías ya definida.');
expect(service.includes('obtenerCategoriaPorSlug(slug: string)'), 'La página canónica debe consumir la frontera HTTP de categoría por slug.');
expect(categoryRules.includes('mapearCategoriaTienda'), 'Debe existir un mapeo explícito del DTO público a CategoriaTienda.');
expect(categoryRules.includes('cantidadProductos: cantidad'), 'El mapeo debe conservar el conteo público sin fabricarlo.');
expect(categoryRules.includes('cantidad !== null'), 'El mapeo debe validar el conteo solo cuando la fuente lo conoce.');
expect(categoryRules.includes('crearCategoriasTiendaEjemplo'), 'Los fixtures de preview deben estar separados de la fuente real.');

for (const required of [
  'CategoriaTienda',
  'EstadoConsultaPublica',
  'VaristorehnService',
  'obtenerCategorias()',
  'mapearCategoriaTienda',
  'VaristorehnCarritoService',
  'this.carrito.hidratar(productos',
  'VARISTOREHN_PATHS'
]) {
  expect(categoriesTs.includes(required), `La página de categorías debe integrar ${required}.`);
}
expect(!categoriesTs.includes('localStorage'), 'La página de categorías no debe mantener una persistencia de carrito paralela.');
expect(categoriesTs.includes("this.error.set('No pudimos cargar las categorías"), 'La página debe exponer un error real cuando falle la fuente de categorías.');
expect(categoriesTs.includes('VARISTOREHN_PATHS.categoria(categoria.slug)'), 'El listado debe navegar a la ruta canónica de la categoría.');
expect(!categoriesTs.includes("queryParams: { categoria: categoria.slug }"), 'El listado no debe seguir usando el home como sustituto de la URL canónica.');
expect(!categoriesTs.includes('CategoriasListComponent'), 'La página pública no debe reutilizar el CRUD administrativo de categorías.');
expect(!categoriesTs.includes('authGuard') && !categoriesTs.includes('permisoGuard'), 'La página pública no debe depender de guards administrativos.');
expect(!/#[0-9a-f]{3,8}\b/i.test(categoriesScss), 'La página de categorías no debe introducir colores hexadecimales fuera del tema.');
expect(categoriesScss.includes('min-height: 44px'), 'Los controles de Fase 2 deben conservar objetivos táctiles de al menos 44px.');

for (const state of ['loading', 'error', 'empty']) {
  expect(categoriesHtml.includes(`estado() === '${state}'`), `La página de categorías debe representar el estado ${state}.`);
}
expect(categoriesHtml.includes("[attr.aria-busy]=\"estado() === 'loading'\""), 'La carga de categorías debe exponerse con aria-busy.');
expect(categoriesHtml.includes('Cantidad no disponible') || categoriesTs.includes('Cantidad no disponible'), 'Un conteo null debe mostrarse como desconocido, no como cero.');
expect(categoriesHtml.includes('destinoSaltar="#contenido-categorias"'), 'El header reutilizado debe tener un destino de salto válido en la página de categorías.');
expect(categoriesHtml.includes('[href]="rutaExplorar(categoria)"'), 'Cada categoría debe enlazar mediante su slug canónico.');

for (const required of [
  'ActivatedRoute',
  'obtenerCategoriaPorSlug',
  'mapearCategoriaTienda',
  'VARISTOREHN_PATHS.categoria',
  'replaceUrl: true',
  'VaristorehnCarritoService',
  'this.carrito.hidratar(productos',
  'productosCategoria',
  'estadoProductos',
  'precioVenta',
  'etiquetaDisponibilidad'
]) {
  expect(categoryTs.includes(required), `La página por slug debe integrar ${required}.`);
}
expect(!categoryTs.includes('localStorage'), 'La página por slug no debe mantener una persistencia de carrito paralela.');
expect(categoryTs.includes("'not-found'"), 'La página por slug debe distinguir categoría no encontrada.');
expect(categoryTs.includes('error.status === 404'), 'La página por slug debe representar HTTP 404 como no encontrado.');
expect(categoryTs.includes('No se sustituyeron los datos reales por ejemplos'), 'Un fallo real no debe caer silenciosamente a fixtures.');
expect(!categoryTs.includes('CategoriasListComponent'), 'La página canónica no debe importar el CRUD administrativo.');
expect(!categoryTs.includes('authGuard') && !categoryTs.includes('permisoGuard'), 'La página canónica no debe depender de guards administrativos.');
expect(categoryHtml.includes("estado() === 'not-found'"), 'La plantilla debe representar explícitamente el estado no encontrado.');
expect(categoryHtml.includes('[href]="rutaCatalogoCategoria()"'), 'La página canónica debe permitir continuar al catálogo actual filtrado.');
expect(categoryHtml.includes('category-products-grid') && categoryHtml.includes('productosCategoria()'), 'La categoría debe mostrar productos relacionados directamente, sin obligar a saltar primero al catálogo.');
for (const state of ['loading', 'error', 'empty']) {
  expect(categoryHtml.includes(`estadoProductos() === '${state}'`), `Los productos de categoría deben representar el estado ${state}.`);
}
expect(categoryHtml.includes('[href]="rutaProducto(producto)"'), 'Cada producto relacionado debe enlazar a su detalle público independiente.');
expect(categoryHtml.includes('destinoSaltar="#contenido-categoria"'), 'La página canónica debe reutilizar el skip link del header.');
expect(!/#[0-9a-f]{3,8}\b/i.test(categoryScss), 'La página canónica no debe introducir una paleta hexadecimal paralela.');
expect(!categoryScss.includes('--color-background'), 'La página canónica debe usar --color-bg, token real del tema, y no --color-background.');
expect(!categoryScss.includes('--color-text-secondary'), 'La página canónica debe usar --color-text-muted, token real del tema, y no --color-text-secondary.');
expect(categoryScss.includes('var(--color-bg)'), 'La página canónica debe heredar explícitamente el fondo canónico del tema.');
expect(categoryScss.includes('var(--color-text-muted)'), 'La página canónica debe heredar explícitamente el texto secundario canónico del tema.');
expect(categoryScss.includes('min-height: 44px'), 'La página canónica debe conservar objetivos táctiles de al menos 44px.');
expect(categoryScss.includes('.category-products-grid'), 'La página canónica debe maquetar los productos relacionados.');
expect(!/@media\s*\(\s*max-width/i.test(categoryScss), 'La página canónica de categoría debe mantener arquitectura mobile-first real.');

expect(cartStore.includes('restaurarCarrito'), 'La rehidratación contra precio/stock actuales debe vivir en el carrito central.');
expect(cartStore.includes('referenciasCarrito'), 'El carrito central debe persistir referencias mínimas.');
expect(storefrontTs.includes('categoriasTienda'), 'El home debe consumir el estado canónico de categorías de Fase 2.');
expect(storefrontTs.includes('cargarCategorias()'), 'El home debe cargar categorías mediante la fuente pública dedicada.');
expect(storefrontTs.includes('this.servicio.obtenerCategorias()'), 'El home no debe inferir categorías reales desde productos.');
expect(!storefrontTs.includes('new Set(this.productos().map(p => p.categoria))'), 'Debe eliminarse la inferencia de categorías basada en texto de producto.');
expect(storefrontHtml.includes('categoriasPortada()'), 'La sección visual del home debe consumir una proyección limitada del estado canónico CategoriaTienda.');
expect(storefrontHtml.includes('estadoCategorias()'), 'La sección de categorías del home debe representar estados explícitos.');
expect(!storefrontHtml.includes('categoria.producto'), 'La imagen de categoría no debe inventarse tomando un producto representativo.');
expect(!storefrontHtml.includes('categoria.cantidad'), 'El conteo visual debe provenir de CategoriaTienda, no de un cálculo local de productos.');

expect(headerTs.includes('categorias: VARISTOREHN_PATHS.categorias'), 'El enlace principal de Categorías debe apuntar a la ruta pública canónica de Fase 2.');
expect(headerHtml.includes('[href]="destinoSaltar"'), 'El skip link del header debe ser reutilizable fuera del home.');
expect(headerTs.includes('totalUnidades: number | null'), 'El resumen de carrito compartido debe poder expresar un valor desconocido sin inventar cero.');

if (failures.length) {
  console.error('Fase 2 — validación de categorías FALLÓ:');
  failures.forEach(failure => console.error(`- ${failure}`));
  process.exit(1);
}

console.info('Fase 2 — categorías: listado, ruta canónica por slug, estados, navegación, tema y carrito central aprobados.');
