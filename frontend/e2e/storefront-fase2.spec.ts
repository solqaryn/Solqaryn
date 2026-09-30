import { expect, Page, test } from '@playwright/test';

const empresaBase = {
  id: 901,
  nombreComercial: 'Storefront Audit',
  nombreVisibleSistema: 'Storefront Audit',
  eslogan: 'Compra simple y segura',
  descripcionSistema: 'Tienda pública de auditoría',
  mensajeLogin: 'Administración',
  copyright: '© 2026 Storefront Audit',
  mostrarCopyright: true,
  usarAnioAutomaticoCopyright: true,
  encabezadoActivo: true,
  encabezadoTexto: 'Atención pública de auditoría',
  piePaginaActivo: true,
  piePaginaTexto: 'Pie de auditoría',
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

async function mockCatalogoVacio(page: Page): Promise<void> {
  await page.route('**/tienda/productos?*', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({
        success: true,
        data: { items: [], page: 1, pageSize: 96, totalCount: 0 }
      })
    });
  });
}

async function mockCatalogoCategoria(page: Page): Promise<void> {
  const producto = (id: number, nombre: string, categoriaId: number, categoriaNombre: string) => ({
    id,
    slug: `${nombre.toLowerCase().replace(/[^a-z0-9]+/g, '-')}-${id}`,
    nombre,
    descripcion: `Producto publicado de ${categoriaNombre}.`,
    categoriaId,
    categoriaNombre,
    marcaNombre: 'Marca pública',
    precio: 1250 + id,
    precioOferta: null,
    cantidadDisponible: 4,
    estaAgotado: false,
    estadoDisponibilidad: 'Disponible',
    sku: `CAT-${id}`,
    activo: true,
    esDestacado: false,
    imagenes: [],
    modelos: []
  });
  const items = [
    producto(701, 'Audífonos de categoría', 21, 'Audio y Vídeo'),
    producto(702, 'Laptop de otra categoría', 22, 'Computadoras')
  ];
  await page.route('**/tienda/productos?*', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({
        success: true,
        data: { items, page: 1, pageSize: 96, totalCount: items.length }
      })
    });
  });
}

async function activarBaseDatos(page: Page): Promise<void> {
  await page.getByRole('group', { name: 'Origen de datos' })
    .getByRole('button', { name: 'Base de datos' }).click();
}

