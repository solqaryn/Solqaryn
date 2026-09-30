import { expect, Page, test } from '@playwright/test';

const empresa = {
  id: 911,
  nombreComercial: 'Storefront Fase 11',
  nombreVisibleSistema: 'Sistema Interno',
  eslogan: 'Tecnología y compras en línea',
  descripcionSistema: 'Administrativo',
  mensajeLogin: 'Administración',
  copyright: '© 2026 Storefront Fase 11',
  mostrarCopyright: true,
  usarAnioAutomaticoCopyright: true,
  encabezadoActivo: true,
  encabezadoTexto: 'Tienda pública',
  piePaginaActivo: true,
  piePaginaTexto: 'Tienda pública',
  moneda: 'HNL',
  zonaHoraria: 'America/Tegucigalpa',
  formatoFecha: 'dd/MM/yyyy',
  whatsApp: '9876-5432'
};

const imagenUrl = 'https://cdn.example.com/producto-seo-11.svg';

const producto = {
  id: 911,
  slug: 'producto-seo-11',
  nombre: 'Laptop SEO 11',
  descripcion: 'Laptop preparada para pruebas de metadatos públicos y rendimiento.',
  categoriaId: 811,
  categoriaNombre: 'Tecnología SEO',
  marcaNombre: 'Marca SEO',
  precio: 25999,
  precioOferta: 23999,
  ofertaActiva: true,
  ofertaNombre: 'Oferta SEO',
  ahorro: 2000,
  porcentajeAhorro: 8,
  cantidadDisponible: 5,
  estaAgotado: false,
  estadoDisponibilidad: 'Disponible',
  sku: 'SEO-911',
  activo: true,
  esDestacado: true,
  imagenPrincipalUrl: imagenUrl,
  imagenes: [{ url: imagenUrl, orden: 1, esPrincipal: true }],
  modelos: [{
    productoVarianteId: 9111,
    modeloId: 91101,
    modeloNombre: '16 GB / 512 GB',
    marcaNombre: 'Marca SEO',
    sku: 'SEO-911-A',
    precio: 25999,
    precioOferta: 23999,
    ofertaActiva: true,
    ofertaNombre: 'Oferta SEO',
    ahorro: 2000,
    porcentajeAhorro: 8,
    cantidadDisponible: 5,
    estaAgotado: false,
    estadoDisponibilidad: 'Disponible',
    imagenes: [{ url: imagenUrl, orden: 1, esPrincipal: true }]
  }]
};

const categoria = {
  id: 811,
  slug: 'tecnologia-seo-11',
  nombre: 'Tecnología SEO',
  descripcion: 'Categoría pública preparada para metadatos SEO.',
  totalProductos: 1
};

async function preparar(page: Page): Promise<void> {
  await page.route('http://localhost:5005/empresa-configuracion/publica', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    headers: { 'Access-Control-Allow-Origin': '*' },
    body: JSON.stringify({ success: true, data: empresa })
  }));

  await page.route('https://cdn.example.com/**', route => route.fulfill({
    status: 200,
    contentType: 'image/svg+xml',
    body: '<svg xmlns="http://www.w3.org/2000/svg" width="800" height="600"><rect width="800" height="600" fill="white"/><text x="400" y="300" text-anchor="middle">SEO 11</text></svg>'
  }));
}

async function mockCatalogoReal(page: Page): Promise<void> {
  await page.route('**/tienda/productos?*', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    headers: { 'Access-Control-Allow-Origin': '*' },
    body: JSON.stringify({ success: true, data: { items: [producto], page: 1, pageSize: 96, totalCount: 1 } })
  }));
  await page.route('**/tienda/productos/*', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    headers: { 'Access-Control-Allow-Origin': '*' },
    body: JSON.stringify({ success: true, data: producto })
  }));
  await page.route('**/tienda/categorias', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    headers: { 'Access-Control-Allow-Origin': '*' },
    body: JSON.stringify({ success: true, data: [categoria] })
  }));
  await page.route('**/tienda/categorias/*', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    headers: { 'Access-Control-Allow-Origin': '*' },
    body: JSON.stringify({ success: true, data: categoria })
  }));
}

function meta(page: Page, selector: string) {
  return page.locator(`head meta[${selector}]`);
}

