import { expect, Page, test } from '@playwright/test';

const API_URL = 'http://localhost:5005';

function api<T>(data: T) {
  return { success: true, message: 'OK', errors: [], data };
}

async function prepararSesion(page: Page): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'e2e-token-n08');
    localStorage.setItem('solqaryn_user', 'e2e_admin');
    localStorage.setItem('solqaryn_nombre_completo', 'Administrador E2E N0.8');
    localStorage.setItem('solqaryn_rol', 'Administrador');
    localStorage.setItem('solqaryn_expira_en', '2099-12-31T23:59:59Z');
  });

  await page.route(`${API_URL}/permisos/mis-permisos/empresa/*`, route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify(api({ esAdministrador: true, permisos: [] }))
  }));
}

const metodosActivos = [
  {
    id: 21,
    codigo: 'QR_EMPRESARIAL',
    nombre: 'QR empresarial',
    tipo: 'Otro',
    activo: true,
    eliminado: false,
    requiereReferencia: false,
    requiereBanco: false,
    permiteCambio: false,
    orden: 0,
    metadata: null
  },
  {
    id: 22,
    codigo: 'EFECTIVO',
    nombre: 'Efectivo',
    tipo: 'Efectivo',
    activo: true,
    eliminado: false,
    requiereReferencia: false,
    requiereBanco: false,
    permiteCambio: true,
    orden: 1,
    metadata: null
  }
];

test.describe('ERP N0.8 — Compras usa MetodoPago relacional dinámico', () => {
  test.beforeEach(async ({ page }) => {
    await prepararSesion(page);
  });

  test('nueva compra ofrece métodos administrables del catálogo y no una lista hardcodeada', async ({ page }) => {
    await page.route(`${API_URL}/metodos-pago/activos`, route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(api(metodosActivos))
    }));

    await page.goto('/compras/nueva');
    await expect(page.getByRole('heading', { name: 'Nueva compra' })).toBeVisible();

    const campoMetodo = page.locator('mat-form-field').filter({ hasText: 'Método de pago' });
    const selector = campoMetodo.locator('mat-select');
    await expect(selector).toContainText('QR empresarial');

    await selector.click();
    await expect(page.getByRole('option', { name: 'QR empresarial' })).toBeVisible();
    await expect(page.getByRole('option', { name: 'Efectivo' })).toBeVisible();
    await expect(page.getByRole('option', { name: 'Transferencia' })).toHaveCount(0);
    await expect(page.getByRole('option', { name: 'Tarjeta' })).toHaveCount(0);
    await expect(page.getByRole('option', { name: 'Otro' })).toHaveCount(0);

    await page.getByRole('option', { name: 'Efectivo' }).click();
    await expect(selector).toContainText('Efectivo');
  });

  test('si el catálogo activo falla, compra queda fail-closed y permite reintentar', async ({ page }) => {
    await page.route(`${API_URL}/metodos-pago/activos`, route => route.fulfill({
      status: 503,
      contentType: 'application/json',
      body: JSON.stringify({ success: false, message: 'Catálogo no disponible', errors: [], data: null })
    }));

    await page.goto('/compras/nueva');
    await expect(page.getByRole('heading', { name: 'Nueva compra' })).toBeVisible();
    await expect(page.getByText('No se pudieron cargar los métodos de pago activos.')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Reintentar métodos de pago' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Guardar borrador' })).toBeDisabled();
  });
});
