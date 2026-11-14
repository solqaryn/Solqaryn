import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function loginConPermisos(page: Page, permisos: string[]): Promise<void> {
  await page.route('**/permisos/mis-permisos/empresa/*', async route => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: 'Permisos cargados', data: { permisos, esAdministrador: false } }) });
  });
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

async function mockCatalogos(page: Page): Promise<void> {
  await page.route('**/proveedores/activos', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', errors: [], data: [{ id: 11, nombre: 'Proveedor Uno' }] }) }));
  await page.route('**/solicitudes-compra**', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', errors: [], data: { items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 } }) }));
  await page.route('**/productos?**', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', errors: [], data: { items: [{ id: 21, nombre: 'Producto Uno' }], page: 1, pageSize: 100, totalCount: 1, totalPages: 1 } }) }));
  await page.route('**/productos/21/variantes**', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', errors: [], data: [] }) }));
}

async function seleccionar(page: Page, control: string, opcion: string): Promise<void> {
  const select = page.locator(`mat-select[formcontrolname="${control}"]`).first();
  await select.focus();
  await select.press('Enter');
  await expect(select).toHaveAttribute('aria-expanded', 'true');
  await page.getByRole('option', { name: opcion, exact: true }).click();
  await expect(select).toHaveAttribute('aria-expanded', 'false');
  await expect(page.locator('.cdk-overlay-backdrop')).toHaveCount(0);
}

test.describe('OrdenCompra editor - rutas, RBAC e idempotencia', () => {
  test('redirige creación al login cuando no existe sesión', async ({ page }) => {
    await page.goto('/ordenes-compra/nueva');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
  });

  test('deniega creación a usuario autenticado sin Compras:Crear', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver', 'Compras:Ver']);
    await page.goto('/ordenes-compra/nueva');
    await expect(page).toHaveURL(/\/dashboard(?:\?|$)/);
  });

  test('recalcula totales y mantiene Idempotency-Key estable al reintentar una creación rechazada', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver', 'Compras:Ver', 'Compras:Crear']);
    await mockCatalogos(page);

    const keys: string[] = [];
    let intento = 0;
    await page.route('**/ordenes-compra', async route => {
      if (route.request().method() !== 'POST') return route.continue();
      keys.push(route.request().headers()['idempotency-key'] ?? '');
      intento++;
      if (intento === 1) return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: false, message: 'Conflicto causal simulado', errors: [], data: null }) });
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', errors: [], data: { id: 77 } }) });
    });

    await page.goto('/ordenes-compra/nueva');
    await expect(page.getByRole('heading', { name: 'Nueva orden de compra' })).toBeVisible();
    await seleccionar(page, 'proveedorId', 'Proveedor Uno');
    await seleccionar(page, 'productoId', 'Producto Uno');
    await page.locator('input[formcontrolname="cantidadOrdenada"]').fill('2');
    await page.locator('input[formcontrolname="precioUnitario"]').fill('50');
    await expect(page.locator('aside.totals .grand strong')).toHaveText('100.00');

    await page.getByRole('button', { name: 'Crear orden' }).click();
    await expect(page.getByRole('alert')).toContainText('Conflicto causal simulado');
    await page.getByRole('button', { name: 'Crear orden' }).click();

    await expect(page).toHaveURL(/\/ordenes-compra\?selected=77$/);
    expect(keys).toHaveLength(2);
    expect(keys[0]).toBeTruthy();
    expect(keys[1]).toBe(keys[0]);
  });

  test('carga la ruta de edición sólo con Compras:Editar', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver', 'Compras:Ver', 'Compras:Editar']);
    await mockCatalogos(page);
    await page.route('**/ordenes-compra/77', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, message: '', errors: [], data: {
        id: 77, numeroOrden: 'OC-000077', solicitudCompraId: null, proveedorId: 11, proveedorNombre: 'Proveedor Uno', estado: 'Borrador', moneda: 'HNL', condicionesCompra: null, fechaEsperadaUtc: null, observaciones: null, subtotal: 100, descuentoTotal: 0, impuestoTotal: 0, total: 100,
        detalles: [{ id: 1, productoId: 21, productoVarianteId: null, cantidadOrdenada: 1, precioUnitario: 100, descuento: 0, impuesto: 0, total: 100, observacion: null }]
      } })
    }));

    await page.goto('/ordenes-compra/77/editar');
    await expect(page.getByRole('heading', { name: 'Editar orden de compra' })).toBeVisible();
    await expect(page.locator('mat-select[formcontrolname="proveedorId"]')).toContainText('Proveedor Uno');
    await expect(page.locator('input[formcontrolname="moneda"]')).toHaveValue('HNL');
    await expect(page.locator('aside.totals .grand strong')).toHaveText('100.00');
  });
});
