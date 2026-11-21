// V22 C01-02.M05.06: real JWT, ASP.NET Core 10, MySQL 8.4 and Chromium. No HTTP mocks.
import { expect, test, type Page } from '@playwright/test';
const api = process.env['C1504_API_URL'];
const password = process.env['C1504_TEST_PASSWORD'];
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !password) {
  throw new Error('R2: GitHub-hosted API + ephemeral MySQL + temporary signed JWT required');
}
const route = '/plataforma/capacidades-v19';
async function login(page: Page, username: string): Promise<string> {
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill(username);
  await page.locator('input[formcontrolname="password"]').fill(password!);
  const request = page.waitForResponse(response =>
    response.url().endsWith('/auth/login') && response.request().method() === 'POST');
  await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
  const response = await request;
  expect(response.status()).toBe(200);
  const token: unknown = (await response.json())?.data?.token;
  expect(typeof token).toBe('string');
  expect((token as string).split('.')).toHaveLength(3);
  return token as string;
}
test.describe('C01-02.M05.06 - real regression, render budget and accessibility', () => {
  test.describe.configure({ retries: 0 });
  test('platform JWT: 320/768/1280 render with bounded timing and no overflow', async ({ page }) => {
    test.setTimeout(150_000);
    const token = await login(page, 'c1504_platform_only');
    const server = await page.request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + token }
    });
    expect(server.status()).toBe(200);
    for (const width of [320, 768, 1280]) {
      await page.setViewportSize({ width, height: 850 });
      const started = Date.now();
      const live = page.waitForResponse(response =>
        response.url().includes('/plataforma/sesion') && response.request().method() === 'GET');
      await page.goto(route);
      expect((await live).status()).toBe(200);
      const main = page.locator('main.catalogo');
      await expect(page.getByRole('heading', { level: 1, name: 'Catálogo global de módulos y capacidades' })).toBeVisible();
      await expect(page.getByTestId('c0102-listado').locator('li').first()).toBeVisible();
      const renderMs = Date.now() - started;
      const bounds = await main.evaluate(element => {
        const cards = Array.from(element.querySelectorAll('li'));
        return { overflow: element.scrollWidth - element.clientWidth,
          widths: cards.map(card => card.getBoundingClientRect().width),
          max: element.getBoundingClientRect().width, count: cards.length };
      });
      console.log('C0102_M0506_WIDTH=' + width + '_RENDER_MS=' + renderMs);
      expect(renderMs, 'CI render budget, NOT public CWV SLA').toBeLessThan(30_000);
      expect(bounds.overflow).toBeLessThanOrEqual(2);
      expect(bounds.count).toBeGreaterThan(0);
      expect(bounds.count).toBeLessThan(200);
      expect(bounds.widths.every(w => w > 0 && w <= bounds.max + 2)).toBe(true);
    }
  });
  test('real platform session: semantic main and Tab/Enter without programmatic focus', async ({ page }) => {
    test.setTimeout(150_000);
    await login(page, 'c1504_platform_only');
    await page.goto(route);
    await expect(page.locator('main.catalogo')).toHaveAttribute('aria-labelledby', 'c0102-title');
    await expect(page.getByRole('heading', { level: 1 })).toHaveCount(1);
    await expect(page.getByTestId('c0102-listado').locator('li').first()).toBeVisible();
    const back = page.getByRole('link', { name: 'Volver a plataforma' });
    await expect(back).toBeVisible();
    let reached = false;
    for (let i = 0; i < 120; i++) {
      await page.keyboard.press('Tab');
      if (await back.evaluate(el => el === document.activeElement)) { reached = true; break; }
    }
    expect(reached, 'natural keyboard focus').toBe(true);
    const outline = await back.evaluate(el => {
      const style = getComputedStyle(el);
      return { kind: style.outlineStyle, width: parseFloat(style.outlineWidth) };
    });
    expect(outline.kind).not.toBe('none');
    expect(outline.width).toBeGreaterThanOrEqual(3);
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/plataforma\/?(?:\?|$)/);
  });
  for (const username of ['c1504_admin_a', 'c1504_admin_b']) {
    test(username + ': enterprise tenant cannot enumerate global catalog', async ({ page }) => {
      test.setTimeout(150_000);
      const jwt = await login(page, username);
      const server = await page.request.get(api + '/plataforma/sesion', {
        headers: { Authorization: 'Bearer ' + jwt }
      });
      expect(server.status()).toBe(403);
      const denial = page.waitForResponse(response =>
        response.url().includes('/plataforma/sesion') && response.request().method() === 'GET');
      await page.goto(route);
      expect((await denial).status()).toBe(403);
      await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
      await expect(page.getByTestId('c0102-listado')).toHaveCount(0);
    });
  }
  test('anonymous, cross-tenant header and tampered signed JWT stay denied', async ({ page }) => {
    test.setTimeout(150_000);
    const anonymous = await page.request.get(api + '/plataforma/sesion');
    expect([401, 403]).toContain(anonymous.status());
    const jwtA = await login(page, 'c1504_admin_a');
    const tenantB = await page.request.get(api + '/tenant-context/2', {
      headers: { Authorization: 'Bearer ' + jwtA, 'X-Empresa-Id': '2' }
    });
    expect([403, 404]).toContain(tenantB.status());
    const globalB = await page.request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + jwtA, 'X-Empresa-Id': '2' }
    });
    expect(globalB.status()).toBe(403);
    const valid = await login(page, 'c1504_platform_only');
    const [head, payload, signature] = valid.split('.');
    const tampered = [head, payload, (signature[0] === 'A' ? 'B' : 'A') + signature.slice(1)].join('.');
    const denied = await page.request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + tampered }
    });
    expect([401, 403]).toContain(denied.status());
  });
});
