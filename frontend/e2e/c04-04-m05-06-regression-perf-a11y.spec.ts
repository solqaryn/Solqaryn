import { expect, test, type Page } from '@playwright/test';

// C04-04.M05.06: real Angular/Chromium, synthetic platform session only.
// No signed JWT, tenant MySQL, physical phone, PSP or field Web Vitals certification.
const privatePath = '/plataforma/enlaces-cobro-simulacion-v22';
const headers = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

async function fixtureSession(page: Page, allow: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'C0404_M0506_SYNTHETIC_NOT_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers });
      return;
    }
    await route.fulfill({
      status: allow ? 200 : 403,
      headers,
      contentType: 'application/json',
      body: JSON.stringify(allow
        ? { success: true, data: {
            nombreUsuario: 'Regresion', rolNombre: 'Plataforma',
            ambito: 'Plataforma', esAdministrador: true
          } }
        : { success: false, message: 'PRIVATE_PAYMENT_PROVIDER_SECRET' })
    });
  });
}

test.describe('C04-04.M05.06 regression, performance and a11y proportional', () => {
  test.describe.configure({ retries: 0 });

  test('anonymous never receives the private payment preview', async ({ page }) => {
    const writes: string[] = [];
    page.on('request', request => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method())) {
        writes.push(request.method());
      }
    });
    await page.goto(privatePath);
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByTestId('c0404-payment-private')).toHaveCount(0);
    expect(writes).toEqual([]);
  });

  test('403 fails closed on mobile and desktop without provider data or writes', async ({ page }) => {
    await fixtureSession(page, false);
    const writes: string[] = [];
    page.on('request', request => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method())) {
        writes.push(request.method());
      }
    });
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 800 });
      await page.goto(privatePath);
      await expect(page).toHaveURL(/\/login/);
      await expect(page.getByTestId('c0404-payment-private')).toHaveCount(0);
      await expect(page.getByText('PRIVATE_PAYMENT_PROVIDER_SECRET')).toHaveCount(0);
    }
    expect(writes).toEqual([]);
  });

  test('real Chromium responsive and 200 percent zoom remain read-only', async ({ page }) => {
    await fixtureSession(page, true);
    const writes: string[] = [];
    page.on('request', request => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method())) {
        writes.push(request.method());
      }
    });
    await page.goto(privatePath);
    const panel = page.getByTestId('c0404-payment-private');
    await expect(panel).toBeVisible();
    await expect(panel.locator('[data-testid^="c0404-step-"]')).toHaveCount(3);
    await expect(panel.getByText(/SIN_CONEXION/)).toHaveCount(4);
    await expect(page.getByRole('heading', { name: 'Enlaces de cobro', level: 1 })).toHaveCount(1);
    await page.evaluate(() => { document.documentElement.style.fontSize = '200%'; });
    for (const width of [320, 375, 768, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const metric = await panel.evaluate(element => ({
        nodes: element.querySelectorAll('*').length,
        overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth
      }));
      expect(metric.overflow, 'horizontal overflow at ' + width).toBeLessThanOrEqual(2);
      expect(metric.nodes, 'bounded DOM at ' + width).toBeLessThan(160);
      await expect(page.getByRole('link', { name: 'Volver a plataforma' })).toBeVisible();
    }
    await expect(panel.locator('form,input,button,select,textarea,iframe')).toHaveCount(0);
    const localMetrics = await page.evaluate(() => {
      const navigation = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming | undefined;
      const paint = performance.getEntriesByName('first-contentful-paint')[0];
      return { domContentLoadedMs: navigation?.domContentLoadedEventEnd ?? null,
        firstContentfulPaintMs: paint?.startTime ?? null };
    });
    console.log('C0404_M0506_LOCAL_CHROMIUM_METRICS=' + JSON.stringify(localMetrics));
    expect(writes).toEqual([]);
  });

  test('real Tab key reaches visible 44px focus target and reduced motion is usable', async ({ page }) => {
    await fixtureSession(page, true);
    await page.emulateMedia({ reducedMotion: 'reduce' });
    const writes: string[] = [];
    page.on('request', request => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method())) {
        writes.push(request.method());
      }
    });
    await page.goto(privatePath);
    await expect(page.getByTestId('c0404-payment-private')).toBeVisible();
    await page.keyboard.press('Tab');
    const target = page.getByRole('link', { name: 'Volver a plataforma' });
    await expect(target).toBeFocused();
    const focus = await target.evaluate(element => {
      const style = getComputedStyle(element);
      const box = element.getBoundingClientRect();
      return { outlineWidth: parseFloat(style.outlineWidth), outlineStyle: style.outlineStyle,
        width: box.width, height: box.height };
    });
    expect(focus.outlineWidth).toBeGreaterThanOrEqual(3);
    expect(focus.outlineStyle).not.toBe('none');
    expect(focus.width).toBeGreaterThanOrEqual(44);
    expect(focus.height).toBeGreaterThanOrEqual(44);
    expect(writes).toEqual([]);
  });
});
