import { expect, test, type Page } from '@playwright/test';

// PLAN D607: real Angular + Chromium. Fixture session is synthetic, NOT signed JWT.
const routePath = '/plataforma/catalogo-planes-v22';
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

async function fixture(page: Page, status: 200 | 403 | 503 = 200) {
  const writes: string[] = [];
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'C0401_TEST_ONLY_NOT_SIGNED_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) {
      writes.push(request.method() + ' ' + new URL(request.url()).pathname);
    }
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async intercepted => {
    if (intercepted.request().method() === 'OPTIONS') {
      await intercepted.fulfill({ status: 204, headers: cors }); return;
    }
    await intercepted.fulfill({
      status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: { nombreUsuario: 'Fixture', rolNombre: 'Plataforma',
            ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, data: null, message: 'PRIVATE_ERROR_NEVER_DISPLAY' })
    });
  });
  return writes;
}

test('C04-01.M05.06 real Chromium regression: readonly plan reference has no billing controls', async ({ page }) => {
  const writes = await fixture(page);
  await page.goto(routePath);
  const panel = page.getByTestId('c0401-private');
  await expect(panel).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Catálogo de planes', level: 1 })).toBeVisible();
  for (const [id, heading] of [
    ['versiones', 'Versiones y vigencia'],
    ['capacidades', 'Capacidades y límites'],
    ['transiciones', 'Cambios y conservación']
  ]) {
    await expect(panel.getByTestId('c0401-section-' + id)
      .getByRole('heading', { name: heading, level: 3 })).toBeVisible();
  }
  await expect(panel.getByTestId('c0401-unapproved')).toContainText('NO_CERTIFICADO');
  await expect(panel.locator('form,button,input,select,textarea,iframe,[data-precio],[data-cobro]')).toHaveCount(0);
  expect(writes).toEqual([]);
});

test('C04-01.M05.06 responsive and semantics across four real Chromium viewports', async ({ page }) => {
  const writes = await fixture(page);
  await page.goto(routePath);
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await expect(page.getByTestId('c0401-private')).toBeVisible();
    const state = await page.locator('main[data-testid="c0401-planes-reference"]').evaluate(main => {
      const rect = main.getBoundingClientRect();
      return {
        overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
        width: document.documentElement.clientWidth,
        right: rect.right,
        label: main.getAttribute('aria-labelledby'),
        heading: main.querySelector('h1')?.id
      };
    });
    expect(state.overflow).toBeLessThanOrEqual(2);
    expect(state.right).toBeLessThanOrEqual(state.width + 2);
    expect(state.label).toBe('c0401-title');
    expect(state.heading).toBe(state.label);
    await expect(page.getByRole('link', { name: 'Volver a plataforma' })).toBeVisible();
  }
  expect(writes).toEqual([]);
});

test('C04-01.M05.06 keyboard Tab/Enter, reduced motion and visible focus', async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  const writes = await fixture(page);
  await page.goto(routePath);
  await expect(page.getByTestId('c0401-private')).toBeVisible();
  await page.keyboard.press('Tab'); // Never synthesize focus with element.focus().
  const back = page.getByRole('link', { name: 'Volver a plataforma' });
  await expect(back).toBeFocused();
  const focus = await back.evaluate(el => {
    const css = getComputedStyle(el), r = el.getBoundingClientRect();
    return { width: r.width, height: r.height, outline: Number.parseFloat(css.outlineWidth) };
  });
  expect(focus.width).toBeGreaterThanOrEqual(44);
  expect(focus.height).toBeGreaterThanOrEqual(44);
  expect(focus.outline).toBeGreaterThanOrEqual(3);
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/\/plataforma(?:\?|$)/);
  expect(writes).toEqual([]);
});

test('C04-01.M05.06 real Chromium navigation timing and DOM regression budget', async ({ page }) => {
  const writes = await fixture(page);
  const start = Date.now();
  await page.goto(routePath, { waitUntil: 'domcontentloaded' });
  await expect(page.getByTestId('c0401-private')).toBeVisible();
  await expect(page.locator('[data-testid^="c0401-section-"]')).toHaveCount(3);
  const visibleMs = Date.now() - start;
  const metrics = await page.evaluate(() => {
    const nav = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming | undefined;
    return {
      hasNavigationTiming: Boolean(nav),
      domContentLoaded: nav?.domContentLoadedEventEnd ?? -1,
      nodes: document.querySelectorAll('*').length
    };
  });
  expect(metrics.hasNavigationTiming).toBe(true);
  expect(metrics.domContentLoaded).toBeGreaterThan(0);
  expect(metrics.nodes).toBeLessThan(4000);
  expect(visibleMs, 'CI navigation timing only; not field CWV').toBeLessThan(15000);
  expect(writes).toEqual([]);
});

test('C04-01.M05.06 anonymous, forbidden and unavailable sessions never leak private panel', async ({ context }) => {
  const anonymous = await context.newPage();
  await anonymous.goto(routePath);
  await expect(anonymous).toHaveURL(/\/login(?:\?|$)/);
  await expect(anonymous.getByTestId('c0401-private')).toHaveCount(0);
  await anonymous.close();
  for (const status of [403, 503] as const) {
    const page = await context.newPage();
    const writes = await fixture(page, status);
    await page.goto(routePath);
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.getByTestId('c0401-private')).toHaveCount(0);
    await expect(page.getByText('PRIVATE_ERROR_NEVER_DISPLAY')).toHaveCount(0);
    expect(writes).toEqual([]);
    await page.close();
  }
});
