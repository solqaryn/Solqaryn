import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const frontendDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const repoDir = path.resolve(frontendDir, '..');
const readFrontend = relative => readFile(path.join(frontendDir, relative), 'utf8');
const readRepo = relative => readFile(path.join(repoDir, relative), 'utf8');
const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };

const [
  models,
  catalog,
  service,
  cartService,
  products,
  product,
  category,
  cart,
  checkout,
  account,
  order,
  paths,
  config,
  routes,
  responsiveSpec,
  productsCss,
  productCss,
  headerCss,
  controller,
  accountController,
  publicDto,
  phase10Workflow,
  phase11Workflow,
  phase12Workflow
] = await Promise.all([
  readFrontend('src/app/features/varistorehn/varistorehn.models.ts'),
  readFrontend('src/app/features/varistorehn/varistorehn.catalog.ts'),
  readFrontend('src/app/features/varistorehn/varistorehn.service.ts'),
  readFrontend('src/app/features/varistorehn/varistorehn-carrito.service.ts'),
  readFrontend('src/app/features/varistorehn/varistorehn-productos.component.ts'),
  readFrontend('src/app/features/varistorehn/varistorehn-producto.component.ts'),
  readFrontend('src/app/features/varistorehn/varistorehn-categoria.component.ts'),
  readFrontend('src/app/features/varistorehn/varistorehn-carrito.component.ts'),
  readFrontend('src/app/features/varistorehn/varistorehn-checkout.component.ts'),
  readFrontend('src/app/features/varistorehn/varistorehn-cuenta.component.ts'),
  readFrontend('src/app/features/varistorehn/varistorehn-pedido.component.ts'),
  readFrontend('src/app/features/varistorehn/varistorehn.paths.ts'),
  readFrontend('src/app/features/varistorehn/varistorehn.config.ts'),
  readFrontend('src/app/app.routes.ts'),
  readFrontend('e2e/varistorehn-fase10.spec.ts'),
  readFrontend('src/app/features/varistorehn/varistorehn-productos.component.scss'),
  readFrontend('src/app/features/varistorehn/varistorehn-producto.component.scss'),
  readFrontend('src/app/features/varistorehn/varistorehn-header.component.scss'),
  readRepo('backend/src/API/Controllers/TiendaController.cs'),
  readRepo('backend/src/API/Controllers/TiendaCuentaController.cs'),
  readRepo('backend/src/Application/DTOs/ProductoCatalogoPublicoDto.cs'),
  readRepo('.github/workflows/varistorehn-fase10-regression.yml'),
  readRepo('.github/workflows/varistorehn-fase11-regression.yml'),
  readRepo('.github/workflows/varistorehn-fase12-regression.yml')
]);

const storefrontIdentity = await readFrontend('src/app/features/varistorehn/varistorehn-identidad.service.ts');
const appComponent = await readFrontend('src/app/app.component.ts');
const bootstrapService = await readRepo('backend/src/Application/Services/TiendaBootstrapService.cs');

// Datos: una frontera publica y reglas comerciales centralizadas.
expect(controller.includes('ICatalogoPublicoService') && !controller.includes('MapearProductoAsync'), 'Datos: TiendaController debe delegar el producto publico al read path dedicado sin mapper legacy duplicado.');
expect(controller.includes('[HttpGet("bootstrap")]') && controller.includes('_bootstrapService.ObtenerAsync'), 'Datos: la portada debe tener un único endpoint bootstrap público.');
expect(bootstrapService.includes('_empresaConfiguracion.GetActivaAsync()') && bootstrapService.includes('_temaVisual.GetAsync()') && bootstrapService.includes('_categorias.GetActivasAsync()') && bootstrapService.includes('_catalogo.ObtenerDestacadosAsync(4'), 'Datos: el bootstrap debe componer identidad, tema, categorías y cuatro destacados dentro del mismo caso de uso.');
expect(service.includes('obtenerBootstrap(force = false)') && service.includes('shareReplay({ bufferSize: 1, refCount: false })'), 'Datos: Angular debe compartir y deduplicar el bootstrap entre consumidores concurrentes.');
expect(storefrontIdentity.includes('this.tiendaService.obtenerBootstrap(force)') && storefrontIdentity.includes('catchError(() => this.cargarLegacy())'), 'Datos: identidad storefront debe usar bootstrap en camino feliz y reservar legacy sólo como recovery.');
expect(appComponent.includes('if (bootstrap) this.themeApplier.aplicar(bootstrap.tema)') && !appComponent.includes('this.themeApplier.aplicarTemaGuardado();\n      this.tiendaIdentidad.cargar()'), 'Datos: el tema storefront debe venir del bootstrap y no disparar una lectura paralela en el camino feliz.');
expect(controller.includes('_inventarioPublicoService.ObtenerPorVariantesAsync'), 'Datos: stock publico debe venir de InventarioPublicoService.');
expect(controller.includes('_promocionPublicaService.ResolverAsync'), 'Datos: precio promocional debe venir de PromocionPublicaService.');
expect(catalog.includes('export function mapearProducto') && catalog.includes('export function precioVenta'), 'Datos: frontend debe normalizar producto y precio en reglas compartidas.');
expect(products.includes('mapearProducto') && products.includes('precioVenta'), 'Datos: catalogo debe consumir las reglas compartidas.');
expect(product.includes('mapearProducto') && product.includes('precioVenta'), 'Datos: detalle debe consumir las reglas compartidas.');
expect(controller.includes('El cliente nunca envía importes') && controller.includes('precioVigente = oferta?.PrecioOferta ?? precio'), 'Datos: checkout debe recalcular importes en servidor.');
expect(cartService.includes('Never persist prices, stock, image URLs') || catalog.includes('Never persist prices, stock, image URLs'), 'Datos: persistencia del carrito no debe convertir datos comerciales en autoridad local.');

