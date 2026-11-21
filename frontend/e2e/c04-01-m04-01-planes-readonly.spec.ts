import { expect, test, type Page } from '@playwright/test';

const ruta = '/plataforma/catalogo-planes-v22';
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function sesionSintetica(page: Page, status: 200 | 403) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'UI_FIXTURE_NOT_SIGNED_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors }); return;
    }
    await route.fulfill({
      status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: { nombreUsuario: 'Fixture', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'REDACTED_INTERNAL_ERROR', data: null })
    });
  });
}

test('C04-01.M04.01 anónimo y respuesta 403 deniegan el contenido privado', async ({ page }) => {
  await page.goto(ruta);
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByTestId('c0401-private')).toHaveCount(0);
  await sesionSintetica(page, 403);
  await page.goto(ruta);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByTestId('c0401-private')).toHaveCount(0);
  await expect(page.getByText('REDACTED_INTERNAL_ERROR')).toHaveCount(0);
});

test('C04-01.M04.01 ruta privada sintética responsive sin escrituras comerciales', async ({ page }) => {
  await sesionSintetica(page, 200);
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  await page.goto(ruta);
  const privatePanel = page.getByTestId('c0401-private');
  await expect(privatePanel).toBeVisible();
  await expect(privatePanel.locator('[data-testid^="c0401-section-"]')).toHaveCount(3);
  await expect(privatePanel.getByText('NO_CERTIFICADO')).toBeVisible();
  await expect(privatePanel.locator('form,input,button,select,textarea,iframe')).toHaveCount(0);
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, 'horizontal overflow width=' + width).toBeLessThanOrEqual(2);
  }
  expect(writes).toEqual([]);
});

test('C04-01.M04.01 enlace por teclado con foco visible y área mínima', async ({ page }) => {
  await sesionSintetica(page, 200);
  await page.goto(ruta);
  await expect(page.getByTestId('c0401-private')).toBeVisible();
  await page.keyboard.press('Tab');
  const enlace = page.getByRole('link', { name: 'Volver a plataforma' });
  await expect(enlace).toBeFocused();
  const foco = await enlace.evaluate(element => {
    const css = getComputedStyle(element), bounds = element.getBoundingClientRect();
    return { outline: parseFloat(css.outlineWidth), width: bounds.width, height: bounds.height };
  });
  expect(foco.outline).toBeGreaterThanOrEqual(3);
  expect(foco.width).toBeGreaterThanOrEqual(44);
  expect(foco.height).toBeGreaterThanOrEqual(44);
});
