import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';

const scriptsDir = path.dirname(fileURLToPath(import.meta.url));
const frontendDir = path.resolve(scriptsDir, '..');
const read = relative => readFile(path.join(frontendDir, relative), 'utf8');
const require = createRequire(import.meta.url);

const [
  indexHtml,
  appComponent,
  seoService,
  productoTs,
  categoriaTs,
  homeHtml,
  productosHtml,
  categoriasHtml,
  productoHtml,
  carritoHtml,
  headerHtml,
  categoriaHtml,
  vercelRaw,
  seoUtilsSource,
  sitemapSource
] = await Promise.all([
  read('src/index.html'),
  read('src/app/app.component.ts'),
  read('src/app/features/varistorehn/varistorehn-seo.service.ts'),
  read('src/app/features/varistorehn/varistorehn-producto.component.ts'),
  read('src/app/features/varistorehn/varistorehn-categoria.component.ts'),
  read('src/app/features/varistorehn/varistorehn.component.html'),
  read('src/app/features/varistorehn/varistorehn-productos.component.html'),
  read('src/app/features/varistorehn/varistorehn-categorias.component.html'),
  read('src/app/features/varistorehn/varistorehn-producto.component.html'),
  read('src/app/features/varistorehn/varistorehn-carrito.component.html'),
  read('src/app/features/varistorehn/varistorehn-header.component.html'),
  read('src/app/features/varistorehn/varistorehn-categoria.component.html'),
  read('vercel.json'),
  read('server/seo-utils.js'),
  read('api/sitemap.js')
]);

const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };

expect(!/<title>[^<]*administrativ/i.test(indexHtml), 'El shell público no debe titularse como sistema administrativo.');
expect(indexHtml.includes('SOLQARYN | Plataforma empresarial'), 'El shell base debe identificar a SOLQARYN, no a un cliente.');
expect(indexHtml.includes('name="description"'), 'El shell debe incluir descripción SEO base.');
expect(!indexHtml.includes('assets/varistorehn-logo.png'), 'El shell base no debe usar el logo de VariStoreHN como favicon global.');
expect(indexHtml.includes('name="robots" content="noindex,nofollow,noarchive"'), 'El shell base debe fallar cerrado para indexación.');
expect(indexHtml.includes('fonts.gstatic.com') && indexHtml.includes('crossorigin'), 'Las fuentes deben usar preconnect a gstatic para reducir bloqueo de render.');

for (const token of ['Title', 'Meta', 'link[rel="canonical"]', 'og:title', 'og:image', 'twitter:card', 'application/ld+json']) {
  expect(seoService.includes(token), `SEO dinámico debe conservar ${token}.`);
}
expect(seoService.includes('noindex,nofollow,noarchive'), 'SEO dinámico debe poder bloquear rutas privadas y entornos no publicados.');
expect(seoService.includes("return host === 'solqaryn-prod.vercel.app'"), 'El cliente solo debe habilitar indexación en el host productivo autorizado.');
expect(seoUtilsSource.includes("return hostFromRequest(req) === PRODUCTION_HOST"), 'El SEO server-side debe fallar cerrado para cualquier host no autorizado.');
expect(!seoUtilsSource.includes('nombreComercial || data?.nombreVisibleSistema'), 'El SEO server-side nunca debe caer al nombre interno si falta la marca comercial.');
expect(!appComponent.includes('nombreComercial || this.identidad.nombreSistema()'), 'El SEO cliente nunca debe caer al nombre interno si falta la marca comercial.');
expect(!productoTs.includes('nombreComercial || this.identidad.nombreSistema()'), 'Producto público nunca debe caer al nombre interno.');
expect(!categoriaTs.includes('nombreComercial || this.identidad.nombreSistema()'), 'Categoría pública nunca debe caer al nombre interno.');
expect(sitemapSource.includes("res.statusCode = 503"), 'El sitemap debe responder 503 si no puede construir el catálogo completo.');
expect(sitemapSource.includes("Retry-After"), 'El sitemap incompleto debe pedir reintento y evitar cachear un 200 parcial.');
expect(seoService.includes("path === VARISTOREHN_PATHS.productos"), 'Catálogo debe tener metadata propia.');
expect(seoService.includes("path === VARISTOREHN_PATHS.ofertas"), 'Ofertas debe tener metadata propia.');
expect(seoService.includes("path === VARISTOREHN_PATHS.categorias"), 'Categorías debe tener metadata propia.');
expect(appComponent.includes('this.aplicarContextoRuta(event.urlAfterRedirects)'), 'El router debe aplicar identidad, tema y SEO según el contexto de cada navegación.');
expect(appComponent.includes('VaristorehnIdentidadService'), 'El storefront debe usar una identidad distinta del shell SOLQARYN.');
expect(appComponent.includes("this.seo.aplicarNoIndex('SOLQARYN')"), 'Las rutas de plataforma no deben heredar SEO/branding de un cliente.');
for (const [name, html] of [
  ['header público', headerHtml],
  ['home público', homeHtml],
  ['catálogo público', productosHtml],
  ['categorías públicas', categoriasHtml],
  ['categoría pública', categoriaHtml],
  ['producto público', productoHtml]
]) {
  expect(!html.includes('identidad.nombreSistema()'), `${name} no debe renderizar el nombre interno del sistema.`);
}
expect(productoTs.includes('this.seo.aplicarProducto('), 'Producto debe publicar title/description/canonical/OG propios.');
expect(categoriaTs.includes('this.seo.aplicarCategoria('), 'Categoría debe publicar metadata propia.');
expect(productoTs.includes('this.seo.aplicarNoIndex('), 'Producto no encontrado/error debe quedar noindex.');
expect(categoriaTs.includes('this.seo.aplicarNoIndex('), 'Categoría no encontrada/error debe quedar noindex.');

