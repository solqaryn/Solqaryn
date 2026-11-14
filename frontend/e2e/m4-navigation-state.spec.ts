import { test, expect, Page } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function loginUi(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => url.pathname !== '/login', { timeout: 20_000 });
}

test('M4 restaura filtros, pagina, orden y aísla sessionStorage por usuario', async ({ page }) => {
  await loginUi(page);

  await page.goto('/productos?search=M4Producto&pageSize=25&sortBy=Precio&sortDirection=desc&estado=activos');
  await expect(page.getByRole('textbox', { name: 'Buscar productos' })).toHaveValue('M4Producto');
  await expect(page).toHaveURL(/search=M4Producto/);
  await expect.poll(async () => page.evaluate(() => Object.keys(sessionStorage).some((key) => key.includes('.e2e_admin.productos')))).toBe(true);

  await page.goto('/dashboard');
  await page.goto('/productos');
  await expect(page.getByRole('textbox', { name: 'Buscar productos' })).toHaveValue('M4Producto');
  await expect(page).toHaveURL(/search=M4Producto/);

  await page.goto('/ventas?search=M4Venta&pageSize=25&sortBy=Total&sortDirection=asc');
  await expect(page.getByRole('textbox', { name: 'Buscar por número o cliente' })).toHaveValue('M4Venta');
  await page.goto('/dashboard');
  await page.goto('/ventas');
  await expect(page.getByRole('textbox', { name: 'Buscar por número o cliente' })).toHaveValue('M4Venta');
  await expect(page).toHaveURL(/search=M4Venta/);

  await page.goto('/ventas?search=URLTienePrioridad');
  await expect(page.getByRole('textbox', { name: 'Buscar por número o cliente' })).toHaveValue('URLTienePrioridad');
  await page.goto('/dashboard');
  await page.goto('/ventas');
  await expect(page.getByRole('textbox', { name: 'Buscar por número o cliente' })).toHaveValue('URLTienePrioridad');

  await page.goto('/compras?search=M4Compra&pageSize=25&sortBy=Total&sortDirection=asc');
  await expect(page.getByRole('textbox', { name: 'Buscar por número o proveedor' })).toHaveValue('M4Compra');
  await page.goto('/dashboard');
  await page.goto('/compras');
  await expect(page.getByRole('textbox', { name: 'Buscar por número o proveedor' })).toHaveValue('M4Compra');

  await page.goto('/clientes?search=M4Cliente&estado=activos&pageSize=25&sortBy=totalVendido&sortDirection=desc');
  await expect(page.getByRole('textbox', { name: 'Buscar clientes' })).toHaveValue('M4Cliente');
  await page.goto('/dashboard');
  await page.goto('/clientes');
  await expect(page.getByRole('textbox', { name: 'Buscar clientes' })).toHaveValue('M4Cliente');

  await page.goto('/inventario/movimientos?filtroTipo=Entrada&filtroCausa=Compra&correlationId=M4-CORR&pageSize=25');
  await expect(page.getByRole('combobox', { name: 'Tipo de movimiento', exact: true })).toContainText('Entrada');
  await expect(page.getByRole('combobox', { name: 'Causa', exact: true })).toContainText('Compra');
  await expect(page.getByRole('textbox', { name: 'Correlation ID', exact: true })).toHaveValue('M4-CORR');
  await expect(page).toHaveURL(/filtroTipo=Entrada/);
  await page.goto('/dashboard');
  await page.goto('/inventario/movimientos');
  await expect(page.getByRole('combobox', { name: 'Tipo de movimiento', exact: true })).toContainText('Entrada');
  await expect(page.getByRole('combobox', { name: 'Causa', exact: true })).toContainText('Compra');
  await expect(page.getByRole('textbox', { name: 'Correlation ID', exact: true })).toHaveValue('M4-CORR');
  await expect(page).toHaveURL(/correlationId=M4-CORR/);

  await page.goto('/finanzas?search=M4Finanzas&filtroTipo=Egreso&sortBy=monto&sortDirection=asc');
  await expect(page.getByRole('textbox', { name: 'Buscar movimiento' })).toHaveValue('M4Finanzas');
  await page.goto('/dashboard');
  await page.goto('/finanzas');
  await expect(page.getByRole('textbox', { name: 'Buscar movimiento' })).toHaveValue('M4Finanzas');

  await page.getByRole('button', { name: 'Limpiar filtros' }).click();
  await expect(page.getByRole('textbox', { name: 'Buscar movimiento' })).toHaveValue('');
  await expect(page).not.toHaveURL(/search=/);
});
