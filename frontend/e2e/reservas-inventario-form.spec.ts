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

test.describe('Reservas de inventario - formulario', () => {
  test('crea un borrador con clave física y cantidad', async ({ page }) => {
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
              id: 915,
              productoVarianteId: 15,
              productoNombre: 'Producto E2E',
              varianteSku: 'SKU-E2E-15',
              almacenId: 3,
              almacenCodigo: 'ALM-03',
              almacenNombre: 'Almacén E2E',
              ubicacionAlmacenId: 8,
              ubicacionCodigo: 'UBI-08',
              ubicacionNombre: 'Ubicación E2E',
              stockFisico: 10,
              stockReservado: 0,
              stockDisponible: 10,
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

    let payload: unknown;
    await page.route('**/reservas-inventario', async route => {
      if (route.request().method() !== 'POST') { await route.continue(); return; }
      payload = route.request().postDataJSON();
      await route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify({ success: true, message: 'Reserva creada correctamente.', data: { id: 901, numero: 'RES-E2E-901', ventaId: null, estado: 'Borrador', fechaCreacion: '2026-08-17T10:00:00Z', fechaExpiracion: null, fechaActivacion: null, fechaConsumo: null, fechaLiberacion: null, fechaExpiracionAplicada: null, fechaCancelacion: null, motivoLiberacion: null, motivoCancelacion: null, detalles: [{ id: 1, productoVarianteId: 15, almacenId: 3, ubicacionAlmacenId: 8, cantidadReservada: 4, cantidadConsumida: 0 }] } }) });
    });

    await page.goto('/inventario/reservas/nueva');
    await expect(page.getByRole('heading', { name: 'Nueva reserva' })).toBeVisible();

    const selector = page.locator('mat-select[formcontrolname="existenciaVarianteId"]').first();
    await selector.click();
    await expect(selector).toHaveAttribute('aria-expanded', 'true');
    await page.getByRole('option', { name: /Producto E2E.*SKU-E2E-15.*ALM-03.*UBI-08.*disponible 10/ }).click();
    await expect(selector).toHaveAttribute('aria-expanded', 'false');
    await expect(page.locator('.cdk-overlay-backdrop')).toHaveCount(0);

    await page.locator('input[formcontrolname="cantidad"]').first().fill('4');
    await page.getByRole('button', { name: 'Guardar reserva', exact: true }).click();

    await expect(page).toHaveURL(/\/inventario\/reservas\/901$/);
    expect(payload).toMatchObject({ detalles: [{ productoVarianteId: 15, almacenId: 3, ubicacionAlmacenId: 8, cantidad: 4 }] });
  });
});
