import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function login(page: Page): Promise<void> {
  await page.route('**/permisos/mis-permisos/empresa/*', async route => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: 'OK', data: { permisos: ['Dashboard:Ver', 'MovimientosInventario:Ver', 'MovimientosInventario:Crear'], esAdministrador: false } }) });
  });
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

test('rechaza dos líneas de la misma existencia física antes de llamar al API', async ({ page }) => {
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
            id: 901,
            productoVarianteId: 101,
            productoNombre: 'Camisa',
            varianteSku: 'CAM-M',
            almacenId: 11,
            almacenCodigo: 'ALM-01',
            almacenNombre: 'Principal',
            ubicacionAlmacenId: 31,
            ubicacionCodigo: 'A-01',
            ubicacionNombre: 'Rack A',
            stockFisico: 20,
            stockReservado: 0,
            stockDisponible: 20,
            stockTransito: 0,
            stockMinimo: 1,
            stockMaximo: null,
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
    if (route.request().method() === 'POST') posts++;
    await route.continue();
  });

  await page.goto('/inventario/reservas/nueva');
  await page.getByRole('button', { name: 'Agregar línea', exact: true }).click();

  const selects = page.locator('mat-select[formcontrolname="existenciaVarianteId"]');
  await expect(selects).toHaveCount(2);
  for (let i = 0; i < 2; i++) {
    const select = selects.nth(i);
    await select.focus();
    await select.press('Enter');
    await page.getByRole('option', { name: /Camisa.*CAM-M/ }).click();
    await expect(page.locator('.cdk-overlay-backdrop')).toHaveCount(0);
  }

  await page.getByRole('button', { name: 'Guardar reserva', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('misma existencia física');
  expect(posts).toBe(0);
});
