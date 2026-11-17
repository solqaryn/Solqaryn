import { expect, test, type Page } from '@playwright/test';

// C00-04.M05.06: real Chromium, synthetic session only; not live JWT/RBAC/MySQL.
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function setup(page: Page) {
  let status = 200, requests = 0;
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'C0004-M0506-SYNTHETIC-NOT-JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(sessionUrl, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors }); return;
    }
    requests++;
    await route.fulfill({
      status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200 ? {
        success: true, data: { nombreUsuario: 'Prueba V19', rolNombre: 'Plataforma',
          ambito: 'Plataforma', esAdministrador: true }
      } : { success: false, message: 'SECRET_PROVIDER_DO_NOT_LEAK', data: null })
    });
  });
  return { count: () => requests, deny: () => { status = 403; } };
}
test.describe('C00-04.M05.06 regresion de experiencia y seguridad visual', () => {
  test.describe.configure({ retries: 0 });
  test('Tab desde inicio alcanza regreso y Enter navega sin focus artificial', async ({ page }) => {
    const fixture = await setup(page);
    await page.goto('/plataforma/gobierno-cambios-v5');
    const link = page.getByRole('link', { name: 'Volver a administración de plataforma' });
    await expect(link).toBeVisible();
    let reached = false;
    for (let i = 0; i < 40; i++) {
      await page.keyboard.press('Tab');
      reached = await link.evaluate(el => document.activeElement === el);
      if (reached) break;
    }
    expect(reached, 'Tab real debe alcanzar retorno').toBe(true);
    await expect(link).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/plataforma(?:\?|$)/);
    expect(fixture.count()).toBeGreaterThanOrEqual(1);
  });
  test('320 375 768 1280: 4 pasos, targets 44px, reduced motion, sin overflow', async ({ page }) => {
    const fixture = await setup(page);
    await page.emulateMedia({ reducedMotion: 'reduce' });
    await page.goto('/plataforma/gobierno-cambios-v5');
    await expect(page.locator('main.governance')).toHaveAttribute('aria-labelledby', 'governance-title');
    await expect(page.locator('ol.steps > li h3')).toHaveText([
      'Alcance y dependencias', 'Pruebas dirigidas', 'Verificación DEV', 'Revisión independiente A5'
    ]);
    for (const width of [320, 375, 768, 1280]) {
      await page.setViewportSize({ width, height: 820 });
      const overflow = await page.evaluate(() =>
        Math.max(document.documentElement.scrollWidth, document.body.scrollWidth) - innerWidth);
      expect(overflow, 'overflow at ' + width).toBeLessThanOrEqual(2);
      for (const selector of [
        'button[aria-label="Revalidar sesión de plataforma"]',
        'a[aria-label="Volver a administración de plataforma"]'
      ]) {
        const box = await page.locator(selector).boundingBox();
        expect(box).not.toBeNull();
        expect(box!.height).toBeGreaterThanOrEqual(44);
      }
    }
    expect(await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches)).toBe(true);
    expect(fixture.count(), 'resizing must not refetch').toBe(1);
  });
  test('budget causal: solo un GET de sesion, sin writes ni plan institucional', async ({ page }) => {
    const fixture = await setup(page);
    const writes: string[] = [], forbidden: string[] = [];
    page.on('request', request => {
      if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
    });
    await page.route(/\/(?:plan-maestro|bitacora|cola|v5\/claims)(?:\/|\?|$)/i, async route => {
      forbidden.push(route.request().url()); await route.abort();
    });
    await page.goto('/plataforma/gobierno-cambios-v5');
    await expect(page.locator('ol.steps > li')).toHaveCount(4);
    await page.setViewportSize({ width: 320, height: 820 });
    await page.setViewportSize({ width: 1280, height: 820 });
    await expect(page.getByRole('note')).toContainText('Contenido estático');
    expect(fixture.count()).toBe(1);
    expect(writes).toEqual([]);
    expect(forbidden).toEqual([]);
  });
  test('403 sintetico revalidacion oculta pasos y mensaje del proveedor', async ({ page }) => {
    const fixture = await setup(page);
    await page.goto('/plataforma/gobierno-cambios-v5');
    await expect(page.locator('ol.steps > li')).toHaveCount(4);
    fixture.deny();
    await page.getByRole('button', { name: 'Revalidar sesión de plataforma' }).click();
    await expect(page.getByRole('alert')).toContainText('Sin permiso de plataforma confirmado');
    await expect(page.locator('ol.steps > li')).toHaveCount(0);
    await expect(page.getByText('SECRET_PROVIDER_DO_NOT_LEAK')).toHaveCount(0);
    expect(fixture.count()).toBe(2);
  });
});
