const {
  apiBase,
  publicOrigin,
  isIndexableHost,
  fetchJson,
  loadBrand,
  absoluteUrl,
  escapeHtml,
  cleanText,
  safeSlug,
  sendNoIndex
} = require('../server/seo-utils');

function meta(name, content, property = false) {
  if (!content) return '';
  const attr = property ? 'property' : 'name';
  return `<meta ${attr}="${escapeHtml(name)}" content="${escapeHtml(content)}">`;
}

function renderPage({ title, description, canonical, brand, image, imageAlt, type = 'website', bodyTitle, bodyText, jsonLd, indexable }) {
  const robots = indexable
    ? 'index,follow,max-image-preview:large,max-snippet:-1,max-video-preview:-1'
    : 'noindex,nofollow,noarchive';
  return `<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width,initial-scale=1">
  <title>${escapeHtml(title)}</title>
  ${meta('description', description)}
  ${meta('robots', robots)}
  ${meta('googlebot', robots)}
  <link rel="canonical" href="${escapeHtml(canonical)}">
  ${meta('og:locale', 'es_HN', true)}
  ${meta('og:type', type, true)}
  ${meta('og:site_name', brand, true)}
  ${meta('og:title', title, true)}
  ${meta('og:description', description, true)}
  ${meta('og:url', canonical, true)}
  ${meta('og:image', image, true)}
  ${meta('og:image:alt', imageAlt || bodyTitle || title, true)}
  ${meta('twitter:card', image ? 'summary_large_image' : 'summary')}
  ${meta('twitter:title', title)}
  ${meta('twitter:description', description)}
  ${meta('twitter:image', image)}
  ${meta('twitter:image:alt', imageAlt || bodyTitle || title)}
  ${jsonLd ? `<script type="application/ld+json">${JSON.stringify(jsonLd).replace(/</g, '\\u003c')}</script>` : ''}
</head>
<body>
  <main>
    <h1>${escapeHtml(bodyTitle || title)}</h1>
    <p>${escapeHtml(bodyText || description)}</p>
    <p><a href="${escapeHtml(canonical)}">Abrir tienda</a></p>
  </main>
</body>
</html>`;
}

async function productPage(req, slug, brand) {
  const safe = safeSlug(slug);
  if (!safe) return null;
  const payload = await fetchJson(`${apiBase(req)}/tienda/productos/${encodeURIComponent(safe)}`);
  const product = payload && payload.success ? payload.data : null;
  if (!product || product.activo === false || !product.slug) return null;

  const origin = publicOrigin(req);
  const canonical = `${origin}/tienda/producto/${encodeURIComponent(product.slug)}`;
  const description = cleanText(product.descripcion, `Compra ${product.nombre} en ${brand.name}. Consulta precio, disponibilidad y opciones del producto.`);
  const images = Array.isArray(product.imagenes) ? product.imagenes : [];
  const primary = String(
    product.imagenPrincipalUrl
    || images.find(image => image && image.esPrincipal)?.url
    || images[0]?.url
    || ''
  );
  const image = absoluteUrl(primary, origin);
  const rawOffer = product.ofertaActiva === true && Number(product.precioOferta) > 0 ? Number(product.precioOferta) : null;
  const price = rawOffer || Number(product.precio) || 0;
  const available = product.estaAgotado !== true && Number(product.cantidadDisponible) > 0;

  return {
    title: cleanText(`${product.nombre} | ${brand.name}`, '', 70),
    description,
    canonical,
    brand: brand.name,
    image,
    imageAlt: product.nombre,
    type: 'product',
    bodyTitle: product.nombre,
    bodyText: description,
    jsonLd: {
      '@context': 'https://schema.org',
      '@type': 'Product',
      name: product.nombre,
      description,
      sku: product.sku || undefined,
      brand: product.marcaNombre ? { '@type': 'Brand', name: product.marcaNombre } : undefined,
      category: product.categoriaNombre || undefined,
      image: image || undefined,
      url: canonical,
      offers: price > 0 ? {
        '@type': 'Offer',
        priceCurrency: brand.currency || 'HNL',
        price: price.toFixed(2),
        availability: available ? 'https://schema.org/InStock' : 'https://schema.org/OutOfStock',
        url: canonical
      } : undefined
    }
  };
}

