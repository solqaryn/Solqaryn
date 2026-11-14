import { test, expect, Page, Route } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
const evidenceDirectory = 'test-results/fase5';

const okImage = 'https://fase5.invalid/producto-ok.svg';
const brokenImage = 'https://fase5.invalid/producto-roto.webp';

const product = {
  id: 501,
  nombre: 'Producto visual Fase 5',
  marca: 'Marca Visual',
  modelo: 'Modelo Visual',
  marcaId: 1,
  modeloId: 2,
  marcaNombre: 'Marca Visual',
  modeloNombre: 'Modelo Visual',
  colorId: null,
  colorNombre: 'Azul',
  colorCodigoVisual: '#2563EB',
  tallaId: null,
  tallaNombre: 'Mediana',
  categoriaId: null,
  categoriaNombre: 'Demostración',
  descripcion: 'Producto utilizado para certificar el tratamiento de imágenes.',
  cantidad: 8,
  costo: 100,
  precio: 180,
  precioMinimo: 180,
  precioMaximo: 180,
  umbralStockBajo: 2,
  tieneStockBajo: false,
  estaAgotado: false,
  estadoInventario: 'Disponible',
  activo: true,
  imagenPrincipalUrl: okImage,
  totalImagenes: 2,
  totalVariantes: 0,
  usaVariantes: false,
  variantes: [],
  fechaCreacion: '2026-07-27T12:00:00Z',
  fechaActualizacion: '2026-07-27T12:00:00Z',
  imagenes: [
    { id: 1, url: okImage, publicId: 'fase5/ok', esPrincipal: true, orden: 0 },
    { id: 2, url: brokenImage, publicId: 'fase5/roto', esPrincipal: false, orden: 1 }
  ]
};

const productWithoutImage = {
  ...product,
  id: 502,
  nombre: 'Producto visual sin imagen',
  imagenPrincipalUrl: null,
  totalImagenes: 0,
  imagenes: []
};

const productOperationSale = {
  productoId: product.id,
  productoVarianteId: 503,
  productoNombre: product.nombre,
  marca: product.marca,
  modelo: product.modelo,
  esVarianteTecnica: true,
  colorId: null,
  colorNombre: 'Predeterminada',
  sku: 'SKU-F5-VISUAL',
  codigoBarras: '000000000503',
  cantidadDisponible: product.cantidad,
  precio: product.precio,
  imagenMiniaturaUrl: okImage
};

const productOperationPurchase = {
  ...productOperationSale,
  costo: product.costo
};

const detail = {
  id: 11,
  productoId: product.id,
  productoNombre: product.nombre,
  productoMarca: product.marca,
  productoModelo: product.modelo,
  productoImagenPrincipalUrl: okImage,
  cantidad: 1,
  costoUnitario: 100,
  precioUnitario: 180,
  subtotal: 180
};

const purchase = {
  id: 601,
  numeroCompra: 'COM-F5-001',
  fecha: '2026-07-27T12:00:00Z',
  proveedorId: 1,
  proveedorNombre: 'Proveedor Visual',
  proveedorTelefono: '99999999',
  proveedorDocumento: 'RTN-F5',
  documentoReferencia: 'REF-F5',
  metodoPago: 'Efectivo',
  estadoPago: 'Pendiente',
  estado: 'Borrador',
  subtotal: 100,
  descuento: 0,
  impuesto: 0,
  total: 100,
  notas: '',
  detalles: [{ ...detail, precioUnitario: undefined, subtotal: 100 }],
  impuestosAplicados: [],
  creadoPorNombreUsuario: ADMIN_USERNAME,
  fechaCreacion: '2026-07-27T12:00:00Z'
};

const sale = {
  id: 701,
  numeroVenta: 'VEN-F5-001',
  fecha: '2026-07-27T12:00:00Z',
  clienteId: 1,
  clienteNombre: 'Cliente Visual',
  clienteTelefono: '88888888',
  clienteIdentidadORTN: 'ID-F5',
  clienteCorreo: '',
  clienteDireccion: '',
  metodoPago: 'Efectivo',
  estadoPago: 'Pendiente',
  estado: 'Borrador',
  subtotal: 180,
  descuento: 0,
  impuesto: 0,
  total: 180,
  utilidadBruta: 80,
  numeroFactura: null,
  facturaId: null,
  notas: '',
  detalles: [detail],
  descuentosAplicados: [],
  impuestosAplicados: [],
  creadoPorNombreUsuario: ADMIN_USERNAME,
  fechaCreacion: '2026-07-27T12:00:00Z'
};

