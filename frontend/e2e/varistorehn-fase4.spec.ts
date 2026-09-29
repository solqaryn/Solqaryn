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

const imagenData = (texto: string) => `data:image/svg+xml,${encodeURIComponent(
  `<svg xmlns="http://www.w3.org/2000/svg" width="800" height="600"><rect width="800" height="600" fill="white"/><text x="400" y="300" text-anchor="middle" font-size="48">${texto}</text></svg>`
)}`;

function productoReal(
  id: number,
  nombre: string,
  opciones: { slug?: string; categoriaId?: number; categoria?: string; precio?: number; oferta?: number | null; stock?: number; imagenes?: string[] } = {}
) {
  const precio = opciones.precio ?? 1499;
  const stock = opciones.stock ?? 4;
  const oferta = opciones.oferta ?? null;
  const ofertaActiva = oferta !== null && oferta >= 0 && oferta < precio;
  const imagenes = opciones.imagenes ?? [];
  const imagenesDto = imagenes.map((url, index) => ({ url, orden: index + 1, esPrincipal: index === 0 }));
  return {
    id,
    slug: opciones.slug ?? `producto-real-${id}`,
    nombre,
    descripcion: `Descripción pública y verificable de ${nombre}.`,
    categoriaId: opciones.categoriaId ?? 21,
    categoriaNombre: opciones.categoria ?? 'Computadoras',
    marcaNombre: 'Marca real',
    precio,
    precioOferta: ofertaActiva ? oferta : null,
    ofertaActiva,
    ofertaNombre: ofertaActiva ? 'Oferta de auditoría' : null,
    ahorro: ofertaActiva ? precio - oferta! : 0,
    porcentajeAhorro: ofertaActiva ? Math.round((precio - oferta!) * 100 / precio) : 0,
    cantidadDisponible: stock,
    estaAgotado: stock <= 0,
    estadoDisponibilidad: stock <= 0 ? 'Agotado' : stock <= 3 ? 'Últimas unidades' : 'Disponible',
    sku: `SKU-${id}`,
    activo: true,
    esDestacado: false,
    imagenes: imagenesDto,
    modelos: [{
      modeloId: id * 10,
      modeloNombre: '16 GB / 512 GB',
      marcaNombre: 'Marca real',
      sku: `SKU-${id}-A`,
      precio,
      precioOferta: ofertaActiva ? oferta : null,
      ofertaActiva,
      ofertaNombre: ofertaActiva ? 'Oferta de auditoría' : null,
      ahorro: ofertaActiva ? precio - oferta! : 0,
      porcentajeAhorro: ofertaActiva ? Math.round((precio - oferta!) * 100 / precio) : 0,
      cantidadDisponible: stock,
      estaAgotado: stock <= 0,
      estadoDisponibilidad: stock <= 0 ? 'Agotado' : stock <= 3 ? 'Últimas unidades' : 'Disponible',
      imagenes: imagenesDto
    }]
  };
}

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

async function mockFuenteReal(
  page: Page,
  producto: ReturnType<typeof productoReal>,
  catalogo: ReturnType<typeof productoReal>[] = [producto],
  detalleStatus = 200
): Promise<void> {
  await page.route('**/tienda/productos?*', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({ success: true, data: { items: catalogo, page: 1, pageSize: 96, totalCount: catalogo.length } })
    });
  });

  await page.route('**/tienda/productos/*', async route => {
    const url = new URL(route.request().url());
    if (route.request().method() === 'POST' && url.pathname.endsWith('/tienda/productos/contexto')) {
      const body = route.request().postDataJSON() as { productoIds?: number[] } | null;
      const ids = new Set(Array.isArray(body?.productoIds) ? body!.productoIds : []);
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: true, data: catalogo.filter(item => ids.has(item.id)) })
      });
      return;
    }
    if (detalleStatus !== 200) {
      await route.fulfill({
        status: detalleStatus,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify({ success: false, message: detalleStatus === 404 ? 'Producto no encontrado.' : 'Servicio temporalmente no disponible.' })
      });
      return;
    }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({ success: true, data: producto })
    });
  });

  await page.route('**/tienda/categorias', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': '*' },
      body: JSON.stringify({
        success: true,
        data: [{ id: 21, slug: 'computadoras-21', nombre: 'Computadoras', descripcion: 'Equipos publicados.', totalProductos: catalogo.filter(item => item.categoriaId === 21).length }]
      })
    });
  });
}

