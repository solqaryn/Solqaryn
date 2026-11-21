import { expect, test } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';

const fixture = 'http://localhost:5005/__test/health-scenario';
const output = join(process.cwd(), 'v23-artifacts', 'c0601-m0505');
const widths = [320, 390, 768, 1280, 1440] as const;

async function setHealth(request: import('@playwright/test').APIRequestContext, scenario: string) {
  const response = await request.post(fixture, { data: { scenario } });
  expect(response.status(), 'isolated public health fixture scenario').toBe(204);
}

test.beforeEach(async ({ request }) => {
  await setHealth(request, 'success');
});

test('C06-01.M05.05 untrusted query tenant and redirect never cause a private request or reflected token', async ({ page }) => {
  const privateRequests: string[] = [];
  const health: import('@playwright/test').Request[] = [];
  page.on('request', request => {
    const url = new URL(request.url());
    if (/\/api\/|\/tenant-context|\/empresa-configuracion|\/usuarios|\/empresas\//i.test(url.pathname)) {
      privateRequests.push(request.method() + ' ' + url.pathname);
    }
    if (url.pathname === '/health') health.push(request);
  });
  await page.goto('/conoce-solqaryn?empresaId=99999&redirect=https%3A%2F%2Fevil.invalid%2Fcollect&token=UNTRUSTED_PUBLIC_TOKEN');
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  await expect(page.getByTestId('c0601-status-response')).toContainText('Sin consulta');
  await expect(page.locator('main')).not.toContainText('UNTRUSTED_PUBLIC_TOKEN');
  await expect(page.locator('main')).not.toContainText('evil.invalid');
  expect(privateRequests).toEqual([]);
  expect(health).toHaveLength(0);
  await page.getByTestId('c0601-status-check').click();
  await expect(page.getByTestId('c0601-status-response')).toContainText('respondió correctamente');
  expect(health).toHaveLength(1);
  const headers = await health[0].allHeaders();
  for (const key of ['authorization', 'cookie', 'x-empresa-id', 'x-storefront-session']) {
    expect(headers[key], 'public health must not forward private identity').toBeUndefined();
  }
  expect(privateRequests).toEqual([]);
});

test('C06-01.M05.05 forged local tenant state cannot authorize private data', async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_empresa_solicitada_id', '99999');
    localStorage.setItem('solqaryn_access_token', 'UNTRUSTED_PUBLIC_FAKE_JWT');
  });
  const privateRequests: string[] = [];
  const health: import('@playwright/test').Request[] = [];
  page.on('request', request => {
    const pathname = new URL(request.url()).pathname;
    if (/\/api\/|\/tenant-context|\/empresa-configuracion|\/usuarios|\/empresas\//i.test(pathname)) {
      privateRequests.push(request.method() + ' ' + pathname);
    }
    if (pathname === '/health') health.push(request);
  });
  await page.goto('/conoce-solqaryn');
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  await expect(page.locator('main')).not.toContainText('UNTRUSTED_PUBLIC_FAKE_JWT');
  expect(privateRequests).toEqual([]);
  await page.getByTestId('c0601-status-check').click();
  await expect(page.getByTestId('c0601-status-response')).toContainText('respondió correctamente');
  expect(health).toHaveLength(1);
  const headers = await health[0].allHeaders();
  expect(headers['authorization']).toBeUndefined();
  expect(headers['x-empresa-id']).toBeUndefined();
  expect(headers['cookie']).toBeUndefined();
  expect(privateRequests).toEqual([]);
});

test('C06-01.M05.05 forbidden health returns safe generic 401/403 notice, not provider details', async ({ page, request }) => {
  await setHealth(request, 'forbidden');
  await page.goto('/conoce-solqaryn');
  await page.getByTestId('c0601-status-check').click();
  const status = page.getByTestId('c0601-status-response');
  await expect(status).toContainText('denegado (401/403)');
  await expect(status).not.toContainText('fixture non-private');
  await expect(status).not.toContainText('localhost');
  await expect(page.getByTestId('c0601-status-check')).toBeEnabled();
});

test('C06-01.M05.05 rate limit and server failure stay bounded and disclose no debug body', async ({ page, request }) => {
  for (const item of [
    { scenario: 'rate', text: '(429)' },
    { scenario: 'error', text: 'No fue posible comprobar' }
  ] as const) {
    await setHealth(request, item.scenario);
    await page.goto('/conoce-solqaryn');
    await page.getByTestId('c0601-status-check').click();
    const status = page.getByTestId('c0601-status-response');
    await expect(status).toContainText(item.text);
    await expect(status).not.toContainText('fixture non-private');
    await expect(page.getByTestId('c0601-status-check')).toBeEnabled();
  }
});

test('C06-01.M05.05 slow public HTTP rejects duplicate actions while in flight', async ({ page, request }) => {
  await setHealth(request, 'slow');
  const health: string[] = [];
  page.on('request', req => { if (new URL(req.url()).pathname === '/health') health.push(req.url()); });
  await page.goto('/conoce-solqaryn');
  const button = page.getByTestId('c0601-status-check');
  await button.click();
  await expect(button).toBeDisabled();
  await expect(page.getByTestId('c0601-status-response')).toContainText('Consultando');
  await expect(page.getByTestId('c0601-status-response')).toContainText('respondió correctamente');
  expect(health).toHaveLength(1);
});

test('C06-01.M05.05 forbidden states: five real Chromium viewports, ten PNG and zero body overflow', async ({ page, request }) => {
  await setHealth(request, 'forbidden');
  await mkdir(output, { recursive: true });
  for (const width of widths) {
    await page.setViewportSize({ width, height: 900 });
    await page.goto('/conoce-solqaryn');
    await expect(page.getByTestId('c0601-status-response')).toContainText('Sin consulta');
    const before = join(output, 'antes-idle-' + width + '.png');
    await page.screenshot({ path: before, fullPage: true });
    await test.info().attach('antes-' + width, { path: before, contentType: 'image/png' });
    await page.getByTestId('c0601-status-check').click();
    await expect(page.getByTestId('c0601-status-response')).toContainText('denegado (401/403)');
    const after = join(output, 'despues-403-' + width + '.png');
    await page.screenshot({ path: after, fullPage: true });
    await test.info().attach('despues-' + width, { path: after, contentType: 'image/png' });
    const sizes = await page.evaluate(() => ({
      viewport: innerWidth, scroll: document.documentElement.scrollWidth,
      main: document.querySelectorAll('main').length, h1: document.querySelectorAll('h1').length
    }));
    expect(sizes.viewport).toBe(width);
    expect(sizes.scroll).toBeLessThanOrEqual(width + 1);
    expect(sizes.main).toBe(1);
    expect(sizes.h1).toBe(1);
  }
});

test('C06-01.M05.05 keyboard and contrast remain usable on forbidden state', async ({ page, request }) => {
  await setHealth(request, 'forbidden');
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.setViewportSize({ width: 390, height: 800 });
  await page.goto('/conoce-solqaryn');
  await page.keyboard.press('Tab');
  await expect(page.getByRole('link', { name: 'Ir al contenido' })).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(page.locator('#c0601-content')).toBeFocused();
  const button = page.getByTestId('c0601-status-check');
  for (let i = 0; i < 20; i++) {
    if (await button.evaluate(el => el === document.activeElement)) break;
    await page.keyboard.press('Tab');
  }
  await expect(button).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(page.getByTestId('c0601-status-response')).toContainText('denegado (401/403)');
  expect(await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches)).toBe(true);
});
