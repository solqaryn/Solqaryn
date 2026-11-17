import { expect, test, type Page } from '@playwright/test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

/**
 * C02-04.M05.06 R2: real Angular router/UI/Chromium with a SYNTHETIC session.
 * This is NOT signed-JWT HTTP, MFA, database, real tenant A/B or production QA.
 */
const catalog = JSON.parse(readFileSync(
  resolve(process.cwd(), '../docs/v5/c02-04-m05-01-routes.json'), 'utf8'
)) as { schemaVersion: string; routes: Array<{ id: string; path: string }> };

const sessionEndpoint = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
const secretDiagnostic = 'PRIVATE_C0204_M0506_SECRET_DO_NOT_RENDER';
type HttpStatus = 200 | 403 | 503;

async function syntheticSession(
  page: Page,
  decide: (count: number) => HttpStatus,
  beforeReply?: (count: number) => Promise<void>
): Promise<() => number> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'SYNTHETIC_C0204_M0506_NOT_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  let gets = 0;
  await page.route(sessionEndpoint, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    gets++;
    if (beforeReply) await beforeReply(gets);
    const status = decide(gets);
    await route.fulfill({
      status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: { nombreUsuario: 'Fixture global',
            rolNombre: 'Global', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, data: null, message: secretDiagnostic })
    });
  });
  return () => gets;
}

function collectWrites(page: Page): string[] {
  const writes: string[] = [];
  page.on('request', request => {
    if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method())) {
      writes.push(request.method() + ' ' + request.url());
    }
  });
  return writes;
}