async function categoryPage(req, slug, brand) {
  const safe = safeSlug(slug);
  if (!safe) return null;
  const payload = await fetchJson(`${apiBase(req)}/tienda/categorias/${encodeURIComponent(safe)}`);
  const category = payload && payload.success ? payload.data : null;
  if (!category || !category.slug || !category.nombre) return null;

  const canonical = `${publicOrigin(req)}/tienda/categoria/${encodeURIComponent(category.slug)}`;
  const description = cleanText(
    category.descripcion,
    `Explora productos de ${category.nombre} en ${brand.name}. Consulta disponibilidad y opciones del catálogo público.`
  );
  return {
    title: cleanText(`${category.nombre} | ${brand.name}`, '', 70),
    description,
    canonical,
    brand: brand.name,
    type: 'website',
    bodyTitle: category.nombre,
    bodyText: description
  };
}

function staticPage(req, kind, brand) {
  const origin = publicOrigin(req);
  const pages = {
    home: {
      path: '/tienda',
      title: `${brand.name} | Tecnología y compras en línea`,
      description: `Compra tecnología, accesorios y productos seleccionados en ${brand.name}. Explora categorías, ofertas y disponibilidad en línea.`,
      heading: brand.name
    },
    products: {
      path: '/tienda/productos',
      title: `Productos | ${brand.name}`,
      description: `Explora el catálogo público de ${brand.name}, consulta precios, disponibilidad, categorías y modelos.`,
      heading: 'Productos'
    },
    offers: {
      path: '/tienda/ofertas',
      title: `Ofertas vigentes | ${brand.name}`,
      description: `Descubre promociones vigentes de ${brand.name} con precios y disponibilidad actualizados.`,
      heading: 'Ofertas vigentes'
    },
    categories: {
      path: '/tienda/categorias',
      title: `Categorías | ${brand.name}`,
      description: `Explora las categorías públicas de ${brand.name} y encuentra productos por tipo de compra.`,
      heading: 'Categorías'
    }
  };
  const page = pages[kind];
  if (!page) return null;
  return {
    title: cleanText(page.title, '', 70),
    description: cleanText(page.description),
    canonical: `${origin}${page.path}`,
    brand: brand.name,
    image: brand.logo,
    imageAlt: brand.name,
    type: 'website',
    bodyTitle: page.heading,
    bodyText: page.description
  };
}

module.exports = async function handler(req, res) {
  const kind = String(req.query.kind || '').toLowerCase();
  const slug = Array.isArray(req.query.slug) ? req.query.slug[0] : req.query.slug;
  const indexable = isIndexableHost(req);

  try {
    const brand = await loadBrand(req);
    let page;
    if (kind === 'product') page = await productPage(req, slug, brand);
    else if (kind === 'category') page = await categoryPage(req, slug, brand);
    else page = staticPage(req, kind, brand);

    if (!page) {
      sendNoIndex(res, 404, 'Contenido público no encontrado.');
      return;
    }

    res.statusCode = 200;
    res.setHeader('Content-Type', 'text/html; charset=utf-8');
    res.setHeader('Cache-Control', indexable ? 'public, s-maxage=300, stale-while-revalidate=3600' : 'no-store');
    res.setHeader('X-Robots-Tag', indexable ? 'index,follow,max-image-preview:large' : 'noindex,nofollow,noarchive');
    res.end(renderPage({ ...page, indexable }));
  } catch (error) {
    const status = Number(error && error.status) === 404 ? 404 : 503;
    sendNoIndex(res, status, status === 404 ? 'Contenido público no encontrado.' : 'Metadatos temporalmente no disponibles.');
  }
};
