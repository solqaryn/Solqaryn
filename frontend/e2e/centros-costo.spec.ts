import { expect, test } from '@playwright/test';

test.describe('Centros de costo - regresión E2E N4.11.G', () => {
  test('redirige a login cuando se intenta abrir el módulo sin sesión', async ({ page }) => {
    await page.goto('/centros-costo');

    await expect(page).toHaveURL(/\/login(?:\?|$)/);
  });
});
