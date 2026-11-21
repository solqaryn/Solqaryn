import { test, expect } from '@playwright/test';

// Public Angular route under actual Chromium: no JWT fixture or API mocks.
test('C06-01.M04.01 anonymous visitor sees public SaaS landing and safe login link', async ({ page }) => {
  const privateRequests: string[] = [];
  page.on('request', request => {
    const path = new URL(request.url()).pathname;
    if (/\/api\//.test(path) || /\/tenant-context|\/productos|\/empresas/.test(path)) {
      privateRequests.push(request.method() + ' ' + path);
    }
  });
  await page.goto('/conoce-solqaryn');
  await expect(page.getByRole('heading', { level: 1, name: 'Tu operación merece un lugar claro.' })).toBeVisible();
  await expect(page.getByTestId('c0601-public-saas-landing')).toBeVisible();
  await expect(page.getByRole('link', { name: /Ir a mi cuenta/ })).toHaveAttribute('href', '/login');
  expect(privateRequests).toEqual([]);
});

test('C06-01.M04.01 public page is keyboard navigable without authentication', async ({ page }) => {
  await page.goto('/conoce-solqaryn');
  await page.keyboard.press('Tab');
  await expect(page.getByRole('link', { name: 'Ir al contenido' })).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(page.locator('#c0601-content')).toBeFocused();
  await expect(page.getByRole('navigation', { name: 'Navegación de presentación' })).toBeVisible();
});

test('C06-01.M04.01 mobile Chromium renders no horizontal overflow and clear CTA', async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 812 });
  await page.goto('/conoce-solqaryn');
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  await expect(page.getByRole('link', { name: /Abrir inicio de sesión/ })).toBeVisible();
  const size = await page.evaluate(() => ({ scrollWidth: document.documentElement.scrollWidth, viewport: window.innerWidth }));
  expect(size.scrollWidth).toBeLessThanOrEqual(size.viewport);
});

test('C06-01.M04.01 page does not publish fictional pricing or private data', async ({ page }) => {
  await page.goto('/conoce-solqaryn');
  await expect(page.getByRole('heading', { name: 'La presentación es pública. Tus datos, no.' })).toBeVisible();
  await expect(page.getByText('Esta página no consulta datos de clientes, empresas, inventario ni cuentas;', { exact: false })).toBeVisible();
  await expect(page.getByRole('button', { name: /Pagar|Comprar|Suscribirme/i })).toHaveCount(0);
  await page.getByRole('link', { name: /Ir a mi cuenta/ }).click();
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
});
