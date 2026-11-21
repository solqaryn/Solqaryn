import { expect, test } from '@playwright/test';

// C06-01.M04.04: real Angular navigation and negative proof of no submit/payment.
test('C06-01.M04.04 section navigation preserves route and focuses real content', async ({ page }) => {
  await page.goto('/conoce-solqaryn');
  const nav = page.getByRole('navigation', { name: 'Navegación de presentación' });
  await nav.getByRole('link', { name: 'Qué resuelve' }).click();
  await expect(page.locator('#beneficios')).toBeFocused();
  await expect(page.getByRole('heading', { name: 'Más contexto, menos saltos.' })).toBeVisible();
  await expect(page).toHaveURL(/\/conoce-solqaryn(?:#.*)?$/);
  await nav.getByRole('link', { name: 'Alcance' }).click();
  await expect(page.locator('#alcance')).toBeFocused();
  await expect(page.getByRole('heading', { name: 'La presentación es pública. Tus datos, no.' })).toBeVisible();
  await expect(page).toHaveURL(/\/conoce-solqaryn(?:#.*)?$/);
});
test('C06-01.M04.04 hero anchor never navigates to the root route', async ({ page }) => {
  await page.goto('/conoce-solqaryn');
  await page.getByRole('link', { name: 'Conocer la propuesta' }).click();
  await expect(page.locator('#beneficios')).toBeFocused();
  await expect(page).toHaveURL(/\/conoce-solqaryn(?:#.*)?$/);
});
test('C06-01.M04.04 informative screen has no form or repeated mutation on rapid login action', async ({ page }) => {
  const mutations: string[] = [];
  page.on('request', request => {
    if (/POST|PUT|PATCH|DELETE/.test(request.method())) mutations.push(request.method() + ' ' + request.url());
  });
  await page.goto('/conoce-solqaryn');
  const root = page.locator('app-c0601-public-saas-landing');
  await expect(root.locator('form, input, textarea, select, button[type=submit]')).toHaveCount(0);
  await expect(root.getByText('La presentación es pública. Tus datos, no.')).toBeVisible();
  await root.getByRole('link', { name: 'Ir a mi cuenta' }).evaluate(link => {
    link.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
    link.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
  });
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  expect(mutations, 'route links must not submit twice or mutate data').toEqual([]);
});
