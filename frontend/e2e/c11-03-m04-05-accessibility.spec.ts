import { expect, test, type Page } from '@playwright/test';
const RUTA = '/plataforma/eventos-outbox-v22';
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function sesionDeLaboratorio(page: Page, status: 200 | 403): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'C11_M0405_TOKEN_FALSO_NO_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async handler => {
    if (handler.request().method() === 'OPTIONS') {
      await handler.fulfill({ status: 204, headers: cors });
      return;
    }
    await handler.fulfill({
      status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: { nombreUsuario: 'Laboratorio', rolNombre: 'Plataforma',
            ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'SENSITIVE_SERVER_TRACE_MUST_NOT_LEAK' })
    });
  });
}
test('C11-03.M04.05: anonimo y 403 no exponen la vista ni errores internos', async ({ page }) => {
  await page.goto(RUTA);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByTestId('c1103-outbox-private')).toHaveCount(0);
  await sesionDeLaboratorio(page, 403);
  await page.goto(RUTA);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByTestId('c1103-outbox-private')).toHaveCount(0);
  await expect(page.getByText('SENSITIVE_SERVER_TRACE_MUST_NOT_LEAK')).toHaveCount(0);
});
test('C11-03.M04.05: responsive real 320-1280, semantica y teclado Tab desde inicio', async ({ page }) => {
  await sesionDeLaboratorio(page, 200);
  const mutaciones: string[] = [];
  page.on('request', req => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(req.method()))
      mutaciones.push(req.method() + ' ' + req.url());
  });
  await page.goto(RUTA);
  const panel = page.getByTestId('c1103-outbox-private');
  await expect(panel).toBeVisible();
  await expect(page.getByRole('heading', { level: 1, name: 'Observabilidad del outbox' })).toHaveCount(1);
  await expect(panel.getByRole('heading', { level: 2, name: 'Etapas del procesamiento' })).toHaveCount(1);
  await expect(panel.locator('li h3')).toHaveCount(3);
  await expect(panel.getByRole('status')).toContainText('SIN_FUENTE_CONECTADA');
  await expect(panel.locator('form,input,button,select,textarea,iframe')).toHaveCount(0);
  await expect(panel.getByText(/no confirma JWT del servidor/)).toHaveCount(1);
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await expect.poll(async () => page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth
    )).toBeLessThanOrEqual(2);
    await expect(panel).toBeVisible();
  }
  const volver = page.getByRole('link', { name: 'Volver a plataforma' });
  let alcanzadoPorTab = false;
  for (let i = 0; i < 20; i++) {
    await page.keyboard.press('Tab');
    if (await volver.evaluate(el => document.activeElement === el)) {
      alcanzadoPorTab = true;
      break;
    }
  }
  expect(alcanzadoPorTab, 'enlace alcanzable solo con Tab real').toBe(true);
  const focoVisible = await volver.evaluate(el => {
    const s = getComputedStyle(el);
    return s.outlineStyle !== 'none' && parseFloat(s.outlineWidth) >= 2;
  });
  expect(focoVisible, 'indicador visual de foco en teclado').toBe(true);
  expect(mutaciones, 'nunca escribir ni llamar a pagos/outbox').toEqual([]);
});
test('C11-03.M04.05: contraste calculado en Chromium sobre texto real', async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'light' });
  await sesionDeLaboratorio(page, 200);
  await page.goto(RUTA);
  await expect(page.getByTestId('c1103-outbox-private')).toBeVisible();
  const medidas = await page.evaluate(() => {
    type Rgba = { r: number; g: number; b: number; a: number };
    const parse = (css: string): Rgba => {
      const m = css.match(/[\d.]+/g);
      if (!m || m.length < 3) throw new Error('COLOR_NO_RGB: ' + css);
      return { r: Number(m[0]), g: Number(m[1]), b: Number(m[2]),
        a: m[3] === undefined ? 1 : Number(m[3]) };
    };
    const sobre = (f: Rgba, b: Rgba): Rgba => ({
      r: f.r * f.a + b.r * (1 - f.a),
      g: f.g * f.a + b.g * (1 - f.a),
      b: f.b * f.a + b.b * (1 - f.a), a: 1
    });
    const fondo = (e: Element): Rgba => {
      const pila: Element[] = [];
      for (let n: Element | null = e; n; n = n.parentElement) pila.unshift(n);
      return pila.reduce((base, node) => sobre(parse(getComputedStyle(node).backgroundColor), base),
        { r: 255, g: 255, b: 255, a: 1 });
    };
    const lum = (c: Rgba) => {
      const convertir = (v: number) => {
        const s = v / 255;
        return s <= 0.04045 ? s / 12.92 : Math.pow((s + 0.055) / 1.055, 2.4);
      };
      return 0.2126 * convertir(c.r) + 0.7152 * convertir(c.g) + 0.0722 * convertir(c.b);
    };
    return ['#c1103-title', '.c1103 header a', '.c1103 header p', '.panel h2', '.panel li p']
      .map(selector => {
        const el = document.querySelector(selector);
        if (!el) throw new Error('TEXTO_NO_ENCONTRADO: ' + selector);
        const bg = fondo(el);
        const fg = sobre(parse(getComputedStyle(el).color), bg);
        const a = lum(bg), b = lum(fg);
        return { selector, ratio: (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05) };
      });
  });
  expect(medidas).toHaveLength(5);
  for (const medida of medidas)
    expect(medida.ratio, 'WCAG AA texto: ' + medida.selector).toBeGreaterThanOrEqual(4.5);
});
