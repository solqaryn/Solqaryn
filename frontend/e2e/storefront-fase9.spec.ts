import { expect, Page, test } from '@playwright/test';

const empresa = {
  id: 909, nombreComercial: 'Storefront Fase 9', nombreVisibleSistema: 'Storefront Fase 9',
  eslogan: 'Ofertas reales, inventario claro', descripcionSistema: 'Auditoría Fase 9', mensajeLogin: 'Administración',
  copyright: '© 2026 Storefront', mostrarCopyright: true, usarAnioAutomaticoCopyright: true,
  encabezadoActivo: true, encabezadoTexto: 'Catálogo público', piePaginaActivo: true,
  piePaginaTexto: 'Catálogo público', moneda: 'HNL', zonaHoraria: 'America/Tegucigalpa', formatoFecha: 'dd/MM/yyyy',
  whatsApp: '9876-5432'
};

function producto(id: number, nombre: string, precio: number, stock: number, opciones: {
  oferta?: number | null; activa?: boolean; estado?: string; categoriaId?: number; categoria?: string;
} = {}) {
  const oferta = opciones.oferta ?? null;
  const activa = opciones.activa === true && oferta !== null && oferta < precio;
  const estado = opciones.estado ?? (stock <= 0 ? 'Agotado' : stock <= 3 ? 'Últimas unidades' : 'Disponible');
  return {
    id, slug: `${nombre.toLowerCase().replace(/[^a-z0-9]+/g, '-')}-${id}`, nombre,
    descripcion: `Descripción de ${nombre}`, categoriaId: opciones.categoriaId ?? 81,
    categoriaNombre: opciones.categoria ?? 'Tecnología', marcaNombre: 'Marca F9', modeloNombre: 'Base',
    precio, precioOferta: activa ? oferta : null, ofertaActiva: activa,
    ofertaNombre: activa ? 'Semana Storefront' : null,
    ofertaInicioUtc: activa ? '2026-09-18T00:00:00Z' : null,
    ofertaFinUtc: activa ? '2026-09-30T23:59:59Z' : null,
    ahorro: activa ? precio - oferta! : 0,
    porcentajeAhorro: activa ? Math.round((precio - oferta!) * 100 / precio) : 0,
    cantidadDisponible: stock, estaAgotado: stock <= 0, estadoDisponibilidad: estado,
    sku: `F9-${id}`, activo: true, esDestacado: false, fechaCreacion: '2026-09-18T12:00:00Z',
    imagenPrincipalUrl: null, imagenes: [],
    modelos: [{
      productoVarianteId: id * 10, modeloId: id * 100, modeloNombre: 'Base', marcaNombre: 'Marca F9', sku: `F9-${id}-A`,
      precio, precioOferta: activa ? oferta : null, ofertaActiva: activa,
      ofertaNombre: activa ? 'Semana Storefront' : null,
      ofertaInicioUtc: activa ? '2026-09-18T00:00:00Z' : null,
      ofertaFinUtc: activa ? '2026-09-30T23:59:59Z' : null,
      ahorro: activa ? precio - oferta! : 0,
      porcentajeAhorro: activa ? Math.round((precio - oferta!) * 100 / precio) : 0,
      cantidadDisponible: stock, estaAgotado: stock <= 0, estadoDisponibilidad: estado, imagenes: []
    }]
  };
}

const activo = producto(901, 'Audífonos Oferta', 1200, 2, { oferta: 900, activa: true, estado: 'Últimas unidades' });
const vencido = producto(902, 'Bocina Oferta Vencida', 800, 8, { oferta: null, activa: false });
const agotado = producto(903, 'Teclado Agotado', 600, 0, { estado: 'Agotado' });
const normal = producto(904, 'Mouse Normal', 500, 10, { estado: 'Disponible' });
const catalogo = [activo, vencido, agotado, normal];

async function preparar(page: Page): Promise<void> {
  await page.route('http://localhost:5005/empresa-configuracion/publica', route => route.fulfill({
    status: 200, contentType: 'application/json', headers: { 'Access-Control-Allow-Origin': '*' },
    body: JSON.stringify({ success: true, data: empresa })
  }));
  await page.route('**/tienda/categorias', route => route.fulfill({
    status: 200, contentType: 'application/json', headers: { 'Access-Control-Allow-Origin': '*' },
    body: JSON.stringify({ success: true, data: [{ id: 81, slug: 'tecnologia-81', nombre: 'Tecnología', descripcion: '', totalProductos: 4 }] })
  }));
  await page.route('**/tienda/productos?*', route => route.fulfill({
    status: 200, contentType: 'application/json', headers: { 'Access-Control-Allow-Origin': '*' },
    body: JSON.stringify({ success: true, data: { items: catalogo, page: 1, pageSize: 96, totalCount: catalogo.length } })
  }));
  await page.route('**/tienda/productos/*', route => {
    const url = new URL(route.request().url());
    if (route.request().method() === 'POST' && url.pathname.endsWith('/tienda/productos/contexto')) {
      const body = route.request().postDataJSON() as { productoIds?: number[] } | null;
      const ids = new Set(Array.isArray(body?.productoIds) ? body!.productoIds : []);
      return route.fulfill({
        status: 200, contentType: 'application/json', headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: true, data: catalogo.filter(item => ids.has(item.id)) })
      });
    }
    const slug = decodeURIComponent(url.pathname.split('/').pop() || '');
    const encontrado = catalogo.find(item => item.slug === slug);
    return route.fulfill({
      status: encontrado ? 200 : 404, contentType: 'application/json', headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify(encontrado ? { success: true, data: encontrado } : { success: false, message: 'Producto no encontrado.' })
    });
  });
}

