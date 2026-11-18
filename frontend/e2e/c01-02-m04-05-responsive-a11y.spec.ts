import { expect, test, type Page } from '@playwright/test';

const url = '/plataforma/capacidades-v19';
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function syntheticPlatformSession(page: Page): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0102-m0405-test-token-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    await route.fulfill({
      status: 200, headers: cors, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: {
        nombreUsuario: 'Synthetic Global QA', rolNombre: 'Plataforma',
        ambito: 'Plataforma', esAdministrador: true
      } })
    });
  });
}

test.describe('C01-02.M04.05 - responsive, teclado, foco y contraste del catalogo', () => {
  test.describe.configure({ retries: 0 });

  test('320 px y 1280 px: catalogo legible, cards no recortadas y sin scroll horizontal interno', async ({ page }) => {
    await syntheticPlatformSession(page);
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 850 });
      await page.goto(url);
      const main = page.locator('main.catalogo');
      await expect(page.getByRole('heading', { level: 1, name: 'Catálogo global de módulos y capacidades' })).toBeVisible();
      await expect(page.locator('[data-testid="c0102-listado"] li').first()).toBeVisible();
      const metrics = await main.evaluate(element => ({
        scrollWidth: element.scrollWidth, clientWidth: element.clientWidth,
        childWidths: Array.from(element.querySelectorAll('li')).map(child => child.getBoundingClientRect().width),
        bounds: element.getBoundingClientRect().width
      }));
      expect(metrics.scrollWidth, 'contenido interno no debe desbordarse horizontalmente').toBeLessThanOrEqual(metrics.clientWidth + 1);
      expect(metrics.childWidths.every(w => w > 0 && w <= metrics.bounds + 1)).toBe(true);
    }
  });

  test('Tab real alcanza enlace de retorno, focus-visible tiene contorno perceptible y Enter navega', async ({ page }) => {
    await syntheticPlatformSession(page);
    await page.goto(url);
    const back = page.getByRole('link', { name: 'Volver a plataforma' });
    let reached = false;
    for (let i = 0; i < 120; i++) {
      await page.keyboard.press('Tab');
      if (await back.evaluate(el => el === document.activeElement)) {
        reached = true;
        break;
      }
    }
    expect(reached, 'el enlace debe ser accesible por tabulacion natural').toBe(true);
    const focus = await back.evaluate(el => {
      const style = getComputedStyle(el);
      return { width: parseFloat(style.outlineWidth), type: style.outlineStyle };
    });
    expect(focus.type).not.toBe('none');
    expect(focus.width).toBeGreaterThanOrEqual(3);
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/plataforma\/?(?:\?|$)/);
  });

  test('etiquetas semanticas y contraste de texto real, sin controles mutantes', async ({ page }) => {
    await syntheticPlatformSession(page);
    await page.goto(url);
    await expect(page.locator('main.catalogo')).toHaveAttribute('aria-labelledby', 'c0102-title');
    await expect(page.getByRole('heading', { level: 1 })).toHaveCount(1);
    await expect(page.getByRole('list')).toHaveCount(1);
    await expect(page.getByRole('link', { name: 'Volver a plataforma' })).toBeVisible();
    await expect(page.locator('main.catalogo form, main.catalogo input, main.catalogo button')).toHaveCount(0);
    const contrast = await page.locator('#c0102-title').evaluate(el => {
      const rgb = (value: string): number[] => {
        const matched = value.match(/rgba?\(([\d.]+),\s*([\d.]+),\s*([\d.]+)/);
        if (!matched) throw new Error('color cannot be measured: ' + value);
        return [Number(matched[1]), Number(matched[2]), Number(matched[3])];
      };
      const luminance = (colors: number[]) => colors
        .map(channel => channel / 255)
        .map(channel => channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4)
        .reduce((acc, channel, index) => acc + channel * [0.2126, 0.7152, 0.0722][index], 0);
      const foreground = rgb(getComputedStyle(el).color);
      let ancestor: Element | null = el;
      let background = [255, 255, 255];
      while (ancestor) {
        const value = getComputedStyle(ancestor).backgroundColor;
        if (value && value !== 'transparent' && !/^rgba\([^)]*,\s*0\)$/.test(value)) {
          background = rgb(value);
          break;
        }
        ancestor = ancestor.parentElement;
      }
      const [high, low] = [luminance(foreground), luminance(background)].sort((a, b) => b - a);
      return (high + 0.05) / (low + 0.05);
    });
    expect(contrast, 'contraste real del titulo no debe ser inferior a WCAG AA').toBeGreaterThanOrEqual(4.5);
  });
});
