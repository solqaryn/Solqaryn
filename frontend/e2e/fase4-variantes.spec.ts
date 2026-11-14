import { test, expect, APIRequestContext, APIResponse, Page } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
const suffix = `${Date.now()}`;

let token = '';
let productoId = 0;
let varianteId = 0;
let marcaId = 0;
let modeloId = 0;
let colorId = 0;
let color2Id = 0;

const nombres = {
  marca: `Marca Variantes ${suffix}`,
  modelo: `Modelo Variantes ${suffix}`,
  color: `Negro Variantes ${suffix}`,
  color2: `Azul Variantes ${suffix}`,
  producto: `Producto Variantes ${suffix}`,
  productoUi: `Producto UI Variantes ${suffix}`,
  sku: `VAR-BLK-${suffix}`,
  sku2: `VAR-BLU-${suffix}`
};

function headers(): Record<string, string> {
  return { Authorization: `Bearer ${token}` };
}

async function dataOf(response: APIResponse): Promise<any> {
  const payload = await response.json();
  return payload.data ?? payload.Data;
}

async function loginApi(request: APIRequestContext): Promise<string> {
  const response = await request.post(`${API_URL}/auth/login`, {
    data: { nombreUsuario: ADMIN_USERNAME, password: ADMIN_PASSWORD }
  });
  expect(response.status(), await response.text()).toBe(200);
  const data = await dataOf(response);
  expect(data?.token).toBeTruthy();
  return data.token;
}

async function loginUi(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => url.pathname !== '/login', { timeout: 20_000 });
}

async function crearCatalogo(
  request: APIRequestContext,
  ruta: string,
  data: Record<string, unknown>
): Promise<Record<string, any>> {
  const response = await request.post(`${API_URL}/${ruta}`, { headers: headers(), data });
  expect(response.status(), `${ruta}: ${await response.text()}`).toBe(201);
  return await dataOf(response);
}

