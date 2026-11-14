import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function loginConPermisos(page: Page, permisos: string[]): Promise<void> {
  await page.route('**/permisos/mis-permisos/empresa/*', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, message: 'Permisos cargados', data: { permisos, esAdministrador: false } })
    });
  });

  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/dashboard(?:\?|$)/, { timeout: 20_000 });
}

async function mockCosteoLectura(page: Page): Promise<void> {
  await page.route('**/costeo-inventario/politica-vigente', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, message: '', errors: [], data: {
      id: 1,
      empresaConfiguracionId: 1,
      metodo: 1,
      metodoNombre: 'Promedio Ponderado',
      vigenteDesdeUtc: '2026-08-18T09:00:00Z',
      vigenteHastaUtc: null,
      estaVigente: true,
      motivo: 'Política inicial',
      fechaCreacion: '2026-08-18T09:00:00Z',
      fechaActualizacion: '2026-08-18T09:00:00Z'
    } })
  }));
  await page.route('**/costeo-inventario/metodos', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', errors: [], data: [{ id: 1, nombre: 'Promedio Ponderado' }, { id: 2, nombre: 'FIFO' }] }) }));
  await page.route('**/costeo-inventario/politicas**', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', errors: [], data: { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 } }) }));
}

test.describe('Costeo de inventario - acceso', () => {
  test('redirige al login cuando no existe sesión', async ({ page }) => {
    await page.goto('/inventario/costeo');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
  });

  test('deniega la ruta a un usuario autenticado sin MovimientosInventario:Ver', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver']);
    await page.goto('/inventario/costeo');
    await expect(page).toHaveURL(/\/dashboard(?:\?|$)/);
    await expect(page).not.toHaveURL(/\/inventario\/costeo(?:\?|$)/);
  });

  test('permite consulta con Ver pero oculta el cambio de política sin Editar', async ({ page }) => {
    await loginConPermisos(page, ['Dashboard:Ver', 'MovimientosInventario:Ver']);
    await mockCosteoLectura(page);
    await page.goto('/inventario/costeo');
    await expect(page).toHaveURL(/\/inventario\/costeo(?:\?|$)/);
    await expect(page.getByRole('heading', { name: 'Política de costeo' })).toBeVisible();
    await expect(page.getByText('Promedio Ponderado', { exact: true }).first()).toBeVisible();
    await expect(page.locator('mat-card-title').filter({ hasText: /^Cambiar política$/ })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Aplicar política' })).toHaveCount(0);
  });
});
