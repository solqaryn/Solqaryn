const {
  apiBase,
  publicOrigin,
  isIndexableHost,
  fetchJson,
  escapeXml,
  safeSlug
} = require('../server/seo-utils');

const MAX_SITEMAP_URLS = 50000;

function xmlUrl(loc, lastmod) {
  const mod = lastmod && Number.isFinite(Date.parse(lastmod))
    ? `<lastmod>${escapeXml(new Date(lastmod).toISOString())}</lastmod>`
    : '';
  return `<url><loc>${escapeXml(loc)}</loc>${mod}</url>`;
}

function emptySitemap() {
  return '<?xml version="1.0" encoding="UTF-8"?><urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9"></urlset>';
}

async function loadCategories(req) {
  const payload = await fetchJson(`${apiBase(req)}/tienda/categorias`);
  if (!payload?.success || !Array.isArray(payload.data)) {
    throw new Error('Respuesta de categorías no válida para sitemap.');
  }
  return payload.data.filter(item => safeSlug(item?.slug));
}

async function loadProducts(req) {
  const all = [];
  let page = 1;
  let totalPages = 1;

  while (page <= totalPages) {
    const payload = await fetchJson(`${apiBase(req)}/tienda/productos?page=${page}&pageSize=96`);
    const data = payload?.success ? payload.data : null;
    if (!data
      || !Array.isArray(data.items)
      || Number(data.page) !== page
      || !Number.isSafeInteger(Number(data.pageSize))
      || Number(data.pageSize) <= 0
      || !Number.isSafeInteger(Number(data.totalCount))
      || Number(data.totalCount) < 0) {
      throw new Error('Respuesta de productos no válida para sitemap.');
    }

    all.push(...data.items.filter(item => item && item.activo !== false && safeSlug(item.slug)));

    const pageSize = Number(data.pageSize);
    const totalCount = Number(data.totalCount);
    totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

    if (totalCount + 4 > MAX_SITEMAP_URLS) {
      throw new Error('El catálogo requiere sitemap index por exceder el límite de URLs.');
    }
    page += 1;
  }

  return all;
}

module.exports = async function handler(req, res) {
  if (!isIndexableHost(req)) {
    res.statusCode = 200;
    res.setHeader('Content-Type', 'application/xml; charset=utf-8');
    res.setHeader('Cache-Control', 'no-store');
    res.setHeader('X-Robots-Tag', 'noindex');
    res.end(emptySitemap());
    return;
  }

  const origin = publicOrigin(req);

  try {
    const [categories, products] = await Promise.all([loadCategories(req), loadProducts(req)]);
    const urls = new Map();

    for (const path of ['/tienda', '/tienda/productos', '/tienda/ofertas', '/tienda/categorias']) {
      urls.set(`${origin}${path}`, '');
    }
    for (const category of categories) {
      urls.set(`${origin}/tienda/categoria/${encodeURIComponent(category.slug)}`, '');
    }
    for (const product of products) {
      urls.set(
        `${origin}/tienda/producto/${encodeURIComponent(product.slug)}`,
        product.fechaCreacion || ''
      );
    }

    if (urls.size > MAX_SITEMAP_URLS) {
      throw new Error('El sitemap excede el límite de 50,000 URLs.');
    }

    const body = [
      '<?xml version="1.0" encoding="UTF-8"?>',
      '<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">',
      ...[...urls.entries()].map(([loc, lastmod]) => xmlUrl(loc, lastmod)),
      '</urlset>'
    ].join('');

    res.statusCode = 200;
    res.setHeader('Content-Type', 'application/xml; charset=utf-8');
    res.setHeader('Cache-Control', 'public, s-maxage=3600, stale-while-revalidate=86400');
    res.setHeader('X-Robots-Tag', 'noindex');
    res.end(body);
  } catch {
    res.statusCode = 503;
    res.setHeader('Content-Type', 'application/xml; charset=utf-8');
    res.setHeader('Cache-Control', 'no-store');
    res.setHeader('Retry-After', '300');
    res.setHeader('X-Robots-Tag', 'noindex');
    res.end(emptySitemap());
  }
};
