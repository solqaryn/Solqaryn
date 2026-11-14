import { test, expect, APIRequestContext, APIResponse, Page } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
const suffix = `${Date.now()}`;

function authHeaders(token: string): Record<string, string> {
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

async function createCatalog(
  request: APIRequestContext,
  token: string,
  route: string,
  input: Record<string, unknown>
): Promise<Record<string, any>> {
  const response = await request.post(`${API_URL}/${route}`, {
    headers: authHeaders(token),
    data: input
  });
  expect(response.status(), await response.text()).toBe(201);
  return await dataOf(response);
}

async function waitForSelectToClose(page: Page): Promise<void> {
  await expect(page.locator('.cdk-overlay-backdrop')).toHaveCount(0, { timeout: 10_000 });
}

test('Productos filtra por relaciones normalizadas y estado de inventario', async ({ request, page }) => {
  const token = await loginApi(request);
  const brand = await createCatalog(request, token, 'marcas', { nombre: `Filtro Marca ${suffix}`, orden: 1 });
  const model = await createCatalog(request, token, 'modelos', {
    nombre: `Filtro Modelo ${suffix}`,
    catalogoPadreId: brand.id,
    orden: 1
  });
  const color = await createCatalog(request, token, 'colores', {
    nombre: `Filtro Color ${suffix}`,
    codigoVisual: '#0F766E',
    orden: 1
  });
  const size = await createCatalog(request, token, 'tallas', { nombre: `Filtro Talla ${suffix}`, orden: 1 });

  const productName = `Producto agotado filtrable ${suffix}`;
  const productResponse = await request.post(`${API_URL}/productos`, {
    headers: authHeaders(token),
    multipart: {
      Nombre: productName,
      MarcaId: String(brand.id),
      ModeloId: String(model.id),
      ColorId: String(color.id),
      TallaId: String(size.id),
      Cantidad: '0',
      Costo: '80',
      Precio: '140',
      UmbralStockBajo: '2',
      'Variantes[0].MarcaId': String(brand.id),
      'Variantes[0].ModeloId': String(model.id),
      'Variantes[0].ColorId': String(color.id),
      'Variantes[0].TallaId': String(size.id),
      'Variantes[0].Cantidad': '0',
      'Variantes[0].Costo': '80',
      'Variantes[0].Precio': '140',
      'Variantes[0].UmbralStockBajo': '2',
      'Variantes[0].Activo': 'true'
    }
  });
  expect(productResponse.status(), await productResponse.text()).toBe(201);
  const product = await dataOf(productResponse);

  const filteredResponse = await request.get(
    `${API_URL}/productos?page=1&pageSize=20&marcaId=${brand.id}&modeloId=${model.id}&colorId=${color.id}&tallaId=${size.id}&agotado=true`,
    { headers: authHeaders(token) }
  );
  expect(filteredResponse.status(), await filteredResponse.text()).toBe(200);
  const filtered = await dataOf(filteredResponse);
  expect(filtered.totalCount).toBe(1);
  expect(filtered.items[0].id).toBe(product.id);

  const availableResponse = await request.get(
    `${API_URL}/productos?page=1&pageSize=20&marcaId=${brand.id}&agotado=false`,
    { headers: authHeaders(token) }
  );
  expect(availableResponse.status()).toBe(200);
  expect((await dataOf(availableResponse)).items.some((item: any) => item.id === product.id)).toBe(false);

  await loginUi(page);
  await page.goto('/productos');

  const brandSelect = page.getByRole('combobox', { name: 'Marca' });
  await expect(brandSelect).toBeEnabled();
  await brandSelect.click();
  await page.getByRole('option', { name: brand.nombre, exact: true }).click();
  await expect(brandSelect).toContainText(brand.nombre);
  await waitForSelectToClose(page);

  const modelSelect = page.getByRole('combobox', { name: 'Modelo' });
  await expect(modelSelect).toBeEnabled();
  await modelSelect.click();
  await page.getByRole('option', { name: model.nombre, exact: true }).click();
  await expect(modelSelect).toContainText(model.nombre);
  await waitForSelectToClose(page);

  const colorSelect = page.getByRole('combobox', { name: 'Color' });
  await expect(colorSelect).toBeEnabled();
  await colorSelect.click();
  await page.getByRole('option', { name: color.nombre, exact: true }).click();
  await expect(colorSelect).toContainText(color.nombre);
  await waitForSelectToClose(page);

  const sizeSelect = page.getByRole('combobox', { name: 'Talla o tamaño' });
  await expect(sizeSelect).toBeEnabled();
  await sizeSelect.focus();
  await sizeSelect.press('Enter');
  await page.getByRole('option', { name: size.nombre, exact: true }).click();
  await expect(sizeSelect).toContainText(size.nombre);
  await waitForSelectToClose(page);

  const statusSelect = page.getByRole('combobox', { name: 'Estado' });
  await expect(statusSelect).toBeEnabled();
  await statusSelect.click();
  await page.getByRole('option', { name: 'Agotados', exact: true }).click();
  await expect(statusSelect).toContainText('Agotados');
  await waitForSelectToClose(page);

  const row = page.locator('table.table-desktop tbody tr', { hasText: productName });
  await expect(row).toBeVisible();
  await expect(row.getByText('Agotado', { exact: true })).toBeVisible();

  await page.getByRole('button', { name: 'Limpiar filtros' }).click();
  await expect(brandSelect).toHaveClass(/mat-mdc-select-empty/);
  await expect(modelSelect).toBeDisabled();
});
