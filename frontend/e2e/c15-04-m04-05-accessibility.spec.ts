import { expect, test, type Page } from '@playwright/test';

// C15-04.M04.05: Chromium real, plataforma/sesion sintética; NO JWT/RBAC o MySQL live.
const views = [
  { url: '/plataforma/gobierno-cambios-v5', heading: 'Decisiones y controles V5',
    link: 'Volver a administración de plataforma', cards: '.steps li' },
  { url: '/plataforma/trazabilidad-cobertura-v5', heading: 'Revisión de cobertura del plan',
    link: 'Regresar a administración de plataforma', cards: '.coverage-list li' }
] as const;
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function syntheticSession(page: Page, allowed: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c15-04-m04-05-not-real-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(sessionUrl, async handler => {
    if (handler.request().method() === 'OPTIONS') {
      await handler.fulfill({ status: 204, headers: cors }); return;
    }
    await handler.fulfill({
      status: allowed ? 200 : 403, headers: cors, contentType: 'application/json',
      body: JSON.stringify(allowed
        ? { success: true, data: { nombreUsuario: 'Synthetic',
            rolNombre: 'Global', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'SECRET_TENANT_B_NEVER_RENDER' })
    });
  });
}
test.describe('C15-04.M04.05 accesibilidad de dos vistas privadas existentes', () => {
  test.describe.configure({ retries: 0 });

  test('anónimo y 403 deniegan rutas y no filtran datos o tarjetas', async ({ page }) => {
    for (const v of views) {
      await page.goto(v.url);
      await expect(page).toHaveURL(/\/login/);
      await expect(page.locator(v.cards)).toHaveCount(0);
    }
    await syntheticSession(page, false);
    for (const v of views) {
      await page.goto(v.url);
      await expect(page).toHaveURL(/\/login/);
      await expect(page.locator(v.cards)).toHaveCount(0);
      await expect(page.getByText('SECRET_TENANT_B_NEVER_RENDER')).toHaveCount(0);
    }
  });

  test('responsive 320 375 768 1280, landmarks y etiquetas sin overflow', async ({ page }) => {
    await syntheticSession(page, true);
    for (const v of views) {
      for (const width of [320, 375, 768, 1280]) {
        await page.setViewportSize({ width, height: 812 });
        await page.goto(v.url);
        await expect(page.getByRole('heading', { name: v.heading })).toBeVisible();
        await expect(page.getByRole('link', { name: v.link })).toBeVisible();
        await expect(page.locator(v.cards)).toHaveCount(4);
        const overflow = await page.evaluate(() =>
          document.documentElement.scrollWidth - document.documentElement.clientWidth);
        expect(overflow, v.url + ' at ' + width).toBeLessThanOrEqual(2);
      }
    }
  });

  test('Tab verdadero llega a enlaces con foco visible y objetivo 44x44', async ({ page }) => {
    await syntheticSession(page, true);
    for (const v of views) {
      await page.goto(v.url);
      const link = page.getByRole('link', { name: v.link });
      let reached = false;
      for (let n = 0; n < 100; n++) {
        await page.keyboard.press('Tab');
        if (await link.evaluate(el => document.activeElement === el)) {
          reached = true; break;
        }
      }
      expect(reached, 'Tab to ' + v.url).toBe(true);
      const css = await link.evaluate(el => {
        const rect = el.getBoundingClientRect(), style = getComputedStyle(el);
        return { width: rect.width, height: rect.height,
          outline: Number.parseFloat(style.outlineWidth), type: style.outlineStyle };
      });
      expect(css.width).toBeGreaterThanOrEqual(44);
      expect(css.height).toBeGreaterThanOrEqual(44);
      expect(css.type).not.toBe('none');
      expect(css.outline).toBeGreaterThanOrEqual(2);
    }
  });

  test('contraste computado encabezados >=4.5:1 y sin escritura de UI', async ({ page }) => {
    await syntheticSession(page, true);
    const writes: string[] = [];
    page.on('request', r => {
      if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method());
    });
    for (const v of views) {
      await page.goto(v.url);
      const el = page.getByRole('heading', { name: v.heading });
      await expect(el).toBeVisible();
      const contrast = await el.evaluate(node => {
        const channels = (css: string) => {
          const match = css.match(/[\d.]+/g);
          if (!match || match.length < 3) throw new Error('Invalid color');
          return match.slice(0, 3).map(Number);
        };
        const lum = (rgb: number[]) => rgb.map(v => {
          const s = v / 255;
          return s <= .04045 ? s / 12.92 : ((s + .055) / 1.055) ** 2.4;
        }).reduce((sum, value, i) => sum + value * [0.2126, 0.7152, 0.0722][i], 0);
        const foreground = lum(channels(getComputedStyle(node).color));
        let background = [255, 255, 255];
        for (let parent: Element | null = node; parent; parent = parent.parentElement) {
          const computed = getComputedStyle(parent).backgroundColor;
          const values = computed.match(/[\d.]+/g);
          if (values && values.length >= 3 && (values.length === 3 || Number(values[3]) === 1)) {
            background = channels(computed); break;
          }
        }
        const back = lum(background);
        return (Math.max(back, foreground) + .05) / (Math.min(back, foreground) + .05);
      });
      expect(contrast, v.url).toBeGreaterThanOrEqual(4.5);
    }
    expect(writes).toEqual([]);
  });
});
