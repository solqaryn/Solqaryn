import { test, expect, Page, Route } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
const okImage = 'https://fase2c5.invalid/producto-remoto.svg';

const productoVenta = {
  productoId: 9501,
  productoVarianteId: 9502,
  productoNombre: 'Producto remoto con imagen',
  marca: 'Marca Remota',
  modelo: 'Modelo Remoto',
  esVarianteTecnica: false,
  colorId: 9503,
  colorNombre: 'Azul',
  sku: 'SKU-REMOTO-IMAGEN',
  codigoBarras: '000000009502',
  cantidadDisponible: 8,
  precio: 180,
  imagenMiniaturaUrl: okImage
};

const productoCompra = {
  ...productoVenta,
  costo: 100
};

const detalle = {
  id: 11,
  productoId: productoVenta.productoId,
  productoVarianteId: productoVenta.productoVarianteId,
  productoNombre: productoVenta.productoNombre,
  productoMarca: productoVenta.marca,
  productoModelo: productoVenta.modelo,
  productoImagenPrincipalUrl: okImage,
  cantidad: 1,
  costoUnitario: 100,
  precioUnitario: 180,
  subtotal: 180
};

const compra = {
  id: 9601,
  numeroCompra: 'COM-2C5-001',
  fecha: '2026-08-07T12:00:00Z',
  proveedorId: 1,
  proveedorNombre: 'Proveedor 2C5',
  proveedorTelefono: '99999999',
  proveedorDocumento: 'RTN-2C5',
  documentoReferencia: 'REF-2C5',
  metodoPago: 'Efectivo',
  estadoPago: 'Pendiente',
  estado: 'Borrador',
  subtotal: 100,
  descuento: 0,
  impuesto: 0,
  total: 100,
  notas: '',
  detalles: [{ ...detalle, precioUnitario: undefined, subtotal: 100 }],
  impuestosAplicados: [],
  creadoPorNombreUsuario: ADMIN_USERNAME,
  fechaCreacion: '2026-08-07T12:00:00Z'
};

const venta = {
  id: 9701,
  numeroVenta: 'VEN-2C5-001',
  fecha: '2026-08-07T12:00:00Z',
  clienteId: 1,
  clienteNombre: 'Cliente 2C5',
  clienteTelefono: '88888888',
  clienteIdentidadORTN: 'ID-2C5',
  clienteCorreo: '',
  clienteDireccion: '',
  metodoPago: 'Efectivo',
  estadoPago: 'Pendiente',
  estado: 'Borrador',
  importeBruto: 180,
  importeProductos: 180,
  subtotal: 180,
  descuento: 0,
  impuesto: 0,
  total: 180,
  utilidadBruta: 80,
  costoEnvioId: null,
  costoEnvioNombre: null,
  costoEnvio: 0,
  envioExonerado: false,
  motivoExoneracionEnvio: null,
  numeroFactura: null,
  facturaId: null,
  notas: '',
  detalles: [detalle],
  descuentosAplicados: [],
  impuestosAplicados: [],
  creadoPorNombreUsuario: ADMIN_USERNAME,
  fechaCreacion: '2026-08-07T12:00:00Z'
};

async function login(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => url.pathname !== '/login', { timeout: 20_000 });
}

async function json(route: Route, data: unknown): Promise<void> {
  await route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, data })
  });
}

