import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptsDir = path.dirname(fileURLToPath(import.meta.url));
const frontendDir = path.resolve(scriptsDir, '..');
const featureDir = path.join(frontendDir, 'src/app/features/varistorehn');
const read = name => readFile(path.join(featureDir, name), 'utf8');

const [homeTs, homeHtml, homeScss, responsiveScss, productTs, paths, catalog, storeService, adminFormTs, adminProductModel, adminProductService] = await Promise.all([
  read('varistorehn.component.ts'),
  read('varistorehn.component.html'),
  read('varistorehn.component.scss'),
  read('varistorehn.responsive.scss'),
  read('varistorehn-productos.component.ts'),
  read('varistorehn.paths.ts'),
  read('varistorehn.catalog.ts'),
  read('varistorehn.service.ts'),
  readFile(path.join(frontendDir, 'src/app/features/productos/producto-form.component.ts'), 'utf8'),
  readFile(path.join(frontendDir, 'src/app/core/models/producto.model.ts'), 'utf8'),
  readFile(path.join(frontendDir, 'src/app/services/producto.service.ts'), 'utf8')
]);

const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };

expect(homeTs.includes('VaristorehnHeaderComponent'), 'El home debe reutilizar el header público certificado.');
expect(homeHtml.includes('<app-varistorehn-header'), 'La portada debe renderizar el header compartido.');
expect(homeHtml.includes('destinoSaltar="#contenido-principal"'), 'El skip link del home debe apuntar al contenido principal.');
expect(homeHtml.includes('(buscarSolicitado)="buscarCatalogo()"'), 'La búsqueda del home debe delegar en el catálogo canónico.');
expect(homeTs.includes('this.router.navigate([VARISTOREHN_PATHS.productos]'), 'La búsqueda debe navegar a /varistorehn/productos.');
expect(homeTs.includes('VARISTOREHN_PATHS.categoria(categoria.slug)'), 'Las categorías del home deben abrir su URL pública canónica.');
expect(homeTs.includes('VARISTOREHN_PATHS.producto(producto.slug)'), 'Los destacados deben abrir su URL pública canónica.');
expect(homeTs.includes('navigateByUrl(VARISTOREHN_PATHS.carrito'), 'El home debe conservar el carrito global canónico.');

for (const debt of [
  'filtrarProductos', 'OrdenCatalogo', 'precioMaximo', 'soloDisponibles', 'tamanoPagina',
  'totalPaginas', 'productosVisibles', 'filtrosAbiertos', 'cambiarPagina(', 'cambiarOrden(', 'cambiarPrecio('
]) {
  expect(!homeTs.includes(debt), `El home comercial no debe reintroducir lógica del catálogo: ${debt}.`);
}
expect(!homeTs.includes('this.servicio.obtenerCatalogo()'), 'El home no debe descargar el catálogo completo solo para construir la portada.');
expect(!homeTs.includes('this.carritoStore.hidratar('), 'El home no debe hidratar el carrito descargando el catálogo completo.');
expect(!homeTs.includes('this.carritoStore.reiniciarContexto()'), 'Cambiar la vista previa del home no debe borrar el estado global del carrito.');
expect(homeTs.includes('this.carritoStore.listo() ? this.carritoStore.totalUnidades() : null'), 'El header debe mostrar el carrito solo cuando el store ya está hidratado.');

