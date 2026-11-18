import { expect, test, type Page } from '@playwright/test';

// Solo navegador/Angular localhost y sesion sintetica. NO prueba JWT firmado ni MySQL.
const url = '/plataforma/capacidades-estados-v19';

async function simularSesion(page: Page, status: 200 | 403) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  let consultas = 0;
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: {
        'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
        'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
        'Access-Control-Allow-Methods': 'GET, OPTIONS'
      } });
      return;
    }
    consultas++;
    await route.fulfill({ status, contentType: 'application/json',
      headers: { 'Access-Control-Allow-Origin': 'http://127.0.0.1:4200' },
      body: JSON.stringify(status === 200
        ? { success: true, data: {
          nombreUsuario: 'Synthetic QA', rolNombre: 'Plataforma',
          ambito: 'Plataforma', esAdministrador: true
        } }
        : { success: false, message: 'PRIVATE_TENANT_B_SECRET_NOT_DISPLAY' })
    });
  });
  return { consultas: () => consultas };
}

test.describe('C01-02.M04.02 - estados de cliente en Chromium', () => {
  test.describe.configure({ retries: 0 });
  test('anonimo no entra ni ve las rutas protegidas', async ({ page }) => {
    await page.goto(url);
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.getByTestId('estados-listado')).toHaveCount(0);
  });
  test('403 sintetico no filtra datos internos ni concede la vista', async ({ page }) => {
    const probe = await simularSesion(page, 403);
    await page.goto(url);
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    expect(probe.consultas()).toBeGreaterThan(0);
    await expect(page.getByTestId('estados-listado')).toHaveCount(0);
    await expect(page.getByText('PRIVATE_TENANT_B_SECRET_NOT_DISPLAY')).toHaveCount(0);
  });
  test('sesion global sintetica sin tenant verificado: SIN_TENANT, cero mutaciones, 320px', async ({ page }) => {
    const probe = await simularSesion(page, 200);
    await page.setViewportSize({ width: 320, height: 740 });
    const escrituras: string[] = [];
    page.on('request', request => {
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method())) escrituras.push(request.method());
    });
    await page.goto(url);
    await expect(page.getByRole('heading', { name: 'Estado de capacidades y permisos' })).toBeVisible();
    await expect(page.getByTestId('estado-sin-tenant')).toBeVisible();
    await expect(page.getByTestId('estado-exito')).toHaveCount(0);
    await expect(page.getByTestId('estados-listado')).toHaveCount(0);
    expect(probe.consultas()).toBeGreaterThan(0);
    expect(escrituras).toEqual([]);
    expect(await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth
    )).toBeLessThanOrEqual(2);
  });
});
