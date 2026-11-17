import { expect, test, type Page } from '@playwright/test';

// C05-01.M05.06: Angular/Chromium real; sesion HTTP sintetica, NO JWT real.
type Sesion = { status: 200 | 403 };
async function configurar(page: Page, sesion: Sesion) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0501m0506-fixture-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
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
      status: sesion.status, headers, contentType: 'application/json',
      body: JSON.stringify({
        success: sesion.status === 200,
        data: sesion.status === 200
          ? { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true }
          : null,
        message: 'PRIVATE_V19_DIAGNOSTIC_MUST_NOT_LEAK'
      })
    });
  });
}

test('C05-01.M05.06 Tab desde inicio, Enter, foco visible y 44px con reduced-motion', async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await configurar(page, { status: 200 });
  await page.goto('/plataforma');
  const details = page.locator('details.design-system-preview');
  const summary = details.locator('summary');
  await expect(summary).toBeVisible();
  let summaryReached = false;
  for (let i = 0; i < 120; i++) {
    await page.keyboard.press('Tab');
    if (await summary.evaluate(node => document.activeElement === node)) {
      summaryReached = true;
      break;
    }
  }
  expect(summaryReached, 'summary debe alcanzarse por Tab natural').toBe(true);
  await page.keyboard.press('Enter');
  await expect(details).toHaveAttribute('open', '');
  const contrast = details.getByRole('button', { name: 'Contraste alto' });
  let contrastReached = false;
  for (let i = 0; i < 20; i++) {
    await page.keyboard.press('Tab');
    if (await contrast.evaluate(node => document.activeElement === node)) {
      contrastReached = true;
      break;
    }
  }
  expect(contrastReached, 'preset debe alcanzarse por Tab').toBe(true);
  await expect(contrast).toHaveCSS('outline-style', 'solid');
  const box = await contrast.boundingBox();
  expect(box).not.toBeNull();
  expect(box!.height).toBeGreaterThanOrEqual(44);
  expect(box!.width).toBeGreaterThanOrEqual(44);
  await page.keyboard.press('Enter');
  await expect(contrast).toHaveAttribute('aria-pressed', 'true');
  await expect(details.locator('.brand-preview')).toHaveClass(/brand-preview-contrast/);
  const transition = await contrast.evaluate(el => getComputedStyle(el).transitionDuration);
  expect(transition.split(',').every(v => Number.parseFloat(v) === 0)).toBe(true);
});

test('C05-01.M05.06 responsive y presupuesto HTTP sin refetch por preset', async ({ page }) => {
  let gets = 0;
  const writes: string[] = [];
  page.on('request', r => {
    if (r.method() === 'GET' && /\/plataforma\/sesion(?:\?|$)/.test(r.url())) gets++;
    if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method());
  });
  await configurar(page, { status: 200 });
  await page.goto('/plataforma');
  const details = page.locator('details.design-system-preview');
  await details.locator('summary').click();
  const catalog = details.locator('.design-catalog');
  await expect(catalog.locator('.token-item')).toHaveCount(4);
  await expect(catalog.locator('.state-example')).toHaveCount(5);
  await expect(catalog.locator('.brand-choice')).toHaveCount(2);
  expect(gets).toBeGreaterThanOrEqual(1);
  expect(gets).toBeLessThanOrEqual(2);
  const initialGets = gets;
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    const overflow = await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, 'overflow a ' + width).toBeLessThanOrEqual(2);
    await expect(catalog.locator('.token-item')).toHaveCount(4);
  }
  const contrast = details.getByRole('button', { name: 'Contraste alto' });
  await contrast.click();
  await contrast.click();
  await details.getByRole('button', { name: 'Institucional' }).click();
  expect(gets, 'preset y viewport no deben refetchear sesion').toBe(initialGets);
  expect(writes).toEqual([]);
});

test('C05-01.M05.06 403 tras exito niega vista y diagnostico sin escrituras', async ({ page }) => {
  const state: Sesion = { status: 200 };
  const writes: string[] = [];
  page.on('request', r => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method());
  });
  await configurar(page, state);
  await page.goto('/plataforma');
  await page.locator('details.design-system-preview summary').click();
  await expect(page.locator('.design-catalog .token-item')).toHaveCount(4);
  state.status = 403;
  await page.reload();
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.design-catalog .token-item, .design-catalog .brand-choice')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_V19_DIAGNOSTIC_MUST_NOT_LEAK')).toHaveCount(0);
  expect(writes).toEqual([]);
});
