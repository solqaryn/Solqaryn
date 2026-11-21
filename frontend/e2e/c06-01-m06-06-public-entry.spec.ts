import { expect, test } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';

const widths = [320, 390, 768, 1280, 1440] as const;
const artifactDir = join(process.cwd(), 'v23-artifacts', 'c0601-m0606');

test('C06-01.M06.06 public root lands on real Angular presentation without private calls', async ({ page }) => {
  const privatePaths: string[] = [];
  page.on('request', (request) => {
    const url = new URL(request.url());
    if (/^\/api\/|^\/tenant-context|^\/empresa-configuracion/.test(url.pathname)) {
      privatePaths.push(url.pathname);
    }
  });

  await page.goto('/');
  await expect(page).toHaveURL(/\/conoce-solqaryn$/);
  await expect(page.getByRole('heading', { level: 1 })).toContainText('Tu operación merece');
  await expect(page.locator('main')).toHaveCount(1);
  await expect(page.locator('h1')).toHaveCount(1);
  await expect(page.locator('input[type="password"], form, iframe')).toHaveCount(0);
  expect(privatePaths).toEqual([]);

  await page.getByRole('link', { name: /Ir a mi cuenta/ }).click();
  await expect(page).toHaveURL(/\/login$/);
  await expect(page.locator('app-login')).toBeVisible();
});

test('C06-01.M06.06 private dashboard still denies anonymous navigation', async ({ page }) => {
  await page.goto('/dashboard');
  await expect(page).toHaveURL(/\/login$/);
  await expect(page.locator('app-login')).toBeVisible();
});

test('C06-01.M06.06 mobile desktop widths and real keyboard focus', async ({ page }) => {
  await mkdir(artifactDir, { recursive: true });
  await page.emulateMedia({ reducedMotion: 'reduce' });
  for (const width of widths) {
    await page.setViewportSize({ width, height: 900 });
    await page.goto('/');
    await expect(page).toHaveURL(/\/conoce-solqaryn$/);
    const measurements = await page.evaluate(() => ({
      scrollWidth: document.documentElement.scrollWidth,
      viewportWidth: window.innerWidth,
      h1: document.querySelectorAll('h1').length,
      main: document.querySelectorAll('main').length
    }));
    expect(measurements.viewportWidth).toBe(width);
    expect(measurements.scrollWidth).toBeLessThanOrEqual(width + 1);
    expect(measurements.h1).toBe(1);
    expect(measurements.main).toBe(1);
    await expect(page.getByRole('link', { name: /Ir a mi cuenta/ })).toBeVisible();
    const path = join(artifactDir, 'root-public-' + width + '.png');
    await page.screenshot({ path, fullPage: true });
    await test.info().attach('root-public-' + width, { path, contentType: 'image/png' });
  }

  await page.setViewportSize({ width: 390, height: 800 });
  await page.goto('/');
  await page.keyboard.press('Tab');
  await expect(page.getByRole('link', { name: 'Ir al contenido' })).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(page.locator('#c0601-content')).toBeFocused();
  expect(await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches)).toBe(true);
});
