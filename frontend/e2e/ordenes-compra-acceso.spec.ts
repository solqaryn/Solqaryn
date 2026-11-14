import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function loginConPermisos(page: Page, permisos: string[]): Promise<void> {
  await page.route('**/permisos/mis-permisos/empresa/*', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        message: 'Permisos cargados',
        data: { permisos, esAdministrador: false }
      })
    });
  });

  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

async function mockOrdenesCompraLectura(page: Page): Promise<void> {
  await page.route('**/proveedores/activos', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, message: '', errors: [], data: [] })
  }));

  await page.route('**/ordenes-compra?**', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({
      success: true,
      message: '',
      errors: [],
      data: {
        items: [{
          id: 41,
          numeroOrden: 'OC-000041',
          estado: 2,
          solicitudCompraId: 17,
          proveedorId: 9,
          proveedorNombre: 'Proveedor QA',
          moneda: 'HNL',
          condicionesCompra: 'Crédito 30 días',
          fechaEsperadaUtc: '2026-08-30T00:00:00Z',
          observaciones: null,
          subtotal: 1000,
          descuento: 0,
          impuesto: 150,
          total: 1150,
          fechaEnvioAprobacionUtc: '2026-08-18T20:00:00Z',
          fechaAprobacionUtc: null,
          fechaCancelacionUtc: null,
          detalles: []
        }],
        page: 1,
        pageSize: 10,
        totalCount: 1,
        totalPages: 1
      }
    })
  }));
}

test.describe('Órdenes de compra - acceso y listado N2.2.E.1', () => {
  test('redirige al login cuando no existe sesión', async ({ page }) => {
    await page.goto('/ordenes-compra');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
  });

  test('deniega la ruta a un usuario autenticado sin Compras:Ver', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver']);

    await page.goto('/ordenes-compra');

    await expect(page).toHaveURL(/\/dashboard(?:\?|$)/);
    await expect(page).not.toHaveURL(/\/ordenes-compra(?:\?|$)/);
  });

  test('permite Compras:Ver, muestra navegación y normaliza estado numérico del API', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver', 'Compras:Ver']);
    await mockOrdenesCompraLectura(page);

    await page.goto('/ordenes-compra');

    await expect(page).toHaveURL(/\/ordenes-compra(?:\?|$)/);
    await expect(page.getByRole('heading', { name: 'Órdenes de compra' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Órdenes de compra' })).toBeVisible();
    await expect(page.getByText('OC-000041', { exact: true })).toBeVisible();
    await expect(page.getByText('Proveedor QA', { exact: true })).toBeVisible();
    await expect(page.getByText('Pendiente de aprobación', { exact: true })).toBeVisible();
    await expect(page.getByText('Creación disponible en el editor')).toHaveCount(0);
  });
});
