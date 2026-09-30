import { expect, Page, test } from '@playwright/test';

const empresaBase = {
  id: 907,
  nombreComercial: 'Storefront Home Audit',
  nombreVisibleSistema: 'Storefront Home Audit',
  eslogan: 'Compra simple y segura',
  descripcionSistema: 'Portada comercial de auditoría',
  mensajeLogin: 'Administración',
  copyright: '© 2026 Storefront Home Audit',
  mostrarCopyright: true,
  usarAnioAutomaticoCopyright: true,
  encabezadoActivo: true,
  encabezadoTexto: 'Atención pública de auditoría',
  piePaginaActivo: true,
  piePaginaTexto: 'Compra simple y segura',
  moneda: 'HNL',
  zonaHoraria: 'America/Tegucigalpa',
  formatoFecha: 'dd/MM/yyyy',
  whatsApp: '9876-5432'
};

async function prepararEmpresa(page: Page): Promise<void> {
  await page.route('http://localhost:5005/empresa-configuracion/publica', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({ success: true, data: empresaBase })
    });
  });
}

async function abrirHome(page: Page): Promise<void> {
  await page.goto('/tienda');
  await expect(page.locator('.storefront')).toBeVisible();
  await expect(page.getByRole('heading', { name: /Todo lo que buscas/i })).toBeVisible();
}

async function activarBaseDatos(page: Page): Promise<void> {
  await page.getByRole('group', { name: 'Origen de datos' })
    .getByRole('button', { name: 'Base de datos' }).click();
}

