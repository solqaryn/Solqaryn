import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function loginConPermisos(page: Page): Promise<void> {
  await page.route('**/permisos/mis-permisos/empresa/*', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        message: 'Permisos cargados',
        data: {
          permisos: ['Dashboard:Ver', 'MovimientosInventario:Ver', 'MovimientosInventario:Crear'],
          esAdministrador: false
        }
      })
    });
  });

  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

test.describe('Reservas de inventario - selector físico', () => {
  test('crea una reserva enviando la clave física derivada de ExistenciaVariante', async ({ page }) => {
    await loginConPermisos(page);

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
              productoNombre: 'Camisa Premium',
              varianteSku: 'CAM-AZ-M',
              almacenId: 11,
              almacenCodigo: 'ALM-01',
              almacenNombre: 'Principal',
              ubicacionAlmacenId: 31,
              ubicacionCodigo: 'A-01',
              ubicacionNombre: 'Rack A',
              stockFisico: 12,
              stockReservado: 2,
              stockDisponible: 10,
              stockTransito: 0,
              stockMinimo: 1,
              stockMaximo: 50,
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

    let payload: Record<string, unknown> | undefined;
    await page.route('**/reservas-inventario', async route => {
      if (route.request().method() !== 'POST') return route.continue();
      payload = route.request().postDataJSON() as Record<string, unknown>;
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({ success: true, message: 'Creada', data: { id: 701 } })
      });
    });

    await page.goto('/inventario/reservas/nueva');
    const selector = page.locator('mat-select[formcontrolname="existenciaVarianteId"]').first();
    await selector.focus();
    await selector.press('Enter');
    await expect(selector).toHaveAttribute('aria-expanded', 'true');
    await page.getByRole('option', { name: /Camisa Premium.*CAM-AZ-M.*ALM-01.*A-01.*disponible 10/ }).click();
    await expect(selector).toHaveAttribute('aria-expanded', 'false');
    await expect(page.locator('.cdk-overlay-backdrop')).toHaveCount(0);

    await page.locator('input[formcontrolname="cantidad"]').first().fill('4');
    await page.getByRole('button', { name: 'Guardar reserva', exact: true }).click();

    await expect.poll(() => payload).toBeTruthy();
    expect(payload).toMatchObject({
      detalles: [{ productoVarianteId: 101, almacenId: 11, ubicacionAlmacenId: 31, cantidad: 4 }]
    });
  });

  test('bloquea localmente una cantidad superior al stock disponible', async ({ page }) => {
    await loginConPermisos(page);

    await page.route('**/existencias-variante**', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          message: 'OK',
          data: {
            items: [{
              id: 902,
              productoVarianteId: 102,
              productoNombre: 'Pantalón',
              varianteSku: 'PAN-NEG-L',
              almacenId: 11,
              almacenCodigo: 'ALM-01',
              almacenNombre: 'Principal',
              ubicacionAlmacenId: null,
              ubicacionCodigo: null,
              ubicacionNombre: null,
              stockFisico: 3,
              stockReservado: 1,
              stockDisponible: 2,
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

    let postCount = 0;
    await page.route('**/reservas-inventario', async route => {
      if (route.request().method() === 'POST') postCount++;
      await route.continue();
    });

    await page.goto('/inventario/reservas/nueva');
    const selector = page.locator('mat-select[formcontrolname="existenciaVarianteId"]').first();
    await selector.focus();
    await selector.press('Enter');
    await page.getByRole('option', { name: /Pantalón.*PAN-NEG-L.*disponible 2/ }).click();
    await expect(page.locator('.cdk-overlay-backdrop')).toHaveCount(0);

    await page.locator('input[formcontrolname="cantidad"]').first().fill('3');
    await page.getByRole('button', { name: 'Guardar reserva', exact: true }).click();

    await expect(page.getByRole('alert')).toContainText('supera el stock disponible');
    expect(postCount).toBe(0);
  });
});