async function esperarDetalleDemo(page: Page): Promise<void> {
  await expect(page.getByRole('heading', { level: 1, name: 'Laptop Pro 14' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Agregar al carrito' })).toBeEnabled();
}

async function gestoSwipeIzquierda(page: Page, selector: string): Promise<void> {
  const elemento = page.locator(selector);
  await elemento.dispatchEvent('pointerdown', { pointerId: 1, pointerType: 'touch', clientX: 310, clientY: 220, isPrimary: true });
  await elemento.dispatchEvent('pointerup', { pointerId: 1, pointerType: 'touch', clientX: 170, clientY: 224, isPrimary: true });
}

test.describe('VariStoreHn Fase 4 — detalle público de producto', () => {
  test.describe.configure({ retries: 0 });

  test('URL demo directa muestra detalle independiente y cantidad nunca supera stock', async ({ page }) => {
    await prepararEmpresa(page);
    await page.setViewportSize({ width: 1366, height: 900 });
    await page.goto('/varistorehn/producto/demo-producto-1');
    await esperarDetalleDemo(page);

    await expect(page).toHaveURL(/\/varistorehn\/producto\/demo-producto-1$/);
    await expect(page.getByRole('navigation', { name: 'Migas de pan' })).toContainText('Computadoras');
    await expect(page.locator('dialog.detail-dialog')).toHaveCount(0);
    await expect(page.locator('dialog.lightbox')).toHaveCount(1);

    const cantidad = page.getByRole('spinbutton', { name: 'Cantidad de producto' });
    await expect(cantidad).toHaveAttribute('max', '5');
    await cantidad.fill('999');
    await cantidad.press('Tab');
    await expect(cantidad).toHaveValue('5');

    await page.getByRole('button', { name: 'Agregar al carrito' }).click();
    const header = page.locator('app-varistorehn-header');
    await expect(header.getByRole('button', { name: 'Abrir carrito con 5 unidades' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Stock ya agregado', exact: true })).toBeDisabled();

    const persistido = await page.evaluate(() => {
      const valor = localStorage.getItem('varistorehn:carrito:v2:901:demo');
      return valor ? JSON.parse(valor) : null;
    });
    expect(persistido).toHaveLength(1);
    expect(Object.keys(persistido[0]).sort()).toEqual(['modeloClave', 'productoId', 'unidades']);
    expect(persistido[0].productoId).toBe(1);
    expect(persistido[0].unidades).toBe(5);
  });

  test('botón icono volver restaura exactamente búsqueda, variante y posición del catálogo', async ({ page }) => {
    await prepararEmpresa(page);
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/varistorehn/productos?q=Laptop&orden=precio-desc');
    const tarjeta = page.locator('article.product-card').filter({ hasText: 'Laptop Pro 14' });
    await expect(tarjeta).toBeVisible();

    const modeloCatalogo = tarjeta.getByLabel('Modelo de Laptop Pro 14');
    await modeloCatalogo.selectOption({ label: '16 GB / 512 GB' });
    const modeloAntes = await modeloCatalogo.inputValue();

    await tarjeta.scrollIntoViewIfNeeded();
    await page.evaluate(() => window.scrollBy(0, 90));
    const scrollAntes = await page.evaluate(() => Math.round(window.scrollY));

    const verProducto = tarjeta.getByRole('link', { name: 'Ver producto' });
    await expect(verProducto).toHaveAttribute('href', '/varistorehn/producto/demo-producto-1');
    await verProducto.click();
    await esperarDetalleDemo(page);
    await expect(page).toHaveURL(/\/varistorehn\/producto\/demo-producto-1$/);
    const retornoGuardado = await page.evaluate(() => sessionStorage.getItem('varistorehn:retorno-catalogo:v1'));
    expect(retornoGuardado).toContain(modeloAntes);

    const volver = page.getByRole('button', { name: 'Volver a la pantalla anterior' });
    await expect(volver).toBeVisible();
    await expect(volver.locator('app-store-icon')).toHaveCount(1);
    await expect(volver).toHaveText('');
    await volver.click();

    await expect(page).toHaveURL(/\/varistorehn\/productos\?q=Laptop&orden=precio-desc$/);
    await expect(page.locator('app-varistorehn-header').getByRole('searchbox')).toHaveValue('Laptop');
    await expect(tarjeta).toBeVisible();
    await expect(modeloCatalogo).toHaveValue(modeloAntes);
    await expect.poll(async () => Math.abs((await page.evaluate(() => Math.round(window.scrollY))) - scrollAntes))
      .toBeLessThanOrEqual(4);
    await expect.poll(() => page.evaluate(() => sessionStorage.getItem('varistorehn:retorno-catalogo:v1')))
      .toBeNull();
  });

  test('fuente real muestra galería, SKU, promoción y relacionados sin inventar datos', async ({ page }) => {
    await prepararEmpresa(page);
    const imagenes = [imagenData('Vista 1'), imagenData('Vista 2'), imagenData('Vista 3')];
    const principal = productoReal(101, 'Laptop Real Pro', { slug: 'laptop-real-pro-101', oferta: 1299, imagenes });
    const relacionado = productoReal(102, 'Laptop Relacionada', { slug: 'laptop-relacionada-102', precio: 1899, categoriaId: 21 });
    const otro = productoReal(103, 'Audio Externo', { slug: 'audio-externo-103', categoriaId: 22, categoria: 'Audio' });
    await mockFuenteReal(page, principal, [principal, relacionado, otro]);

    await page.goto('/varistorehn/producto/laptop-real-pro-101');
    await activarBaseDatos(page);

    await expect(page.getByRole('heading', { level: 1, name: 'Laptop Real Pro' })).toBeVisible();
    await expect(page.locator('.product-kicker')).toContainText('SKU SKU-101-A');
    await expect(page.locator('.previous-price')).toContainText('1,499');
    await expect(page.locator('.price-block strong')).toContainText('1,299');
    await expect(page.locator('.thumbnail')).toHaveCount(3);
    await expect(page.locator('.image-position').first()).toHaveText('1 / 3');

    await page.getByRole('button', { name: 'Imagen siguiente' }).click();
    await expect(page.locator('.image-position').first()).toHaveText('2 / 3');
    await gestoSwipeIzquierda(page, '.gallery-stage');
    await expect(page.locator('.image-position').first()).toHaveText('3 / 3');

    const relacionados = page.locator('.related-card');
    await expect(relacionados).toHaveCount(1);
    await expect(relacionados).toContainText('Laptop Relacionada');
    await expect(relacionados).not.toContainText('Audio Externo');
    await expect(page.locator('.spec-list')).toContainText('Marca real');
    await expect(page.locator('.spec-list')).toContainText('SKU-101-A');
  });

  test('lightbox amplía solo fotografías, conserva índice y devuelve foco al detalle', async ({ page }) => {
    await prepararEmpresa(page);
    const imagenes = [imagenData('Foto 1'), imagenData('Foto 2'), imagenData('Foto 3')];
    const principal = productoReal(111, 'Producto Fotográfico', { slug: 'producto-fotografico-111', imagenes });
    await mockFuenteReal(page, principal);

    await page.goto('/varistorehn/producto/producto-fotografico-111');
    await activarBaseDatos(page);
    const imagenPrincipal = page.getByRole('button', { name: /Ampliar imagen 1 de 3/ });
    await imagenPrincipal.click();

    const dialog = page.locator('dialog.lightbox');
    await expect(dialog).toBeVisible();
    await expect(dialog.getByRole('button', { name: 'Agregar al carrito' })).toHaveCount(0);
    await expect(dialog.locator('.lightbox-controls span')).toHaveText('1 / 3');
    await dialog.getByRole('button', { name: 'Imagen siguiente ampliada' }).click();
    await expect(dialog.locator('.lightbox-controls span')).toHaveText('2 / 3');

    await page.keyboard.press('Escape');
    await expect(dialog).not.toBeVisible();
    await expect(page.locator('.image-position').first()).toHaveText('2 / 3');
    const imagenDos = page.getByRole('button', { name: /Ampliar imagen 2 de 3/ });
    await expect(imagenDos).toBeFocused();

    await imagenDos.click();
    await dialog.getByRole('button', { name: 'Cerrar imagen ampliada' }).click();
    await expect(dialog).not.toBeVisible();
    await expect(page.locator('.image-position').first()).toHaveText('2 / 3');
  });

  test('precio promocional y carrito usan una sola regla sin persistir precios manipulables', async ({ page }) => {
    await prepararEmpresa(page);
    const principal = productoReal(121, 'Producto Promocional', { slug: 'producto-promocional-121', precio: 1499, oferta: 1299, stock: 4, imagenes: [imagenData('Promo')] });
    await mockFuenteReal(page, principal);

    await page.goto('/varistorehn/producto/producto-promocional-121');
    await activarBaseDatos(page);
    const cantidad = page.getByRole('spinbutton', { name: 'Cantidad de producto' });
    await cantidad.fill('2');
    await cantidad.press('Tab');
    await page.getByRole('button', { name: 'Agregar al carrito' }).click();

    const header = page.locator('app-varistorehn-header');
    await expect(header.getByRole('button', { name: 'Abrir carrito con 2 unidades' })).toBeVisible();
    await expect(header.locator('.cart-copy small')).toContainText('2,598');

    const persistido = await page.evaluate(() => {
      const valor = localStorage.getItem('varistorehn:carrito:v2:901:bd');
      return valor ? JSON.parse(valor) : null;
    });
    expect(persistido).toHaveLength(1);
    expect(Object.keys(persistido[0]).sort()).toEqual(['modeloClave', 'productoId', 'unidades']);
    expect(persistido[0].unidades).toBe(2);

    await page.reload();
    await activarBaseDatos(page);
    await expect(page.locator('app-varistorehn-header').getByRole('button', { name: 'Abrir carrito con 2 unidades' })).toBeVisible();
    await expect(page.locator('app-varistorehn-header .cart-copy small')).toContainText('2,598');
  });

  test('stock 0 representa agotado, cantidad 0 y bloquea todas las compras del detalle', async ({ page }) => {
    await prepararEmpresa(page);
    const agotado = productoReal(126, 'Producto Agotado', { slug: 'producto-agotado-126', stock: 0, imagenes: [imagenData('Agotado')] });
    await mockFuenteReal(page, agotado);

    await page.goto('/varistorehn/producto/producto-agotado-126');
    await activarBaseDatos(page);
    await expect(page.getByRole('heading', { level: 1, name: 'Producto Agotado' })).toBeVisible();
    await expect(page.locator('.availability')).toContainText('Agotado');

    const cantidad = page.getByRole('spinbutton', { name: 'Cantidad de producto' });
    await expect(cantidad).toBeDisabled();
    await expect(cantidad).toHaveAttribute('max', '0');
    await expect(cantidad).toHaveValue('0');
    await expect(page.getByRole('button', { name: 'Producto agotado', exact: true })).toBeDisabled();
    await expect(page.getByRole('button', { name: 'Comprar por WhatsApp', exact: true })).toBeDisabled();
  });

  test('slug histórico se reemplaza por el slug canónico sin duplicar historial', async ({ page }) => {
    await prepararEmpresa(page);
    const principal = productoReal(128, 'Producto Canónico', { slug: 'producto-canonico-128', imagenes: [imagenData('Canon')] });
    await mockFuenteReal(page, principal);

    await page.goto('/varistorehn/producto/nombre-viejo-128');
    await activarBaseDatos(page);
    await expect(page.getByRole('heading', { level: 1, name: 'Producto Canónico' })).toBeVisible();
    await expect(page).toHaveURL(/\/varistorehn\/producto\/producto-canonico-128$/);

    await page.goBack();
    await expect(page).not.toHaveURL(/\/varistorehn\/producto\/nombre-viejo-128$/);
  });

  test('404 e inactivo se controlan sin convertirlos en productos demo', async ({ page }) => {
    await prepararEmpresa(page);
    const principal = productoReal(131, 'Producto inexistente', { slug: 'producto-inexistente-131' });
    let consultasDetalle = 0;
    await mockFuenteReal(page, principal, [], 404);
    page.on('request', request => {
      if (/\/tienda\/productos\/[^?]+$/.test(request.url())) consultasDetalle += 1;
    });

    await page.goto('/varistorehn/producto/producto-inexistente-131');
    await activarBaseDatos(page);
    await expect(page.getByRole('heading', { name: 'No encontramos este producto' })).toBeVisible();
    expect(consultasDetalle).toBeGreaterThan(0);
    await expect(page.getByText('Laptop Pro 14', { exact: true })).toHaveCount(0);
  });

  test('error real permanece visible y no cae silenciosamente a fixtures', async ({ page }) => {
    await prepararEmpresa(page);
    const principal = productoReal(141, 'Producto Error', { slug: 'producto-error-141' });
    await mockFuenteReal(page, principal, [], 503);

    await page.goto('/varistorehn/producto/producto-error-141');
    await activarBaseDatos(page);
    const alert = page.getByRole('alert');
    await expect(alert).toContainText('No pudimos cargar este producto');
    await expect(alert).toContainText('No se sustituyeron los datos reales por ejemplos');
    await expect(page.getByText('Laptop Pro 14', { exact: true })).toHaveCount(0);
  });

  test('móvil conserva swipe, CTA fijo, lightbox y no produce overflow desde 320px', async ({ page }) => {
    await prepararEmpresa(page);
    const imagenes = [imagenData('Móvil 1'), imagenData('Móvil 2'), imagenData('Móvil 3')];
    const principal = productoReal(151, 'Producto Móvil', { slug: 'producto-movil-151', imagenes, stock: 3 });
    await mockFuenteReal(page, principal);

    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/varistorehn/producto/producto-movil-151');
    await activarBaseDatos(page);
    const barra = page.locator('.mobile-buy-bar');
    await expect(barra).toBeVisible();
    await expect(barra).toContainText('Últimas unidades · 3 unidades');
    const cta = barra.getByRole('button', { name: 'Agregar' });
    expect(await cta.evaluate(element => Math.round(element.getBoundingClientRect().height))).toBeGreaterThanOrEqual(44);

    await expect(page.locator('.image-position').first()).toHaveText('1 / 3');
    await gestoSwipeIzquierda(page, '.gallery-stage');
    await expect(page.locator('.image-position').first()).toHaveText('2 / 3');

    for (const width of [1000, 760, 390, 320]) {
      await page.setViewportSize({ width, height: 844 });
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
      expect(overflow, `overflow horizontal a ${width}px`).toBeLessThanOrEqual(0);
    }

    await page.setViewportSize({ width: 320, height: 700 });
    const imagen = page.getByRole('button', { name: /Ampliar imagen 2 de 3/ });
    await imagen.click();
    const lightbox = page.locator('dialog.lightbox');
    await expect(lightbox).toBeVisible();
    await expect(lightbox.locator('.lightbox-controls span')).toHaveText('2 / 3');
    await gestoSwipeIzquierda(page, '.lightbox-stage');
    await expect(lightbox.locator('.lightbox-controls span')).toHaveText('3 / 3');
    await page.keyboard.press('Escape');
    await expect(lightbox).not.toBeVisible();
    await expect(page.locator('.image-position').first()).toHaveText('3 / 3');
  });

  test('WhatsApp en demo muestra mensaje estructurado sin enviar nada', async ({ page }) => {
    await prepararEmpresa(page);
    await page.goto('/varistorehn/producto/demo-producto-1');
    await esperarDetalleDemo(page);

    const cantidad = page.getByRole('spinbutton', { name: 'Cantidad de producto' });
    await cantidad.fill('2');
    await cantidad.press('Tab');
    await page.getByRole('button', { name: 'Ver mensaje de WhatsApp' }).click();

    const preview = page.getByRole('region', { name: 'Vista previa de WhatsApp' });
    await expect(preview).toContainText('VISTA PREVIA — NO ENVIADO');
    await expect(preview).toContainText('Laptop Pro 14');
    await expect(preview).toContainText('Cantidad: *2*');
    await expect(preview).toContainText('36,980');
  });
});
