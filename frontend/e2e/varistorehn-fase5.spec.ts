import { expect, Page, test } from '@playwright/test';

const empresaBase = {
  id: 905,
  nombreComercial: 'VariStore Cart Audit',
  nombreVisibleSistema: 'VariStore Cart Audit',
  eslogan: 'Compra simple y segura',
  descripcionSistema: 'Tienda pública de auditoría',
  mensajeLogin: 'Administración',
  copyright: '© 2026 VariStore Cart Audit',
  mostrarCopyright: true,
  usarAnioAutomaticoCopyright: true,
  encabezadoActivo: true,
  encabezadoTexto: 'Atención pública',
  piePaginaActivo: true,
  piePaginaTexto: 'Pie público',
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

function productoReal(precio = 1750, stock = 3) {
  return {
    id: 501,
    slug: 'producto-carrito-real-501',
    nombre: 'Producto Carrito Real',
    descripcion: 'Producto real para auditar rehidratación del carrito.',
    categoriaId: 21,
    categoriaNombre: 'Computadoras',
    marcaNombre: 'Marca real',
    precio,
    precioOferta: null,
    cantidadDisponible: stock,
    estaAgotado: stock <= 0,
    sku: 'SKU-501',
    activo: true,
    esDestacado: false,
    imagenes: [],
    modelos: [{
      modeloId: 5010,
      modeloNombre: 'Modelo auditoría',
      marcaNombre: 'Marca real',
      sku: 'SKU-501-A',
      precio,
      cantidadDisponible: stock,
      estaAgotado: stock <= 0,
      imagenes: []
    }]
  };
}

async function mockCatalogoReal(page: Page, precio = 1750, stock = 3): Promise<void> {
  const producto = productoReal(precio, stock);
  await page.route('**/tienda/productos?*', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({ success: true, data: { items: [producto], page: 1, pageSize: 96, totalCount: 1 } })
    });
  });
  await page.route('**/tienda/productos/contexto', async route => {
    const body = route.request().postDataJSON() as { productoIds?: number[] } | null;
    const ids = new Set(Array.isArray(body?.productoIds) ? body!.productoIds : []);
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({ success: true, data: ids.has(producto.id) ? [producto] : [] })
    });
  });
  await page.route('**/tienda/categorias', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({ success: true, data: [{ id: 21, slug: 'computadoras-21', nombre: 'Computadoras', descripcion: '', totalProductos: 1 }] })
    });
  });
}

async function limpiarCarritos(page: Page): Promise<void> {
  await page.evaluate(() => {
    Object.keys(localStorage).filter(key => key.startsWith('varistorehn:carrito:')).forEach(key => localStorage.removeItem(key));
  });
}

async function abrirCatalogoDemo(page: Page): Promise<void> {
  await page.goto('/varistorehn/productos');
  await expect(page.getByRole('heading', { level: 1, name: 'Productos' })).toBeVisible();
}

