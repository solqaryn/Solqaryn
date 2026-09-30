import { expect, Page, test } from '@playwright/test';

const empresaBase = {
  id: 901,
  nombreComercial: 'VariStore Audit',
  nombreVisibleSistema: 'VariStore Audit',
  eslogan: 'Compra simple y segura',
  descripcionSistema: 'Tienda pública de auditoría',
  mensajeLogin: 'Administración',
  copyright: '© 2026 VariStore Audit',
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

async function activarBaseDatos(page: Page): Promise<void> {
  await page.getByRole('group', { name: 'Origen de datos' })
    .getByRole('button', { name: 'Base de datos' }).click();
}

function productoReal(id: number, nombre: string, precio = 1000, categoria = 'Computadoras') {
  return {
    id,
    slug: `producto-real-${id}`,
    nombre,
    descripcion: `Descripción pública de ${nombre}`,
    categoriaId: 21,
    categoriaNombre: categoria,
    marcaNombre: 'Marca real',
    precio,
    precioOferta: null,
    cantidadDisponible: 5,
    estaAgotado: false,
    sku: `SKU-${id}`,
    activo: true,
    esDestacado: false,
    imagenes: [],
    modelos: []
  };
}

async function mockCategoriasReales(page: Page, totalProductos: number | null = null): Promise<void> {
  await page.route('**/tienda/categorias', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({
        success: true,
        data: [{ id: 21, slug: 'computadoras-21', nombre: 'Computadoras', descripcion: 'Equipos publicados.', totalProductos }]
      })
    });
  });
}

async function esperarDemo(page: Page): Promise<void> {
  await expect(page.getByRole('status').filter({ hasText: '14 productos encontrados' })).toBeVisible();
  await expect(page.locator('article.product-card')).toHaveCount(12);
}

