import { expect, test, type Page } from '@playwright/test';
/** C02-04.M04.05: Chromium real, sesion SINTETICA. No JWT HTTP ni permisos productivos. */
async function session(page: Page, status: 200 | 403): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'm0405-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers }); return;
    }
    await route.fulfill({ status, headers, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: { nombreUsuario: 'Admin', rolNombre: 'Global',
            ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, data: null, message: 'SENSITIVE_GRANTS_AUDIT_LOG' }) });
  });
}
test('C02-04.M04.05 anonimo permanece fuera del control plane', async ({ page }) => {
  await page.goto('/plataforma/consola-global-v5');
  await expect(page).toHaveURL(/\/login(?:\?.*)?$/);
  await expect(page.locator('.control-cards li')).toHaveCount(0);
});
test('C02-04.M04.05 403 rechaza acceso sin exponer diagnostico', async ({ page }) => {
  await session(page, 403);
  await page.goto('/plataforma/consola-global-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.control-cards li')).toHaveCount(0);
  await expect(page.getByText('SENSITIVE_GRANTS_AUDIT_LOG')).toHaveCount(0);
});
test('C02-04.M04.05 mobile y escritorio preservan labels y cuatro tarjetas', async ({ page }) => {
  await session(page, 200);
  await page.goto('/plataforma/consola-global-v5');
  const panel = page.locator('.control-plane');
  await expect(panel.getByRole('heading', { name: 'Consola global auditada' })).toBeVisible();
  await expect(panel.locator('.control-cards li')).toHaveCount(4);
  await expect(panel.getByRole('navigation', { name: 'Navegación de consola global' })).toBeVisible();
  await expect(panel.getByRole('link', { name: 'Volver a administración de plataforma' })).toBeVisible();
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 812 });
    await expect(panel.locator('.control-cards li')).toHaveCount(4);
    const overflow = await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, 'horizontal overflow at ' + width + 'px').toBeLessThanOrEqual(2);
  }
  await expect(panel).toContainText('JWT HTTP y auditoría durable: NO_MEDIDO');
  await expect(panel.locator('input, textarea, form')).toHaveCount(0);
});
test('C02-04.M04.05 teclado Tab y foco visible 44px', async ({ page }) => {
  await session(page, 200);
  await page.goto('/plataforma/consola-global-v5');
  const link = page.getByRole('link', { name: 'Volver a administración de plataforma' });
  const button = page.getByRole('button', { name: 'Revalidar sesión global' });
  await link.focus();
  await page.keyboard.press('Tab');
  await expect(button).toBeFocused();
  const style = await button.evaluate(el => {
    const c = getComputedStyle(el), r = el.getBoundingClientRect();
    return { width: r.width, height: r.height, outline: parseFloat(c.outlineWidth), kind: c.outlineStyle };
  });
  expect(style.height).toBeGreaterThanOrEqual(44);
  expect(style.width).toBeGreaterThanOrEqual(44);
  expect(style.outline).toBeGreaterThanOrEqual(2);
  expect(style.kind).not.toBe('none');
});
test('C02-04.M04.05 contraste calculado de texto en tarjeta >= 4.5:1', async ({ page }) => {
  await session(page, 200);
  await page.goto('/plataforma/consola-global-v5');
  const ratio = await page.locator('.control-cards li').first().evaluate(el => {
    const style = getComputedStyle(el);
    const rgb = (s: string) => [...s.matchAll(/\d+(?:\.\d+)?/g)].slice(0, 3).map(m => Number(m[0]));
    const luminance = (s: string) => {
      const ch = rgb(s).map(v => {
        const x = v / 255;
        return x <= 0.04045 ? x / 12.92 : ((x + 0.055) / 1.055) ** 2.4;
      });
      return ch[0] * 0.2126 + ch[1] * 0.7152 + ch[2] * 0.0722;
    };
    const fg = luminance(style.color), bg = luminance(style.backgroundColor);
    return (Math.max(fg, bg) + 0.05) / (Math.min(fg, bg) + 0.05);
  });
  expect(ratio).toBeGreaterThanOrEqual(4.5);
});
