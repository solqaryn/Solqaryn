import { expect, test, type Page } from '@playwright/test';

/** Real Chromium + Angular; intercepted session is synthetic, NOT signed JWT/server authorization. */
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
type Mode = 'allowed' | 'forbidden' | 'unavailable';
async function session(page: Page, mode: () => Mode): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'synthetic-c1402-m0506-NOT-JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    const state = mode();
    await route.fulfill({
      status: state === 'allowed' ? 200 : state === 'forbidden' ? 403 : 503,
      headers: cors, contentType: 'application/json',
      body: JSON.stringify(state === 'allowed'
        ? { success: true, data: { nombreUsuario: 'Fixture', rolNombre: 'Admin',
            ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'PRIVATE_NEVER_EXPOSE_C1402' })
    });
  });
}

test.describe('C14-02.M05.06 regression, performance and accessibility', () => {
  test.describe.configure({ retries: 0 });
  test('4 viewports, no overflow, 44px touch targets and no writes', async ({ page }) => {
    await session(page, () => 'allowed');
    const writes: string[] = [];
    page.on('request', request => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method())) writes.push(request.method());
    });
    for (const width of [320, 375, 768, 1280]) {
      await page.setViewportSize({ width, height: 844 });
      await page.goto('/plataforma/revision-amenazas-v5');
      await expect(page.locator('.threat-list li')).toHaveCount(4);
      const result = await page.evaluate(() => ({
        overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
        targets: Array.from(document.querySelectorAll('.threat-guide button, .threat-guide header a'))
          .map(x => x.getBoundingClientRect().height),
        input: document.querySelectorAll('.threat-guide form, .threat-guide input').length
      }));
      expect(result.overflow, 'viewport ' + width).toBeLessThanOrEqual(2);
      expect(result.targets).toHaveLength(4);
      expect(result.targets.every(height => height >= 44)).toBe(true);
      expect(result.input).toBe(0);
    }
    expect(writes).toEqual([]);
  });

  test('natural Tab from start exposes focus outline and Enter refreshes session', async ({ page }) => {
    await session(page, () => 'allowed');
    await page.goto('/plataforma/revision-amenazas-v5');
    const back = page.getByRole('link', { name: 'Volver a administración de plataforma' });
    const refresh = page.getByRole('button', { name: 'Revalidar sesión de plataforma' });
    let reached = false;
    for (let n = 0; n < 60; n++) {
      await page.keyboard.press('Tab');
      if (await back.evaluate(el => document.activeElement === el)) { reached = true; break; }
    }
    expect(reached).toBe(true);
    await expect(back).toBeFocused();
    expect(await back.evaluate(el => parseFloat(getComputedStyle(el).outlineWidth))).toBeGreaterThanOrEqual(3);
    await page.keyboard.press('Tab');
    await expect(refresh).toBeFocused();
    expect(await refresh.evaluate(el => parseFloat(getComputedStyle(el).outlineWidth))).toBeGreaterThanOrEqual(3);
    await page.keyboard.press('Enter');
    await expect(page.getByText(/Sesión de plataforma confirmada/)).toBeVisible();
  });

  test('initial 403 denies surface and secret diagnostic', async ({ page }) => {
    await session(page, () => 'forbidden');
    await page.goto('/plataforma/revision-amenazas-v5');
    await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
    await expect(page.locator('.threat-list li')).toHaveCount(0);
    await expect(page.getByText('PRIVATE_NEVER_EXPOSE_C1402')).toHaveCount(0);
  });

  test('subsequent 503 fails closed without leaking private message', async ({ page }) => {
    let state: Mode = 'allowed';
    await session(page, () => state);
    await page.goto('/plataforma/revision-amenazas-v5');
    await expect(page.locator('.threat-list li')).toHaveCount(4);
    state = 'unavailable';
    await page.getByRole('button', { name: 'Revalidar sesión de plataforma' }).click();
    await expect(page.getByRole('alert')).toContainText('No se pudo comprobar la sesión');
    await expect(page.locator('.threat-list li')).toHaveCount(0);
    await expect(page.getByText('PRIVATE_NEVER_EXPOSE_C1402')).toHaveCount(0);
  });

  test('adversarial CSS overflow mutation detected then reverted; local timing logged', async ({ page }) => {
    await session(page, () => 'allowed');
    await page.setViewportSize({ width: 320, height: 844 });
    await page.goto('/plataforma/revision-amenazas-v5');
    await expect(page.locator('.threat-list li')).toHaveCount(4);
    const overflow = () => page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(await overflow()).toBeLessThanOrEqual(2);
    const mutation = await page.addStyleTag({ content: '.threat-list { grid-template-columns: 600px !important; }' });
    expect(await overflow()).toBeGreaterThan(2);
    await mutation.evaluate(el => el.remove());
    expect(await overflow()).toBeLessThanOrEqual(2);
    const timing = await page.evaluate(() => {
      const nav = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming | undefined;
      return nav ? nav.domContentLoadedEventEnd - nav.startTime : -1;
    });
    expect(timing).toBeGreaterThan(0);
    console.log('C1402_M0506_CHROMIUM_LOCAL_DOMCONTENTLOADED_MS=' + Math.round(timing));
    // No field Core Web Vitals, no physical device/SLA certification.
  });
});