test.describe('Storefront Fase 7 — home comercial', () => {
  test.describe.configure({ retries: 0 });

  test('bootstrap inicial consolida identidad, tema, categorías y destacados en una sola llamada', async ({ page }) => {
    const requests = {
      bootstrap: 0,
      identidad: 0,
      whatsapp: 0,
      tema: 0,
      categorias: 0,
      destacados: 0
    };

    page.on('request', request => {
      const url = request.url();
      if (/\/tienda\/bootstrap(?:\?|$)/.test(url)) requests.bootstrap += 1;
      if (/\/empresa-configuracion\/publica(?:\?|$)/.test(url)) requests.identidad += 1;
      if (/\/whatsapp\/publico(?:\?|$)/.test(url)) requests.whatsapp += 1;
      if (/\/tema-visual(?:\?|$)/.test(url)) requests.tema += 1;
      if (/\/tienda\/categorias(?:\?|$)/.test(url)) requests.categorias += 1;
      if (/\/tienda\/productos\/destacados(?:\?|$)/.test(url)) requests.destacados += 1;
    });

    await page.route('**/tienda/bootstrap', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({
          success: true,
          data: {
            identidad: empresaBase,
            tema: {
              colorPrimario: '#8b0000',
              colorSecundario: '#660000',
              colorAcento: '#b22222',
              fondoPrincipal: '#ffffff',
              fondoTarjetas: '#ffffff',
              menuLateral: '#8b0000',
              barraSuperior: '#ffffff',
              encabezados: '#111111',
              botonesPrincipales: '#8b0000',
              textoPrincipal: '#111111',
              textoSecundario: '#555555',
              colorExito: '#008000',
              colorAdvertencia: '#a06000',
              colorError: '#b00020',
              colorInformacion: '#005ea8'
            },
            categorias: [],
            destacados: []
          }
        })
      });
    });

    await abrirHome(page);

    await expect(page.locator('app-storefront-header .brand strong')).toContainText('Storefront Home Audit');
    await expect.poll(() => requests.bootstrap).toBe(1);
    expect(requests.identidad).toBe(0);
    expect(requests.whatsapp).toBe(0);
    expect(requests.tema).toBe(0);
    expect(requests.categorias).toBe(0);
    expect(requests.destacados).toBe(0);
  });

  test('demo: portada comercial no duplica catálogo, filtros ni paginación', async ({ page }) => {
    await prepararEmpresa(page);
    await page.setViewportSize({ width: 1366, height: 900 });
    await abrirHome(page);

    await expect(page.locator('app-storefront-header .skip-link')).toHaveAttribute('href', '#contenido-principal');
    await expect(page.locator('.category-card')).toHaveCount(6);
    await expect(page.locator('.featured-card')).toHaveCount(3);
    await expect(page.locator('.filters')).toHaveCount(0);
    await expect(page.locator('.catalog-toolbar')).toHaveCount(0);
    await expect(page.locator('.pagination')).toHaveCount(0);
    await expect(page.locator('article.product-card')).toHaveCount(0);
    await expect(page.getByRole('link', { name: /Explorar productos/i })).toHaveAttribute('href', '/tienda/productos');
    await expect(page.getByRole('link', { name: 'Ver categorías', exact: true })).toHaveAttribute('href', '/tienda/categorias');
  });

  test('búsqueda del home navega al catálogo canónico y conserva el término', async ({ page }) => {
    await prepararEmpresa(page);
    await abrirHome(page);

    const search = page.locator('app-storefront-header').getByRole('searchbox', { name: 'Buscar productos, marcas o modelos' });
    await search.fill('Wireless Studio');
    await search.press('Enter');

    await expect(page).toHaveURL(/\/storefront\/productos\?q=Wireless(?:%20|\+)Studio/);
    await expect(page.getByRole('heading', { level: 1, name: 'Productos', exact: true })).toBeVisible();
    await expect(page.getByRole('status').filter({ hasText: '1 productos encontrados' })).toBeVisible();
    await expect(page.locator('article.product-card')).toContainText('Audífonos Wireless Studio');
  });

  test('categorías del home abren la URL pública canónica por slug', async ({ page }) => {
    await prepararEmpresa(page);
    await abrirHome(page);

    await page.getByRole('button', { name: 'Explorar Audio', exact: true }).click();
    await expect(page).toHaveURL(/\/storefront\/categoria\/demo-categoria-2$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Audio', exact: true })).toBeVisible();
  });

  test('destacados demo respetan la marca comercial y navegan al detalle real', async ({ page }) => {
    await prepararEmpresa(page);
    await abrirHome(page);

    const destacados = page.locator('.featured-card');
    await expect(destacados).toHaveCount(3);
    await expect(destacados.nth(0)).toContainText('Laptop Pro 14');
    await destacados.nth(0).getByRole('button', { name: 'Ver Laptop Pro 14' }).click();
    await expect(page).toHaveURL(/\/storefront\/producto\/demo-producto-1$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Laptop Pro 14', exact: true })).toBeVisible();
  });

  test('fuente real consulta destacados limitados sin descargar el catálogo completo', async ({ page }) => {
    await prepararEmpresa(page);
    let solicitudesCatalogoCompleto = 0;
    let solicitudesDestacados = 0;
    page.on('request', request => {
      if (/\/tienda\/productos(?:\?|$)/.test(request.url())) solicitudesCatalogoCompleto += 1;
      if (/\/tienda\/productos\/destacados(?:\?|$)/.test(request.url())) solicitudesDestacados += 1;
    });
    await page.route('**/tienda/categorias', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({
          success: true,
          data: [
            { id: 71, slug: 'hogar-real-71', nombre: 'Hogar real', descripcion: 'Categoría desde BD', totalProductos: 4 },
            { id: 72, slug: 'oficina-real-72', nombre: 'Oficina real', descripcion: 'Categoría desde BD', totalProductos: null }
          ]
        })
      });
    });
    await page.route('**/tienda/productos/destacados?*', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({
          success: true,
          data: [{
            id: 501,
            slug: 'laptop-real-501',
            nombre: 'Laptop Real Destacada',
            descripcion: 'Producto destacado persistido',
            categoriaId: 72,
            categoriaNombre: 'Oficina real',
            marcaNombre: 'Marca real',
            modeloNombre: 'Pro',
            precio: 12345,
            precioOferta: null,
            cantidadDisponible: 5,
            estaAgotado: false,
            sku: 'REAL-501',
            activo: true,
            esDestacado: true,
            fechaCreacion: '2026-09-18T12:00:00Z',
            imagenPrincipalUrl: null,
            imagenes: [],
            modelos: [{
              productoVarianteId: 9001,
              modeloId: 77,
              modeloNombre: '16 GB / 512 GB',
              marcaNombre: 'Marca real',
              sku: 'REAL-501',
              precio: 12345,
              cantidadDisponible: 5,
              estaAgotado: false,
              imagenes: []
            }]
          }]
        })
      });
    });

    await abrirHome(page);
    await activarBaseDatos(page);

    await expect(page.locator('.category-card')).toHaveCount(2);
    await expect(page.getByRole('button', { name: 'Explorar Hogar real' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Explorar Oficina real' })).toBeVisible();
    await expect(page.locator('.featured-card')).toHaveCount(1);
    await expect(page.locator('.featured-card')).toContainText('Laptop Real Destacada');
    await expect(page.getByText('Destacado de la tienda', { exact: true })).toBeVisible();
    await expect.poll(() => solicitudesCatalogoCompleto).toBe(0);
    await expect.poll(() => solicitudesDestacados).toBe(1);
  });

  test('fuente real sin destacados mantiene fallback comercial honesto', async ({ page }) => {
    await prepararEmpresa(page);
    await page.route('**/tienda/categorias', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: true, data: [] })
      });
    });
    await page.route('**/tienda/productos/destacados?*', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: true, data: [] })
      });
    });

    await abrirHome(page);
    await activarBaseDatos(page);

    await expect(page.locator('.featured-card')).toHaveCount(0);
    await expect(page.getByText('Aún no hay productos marcados como destacados', { exact: true })).toBeVisible();
    await expect(page.getByText('El home no elige productos arbitrarios', { exact: false })).toBeVisible();
  });
  test('error de categorías reales permanece visible y nunca cae silenciosamente a demo', async ({ page }) => {
    await prepararEmpresa(page);
    await page.route('**/tienda/categorias', async route => {
      await route.fulfill({
        status: 503,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: false, message: 'Servicio no disponible' })
      });
    });
    await page.route('**/tienda/productos/destacados?*', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: true, data: [] })
      });
    });

    await abrirHome(page);
    await activarBaseDatos(page);

    await expect(page.getByRole('heading', { name: 'No pudimos cargar las categorías' })).toBeVisible();
    await expect(page.locator('.category-card')).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Explorar Computadoras' })).toHaveCount(0);
  });

  test('carrito ya hidratado conserva continuidad al volver al home y la portada refluye hasta 320px', async ({ page }) => {
    await prepararEmpresa(page);
    await page.setViewportSize({ width: 1366, height: 900 });
    await page.goto('/tienda/productos');
    await expect(page.getByRole('status').filter({ hasText: '14 productos encontrados' })).toBeVisible();

    const laptop = page.locator('article.product-card').filter({ hasText: 'Laptop Pro 14' });
    await laptop.getByRole('button', { name: 'Agregar Laptop Pro 14' }).click();
    const catalogHeader = page.locator('app-storefront-header');
    await expect(catalogHeader.getByRole('button', { name: 'Abrir carrito con 1 unidades' })).toBeVisible();
    await catalogHeader.getByRole('link', { name: 'Inicio', exact: true }).click();

    await expect(page).toHaveURL(/\/storefront$/);
    const homeHeader = page.locator('app-storefront-header');
    await expect(homeHeader.getByRole('button', { name: 'Abrir carrito con 1 unidades' })).toBeVisible();
    await homeHeader.getByRole('button', { name: 'Abrir carrito con 1 unidades' }).click();
    await expect(page).toHaveURL(/\/storefront\/carrito$/);
    await expect(page.locator('.cart-item')).toContainText('Laptop Pro 14');

    await page.goto('/tienda');
    for (const width of [760, 390, 320]) {
      await page.setViewportSize({ width, height: 844 });
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
      expect(overflow, `overflow horizontal a ${width}px`).toBeLessThanOrEqual(0);
    }

    await page.setViewportSize({ width: 320, height: 844 });
    const ctas = await page.locator('.hero-actions .button').evaluateAll(elements =>
      elements.map(element => Math.round(element.getBoundingClientRect().height))
    );
    expect(ctas.length).toBeGreaterThan(0);
    expect(ctas.every(height => height >= 44)).toBe(true);
  });
});