test.describe('Storefront Fase 11 — SEO, URLs y rendimiento', () => {
  test.describe.configure({ retries: 0 });

  test('shell público usa identidad comercial y las rutas privadas quedan noindex', async ({ page }) => {
    await preparar(page);

    await page.goto('/tienda');
    await expect(page).toHaveTitle('Storefront Fase 11 | Tecnología y compras en línea');
    await expect(page.getByText('Sistema Interno', { exact: true })).toHaveCount(0);
    await expect(page.locator('app-storefront-header .brand strong')).toHaveText('Storefront Fase 11');
    await expect(meta(page, 'name="description"')).not.toHaveAttribute('content', /administrativ/i);
    await expect(page.locator('head link[rel="canonical"]')).toHaveAttribute(
      'href',
      'https://solqaryn-prod.vercel.app/tienda'
    );
    await expect(meta(page, 'name="robots"')).toHaveAttribute('content', /noindex/);

    await page.goto('/login');
    await expect(meta(page, 'name="robots"')).toHaveAttribute('content', /noindex,nofollow/);
    await expect(page.locator('head link[rel="canonical"]')).toHaveCount(0);
    await expect(meta(page, 'name="description"')).toHaveCount(0);
    await expect(meta(page, 'property="og:site_name"')).toHaveCount(0);
    await expect(page).not.toHaveTitle(/administrativ|sistema interno/i);
  });

  test('producto real publica title description canonical Open Graph e imagen propios', async ({ page }) => {
    await preparar(page);
    await mockCatalogoReal(page);

    await page.goto('/tienda/producto/producto-seo-11');
    await page.getByRole('group', { name: 'Origen de datos' }).getByRole('button', { name: 'Base de datos' }).click();
    await expect(page.getByRole('heading', { level: 1, name: 'Laptop SEO 11' })).toBeVisible();

    await expect(page).toHaveTitle('Laptop SEO 11 | Storefront Fase 11');
    await expect(meta(page, 'name="description"')).toHaveAttribute('content', /pruebas de metadatos públicos/i);
    await expect(page.locator('head link[rel="canonical"]')).toHaveAttribute(
      'href',
      'https://solqaryn-prod.vercel.app/tienda/producto/producto-seo-11'
    );
    await expect(meta(page, 'property="og:type"')).toHaveAttribute('content', 'product');
    await expect(meta(page, 'property="og:title"')).toHaveAttribute('content', 'Laptop SEO 11 | Storefront Fase 11');
    await expect(meta(page, 'property="og:site_name"')).toHaveAttribute('content', 'Storefront Fase 11');
    await expect(meta(page, 'property="og:image"')).toHaveAttribute('content', imagenUrl);
    await expect(meta(page, 'name="twitter:card"')).toHaveAttribute('content', 'summary_large_image');

    const jsonLd = page.locator('head script[data-storefront-seo="jsonld"]');
    await expect(jsonLd).toHaveCount(1);
    const schema = JSON.parse(await jsonLd.textContent() || '{}');
    expect(schema['@type']).toBe('Product');
    expect(schema.name).toBe('Laptop SEO 11');
    expect(schema.url).toBe('https://solqaryn-prod.vercel.app/tienda/producto/producto-seo-11');
    expect(schema.offers?.price).toBe('23999.00');
  });

  test('categoría por slug publica metadata propia y canonical limpio', async ({ page }) => {
    await preparar(page);
    await mockCatalogoReal(page);

    await page.goto('/tienda/categoria/tecnologia-seo-11');
    await page.getByRole('group', { name: 'Origen de datos' }).getByRole('button', { name: 'Base de datos' }).click();
    await expect(page.getByRole('heading', { level: 1, name: 'Tecnología SEO' })).toBeVisible();

    await expect(page).toHaveTitle('Tecnología SEO | Storefront Fase 11');
    await expect(meta(page, 'name="description"')).toHaveAttribute('content', /Categoría pública preparada/i);
    await expect(page.locator('head link[rel="canonical"]')).toHaveAttribute(
      'href',
      'https://solqaryn-prod.vercel.app/tienda/categoria/tecnologia-seo-11'
    );
    await expect(meta(page, 'property="og:url"')).toHaveAttribute(
      'content',
      'https://solqaryn-prod.vercel.app/tienda/categoria/tecnologia-seo-11'
    );
  });

  test('filtros compartibles conservan URL pero canonicalizan el catálogo base', async ({ page }) => {
    await preparar(page);
    await page.goto('/tienda/productos?q=laptop&oferta=1&pagina=2');

    await expect(page).toHaveURL(/q=laptop/);
    await expect(page.locator('head link[rel="canonical"]')).toHaveAttribute(
      'href',
      'https://solqaryn-prod.vercel.app/tienda/productos'
    );
    await expect(page).toHaveTitle('Productos | Storefront Fase 11');
  });

  test('móvil mide LCP CLS e INP y no detecta cuello de botella severo', async ({ page }) => {
    await preparar(page);
    await page.setViewportSize({ width: 390, height: 844 });
    await page.addInitScript(() => {
      const vitals = { lcp: 0, cls: 0, inp: 0 };
      (window as unknown as { __fase11Vitals: typeof vitals }).__fase11Vitals = vitals;

      try {
        new PerformanceObserver(list => {
          const entries = list.getEntries();
          const last = entries[entries.length - 1] as PerformanceEntry | undefined;
          if (last) vitals.lcp = last.startTime;
        }).observe({ type: 'largest-contentful-paint', buffered: true });
      } catch {}

      try {
        new PerformanceObserver(list => {
          for (const entry of list.getEntries()) {
            const shift = entry as PerformanceEntry & { value?: number; hadRecentInput?: boolean };
            if (!shift.hadRecentInput) vitals.cls += Number(shift.value || 0);
          }
        }).observe({ type: 'layout-shift', buffered: true });
      } catch {}

      try {
        new PerformanceObserver(list => {
          for (const entry of list.getEntries()) {
            vitals.inp = Math.max(vitals.inp, Number(entry.duration || 0));
          }
        }).observe({ type: 'event', buffered: true, durationThreshold: 16 } as PerformanceObserverInit);
      } catch {}
    });

    await page.goto('/tienda');
    await expect(page.locator('.storefront')).toBeVisible();
    await page.getByRole('button', { name: 'Abrir navegación' }).click();
    await page.getByRole('button', { name: 'Cerrar navegación' }).click();
    await page.waitForTimeout(1200);

    const vitals = await page.evaluate(() =>
      (window as unknown as { __fase11Vitals: { lcp: number; cls: number; inp: number } }).__fase11Vitals
    );

    expect(vitals.lcp, 'LCP debe medirse en Chromium').toBeGreaterThan(0);
    expect(vitals.lcp, 'LCP local no debe mostrar un cuello severo').toBeLessThanOrEqual(4000);
    expect(vitals.cls, 'CLS debe mantenerse estable').toBeLessThanOrEqual(0.15);
    expect(vitals.inp, 'INP observado no debe mostrar interacción severamente lenta').toBeLessThanOrEqual(500);
  });
});
