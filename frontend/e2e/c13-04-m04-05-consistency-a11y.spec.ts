import { test, expect, type Page } from '@playwright/test';

// C13-04.M04.05: browser real, session/API responses synthetic. NOT JWT/MySQL/tenant certification.
const views = [
  { url: '/plataforma/consola-global-v5', title: 'Consola global auditada', cards: '.control-cards li',
    link: 'Volver a administración de plataforma', target: '.control-cards li' },
  { url: '/plataforma/trazabilidad-cobertura-v5', title: 'Revisión de cobertura del plan', cards: '.coverage-list li',
    link: 'Regresar a administración de plataforma', target: '.coverage-shell [role="status"]' }
] as const;

async function syntheticPlatform(page: Page, allowed: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1304-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers });
      return;
    }
    await route.fulfill({
      status: allowed ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(allowed
        ? { success: true, data: { nombreUsuario: 'Synthetic', rolNombre: 'Global',
            ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'SECRET_TENANT_B_NEVER_RENDER' })
    });
  });
}

test('C13-04.M04.05 anonymous and forbidden routes hide private cards and error text', async ({ page }) => {
  for (const view of views) {
    await page.goto(view.url);
    await expect(page).toHaveURL(/\/login/);
    await expect(page.locator(view.cards)).toHaveCount(0);
  }
  await syntheticPlatform(page, false);
  for (const view of views) {
    await page.goto(view.url);
    await expect(page).toHaveURL(/\/login/);
    await expect(page.locator(view.cards)).toHaveCount(0);
    await expect(page.getByText('SECRET_TENANT_B_NEVER_RENDER')).toHaveCount(0);
  }
});

test('C13-04.M04.05 two existing views preserve landmarks and 320/375/768/1280 reflow', async ({ page }) => {
  await syntheticPlatform(page, true);
  for (const view of views) {
    for (const width of [320, 375, 768, 1280]) {
      await page.setViewportSize({ width, height: 812 });
      await page.goto(view.url);
      await expect(page.getByRole('heading', { name: view.title })).toBeVisible();
      await expect(page.locator(view.cards)).toHaveCount(4);
      await expect(page.getByRole('link', { name: view.link })).toBeVisible();
      const overflow = await page.evaluate(() =>
        document.documentElement.scrollWidth - document.documentElement.clientWidth);
      expect(overflow, view.url + ' width=' + width).toBeLessThanOrEqual(2);
    }
  }
});

test('C13-04.M04.05 initial Tab reaches both labeled return links with visible focus and 44px', async ({ page }) => {
  await syntheticPlatform(page, true);
  for (const view of views) {
    await page.goto(view.url);
    const link = page.getByRole('link', { name: view.link });
    let reached = false;
    for (let i = 0; i < 100; i++) {
      await page.keyboard.press('Tab');
      if (await link.evaluate(el => document.activeElement === el)) {
        reached = true;
        break;
      }
    }
    expect(reached, 'real Tab reachability ' + view.url).toBe(true);
    const css = await link.evaluate(el => {
      const rect = el.getBoundingClientRect(), s = getComputedStyle(el);
      return { w: rect.width, h: rect.height, outline: Number.parseFloat(s.outlineWidth),
        style: s.outlineStyle };
    });
    expect(css.w).toBeGreaterThanOrEqual(44);
    expect(css.h).toBeGreaterThanOrEqual(44);
    expect(css.style).not.toBe('none');
    expect(css.outline).toBeGreaterThanOrEqual(2);
  }
});

test('C13-04.M04.05 card/status text has >=4.5:1 computed contrast', async ({ page }) => {
  await syntheticPlatform(page, true);
  for (const view of views) {
    await page.goto(view.url);
    const el = page.locator(view.target).first();
    await expect(el).toBeVisible();
    const contrast = await el.evaluate(node => {
      const channels = (css: string) => {
        const n = css.match(/[\d.]+/g);
        if (!n || n.length < 3) throw new Error('Expected CSS rgb');
        return n.slice(0, 3).map(Number);
      };
      const luminance = (rgb: number[]) => {
        const c = rgb.map(v => {
          const s = v / 255;
          return s <= .04045 ? s / 12.92 : ((s + .055) / 1.055) ** 2.4;
        });
        return .2126 * c[0] + .7152 * c[1] + .0722 * c[2];
      };
      const fg = luminance(channels(getComputedStyle(node).color));
      let bg = [255, 255, 255];
      for (let n: Element | null = node; n; n = n.parentElement) {
        const style = getComputedStyle(n).backgroundColor;
        const c = style.match(/[\d.]+/g);
        if (!c || c.length < 3) continue;
        if (c.length < 4 || Number(c[3]) === 1) {
          bg = channels(style);
          break;
        }
      }
      const a = luminance(bg);
      return (Math.max(a, fg) + .05) / (Math.min(a, fg) + .05);
    });
    expect(contrast, view.url).toBeGreaterThanOrEqual(4.5);
  }
});