async function instalarFixtures(page: Page): Promise<void> {
  await page.route(okImage, (route) => route.fulfill({
    status: 200,
    contentType: 'image/svg+xml',
    body: '<svg xmlns="http://www.w3.org/2000/svg" width="320" height="320"><rect width="320" height="320" fill="#2563eb"/><circle cx="160" cy="135" r="72" fill="#fff"/></svg>'
  }));

  // Los mocks deben apuntar únicamente a la API. Patrones sin host también
  // interceptan las rutas Angular /compras y /ventas durante page.goto().
  await page.route(/^http:\/\/localhost:5005\/compras\/productos\/buscar(?:\?.*)?$/, (route) => json(route, [productoCompra]));
  await page.route(/^http:\/\/localhost:5005\/ventas\/productos\/buscar(?:\?.*)?$/, (route) => json(route, [productoVenta]));

  await page.route(/^http:\/\/localhost:5005\/compras\/calcular$/, (route) => json(route, {
    importeBruto: 100,
    subtotal: 100,
    subtotalNeto: 100,
    descuentosAplicados: [],
    totalDescuento: 0,
    impuestosAplicados: [],
    totalImpuesto: 0,
    impuestoIncluido: 0,
    impuestoAdicional: 0,
    total: 100
  }));
  await page.route(/^http:\/\/localhost:5005\/ventas\/calcular$/, (route) => json(route, {
    importeBruto: 180,
    importeProductos: 180,
    subtotal: 180,
    descuentosAplicados: [],
    totalDescuento: 0,
    impuestosAplicados: [],
    totalImpuesto: 0,
    impuestoIncluido: 0,
    impuestoAdicional: 0,
    costoEnvioId: null,
    costoEnvioNombre: null,
    costoEnvio: 0,
    envioExonerado: false,
    motivoExoneracionEnvio: null,
    total: 180
  }));

  await page.route(/^http:\/\/localhost:5005\/costos-envio(?:\?.*)?$/, (route) => json(route, []));

  await page.route(/^http:\/\/localhost:5005\/compras(?:\?.*)?$/, (route) => json(route, {
    items: [compra], totalCount: 1, page: 1, pageSize: 10, totalPages: 1
  }));
  await page.route(/^http:\/\/localhost:5005\/compras\/9601$/, (route) => json(route, compra));
  await page.route(/^http:\/\/localhost:5005\/compras\/9601\/documentos$/, (route) => json(route, []));

  await page.route(/^http:\/\/localhost:5005\/ventas(?:\?.*)?$/, (route) => json(route, {
    items: [venta], totalCount: 1, page: 1, pageSize: 10, totalPages: 1
  }));
  await page.route(/^http:\/\/localhost:5005\/ventas\/9701$/, (route) => json(route, venta));
}

test.describe('Fase 2C.5 — regresiones de variantes e imágenes', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
    await instalarFixtures(page);
  });

  test('venta y compra seleccionan la variante exacta mediante autocomplete remoto', async ({ page }) => {
    await page.goto('/ventas/nueva');
    const ventaInput = page.getByTestId('venta-producto-autocomplete');
    await ventaInput.fill(productoVenta.sku);
    await page.getByRole('option', { name: new RegExp(productoVenta.productoNombre) }).click();
    const varianteVenta = page.locator('mat-select[formcontrolname="productoVarianteId"]').first();
    await expect(varianteVenta).toBeVisible();
    await expect(varianteVenta).toContainText(productoVenta.sku);

    await page.goto('/compras/nueva');
    const compraInput = page.getByTestId('compra-producto-autocomplete');
    await compraInput.fill(productoCompra.sku);
    await page.getByRole('option', { name: new RegExp(productoCompra.productoNombre) }).click();
    const varianteCompra = page.locator('mat-select[formcontrolname="productoVarianteId"]').first();
    await expect(varianteCompra).toBeVisible();
    await expect(varianteCompra).toContainText(productoCompra.sku);
  });

  test('listas, detalles y formularios conservan imagen de producto con búsqueda remota', async ({ page }) => {
    await page.goto('/compras');
    await expect(page.locator('app-producto-imagen img').first()).toBeVisible();

    await page.goto('/compras/9601');
    await expect(page.locator('.producto-imagen-cell app-producto-imagen img')).toBeVisible();

    await page.goto('/compras/nueva');
    await page.getByTestId('compra-producto-autocomplete').fill(productoCompra.sku);
    await page.getByRole('option', { name: new RegExp(productoCompra.productoNombre) }).click();
    await expect(page.locator('.col-imagen app-producto-imagen img')).toBeVisible();

    await page.goto('/ventas');
    await expect(page.locator('app-producto-imagen img').first()).toBeVisible();

    await page.goto('/ventas/9701');
    await expect(page.locator('.producto-imagen-cell app-producto-imagen img')).toBeVisible();

    await page.goto('/ventas/nueva');
    await page.getByTestId('venta-producto-autocomplete').fill(productoVenta.sku);
    await page.getByRole('option', { name: new RegExp(productoVenta.productoNombre) }).click();
    await expect(page.locator('.col-imagen app-producto-imagen img')).toBeVisible();
  });
});
