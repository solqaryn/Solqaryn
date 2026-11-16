import { expect, test, type Page } from '@playwright/test';

// UI aislada: token y API sinteticos, NO JWT/tenant HTTP real.
async function session(page: Page, authorized: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'synthetic-not-a-real-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    await route.fulfill({
      status: authorized ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(authorized
        ? { success: true, data: { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'PRIVATE_SERVER_DIAGNOSTIC' })
    });
  });
}

test('C14-02.M04.05 320 y 1280 px sin scroll ni controles táctiles menores a 44px', async ({ page }) => {
  await session(page, true);
  for (const width of [320, 1280]) {
    await page.setViewportSize({ width, height: 844 });
    await page.goto('/plataforma/revision-amenazas-v5');
    await expect(page.getByRole('heading', { name: 'Verificaciones de acceso y abuso' })).toBeVisible();
    await expect(page.locator('.threat-list li')).toHaveCount(4);
    const sizing = await page.evaluate(() => ({
      overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
      heights: Array.from(document.querySelectorAll('.threat-guide button, .threat-guide header a'))
        .map(node => node.getBoundingClientRect().height)
    }));
    expect(sizing.overflow).toBeLessThanOrEqual(2);
    expect(sizing.heights).toHaveLength(4);
    expect(sizing.heights.every(height => height >= 44)).toBe(true);
    await expect(page.locator('.threat-guide form, .threat-guide input')).toHaveCount(0);
  }
});

test('C14-02.M04.05 tabulacion conserva foco visible y navegación de solo lectura', async ({ page }) => {
  await session(page, true);
  await page.goto('/plataforma/revision-amenazas-v5');
  const back = page.getByRole('link', { name: 'Volver a administración de plataforma' });
  const reload = page.getByRole('button', { name: 'Revalidar sesión de plataforma' });
  await back.focus();
  await expect(back).toBeFocused();
  await page.keyboard.press('Tab');
  await expect(reload).toBeFocused();
  const outline = await reload.evaluate(element => parseFloat(getComputedStyle(element).outlineWidth));
  expect(outline).toBeGreaterThanOrEqual(3);
  await page.keyboard.press('Enter');
  await expect(page.getByText(/Sesión de plataforma confirmada/)).toBeVisible();
  await expect(page.locator('.threat-list li')).toHaveCount(4);
});

test('C14-02.M04.05 apariencia neutra contraste de texto normal >= 4.5', async ({ page }) => {
  await session(page, true);
  await page.goto('/plataforma/revision-amenazas-v5');
  await page.getByRole('button', { name: 'Presentación neutra' }).click();
  const result = await page.locator('.threat-guide').evaluate(element => {
    const style = getComputedStyle(element);
    const channels = (value: string) => (value.match(/[\d.]+/g) ?? []).slice(0, 3).map(Number);
    const luminance = (values: number[]) => values.reduce((sum, number, index) => {
      const v = number / 255;
      return sum + (v <= 0.04045 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4)
        * [0.2126, 0.7152, 0.0722][index];
    }, 0);
    const a = channels(style.color), b = channels(style.backgroundColor);
    if (a.length !== 3 || b.length !== 3) return { contrast: 0, inline: true };
    const x = luminance(a), y = luminance(b);
    return { contrast: (Math.max(x, y) + 0.05) / (Math.min(x, y) + 0.05),
      inline: element.hasAttribute('style') };
  });
  expect(result.contrast).toBeGreaterThanOrEqual(4.5);
  expect(result.inline).toBe(false);
});

test('C14-02.M04.05 denegación 403 sin revelar datos ni controles internos', async ({ page }) => {
  await session(page, false);
  await page.goto('/plataforma/revision-amenazas-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.threat-list li')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_SERVER_DIAGNOSTIC')).toHaveCount(0);
});
