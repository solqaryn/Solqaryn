import { expect, test, type Page } from '@playwright/test';

// C10-04.M05.06 R2: real Chromium regression/performance/a11y, synthetic session.
// Does NOT certify JWT signature, tenant DB, worker, R2 provider, backup or restore.
const privatePath = '/plataforma/continuidad-operativa-v5';
const sessionEndpoint = /\/plataforma\/sesion(?:\?|$)/;
const corsHeaders = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

async function mockPlatformSession(page: Page, response: () => number) {
  let reads = 0;
  const mutations: string[] = [];
  page.on('request', request => {
    if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method())) {
      mutations.push(request.method() + ' ' + request.url());
    }
  });
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'synthetic-m0506-not-a-signed-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(sessionEndpoint, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: corsHeaders });
      return;
    }
    reads += 1;
    const status = response();
    await route.fulfill({
      status,
      headers: corsHeaders,
      contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: {
            nombreUsuario: 'Synthetic', rolNombre: 'Admin',
            ambito: 'Plataforma', esAdministrador: true
          } }
        : { success: false, data: null, message: 'PRIVATE_RESTORE_SECRET_MUST_NOT_RENDER' })
    });
  });
  return { get reads() { return reads; }, mutations };
}

test.describe('C10-04.M05.06 browser-only regression, perf budgets and accessibility', () => {
  test.describe.configure({ retries: 0 });

  for (const width of [320, 768, 1440]) {
    test('read-only private controls remain lightweight and usable at ' + width + 'px', async ({ page }) => {
      await page.setViewportSize({ width, height: 840 });
      const io = await mockPlatformSession(page, () => 200);
      await page.goto(privatePath);
      const surface = page.locator('section.continuidad');
      await expect(surface.locator('ol.controles li')).toHaveCount(4);
      await expect(page.locator('main')).toHaveCount(1);
      await expect(surface.getByRole('heading', { level: 1 })).toBeVisible();
      await expect(surface.locator('section.aviso')).toContainText('Solo lectura');
      const metrics = await surface.evaluate(element => ({
        nodes: element.querySelectorAll('*').length,
        visible: element.getBoundingClientRect().width > 0,
        overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth
      }));
      expect(metrics.visible).toBe(true);
      expect(metrics.nodes).toBeLessThan(90);
      expect(metrics.overflow).toBeLessThanOrEqual(2);
      expect(io.mutations).toEqual([]);
    });
  }

  test('real keyboard Tab/Enter revalidates read-only session with visible focus', async ({ page }) => {
    const io = await mockPlatformSession(page, () => 200);
    await page.goto(privatePath);
    const button = page.getByRole('button', { name: 'Revalidar sesión de continuidad' });
    await expect(button).toBeVisible();
    let keyboardFocused = false;
    for (let attempt = 0; attempt < 32; attempt++) {
      await page.keyboard.press('Tab');
      if (await button.evaluate(element => element === document.activeElement)) {
        keyboardFocused = true;
        break;
      }
    }
    expect(keyboardFocused).toBe(true);
    const focus = await button.evaluate(element => getComputedStyle(element).outlineWidth);
    expect(parseFloat(focus)).toBeGreaterThanOrEqual(3);
    await page.keyboard.press('Enter');
    await expect(page.locator('section.estado')).toContainText('Sesión revalidada');
    await expect(page.locator('section.continuidad ol.controles li')).toHaveCount(4);
    expect(io.reads).toBeGreaterThan(0);
    expect(io.mutations).toEqual([]);
  });

  test('403 revocation hides private guidance and never renders provider detail', async ({ page }) => {
    let status = 200;
    const io = await mockPlatformSession(page, () => status);
    await page.goto(privatePath);
    await expect(page.locator('section.continuidad ol.controles li')).toHaveCount(4);
    status = 403;
    await page.getByRole('button', { name: 'Revalidar sesión de continuidad' }).click();
    await expect(page.locator('section.continuidad ol.controles li')).toHaveCount(0);
    await expect(page.locator('section.continuidad section.aviso')).toHaveCount(0);
    await expect(page.locator('section.estado')).toContainText('Acceso de plataforma no confirmado');
    await expect(page.getByText('PRIVATE_RESTORE_SECRET_MUST_NOT_RENDER')).toHaveCount(0);
    expect(io.mutations).toEqual([]);
  });

  test('non-auth failure fails closed with safe error and no provider text', async ({ page }) => {
    let status = 200;
    const io = await mockPlatformSession(page, () => status);
    await page.goto(privatePath);
    await expect(page.locator('section.continuidad ol.controles li')).toHaveCount(4);
    status = 500;
    await page.getByRole('button', { name: 'Revalidar sesión de continuidad' }).click();
    await expect(page.locator('section.continuidad ol.controles li')).toHaveCount(0);
    await expect(page.locator('section.estado')).toContainText('No fue posible validar');
    await expect(page.getByText('PRIVATE_RESTORE_SECRET_MUST_NOT_RENDER')).toHaveCount(0);
    expect(io.mutations).toEqual([]);
  });

  test('reduced motion keeps controls accessible without animations', async ({ page }) => {
    await page.emulateMedia({ reducedMotion: 'reduce' });
    await mockPlatformSession(page, () => 200);
    await page.goto(privatePath);
    const button = page.getByRole('button', { name: 'Revalidar sesión de continuidad' });
    await expect(button).toBeVisible();
    const durations = await button.evaluate(element => getComputedStyle(element).transitionDuration
      .split(',').map(value => {
        const match = /^(\d+(?:\.\d+)?(?:e[+-]?\d+)?)(ms|s)$/i.exec(value.trim());
        if (!match) return Number.NaN;
        return Number(match[1]) * (match[2].toLowerCase() === 'ms' ? 0.001 : 1);
      }));
    expect(durations.length).toBeGreaterThan(0);
    // Chromium/Material may clamp reduced motion to 0.01 ms instead of literal 0s.
    // A strict <= 0.1 ms budget still rejects observable motion.
    expect(durations.every(seconds => Number.isFinite(seconds) && seconds >= 0 && seconds <= 0.0001)).toBe(true);
    await expect(page.getByRole('link', { name: 'Regresar a plataforma' })).toBeVisible();
  });
});
