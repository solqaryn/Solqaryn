import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function login(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

test('una activación rechazada por stock insuficiente permanece fail-closed', async ({ page }) => {
  await login(page);
  const reserva = { id: 811, numero: 'RES-E2E-811', ventaId: null, estado: 'Borrador', fechaCreacion: '2026-08-17T10:00:00Z', fechaExpiracion: null, fechaActivacion: null, fechaConsumo: null, fechaLiberacion: null, fechaExpiracionAplicada: null, fechaCancelacion: null, motivoLiberacion: null, motivoCancelacion: null, detalles: [{ id: 1, productoVarianteId: 40, almacenId: 2, ubicacionAlmacenId: null, cantidadReservada: 50, cantidadConsumida: 0, productoSku: 'SIN-STOCK' }] };

  await page.route('**/reservas-inventario/811', async route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', data: reserva }) }));
  await page.route('**/reservas-inventario/811/activar', async route => route.fulfill({ status: 409, contentType: 'application/problem+json', body: JSON.stringify({ title: 'Conflicto de inventario', status: 409, detail: 'Stock disponible insuficiente para activar la reserva.' }) }));

  await page.goto('/inventario/reservas/811');
  await expect(page.getByText('Borrador', { exact: true })).toBeVisible();
  page.once('dialog', dialog => dialog.accept());
  await page.getByRole('button', { name: 'Activar', exact: true }).click();

  await expect(page.getByText('No se pudo completar la operación.', { exact: true })).toBeVisible();
  await expect(page.getByText('Borrador', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Activar', exact: true })).toBeVisible();
});
