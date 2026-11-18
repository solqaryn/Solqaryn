import { expect, test, type Page } from '@playwright/test';

/**
 * C14-04.M05.06: real Chromium regression/a11y/coarse load smoke for the
 * DEV read-only release preparation view with an intercepted synthetic
 * session. Never certifies JWT, tenant isolation, RPO/RTO, deploy or p95/p99.
 */
async function syntheticPlatformSession(page: Page): Promise<() => number> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'm0506-not-a-signed-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  let reads = 0;
  const headers = {
    'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
    'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
    'Access-Control-Allow-Methods': 'GET, OPTIONS'
  };
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers });
      return;
    }
    reads++;
    await route.fulfill({
      status: 200,
      headers,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        data: {
          nombreUsuario: 'Synthetic',
          rolNombre: 'Admin',
          ambito: 'Plataforma',
          esAdministrador: true
        }
      })
    });
  });
  return () => reads;
}

test('regresion 320/768/1280: evidencia incompleta visible sin overflow ni PASS inventado', async ({ page }) => {
  await syntheticPlatformSession(page);
  for (const width of [320, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await page.goto('/plataforma/flujo-v5');
    const panel = page.getByTestId('release-readiness-contract');
    await expect(panel.getByRole('heading', { name: 'Preparación de salida · evidencia pendiente' })).toBeVisible();
    await expect(panel.getByRole('list', { name: 'Puertas de preparación no certificadas' })).toBeVisible();
    await expect(panel.locator('ol li')).toHaveCount(4);
    await expect(panel.getByText('SIN_VERIFICAR')).toHaveCount(3);
    await expect(panel.getByText('NO_MEDIDO')).toHaveCount(1);
    await expect(panel).not.toContainText('PASS');
    await expect(panel.locator('form, button, input, select, textarea')).toHaveCount(0);
    expect(await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth),
    'horizontal overflow at ' + width).toBeLessThanOrEqual(2);
  }
});

test('teclado real: Tab llega al revalidador sin focus programatico y Enter no concede permisos', async ({ page }) => {
  await syntheticPlatformSession(page);
  await page.goto('/plataforma/flujo-v5');
  const button = page.getByRole('button', { name: /Revalidar sesión/ });
  await expect(button).toBeVisible();
  let keyboardReached = false;
  for (let i = 0; i < 35; i++) {
    await page.keyboard.press('Tab');
    keyboardReached = await button.evaluate(el => document.activeElement === el);
    if (keyboardReached) break;
  }
  expect(keyboardReached, 'reachable with genuine Tab navigation').toBe(true);
  await page.keyboard.press('Enter');
  await expect(page.getByTestId('release-readiness-feedback')).toHaveAttribute('data-state', 'exito');
  await expect(page.getByTestId('release-readiness-contract')).not.toContainText('PASS');
  await expect(page.getByTestId('release-empty')).toHaveAttribute('data-evidence-state', 'vacio');
});

test('movimiento reducido: el contrato permanece legible, sin autorizacion ni animacion requerida', async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await syntheticPlatformSession(page);
  await page.setViewportSize({ width: 320, height: 720 });
  await page.goto('/plataforma/flujo-v5');
  expect(await page.evaluate(() => window.matchMedia('(prefers-reduced-motion: reduce)').matches)).toBe(true);
  const panel = page.getByTestId('release-readiness-contract');
  await expect(panel.getByRole('heading', { name: 'Preparación de salida · evidencia pendiente' })).toBeVisible();
  await expect(panel.locator('ol li')).toHaveCount(4);
  await expect(panel).not.toContainText('PASS');
});

test('rendimiento proporcional: DOM listo bajo budget amplio de humo, no es RPO/RTO ni p95/p99', async ({ page }) => {
  await syntheticPlatformSession(page);
  await page.goto('/plataforma/flujo-v5', { waitUntil: 'domcontentloaded' });
  const panel = page.getByTestId('release-readiness-contract');
  await expect(panel).toBeVisible({ timeout: 12_000 });
  const navigationTime = await page.evaluate(() => {
    const nav = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming | undefined;
    return nav?.domContentLoadedEventEnd ?? NaN;
  });
  expect(Number.isFinite(navigationTime)).toBe(true);
  expect(navigationTime, 'coarse local Chromium DOMContentLoaded smoke in milliseconds').toBeLessThan(30_000);
  await expect(panel.getByText('NO_MEDIDO')).toHaveCount(1);
  await expect(panel).not.toContainText('PASS');
});
