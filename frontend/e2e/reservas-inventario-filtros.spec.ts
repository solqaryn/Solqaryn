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

async function mockAlmacenes(page: Page): Promise<void> {
  await page.route('**/almacenes/activos', async route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', data: [{ id: 11, codigo: 'BOD-11', nombre: 'Bodega Central', activo: true }] }) }));
}

test('filtra reservas usando el catálogo activo de almacenes', async ({ page }) => {
  await login(page);
  const consultas: string[] = [];
  await mockAlmacenes(page);
  await page.route('**/reservas-inventario**', async route => {
    consultas.push(route.request().url());
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', data: { items: [], totalCount: 0, page: 1, pageSize: 20 } }) });
  });

  await page.goto('/inventario/reservas');
  const almacen = page.getByRole('combobox', { name: 'Almacén' });
  await expect(almacen).toBeVisible();
  await almacen.focus();
  await expect(almacen).toBeFocused();
  await almacen.press('Enter');
  await expect(almacen).toHaveAttribute('aria-expanded', 'true');
  await page.getByRole('option', { name: 'BOD-11 · Bodega Central' }).click();
  await expect(almacen).toHaveAttribute('aria-expanded', 'false');
  await expect(page.locator('.cdk-overlay-backdrop')).toHaveCount(0);
  await page.getByRole('button', { name: 'Filtrar', exact: true }).click();

  await expect.poll(() => consultas.some(url => new URL(url).searchParams.get('almacenId') === '11')).toBeTruthy();
});

test('propaga el rango de expiración como fechas ISO', async ({ page }) => {
  await login(page);
  const consultas: string[] = [];
  await mockAlmacenes(page);
  await page.route('**/reservas-inventario**', async route => {
    consultas.push(route.request().url());
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', data: { items: [], totalCount: 0, page: 1, pageSize: 20 } }) });
  });

  await page.goto('/inventario/reservas');
  await page.locator('input[name="expiraDesde"]').fill('2026-08-18T08:00');
  await page.locator('input[name="expiraHasta"]').fill('2026-08-19T18:30');
  await page.getByRole('button', { name: 'Filtrar', exact: true }).click();

  await expect.poll(() => consultas.some(url => {
    const params = new URL(url).searchParams;
    const desde = params.get('expiraDesde');
    const hasta = params.get('expiraHasta');
    return Boolean(desde && hasta && !Number.isNaN(Date.parse(desde)) && !Number.isNaN(Date.parse(hasta)) && Date.parse(desde) < Date.parse(hasta));
  })).toBeTruthy();
});

test('rechaza localmente un rango de expiración invertido', async ({ page }) => {
  await login(page);
  let consultas = 0;
  await mockAlmacenes(page);
  await page.route('**/reservas-inventario**', async route => {
    consultas += 1;
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, message: '', data: { items: [], totalCount: 0, page: 1, pageSize: 20 } }) });
  });

  await page.goto('/inventario/reservas');
  await expect.poll(() => consultas).toBeGreaterThan(0);
  const consultasIniciales = consultas;
  await page.locator('input[name="expiraDesde"]').fill('2026-08-20T18:00');
  await page.locator('input[name="expiraHasta"]').fill('2026-08-19T08:00');
  await page.getByRole('button', { name: 'Filtrar', exact: true }).click();

  await expect(page.getByRole('alert')).toContainText('“Expira desde” no puede ser posterior a “Expira hasta”');
  expect(consultas).toBe(consultasIniciales);
});