for (const [name, html] of [
  ['home', homeHtml],
  ['catálogo', productosHtml],
  ['categorías', categoriasHtml],
  ['producto', productoHtml],
  ['carrito', carritoHtml]
]) {
  const images = [...html.matchAll(/<img\b[^>]*>/gi)].map(match => match[0]);
  expect(images.every(tag => /\bwidth\s*=/.test(tag) && /\bheight\s*=/.test(tag)), `Las imágenes de ${name} deben reservar width/height para evitar CLS.`);
}
expect(homeHtml.includes('fetchpriority="high"'), 'La imagen LCP de portada debe conservar fetchpriority high.');
expect(productosHtml.includes('loading="lazy"') && productosHtml.includes('decoding="async"'), 'Imágenes de catálogo deben mantener lazy loading y decoding async.');
expect(productoHtml.includes('loading="lazy"') && productoHtml.includes('fetchpriority="high"'), 'Detalle debe separar imagen LCP de imágenes lazy.');

let vercel;
try { vercel = JSON.parse(vercelRaw); } catch { vercel = null; }
expect(Boolean(vercel), 'vercel.json debe ser JSON válido.');
if (vercel) {
  const rewrites = Array.isArray(vercel.rewrites) ? vercel.rewrites : [];
  expect(rewrites.some(item => item.source === '/robots.txt' && item.destination === '/api/robots'), 'Vercel debe exponer /robots.txt dinámico.');
  expect(rewrites.some(item => item.source === '/sitemap.xml' && item.destination === '/api/sitemap'), 'Vercel debe exponer /sitemap.xml dinámico.');
  expect(rewrites.some(item => item.source === '/varistorehn/producto/:slug' && String(item.destination).includes('kind=product')), 'Bots de producto deben recibir HTML OG server-side.');
  expect(rewrites.some(item => item.source === '/varistorehn/categoria/:slug' && String(item.destination).includes('kind=category')), 'Bots de categoría deben recibir HTML SEO server-side.');
  expect(rewrites.some(item => item.source === '/api/:path*'), 'El proxy API existente debe preservarse.');
  expect(!rewrites.some(item => item.source === '/' && String(item.destination).includes('kind=home')), 'La raiz SOLQARYN no debe reescribirse al SEO del cliente VariStoreHN.');
  expect(rewrites.some(item => item.source === '/api/:path*' && String(item.destination).includes('solqaryn-api-dev-fxx8.onrender.com')), 'El proxy DEV debe usar el hostname canonico real de Render.');
  expect(rewrites.at(-1)?.source === '/(.*)' && rewrites.at(-1)?.destination === '/index.html', 'El fallback SPA debe permanecer al final.');
}

