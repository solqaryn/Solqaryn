import { expect, test } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';

const fixture = 'http://localhost:5005/__test/health-scenario';
const output = join(process.cwd(), 'v23-artifacts', 'c0601-m0504');
const widths = [320, 390, 768, 1280, 1440] as const;

test.beforeEach(async ({ request }) => {
  expect((await request.post(fixture, { data: { scenario: 'success' } })).status()).toBe(204);
});

test('C06-01.M05.04 direct public landing positive acceptance, safe content and sections', async ({ page }) => {
  await page.goto('/conoce-solqaryn');
  await expect(page).toHaveURL(/\/conoce-solqaryn$/);
  await expect(page.getByRole('heading', { level: 1 })).toContainText('Tu operación merece');
  await expect(page.getByRole('heading', { name: 'Más contexto, menos saltos.' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'La presentación es pública. Tus datos, no.' })).toBeVisible();
  await expect(page.getByRole('link', { name: /Ir a mi cuenta/ })).toHaveAttribute('href', '/login');
  await expect(page.locator('main')).toHaveCount(1);
  await expect(page.locator('h1')).toHaveCount(1);
  await expect(page.getByTestId('c0601-status-response')).toContainText('Sin consulta');
  await expect(page.locator('input[type="password"],iframe,form')).toHaveCount(0);
});

test('C06-01.M05.04 anchors and login CTA navigate real Angular routes', async ({ page }) => {
  await page.goto('/conoce-solqaryn');
  await page.getByRole('link', { name: 'Conocer la propuesta' }).click();
  await expect(page.locator('#beneficios')).toBeFocused();
  await page.getByRole('navigation', { name: 'Navegación de presentación' })
    .getByRole('link', { name: 'Alcance' }).click();
  await expect(page.locator('#alcance')).toBeFocused();
  await page.getByRole('link', { name: /Ir a mi cuenta/ }).click();
  await expect(page).toHaveURL(/\/login$/);
  await expect(page.locator('app-login')).toBeVisible();
});

test('C06-01.M05.04 explicit positive public HTTP succeeds without private identity', async ({ page }) => {
  const health: import('@playwright/test').Request[] = [];
  const privateRequests: string[] = [];
  page.on('request', req => {
    const pathname = new URL(req.url()).pathname;
    if (pathname === '/health') health.push(req);
    if (/\/api\/|\/tenant-context|\/empresa-configuracion/.test(pathname)) privateRequests.push(req.url());
  });
  await page.goto('/conoce-solqaryn');
  expect(health).toHaveLength(0);
  await page.getByTestId('c0601-status-check').click();
  await expect(page.getByTestId('c0601-status-response')).toContainText('respondió correctamente');
  expect(health).toHaveLength(1);
  expect(health[0].method()).toBe('GET');
  const headers = await health[0].allHeaders();
  for (const forbidden of ['authorization', 'cookie', 'x-empresa-id', 'x-storefront-session']) {
    expect(headers[forbidden]).toBeUndefined();
  }
  expect(privateRequests).toEqual([]);
});

test('C06-01.M05.04 real Chromium five widths ten PNG before/after no overflow', async ({ page }) => {
  await mkdir(output, { recursive: true });
  for (const width of widths) {
    await page.setViewportSize({ width, height: 900 });
    await page.goto('/conoce-solqaryn');
    const before = join(output, 'antes-idle-' + width + '.png');
    await page.screenshot({ path: before, fullPage: true });
    await test.info().attach('antes-' + width, { path: before, contentType: 'image/png' });
    await page.getByTestId('c0601-status-check').click();
    await expect(page.getByTestId('c0601-status-response')).toContainText('respondió correctamente');
    const after = join(output, 'despues-ok-' + width + '.png');
    await page.screenshot({ path: after, fullPage: true });
    await test.info().attach('despues-' + width, { path: after, contentType: 'image/png' });
    const audit = await page.evaluate(() => ({
      viewport: innerWidth, body: document.documentElement.scrollWidth,
      main: document.querySelectorAll('main').length, h1: document.querySelectorAll('h1').length
    }));
    expect(audit.viewport).toBe(width);
    expect(audit.body).toBeLessThanOrEqual(width + 1);
    expect(audit.main).toBe(1);
    expect(audit.h1).toBe(1);
    await expect(page.getByRole('link', { name: /Ir a mi cuenta/ })).toBeVisible();
  }
});

test('C06-01.M05.04 reduced motion, real keyboard and contrast acceptance', async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.setViewportSize({ width: 390, height: 800 });
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
  await expect(toggle).toHaveAttribute('aria-pressed', 'true');
  await expect(page.getByTestId('c0601-public-saas-landing')).toHaveAttribute('data-brand-theme', 'contraste');
  expect(await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches)).toBe(true);
});
