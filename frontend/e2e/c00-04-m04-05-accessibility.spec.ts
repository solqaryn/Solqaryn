import { test, expect, type Page } from '@playwright/test';

/**
 * C00-04.M04.05 — regresión Angular real en Chromium local.
 * Se simula exclusivamente /plataforma/sesion para comprobar UX.
 * NO acredita JWT/RBAC ni acceso HTTP/MySQL en backend productivo.
 */
const sessionPath = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function mockToken(page: Page): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c00-04-test-only-not-a-real-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
}
async function mockSession(page: Page, allowed: boolean): Promise<number[]> {
  const results: number[] = [];
  await page.route(sessionPath, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    const code = allowed ? 200 : 403;
    results.push(code);
    await route.fulfill({
      status: code, headers: cors, contentType: 'application/json',
      body: JSON.stringify(allowed
        ? { success: true, data: {
            nombreUsuario: 'Prueba visual sintetica', rolNombre: 'Plataforma simulada',
            ambito: 'Plataforma', esAdministrador: true
          }}
        : { success: false, message: 'DENEGADO', data: null })
    });
  });
  return results;
}

test.describe('C00-04.M04.05 gobierno de cambios — responsive, teclado y seguridad de interfaz', () => {
  test.describe.configure({ retries: 0 });

  test('anonimo no ve la superficie privada ni consulta la sesion', async ({ page }) => {
    const calls = await mockSession(page, true);
    await page.goto('/plataforma/gobierno-cambios-v5');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.getByRole('heading', { name: 'Decisiones y controles V5' })).toHaveCount(0);
    expect(calls).toEqual([]);
  });

  test('respuesta 403 simulada impide exponer el contenido de gobierno', async ({ page }) => {
    await mockToken(page);
    const calls = await mockSession(page, false);
    await page.goto('/plataforma/gobierno-cambios-v5');
    await expect(page).toHaveURL(/\/login\?plataforma=denegado(?:&|$)/);
    await expect(page.getByRole('heading', { name: 'Decisiones y controles V5' })).toHaveCount(0);
    expect(calls).toContain(403);
  });

  test('390 y 1280px: cuatro pasos legibles, sin desbordamiento horizontal ni datos', async ({ page }) => {
    await mockToken(page);
    const calls = await mockSession(page, true);
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/plataforma/gobierno-cambios-v5');
    await expect(page.getByRole('heading', { name: 'Decisiones y controles V5' })).toBeVisible();
    await expect(page.locator('.steps li')).toHaveCount(4);
    await expect(page.getByText('Solo lectura · sin autoridad operativa')).toBeVisible();
    await expect(page.getByText('No se consultan microtareas', { exact: false })).toBeVisible();
    for (const width of [390, 1280]) {
      await page.setViewportSize({ width, height: 844 });
      const dimensions = await page.evaluate(() => ({
        outer: document.documentElement.scrollWidth,
        viewport: document.documentElement.clientWidth
      }));
      expect(dimensions.outer).toBeLessThanOrEqual(dimensions.viewport + 2);
      for (const li of await page.locator('.steps li').all()) {
        const rect = await li.boundingBox();
        expect(rect).not.toBeNull();
        expect(rect!.x).toBeGreaterThanOrEqual(-1);
        expect(rect!.x + rect!.width).toBeLessThanOrEqual(width + 2);
      }
    }
    expect(calls).toContain(200);
  });

  test('teclado y etiquetas: retorno enfocable sin controles de aprobacion', async ({ page }) => {
    await mockToken(page);
    await mockSession(page, true);
    const unwanted: string[] = [];
    await page.route(/\/(?:plan-maestro|bitacora|cola|v5\/claims)(?:\/|\?|$)/i, async route => {
      unwanted.push(route.request().url());
      await route.abort();
    });
    await page.goto('/plataforma/gobierno-cambios-v5');
    const back = page.getByRole('link', { name: 'Volver a administración de plataforma' });
    await expect(back).toBeVisible();
    await back.focus();
    await expect(back).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/plataforma(?:$|[?#])/);
    expect(unwanted).toEqual([]);
  });

  test('texto principal mantiene contraste >=4.5:1 en fondo calculado', async ({ page }) => {
    await mockToken(page);
    await mockSession(page, true);
    await page.goto('/plataforma/gobierno-cambios-v5');
    const contrast = await page.getByRole('heading', { name: 'Decisiones y controles V5' }).evaluate(el => {
      const parse = (raw: string): number[] =>
        (raw.match(/[\d.]+/g) ?? []).slice(0, 3).map(Number);
      const lum = (rgb: number[]): number => {
        const linear = rgb.map(v => {
          const n = v / 255;
          return n <= .04045 ? n / 12.92 : Math.pow((n + .055) / 1.055, 2.4);
        });
        return .2126 * linear[0] + .7152 * linear[1] + .0722 * linear[2];
      };
      const fg = parse(getComputedStyle(el).color);
      const host = el.closest('main') ?? document.body;
      const hostBg = getComputedStyle(host).backgroundColor;
      const bodyBg = getComputedStyle(document.body).backgroundColor;
      // Both transparent => the local page's default white canvas.
      const bgColor = /rgba?\([^)]*,\s*0\s*\)$/.test(hostBg)
        ? (/rgba?\([^)]*,\s*0\s*\)$/.test(bodyBg) ? 'rgb(255, 255, 255)' : bodyBg)
        : hostBg;
      const bg = parse(bgColor);
      if (fg.length !== 3 || bg.length !== 3) throw new Error('CSS color invalido');
      const [a, b] = [lum(fg), lum(bg)].sort((x, y) => y - x);
      return (a + .05) / (b + .05);
    });
    expect(contrast).toBeGreaterThanOrEqual(4.5);
  });
});