test.describe('Storefront Fase 2 — categorías públicas', () => {
  test.describe.configure({ retries: 0 });

  test('ruta independiente reutiliza header, fixtures explícitos y responde sin overflow', async ({ page }) => {
    await prepararEmpresa(page);
    await page.setViewportSize({ width: 1366, height: 900 });
    await page.goto('/tienda/categorias');

    await expect(page.getByRole('heading', { level: 1, name: 'Categorías', exact: true })).toBeVisible();
    await expect(page.locator('.categories-grid .category-card')).toHaveCount(6);
    await expect(page.locator('app-storefront-header .skip-link')).toHaveAttribute('href', '#contenido-categorias');
    await expect(page.locator('app-storefront-header').getByRole('link', { name: 'Categorías', exact: true }).first())
      .toHaveAttribute('href', '/tienda/categorias');
    await expect(page.locator('.demo-note')).toContainText('fixtures de vista previa');

    const target = page.getByRole('link', { name: 'Explorar categoría Computadoras', exact: true });
    await expect(target).toHaveAttribute('href', '/tienda/categoria/demo-categoria-1');
    const alto = await target.evaluate(element => Math.round(element.getBoundingClientRect().height));
    expect(alto).toBeGreaterThanOrEqual(44);

    for (const width of [1366, 1000, 760, 390, 320]) {
      await page.setViewportSize({ width, height: 900 });
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
      expect(overflow, `overflow horizontal a ${width}px`).toBeLessThanOrEqual(0);
    }
  });

  test('fuente real consume CategoriaTienda y conserva conteo desconocido como desconocido', async ({ page }) => {
    await prepararEmpresa(page);
    await mockCatalogoVacio(page);
    await page.route('**/tienda/categorias', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({
          success: true,
          data: [
            { id: 21, slug: 'audio-y-video-21', nombre: 'Audio y Vídeo', descripcion: 'Sonido e imagen para tu espacio.', totalProductos: null },
            { id: 22, slug: 'computadoras-22', nombre: 'Computadoras', descripcion: 'Equipos para trabajo y estudio.', totalProductos: 2 }
          ]
        })
      });
    });

    await page.goto('/tienda/categorias');
    await activarBaseDatos(page);

    const cards = page.locator('.categories-grid .category-card');
    await expect(cards).toHaveCount(2);
    await expect(cards.nth(0)).toContainText('Audio y Vídeo');
    await expect(cards.nth(0)).toContainText('Sonido e imagen para tu espacio.');
    await expect(cards.nth(0)).toContainText('Cantidad no disponible');
    await expect(cards.nth(0)).not.toContainText('0 productos');
    await expect(cards.nth(1)).toContainText('2 productos');
    await expect(page.getByRole('link', { name: 'Explorar categoría Audio y Vídeo', exact: true }))
      .toHaveAttribute('href', '/tienda/categoria/audio-y-video-21');
    await expect(page.getByText('Datos de la tienda', { exact: true }).last()).toBeVisible();
  });

  test('entrar a una categoría muestra sus productos relacionados directamente', async ({ page }) => {
    await prepararEmpresa(page);
    await mockCatalogoCategoria(page);
    await page.route('**/tienda/categorias/audio-y-video-21', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({
          success: true,
          data: { id: 21, slug: 'audio-y-video-21', nombre: 'Audio y Vídeo', descripcion: 'Sonido e imagen para tu espacio.', totalProductos: 1 }
        })
      });
    });

    await page.goto('/tienda/categoria/audio-y-video-21');
    await activarBaseDatos(page);

    await expect(page.getByRole('heading', { level: 1, name: 'Audio y Vídeo', exact: true })).toBeVisible();
    const productos = page.locator('.category-products .category-product-card');
    await expect(productos).toHaveCount(1);
    await expect(productos).toContainText('Audífonos de categoría');
    await expect(productos).toContainText(/L\.?\s*1,951|1,951/);
    await expect(productos).toContainText('Disponible');
    await expect(productos).not.toContainText('Laptop de otra categoría');
    await expect(productos.getByRole('link', { name: 'Ver producto', exact: true })).toHaveAttribute(
      'href',
      '/tienda/producto/aud-fonos-de-categor-a-701'
    );
  });

  test('fuente real vacía representa empty sin fabricar categorías', async ({ page }) => {
    await prepararEmpresa(page);
    await mockCatalogoVacio(page);
    await page.route('**/tienda/categorias', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: true, data: [] })
      });
    });

    await page.goto('/tienda/categorias');
    await activarBaseDatos(page);

    await expect(page.getByRole('heading', { name: 'Aún no hay categorías públicas disponibles' })).toBeVisible();
    await expect(page.locator('.category-card')).toHaveCount(0);
    await expect(page.getByText('Computadoras', { exact: true })).toHaveCount(0);
  });

  test('error de categorías reales no cae silenciosamente a datos demo', async ({ page }) => {
    await prepararEmpresa(page);
    await mockCatalogoVacio(page);
    await page.route('**/tienda/categorias', async route => {
      await route.fulfill({
        status: 503,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: false, message: 'Servicio no disponible' })
      });
    });

    await page.goto('/tienda/categorias');
    await activarBaseDatos(page);

    const alert = page.getByRole('alert');
    await expect(alert).toContainText('No pudimos cargar las categorías');
    await expect(alert).toContainText('No se sustituyeron los datos reales por ejemplos');
    await expect(alert.getByRole('button', { name: 'Intentar de nuevo' })).toBeVisible();
    await expect(page.locator('.category-card')).toHaveCount(0);
  });

  test('búsqueda, categoría canónica y carrito conservan continuidad hacia el catálogo independiente', async ({ page }) => {
    await prepararEmpresa(page);
    await page.setViewportSize({ width: 1366, height: 900 });
    await page.goto('/tienda/productos');
    await expect(page.getByRole('status').filter({ hasText: '14 productos encontrados' })).toBeVisible();

    const laptop = page.locator('article.product-card').filter({ hasText: 'Laptop Pro 14' });
    await laptop.getByRole('button', { name: 'Agregar Laptop Pro 14' }).click();
    await expect(page.locator('dialog.cart-dialog')).toHaveCount(0);

    await page.locator('app-storefront-header').getByRole('link', { name: 'Categorías', exact: true }).first().click();
    await expect(page).toHaveURL(/\/storefront\/categorias$/);
    const categoriesHeader = page.locator('app-storefront-header');
    await expect(categoriesHeader.getByRole('button', { name: 'Abrir carrito con 1 unidades' })).toBeVisible();
    await expect(categoriesHeader.locator('.cart-copy small')).toContainText('18,490');

    const search = categoriesHeader.getByRole('searchbox', { name: 'Buscar productos, marcas o modelos' });
    await search.fill('Wireless Studio');
    await search.press('Enter');
    await expect(page).toHaveURL(/\/storefront\/productos\?q=Wireless(?:%20|\+)Studio$/);
    await expect(page.getByRole('status').filter({ hasText: '1 productos encontrados' })).toBeVisible();
    await expect(page.locator('article.product-card')).toContainText('Audífonos Wireless Studio');

    await page.goto('/tienda/categorias');
    await page.getByRole('link', { name: 'Explorar categoría Audio', exact: true }).click();
    await expect(page).toHaveURL(/\/storefront\/categoria\/demo-categoria-2$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Audio', exact: true })).toBeVisible();
    const productosCategoria = page.locator('.category-products-grid .category-product-card');
    await expect(productosCategoria).toHaveCount(3);
    await expect(productosCategoria).toContainText(['Audífonos Wireless Studio', 'Bocina Sound Mini', 'Audífonos Travel']);
    await expect(productosCategoria.first().getByRole('link', { name: /Ver producto/ })).toBeVisible();
    await page.getByRole('link', { name: 'Ver productos de esta categoría', exact: true }).click();
    await expect(page).toHaveURL(/\/storefront\/productos\?categoria=demo-categoria-2$/);
    await expect(page.locator('#catalog-results-title')).toHaveText('Audio');
    await expect(page.getByRole('status').filter({ hasText: '3 productos encontrados' })).toBeVisible();

    await page.goto('/tienda/categorias');
    await page.locator('app-storefront-header').getByRole('button', { name: 'Abrir carrito con 1 unidades' }).click();
    await expect(page).toHaveURL(/\/storefront\/carrito$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Mi carrito', exact: true })).toBeVisible();
    await expect(page.locator('.cart-item')).toContainText('Laptop Pro 14');
    await expect(page.locator('app-storefront-header').getByRole('button', { name: 'Abrir carrito con 1 unidades' })).toBeVisible();
  });

  test('ruta canónica consume categoría por slug y corrige el prefijo con el slug devuelto por backend', async ({ page }) => {
    await prepararEmpresa(page);
    await mockCatalogoCategoria(page);
    await page.route('**/tienda/categorias/audio-viejo-21', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({
          success: true,
          data: { id: 21, slug: 'audio-y-video-21', nombre: 'Audio y Vídeo', descripcion: 'Sonido e imagen para tu espacio.', totalProductos: null }
        })
      });
    });
    await page.route('**/tienda/categorias/audio-y-video-21', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({
          success: true,
          data: { id: 21, slug: 'audio-y-video-21', nombre: 'Audio y Vídeo', descripcion: 'Sonido e imagen para tu espacio.', totalProductos: null }
        })
      });
    });

    await page.goto('/tienda/categoria/audio-viejo-21');
    await activarBaseDatos(page);

    await expect(page).toHaveURL(/\/storefront\/categoria\/audio-y-video-21$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Audio y Vídeo', exact: true })).toBeVisible();
    await expect(page.getByText('Sonido e imagen para tu espacio.', { exact: true })).toBeVisible();
    await expect(page.getByText('Cantidad no disponible', { exact: true })).toBeVisible();
    const relacionados = page.locator('.category-products-grid .category-product-card');
    await expect(relacionados).toHaveCount(1);
    await expect(relacionados).toContainText('Audífonos de categoría');
    await expect(relacionados).not.toContainText('Laptop de otra categoría');
    await expect(relacionados.getByRole('link', { name: 'Ver Audífonos de categoría' })).toHaveAttribute('href', '/tienda/producto/aud-fonos-de-categor-a-701');
    await expect(page.getByRole('link', { name: 'Ver productos de esta categoría', exact: true }))
      .toHaveAttribute('href', '/tienda/productos?categoria=audio-y-video-21');
    await expect(page.locator('app-storefront-header .skip-link')).toHaveAttribute('href', '#contenido-categoria');
  });

  test('categoría inexistente representa not-found y no fabrica un fixture', async ({ page }) => {
    await prepararEmpresa(page);
    await mockCatalogoVacio(page);
    await page.route('**/tienda/categorias/no-existe', async route => {
      await route.fulfill({
        status: 404,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: false, message: 'No encontrada' })
      });
    });

    await page.goto('/tienda/categoria/no-existe');
    await activarBaseDatos(page);

    await expect(page.getByRole('heading', { level: 1, name: 'No encontramos esta categoría', exact: true })).toBeVisible();
    await expect(page.getByText('Computadoras', { exact: true })).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'Explorar categorías', exact: true })).toHaveAttribute('href', '/tienda/categorias');
  });

  test('error de categoría por slug mantiene el fallo real sin fallback silencioso', async ({ page }) => {
    await prepararEmpresa(page);
    await mockCatalogoVacio(page);
    await page.route('**/tienda/categorias/audio-error', async route => {
      await route.fulfill({
        status: 503,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: false, message: 'Servicio no disponible' })
      });
    });

    await page.goto('/tienda/categoria/audio-error');
    await activarBaseDatos(page);

    const alert = page.getByRole('alert');
    await expect(alert).toContainText('No pudimos cargar esta categoría');
    await expect(alert).toContainText('No se sustituyeron los datos reales por ejemplos');
    await expect(alert.getByRole('button', { name: 'Intentar de nuevo' })).toBeVisible();
  });
});
