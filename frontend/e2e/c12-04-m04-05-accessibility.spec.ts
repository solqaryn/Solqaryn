import { expect, test, type Page } from '@playwright/test';

async function sesionSintetica(page: Page, permitida: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'M0405_FAKE_NOT_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  const writes: string[] = [];
  page.on('request', r => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method());
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers });
      return;
    }
    await route.fulfill({
      status: permitida ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(permitida
        ? { success: true, data: { nombreUsuario: 'Test', rolNombre: 'Global',
          ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'PRIVATE_MYSQL_SECRET_MUST_NOT_LEAK' })
    });
  });
  return writes;
}

test('M04.05 nombres, landmarks y labels sin formulario operativo', async ({ page }) => {
  const writes = await sesionSintetica(page, true);
  await page.goto('/plataforma/mysql-cutover-readonly-v5');
  await expect(page.getByRole('main')).toHaveCount(1);
  await expect(page.getByRole('heading', { level: 1, name: 'Controles de preparación para cutover' })).toBeVisible();
  await expect(page.getByRole('group', { name: 'Tema visual permitido' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Volver a plataforma' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Revalidar sesión de plataforma' })).toBeVisible();
  await expect(page.locator('.cutover input, .cutover form')).toHaveCount(0);
  await expect(page.locator('.controles li')).toHaveCount(4);
  expect(writes).toEqual([]);
});

test('M04.05 cuatro anchos sin overflow y controles legibles', async ({ page }) => {
  const writes = await sesionSintetica(page, true);
  await page.goto('/plataforma/mysql-cutover-readonly-v5');
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth))
      .toBeLessThanOrEqual(2);
    await expect(page.locator('.controles li')).toHaveCount(4);
  }
  expect(writes).toEqual([]);
});

test('M04.05 Tab autentico, foco visible, Enter solo GET', async ({ page }) => {
  const writes = await sesionSintetica(page, true);
  await page.goto('/plataforma/mysql-cutover-readonly-v5');
  const btn = page.getByRole('button', { name: 'Revalidar sesión de plataforma' });
  let found = false;
  for (let i = 0; i < 45; i++) {
    await page.keyboard.press('Tab');
    if (await btn.evaluate(el => document.activeElement === el)) { found = true; break; }
  }
  expect(found, 'Tab sin .focus() artificial').toBe(true);
  await expect(btn).toHaveCSS('outline-style', 'solid');
  const size = await btn.evaluate(el => ({
    height: el.getBoundingClientRect().height,
    outline: parseFloat(getComputedStyle(el).outlineWidth)
  }));
  expect(size.height).toBeGreaterThanOrEqual(44);
  expect(size.outline).toBeGreaterThanOrEqual(3);
  await page.keyboard.press('Enter');
  await expect(page.locator('[data-testid=c1204-m0402-state]')).toContainText('Éxito');
  expect(writes).toEqual([]);
});

test('M04.05 contraste WCAG AA en modo alto y targets 44 px', async ({ page }) => {
  const writes = await sesionSintetica(page, true);
  await page.goto('/plataforma/mysql-cutover-readonly-v5');
  await page.getByRole('button', { name: 'Alto contraste' }).click();
  const ratio = await page.locator('.cutover').evaluate(el => {
    const css = getComputedStyle(el);
    function luminance(color: string) {
      const nums = color.match(/[\d.]+/g);
      if (!nums || nums.length < 3) throw new Error('Color CSS no comparable: ' + color);
      const channels = nums.slice(0,3).map(Number).map(v => {
        const s = v / 255;
        return s <= 0.04045 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4;
      });
      return 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2];
    }
    const a = luminance(css.color), b = luminance(css.backgroundColor);
    return (Math.max(a,b) + 0.05) / (Math.min(a,b) + 0.05);
  });
  expect(ratio).toBeGreaterThanOrEqual(4.5);
  for (const selector of [
    '.cutover a[aria-label="Volver a plataforma"]',
    '.cutover [data-testid=c1204-m0402-revalidate]',
    '.cutover [data-testid=c1204-m0403-theme-institucional]',
    '.cutover [data-testid=c1204-m0403-theme-alto-contraste]'
  ]) {
    const height = await page.locator(selector).evaluate(el => el.getBoundingClientRect().height);
    expect(height, selector).toBeGreaterThanOrEqual(44);
  }
  expect(writes).toEqual([]);
});

test('M04.05 403 sintetico deniega controles y no filtra secreto', async ({ page }) => {
  const writes = await sesionSintetica(page, false);
  await page.goto('/plataforma/mysql-cutover-readonly-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.cutover .controles li')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_MYSQL_SECRET_MUST_NOT_LEAK')).toHaveCount(0);
  expect(writes).toEqual([]);
});
