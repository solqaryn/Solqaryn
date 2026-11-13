import { test, expect, type Page } from '@playwright/test';

// UI/browser regresion solo contra backend sintetico. La autorizacion real
// sigue bajo /plataforma/sesion en el servidor; esto NO prueba JWT/MySQL real.
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

async function syntheticToken(page: Page): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'v5-browser-ui-test-not-a-real-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
}

async function mockPlatformSession(page: Page, allowed: boolean): Promise<number[]> {
  const observed: number[] = [];
  await page.route(sessionUrl, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    observed.push(allowed ? 200 : 403);
    await route.fulfill({
      status: allowed ? 200 : 403,
      contentType: 'application/json',
      headers: cors,
      body: JSON.stringify(allowed
        ? { success: true, data: {
            nombreUsuario: 'V5 UI synthetic',
            rolNombre: 'Plataforma UI mock',
            ambito: 'Plataforma',
            esAdministrador: true
          } }
        : { success: false, message: 'DENEGADO', data: null })
    });
  });
  return observed;
}

test.describe('V5 C00-02/03 M04.01 - rutas de plataforma en Chromium real', () => {
  test.describe.configure({ retries: 0 });

  test('sin JWT bloquea ambas rutas sin consultar sesion ni dibujar contenido privado', async ({ page }) => {
    const observed = await mockPlatformSession(page, true);
    await page.goto('/plataforma/flujo-v5');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.getByRole('heading', { name: 'De la solicitud a la evidencia DEV' })).toHaveCount(0);
    await page.goto('/plataforma/cobertura-v5');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.getByRole('heading', { name: 'Cadena de trazabilidad' })).toHaveCount(0);
    expect(observed).toEqual([]);
  });

  test('token sintetico mas 403 del servidor bloquea el control plane', async ({ page }) => {
    await syntheticToken(page);
    const observed = await mockPlatformSession(page, false);
    await page.goto('/plataforma/cobertura-v5');
    await expect(page).toHaveURL(/\/login\?plataforma=denegado(?:&|$)/);
    await expect(page.getByRole('heading', { name: 'Cadena de trazabilidad' })).toHaveCount(0);
    expect(observed).toContain(403);
  });

  test('sesion global simulada autorizada muestra flujo estatico sin despacho', async ({ page }) => {
    await syntheticToken(page);
    const observed = await mockPlatformSession(page, true);
    await page.goto('/plataforma/flujo-v5');
    await expect(page.getByRole('heading', { name: 'De la solicitud a la evidencia DEV' })).toBeVisible();
    await expect(page.getByRole('list', { name: 'Secuencia contractual de estados' }).locator('li')).toHaveCount(4);
    await expect(page.getByText('Sin operaciones')).toBeVisible();
    await expect(page.getByText('No consulta PLAN_MAESTRO', { exact: false })).toBeVisible();
    expect(observed).toContain(200);
  });

  test('navegacion responsive a cobertura muestra matriz vacia sin endpoints operativos', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await syntheticToken(page);
    const observed = await mockPlatformSession(page, true);
    const unexpectedCalls: string[] = [];
    await page.route(/\/(?:bitacora|cola|plan-maestro|v5\/claims)(?:\/|\?|$)/i, route => {
      unexpectedCalls.push(route.request().url());
      return route.abort();
    });
    await page.goto('/plataforma/flujo-v5');
    await page.getByRole('link', { name: /Administracion|Administración/ }).click();
    await expect(page).toHaveURL(/\/plataforma$/);
    await page.getByRole('link', { name: /Cobertura V5/ }).click();
    await expect(page).toHaveURL(/\/plataforma\/cobertura-v5$/);
    await expect(page.getByRole('heading', { name: 'Cadena de trazabilidad' })).toBeVisible();
    await expect(page.getByRole('columnheader')).toHaveCount(5);
    await expect(page.getByText('Sin datos de cobertura verificados', { exact: false })).toBeVisible();
    await expect(page.getByText('Sin escritura')).toBeVisible();
    expect(unexpectedCalls).toEqual([]);
    expect(observed.length).toBeGreaterThanOrEqual(2);
  });
});
