import { expect, test } from '@playwright/test';

test.describe('V5 C00-01.M04.01 — superficie de rutas públicas y acceso protegido', () => {
  test('ruta desconocida presenta pantalla pública sin divulgar parámetros y permite volver al acceso', async ({ page }) => {
    await page.goto('/modulo-desconocido?empresaId=999&secreto=no-mostrar');
    const main = page.getByRole('main');
    await expect(main.getByRole('heading', { name: 'Página no encontrada' })).toBeVisible();
    await expect(main).not.toContainText('999');
    await expect(main).not.toContainText('no-mostrar');
    await expect(main.getByRole('link', { name: 'Ver tienda pública' })).toHaveAttribute('href', '/tienda');

    await main.getByRole('link', { name: 'Ir a iniciar sesión' }).click();
    await expect(page).toHaveURL(/\/login$/);
    await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
  });

  test('ruta pública conocida mantiene el acceso sin mostrar error de navegación', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Página no encontrada' })).toHaveCount(0);
  });

  test('ruta privada sin sesión conserva el guard y no expone el dashboard', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login(?:\?.*)?$/);
    await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
    await expect(page.getByText('Ingresos del mes')).toHaveCount(0);
  });
});