for (const debt of ['class="catalog-layout"', 'class="filters"', 'class="catalog-toolbar"', 'class="pagination"', 'id="catalogo"', '(click)="agregar(']) {
  expect(!homeHtml.includes(debt), `La portada no debe duplicar UI del catálogo: ${debt}.`);
}
expect(homeHtml.includes('categories-section'), 'La portada debe incluir acceso comercial a categorías.');
expect(homeHtml.includes('featured-section'), 'La portada debe reservar una sección de destacados.');
expect(homeHtml.includes('purchase-path'), 'La portada debe explicar el recorrido de compra sin duplicar sus páginas.');
expect(homeHtml.includes('id="contacto"'), 'La portada debe conservar un punto de contacto público.');
expect(homeHtml.includes('@for (categoria of categoriasPortada()'), 'La portada debe limitar la muestra de categorías.');
expect(homeTs.includes('.filter(producto => producto.activo && producto.destacado && Boolean(producto.slug))'), 'Los destacados deben respetar explícitamente la marca destacado.');
expect(homeTs.includes('bootstrap.destacados') && homeTs.includes('.map(mapearProductoResumen)'), 'La carga inicial del home debe consumir destacados desde el bootstrap público.');
expect(homeTs.includes('bootstrap.categorias.map(mapearCategoriaTienda)'), 'La carga inicial del home debe consumir categorías desde el mismo bootstrap.');
expect(!homeTs.includes('.filter(p => p.disponible).slice(0, 3)'), 'No se deben fabricar destacados reales escogiendo productos disponibles arbitrarios.');
expect(storeService.includes('urlDestacados') && storeService.includes('/destacados'), 'El servicio debe separar el endpoint público de destacados.');
expect(storeService.includes('obtenerDestacados(limite = 4)'), 'El servicio debe conservar la lectura limitada de destacados para retries/rutas específicas.');
expect(storeService.includes('obtenerBootstrap(force = false)') && storeService.includes('/bootstrap'), 'El storefront debe exponer una única lectura bootstrap para la portada.');
expect(storeService.includes('shareReplay({ bufferSize: 1, refCount: false })'), 'El bootstrap debe deduplicarse entre consumidores simultáneos.');
expect(adminProductModel.includes('esDestacado?: boolean'), 'El modelo administrativo debe transportar la bandera de destacado sin romper consumidores legacy.');
expect(adminProductService.includes("formData.append('EsDestacado'"), 'El CRUD de productos debe enviar EsDestacado al backend.');
expect(adminFormTs.includes('esDestacado: [false]') && adminFormTs.includes('p.esDestacado === true'), 'El formulario administrativo debe permitir editar la bandera de destacado.');
expect(homeHtml.includes('Aún no hay productos marcados como destacados'), 'La fuente real sin destacados debe tener un estado comercial honesto.');
expect(homeHtml.includes('El home no elige productos arbitrarios'), 'La UI debe dejar explícito que no fabrica destacados reales.');

expect(productTs.includes('filtrarProductos'), 'Los filtros deben permanecer en la página independiente de productos.');
expect(productTs.includes('tamanoPagina = 12'), 'La paginación debe permanecer en el catálogo independiente.');
expect(paths.includes("productos: '/varistorehn/productos'"), 'Debe conservarse la ruta canónica del catálogo.');
expect(paths.includes("categorias: '/varistorehn/categorias'"), 'Debe conservarse la ruta canónica de categorías.');
expect(catalog.includes('destacado: Boolean(producto.esDestacado)'), 'El mapper debe conservar la marca de destacado del contrato público.');

for (const [name, scss] of [['home', homeScss], ['responsive', responsiveScss]]) {
  expect(!/#[0-9a-f]{3,8}\b/i.test(scss), `${name} no debe introducir colores hexadecimales propios.`);
  expect(!/\brgb(?:a)?\s*\(/i.test(scss), `${name} no debe introducir colores RGB propios.`);
  expect(!/\bhsl(?:a)?\s*\(/i.test(scss), `${name} no debe introducir colores HSL propios.`);
}
expect(homeScss.includes('var(--color-bg)') && homeScss.includes('var(--color-surface)') && homeScss.includes('var(--color-primary)'), 'El home debe usar los tokens del tema empresarial.');
expect(homeScss.includes('var(--color-button)') && homeScss.includes('var(--shadow-card)'), 'Botones y elevación deben seguir el tema configurado.');
expect(responsiveScss.includes('@media (min-width: 381px)') && !/@media\s*\(\s*max-width/i.test(responsiveScss), 'La portada debe usar móvil estrecho como baseline y expandirse con min-width.');
expect(homeScss.includes('min-height: 44px'), 'Los CTA deben conservar objetivos táctiles de al menos 44px.');

for (const forbidden of ['authGuard', 'permisoGuard', 'ProductosListComponent', 'CategoriasListComponent']) {
  expect(!homeTs.includes(forbidden) && !homeHtml.includes(forbidden), `La portada pública no debe depender de ${forbidden}.`);
}

if (failures.length) {
  console.error('Fase 7 — validación de home comercial FALLÓ:');
  failures.forEach(failure => console.error(`- ${failure}`));
  process.exit(1);
}

console.info('Fase 7 — home comercial: bootstrap único, portada ligera, destacados persistidos y limitados, navegación canónica, carrito compartido, tema y límites aprobados.');