const seoHandler = require('../api/seo.js');
const robotsHandler = require('../api/robots.js');
const sitemapHandler = require('../api/sitemap.js');

function responseMock() {
  const headers = new Map();
  return {
    statusCode: 200,
    body: '',
    setHeader(name, value) { headers.set(String(name).toLowerCase(), String(value)); },
    end(value = '') { this.body = String(value); },
    header(name) { return headers.get(String(name).toLowerCase()) || ''; }
  };
}

const originalFetch = globalThis.fetch;
let forceSitemapFailure = false;
let forceMissingCommercial = false;
globalThis.fetch = async input => {
  const url = String(input);
  if (forceSitemapFailure && url.includes('/tienda/')) {
    return new Response('{}', { status: 503, headers: { 'content-type': 'application/json' } });
  }
  let payload;
  if (url.includes('/empresa-configuracion/publica')) {
    payload = forceMissingCommercial
      ? { success: true, data: { nombreComercial: '', nombreVisibleSistema: 'Sistema Interno', eslogan: 'Compra en línea', logoUrl: '/assets/varistorehn-logo.png', moneda: 'USD' } }
      : { success: true, data: { nombreComercial: 'VariStoreHN', nombreVisibleSistema: 'Sistema Interno', eslogan: 'Compra en línea', logoUrl: '/assets/varistorehn-logo.png', moneda: 'USD' } };
  } else if (url.includes('/tienda/productos/producto-seo-11')) {
    payload = {
      success: true,
      data: {
        id: 11, slug: 'producto-seo-11', nombre: 'Producto SEO 11', descripcion: 'Descripción SEO verificable.',
        categoriaNombre: 'Tecnología', marcaNombre: 'Marca SEO', precio: 1299, precioOferta: 999, ofertaActiva: true,
        cantidadDisponible: 3, estaAgotado: false, activo: true, sku: 'SEO-11',
        imagenPrincipalUrl: 'https://cdn.example.com/producto-seo-11.jpg',
        imagenes: [{ url: 'https://cdn.example.com/producto-seo-11.jpg', orden: 1, esPrincipal: true }],
        modelos: []
      }
    };
  } else if (url.includes('/tienda/categorias/tecnologia-11')) {
    payload = { success: true, data: { id: 11, slug: 'tecnologia-11', nombre: 'Tecnología SEO', descripcion: 'Categoría SEO.', totalProductos: 1 } };
  } else if (url.endsWith('/tienda/categorias')) {
    payload = { success: true, data: [{ id: 11, slug: 'tecnologia-11', nombre: 'Tecnología SEO', descripcion: 'Categoría SEO.', totalProductos: 1 }] };
  } else if (url.includes('/tienda/productos?page=')) {
    payload = {
      success: true,
      data: {
        items: [{ id: 11, slug: 'producto-seo-11', nombre: 'Producto SEO 11', activo: true, fechaCreacion: '2026-09-18T00:00:00Z' }],
        page: 1, pageSize: 96, totalCount: 1
      }
    };
  } else {
    return new Response('{}', { status: 404, headers: { 'content-type': 'application/json' } });
  }
  return new Response(JSON.stringify(payload), { status: 200, headers: { 'content-type': 'application/json' } });
};

