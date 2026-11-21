// V22 C14-01.M05.06: real signed JWT, ASP.NET Core, MySQL 8.4, Chromium. No HTTP mocks.
import { expect, test, type Page } from '@playwright/test';
const api = process.env['C1504_API_URL'];
const password = process.env['C1504_TEST_PASSWORD'];
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !password) {
  throw new Error('R2 requires real ASP.NET backend, ephemeral MySQL and signed JWT CI');
}
async function login(page: Page, user: string): Promise<string> {
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill(user);
  await page.locator('input[formcontrolname="password"]').fill(password!);
  const response = page.waitForResponse(r =>
    r.url().endsWith('/auth/login') && r.request().method() === 'POST');
  await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
  const loginResponse = await response;
  expect(loginResponse.status()).toBe(200);
  const token: unknown = (await loginResponse.json())?.data?.token;
  expect(typeof token).toBe('string');
  expect((token as string).split('.')).toHaveLength(3);
  return token as string;
}
async function privateProfile(page: Page, user: string, company: number, expectedName: string): Promise<void> {
  const token = await login(page, user);
  const headers = { Authorization: 'Bearer ' + token };
  const context = await page.request.get(api + '/tenant-context/' + company, { headers });
  expect(context.status()).toBe(200);
  const own = await page.request.get(api + '/empresa-configuracion/tenant/' + company, { headers });
  expect(own.status()).toBe(200);
  expect((await own.json())?.data?.nombre).toBe(expectedName);
  const cross = await page.request.get(api + '/empresa-configuracion/tenant/' + (company === 1 ? 2 : 1), { headers });
  expect([403, 404]).toContain(cross.status());
  await page.evaluate(id => localStorage.setItem('solqaryn_empresa_solicitada_id', String(id)), company);
  const verified = page.waitForResponse(r =>
    r.url().includes('/tenant-context/' + company) && r.request().method() === 'GET');
  await page.goto('/perfil');
  expect((await verified).status()).toBe(200);
  await expect(page.getByRole('heading', { name: 'Mi perfil' })).toBeVisible();
  await expect(page.locator('#main-sidebar .brand > span').first()).toHaveText(expectedName);
}
test.describe('C14-01.M05.06 real flow regression, CI timing and accessibility', () => {
  test.describe.configure({ retries: 0 });
  test('A signed JWT profile reload 320/375/768/1280 without overflow, CI render under 30 sec', async ({ page }) => {
    test.setTimeout(180_000);
    await privateProfile(page, 'c1504_admin_a', 1, 'Empresa A efimera C15');
    for (const width of [320, 375, 768, 1280]) {
      await page.setViewportSize({ width, height: 850 });
      const started = Date.now();
      const request = page.waitForResponse(r =>
        r.url().includes('/tenant-context/1') && r.request().method() === 'GET');
      await page.reload();
      expect((await request).status()).toBe(200);
      await expect(page.locator('#main-sidebar .brand > span').first()).toHaveText('Empresa A efimera C15');
      const elapsed = Date.now() - started;
      const bounds = await page.evaluate(() =>
        ({ visible: document.documentElement.clientWidth, full: document.documentElement.scrollWidth }));
      console.log('C1401_M0506_VIEWPORT=' + width + '_LOCAL_CI_RELOAD_MS=' + elapsed);
      expect(elapsed, 'CI-only budget is not public CWV SLA').toBeLessThan(30_000);
      expect(bounds.full).toBeLessThanOrEqual(bounds.visible + 2);
      await expect(page.getByText('Empresa B efimera C15')).toHaveCount(0);
    }
  });
  test('B JWT private provider remains isolated from A and shows persisted B brand', async ({ page }) => {
    test.setTimeout(180_000);
    await privateProfile(page, 'c1504_admin_b', 2, 'Empresa B efimera C15');
    await expect(page.locator('#main-sidebar .brand img.brand-logo'))
      .toHaveAttribute('src', 'https://assets.example.invalid/c14-b.svg');
    await expect(page.getByText('Empresa A efimera C15')).toHaveCount(0);
  });
  test('login accessible via natural keyboard Tab and Enter with signed JWT HTTP request', async ({ page }) => {
    test.setTimeout(180_000);
    await page.goto('/login');
    const username = page.getByRole('textbox', { name: 'Nombre de usuario' });
    const pwd = page.locator('input[formcontrolname="password"]');
    const submit = page.getByRole('button', { name: /Ingresar al sistema/ });
    await expect(username).toBeVisible();
    await expect(pwd).toBeVisible();
    await expect(submit).toBeEnabled();
    let reached = false;
    for (let i = 0; i < 80; i++) {
      await page.keyboard.press('Tab');
      if (await username.evaluate(el => el === document.activeElement)) { reached = true; break; }
    }
    expect(reached, 'username must be in real Tab order').toBe(true);
    await page.keyboard.type('c1504_admin_a');
    await pwd.fill(password!);
    let buttonReached = false;
    for (let i = 0; i < 25; i++) {
      await page.keyboard.press('Tab');
      if (await submit.evaluate(el => el === document.activeElement)) { buttonReached = true; break; }
    }
    expect(buttonReached, 'real Tab must reach submit').toBe(true);
    const focus = await submit.evaluate(el => {
      const css = getComputedStyle(el);
      return { outline: parseFloat(css.outlineWidth), shadow: css.boxShadow };
    });
    expect((Number.isFinite(focus.outline) && focus.outline >= 2) ||
      (focus.shadow !== 'none' && focus.shadow.length > 0),
      'visual focus indicator').toBe(true);
    const request = page.waitForResponse(r =>
      r.url().endsWith('/auth/login') && r.request().method() === 'POST');
    await page.keyboard.press('Enter');
    expect((await request).status()).toBe(200);
  });
  test('global platform JWT stays neutral and anonymous/foreign A to B reads denied', async ({ page }) => {
    test.setTimeout(180_000);
    const platform = await login(page, 'c1504_platform_only');
    const verified = await page.request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + platform }
    });
    expect(verified.status()).toBe(200);
    await page.goto('/plataforma/auditoria-ux-v5');
    await expect(page.locator('#main-sidebar .brand > span').first()).toHaveText('SOLQARYN');
    await expect(page.getByText('Empresa A efimera C15')).toHaveCount(0);
    await expect(page.getByText('Empresa B efimera C15')).toHaveCount(0);
    const anonymous = await page.request.get(api + '/empresa-configuracion/tenant/1');
    expect([401, 403]).toContain(anonymous.status());
    const tokenA = await login(page, 'c1504_admin_a');
    const foreign = await page.request.get(api + '/empresa-configuracion/tenant/2', {
      headers: { Authorization: 'Bearer ' + tokenA }
    });
    expect([403, 404]).toContain(foreign.status());
    expect(await foreign.text()).not.toMatch(/Empresa B efimera C15|c14-b\.svg/);
  });
});
