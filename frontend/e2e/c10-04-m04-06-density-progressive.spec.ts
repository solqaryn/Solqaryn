import { expect, test, type Page } from '@playwright/test';

// C10-04.M04.06. Chromium browser is real, session/API are synthetic.
// This is NOT verification of signed JWT, tenant A/B, database, worker or restore.
const path = '/plataforma/continuidad-operativa-v5';
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function withSession(page: Page, status: () => number, gate?: () => Promise<void>) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'synthetic-m0406-not-real-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(sessionUrl, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    if (gate) await gate();
    const code = status();
    await route.fulfill({
      status: code, headers: cors, contentType: 'application/json',
      body: JSON.stringify(code === 200
        ? { success: true, data: { nombreUsuario: 'Synthetic', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, data: null, message: 'PRIVATE_WORKER_SECRET_NOT_RENDERED' })
    });
  });
}

test.describe('C10-04.M04.06 density and progressive load', () => {
  test.describe.configure({ retries: 0 });
  for (const { width, columns } of [
    { width: 320, columns: 1 }, { width: 768, columns: 2 }, { width: 1440, columns: 2 }
  ]) {
    test('responsive ' + width + 'px no overflow', async ({ page }) => {
      await page.setViewportSize({ width, height: 800 });
      await withSession(page, () => 200);
      await page.goto(path);
      const surface = page.locator('section.continuidad');
      const cards = surface.locator('ol.controles li');
      await expect(cards).toHaveCount(4);
      await expect(page.locator('main')).toHaveCount(1);
      await expect(surface).toHaveAttribute('data-densidad', 'normal');
      const observed = await surface.locator('ol.controles').evaluate(element => {
        const s = getComputedStyle(element);
        return { columns: s.gridTemplateColumns.split(' ').filter(Boolean).length,
          width: element.getBoundingClientRect().width };
      });
      expect(observed.columns).toBe(columns);
      expect(observed.width).toBeGreaterThan(0);
      await expect(cards.first()).toBeVisible();
      await expect(cards.last()).toBeVisible();
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
      expect(overflow).toBeLessThanOrEqual(2);
    });
  }

  test('slow response hides cards, marks busy and prevents overlapping retry', async ({ page }) => {
    let hold = false;
    let release: (() => void) | undefined;
    const pending = new Promise<void>(resolve => { release = resolve; });
    await withSession(page, () => 200, async () => { if (hold) await pending; });
    await page.goto(path);
    const surface = page.locator('section.continuidad');
    await expect(surface.locator('ol.controles li')).toHaveCount(4);
    hold = true;
    const button = page.getByRole('button', { name: 'Revalidar sesión de continuidad' });
    await button.click();
    await expect(surface.locator('section.estado')).toHaveAttribute('aria-busy', 'true');
    await expect(button).toBeDisabled();
    await expect(surface.locator('ol.controles li')).toHaveCount(0);
    release?.();
    await expect(surface.locator('section.estado')).toHaveAttribute('aria-busy', 'false');
    await expect(surface.locator('ol.controles li')).toHaveCount(4);
    await expect(button).toBeEnabled();
  });

  test('403 on retry hides private cards and does not leak provider text', async ({ page }) => {
    let status = 200;
    await withSession(page, () => status);
    await page.goto(path);
    await expect(page.locator('section.continuidad ol.controles li')).toHaveCount(4);
    status = 403;
    await page.getByRole('button', { name: 'Revalidar sesión de continuidad' }).click();
    await expect(page.locator('section.continuidad ol.controles li')).toHaveCount(0);
    await expect(page.locator('section.continuidad section.aviso')).toHaveCount(0);
    await expect(page.getByText('PRIVATE_WORKER_SECRET_NOT_RENDERED')).toHaveCount(0);
  });
});