test.describe('C02-04.M05.06 global catalogue regression & a11y (Chromium real)', () => {
  test.describe.configure({ retries: 0 });
  test.use({ extraHTTPHeaders: {} });

  test('15 rutas del catálogo navegan realmente con guard, sin escrituras', async ({ page }) => {
    expect(catalog.routes).toHaveLength(15);
    expect(new Set(catalog.routes.map(item => item.id)).size).toBe(15);
    expect(new Set(catalog.routes.map(item => item.path)).size).toBe(15);
    const gets = await syntheticSession(page, () => 200);
    const writes = collectWrites(page);
    for (const item of catalog.routes) {
      await page.goto('/' + item.path);
      await expect.poll(() => new URL(page.url()).pathname).toBe('/' + item.path);
      await expect(page.getByRole('heading', { level: 1 }).first()).toBeVisible();
    }
    expect(gets()).toBeGreaterThanOrEqual(catalog.routes.length);
    expect(writes).toEqual([]);
  });

  test('320/375/768/1280 sin overflow, controles 44px y foco real por Tab/Enter', async ({ page }) => {
    const gets = await syntheticSession(page, () => 200);
    const writes = collectWrites(page);
    for (const width of [320, 375, 768, 1280]) {
      await page.setViewportSize({ width, height: 820 });
      await page.goto('/plataforma/consola-global-v5');
      const panel = page.locator('.control-plane');
      await expect(panel.locator('.control-cards li')).toHaveCount(4);
      await expect(page.getByRole('heading', { name: 'Consola global auditada' })).toBeVisible();
      const overflow = await page.evaluate(() =>
        document.documentElement.scrollWidth - document.documentElement.clientWidth);
      expect(overflow, 'overflow at viewport ' + width).toBeLessThanOrEqual(2);
      const box = await page.getByRole('button', { name: 'Revalidar sesión global' }).boundingBox();
      expect(box, 'button exists and has a measurable hit target').not.toBeNull();
      expect(box!.height).toBeGreaterThanOrEqual(44);
    }
    // Starting from the browser's natural focus order; no programmatic .focus().
    let reached = false;
    for (let i = 0; i < 16; i++) {
      await page.keyboard.press('Tab');
      reached = await page.getByRole('button', { name: 'Revalidar sesión global' })
        .evaluate(el => el === document.activeElement);
      if (reached) break;
    }
    expect(reached, 'real Tab traversal must reach the named revalidation button').toBe(true);
    const outline = await page.getByRole('button', { name: 'Revalidar sesión global' })
      .evaluate(el => ({ width: getComputedStyle(el).outlineWidth,
        style: getComputedStyle(el).outlineStyle }));
    expect(outline.style).not.toBe('none');
    expect(parseFloat(outline.width)).toBeGreaterThanOrEqual(2);
    const before = gets();
    await page.keyboard.press('Enter');
    await expect.poll(gets).toBe(before + 1);
    expect(writes).toEqual([]);
  });

  test('contraste computado 4.5:1 en las dos marcas, botones sin grants', async ({ page }) => {
    await syntheticSession(page, () => 200);
    const writes = collectWrites(page);
    await page.goto('/plataforma/consola-global-v5');
    const contrast = async () => page.getByTestId('c0204-branding').evaluate(el => {
      const luminance = (s: string) => {
        const values = (s.match(/[\d.]+/g) || []).slice(0, 3).map(Number);
        if (values.length !== 3) return NaN;
        const rgb = values.map(x => {
          const c = x / 255;
          return c <= .04045 ? c / 12.92 : Math.pow((c + .055) / 1.055, 2.4);
        });
        return .2126 * rgb[0]! + .7152 * rgb[1]! + .0722 * rgb[2]!;
      };
      const style = getComputedStyle(el);
      const a = luminance(style.color);
      const b = luminance(style.backgroundColor);
      return (Math.max(a, b) + .05) / (Math.min(a, b) + .05);
    });
    const normal = await contrast();
    expect(Number.isFinite(normal)).toBe(true);
    expect(normal).toBeGreaterThanOrEqual(4.5);
    await page.getByRole('button', { name: 'Contraste alto' }).click();
    await expect(page.getByTestId('c0204-brand-state')).toContainText('Contraste alto seleccionado');
    const high = await contrast();
    expect(Number.isFinite(high)).toBe(true);
    expect(high).toBeGreaterThanOrEqual(4.5);
    expect(writes).toEqual([]);
  });

  test('GET lento, 503 opaco, recuperación por nuevo GET sin mutación', async ({ page }) => {
    let release!: () => void;
    const held = new Promise<void>(resolveHold => { release = resolveHold; });
    const gets = await syntheticSession(page, n => n === 2 ? 503 : 200,
      async n => { if (n === 2) await held; });
    const writes = collectWrites(page);
    await page.goto('/plataforma/consola-global-v5');
    await expect(page.locator('.control-cards li')).toHaveCount(4);
    const button = page.getByRole('button', { name: 'Revalidar sesión global' });
    try {
      await button.click();
      await expect.poll(gets).toBe(2);
      await expect(page.locator('#control-plane-estado')).toHaveAttribute('aria-busy', 'true');
      await expect(button).toBeDisabled();
      await expect(page.locator('.control-cards li')).toHaveCount(0);
      const holdStart = Date.now();
      await page.waitForTimeout(225);
      expect(Date.now() - holdStart, 'synthetic delayed HTTP response observed for >=200ms').toBeGreaterThanOrEqual(200);
      expect(gets(), 'still one pending GET, never duplicate requests').toBe(2);
    } finally {
      release();
    }
    await expect(page.locator('#control-plane-estado')).toHaveAttribute('aria-busy', 'false');
    await expect(page.getByRole('alert')).toContainText('No se pudo verificar la sesión');
    await expect(page.getByText(secretDiagnostic)).toHaveCount(0);
    await page.getByRole('button', { name: 'Reintentar verificación' }).click();
    await expect(page.locator('.control-cards li')).toHaveCount(4);
    await expect.poll(gets).toBe(3);
    expect(writes).toEqual([]);
  });

  test('anónimo redirigido y respuesta HTTP 403 rechazada sin exponer consola', async ({ page }) => {
    const writes = collectWrites(page);
    await page.goto('/plataforma/consola-global-v5');
    await expect.poll(() => new URL(page.url()).pathname).toBe('/login');
    await expect(page.locator('.control-plane')).toHaveCount(0);
    const gets = await syntheticSession(page, () => 403);
    await page.goto('/plataforma/consola-global-v5');
    await expect.poll(() => new URL(page.url()).pathname).toBe('/login');
    await expect(page).toHaveURL(/plataforma=denegado/);
    await expect(page.locator('.control-plane')).toHaveCount(0);
    await expect(page.getByText(secretDiagnostic)).toHaveCount(0);
    expect(gets()).toBeGreaterThanOrEqual(1);
    expect(writes).toEqual([]);
  });

  test('inventario global como laboratorio de inspección browser: rutas y regreso seguros', async ({ page }) => {
    // No existe una ruta de laboratorio independiente; se verifica el inventario real,
    // sin presentarlo como aprobación de un laboratorio aislado de QA.
    const gets = await syntheticSession(page, () => 200);
    const writes = collectWrites(page);
    await page.goto('/plataforma/inventario-rutas-v19');
    await expect(page.getByRole('heading', { name: 'Inventario de programas y rutas declaradas' })).toBeVisible();
    await expect(page.getByTestId('c1304-route-catalogo').locator('li').first()).toBeVisible();
    await page.getByRole('link', { name: 'Volver a consola global' }).click();
    await expect.poll(() => new URL(page.url()).pathname).toBe('/plataforma/consola-global-v5');
    await expect(page.getByRole('heading', { name: 'Consola global auditada' })).toBeVisible();
    expect(gets()).toBeGreaterThanOrEqual(2);
    expect(writes).toEqual([]);
  });
});
