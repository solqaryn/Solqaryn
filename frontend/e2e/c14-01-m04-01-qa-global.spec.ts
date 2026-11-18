import { expect, test, type Page } from '@playwright/test';

const url = '/plataforma/qa-global-v19';
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function session(page: Page, status = 200) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1401-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(sessionUrl, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors }); return;
    }
    await route.fulfill({ status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: { nombreUsuario: 'Synthetic', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'SECRET_TENANT_B_PRIVATE_DSN' }) });
  });
}
test.describe('C14-01.M04.01 QA global - Chromium real', () => {
  test.describe.configure({ retries: 0 });
  test('anonimo y 403 no muestran QA privado ni error sensible', async ({ page }) => {
    await page.goto(url);
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByTestId('c1401-qa-global')).toHaveCount(0);
    await session(page, 403);
    await page.goto(url);
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByTestId('c1401-qa-global')).toHaveCount(0);
    await expect(page.getByText('SECRET_TENANT_B_PRIVATE_DSN')).toHaveCount(0);
  });
  test('sesion sintetica: cinco areas NO_VERIFICADO, sin aprobacion humana ni escrituras', async ({ page }) => {
    await session(page);
    const writes: string[] = [];
    page.on('request', r => { if (!['GET','HEAD','OPTIONS'].includes(r.method())) writes.push(r.method()); });
    await page.goto(url);
    await expect(page.getByRole('heading', { name: 'Consulta de QA automático y cobertura global' })).toBeVisible();
    await expect(page.locator('.qa-controles li')).toHaveCount(5);
    await expect(page.locator('.pendiente')).toHaveCount(5);
    await expect(page.getByTestId('c1401-qa-global').locator('button, form, input, textarea, select')).toHaveCount(0);
    expect(await page.getByRole('link', { name: /Consultar GitHub Actions/ }).getAttribute('href')).toContain('github.com/solqaryn/Solqaryn/actions');
    expect(writes).toEqual([]);
  });
  test('cuatro viewports y Tab real con foco visible y destino 44px', async ({ page }) => {
    await session(page);
    for (const width of [320,375,768,1280]) {
      await page.setViewportSize({ width, height: 812 });
      await page.goto(url);
      await expect(page.getByTestId('c1401-qa-global')).toBeVisible();
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
      expect(overflow, 'overflow a ' + width).toBeLessThanOrEqual(2);
    }
    const link = page.getByRole('link', { name: 'Volver a plataforma' });
    let reached = false;
    for (let i=0; i<110; i++) {
      await page.keyboard.press('Tab');
      if (await link.evaluate(el => document.activeElement === el)) { reached = true; break; }
    }
    expect(reached).toBe(true);
    const focus = await link.evaluate(el => {
      const rect=el.getBoundingClientRect(), s=getComputedStyle(el);
      return { width:rect.width,height:rect.height,outline:Number.parseFloat(s.outlineWidth) };
    });
    expect(focus.width).toBeGreaterThanOrEqual(44);
    expect(focus.height).toBeGreaterThanOrEqual(44);
    expect(focus.outline).toBeGreaterThanOrEqual(2);
  });
});
