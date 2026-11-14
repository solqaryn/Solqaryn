import { test, expect, APIRequestContext, APIResponse, Page } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
const suffix = `${Date.now()}`;

let token = '';
let nombreConStock = '';
let skuConStock = '';
let nombreSinStock = '';
let skuSinStock = '';

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
  return (await dataOf(response)).token;
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

async function crearProducto(
  request: APIRequestContext,
  marcaId: number,
  modeloId: number,
  colorId: number,
  nombre: string,
  sku: string,
  codigoBarras: string,
  cantidad: number,
  costo: number,
  precio: number
): Promise<void> {
  const response = await request.post(`${API_URL}/productos`, {
    headers: headers(),
    multipart: {
      Nombre: nombre,
      Marca: `Marca Remota ${suffix}`,
      Modelo: `Modelo Remoto ${suffix}`,
      MarcaId: String(marcaId),
      ModeloId: String(modeloId),
      Cantidad: String(cantidad),
      Costo: String(costo),
      Precio: String(precio),
      UmbralStockBajo: '1',
      'Variantes[0].ColorId': String(colorId),
      'Variantes[0].Sku': sku,
      'Variantes[0].CodigoBarras': codigoBarras,
      'Variantes[0].Cantidad': String(cantidad),
      'Variantes[0].UmbralStockBajo': '1',
      'Variantes[0].Costo': String(costo),
      'Variantes[0].Precio': String(precio),
      'Variantes[0].Activo': 'true'
    }
  });
  expect(response.status(), await response.text()).toBe(201);
}

test.describe('Fase 2C.5 — autocomplete remoto de productos', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test.beforeAll(async ({ request }) => {
    token = await loginApi(request);

    const marca = await crearCatalogo(request, 'marcas', {
      nombre: `Marca Remota ${suffix}`,
      descripcion: 'Marca temporal de certificación 2C.5',
      orden: 91
    });
    const modelo = await crearCatalogo(request, 'modelos', {
      nombre: `Modelo Remoto ${suffix}`,
      descripcion: 'Modelo temporal de certificación 2C.5',
      catalogoPadreId: marca.id,
      orden: 91
    });
    const color = await crearCatalogo(request, 'colores', {
      nombre: `Azul Remoto ${suffix}`,
      descripcion: 'Color temporal de certificación 2C.5',
      codigoVisual: '#2563EB',
      orden: 91
    });

    nombreConStock = `Producto Remoto Venta ${suffix}`;
    skuConStock = `REMOTO-VENTA-${suffix}`;
    await crearProducto(
      request,
      marca.id,
      modelo.id,
      color.id,
      nombreConStock,
      skuConStock,
      `00081${suffix.slice(-8)}`,
      3,
      110,
      230
    );

    nombreSinStock = `Producto Remoto Compra ${suffix}`;
    skuSinStock = `REMOTO-COMPRA-${suffix}`;
    await crearProducto(
      request,
      marca.id,
      modelo.id,
      color.id,
      nombreSinStock,
      skuSinStock,
      `00082${suffix.slice(-8)}`,
      0,
      45,
      90
    );
  });

  test('venta consulta bajo demanda, no carga catálogo masivo y no expone costo', async ({ page }) => {
    const catalogoRequests: string[] = [];
    const busquedaRequests: string[] = [];
    page.on('request', (request) => {
      const url = new URL(request.url());
      if (url.pathname === '/productos') catalogoRequests.push(request.url());
      if (url.pathname === '/ventas/productos/buscar') busquedaRequests.push(request.url());
    });

    await loginUi(page);
    catalogoRequests.length = 0;
    busquedaRequests.length = 0;
    await page.goto('/ventas/nueva');
    await page.waitForTimeout(500);
    expect(catalogoRequests).toHaveLength(0);

    const input = page.getByTestId('venta-producto-autocomplete');
    await input.fill('x');
    await page.waitForTimeout(450);
    expect(busquedaRequests).toHaveLength(0);

    const respuestaPromise = page.waitForResponse((response) =>
      response.url().includes('/ventas/productos/buscar') && response.request().method() === 'GET'
    );
    await input.fill(nombreConStock);
    const respuesta = await respuestaPromise;
    expect(respuesta.status(), await respuesta.text()).toBe(200);

    const payload = await respuesta.json();
    const resultados = payload.data ?? payload.Data;
    expect(Array.isArray(resultados)).toBeTruthy();
    expect(resultados.length).toBeGreaterThan(0);
    expect(resultados.length).toBeLessThanOrEqual(30);
    expect(resultados[0]).not.toHaveProperty('costo');
    expect(resultados[0].sku).toBe(skuConStock);

    await page.getByRole('option', { name: new RegExp(nombreConStock) }).click();
    const fila = page.locator('.detalle-row').first();
    await expect(fila).toContainText(skuConStock);
    await expect(fila.locator('input[formcontrolname="cantidad"]')).toHaveValue('1');
    await expect(fila.locator('input[formcontrolname="precioUnitario"]')).toHaveValue('230');
  });

  test('venta excluye variantes agotadas del autocomplete remoto', async ({ page }) => {
    await loginUi(page);
    await page.goto('/ventas/nueva');

    const input = page.getByTestId('venta-producto-autocomplete');
    const respuestaPromise = page.waitForResponse((response) =>
      response.url().includes('/ventas/productos/buscar') && response.request().method() === 'GET'
    );
    await input.fill(skuSinStock);
    const respuesta = await respuestaPromise;
    const payload = await respuesta.json();
    const resultados = payload.data ?? payload.Data;
    expect(resultados).toEqual([]);
  });

  test('compra admite stock cero, recibe costo y consolida selecciones repetidas', async ({ page }) => {
    await loginUi(page);
    await page.goto('/compras/nueva');

    const input = page.getByTestId('compra-producto-autocomplete');
    const primeraRespuestaPromise = page.waitForResponse((response) =>
      response.url().includes('/compras/productos/buscar') && response.request().method() === 'GET'
    );
    await input.fill(skuSinStock);
    const primeraRespuesta = await primeraRespuestaPromise;
    expect(primeraRespuesta.status(), await primeraRespuesta.text()).toBe(200);

    const payload = await primeraRespuesta.json();
    const resultados = payload.data ?? payload.Data;
    expect(resultados.length).toBeGreaterThan(0);
    expect(resultados[0].sku).toBe(skuSinStock);
    expect(resultados[0].cantidadDisponible).toBe(0);
    expect(resultados[0].costo).toBe(45);

    await page.getByRole('option', { name: new RegExp(nombreSinStock) }).click();
    const fila = page.locator('.detalle-row').first();
    await expect(fila.locator('input[formcontrolname="cantidad"]')).toHaveValue('1');
    await expect(fila.locator('input[formcontrolname="costoUnitario"]')).toHaveValue('45');

    const segundaRespuestaPromise = page.waitForResponse((response) =>
      response.url().includes('/compras/productos/buscar') && response.request().method() === 'GET'
    );
    await input.fill(skuSinStock);
    await segundaRespuestaPromise;
    await page.getByRole('option', { name: new RegExp(nombreSinStock) }).click();

    await expect(fila.locator('input[formcontrolname="cantidad"]')).toHaveValue('2');
    await expect(page.getByText(/cantidad consolidada en 2/i)).toBeVisible();
  });
});
