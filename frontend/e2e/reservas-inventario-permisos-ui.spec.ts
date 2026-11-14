import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function login(page: Page, permisos: string[]): Promise<void> {
  await page.route('**/permisos/mis-permisos/empresa/*', async route => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: 'OK', data: { permisos, esAdministrador: false } }) });
  });
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

async function mockListado(page: Page): Promise<void> {
  await page.route('**/almacenes/activos', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: 'OK', data: [] }) }));
  await page.route('**/reservas-inventario**', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: 'OK', data: { items: [{ id: 701, numero: 'RES-701', estado: 'Borrador', ventaId: null, fechaExpiracion: null, fechaCreacion: '2026-08-17T00:00:00Z', detalles: [] }], page: 1, pageSize: 20, totalItems: 1, totalCount: 1, totalPages: 1 } }) }));
}

test.describe('Reservas de inventario - permisos UI', () => {
  test('oculta crear y editar cuando el usuario sólo tiene Ver', async ({ page }) => {
    await login(page, ['Dashboard:Ver', 'MovimientosInventario:Ver']);
    await mockListado(page);
    await page.goto('/inventario/reservas');

    await expect(page.getByRole('button', { name: 'Nueva reserva', exact: true })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Editar reserva', exact: true })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Ver reserva', exact: true })).toBeVisible();
  });

  test('muestra crear y editar cuando existen permisos específicos', async ({ page }) => {
    await login(page, ['Dashboard:Ver', 'MovimientosInventario:Ver', 'MovimientosInventario:Crear', 'MovimientosInventario:Editar']);
    await mockListado(page);
    await page.goto('/inventario/reservas');

    await expect(page.getByRole('button', { name: 'Nueva reserva', exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Editar reserva', exact: true })).toBeVisible();
  });
});
