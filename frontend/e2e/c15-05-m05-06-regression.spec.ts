import { expect, test, type Page } from '@playwright/test';

/** C15-05.M05.06: Chromium real, sesion HTTP sintetica, NO JWT/MySQL/PROD. */
const session = /\/plataforma\/sesion(?:\?|$)/;
async function mockSession(page: Page, status: (count: number) => number): Promise<() => number> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c15-m0506-synthetic-NOT-JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  let reads = 0;
  const headers = {
    'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
    'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
    'Access-Control-Allow-Methods': 'GET, OPTIONS'
  };
  await page.route(session, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers });
      return;
    }
    expect(route.request().method()).toBe('GET');
    const code = status(++reads);
    await route.fulfill({
      status: code, headers, contentType: 'application/json',
      body: JSON.stringify(code === 200
        ? { success: true, data: { nombreUsuario: 'Sintetico', rolNombre: 'Plataforma',
            ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, data: null, message: 'PRIVATE_PROVIDER_DATA_NEVER_EXPOSE' })
    });
  });
  return () => reads;
}

test('C15-05.M05.06: responsive 320/768/1280, sin PASS inventado ni mutaciones', async ({ page }) => {
  const reads = await mockSession(page, () => 200);
  const mutations: string[] = [];
  page.on('request', r => {
    if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(r.method()))
      mutations.push(r.method() + ' ' + r.url());
  });
  for (const width of [320, 768, 1280]) {
    await page.setViewportSize({ width, height: 820 });
    await page.goto('/plataforma/auditoria-operativa-v5');
    const view = page.locator('main.audit-view');
    await expect(view.getByRole('heading', { name: 'Controles de ejecución y evidencia' })).toBeVisible();
    await expect(view.locator('ol.controls > li')).toHaveCount(4);
    await expect(view).toContainText('0 métricas verificadas');
    await expect(view).not.toContainText('A5_PASS');
    await expect(view.locator('form, input, textarea')).toHaveCount(0);
    const overflow = await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, 'horizontal scroll ' + width).toBeLessThanOrEqual(2);
  }
  expect(reads()).toBeGreaterThanOrEqual(3);
  expect(mutations).toEqual([]);
});

test('C15-05.M05.06: Tab real y Enter revalidan sin conceder aprobacion', async ({ page }) => {
  const reads = await mockSession(page, () => 200);
  await page.goto('/plataforma/auditoria-operativa-v5');
  const button = page.getByRole('button', { name: 'Revalidar sesión de auditoría' });
  await expect(button).toBeVisible();
  let reached = false;
  for (let i = 0; i < 48; i++) {
    await page.keyboard.press('Tab');
    if (await button.evaluate(el => document.activeElement === el)) { reached = true; break; }
  }
  expect(reached, 'Tab fisico, no .focus()').toBe(true);
  await page.keyboard.press('Enter');
  await expect(page.getByText('Sesión de plataforma revalidada correctamente. No se consultan registros operativos en vivo.')).toBeVisible();
  await expect(page.locator('main.audit-view ol.controls > li')).toHaveCount(4);
  expect(reads()).toBe(2);
});

test('C15-05.M05.06: revocacion 403 fail-closed sin mensaje privado', async ({ page }) => {
  const reads = await mockSession(page, n => n === 1 ? 200 : 403);
  await page.goto('/plataforma/auditoria-operativa-v5');
  await page.getByRole('button', { name: 'Revalidar sesión de auditoría' }).click();
  await expect(page.getByText('Sin permiso de plataforma confirmado. La guía privada permanece oculta.')).toBeVisible();
  await expect(page.locator('main.audit-view ol.controls > li')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_PROVIDER_DATA_NEVER_EXPOSE')).toHaveCount(0);
  expect(reads()).toBe(2);
});

test('C15-05.M05.06: reduced motion y DOMContentLoaded local de humo', async ({ page }) => {
  await mockSession(page, () => 200);
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/plataforma/auditoria-operativa-v5', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Controles de ejecución y evidencia' })).toBeVisible();
  expect(await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches)).toBe(true);
  const dcl = await page.evaluate(() => {
    const n = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming | undefined;
    return n?.domContentLoadedEventEnd ?? NaN;
  });
  expect(Number.isFinite(dcl)).toBe(true);
  expect(dcl, 'DCL local ms; NO p95/p99 ni Web Vitals de campo').toBeLessThan(30_000);
  await expect(page.locator('main.audit-view')).toContainText('0 métricas verificadas');
});