test.describe('VariStoreHn Fase 5 — carrito global y persistente', () => {
  test.describe.configure({ retries: 0 });

  test('ruta canónica vacía muestra estado útil, copy público y no ofrece checkout incompleto', async ({ page }) => {
    await prepararEmpresa(page);
    await page.goto('/varistorehn/carrito');
    await limpiarCarritos(page);
    await page.reload();

    await expect(page).toHaveURL(/\/varistorehn\/carrito$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Mi carrito' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Encuentra algo que te encante' })).toBeVisible();
    await expect(page.getByText('Agrega al menos un producto antes de continuar con tu compra.')).toBeVisible();
    await expect(page.getByText(/Fase 6/)).toHaveCount(0);
    await expect(page.locator('a[href="/varistorehn/checkout"]')).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'Explorar productos' })).toHaveAttribute('href', '/varistorehn/productos');
    await expect(page.locator('app-varistorehn-header').getByRole('button', { name: 'Abrir carrito con 0 unidades' })).toBeVisible();
  });

  test('home usa una sola ruta de carrito, conserva el store global y no ejecuta cierre de compra', async ({ page }) => {
    await prepararEmpresa(page);
    await page.goto('/varistorehn');
    await limpiarCarritos(page);
    await page.reload();

    await expect(page.getByRole('heading', { name: /Todo lo que buscas/i })).toBeVisible();
    await expect(page.locator('dialog.cart-dialog')).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Pedir por WhatsApp' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Continuar con tarjeta' })).toHaveCount(0);
    await expect(page.locator('article.product-card')).toHaveCount(0);

    const homeHeader = page.locator('app-varistorehn-header');
    await expect(homeHeader.getByRole('button', { name: 'Abrir carrito' })).toBeVisible();
    await expect(homeHeader.locator('.cart-copy small')).toHaveText('Ver carrito');
    await homeHeader.getByRole('link', { name: 'Productos', exact: true }).click();
    await expect(page).toHaveURL(/\/varistorehn\/productos$/);
    await expect(page.getByRole('status').filter({ hasText: '14 productos encontrados' })).toBeVisible();

    const tarjeta = page.locator('article.product-card').filter({ hasText: 'Laptop Pro 14' });
    await tarjeta.getByRole('button', { name: 'Agregar Laptop Pro 14' }).click();
    const catalogHeader = page.locator('app-varistorehn-header');
    await expect(catalogHeader.getByRole('button', { name: 'Abrir carrito con 1 unidades' })).toBeVisible();
    await catalogHeader.getByRole('link', { name: 'Inicio', exact: true }).click();

    await expect(page).toHaveURL(/\/varistorehn$/);
    await expect(page.locator('app-varistorehn-header').getByRole('button', { name: 'Abrir carrito con 1 unidades' })).toBeVisible();
    await page.locator('app-varistorehn-header').getByRole('button', { name: 'Abrir carrito con 1 unidades' }).click();
    await expect(page).toHaveURL(/\/varistorehn\/carrito$/);
    await expect(page.locator('.cart-item').filter({ hasText: 'Laptop Pro 14' })).toBeVisible();

    await page.goto('/varistorehn?carrito=1');
    await expect(page).toHaveURL(/\/varistorehn\/carrito$/);
  });

  test('agregar desde catálogo sincroniza header, página de carrito y sobrevive recarga', async ({ page }) => {
    await prepararEmpresa(page);
    await abrirCatalogoDemo(page);
    await limpiarCarritos(page);
    await page.reload();

    const tarjeta = page.locator('article.product-card').filter({ hasText: 'Laptop Pro 14' });
    await tarjeta.getByRole('button', { name: 'Agregar Laptop Pro 14' }).click();
    await expect(page.locator('app-varistorehn-header').getByRole('button', { name: 'Abrir carrito con 1 unidades' })).toBeVisible();

    await page.locator('app-varistorehn-header').getByRole('button', { name: 'Abrir carrito con 1 unidades' }).click();
    await expect(page).toHaveURL(/\/varistorehn\/carrito$/);
    await expect(page.locator('.cart-item').filter({ hasText: 'Laptop Pro 14' })).toBeVisible();
    await expect(page.locator('.cart-summary')).toContainText('1');

    const persistido = await page.evaluate(() => {
      const raw = localStorage.getItem('varistorehn:carrito:v2:905:demo');
      return raw ? JSON.parse(raw) : null;
    });
    expect(persistido).toHaveLength(1);
    expect(Object.keys(persistido[0]).sort()).toEqual(['modeloClave', 'productoId', 'unidades']);
    expect(persistido[0].unidades).toBe(1);

    await page.reload();
    await expect(page.locator('.cart-item').filter({ hasText: 'Laptop Pro 14' })).toBeVisible();
    await expect(page.locator('app-varistorehn-header').getByRole('button', { name: 'Abrir carrito con 1 unidades' })).toBeVisible();
  });

  test('editar cantidades recalcula total, respeta stock y eliminar es explícito', async ({ page }) => {
    await prepararEmpresa(page);
    await abrirCatalogoDemo(page);
    await limpiarCarritos(page);
    await page.reload();
    await page.locator('article.product-card').filter({ hasText: 'Laptop Pro 14' }).getByRole('button', { name: 'Agregar Laptop Pro 14' }).click();
    await page.locator('app-varistorehn-header').getByRole('button', { name: 'Abrir carrito con 1 unidades' }).click();

    const item = page.locator('.cart-item').filter({ hasText: 'Laptop Pro 14' });
    const cantidad = item.getByRole('spinbutton', { name: 'Cantidad de Laptop Pro 14' });
    const aumentar = item.getByRole('button', { name: 'Aumentar cantidad de Laptop Pro 14' });
    const disminuir = item.getByRole('button', { name: 'Disminuir cantidad de Laptop Pro 14' });

    await expect(cantidad).toHaveValue('1');
    await expect(disminuir).toBeDisabled();
    await aumentar.click();
    await expect(cantidad).toHaveValue('2');
    await expect(page.locator('.cart-summary')).toContainText('36,980');

    await cantidad.fill('999');
    await cantidad.press('Tab');
    await expect(cantidad).toHaveValue('5');
    await expect(aumentar).toBeDisabled();
    await expect(page.locator('app-varistorehn-header').getByRole('button', { name: 'Abrir carrito con 5 unidades' })).toBeVisible();

    await item.getByRole('button', { name: 'Eliminar Laptop Pro 14 del carrito' }).click();
    await expect(page.getByRole('heading', { name: 'Encuentra algo que te encante' })).toBeVisible();
    await expect(page.locator('app-varistorehn-header').getByRole('button', { name: 'Abrir carrito con 0 unidades' })).toBeVisible();
  });

  test('detalle descuenta unidades existentes y limita el selector al stock restante', async ({ page }) => {
    await prepararEmpresa(page);
    await page.goto('/varistorehn/producto/demo-producto-1');
    await limpiarCarritos(page);
    await page.reload();

    const cantidad = page.getByRole('spinbutton', { name: 'Cantidad de producto' });
    await cantidad.fill('2');
    await cantidad.press('Tab');
    await page.getByRole('button', { name: 'Agregar al carrito' }).click();

    await expect(cantidad).toHaveAttribute('max', '3');
    await expect(cantidad).toHaveValue('1');
    await cantidad.fill('999');
    await cantidad.press('Tab');
    await expect(cantidad).toHaveValue('3');
    await page.getByRole('button', { name: 'Agregar al carrito' }).click();
    await expect(page.locator('app-varistorehn-header').getByRole('button', { name: 'Abrir carrito con 5 unidades' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Stock ya agregado', exact: true })).toBeDisabled();
  });

  test('rehidratación real recalcula precio/stock y no confía en valores persistidos', async ({ page }) => {
    await prepararEmpresa(page);
    await mockCatalogoReal(page, 1750, 3);
    await page.goto('/varistorehn/carrito');
    await page.evaluate(() => {
      localStorage.setItem('varistorehn:carrito:v2:905:bd', JSON.stringify([{
        productoId: 501,
        modeloClave: JSON.stringify([5010, 'Modelo auditoría', 'Marca real']),
        unidades: 99,
        precio: 1,
        stock: 9999
      }]));
    });

    await page.getByRole('group', { name: 'Origen de datos' }).getByRole('button', { name: 'Base de datos' }).click();
    const item = page.locator('.cart-item').filter({ hasText: 'Producto Carrito Real' });
    await expect(item).toBeVisible();
    await expect(item.getByRole('spinbutton', { name: 'Cantidad de Producto Carrito Real' })).toHaveValue('3');
    await expect(item.locator('.item-price')).toContainText('1,750');
    await expect(item.locator('.item-total')).toContainText('5,250');

    const persistido = await page.evaluate(() => JSON.parse(localStorage.getItem('varistorehn:carrito:v2:905:bd') || '[]'));
    expect(persistido).toHaveLength(1);
    expect(Object.keys(persistido[0]).sort()).toEqual(['modeloClave', 'productoId', 'unidades']);
    expect(persistido[0].unidades).toBe(3);
  });

  test('carrito es usable sin overflow en móvil estrecho', async ({ page }) => {
    await prepararEmpresa(page);
    await page.setViewportSize({ width: 320, height: 760 });
    await abrirCatalogoDemo(page);
    await limpiarCarritos(page);
    await page.reload();
    await page.locator('article.product-card').filter({ hasText: 'Laptop Pro 14' }).getByRole('button', { name: 'Agregar Laptop Pro 14' }).click();
    await page.locator('app-varistorehn-header').getByRole('button', { name: /Abrir carrito con 1 unidades/ }).click();

    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow).toBeLessThanOrEqual(1);
    await expect(page.locator('.cart-item')).toBeVisible();
    await expect(page.locator('.quantity-control button').first()).toHaveCSS('min-height', '44px');
    await expect(page.locator('.cart-summary')).toBeVisible();
  });
});
