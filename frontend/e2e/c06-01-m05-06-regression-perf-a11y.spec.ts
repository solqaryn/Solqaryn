import { expect, test, type APIRequestContext } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';

// C06-01.M05.06: regression of the existing PUBLIC Angular route, never a private JWT/DB certification.
const fixture = 'http://localhost:5005/__test/health-scenario';
const widths = [320, 390, 768, 1280, 1440] as const;
const output = join(process.cwd(), 'v23-artifacts', 'c0601-m0506');

async function scenario(request: APIRequestContext, value: string): Promise<void> {
  expect((await request.post(fixture, { data: { scenario: value } })).status()).toBe(204);
}

test.beforeEach(async ({ request }) => { await scenario(request, 'success'); });

test('C06-01.M05.06 real Chromium five-width public regression, ten PNG and measured lab budget', async ({ page }) => {
  await mkdir(output, { recursive: true });
  for (const width of widths) {
    const publicGets: string[] = [];
    const privateCalls: string[] = [];
    const observe = (req: import('@playwright/test').Request) => {
      const path = new URL(req.url()).pathname;
      if (path === '/health') publicGets.push(req.method());
      if (/\/api\/|\/tenant-context|\/empresa-configuracion|\/usuarios|\/empresas\//i.test(path)) {
        privateCalls.push(req.method() + ' ' + path);
      }
    };
    page.on('request', observe);
    await page.setViewportSize({ width, height: 900 });
    await page.goto('/conoce-solqaryn', { waitUntil: 'domcontentloaded' });
    await expect(page.getByRole('heading', { level: 1 })).toContainText('Tu operación merece');
    await expect(page.getByTestId('c0601-status-response')).toContainText('Sin consulta');
    expect(publicGets, 'no automatic health request').toEqual([]);
    await expect(page.getByRole('link', { name: /Ir a mi cuenta/ })).toHaveAttribute('href', '/login');
    const before = join(output, 'antes-' + width + '.png');
    await page.screenshot({ path: before, fullPage: true });
    await test.info().attach('antes-' + width, { path: before, contentType: 'image/png' });
    await page.getByTestId('c0601-status-check').click();
    await expect(page.getByTestId('c0601-status-response')).toContainText('respondió correctamente');
    expect(publicGets, 'exactly one deliberate public health GET').toEqual(['GET']);
    const after = join(output, 'despues-' + width + '.png');
    await page.screenshot({ path: after, fullPage: true });
    await test.info().attach('despues-' + width, { path: after, contentType: 'image/png' });
    const lab = await page.evaluate(() => {
      const nav = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming | undefined;
      return {
        width: innerWidth, scrollWidth: document.documentElement.scrollWidth,
        mainCount: document.querySelectorAll('main').length,
        h1Count: document.querySelectorAll('h1').length,
        documentNodes: document.querySelectorAll('*').length,
        navDclMs: nav?.domContentLoadedEventEnd ?? -1,
        unnamedButtons: Array.from(document.querySelectorAll('button,a'))
          .filter(el => !(el.getAttribute('aria-label') || el.textContent || '').trim()).length
      };
    });
    expect(lab.width).toBe(width);
    expect(lab.scrollWidth, 'zero page horizontal overflow').toBeLessThanOrEqual(width + 1);
    expect(lab.mainCount).toBe(1);
    expect(lab.h1Count).toBe(1);
    expect(lab.unnamedButtons).toBe(0);
    expect(lab.documentNodes).toBeLessThan(4000);
    expect(lab.navDclMs, 'lab navigation observation, not field CWV').toBeGreaterThan(0);
    expect(lab.navDclMs, 'bounded CI lab navigation, not a production SLO').toBeLessThan(15000);
    expect(privateCalls).toEqual([]);
    page.off('request', observe);
  }
});

test('C06-01.M05.06 existing public errors and empty response remain recoverable and private-data safe', async ({ page, request }) => {
  for (const item of [
    { source: 'empty', expected: 'sin datos de estado' },
    { source: 'forbidden', expected: 'denegado (401/403)' },
    { source: 'missing', expected: '(404)' },
    { source: 'rate', expected: '(429)' },
    { source: 'error', expected: 'No fue posible comprobar' }
  ] as const) {
    await scenario(request, item.source);
    await page.goto('/conoce-solqaryn');
    await expect(page.getByTestId('c0601-status-response')).toContainText('Sin consulta');
    const button = page.getByTestId('c0601-status-check');
    await button.click();
    await expect(page.getByTestId('c0601-status-response')).toContainText(item.expected);
    await expect(button).toBeEnabled();
    await expect(page.getByTestId('c0601-status-response')).not.toContainText('fixture non-private');
    await expect(page.getByTestId('c0601-status-response')).not.toContainText('localhost');
  }
});