async function login(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => url.pathname !== '/login', { timeout: 20_000 });
}

async function json(route: Route, data: unknown): Promise<void> {
  await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, data }) });
}

async function installFixtures(page: Page): Promise<void> {
  await page.route(okImage, (route) => route.fulfill({
    status: 200,
    contentType: 'image/svg+xml',
    body: '<svg xmlns="http://www.w3.org/2000/svg" width="320" height="320"><rect width="320" height="320" fill="#2563eb"/><circle cx="160" cy="135" r="72" fill="#fff"/><text x="160" y="255" text-anchor="middle" fill="#fff" font-size="32">Fase 5</text></svg>'
  }));
  await page.route(brokenImage, (route) => route.abort('failed'));

  await page.route(/^https?:\/\/(?:localhost|127\.0\.0\.1):5005\/(?:api\/)?productos(?:\?.*)?$/, (route) => json(route, {
    items: [product, productWithoutImage],
    totalCount: 2,
    page: 1,
    pageSize: 200,
    totalPages: 1
  }));
  await page.route(/^https?:\/\/(?:localhost|127\.0\.0\.1):5005\/(?:api\/)?productos\/501$/, (route) => json(route, product));

  await page.route(/^https?:\/\/(?:localhost|127\.0\.0\.1):5005\/(?:api\/)?compras\/productos\/buscar(?:\?.*)?$/, (route) => json(route, [productOperationPurchase]));
  await page.route(/^https?:\/\/(?:localhost|127\.0\.0\.1):5005\/(?:api\/)?ventas\/productos\/buscar(?:\?.*)?$/, (route) => json(route, [productOperationSale]));

  await page.route(/^https?:\/\/(?:localhost|127\.0\.0\.1):5005\/(?:api\/)?compras(?:\?.*)?$/, (route) => {
    if (route.request().method() === 'POST') return json(route, purchase);
    return json(route, { items: [purchase], totalCount: 1, page: 1, pageSize: 10, totalPages: 1 });
  });
  await page.route(/^https?:\/\/(?:localhost|127\.0\.0\.1):5005\/(?:api\/)?compras\/calcular$/, (route) => json(route, {
    importeBruto: 100,
    subtotal: 100,
    subtotalNeto: 100,
    impuestosAplicados: [],
    impuestoIncluido: 0,
    impuestoAdicional: 0,
    total: 100
  }));
  await page.route(/^https?:\/\/(?:localhost|127\.0\.0\.1):5005\/(?:api\/)?compras\/601$/, (route) => json(route, purchase));
  await page.route(/^https?:\/\/(?:localhost|127\.0\.0\.1):5005\/(?:api\/)?compras\/601\/documentos$/, (route) => json(route, []));

  await page.route(/^https?:\/\/(?:localhost|127\.0\.0\.1):5005\/(?:api\/)?ventas(?:\?.*)?$/, (route) => {
    if (route.request().method() === 'POST') return json(route, sale);
    return json(route, { items: [sale], totalCount: 1, page: 1, pageSize: 10, totalPages: 1 });
  });
  await page.route(/^https?:\/\/(?:localhost|127\.0\.0\.1):5005\/(?:api\/)?ventas\/calcular$/, (route) => json(route, {
    importeBruto: 180,
    subtotal: 180,
    subtotalNeto: 180,
    descuentosAplicados: [],
    totalDescuento: 0,
    impuestosAplicados: [],
    totalImpuesto: 0,
    total: 180
  }));
  await page.route(/^https?:\/\/(?:localhost|127\.0\.0\.1):5005\/(?:api\/)?ventas\/701$/, (route) => json(route, sale));

  await page.route(/^https?:\/\/(?:localhost|127\.0\.0\.1):5005\/(?:api\/)?inventario\/movimientos\/paged(?:\?.*)?$/, (route) => json(route, {
    items: [{
      id: 801,
      productoId: product.id,
      productoVarianteId: productOperationPurchase.productoVarianteId,
      almacenId: null,
      ubicacionAlmacenId: null,
      productoNombre: product.nombre,
      productoSku: productOperationPurchase.sku,
      productoImagenPrincipalUrl: okImage,
      tipo: 'Entrada',
      causa: 'Compra',
      cantidad: 1,
      stockAnterior: 7,
      stockNuevo: 8,
      costoUnitario: product.costo,
      precioUnitario: product.precio,
      correlationId: 'compra:601:confirmar',
      origenTipo: 'Compra',
      origenId: 601,
      compraId: 601,
      referenciaTipo: 'Compra',
      referenciaId: 601,
      fecha: '2026-07-27T12:00:00Z',
      creadoPorNombreUsuario: ADMIN_USERNAME
    }],
    totalCount: 1,
    page: 1,
    pageSize: 25,
    totalPages: 1
  }));
}