test.describe('VariStoreHn Fase 3 — catálogo público independiente', () => {
  test.describe.configure({ retries: 0 });

  test('ruta pública independiente reutiliza header, pagina el demo y responde sin overflow', async ({ page }) => {
    await prepararEmpresa(page);
    await page.setViewportSize({ width: 1366, height: 900 });
    await page.goto('/varistorehn/productos');
    await esperarDemo(page);

    await expect(page.getByRole('heading', { level: 1, name: 'Productos', exact: true })).toBeVisible();
    const header = page.locator('app-varistorehn-header');
    await expect(header.getByRole('link', { name: 'Productos', exact: true }).first())
      .toHaveAttribute('href', '/varistorehn/productos');
    await expect(header.locator('.skip-link')).toHaveAttribute('href', '#catalogo-productos');
    await expect(page.getByRole('navigation', { name: 'Paginación del catálogo' })).toBeVisible();

    await page.getByRole('button', { name: 'Siguiente →' }).click();
    await expect(page.locator('article.product-card')).toHaveCount(2);
    await expect(page.getByText('Página', { exact: false }).filter({ hasText: '2 de 2' })).toBeVisible();
    await page.getByRole('button', { name: '← Anterior' }).click();
    await expect(page.locator('article.product-card')).toHaveCount(12);

    for (const width of [1366, 1000, 760, 390, 320]) {
      await page.setViewportSize({ width, height: 900 });
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
      expect(overflow, `overflow horizontal a ${width}px`).toBeLessThanOrEqual(0);
    }

    await page.setViewportSize({ width: 390, height: 844 });
    const filtros = page.getByRole('button', { name: 'Filtros', exact: true });
    await expect(filtros).toBeVisible();
    const altoFiltro = await filtros.evaluate(element => Math.round(element.getBoundingClientRect().height));
    expect(altoFiltro).toBeGreaterThanOrEqual(44);
    const agregar = page.locator('article.product-card').first().getByRole('button', { name: /^Agregar / });
    const altoAgregar = await agregar.evaluate(element => Math.round(element.getBoundingClientRect().height));
    expect(altoAgregar).toBeGreaterThanOrEqual(44);
  });

  test('búsqueda, categoría, disponibilidad, precio y orden modifican el grid real de la página', async ({ page }) => {
    await prepararEmpresa(page);
    await page.goto('/varistorehn/productos');
    await esperarDemo(page);

    const header = page.locator('app-varistorehn-header');
    const search = header.getByRole('searchbox', { name: 'Buscar productos, marcas o modelos' });
    await search.fill('Wireless Studio');
    await expect(page.getByRole('status').filter({ hasText: '1 productos encontrados' })).toBeVisible();
    await expect(page.locator('article.product-card')).toHaveCount(1);
    await expect(page.locator('article.product-card')).toContainText('Audífonos Wireless Studio');
    await search.press('Enter');
    await expect(page).toHaveURL(/\/varistorehn\/productos\?q=Wireless(?:%20|\+)Studio$/);

    await page.getByRole('button', { name: 'Limpiar' }).click();
    await expect(page).toHaveURL(/\/varistorehn\/productos$/);
    await expect(page.getByRole('status').filter({ hasText: '14 productos encontrados' })).toBeVisible();

    const filtros = page.locator('aside.filters');
    await filtros.getByRole('button', { name: /Hogar inteligente/ }).click();
    await expect(page).toHaveURL(/\/varistorehn\/productos\?categoria=demo-categoria-6$/);
    await expect(page.getByRole('status').filter({ hasText: '2 productos encontrados' })).toBeVisible();
    await expect(page.locator('article.product-card')).toHaveCount(2);

    await filtros.getByText('Solo disponibles', { exact: true }).click();
    await expect(page.getByRole('status').filter({ hasText: '1 productos encontrados' })).toBeVisible();
    await expect(page.locator('article.product-card')).toContainText('Bocina Home Sound');
    await expect(page.locator('article.product-card')).not.toContainText('Cámara Home Connect');

    await page.getByRole('button', { name: 'Limpiar' }).click();
    const precio = page.getByRole('spinbutton', { name: 'Precio máximo' });
    await precio.fill('1000');
    await expect(page.getByRole('status').filter({ hasText: '3 productos encontrados' })).toBeVisible();

    await precio.fill('');
    await expect(page.getByRole('status').filter({ hasText: '14 productos encontrados' })).toBeVisible();
    await page.getByLabel('Ordenar por').selectOption('precio-asc');
    await expect(page.locator('article.product-card').first().getByRole('heading')).toHaveText('Hub USB-C Connect');
  });

  test('query params profundos hidratan búsqueda y categoría por slug sin volver al home', async ({ page }) => {
    await prepararEmpresa(page);
    await page.goto('/varistorehn/productos?q=wireless');
    await expect(page.getByRole('status').filter({ hasText: '1 productos encontrados' })).toBeVisible();
    await expect(page.locator('article.product-card')).toContainText('Audífonos Wireless Studio');
    await expect(page.locator('app-varistorehn-header').getByRole('searchbox')).toHaveValue('wireless');

    await page.goto('/varistorehn/productos?categoria=demo-categoria-2');
    await expect(page.locator('#catalog-results-title')).toHaveText('Audio');
    await expect(page.getByRole('status').filter({ hasText: '3 productos encontrados' })).toBeVisible();
    await expect(page.locator('article.product-card')).toHaveCount(3);

    await page.goto('/varistorehn/productos?categoria=slug-que-no-existe');
    await expect(page.getByRole('alert')).toContainText('La categoría indicada ya no está disponible');
    await expect(page.getByRole('status').filter({ hasText: '14 productos encontrados' })).toBeVisible();
    await expect(page.locator('article.product-card')).toHaveCount(12);
  });

  test('selección de modelo y carrito mantienen referencias seguras y continuidad con la ruta canónica', async ({ page }) => {
    await prepararEmpresa(page);
    await page.goto('/varistorehn/productos');
    await esperarDemo(page);

    const laptop = page.locator('article.product-card').filter({ hasText: 'Laptop Pro 14' });
    const modelo = laptop.getByLabel('Modelo de Laptop Pro 14');
    await modelo.selectOption({ label: '16 GB / 512 GB' });
    await expect(laptop).toContainText('20,990');
    await laptop.getByRole('button', { name: 'Agregar Laptop Pro 14' }).click();

    const header = page.locator('app-varistorehn-header');
    await expect(header.getByRole('button', { name: 'Abrir carrito con 1 unidades' })).toBeVisible();
    await expect(header.locator('.cart-copy small')).toContainText('20,990');

    const persistido = await page.evaluate(() => {
      const valor = localStorage.getItem('varistorehn:carrito:v2:901:demo');
      return valor ? JSON.parse(valor) : null;
    });
    expect(persistido).toHaveLength(1);
    expect(Object.keys(persistido[0]).sort()).toEqual(['modeloClave', 'productoId', 'unidades']);
    expect(persistido[0].productoId).toBe(1);
    expect(persistido[0].unidades).toBe(1);

    await page.goto('/varistorehn/categorias');
    const categoriesHeader = page.locator('app-varistorehn-header');
    await expect(categoriesHeader.getByRole('button', { name: 'Abrir carrito con 1 unidades' })).toBeVisible();
    await expect(categoriesHeader.locator('.cart-copy small')).toContainText('20,990');
    await categoriesHeader.getByRole('button', { name: 'Abrir carrito con 1 unidades' }).click();
    await expect(page).toHaveURL(/\/varistorehn\/carrito$/);
    const carrito = page.locator('.cart-item').filter({ hasText: 'Laptop Pro 14' });
    await expect(carrito).toBeVisible();
    await expect(carrito).toContainText('16 GB / 512 GB');
    await expect(page.locator('app-varistorehn-header').getByRole('button', { name: 'Abrir carrito con 1 unidades' })).toBeVisible();
  });

  test('fuente real pagina y busca en servidor sin descargar el catálogo completo', async ({ page }) => {
    await prepararEmpresa(page);
    await mockCategoriasReales(page, 97);
    const solicitudes: Array<{ pagina: number; pageSize: number; search: string }> = [];

    await page.route('**/tienda/productos?*', async route => {
      const url = new URL(route.request().url());
      const pagina = Number(url.searchParams.get('page') || '1');
      const pageSize = Number(url.searchParams.get('pageSize') || '12');
      const search = url.searchParams.get('search') || '';
      solicitudes.push({ pagina, pageSize, search });

      const items = search
        ? [productoReal(97, 'Hallazgo Página Dos', 2500)]
        : Array.from({ length: pageSize }, (_, index) =>
            productoReal(index + 1, `Producto real ${String(index + 1).padStart(3, '0')}`, 1000 + index));

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({
          success: true,
          data: {
            items,
            page: pagina,
            pageSize,
            totalCount: search ? 1 : 97
          }
        })
      });
    });

    await page.goto('/varistorehn/productos');
    await activarBaseDatos(page);
    await expect(page.getByRole('status').filter({ hasText: '97 productos encontrados' })).toBeVisible();
    expect(solicitudes.some(item => item.pagina === 1 && item.pageSize === 12 && item.search === '')).toBeTruthy();
    expect(solicitudes.some(item => item.pagina === 2 && item.search === '')).toBeFalsy();

    const search = page.locator('app-varistorehn-header').getByRole('searchbox');
    await search.fill('Hallazgo Página Dos');
    await search.press('Enter');

    await expect(page.getByRole('status').filter({ hasText: '1 productos encontrados' })).toBeVisible();
    await expect(page.locator('article.product-card')).toHaveCount(1);
    await expect(page.locator('article.product-card')).toContainText('Hallazgo Página Dos');
    expect(solicitudes.some(item => item.search === 'Hallazgo Página Dos' && item.pagina === 1 && item.pageSize === 12)).toBeTruthy();
    await expect(page.getByText('Datos de la tienda', { exact: true }).last()).toBeVisible();
  });

  test('catálogo real vacío muestra empty y nunca fabrica los catorce fixtures demo', async ({ page }) => {
    await prepararEmpresa(page);
    await page.route('**/tienda/productos?*', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: true, data: { items: [], page: 1, pageSize: 96, totalCount: 0 } })
      });
    });
    await page.route('**/tienda/categorias', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: true, data: [] })
      });
    });

    await page.goto('/varistorehn/productos');
    await activarBaseDatos(page);
    await expect(page.getByRole('heading', { name: 'Aún no hay productos públicos disponibles' })).toBeVisible();
    await expect(page.locator('article.product-card')).toHaveCount(0);
    await expect(page.getByText('Laptop Pro 14', { exact: true })).toHaveCount(0);
  });

  test('error del catálogo real permanece visible y no cae silenciosamente a datos demo', async ({ page }) => {
    await prepararEmpresa(page);
    await page.route('**/tienda/productos?*', async route => {
      await route.fulfill({
        status: 503,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: false, message: 'Servicio no disponible' })
      });
    });
    await page.route('**/tienda/categorias', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: true, data: [] })
      });
    });

    await page.goto('/varistorehn/productos');
    await activarBaseDatos(page);
    const alert = page.getByRole('alert');
    await expect(alert).toContainText('No pudimos cargar el catálogo');
    await expect(alert).toContainText('No se sustituyeron los datos reales por ejemplos');
    await expect(alert.getByRole('button', { name: 'Intentar de nuevo' })).toBeVisible();
    await expect(page.locator('article.product-card')).toHaveCount(0);
    await expect(page.getByText('Laptop Pro 14', { exact: true })).toHaveCount(0);
  });

  test('fallo de categorías reales no inventa categorías y mantiene disponible el catálogo recibido', async ({ page }) => {
    await prepararEmpresa(page);
    await page.route('**/tienda/productos?*', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: true, data: { items: [productoReal(501, 'Producto independiente')], page: 1, pageSize: 96, totalCount: 1 } })
      });
    });
    await page.route('**/tienda/categorias', async route => {
      await route.fulfill({
        status: 503,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: false, message: 'Categorías no disponibles' })
      });
    });

    await page.goto('/varistorehn/productos');
    await activarBaseDatos(page);
    await expect(page.getByRole('status').filter({ hasText: '1 productos encontrados' })).toBeVisible();
    await expect(page.locator('article.product-card')).toContainText('Producto independiente');
    await expect(page.locator('aside.filters').getByRole('alert')).toContainText('No pudimos cargar las categorías públicas');
    await expect(page.locator('aside.filters').getByRole('button', { name: /Audio/ })).toHaveCount(0);
  });
});