test.describe('Fase 4 — variantes multidimensionales, SKU e inventario', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test.beforeAll(async ({ request }) => {
    token = await loginApi(request);
  });

  test('El acceso no muestra controles internos de seguridad', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
    await expect(page.getByText('Acceso protegido')).toHaveCount(0);
    await expect(page.getByText('Permisos por rol')).toHaveCount(0);
    await expect(page.getByText('Operaciones auditadas')).toHaveCount(0);
    await expect(page.getByText(/credenciales se transmiten/i)).toHaveCount(0);
  });

  test('API crea múltiples colores, conserva la variante y bloquea anulación de compra con movimientos posteriores', async ({ request }) => {
    const marca = await crearCatalogo(request, 'marcas', {
      nombre: nombres.marca,
      descripcion: 'Marca temporal para variantes',
      orden: 20
    });
    marcaId = marca.id;

    const modelo = await crearCatalogo(request, 'modelos', {
      nombre: nombres.modelo,
      descripcion: 'Modelo temporal para variantes',
      catalogoPadreId: marca.id,
      orden: 20
    });
    modeloId = modelo.id;

    const color = await crearCatalogo(request, 'colores', {
      nombre: nombres.color,
      descripcion: 'Primer color temporal para variantes',
      codigoVisual: '#111827',
      orden: 20
    });
    colorId = color.id;

    const color2 = await crearCatalogo(request, 'colores', {
      nombre: nombres.color2,
      descripcion: 'Segundo color temporal para variantes',
      codigoVisual: '#2563EB',
      orden: 21
    });
    color2Id = color2.id;

    const productoResponse = await request.post(`${API_URL}/productos`, {
      headers: headers(),
      multipart: {
        Nombre: nombres.producto,
        Marca: nombres.marca,
        Modelo: nombres.modelo,
        MarcaId: String(marca.id),
        ModeloId: String(modelo.id),
        Cantidad: '10',
        Costo: '104',
        Precio: '220',
        UmbralStockBajo: '4',
        'Variantes[0].ColorId': String(color.id),
        'Variantes[0].Sku': nombres.sku.toLowerCase(),
        'Variantes[0].CodigoBarras': `7501${suffix.slice(-8)}`,
        'Variantes[0].Cantidad': '6',
        'Variantes[0].UmbralStockBajo': '2',
        'Variantes[0].Costo': '100',
        'Variantes[0].Precio': '220',
        'Variantes[0].Activo': 'true',
        'Variantes[1].ColorId': String(color2.id),
        'Variantes[1].Sku': nombres.sku2.toLowerCase(),
        'Variantes[1].CodigoBarras': `7502${suffix.slice(-8)}`,
        'Variantes[1].Cantidad': '4',
        'Variantes[1].UmbralStockBajo': '2',
        'Variantes[1].Costo': '110',
        'Variantes[1].Precio': '230',
        'Variantes[1].Activo': 'true'
      }
    });
    expect(productoResponse.status(), await productoResponse.text()).toBe(201);
    const producto = await dataOf(productoResponse);
    productoId = producto.id;
    expect(producto.cantidad).toBe(10);
    expect(producto.totalVariantes).toBe(2);
    expect(producto.variantes).toHaveLength(2);
    expect(producto.variantes.map((v: any) => v.cantidad).reduce((a: number, b: number) => a + b, 0)).toBe(10);

    const variante = producto.variantes.find((v: any) => v.colorId === color.id);
    expect(variante).toBeTruthy();
    varianteId = variante.id;
    expect(variante.sku).toBe(nombres.sku.toUpperCase());
    expect(variante.cantidad).toBe(6);

    const skuDuplicado = await request.post(`${API_URL}/productos/${productoId}/variantes`, {
      headers: headers(),
      data: {
        colorId: color.id,
        sku: nombres.sku,
        cantidad: 0,
        umbralStockBajo: 2,
        costo: 100,
        precio: 220
      }
    });
    expect(skuDuplicado.status()).toBe(400);

    const compraResponse = await request.post(`${API_URL}/compras`, {
      headers: headers(),
      data: {
        proveedorNombre: `Proveedor Variantes ${suffix}`,
        metodoPago: 'Efectivo',
        estadoPago: 'Pendiente',
        descuento: 0,
        impuesto: 0,
        detalles: [{ productoId, productoVarianteId: varianteId, cantidad: 4, costoUnitario: 90 }]
      }
    });
    expect(compraResponse.status(), await compraResponse.text()).toBe(201);
    const compra = await dataOf(compraResponse);
    expect(compra.detalles[0].productoSku).toBe(nombres.sku.toUpperCase());
    expect(compra.detalles[0].productoColor).toBe(nombres.color);

    const confirmarCompra = await request.post(`${API_URL}/compras/${compra.id}/confirmar`, {
      headers: headers()
    });
    expect(confirmarCompra.status(), await confirmarCompra.text()).toBe(200);

    let productoActual = await dataOf(await request.get(`${API_URL}/productos/${productoId}`, { headers: headers() }));
    expect(productoActual.cantidad).toBe(14);
    expect(productoActual.variantes.find((v: any) => v.id === varianteId)?.cantidad).toBe(10);

    const envioResponse = await request.get(`${API_URL}/costos-envio/predeterminado`, { headers: headers() });
    expect(envioResponse.status(), await envioResponse.text()).toBe(200);
    const envio = await dataOf(envioResponse);

    const ventaResponse = await request.post(`${API_URL}/ventas`, {
      headers: headers(),
      data: {
        clienteNombre: `Cliente Variantes ${suffix}`,
        metodoPago: 'Efectivo',
        estadoPago: 'Pendiente',
        descuento: 0,
        impuesto: 0,
        costoEnvioId: envio.id,
        envioExonerado: false,
        detalles: [{ productoId, productoVarianteId: varianteId, cantidad: 3, precioUnitario: 220 }]
      }
    });
    expect(ventaResponse.status(), await ventaResponse.text()).toBe(201);
    const venta = await dataOf(ventaResponse);
    expect(venta.detalles[0].productoSku).toBe(nombres.sku.toUpperCase());

    const confirmarVenta = await request.post(`${API_URL}/ventas/${venta.id}/confirmar`, {
      headers: headers()
    });
    expect(confirmarVenta.status(), await confirmarVenta.text()).toBe(200);
    const ventaConfirmada = await dataOf(confirmarVenta);
    expect(ventaConfirmada.facturaId).toBeTruthy();

    productoActual = await dataOf(await request.get(`${API_URL}/productos/${productoId}`, { headers: headers() }));
    expect(productoActual.cantidad).toBe(11);
    expect(productoActual.variantes.find((v: any) => v.id === varianteId)?.cantidad).toBe(7);

    const facturaResponse = await request.get(`${API_URL}/facturas/${ventaConfirmada.facturaId}`, {
      headers: headers()
    });
    expect(facturaResponse.status(), await facturaResponse.text()).toBe(200);
    const factura = await dataOf(facturaResponse);
    expect(factura.detalles[0].productoVarianteId).toBe(varianteId);
    expect(factura.detalles[0].varianteSku).toBe(nombres.sku.toUpperCase());
    expect(factura.detalles[0].varianteColor).toBe(nombres.color);

    const anularVenta = await request.post(`${API_URL}/ventas/${venta.id}/anular`, {
      headers: headers(),
      data: { motivoAnulacion: 'Reversión E2E de variante' }
    });
    expect(anularVenta.status(), await anularVenta.text()).toBe(200);

    productoActual = await dataOf(await request.get(`${API_URL}/productos/${productoId}`, { headers: headers() }));
    expect(productoActual.cantidad).toBe(14);
    expect(productoActual.variantes.find((v: any) => v.id === varianteId)?.cantidad).toBe(10);

    const anularCompra = await request.post(`${API_URL}/compras/${compra.id}/anular`, {
      headers: headers(),
      data: { motivoAnulacion: 'Reversión E2E de compra por variante' }
    });
    const anularCompraTexto = await anularCompra.text();
    expect(anularCompra.status(), anularCompraTexto).toBe(400);
    expect(anularCompraTexto.toLowerCase()).toContain('movimientos posteriores');

    productoActual = await dataOf(await request.get(`${API_URL}/productos/${productoId}`, { headers: headers() }));
    expect(productoActual.cantidad).toBe(14);
    expect(productoActual.variantes.find((v: any) => v.id === varianteId)?.cantidad).toBe(10);
  });

  test('Formulario guarda dos colores con SKU automático y stock consolidado', async ({ page }) => {
    await loginUi(page);
    await page.goto('/productos/nuevo');

    await page.locator('input[formcontrolname="nombre"]').fill(nombres.productoUi);

    const datosFamilia = page.locator('.data-section');
    const marcaPredeterminada = datosFamilia.getByRole('combobox', { name: 'Marca predeterminada (opcional)' });
    await marcaPredeterminada.focus();
    await marcaPredeterminada.press('Enter');
    await page.getByRole('option', { name: nombres.marca, exact: true }).click();

    const modeloPredeterminado = datosFamilia.getByRole('combobox', { name: 'Modelo predeterminado (opcional)' });
    await modeloPredeterminado.focus();
    await modeloPredeterminado.press('Enter');
    await page.getByRole('option', { name: nombres.modelo, exact: true }).click();

    const variantes = page.locator('.variant-card');
    const primeraVariante = variantes.nth(0);
    const colorPrimera = primeraVariante.getByRole('combobox', { name: 'Color (opcional)' });
    await colorPrimera.focus();
    await colorPrimera.press('Enter');
    await page.getByRole('option', { name: nombres.color, exact: true }).click();

    await primeraVariante.locator('input[formcontrolname="cantidad"]').fill('2');
    await primeraVariante.locator('input[formcontrolname="costo"]').fill('100');
    await primeraVariante.locator('input[formcontrolname="precio"]').fill('300');
    await primeraVariante.locator('input[formcontrolname="umbralStockBajo"]').fill('0');

    await page.getByRole('button', { name: 'Agregar variante' }).first().click();
    await expect(variantes).toHaveCount(2);

    const segundaVariante = variantes.nth(1);
    const colorSegunda = segundaVariante.getByRole('combobox', { name: 'Color (opcional)' });
    await colorSegunda.focus();
    await colorSegunda.press('Enter');
    await page.getByRole('option', { name: nombres.color2, exact: true }).click();
    await segundaVariante.locator('input[formcontrolname="cantidad"]').fill('3');
    await segundaVariante.locator('input[formcontrolname="costo"]').fill('100');
    await segundaVariante.locator('input[formcontrolname="precio"]').fill('300');
    await segundaVariante.locator('input[formcontrolname="umbralStockBajo"]').fill('0');

    await expect(page.locator('.stock-summary').getByText('5 unidades', { exact: true })).toBeVisible();

    const guardarResponse = page.waitForResponse((response) =>
      response.url().endsWith('/productos') && response.request().method() === 'POST'
    );
    await page.getByRole('button', { name: 'Guardar producto' }).click();
    const respuesta = await guardarResponse;
    expect(respuesta.status(), await respuesta.text()).toBe(201);

    const creado = await dataOf(respuesta);
    expect(creado.cantidad).toBe(5);
    expect(creado.variantes).toHaveLength(2);
    expect(creado.variantes.map((v: any) => v.cantidad).sort()).toEqual([2, 3]);
    expect(creado.variantes.every((v: any) => typeof v.sku === 'string' && v.sku.length > 0)).toBeTruthy();
    expect(new Set(creado.variantes.map((v: any) => v.sku)).size).toBe(2);
    expect(creado.variantes.map((v: any) => v.colorId).sort()).toEqual([colorId, color2Id].sort());

    await page.waitForURL((url) => url.pathname === '/productos', { timeout: 20_000 });
  });

  test('Formulario principal separa metadatos de la administración de variantes', async ({ page }) => {
    await loginUi(page);

    await page.goto(`/productos/${productoId}/editar`);
    await expect(page.getByRole('heading', { name: 'Variantes y existencias' })).toBeVisible();
    await expect(page.getByText('Stock total calculado')).toBeVisible();
    await expect(page.locator('.variant-card')).toHaveCount(2);
    await expect(page.locator('.stock-summary').getByText('14 unidades', { exact: true })).toBeVisible();

    await expect(page.getByRole('button', { name: 'Agregar variante' })).toHaveCount(0);
    const cantidades = page.locator('input[formcontrolname="cantidad"]');
    await expect(cantidades).toHaveCount(2);
    await expect(cantidades.nth(0)).toBeDisabled();
    await expect(cantidades.nth(1)).toBeDisabled();

    const administrarVariantes = page.getByRole('link', { name: 'Administrar variantes' });
    await expect(administrarVariantes).toBeVisible();
    await expect(administrarVariantes).toHaveAttribute('href', `/productos/${productoId}/variantes`);
  });

  test('Angular exige la variante exacta en compra y venta', async ({ page }) => {
    await loginUi(page);

    await page.goto(`/productos/${productoId}/variantes`);
    await expect(page.getByRole('heading', { name: 'Variantes y existencias' })).toBeVisible();
    const fila = page.locator('table tr', { hasText: nombres.sku.toUpperCase() });
    await expect(fila).toBeVisible();
    await expect(fila.getByText(nombres.color)).toBeVisible();
    await expect(fila.getByText('10', { exact: true })).toBeVisible();

    await page.goto('/ventas/nueva');
    const ventaInput = page.getByTestId('venta-producto-autocomplete');
    await ventaInput.fill(nombres.sku);
    await page.getByRole('option', { name: new RegExp(nombres.producto) }).click();
    const varianteVenta = page.locator('mat-select[formcontrolname="productoVarianteId"]').first();
    await expect(varianteVenta).toBeVisible();
    await expect(varianteVenta).toContainText(nombres.sku.toUpperCase());

    await page.goto('/compras/nueva');
    const compraInput = page.getByTestId('compra-producto-autocomplete');
    await compraInput.fill(nombres.sku);
    await page.getByRole('option', { name: new RegExp(nombres.producto) }).click();
    const varianteCompra = page.locator('mat-select[formcontrolname="productoVarianteId"]').first();
    await expect(varianteCompra).toBeVisible();
    await expect(varianteCompra).toContainText(nombres.sku.toUpperCase());
  });
});