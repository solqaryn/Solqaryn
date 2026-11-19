import { expect, test, type Page } from '@playwright/test';

const ruta = '/plataforma/revision-arquitectura-v20';
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

async function sesionSintetica(page: Page, status: 200 | 403) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1502m0405-synthetic-not-real-jwt');
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
        ? { success: true, data: { nombreUsuario: 'Admin', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'C1502_PRIVATE_ERROR_SHOULD_NOT_RENDER' })
    });
  });
}

test('C15-02.M04.05 roles, encabezados y tarjetas semánticas', async ({ page }) => {
  await sesionSintetica(page, 200);
  await page.goto(ruta);
  const vista = page.getByTestId('c1502-review');
  await expect(vista.getByRole('heading', { level: 1, name: 'Revisión de arquitectura e integraciones' })).toBeVisible();
  await expect(vista.getByRole('heading', { level: 2, name: 'Superficies por contrastar' })).toBeVisible();
  await expect(vista.getByRole('listitem')).toHaveCount(5);
  await expect(vista.getByRole('heading', { level: 3 })).toHaveCount(5);
  await expect(vista.getByText('NO_VERIFICADO')).toHaveCount(5);
  await expect(vista.locator('button,form,input,select,textarea')).toHaveCount(0);
});

test('C15-02.M04.05 Tab y Enter reales alcanzan navegación con foco visible', async ({ page }) => {
  await sesionSintetica(page, 200);
  await page.goto(ruta);
  await expect(page.getByTestId('c1502-review')).toBeVisible();
  let alcanzado = false;
  for (let i = 0; i < 80; i++) {
    await page.keyboard.press('Tab');
    const destino = await page.evaluate(() => {
      const activo = document.activeElement;
      return activo?.tagName === 'A' && activo.getAttribute('aria-label') === 'Volver a plataforma';
    });
    if (destino) { alcanzado = true; break; }
  }
  expect(alcanzado, 'Enlace alcanzable con Tab sin foco programático').toBe(true);
  const estilo = await page.evaluate(() => getComputedStyle(document.activeElement!).outlineStyle);
  expect(estilo, 'Indicador de foco visible').not.toBe('none');
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/\/plataforma$/);
});

test('C15-02.M04.05 responsive, target táctil y no mutaciones', async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await sesionSintetica(page, 200);
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  await page.goto(ruta);
  const enlace = page.getByRole('link', { name: 'Volver a plataforma' });
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await expect(enlace).toBeVisible();
    const caja = await enlace.boundingBox();
    expect(caja?.height ?? 0, 'Alto mínimo de objetivo táctil').toBeGreaterThanOrEqual(44);
    const overflow = await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, 'Desbordamiento horizontal a ' + width).toBeLessThanOrEqual(2);
  }
  expect(writes, 'Lectura privada sin HTTP mutante').toEqual([]);
});

test('C15-02.M04.05 403 no expone inventario ni detalles privados', async ({ page }) => {
  await sesionSintetica(page, 403);
  await page.goto(ruta);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByTestId('architecture-item')).toHaveCount(0);
  await expect(page.getByText('C1502_PRIVATE_ERROR_SHOULD_NOT_RENDER')).toHaveCount(0);
});