async function expectNoOverflow(page: Page): Promise<void> {
  const overflow = await page.evaluate(() => Math.max(document.documentElement.scrollWidth, document.body.scrollWidth) - document.documentElement.clientWidth);
  expect(overflow).toBeLessThanOrEqual(2);
}

async function capture(page: Page, name: string): Promise<void> {
  await page.screenshot({ path: `${evidenceDirectory}/${name}.png`, fullPage: true });
}

test.describe('Fase 5 - tratamiento integral de imágenes', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await installFixtures(page);
  });

  test('Productos usa carga diferida, fallback accesible e imagen principal', async ({ page }) => {
    await page.goto('/productos');
    const valid = page.getByRole('img', { name: `Imagen principal de ${product.nombre}` }).first();
    await expect(valid).toBeVisible();
    await expect(valid).toHaveAttribute('loading', 'lazy');
    await expect(page.getByRole('img', { name: `${productWithoutImage.nombre} no tiene imagen disponible` }).first()).toBeVisible();
    await expectNoOverflow(page);
    await capture(page, 'productos-listado-fallback');
  });

  test('Detalle prioriza la imagen principal y la galería es operable con teclado', async ({ page }) => {
    await page.goto('/productos/501');
    const hero = page.locator('app-producto-imagen[data-variant="hero"] img');
    await expect(hero).toBeVisible();
    await expect(hero).toHaveAttribute('loading', 'eager');
    await expect(hero).toHaveAttribute('fetchpriority', 'high');
    await capture(page, 'producto-detalle-galeria');

    const secondTrigger = page.getByRole('button', { name: /Ampliar imagen 2/ });
    await secondTrigger.focus();
    await page.keyboard.press('Enter');
    await expect(page.getByRole('dialog', { name: /Imagen ampliada/ })).toBeVisible();
    await expect(page.getByRole('img', { name: /No se pudo cargar la imagen ampliada/ })).toBeVisible();
    await capture(page, 'producto-lightbox-fallback');
    await page.keyboard.press('Escape');
    await expect(page.getByRole('dialog')).toBeHidden();
    await expectNoOverflow(page);
  });

  test('Compras y Ventas muestran imágenes o fallback en listas, formularios y detalles', async ({ page }) => {
    await page.goto('/compras');
    await expect(page.locator('app-producto-imagen img').first()).toBeVisible();
    await capture(page, 'compras-listado');

    await page.goto('/compras/601');
    await expect(page.locator('.producto-imagen-cell app-producto-imagen img')).toBeVisible();
    await capture(page, 'compra-detalle');

    await page.goto('/compras/nueva');
    await page.getByTestId('compra-producto-autocomplete').fill(productOperationPurchase.sku);
    await page.getByRole('option', { name: /Producto visual Fase 5/ }).click();
    await expect(page.locator('.col-imagen app-producto-imagen img')).toBeVisible();
    await capture(page, 'compra-formulario');

    await page.goto('/ventas');
    await expect(page.locator('app-producto-imagen img').first()).toBeVisible();
    await capture(page, 'ventas-listado');

    await page.goto('/ventas/701');
    await expect(page.locator('.producto-imagen-cell app-producto-imagen img')).toBeVisible();
    await capture(page, 'venta-detalle');

    await page.goto('/ventas/nueva');
    await page.getByTestId('venta-producto-autocomplete').fill(productOperationSale.sku);
    await page.getByRole('option', { name: /Producto visual Fase 5/ }).click();
    await expect(page.locator('.col-imagen app-producto-imagen img')).toBeVisible();
    await capture(page, 'venta-formulario');
    await expectNoOverflow(page);
  });

  test('El historial de inventario conserva la miniatura principal', async ({ page }) => {
    await page.goto('/inventario/movimientos');
    const image = page.locator('.producto-imagen-cell app-producto-imagen img');
    await expect(image).toBeVisible();
    await expect(image).toHaveAttribute('alt', /Imagen de Producto visual Fase 5/);
    await expectNoOverflow(page);
    await capture(page, 'movimientos-inventario');
  });
});