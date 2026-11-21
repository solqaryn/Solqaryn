import { expect, test, type Page } from '@playwright/test';

// C04-01.M04.06: actual Angular 22 + Chromium; HTTP platform session is synthetic.
// This never certifies signed JWT, commercial billing, MySQL or real Web Vitals.
const route = '/plataforma/catalogo-planes-v22';
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

async function platformSession(page: Page, status: 200 | 403 | 503) {
  const writes: string[] = [];
  const privateRequests: string[] = [];
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'SYNTHETIC_TEST_ONLY_NOT_SIGNED_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  page.on('request', request => {
    const path = new URL(request.url()).pathname;
    if (/\/(api|plataforma)\//.test(path)) privateRequests.push(request.method() + ' ' + path);
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method() + ' ' + path);
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async r => {
    await r.fulfill({ status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: { nombreUsuario: 'Fixture', rolNombre: 'Plataforma',
          ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'INTERNAL_ONLY_NOT_EXPOSE', data: null }) });
  });
  return { writes, privateRequests };
}

test('C04-01.M04.06 reference is compact without horizontal overflow across four real viewports', async ({ page }) => {
  const io = await platformSession(page, 200);
  await page.goto(route);
  await expect(page.getByTestId('c0401-private')).toBeVisible();
  await expect(page.getByTestId('c0401-unapproved')).toContainText('NO_CERTIFICADO');
  await expect(page.locator('[data-testid^="c0401-section-"]')).toHaveCount(3);
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    const bounds = await page.locator('.planes').evaluate(el => ({
      viewport: document.documentElement.clientWidth,
      left: el.getBoundingClientRect().left,
      right: el.getBoundingClientRect().right,
      over: document.documentElement.scrollWidth - document.documentElement.clientWidth
    }));
    expect(bounds.left).toBeGreaterThanOrEqual(0);
    expect(bounds.right).toBeLessThanOrEqual(bounds.viewport + 2);
    expect(bounds.over).toBeLessThanOrEqual(2);
    await expect(page.getByRole('heading', { name: 'Catálogo de planes', level: 1 })).toBeVisible();
  }
  expect(io.writes).toEqual([]);
});

test('C04-01.M04.06 lazy reference paints immediately after authorized navigation without data or pricing fetch', async ({ page }) => {
  const io = await platformSession(page, 200);
  await page.goto(route);
  await expect(page.getByTestId('c0401-private')).toBeVisible();
  await expect(page.locator('[data-testid^="c0401-section-"]')).toHaveCount(3);
  await expect(page.locator('form,button,input,select,textarea,iframe')).toHaveCount(0);
  expect(io.privateRequests.filter(x => /\/plataforma\/sesion/.test(x))).toHaveLength(1);
  expect(io.privateRequests.filter(x => /\/(planes|suscripciones|upgrade|checkout|cobros)(?:\/|$)/.test(x))).toEqual([]);
  expect(io.writes).toEqual([]);
});

test('C04-01.M04.06 real Tab focus and reduced-motion do not hide the only return action', async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  const io = await platformSession(page, 200);
  await page.goto(route);
  await expect(page.getByTestId('c0401-private')).toBeVisible();
  await page.keyboard.press('Tab');
  const link = page.getByRole('link', { name: 'Volver a plataforma' });
  await expect(link).toBeFocused();
  const focus = await link.evaluate(el => {
    const css = getComputedStyle(el), r = el.getBoundingClientRect();
    return { width: r.width, height: r.height, outline: parseFloat(css.outlineWidth) };
  });
  expect(focus.width).toBeGreaterThanOrEqual(44);
  expect(focus.height).toBeGreaterThanOrEqual(44);
  expect(focus.outline).toBeGreaterThanOrEqual(3);
  expect(io.writes).toEqual([]);
});

test('C04-01.M04.06 anonymous and denied/unavailable sessions fail closed before rendering private content', async ({ context }) => {
  const anonymous = await context.newPage();
  await anonymous.goto(route);
  await expect(anonymous).toHaveURL(/\/login(?:\?|$)/);
  await expect(anonymous.getByTestId('c0401-private')).toHaveCount(0);
  await anonymous.close();
  for (const deniedStatus of [403, 503] as const) {
    const page = await context.newPage();
    const io = await platformSession(page, deniedStatus);
    await page.goto(route);
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByTestId('c0401-private')).toHaveCount(0);
    await expect(page.getByText('INTERNAL_ONLY_NOT_EXPOSE')).toHaveCount(0);
    expect(io.writes).toEqual([]);
    await page.close();
  }
});
