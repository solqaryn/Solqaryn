import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptsDir = path.dirname(fileURLToPath(import.meta.url));
const frontendDir = path.resolve(scriptsDir, '..');
const featureDir = path.join(frontendDir, 'src/app/features/varistorehn');
const read = name => readFile(path.join(featureDir, name), 'utf8');

const [ts, html, scss, models, catalog, service, cart] = await Promise.all([
  read('varistorehn-productos.component.ts'),
  read('varistorehn-productos.component.html'),
  read('varistorehn-productos.component.scss'),
  read('varistorehn.models.ts'),
  read('varistorehn.catalog.ts'),
  read('varistorehn.service.ts'),
  read('varistorehn-carrito.service.ts')
]);

const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };

for (const token of ['busqueda', 'categoriaSlug', 'soloDisponibles', 'precioMinimo', 'precioMaximo', 'orden', 'pagina']) {
  expect(ts.includes(token), `Debe existir el estado de Fase 8: ${token}.`);
}
for (const param of ['q:', 'categoria:', 'disponible:', 'precioMin:', 'precioMax:', 'orden:', 'pagina:']) {
  expect(ts.includes(param), `La URL compartible debe sincronizar ${param}.`);
}
for (const getter of ["params.get('q')", "params.get('categoria')", "params.get('disponible')", "params.get('precioMin')", "params.get('precioMax')", "params.get('orden')", "params.get('pagina')"]) {
  expect(ts.includes(getter), `Recargar una URL debe hidratar ${getter}.`);
}

expect(models.includes("'relevancia'") && models.includes("'recientes'"), 'El ordenamiento debe incluir relevancia y recientes.');
expect(models.includes('precioMinimo: number | null'), 'El contrato de filtros debe soportar precio mínimo.');
expect(catalog.includes('filtros.precioMinimo') && catalog.includes('filtros.precioMaximo'), 'El filtro puro debe combinar ambos extremos del precio.');
expect(catalog.includes("case 'recientes'"), 'El catálogo debe ordenar por recientes.');
expect(catalog.includes("case 'relevancia'"), 'El catálogo debe ordenar por relevancia.');
expect(catalog.includes("normalize('NFD')"), 'La búsqueda debe normalizar acentos y mayúsculas/minúsculas.');
expect(html.includes('Precio mínimo') && html.includes('Precio máximo'), 'La UI debe exponer rango de precio completo.');
expect(html.includes('Solo disponibles'), 'La UI debe exponer disponibilidad.');
expect(html.includes('value="relevancia"') && html.includes('value="recientes"'), 'La UI debe exponer relevancia y recientes.');
expect(html.includes('No encontramos coincidencias') && html.includes('Limpiar filtros'), 'Debe existir estado sin resultados recuperable.');
expect(html.includes('[attr.aria-expanded]="filtrosAbiertos()"'), 'El disparador móvil debe exponer su estado.');
expect(ts.includes('normalizarPagina()'), 'Las URLs con página fuera de rango deben autocorregirse tras conocer los resultados.');
expect(ts.includes("if (valor === 'destacados') return 'relevancia'"), 'El alias legacy destacados debe normalizarse al orden canónico relevancia.');
expect(scss.includes('max-height: min(70vh, 620px)'), 'El panel móvil abierto debe quedar acotado al viewport.');
expect(!/#[0-9a-f]{3,8}\b/i.test(scss), 'Fase 8 no debe introducir colores hexadecimales fuera del tema.');

expect(service.includes('obtenerProductosContexto(productoIds: number[])'), 'La hidratación debe consultar solo las referencias persistidas del carrito.');
expect(!service.includes('obtenerCatalogo()'), 'La frontera pública no debe reintroducir descarga completa del catálogo.');
expect(cart.includes('restaurarCarrito(originales, productos)'), 'No se debe sustituir la revalidación canónica del carrito por una página parcial.');
expect(ts.includes('soloOfertas'), 'Tras Fase 9, el filtro opcional de ofertas debe consumir la autoridad comercial real.');

if (failures.length) {
  console.error('Fase 8 — validación de búsqueda, filtros y ordenamiento FALLÓ:');
  failures.forEach(failure => console.error(`- ${failure}`));
  process.exit(1);
}

console.info('Fase 8 — filtros combinables, URL persistente, relevancia/recientes, rango de precio y UX móvil aprobados.');
