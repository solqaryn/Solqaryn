import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const frontendDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const repoDir = path.resolve(frontendDir, '..');
const readFrontend = relative => readFile(path.join(frontendDir, relative), 'utf8');
const readRepo = relative => readFile(path.join(repoDir, relative), 'utf8');

const [
  program,
  cacheFilter,
  tienda,
  empresaConfiguracion,
  temaVisual,
  whatsapp,
  vercelRaw
] = await Promise.all([
  readRepo('backend/src/API/Program.cs'),
  readRepo('backend/src/API/Filters/PublicHttpCacheAttribute.cs'),
  readRepo('backend/src/API/Controllers/TiendaController.cs'),
  readRepo('backend/src/API/Controllers/EmpresaConfiguracionController.cs'),
  readRepo('backend/src/API/Controllers/TemaVisualController.cs'),
  readRepo('backend/src/API/Controllers/WhatsAppController.cs'),
  readFrontend('vercel.json')
]);

const vercel = JSON.parse(vercelRaw);
const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };

expect(program.includes('AddResponseCompression(options =>'), 'HTTP: backend debe registrar ResponseCompression.');
expect(program.includes('BrotliCompressionProvider') && program.includes('GzipCompressionProvider'), 'HTTP: backend debe conservar Brotli y Gzip.');
expect(program.includes('EnableForHttps = true'), 'HTTP: compresion debe permanecer habilitada sobre HTTPS.');
expect(program.includes('app.UseResponseCompression();'), 'HTTP: pipeline debe ejecutar ResponseCompression.');
expect(program.includes('private, no-store, max-age=0'), 'HTTP: backend debe conservar no-store por defecto.');

expect(cacheFilter.includes('SHA256.HashData') && cacheFilter.includes('HeaderNames.ETag'), 'HTTP: respuestas publicas deben conservar ETag estable.');
expect(cacheFilter.includes('HeaderNames.IfNoneMatch') && cacheFilter.includes('Status304NotModified'), 'HTTP: If-None-Match debe poder resolver 304.');
expect(cacheFilter.includes('stale-while-revalidate=600'), 'HTTP: identidad/categorias deben conservar stale-while-revalidate moderado.');
expect(cacheFilter.includes('public, max-age=5, s-maxage=15, must-revalidate'), 'HTTP: productos deben conservar TTL HTTP corto.');
expect(cacheFilter.includes('AcceptEncoding'), 'HTTP: ETag debe variar correctamente junto a compresion.');

for (const signature of [
  '[HttpGet("bootstrap")]\n    [PublicHttpCache(PublicHttpCacheProfile.Bootstrap)]',
  '[HttpGet("productos")]\n    [PublicHttpCache(PublicHttpCacheProfile.Products)]',
  '[HttpGet("productos/destacados")]\n    [PublicHttpCache(PublicHttpCacheProfile.Products)]',
  '[HttpGet("productos/{slug}")]\n    [PublicHttpCache(PublicHttpCacheProfile.Products)]',
  '[HttpGet("categorias")]\n    [PublicHttpCache(PublicHttpCacheProfile.Categories)]',
  '[HttpGet("categorias/{slug}")]\n    [PublicHttpCache(PublicHttpCacheProfile.Categories)]'
]) {
  expect(tienda.includes(signature), `HTTP: falta politica publica explicita en TiendaController: ${signature}`);
}

expect(!/HttpPost\("productos\/contexto"\)[\s\S]{0,160}PublicHttpCache/.test(tienda), 'Seguridad: contexto de carrito no puede llevar cache publico.');
expect(!/HttpPost\("checkout\/validar"\)[\s\S]{0,160}PublicHttpCache/.test(tienda), 'Seguridad: checkout no puede llevar cache publico.');

expect(empresaConfiguracion.includes('[HttpGet("publica")]\n    [AllowAnonymous]\n    [PublicHttpCache(PublicHttpCacheProfile.Identity)]'), 'HTTP: identidad publica debe tener cache HTTP explicita.');
expect(temaVisual.includes('[HttpGet]\n    [AllowAnonymous]\n    [PublicHttpCache(PublicHttpCacheProfile.Identity)]'), 'HTTP: tema publico debe tener cache HTTP explicita.');
expect(whatsapp.includes('[HttpGet("publico")]\n    [AllowAnonymous]\n    [PublicHttpCache(PublicHttpCacheProfile.Identity)]'), 'HTTP: WhatsApp publico debe tener cache HTTP explicita.');
expect(!/HttpPost\("iniciar-whatsapp"\)[\s\S]{0,180}PublicHttpCache/.test(whatsapp), 'Seguridad: administracion WhatsApp no puede llevar cache publico.');

const headers = Array.isArray(vercel.headers) ? vercel.headers : [];
const immutable = headers.find(item => String(item?.source || '').includes('[A-Za-z0-9]{8,}'));
expect(Boolean(immutable), 'CDN: vercel.json debe reconocer bundles Angular hashados.');
expect(
  immutable?.headers?.some(header => header.key === 'Cache-Control' && header.value === 'public, max-age=31536000, immutable'),
  'CDN: bundles hashados deben ser immutable por un anio.'
);

const rewriteCache = headers.find(item => item?.source === '/api/:path*');
expect(
  rewriteCache?.headers?.some(header => header.key === 'x-vercel-enable-rewrite-caching' && header.value === '1'),
  'CDN: Vercel debe respetar el Cache-Control del backend en rewrites API.'
);

if (failures.length > 0) {
  console.error('Contrato HTTP cache/ETag/compresion FALLÓ:');
  for (const failure of failures) console.error(`- ${failure}`);
  process.exit(1);
}

console.log('Contrato HTTP cache/ETag/compresion: OK');
