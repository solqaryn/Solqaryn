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

test('rechaza expiración pasada antes de enviar la reserva al backend', async ({ page }) => {
  await login(page);

  await page.route('**/existencias-variante**', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        message: 'OK',
        data: {
          items: [{
            id: 916,
            productoVarianteId: 16,
            productoNombre: 'Producto Expiración E2E',
            varianteSku: 'SKU-EXP-16',
            almacenId: 3,
            almacenCodigo: 'ALM-03',
            almacenNombre: 'Almacén E2E',
            ubicacionAlmacenId: 9,
            ubicacionCodigo: 'UBI-09',
            ubicacionNombre: 'Ubicación E2E',
            stockFisico: 8,
            stockReservado: 0,
            stockDisponible: 8,
            stockTransito: 0,
            stockMinimo: 0,
            stockMaximo: 100,
            tieneStockBajo: false,
            estaAgotada: false,
            fechaCreacion: '2026-08-17T00:00:00Z',
            fechaActualizacion: '2026-08-17T00:00:00Z'
          }],
          page: 1,
          pageSize: 200,
          totalItems: 1,
          totalPages: 1
        }
      })
    });
  });

  let posts = 0;
  await page.route('**/reservas-inventario', async route => {
    if (route.request().method() === 'POST') {
      posts += 1;
      await route.fulfill({ status: 500, contentType: 'application/json', body: '{}' });
      return;
    }
    await route.continue();
  });

  await page.goto('/inventario/reservas/nueva');
  const selector = page.locator('mat-select[formcontrolname="existenciaVarianteId"]').first();
  await selector.click();
  await page.getByRole('option', { name: /Producto Expiración E2E.*SKU-EXP-16.*disponible 8/ }).click();
  await expect(page.locator('.cdk-overlay-backdrop')).toHaveCount(0);
  await page.locator('input[formcontrolname="cantidad"]').first().fill('2');

  const expiracion = page.locator('input[formcontrolname="fechaExpiracion"]');
  await expect(expiracion).toHaveAttribute('min', /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/);
  await expiracion.fill('2020-01-01T00:00');
  await page.getByRole('button', { name: 'Guardar reserva', exact: true }).click();

  await expect(page.getByRole('alert')).toContainText('La fecha de expiración debe ser futura.');
  await expect(page).toHaveURL(/\/inventario\/reservas\/nueva$/);
  expect(posts).toBe(0);
});