try {
  const prodReq = { headers: { host: 'solqaryn-prod.vercel.app' }, query: { kind: 'product', slug: 'producto-seo-11' } };
  const seoRes = responseMock();
  await seoHandler(prodReq, seoRes);
  expect(seoRes.statusCode === 200, 'HTML SEO de producto debe responder 200.');
  expect(seoRes.body.includes('property="og:title" content="Producto SEO 11 | VariStoreHN"'), 'Open Graph debe exponer el nombre real del producto.');
  expect(seoRes.body.includes('property="og:image" content="https://cdn.example.com/producto-seo-11.jpg"'), 'Open Graph debe exponer la imagen real del producto.');
  expect(seoRes.body.includes('rel="canonical" href="https://solqaryn-prod.vercel.app/varistorehn/producto/producto-seo-11"'), 'HTML SEO debe usar canonical por slug.');
  expect(seoRes.body.includes('"@type":"Product"'), 'HTML SEO de producto debe incluir Product JSON-LD.');
  expect(seoRes.body.includes('"priceCurrency":"USD"'), 'JSON-LD server-side debe respetar la moneda pública configurada.');
  expect(seoRes.header('x-robots-tag').startsWith('index,follow'), 'Producción debe permitir indexación del producto.');

  forceMissingCommercial = true;
  const noCommercialRes = responseMock();
  await seoHandler(prodReq, noCommercialRes);
  expect(noCommercialRes.body.includes('Producto SEO 11 | Tienda'), 'Sin nombre comercial, el SEO debe degradar a una identidad neutra y nunca al shell SOLQARYN ni al nombre interno.');
  expect(!noCommercialRes.body.includes('Sistema Interno'), 'El nombre interno no debe filtrarse al HTML SEO.');
  forceMissingCommercial = false;

  const customHostRes = responseMock();
  await seoHandler({ headers: { host: 'staging.example.com' }, query: { kind: 'product', slug: 'producto-seo-11' } }, customHostRes);
  expect(customHostRes.header('x-robots-tag').startsWith('noindex'), 'Un dominio custom no autorizado debe permanecer noindex.');
  expect(customHostRes.body.includes('https://solqaryn-prod.vercel.app/varistorehn/producto/producto-seo-11'), 'Un host no autorizado nunca debe convertirse en canonical productivo.');

  const robotsProd = responseMock();
  await robotsHandler({ headers: { host: 'solqaryn-prod.vercel.app' }, query: {} }, robotsProd);
  expect(robotsProd.body.includes('Allow: /varistorehn/'), 'robots de producción debe permitir tienda pública.');
  expect(robotsProd.body.includes('Disallow: /varistorehn/checkout'), 'robots de producción debe bloquear checkout.');
  expect(robotsProd.body.includes('Disallow: /varistorehn/pedido/'), 'robots de producción debe bloquear referencias de pedido.');

  const robotsPreview = responseMock();
  await robotsHandler({ headers: { host: 'solqaryn-desarrollo-preview.vercel.app' }, query: {} }, robotsPreview);
  expect(robotsPreview.body.trim() === 'User-agent: *\nDisallow: /', 'Preview/desarrollo debe bloquear toda indexación.');

  const robotsCustom = responseMock();
  await robotsHandler({ headers: { host: 'staging.example.com' }, query: {} }, robotsCustom);
  expect(robotsCustom.body.trim() === 'User-agent: *\nDisallow: /', 'Hosts custom no autorizados también deben bloquear toda indexación.');

  const sitemapRes = responseMock();
  await sitemapHandler({ headers: { host: 'solqaryn-prod.vercel.app' }, query: {} }, sitemapRes);
  expect(sitemapRes.body.includes('/varistorehn/producto/producto-seo-11'), 'Sitemap debe descubrir productos por slug.');
  expect(sitemapRes.body.includes('/varistorehn/categoria/tecnologia-11'), 'Sitemap debe descubrir categorías por slug.');
  expect(!sitemapRes.body.includes('/dashboard') && !sitemapRes.body.includes('/checkout'), 'Sitemap no debe incluir rutas privadas/transaccionales.');

  forceSitemapFailure = true;
  const sitemapFailure = responseMock();
  await sitemapHandler({ headers: { host: 'solqaryn-prod.vercel.app' }, query: {} }, sitemapFailure);
  expect(sitemapFailure.statusCode === 503, 'Un catálogo incompleto debe producir sitemap 503, no un 200 parcial cacheable.');
  expect(sitemapFailure.header('cache-control') === 'no-store', 'El sitemap fallido no debe cachearse.');
  expect(sitemapFailure.header('retry-after') === '300', 'El sitemap fallido debe indicar reintento.');
  forceSitemapFailure = false;
} finally {
  globalThis.fetch = originalFetch;
}

if (failures.length) {
  console.error('Fase 11 — validación SEO/URLs/rendimiento FALLÓ:');
  failures.forEach(failure => console.error(`- ${failure}`));
  process.exit(1);
}

assert.ok(true);
console.info('Fase 11 — SEO, Open Graph server-side, canonical, robots, sitemap, imágenes y aislamiento privado aprobados.');
