import { expect, test, type Page } from '@playwright/test';

const url = '/plataforma/enlaces-cobro-simulacion-v22';
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function sessionFixture(page: Page, gate?: Promise<void>) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'C0404_M0406_SYNTHETIC_NOT_SIGNED_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    if (gate) await gate;
    await route.fulfill({
      status: 200, headers: cors, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: {
        nombreUsuario: 'Fixture', rolNombre: 'Plataforma',
        ambito: 'Plataforma', esAdministrador: true
      } })
    });
  });
}

test('C04-04.M04.06 density at text zoom 200 percent remains lightweight', async ({ page }) => {
  await sessionFixture(page);
  const mutations: string[] = [];
  page.on('request', req => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(req.method())) mutations.push(req.method());
  });
  await page.goto(url);
  const panel = page.getByTestId('c0404-payment-private');
  await expect(panel).toBeVisible();
  await expect(panel.locator('[data-testid^="c0404-step-"]')).toHaveCount(3);
  await expect(panel.locator('table,video,canvas,iframe,form,input,button')).toHaveCount(0);
  const nodeCount = await panel.locator('*').count();
  expect(nodeCount, 'bounded private DOM density').toBeLessThanOrEqual(90);
  await page.evaluate(() => { document.documentElement.style.fontSize = '200%'; });
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    const layout = await page.evaluate(() => ({
      overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth
    }));
    expect(layout.overflow, 'horizontal overflow width=' + width).toBeLessThanOrEqual(2);
    await expect(page.getByRole('link', { name: 'Volver a plataforma' })).toBeVisible();
  }
  expect(mutations).toEqual([]);
});

test('C04-04.M04.06 slow session never displays private UI before authorization', async ({ page }) => {
  let releaseGate: (() => void) | undefined;
  const gate = new Promise<void>(resolve => { releaseGate = resolve; });
  await sessionFixture(page, gate);
  const mutations: string[] = [];
  page.on('request', req => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(req.method())) mutations.push(req.method());
  });
  const sessionStarted = page.waitForRequest(req =>
    /\/plataforma\/sesion(?:\?|$)/.test(req.url()) && req.method() !== 'OPTIONS'
  );
  const navigation = page.goto(url);
  try {
    await sessionStarted;
    await expect(page.getByTestId('c0404-payment-private')).toHaveCount(0);
  } finally {
    releaseGate?.();
  }
  await navigation;
  await expect(page.getByTestId('c0404-payment-private')).toBeVisible();
  await expect(page.getByTestId('c0404-state-empty')).toHaveAttribute('role', 'status');
  await expect(page.getByTestId('c0404-payment-private').locator('[data-testid^="c0404-step-"]')).toHaveCount(3);
  expect(mutations).toEqual([]);
});
