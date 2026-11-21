import { expect, test } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';

const fixture = 'http://localhost:5005/__test/health-scenario';
const evidenceDir = join(process.cwd(), 'v23-artifacts', 'c0601-m0406');
const widths = [320, 390, 768, 1280, 1440] as const;

test.beforeEach(async ({ request }) => {
  const response = await request.post(fixture, { data: { scenario: 'success' } });
  expect(response.status()).toBe(204);
});

test('C06-01.M04.06 public progressive density and authentic before/after across five widths', async ({ page }) => {
  await mkdir(evidenceDir, { recursive: true });
  for (const width of widths) {
    await page.setViewportSize({ width, height: 900 });
    const health: string[] = [], privateRequests: string[] = [];
    const collector = (req: import('@playwright/test').Request) => {
      const path = new URL(req.url()).pathname;
      if (path === '/health') health.push(req.url());
      if (/\/api\/|\/tenant-context|\/empresa-configuracion/.test(path)) privateRequests.push(req.url());
    };
    page.on('request', collector);
    await page.goto('/conoce-solqaryn');
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
    await expect(page.getByTestId('c0601-status-response')).toContainText('Sin consulta');
    expect(health, 'no early health fetch ' + width).toHaveLength(0);
    await expect(page.getByRole('link', { name: /Ir a mi cuenta/ })).toHaveAttribute('href', '/login');
    const before = join(evidenceDir, 'antes-idle-' + width + '.png');
    await page.screenshot({ path: before, fullPage: true });
    await test.info().attach('antes-' + width, { path: before, contentType: 'image/png' });
    await page.getByTestId('c0601-status-check').click();
    await expect(page.getByTestId('c0601-status-response')).toContainText('respondió correctamente');
    expect(health, 'one explicit public GET ' + width).toHaveLength(1);
    const after = join(evidenceDir, 'despues-ok-' + width + '.png');
    await page.screenshot({ path: after, fullPage: true });
    await test.info().attach('despues-' + width, { path: after, contentType: 'image/png' });
    const metrics = await page.evaluate(() => ({
      width: innerWidth,
      total: document.documentElement.scrollWidth,
      main: document.querySelectorAll('main').length,
      h1: document.querySelectorAll('h1').length,
      unnamed: [...document.querySelectorAll('a,button')]
        .filter(x => !(x.getAttribute('aria-label') || x.textContent || '').trim()).length,
      privateWidgets: document.querySelectorAll('iframe,form,textarea,table,[role="dialog"],[role="grid"]').length
    }));
    expect(metrics.width).toBe(width);
    expect(metrics.total, 'no document horizontal overflow ' + width).toBeLessThanOrEqual(width + 1);
    expect(metrics.main).toBe(1);
    expect(metrics.h1).toBe(1);
    expect(metrics.unnamed).toBe(0);
    expect(metrics.privateWidgets, 'no invented private panels').toBe(0);
    expect(privateRequests).toEqual([]);
    page.off('request', collector);
  }
});

test('C06-01.M04.06 delayed HTTP indicates real loading and rejects duplicate request', async ({ page, request }) => {
  expect((await request.post(fixture, { data: { scenario: 'slow' } })).status()).toBe(204);
  const health: string[] = [];
  page.on('request', req => {
    if (new URL(req.url()).pathname === '/health') health.push(req.url());
  });
  await page.goto('/conoce-solqaryn');
  expect(health).toHaveLength(0);
  const button = page.getByTestId('c0601-status-check');
  await button.click();
  await expect(button).toBeDisabled();
  await expect(page.getByTestId('c0601-status-response')).toContainText('Consultando');
  await expect(page.getByTestId('c0601-status-response')).toContainText('respondió correctamente');
  await expect(button).toBeEnabled();
  expect(health).toHaveLength(1);
});

test('C06-01.M04.06 reduced-motion real keyboard and contrast at 390 and 1440', async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  for (const width of [390, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    await page.goto('/conoce-solqaryn');
    await page.keyboard.press('Tab');
    await expect(page.getByRole('link', { name: 'Ir al contenido' })).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(page.locator('#c0601-content')).toBeFocused();
    const toggle = page.getByTestId('c0601-brand-toggle');
    for (let i = 0; i < 12; i++) {
      if (await toggle.evaluate(el => el === document.activeElement)) break;
      await page.keyboard.press('Tab');
    }
    await expect(toggle).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(page.getByTestId('c0601-public-saas-landing')).toHaveAttribute('data-brand-theme', 'contraste');
    const metrics = await page.evaluate(() => ({
      reduced: matchMedia('(prefers-reduced-motion: reduce)').matches,
      width: document.documentElement.scrollWidth, viewport: innerWidth
    }));
    expect(metrics.reduced).toBe(true);
    expect(metrics.width).toBeLessThanOrEqual(metrics.viewport + 1);
  }
});

test('C06-01.M04.06 real CDP page scale 200 percent and keyboard-only CTA', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 800 });
  await page.goto('/conoce-solqaryn');
  const cdp = await page.context().newCDPSession(page);
  try {
    await cdp.send('Emulation.setPageScaleFactor', { pageScaleFactor: 2 });
    expect(await page.evaluate(() => visualViewport?.scale ?? 1)).toBeGreaterThanOrEqual(1.8);
    const button = page.getByTestId('c0601-status-check');
    for (let i = 0; i < 20; i++) {
      if (await button.evaluate(el => el === document.activeElement)) break;
      await page.keyboard.press('Tab');
    }
    await expect(button).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(page.getByTestId('c0601-status-response')).toContainText('respondió correctamente');
  } finally {
    if (!page.isClosed()) {
      try { await cdp.send('Emulation.setPageScaleFactor', { pageScaleFactor: 1 }); } catch {}
    }
    try { await cdp.detach(); } catch {}
  }
});
