import { expect, test } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';

const fixture = 'http://localhost:5005/__test/health-scenario';
const evidenceDir = join(process.cwd(), 'v23-artifacts', 'c0601-m0402');
const setScenario = async (request: import('@playwright/test').APIRequestContext, scenario: string) => {
  const response = await request.post(fixture, { data: { scenario } });
  expect(response.status()).toBe(204);
};
test.beforeEach(async ({ request }) => { await setScenario(request, 'success'); });

test('C06-01.M04.02 only requests public /health after explicit consent', async ({ page }) => {
  const requests: { method: string; url: string; auth: string | undefined; cookie: string | undefined; tenant: string | undefined }[] = [];
  page.on('request', req => {
    if (req.url().includes(':5005/')) {
      const h = req.headers();
      requests.push({ method: req.method(), url: req.url(), auth: h['authorization'],
        cookie: h['cookie'], tenant: h['x-empresa-id'] });
    }
  });
  await page.goto('/conoce-solqaryn?tenant=other&token=evil');
  const button = page.getByTestId('c0601-status-check');
  await expect(page.getByTestId('c0601-status-response')).toContainText('Sin consulta');
  expect(requests).toHaveLength(0);
  await button.click();
  await expect(page.getByTestId('c0601-status-response')).toContainText('respondió correctamente');
  expect(requests).toHaveLength(1);
  expect(requests[0]!.method).toBe('GET');
  expect(new URL(requests[0]!.url).pathname).toBe('/health');
  expect(requests[0]!.auth).toBeUndefined();
  expect(requests[0]!.cookie).toBeUndefined();
  expect(requests[0]!.tenant).toBeUndefined();
});

test('C06-01.M04.02 real HTTP 200/204/403/404/429/500 and recovery', async ({ page, request }) => {
  await page.goto('/conoce-solqaryn');
  const button = page.getByTestId('c0601-status-check');
  const response = page.getByTestId('c0601-status-response');
  const cases = [
    ['empty', 'sin datos de estado'],
    ['forbidden', 'denegado (401/403)'],
    ['missing', 'dirección (404)'],
    ['rate', 'solicitudes (429)'],
    ['error', 'No fue posible'],
    ['success', 'respondió correctamente']
  ] as const;
  for (const [scenario, expected] of cases) {
    await setScenario(request, scenario);
    await button.click();
    await expect(response).toContainText(expected);
    await expect(button).toBeEnabled();
  }
  await expect(response).toHaveAttribute('aria-live', 'polite');
});

test('C06-01.M04.02 loading and no duplicate request while HTTP is pending', async ({ page, request }) => {
  await setScenario(request, 'slow');
  const requests: string[] = [];
  page.on('request', req => {
    if (new URL(req.url()).pathname === '/health') requests.push(req.url());
  });
  await page.goto('/conoce-solqaryn');
  const button = page.getByTestId('c0601-status-check');
  await button.click();
  await expect(button).toBeDisabled();
  await expect(page.getByTestId('c0601-status-response')).toContainText('Consultando');
  await expect(page.getByTestId('c0601-status-response')).toContainText('respondió correctamente');
  expect(requests).toHaveLength(1);
});

test('C06-01.M04.02 Chromium before/after 320,390,768,1280,1440 no overflow', async ({ page }) => {
  await mkdir(evidenceDir, { recursive: true });
  for (const width of [320, 390, 768, 1280, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    await page.goto('/conoce-solqaryn');
    const before = join(evidenceDir, 'antes-sin-consulta-' + width + '.png');
    await page.screenshot({ path: before, fullPage: true });
    await test.info().attach('antes-' + width, { path: before, contentType: 'image/png' });
    await page.getByTestId('c0601-status-check').click();
    await expect(page.getByTestId('c0601-status-response')).toContainText('respondió correctamente');
    const after = join(evidenceDir, 'despues-salud-ok-' + width + '.png');
    await page.screenshot({ path: after, fullPage: true });
    await test.info().attach('despues-' + width, { path: after, contentType: 'image/png' });
    const size = await page.evaluate(() => ({ scroll: document.documentElement.scrollWidth, viewport: innerWidth }));
    expect(size.viewport).toBe(width);
    expect(size.scroll, 'body horizontal overflow at width ' + width).toBeLessThanOrEqual(width + 1);
  }
});

test('C06-01.M04.02 keyboard-only 200 percent real Chromium visual scale', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 800 });
  await page.goto('/conoce-solqaryn');
  const cdp = await page.context().newCDPSession(page);
  try {
    await cdp.send('Emulation.setPageScaleFactor', { pageScaleFactor: 2 });
    const scale = await page.evaluate(() => window.visualViewport?.scale ?? 1);
    expect(scale).toBeGreaterThanOrEqual(1.8);
    const button = page.getByTestId('c0601-status-check');
    for (let n = 0; n < 16; n++) {
      if (await button.evaluate(element => element === document.activeElement)) break;
      await page.keyboard.press('Tab');
    }
    await expect(button).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(page.getByTestId('c0601-status-response')).toContainText('respondió correctamente');
  } finally {
    try { if (!page.isClosed()) await cdp.send('Emulation.setPageScaleFactor', { pageScaleFactor: 1 }); } catch {}
    try { await cdp.detach(); } catch {}
  }
});

test('C06-01.M04.02 closing heading baseline line-height A5 P2 regression at five widths and both palettes', async ({ page }) => {
  for (const width of [320, 390, 768, 1280, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    await page.goto('/conoce-solqaryn');
    for (const highContrast of [false, true]) {
      if (highContrast) await page.getByTestId('c0601-brand-toggle').click();
      const metrics = await page.locator('#scope-heading').evaluate(element => {
        const style = getComputedStyle(element);
        const paragraph = element.nextElementSibling as HTMLElement | null;
        return {
          fontSize: Number.parseFloat(style.fontSize),
          lineHeight: Number.parseFloat(style.lineHeight),
          headingBottom: element.getBoundingClientRect().bottom,
          paragraphTop: paragraph?.getBoundingClientRect().top ?? 0,
          documentWidth: document.documentElement.scrollWidth,
          viewportWidth: window.innerWidth
        };
      });
      expect(Number.isFinite(metrics.lineHeight), 'explicit line-height for closing h2').toBeTruthy();
      expect(metrics.lineHeight, 'no line collision at ' + width).toBeGreaterThanOrEqual(metrics.fontSize * 1.15);
      expect(metrics.paragraphTop, 'paragraph below heading at ' + width).toBeGreaterThanOrEqual(metrics.headingBottom - 1);
      expect(metrics.documentWidth, 'no horizontal overflow at ' + width).toBeLessThanOrEqual(metrics.viewportWidth + 1);
    }
  }
});
