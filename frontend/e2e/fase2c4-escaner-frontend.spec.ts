import { test, expect, APIRequestContext, APIResponse, Page } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
const suffix = `${Date.now()}`;

let token = '';
let codigoBarras = '';

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

async function escanear(page: Page, codigo: string): Promise<void> {
  const input = page.getByLabel(/Escanear SKU o código de barras/i);
  await input.fill(codigo);
  await input.press('Enter');
}

test.describe('Fase 2C.4 — frontend del escáner', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test.beforeAll(async ({ request }) => {
    token = await loginApi(request);

    const marca = await crearCatalogo(request, 'marcas', {
      nombre: `Marca Escáner ${suffix}`,
      descripcion: 'Marca temporal de certificación 2C.4',
      orden: 90
    });
    const modelo = await crearCatalogo(request, 'modelos', {
      nombre: `Modelo Escáner ${suffix}`,
      descripcion: 'Modelo temporal de certificación 2C.4',
      catalogoPadreId: marca.id,
      orden: 90
    });
    const color = await crearCatalogo(request, 'colores', {
      nombre: `Negro Escáner ${suffix}`,
      descripcion: 'Color temporal de certificación 2C.4',
      codigoVisual: '#111827',
      orden: 90
    });

    codigoBarras = `00075${suffix.slice(-8)}`;
    const productoResponse = await request.post(`${API_URL}/productos`, {
      headers: headers(),
      multipart: {
        Nombre: `Producto Escáner ${suffix}`,
        Marca: `Marca Escáner ${suffix}`,
        Modelo: `Modelo Escáner ${suffix}`,
        MarcaId: String(marca.id),
        ModeloId: String(modelo.id),
        Cantidad: '2',
        Costo: '100',
        Precio: '220',
        UmbralStockBajo: '1',
        'Variantes[0].ColorId': String(color.id),
        'Variantes[0].Sku': `scanner-${suffix}`,
        'Variantes[0].CodigoBarras': codigoBarras,
        'Variantes[0].Cantidad': '2',
        'Variantes[0].UmbralStockBajo': '1',
        'Variantes[0].Costo': '100',
        'Variantes[0].Precio': '220',
        'Variantes[0].Activo': 'true'
      }
    });
    expect(productoResponse.status(), await productoResponse.text()).toBe(201);
  });

  test('venta consolida lecturas repetidas, conserva ceros y bloquea superar stock', async ({ page }) => {
    await loginUi(page);
    await page.goto('/ventas/nueva');
    await page.getByRole('button', { name: 'Activar lector físico' }).click();

    await escanear(page, codigoBarras);
    await expect(page.getByText(/agregado a la venta/i)).toBeVisible();
    const cantidad = page.locator('.detalle-row input[formcontrolname="cantidad"]').first();
    await expect(cantidad).toHaveValue('1');

    await escanear(page, codigoBarras);
    await expect(page.getByText(/cantidad consolidada en 2/i)).toBeVisible();
    await expect(cantidad).toHaveValue('2');

    await escanear(page, codigoBarras);
    await expect(page.getByText(/Stock insuficiente/i)).toBeVisible();
    await expect(cantidad).toHaveValue('2');
    await expect(page.getByLabel(/Escanear SKU o código de barras/i)).toBeFocused();
  });

  test('compra consolida lecturas y conserva costo retornado por backend', async ({ page }) => {
    await loginUi(page);
    await page.goto('/compras/nueva');
    await page.getByRole('button', { name: 'Activar lector físico' }).click();

    await escanear(page, codigoBarras);
    await expect(page.getByText(/agregado a la compra/i)).toBeVisible();
    const fila = page.locator('.detalle-row').first();
    await expect(fila.locator('input[formcontrolname="cantidad"]')).toHaveValue('1');
    await expect(fila.locator('input[formcontrolname="costoUnitario"]')).toHaveValue('100');

    await escanear(page, codigoBarras);
    await expect(page.getByText(/cantidad consolidada en 2/i)).toBeVisible();
    await expect(fila.locator('input[formcontrolname="cantidad"]')).toHaveValue('2');
    await expect(page.getByLabel(/Escanear SKU o código de barras/i)).toBeFocused();
  });

  test('cámara e imagen quedan cableadas al formulario sin activar cámara en CI', async ({ page }) => {
    await loginUi(page);
    await page.goto('/ventas/nueva');
    await page.getByRole('button', { name: 'Cámara o imagen' }).click();

    await expect(page.getByRole('heading', { name: 'Escanear código' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Activar cámara' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Leer imagen' })).toBeVisible();
    await page.getByRole('button', { name: 'Cerrar', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Escanear código' })).toBeHidden();
  });
});
