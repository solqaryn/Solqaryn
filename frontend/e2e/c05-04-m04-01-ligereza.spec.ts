import { expect, test, type Page } from '@playwright/test';

/** Session fixtures are synthetic; they do not certify live JWT/RBAC or MySQL. */
async function session(page: Page, allow: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0504-m0401-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    return route.fulfill({
      status: allow ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(allow
        ? { success: true, data: { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'DO_NOT_RENDER_PRIVATE_PROVIDER_MESSAGE' })
    });
  });
  // Existing directory GET is unrelated to this local consultation surface.
  await page.route(/\/plataforma\/empresas(?:\?|$)/, async route =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, data: { items: [], total: 0 } }) }));
}

test('C05-04.M04.01 sin sesión: guard bloquea la superficie', async ({ page }) => {
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.ui-lightness-v5')).toHaveCount(0);
});

test('C05-04.M04.01 403 sintético deniega superficie y no filtra mensaje privado', async ({ page }) => {
  await session(page, false);
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.ui-lightness-v5')).toHaveCount(0);
  await expect(page.getByText('DO_NOT_RENDER_PRIVATE_PROVIDER_MESSAGE')).toHaveCount(0);
});

test('C05-04.M04.01 consulta accesible y modo compacto con teclado, sin escritura HTTP', async ({ page }) => {
  await session(page, true);
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  await page.goto('/plataforma');
  const summary = page.locator('.ui-lightness-v5 summary');
  await expect(summary).toBeVisible();
  await summary.click();
  const panel = page.locator('.ui-lightness-v5');
  await expect(panel.getByRole('heading', { name: 'Presupuesto de ligereza visual' })).toBeVisible();
  await expect(panel.locator('.ui-lightness-v5__checks li')).toHaveCount(3);
  await expect(panel).toContainText('no medido aquí');
  const toggle = panel.getByRole('button', { name: 'Reducir detalles' });
  for (let i = 0; i < 35; i++) {
    if (await toggle.evaluate(el => el === document.activeElement)) break;
    await page.keyboard.press('Tab');
  }
  await expect(toggle).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(panel.locator('.ui-lightness-v5__checks li')).toHaveCount(0);
  await expect(panel.locator('.ui-lightness-v5__status')).toContainText('tres criterios');
  await expect(panel.getByRole('button', { name: 'Ver criterios' })).toHaveAttribute('aria-pressed', 'true');
  expect(writes, 'la consulta no debe enviar POST/PUT/PATCH/DELETE').toEqual([]);
});

test('C05-04.M04.01 evita overflow horizontal en móviles y escritorio', async ({ page }) => {
  await session(page, true);
  await page.goto('/plataforma');
  await page.locator('.ui-lightness-v5 summary').click();
  const panel = page.locator('.ui-lightness-v5');
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await expect(panel.locator('.ui-lightness-v5__checks li')).toHaveCount(3);
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, 'desbordamiento en ' + width).toBeLessThanOrEqual(2);
  }
});
