import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

const borrador = {
  id: 801,
  numero: 'RES-E2E-801',
  ventaId: 701,
  estado: 'Borrador',
  fechaCreacion: '2026-08-17T10:00:00Z',
  fechaExpiracion: '2026-08-18T10:00:00Z',
  fechaActivacion: null,
  fechaConsumo: null,
  fechaLiberacion: null,
  fechaExpiracionAplicada: null,
  fechaCancelacion: null,
  motivoLiberacion: null,
  motivoCancelacion: null,
  detalles: [{ id: 1, productoVarianteId: 11, almacenId: 2, ubicacionAlmacenId: 5, cantidadReservada: 3, cantidadConsumida: 0, productoSku: 'SKU-E2E', productoMarca: 'Marca', productoModelo: 'Modelo', productoColor: 'Negro', productoTalla: 'M' }]
};

async function login(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

test.describe('Reservas de inventario - UX y lifecycle', () => {
  test('lista, abre detalle y activa una reserva sin perder su contexto físico', async ({ page }) => {
    await login(page);

    await page.route('**/reservas-inventario?page=1&pageSize=20', async route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', data: { items: [borrador], totalCount: 1, page: 1, pageSize: 20 } }) }));
    await page.route('**/reservas-inventario/801', async route => {
      if (route.request().method() === 'GET') {
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', data: borrador }) });
        return;
      }
      await route.continue();
    });
    await page.route('**/reservas-inventario/801/activar', async route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: 'Reserva activada correctamente.', data: { ...borrador, estado: 'Activa', fechaActivacion: '2026-08-17T10:15:00Z' } }) }));

    await page.goto('/inventario/reservas');
    await expect(page.getByRole('heading', { name: 'Reservas de inventario' })).toBeVisible();
    await expect(page.getByText('RES-E2E-801', { exact: true })).toBeVisible();
    await expect(page.getByText('3 unidades reservadas', { exact: true })).toBeVisible();

    await page.getByRole('button', { name: 'Ver reserva' }).click();
    await expect(page).toHaveURL(/\/inventario\/reservas\/801$/);
    await expect(page.getByText('SKU-E2E', { exact: true })).toBeVisible();
    await expect(page.getByText('#2', { exact: true })).toBeVisible();
    await expect(page.getByText('#5', { exact: true })).toBeVisible();

    page.once('dialog', dialog => dialog.accept());
    await page.getByRole('button', { name: 'Activar', exact: true }).click();
    await expect(page.getByText('Activa', { exact: true })).toBeVisible();
    await expect(page.getByText('Reserva activada.', { exact: true })).toBeVisible();
  });

  test('impide expirar desde la UI una reserva activa antes de su vencimiento', async ({ page }) => {
    await login(page);
    const activaFutura = { ...borrador, estado: 'Activa', fechaActivacion: '2026-08-17T10:15:00Z', fechaExpiracion: '2099-08-18T10:00:00Z' };
    await page.route('**/reservas-inventario/801', async route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', data: activaFutura }) }));

    await page.goto('/inventario/reservas/801');
    const expirar = page.getByRole('button', { name: 'Expirar', exact: true });
    await expect(expirar).toBeVisible();
    await expect(expirar).toBeDisabled();
    await expect(expirar).toHaveAttribute('title', 'Disponible cuando alcance su fecha de expiración');
  });
});
