const { publicOrigin, isIndexableHost } = require('../server/seo-utils');

module.exports = async function handler(req, res) {
  const indexable = isIndexableHost(req);
  let origin = '';
  if (indexable) {
    try { origin = publicOrigin(req); } catch { origin = ''; }
  }

  const body = indexable && origin
    ? [
        'User-agent: *',
        'Disallow: /',
        'Allow: /$',
        'Allow: /tienda$',
        'Allow: /tienda/',
        'Disallow: /tienda/carrito',
        'Disallow: /tienda/checkout',
        'Disallow: /tienda/cuenta',
        'Disallow: /tienda/pedido/',
        `Sitemap: ${origin}/sitemap.xml`,
        ''
      ].join('\n')
    : ['User-agent: *', 'Disallow: /', ''].join('\n');

  res.statusCode = 200;
  res.setHeader('Content-Type', 'text/plain; charset=utf-8');
  res.setHeader('Cache-Control', indexable ? 'public, s-maxage=3600, stale-while-revalidate=86400' : 'no-store');
  res.setHeader('X-Robots-Tag', 'noindex');
  res.end(body);
};