// Diseno: identidad propia por tokens; ninguna dependencia de marca de referencia.
const visualSources = [products, product, category, cart, checkout, account, order].join('\n').toLowerCase();
expect(!visualSources.includes('acosa'), 'Diseno: VariStoreHN no debe incorporar la marca de referencia en su implementacion.');
expect(config.includes('Feature-only switches. No changes to company identity, theme or shared settings.'), 'Diseno: la configuracion feature no debe sustituir identidad/tema del sistema.');

// Estado: contrato canonico compartido.
expect(models.includes("EstadoConsultaPublica = 'loading' | 'empty' | 'error' | 'success'"), 'Estado: debe existir contrato loading/error/empty/success.');
expect(models.includes("EstadoRecursoPublico = EstadoConsultaPublica | 'not-found'"), 'Estado: recursos individuales deben extender el contrato con not-found.');
for (const [name, source, token] of [
  ['productos', products, 'EstadoConsultaPublica'],
  ['producto', product, 'EstadoRecursoPublico'],
  ['categoria', category, 'EstadoRecursoPublico'],
  ['carrito', cart, 'EstadoConsultaPublica'],
  ['checkout', checkout, 'EstadoConsultaPublica'],
  ['cuenta', account, 'EstadoConsultaPublica'],
  ['pedido', order, 'EstadoRecursoPublico']
]) {
  expect(source.includes(token), `Estado: ${name} debe usar ${token}.`);
}

// Carrito: una unica autoridad.
for (const [name, source] of [['productos', products], ['producto', product], ['carrito', cart], ['checkout', checkout], ['cuenta', account]]) {
  expect(source.includes('VaristorehnCarritoService'), `Carrito: ${name} debe consumir el store global.`);
}
for (const [name, source] of [['productos', products], ['producto', product], ['carrito', cart], ['checkout', checkout]]) {
  expect(!source.includes('localStorage'), `Carrito: ${name} no debe crear persistencia paralela en localStorage.`);
}
expect(cartService.includes('varistorehn:carrito:v2:'), 'Carrito: debe conservar una unica clave versionada.');
expect(!cartService.includes('JSON.stringify(this._items'), 'Carrito: no debe persistir ItemCarrito completo.');

// URLs compartibles y persistentes.
expect(paths.includes("producto: (slug: string)") && paths.includes("categoria: (slug: string)"), 'URLs: producto y categoria deben construirse por slug.');
for (const token of ["params.get('q')", "params.get('categoria')", "params.get('disponible')", "params.get('precioMin')", "params.get('precioMax')", "params.get('orden')", "params.get('pagina')"]) {
  expect(products.includes(token), `URLs: falta hidratar query param ${token}.`);
}
expect(products.includes('queryParams:'), 'URLs: catalogo debe sincronizar filtros en query params.');

// Mobile-first como Definition of Done: primero movil/tactil, luego escritorio/zoom.
const mobileExploration = responsiveSpec.indexOf('rutas públicas de exploración no desbordan en anchos móviles comunes');
const mobilePurchase = responsiveSpec.indexOf('rutas públicas de compra no desbordan en anchos móviles comunes');
const desktopZoom = responsiveSpec.indexOf('zoom de escritorio 80 a 200 por ciento');
expect(mobileExploration >= 0 && mobilePurchase > mobileExploration, 'Mobile-first: deben existir pruebas moviles de exploracion y compra.');
expect(desktopZoom > mobilePurchase, 'Mobile-first: la matriz movil debe ejecutarse antes que la expansion de escritorio/zoom.');

