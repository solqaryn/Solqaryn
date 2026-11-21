import { expect, test } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';

const fixture = 'http://localhost:5005/__test/health-scenario';
const artifactDir = join(process.cwd(), 'v23-artifacts', 'c0601-m0501');
const widths = [320, 390, 768, 1280, 1440] as const;

async function setScenario(request: import('@playwright/test').APIRequestContext, scenario: string) {
  const response = await request.post(fixture, { data: { scenario } });
  expect(response.status(), 'fixture scenario ' + scenario).toBe(204);
}

test.beforeEach(async ({ request }) => {
  await setScenario(request, 'success');
});

test('C06-01.M05.01 real Angular public health rule matrix: 200 204 403 404 429 500', async ({ page, request }) => {
  const health: import('@playwright/test').Request[] = [];
  const privateCalls: string[] = [];
  page.on('request', req => {
    const pathname = new URL(req.url()).pathname;
    if (pathname === '/health') health.push(req);
    if (/\/api\/|\/tenant-context|\/empresa-configuracion/.test(pathname)) privateCalls.push(req.url());
  });
  const cases = [
    { scenario: 'success', text: 'respondió correctamente' },
    { scenario: 'empty', text: 'respondió sin datos' },
    { scenario: 'forbidden', text: 'denegado (401/403)' },
    { scenario: 'missing', text: '(404)' },
    { scenario: 'rate', text: '(429)' },
    { scenario: 'error', text: 'No fue posible comprobar' }
  ] as const;
  for (const item of cases) {
    await setScenario(request, item.scenario);
    await page.goto('/conoce-solqaryn');
    const status = page.getByTestId('c0601-status-response');
    const button = page.getByTestId('c0601-status-check');
    await expect(status).toHaveAttribute('role', 'status');
    await expect(status).toHaveAttribute('aria-live', 'polite');
    await expect(status).toContainText('Sin consulta');
    const before = health.length;
    await button.click();
    await expect(status).toContainText(item.text);
    await expect(button).toBeEnabled();
    expect(health.length, item.scenario + ' requires exactly one intentional public HTTP GET').toBe(before + 1);
  }
  for (const req of health) {
    expect(req.method()).toBe('GET');
    const headers = await req.allHeaders();
    expect(headers['authorization']).toBeUndefined();
    expect(headers['cookie']).toBeUndefined();
    expect(headers['x-empresa-id']).toBeUndefined();
    expect(headers['x-storefront-session']).toBeUndefined();
  }
  expect(privateCalls).toEqual([]);
});

test('C06-01.M05.01 rejects bad scenario, retains safe error text, and recovers to success', async ({ page, request }) => {
  const invalid = await request.post(fixture, { data: { scenario: 'production-secret' } });
  expect(invalid.status()).toBe(400);
  await setScenario(request, 'error');
  await page.goto('/conoce-solqaryn');
  await page.getByTestId('c0601-status-check').click();
  const status = page.getByTestId('c0601-status-response');
  await expect(status).toContainText('No fue posible comprobar');
  await expect(status).not.toContainText('fixture non-private');
  await expect(status).not.toContainText('localhost');
  await setScenario(request, 'success');
  await page.getByTestId('c0601-status-check').click();
  await expect(status).toContainText('respondió correctamente');
});

test('C06-01.M05.01 slow public HTTP disables duplicates and returns to usable state', async ({ page, request }) => {
  await setScenario(request, 'slow');
  const health: string[] = [];
  page.on('request', req => {
    if (new URL(req.url()).pathname === '/health') health.push(req.url());
  });
  await page.goto('/conoce-solqaryn');
  expect(health).toHaveLength(0);
  const button = page.getByTestId('c0601-status-check');
  const status = page.getByTestId('c0601-status-response');
  await button.click();
  await expect(button).toBeDisabled();
  await expect(status).toContainText('Consultando');
  await expect(status).toContainText('respondió correctamente');
  await expect(button).toBeEnabled();
  expect(health).toHaveLength(1);
});

test('C06-01.M05.01 real browser five widths ten screenshots before/after and no body overflow', async ({ page }) => {
  await mkdir(artifactDir, { recursive: true });
  for (const width of widths) {
    await page.setViewportSize({ width, height: 900 });
    await page.goto('/conoce-solqaryn');
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
    await expect(page.getByTestId('c0601-status-response')).toContainText('Sin consulta');
    const before = join(artifactDir, 'antes-idle-' + width + '.png');
    await page.screenshot({ path: before, fullPage: true });
    await test.info().attach('antes-' + width, { path: before, contentType: 'image/png' });
    await page.getByTestId('c0601-status-check').click();
    await expect(page.getByTestId('c0601-status-response')).toContainText('respondió correctamente');
    const after = join(artifactDir, 'despues-ok-' + width + '.png');
    await page.screenshot({ path: after, fullPage: true });
    await test.info().attach('despues-' + width, { path: after, contentType: 'image/png' });
    const audit = await page.evaluate(() => ({
      viewport: window.innerWidth,
      document: document.documentElement.scrollWidth,
      main: document.querySelectorAll('main').length,
      h1: document.querySelectorAll('h1').length,
      unnamed: [...document.querySelectorAll('button,a')]
        .filter(el => !(el.getAttribute('aria-label') || el.textContent || '').trim()).length
    }));
    expect(audit.viewport).toBe(width);
    expect(audit.document).toBeLessThanOrEqual(width + 1);
    expect(audit.main).toBe(1);
    expect(audit.h1).toBe(1);
    expect(audit.unnamed).toBe(0);
    await expect(page.getByRole('link', { name: /Ir a mi cuenta/ })).toHaveAttribute('href', '/login');
  }
});

test('C06-01.M05.01 keyboard, reduced motion, high contrast and physical Chromium 200% scale', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 800 });
  await page.emulateMedia({ reducedMotion: 'reduce' });
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
  expect(await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches)).toBe(true);
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
