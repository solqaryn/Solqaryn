import { APIRequestContext, APIResponse, expect, Locator, Page, test } from '@playwright/test';

const API_URL = process.env['PHASE7_API_URL'] ?? 'http://127.0.0.1:5005';
const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
const suffix = `${Date.now()}`;
const nombreProducto = `Producto Ajuste E2E ${suffix}`;
const skuVariante = `AJ-E2E-${suffix}`;
const motivoAjuste = `Conteo E2E N0.7 ${suffix}`;
const motivoCreacionUi = `Creación UI E2E N0.7 ${suffix}`;
const nombreAlmacen = `Bodega Ajuste E2E ${suffix}`;

let token = '';
let ajusteId = 0;
let almacenId = 0;

function headers(): Record<string, string> {
  return { Authorization: `Bearer ${token}` };
}

async function dataOf(response: APIResponse): Promise<any> {
  const payload = await response.json();
  return payload.data ?? payload.Data;
}

async function esperarCerrarOverlay(page: Page): Promise<void> {
  await expect(page.locator('.cdk-overlay-backdrop')).toHaveCount(0);
}

async function abrirSelectConTeclado(page: Page, select: Locator): Promise<void> {
  await select.focus();
  await page.keyboard.press('Enter');
  await expect(select).toHaveAttribute('aria-expanded', 'true');
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

async function crearProductoFixture(request: APIRequestContext): Promise<{ productoId: number; varianteId: number }> {
  const marca = await crearCatalogo(request, 'marcas', {
    nombre: `Marca Ajuste E2E ${suffix}`,
    descripcion: 'Fixture determinista N0.7.E',
    orden: 92
  });
  const modelo = await crearCatalogo(request, 'modelos', {
    nombre: `Modelo Ajuste E2E ${suffix}`,
    descripcion: 'Fixture determinista N0.7.E',
    catalogoPadreId: marca.id,
    orden: 92
  });
  const color = await crearCatalogo(request, 'colores', {
    nombre: `Color Ajuste E2E ${suffix}`,
    descripcion: 'Fixture determinista N0.7.E',
    codigoVisual: '#355C7D',
    orden: 92
  });

  const response = await request.post(`${API_URL}/productos`, {
    headers: headers(),
    multipart: {
      Nombre: nombreProducto,
      Marca: `Marca Ajuste E2E ${suffix}`,
      Modelo: `Modelo Ajuste E2E ${suffix}`,
      MarcaId: String(marca.id),
      ModeloId: String(modelo.id),
      Cantidad: '3',
      Costo: '100',
      Precio: '180',
      UmbralStockBajo: '1',
      'Variantes[0].ColorId': String(color.id),
      'Variantes[0].Sku': skuVariante,
      'Variantes[0].CodigoBarras': `91${suffix.slice(-10)}`,
      'Variantes[0].Cantidad': '3',
      'Variantes[0].UmbralStockBajo': '1',
      'Variantes[0].Costo': '100',
      'Variantes[0].Precio': '180',
      'Variantes[0].Activo': 'true'
    }
  });
  expect(response.status(), await response.text()).toBe(201);
  const producto = await dataOf(response);
  expect(producto.id).toBeGreaterThan(0);
  expect(producto.variantes?.length).toBeGreaterThan(0);
  expect(producto.variantes[0].id).toBeGreaterThan(0);
  return { productoId: producto.id, varianteId: producto.variantes[0].id };
}

async function crearContextoFisicoFixture(request: APIRequestContext, varianteId: number): Promise<number> {
  const sucursal = await crearCatalogo(request, 'sucursales', {
    codigo: `SUC-AJ-${suffix.slice(-8)}`,
    nombre: `Sucursal Ajuste E2E ${suffix}`,
    direccion: 'Fixture M13',
    zonaHoraria: 'America/Tegucigalpa'
  });

  const almacen = await crearCatalogo(request, 'almacenes', {
    sucursalId: sucursal.id,
    codigo: `ALM-AJ-${suffix.slice(-8)}`,
    nombre: nombreAlmacen,
    tipo: 'Bodega'
  });

  const response = await request.post(`${API_URL}/existencias-variante`, {
    headers: headers(),
    data: {
      productoVarianteId: varianteId,
      almacenId: almacen.id,
      ubicacionAlmacenId: null,
      stockFisico: 3,
      stockReservado: 0,
      stockTransito: 0,
      stockMinimo: 0,
      stockMaximo: null
    }
  });
  expect(response.status(), await response.text()).toBe(201);
  const existencia = await dataOf(response);
  expect(existencia.id).toBeGreaterThan(0);
  expect(existencia.almacenId).toBe(almacen.id);
  return almacen.id;
}

async function crearAjusteFixture(
  request: APIRequestContext,
  productoId: number,
  varianteId: number,
  almacen: number
): Promise<number> {
  const response = await request.post(`${API_URL}/inventario/ajustes`, {
    headers: headers(),
    data: {
      motivo: motivoAjuste,
      observaciones: 'Fixture Playwright determinista para lifecycle Borrador→Confirmado→Anulado.',
      detalles: [{
        productoId,
        productoVarianteId: varianteId,
        almacenId: almacen,
        ubicacionAlmacenId: null,
        cantidadObjetivo: 5
      }]
    }
  });
  expect(response.status(), await response.text()).toBe(201);
  const ajuste = await dataOf(response);
  expect(ajuste.id).toBeGreaterThan(0);
  expect(ajuste.estado).toBe('Borrador');
  expect(ajuste.detalles[0].almacenId).toBe(almacen);
  return ajuste.id;
}

async function buscarFilaFixture(page: Page): Promise<ReturnType<Page['locator']>> {
  await page.goto('/inventario/ajustes');
  const search = page.getByPlaceholder('Número o motivo');
  await search.fill(motivoAjuste);
  await page.getByRole('button', { name: /^Aplicar$/i }).click();
  const row = page.locator('tbody tr').filter({ hasText: motivoAjuste });
  await expect(row).toHaveCount(1);
  return row;
}

test.describe('N0.7.E - Ajustes de inventario', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test.beforeAll(async ({ request }) => {
    token = await loginApi(request);
    const producto = await crearProductoFixture(request);
    almacenId = await crearContextoFisicoFixture(request, producto.varianteId);
    ajusteId = await crearAjusteFixture(request, producto.productoId, producto.varianteId, almacenId);
  });

  test.beforeEach(async ({ page }) => {
    await loginUi(page);
  });

  test('expone listado, filtro y navegación de un borrador determinista', async ({ page }) => {
    const row = await buscarFilaFixture(page);
    await expect(page.getByRole('heading', { name: 'Ajustes de inventario' })).toBeVisible();
    await expect(row).toContainText('Borrador');
    await expect(row.getByRole('button', { name: /^Editar$/i })).toBeVisible();
    await expect(row.getByRole('button', { name: /^Confirmar$/i })).toBeVisible();

    await row.getByRole('button', { name: /^Ver$/i }).click();
    await expect(page).toHaveURL(new RegExp(`/inventario/ajustes/${ajusteId}$`));
    await expect(page.getByText(motivoAjuste)).toBeVisible();
  });

  test('crea un borrador por UI usando variante y existencia física deterministas', async ({ page }) => {
    await page.goto('/inventario/ajustes/nuevo');

    await expect(page.getByRole('heading', { name: 'Nuevo ajuste' })).toBeVisible();
    const details = page.locator('article.detail');
    await expect(details).toHaveCount(1);

    await page.locator('input[formcontrolname="motivo"]').fill(motivoCreacionUi);
    const productoSelect = details.locator('mat-select[formcontrolname="productoId"]');
    await abrirSelectConTeclado(page, productoSelect);
    await page.getByRole('option', { name: new RegExp(nombreProducto) }).click();
    await esperarCerrarOverlay(page);

    const varianteSelect = details.locator('mat-select[formcontrolname="productoVarianteId"]');
    await expect(varianteSelect).not.toHaveAttribute('aria-disabled', 'true');
    await abrirSelectConTeclado(page, varianteSelect);
    await page.getByRole('option', { name: new RegExp(skuVariante) }).click();
    await esperarCerrarOverlay(page);

    const existenciaSelect = details.locator('mat-select[formcontrolname="existenciaId"]');
    await expect(existenciaSelect).not.toHaveAttribute('aria-disabled', 'true');
    await abrirSelectConTeclado(page, existenciaSelect);
    await page.getByRole('option', { name: new RegExp(nombreAlmacen) }).click();
    await esperarCerrarOverlay(page);

    await details.locator('input[formcontrolname="cantidadObjetivo"]').fill('4');

    const createResponse = page.waitForResponse((response) =>
      response.url().endsWith('/inventario/ajustes')
      && response.request().method() === 'POST'
    );
    await page.getByRole('button', { name: /Guardar borrador/i }).click();
    const response = await createResponse;
    expect(response.status(), await response.text()).toBe(201);
    await expect(page).toHaveURL(/\/inventario\/ajustes$/);

    const search = page.getByPlaceholder('Número o motivo');
    await search.fill(motivoCreacionUi);
    await page.getByRole('button', { name: /^Aplicar$/i }).click();
    const createdRow = page.locator('tbody tr').filter({ hasText: motivoCreacionUi });
    await expect(createdRow).toHaveCount(1);
    await expect(createdRow).toContainText('Borrador');
  });

  test('mantiene detalles dinámicos sin perder el selector de existencia física', async ({ page }) => {
    await page.goto('/inventario/ajustes/nuevo');
    const details = page.locator('article.detail');
    await expect(details).toHaveCount(1);

    await page.getByRole('button', { name: /Agregar detalle/i }).click();
    await expect(details).toHaveCount(2);
    await expect(details.nth(1).locator('mat-select[formcontrolname="productoId"]')).toBeVisible();
    await expect(details.nth(1).locator('mat-select[formcontrolname="productoVarianteId"]')).toBeVisible();
    await expect(details.nth(1).locator('mat-select[formcontrolname="existenciaId"]')).toBeVisible();

    await page.getByRole('button', { name: 'Eliminar detalle' }).last().click();
    await expect(details).toHaveCount(1);
  });

  test('edición recupera la existencia física del borrador y abre la ruta correcta', async ({ page }) => {
    const row = await buscarFilaFixture(page);
    await row.getByRole('button', { name: /^Editar$/i }).click();
    await expect(page).toHaveURL(new RegExp(`/inventario/ajustes/${ajusteId}/editar$`));
    await expect(page.getByRole('heading', { name: 'Editar borrador' })).toBeVisible();
    await expect(page.locator('input[formcontrolname="motivo"]')).toHaveValue(motivoAjuste);
    await expect(page.locator('mat-select[formcontrolname="existenciaId"]')).toContainText(nombreAlmacen);
  });

  test('confirma el fixture mediante diálogo explícito y materializa el estado', async ({ page }) => {
    const row = await buscarFilaFixture(page);
    page.once('dialog', async (dialog) => {
      expect(dialog.type()).toBe('confirm');
      expect(dialog.message()).toMatch(/Confirmar el ajuste/i);
      await dialog.accept();
    });

    const responsePromise = page.waitForResponse((response) =>
      response.url().endsWith(`/inventario/ajustes/${ajusteId}/confirmar`)
      && response.request().method() === 'POST'
    );
    await row.getByRole('button', { name: /^Confirmar$/i }).click();
    const response = await responsePromise;
    expect(response.status(), await response.text()).toBe(200);

    const confirmedRow = page.locator('tbody tr').filter({ hasText: motivoAjuste });
    await expect(confirmedRow).toContainText('Confirmado');
    await expect(confirmedRow.getByRole('button', { name: /^Anular$/i })).toBeVisible();
  });

  test('anula el mismo fixture con motivo obligatorio y deja solo lectura', async ({ page }) => {
    const row = await buscarFilaFixture(page);
    await expect(row).toContainText('Confirmado');

    page.once('dialog', async (dialog) => {
      expect(dialog.type()).toBe('prompt');
      expect(dialog.message()).toMatch(/Motivo obligatorio para anular/i);
      await dialog.accept('Cierre determinista E2E N0.7.E');
    });

    const responsePromise = page.waitForResponse((response) =>
      response.url().endsWith(`/inventario/ajustes/${ajusteId}/anular`)
      && response.request().method() === 'POST'
    );
    await row.getByRole('button', { name: /^Anular$/i }).click();
    const response = await responsePromise;
    expect(response.status(), await response.text()).toBe(200);

    const annulledRow = page.locator('tbody tr').filter({ hasText: motivoAjuste });
    await expect(annulledRow).toContainText('Anulado');
    await expect(annulledRow).toContainText('Solo lectura');
    await expect(annulledRow.getByRole('button', { name: /^Editar$/i })).toHaveCount(0);
    await expect(annulledRow.getByRole('button', { name: /^Confirmar$/i })).toHaveCount(0);
  });
});