expect(responsiveSpec.includes('320') && responsiveSpec.includes('390') && responsiveSpec.includes('hasTouch: true'), 'Mobile-first: deben cubrirse 320/390 px y contexto tactil.');
const featureDir = path.join(frontendDir, 'src/app/features/varistorehn');
const responsiveScss = await Promise.all(
  (await readdir(featureDir))
    .filter(name => name.endsWith('.scss'))
    .map(async name => [name, await readFile(path.join(featureDir, name), 'utf8')])
);
for (const [name, css] of responsiveScss) {
  expect(!/@media\s*\(\s*max-width/i.test(css), `Mobile-first: ${name} no debe reducir un layout de escritorio con max-width.`);
  expect(!/#[0-9a-f]{3,8}\b/i.test(css), `Diseno: ${name} no debe fijar colores hexadecimales propios fuera del tema.`);
  expect(!/\b(?:rgb|hsl)a?\s*\(/i.test(css), `Diseno: ${name} no debe fijar colores RGB/HSL propios fuera del tema.`);
}
for (const [name, css] of [['catalogo', productsCss], ['detalle', productCss], ['header', headerCss]]) {
  expect(/@media\s*\(\s*min-width/i.test(css), `Mobile-first: ${name} debe expandirse desde un baseline movil mediante min-width.`);
}
expect(productsCss.includes('grid-template-columns: 1fr') && productCss.includes('grid-template-columns:1fr'), 'Mobile-first: catalogo y detalle deben declarar una columna como baseline movil.');
expect(headerCss.includes('.mobile-menu-trigger') && headerCss.includes('display: grid'), 'Mobile-first: el header debe exponer el control movil en el baseline.');

// Seguridad: frontera publica separada de administracion.
expect(controller.includes('[AllowAnonymous]') && controller.includes('[Route("tienda")]'), 'Seguridad: la tienda debe exponer una frontera publica explicita.');
expect(publicDto.includes('ProductoCatalogoPublicoDto'), 'Seguridad: la tienda debe usar DTO publico dedicado.');
const publicRoutes = routes.split('\n').filter(line => line.includes("path: 'varistorehn"));
expect(publicRoutes.length > 0 && publicRoutes.every(line => !line.includes('authGuard') && !line.includes('permisoGuard')), 'Seguridad: rutas publicas VariStoreHN no deben reutilizar guards administrativos.');
expect(accountController.includes('X-VaristoreHN-Session') && accountController.includes('HashToken'), 'Seguridad: cuenta cliente debe usar sesion publica aislada y token hasheado.');
expect(!account.includes('AuthService'), 'Seguridad: cuenta publica no debe consumir AuthService administrativo.');
expect(config.includes('endpointCheckoutTarjeta: null') && config.includes('origenesCheckoutPermitidos: []'), 'Seguridad: pago externo debe permanecer cerrado hasta configuracion explicita.');
expect(service.includes("!ruta.startsWith('tienda/')"), 'Seguridad: un checkout configurable nunca debe poder apuntar fuera de la frontera publica /tienda.');

// Calidad: regresion acumulativa antes de cerrar fases.
expect(phase10Workflow.includes('for fase in 1 2 3 4 5 6 7 8 9 10'), 'Calidad: Fase 10 debe reejecutar Fases 1-10.');
expect(phase11Workflow.includes('for fase in 1 2 3 4 5 6 7 8 9 10 11'), 'Calidad: Fase 11 debe reejecutar Fases 1-11.');
expect(phase12Workflow.includes('for fase in 1 2 3 4 5 6 7 8 9 10 11 12'), 'Calidad: Fase 12 debe reejecutar Fases 1-12.');
for (const workflow of [phase10Workflow, phase11Workflow, phase12Workflow]) {
  expect(workflow.includes('npm run lint') && workflow.includes('npm run build:prod'), 'Calidad: cada cierre avanzado debe exigir lint y build de produccion.');
}

if (failures.length) {
  console.error('VariStoreHN - reglas transversales FALLARON:');
  failures.forEach(failure => console.error(`- ${failure}`));
  process.exit(1);
}

console.info('VariStoreHN - reglas transversales: datos, diseno, estados, carrito, URLs, mobile-first, seguridad y calidad aprobados.');
