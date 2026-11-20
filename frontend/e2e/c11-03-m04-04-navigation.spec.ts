import { expect, test, type Page } from '@playwright/test';

const ruta = '/plataforma/eventos-outbox-v22';
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

async function sesionDePrueba(page: Page, estado: 200 | 403, permiso = true): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'C11_M0404_TOKEN_SYNTHETIC_NOT_SIGNED');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    await route.fulfill({
      status: estado, headers: cors, contentType: 'application/json',
      body: JSON.stringify(estado === 200
        ? { success: true, data: {
            nombreUsuario: 'SENSITIVE_USERNAME_DO_NOT_RENDER',
            rolNombre: 'Plataforma', ambito: permiso ? 'Plataforma' : 'Empresa',
            esAdministrador: permiso
          } }
        : { success: false, message: 'SENSITIVE_STACK_TRACE_DO_NOT_RENDER' })
    });
  });
}

test('C11-03.M04.04: navegación anónima y 403 no muestran panel ni traza', async ({ page }) => {
  await page.goto(ruta);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByTestId('c1103-outbox-private')).toHaveCount(0);
  await sesionDePrueba(page, 403);
  await page.goto(ruta);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByTestId('c1103-outbox-private')).toHaveCount(0);
  await expect(page.getByText('SENSITIVE_STACK_TRACE_DO_NOT_RENDER')).toHaveCount(0);
});

test('C11-03.M04.04: ruta de vuelta repetida sin formulario ni doble acción HTTP', async ({ page }) => {
  await sesionDePrueba(page, 200);
  const mutaciones: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method()))
      mutaciones.push(request.method() + ' ' + request.url());
  });
  for (let intento = 0; intento < 2; intento++) {
    await page.goto(ruta);
    const panel = page.getByTestId('c1103-outbox-private');
    await expect(panel).toBeVisible();
    await expect(panel.getByRole('status')).toContainText('SIN_FUENTE_CONECTADA');
    await expect(panel.getByText(/no confirma JWT del servidor/)).toBeVisible();
    await expect(panel.locator('form,button,input,select,textarea,[role="button"]')).toHaveCount(0);
    const volver = page.getByRole('link', { name: 'Volver a plataforma' });
    await expect(volver).toHaveCount(1);
    await expect(volver).toHaveAttribute('href', '/plataforma');
    await volver.click();
    await expect(page).toHaveURL(/\/plataforma(?:\/|[?#]|$)/);
  }
  expect(mutaciones, 'dos navegaciones no ejecutan ninguna escritura').toEqual([]);
});

test('C11-03.M04.04: sesión hostil sin rol no expone mensajes privados', async ({ page }) => {
  await sesionDePrueba(page, 200, false);
  await page.goto(ruta);
  await expect(page.getByTestId('c1103-outbox-private')).toHaveCount(0);
  await expect(page.getByText('SENSITIVE_USERNAME_DO_NOT_RENDER')).toHaveCount(0);
  await expect(page.locator('img[onerror], script[src*="SENSITIVE"]')).toHaveCount(0);
});