async function activarBaseDatos(page: Page): Promise<void> {
  await page.getByRole('group', { name: 'Origen de datos' }).getByRole('button', { name: 'Base de datos' }).click();
}

test.describe('Storefront Fase 9 — ofertas e inventario', () => {
  test.describe.configure({ retries: 0 });

  test('/ofertas muestra solo promociones activas y nunca una vencida', async ({ page }) => {
    await preparar(page);
    await page.goto('/tienda/ofertas');
    await activarBaseDatos(page);

    await expect(page.getByRole('heading', { level: 1, name: 'Ofertas' })).toBeVisible();
    await expect(page.locator('article.product-card')).toHaveCount(1);
    const tarjeta = page.locator('article.product-card');
    await expect(tarjeta).toContainText('Audífonos Oferta');
    await expect(tarjeta).toContainText('L 900.00');
    await expect(tarjeta).toContainText('Ahorras');
    await expect(tarjeta.locator('.discount-badge')).toHaveText('-25%');
    await expect(page.getByText('Bocina Oferta Vencida')).toHaveCount(0);
    await expect(page.locator('app-storefront-header').getByRole('link', { name: 'Ofertas', exact: true })).toHaveAttribute('href', '/tienda/ofertas');
  });

  test('promoción activa conserva el mismo precio en catálogo, detalle y carrito', async ({ page }) => {
    await preparar(page);
    await page.goto('/tienda/productos?oferta=1');
    await activarBaseDatos(page);

    const tarjeta = page.locator('article.product-card').filter({ hasText: 'Audífonos Oferta' });
    await expect(tarjeta).toContainText('L 900.00');
    await expect(tarjeta).toContainText('L 1,200.00');
    await tarjeta.getByRole('link', { name: 'Ver producto' }).click();

    await activarBaseDatos(page);
    await expect(page.locator('.previous-price')).toContainText('1,200');
    await expect(page.locator('.price-block strong')).toContainText('900');
    await expect(page.locator('.price-block')).toContainText('Ahorras');
    await page.getByRole('button', { name: 'Agregar al carrito' }).click();
    await page.locator('app-storefront-header').getByRole('button', { name: 'Abrir carrito con 1 unidades' }).click();
    await activarBaseDatos(page);

    const item = page.locator('.cart-item').filter({ hasText: 'Audífonos Oferta' });
    await expect(item.locator('.item-price')).toContainText('1,200');
    await expect(item.locator('.item-price')).toContainText('900');
    await expect(item.locator('.item-price')).toContainText('Ahorras');
  });

  test('oferta vencida vuelve al precio normal y no muestra badge promocional', async ({ page }) => {
    await preparar(page);
    await page.goto(`/tienda/producto/${vencido.slug}`);
    await activarBaseDatos(page);

    await expect(page.getByRole('heading', { level: 1, name: 'Bocina Oferta Vencida' })).toBeVisible();
    await expect(page.locator('.previous-price')).toHaveCount(0);
    await expect(page.locator('.price-block strong')).toContainText('800');
    await expect(page.locator('.price-block')).not.toContainText('Ahorras');
  });

  test('stock usa estados canónicos y agotado bloquea todas las rutas de agregado', async ({ page }) => {
    await preparar(page);
    await page.goto('/tienda/productos');
    await activarBaseDatos(page);

    const baja = page.locator('article.product-card').filter({ hasText: 'Audífonos Oferta' });
    await expect(baja.locator('.availability')).toHaveText('Últimas unidades');

    const sinStock = page.locator('article.product-card').filter({ hasText: 'Teclado Agotado' });
    await expect(sinStock.locator('.availability')).toHaveText('Agotado');
    await expect(sinStock.getByRole('button', { name: 'Agregar Teclado Agotado' })).toBeDisabled();

    await sinStock.getByRole('link', { name: 'Ver producto' }).click();
    await activarBaseDatos(page);
    await expect(page.locator('.availability strong')).toHaveText('Agotado');
    await expect(page.getByRole('button', { name: 'Producto agotado' })).toBeDisabled();
    await expect(page.getByRole('button', { name: 'Comprar por WhatsApp' })).toBeDisabled();
  });

  test('filtro oferta=1 es compartible y persiste tras recarga', async ({ page }) => {
    await preparar(page);
    await page.goto('/tienda/productos?oferta=1');
    await activarBaseDatos(page);

    await expect(page.getByRole('checkbox', { name: 'Solo productos en oferta' })).toBeChecked();
    await expect(page.locator('article.product-card')).toHaveCount(1);
    await page.reload();
    await expect(page.getByRole('checkbox', { name: 'Solo productos en oferta' })).toBeChecked();
    await expect(page).toHaveURL(/oferta=1/);

    // La fuente BD es un control de vista previa de dev, no estado URL persistente.
    // Rehidratarla tras la recarga mantiene la prueba enfocada en la persistencia del filtro.
    await activarBaseDatos(page);
    await expect(page.locator('article.product-card')).toHaveCount(1);
  });
});