test('C06-01.M05.06 throttled HTTP prevents second action, then recovers', async ({ page, request }) => {
  await scenario(request, 'slow');
  const publicMethods: string[] = [];
  page.on('request', req => {
    if (new URL(req.url()).pathname === '/health') publicMethods.push(req.method());
  });
  await page.goto('/conoce-solqaryn');
  const button = page.getByTestId('c0601-status-check');
  await button.click();
  await expect(button).toBeDisabled();
  await expect(page.getByTestId('c0601-status-response')).toContainText('Consultando');
  await expect(page.getByTestId('c0601-status-response')).toContainText('respondió correctamente');
  await expect(button).toBeEnabled();
  expect(publicMethods).toEqual(['GET']);
});

test('C06-01.M05.06 real Tab/Enter, contrast and reduced-motion with 200-percent Chromium page-scale', async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.setViewportSize({ width: 390, height: 800 });
  await page.goto('/conoce-solqaryn');
  await page.keyboard.press('Tab');
  await expect(page.getByRole('link', { name: 'Ir al contenido' })).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(page.locator('#c0601-content')).toBeFocused();
  const toggle = page.getByTestId('c0601-brand-toggle');
  for (let i = 0; i < 16; i++) {
    if (await toggle.evaluate(el => document.activeElement === el)) break;
    await page.keyboard.press('Tab');
  }
  await expect(toggle).toBeFocused();
  const focus = await toggle.evaluate(el => {
    const style = getComputedStyle(el); const rect = el.getBoundingClientRect();
    return { outline: Number.parseFloat(style.outlineWidth), height: rect.height };
  });
  expect(focus.outline).toBeGreaterThanOrEqual(3);
  expect(focus.height).toBeGreaterThanOrEqual(44);
  await page.keyboard.press('Enter');
  await expect(toggle).toHaveAttribute('aria-pressed', 'true');
  await expect(page.getByTestId('c0601-public-saas-landing')).toHaveAttribute('data-brand-theme', 'contraste');
  const cdp = await page.context().newCDPSession(page);
  try {
    await cdp.send('Emulation.setPageScaleFactor', { pageScaleFactor: 2 });
    expect(await page.evaluate(() => visualViewport?.scale ?? 1)).toBeGreaterThanOrEqual(1.8);
    const status = page.getByTestId('c0601-status-check');
    for (let i = 0; i < 20; i++) {
      if (await status.evaluate(el => document.activeElement === el)) break;
      await page.keyboard.press('Tab');
    }
    await expect(status).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(page.getByTestId('c0601-status-response')).toContainText('respondió correctamente');
    expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(391);
    expect(await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches)).toBe(true);
  } finally {
    if (!page.isClosed()) { try { await cdp.send('Emulation.setPageScaleFactor', { pageScaleFactor: 1 }); } catch {} }
    try { await cdp.detach(); } catch {}
  }
});

test('C06-01.M05.06 untrusted public query cannot leak identity and login navigation stays real', async ({ page }) => {
  const privatePaths: string[] = [];
  page.on('request', req => {
    const path = new URL(req.url()).pathname;
    if (/\/api\/|\/tenant-context|\/empresa-configuracion|\/usuarios|\/empresas\//i.test(path)) privatePaths.push(path);
  });
  await page.goto('/conoce-solqaryn?empresaId=9999&redirect=https%3A%2F%2Fevil.invalid&token=DO_NOT_REFLECT');
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  await expect(page.locator('main')).not.toContainText('DO_NOT_REFLECT');
  await expect(page.locator('main')).not.toContainText('evil.invalid');
  expect(privatePaths).toEqual([]);
  await page.getByRole('link', { name: 'Conocer la propuesta' }).click();
  await expect(page.locator('#beneficios')).toBeFocused();
  await page.getByRole('link', { name: /Ir a mi cuenta/ }).click();
  await expect(page).toHaveURL(/\/login$/);
  await expect(page.locator('app-login')).toBeVisible();
});
