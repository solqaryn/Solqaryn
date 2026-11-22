import { expect, test, type Page } from '@playwright/test';

// Real running .NET 10 backend with signed JWT and ephemeral MySQL 8.4.
// Never substitute synthetic HTTP fixtures for these authorization assertions.
const api = process.env['C1504_API_URL'];
const password = process.env['C1504_TEST_PASSWORD'];
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !password) {
  throw new Error('C01-03.M04.01 requires GitHub-hosted JWT/MySQL/Chromium fixture.');
}
const target = '/configuracion/dinamica-v5';

async function login(page: Page, username: string): Promise<string> {
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill(username);
  await page.locator('input[formcontrolname="password"]').fill(password!);
  const pending = page.waitForResponse(r => r.url().endsWith('/auth/login') && r.request().method() === 'POST');
  await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
  const response = await pending;
  expect(response.status()).toBe(200);
  const token: unknown = (await response.json())?.data?.token;
  expect(typeof token).toBe('string');
  expect((token as string).split('.')).toHaveLength(3);
  return token as string;
}

test.describe('V24 C01-03.M04.01 private configuration tenant A/B', () => {
  test.describe.configure({ retries: 0 });

  test('anonymous visitor cannot access private configuration', async ({ page }) => {
    await page.goto(target);
    await expect(page).toHaveURL(/login/);
    await expect(page.getByRole('heading', { name: 'Configuración dinámica' })).toHaveCount(0);
  });

  for (const [username, tenant] of [['c1504_admin_a', 1], ['c1504_admin_b', 2]] as const) {
    test(username + ' sees scoped configuration with accessible viewport', async ({ page }) => {
      test.setTimeout(180_000);
      const jwt = await login(page, username);
      const membership = await page.request.get(api + '/tenant-context/' + tenant, {
        headers: { Authorization: 'Bearer ' + jwt }
      });
      expect(membership.status()).toBe(200);
      await page.evaluate(id => localStorage.setItem('solqaryn_empresa_solicitada_id', String(id)), tenant);
      await page.goto(target);
      await expect(page.getByRole('heading', { name: 'Configuración dinámica' })).toBeVisible();
      await expect(page.locator('app-empresa-configuracion-tenant-card')).toBeVisible();
      await expect(page.getByText('Empresa #' + tenant, { exact: false })).toBeVisible();
      await expect(page.getByRole('link', { name: 'Ver todos los ajustes' })).toHaveAttribute('href', '/configuracion');
      // Chromium compositor pinch zoom is native CDP, not CSS zoom nor desktop text zoom.
      // It measures zoomed viewport behavior but does not certify desktop zoom at 200%.
      const cdp = await page.context().newCDPSession(page);
      try {
        for (const width of [320, 390, 768, 1280, 1440]) {
          await page.setViewportSize({ width, height: 900 });
          await cdp.send('Emulation.setPageScaleFactor', { pageScaleFactor: 1 });
          const at100 = await page.evaluate(() => ({
            scale: window.visualViewport?.scale ?? 0,
            scrollWidth: document.documentElement.scrollWidth,
            clientWidth: document.documentElement.clientWidth
          }));
          expect(at100.scale, 'Chromium zoom baseline must be 100%').toBeCloseTo(1, 1);
          expect(at100.scrollWidth, 'Horizontal body overflow at ' + width).toBeLessThanOrEqual(at100.clientWidth + 2);
          await page.screenshot({ path: 'test-results/c0103-m0401-' + username + '-' + width + '-zoom100.png' });

          await cdp.send('Emulation.setPageScaleFactor', { pageScaleFactor: 2 });
          const at200 = await page.evaluate(() => ({
            scale: window.visualViewport?.scale ?? 0,
            visualWidth: window.visualViewport?.width ?? 0,
            layoutWidth: document.documentElement.clientWidth,
            scrollWidth: document.documentElement.scrollWidth
          }));
          expect(at200.scale, 'Chromium pinch zoom must actually reach 200%').toBeGreaterThanOrEqual(1.99);
          expect(at200.visualWidth, 'Zoom should shrink the visual viewport').toBeLessThan(at200.layoutWidth * 0.55);
          expect(at200.scrollWidth, 'No document-level overflow from pinch zoom').toBeLessThanOrEqual(at200.layoutWidth + 2);
          await expect(page.getByRole('heading', { name: 'Configuración dinámica' })).toBeVisible();
          await page.screenshot({ path: 'test-results/c0103-m0401-' + username + '-' + width + '-zoom200-pinch.png' });
        }
      } finally {
        await cdp.send('Emulation.setPageScaleFactor', { pageScaleFactor: 1 });
        await cdp.detach();
      }

      // Real keyboard traversal must reach the intended action, not merely leave BODY.
      const settingsLink = page.getByRole('link', { name: 'Ver todos los ajustes' });
      let focusedByKeyboard = false;
      for (let press = 0; press < 80; press++) {
        await page.keyboard.press('Tab');
        if (await settingsLink.evaluate(el => document.activeElement === el)) {
          focusedByKeyboard = true;
          break;
        }
      }
      expect(focusedByKeyboard, 'Tab must reach the actual settings link without .focus()').toBe(true);
    });
  }

  test('JWT company A cannot request company B with forged client headers', async ({ page }) => {
    const jwt = await login(page, 'c1504_admin_a');
    const foreign = await page.request.get(api + '/tenant-context/2', {
      headers: { Authorization: 'Bearer ' + jwt, 'X-Empresa-Id': '2' }
    });
    expect([403,404]).toContain(foreign.status());
    await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '2'));
    await page.goto(target);
    await expect(page.getByRole('heading', { name: 'Configuración dinámica' })).toHaveCount(0);
  });

  test('tampered JWT signature rejected by real API', async ({ page }) => {
    const jwt = await login(page, 'c1504_admin_a');
    const [header, payload, signature] = jwt.split('.');
    const altered = [header,payload,(signature[0] === 'A' ? 'B' : 'A') + signature.slice(1)].join('.');
    const result = await page.request.get(api + '/tenant-context/1', {
      headers: { Authorization:'Bearer ' + altered }
    });
    expect(result.status()).toBe(401);
  });
});
