import { test, expect, type Page } from '@playwright/test';

// C00-03.M04.06: densidad y espera controlada de la superficie cobertura.
// Backend de sesion sintetico SOLAMENTE para probar la UI; no es JWT real.
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function tokenLocal(page: Page): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c00-03-density-not-real-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
}
async function allowMockSession(page: Page): Promise<void> {
  await page.route(sessionUrl, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    await route.fulfill({
      status: 200, contentType: 'application/json', headers: cors,
      body: JSON.stringify({ success: true, data: {
        nombreUsuario: 'Cobertura UI simulada', rolNombre: 'Plataforma simulada',
        ambito: 'Plataforma', esAdministrador: true
      } })
    });
  });
}
test.describe('C00-03.M04.06 - densidad y progreso seguro cobertura V5', () => {
  test.describe.configure({ retries: 0 });

  test('una sola matriz vacia, sin datos institutionales ni widgets operativos', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await tokenLocal(page);
    await allowMockSession(page);
    const unwanted: string[] = [];
    await page.route(/\/(?:plan-maestro|bitacora|cola|v5\/claims|directorio)(?:\/|\?|$)/i, route => {
      unwanted.push(route.request().url());
      return route.abort();
    });
    await page.goto('/plataforma/cobertura-v5');
    await expect(page.getByRole('heading', { name: 'Cadena de trazabilidad' })).toBeVisible();
    await expect(page.locator('.v5-card')).toHaveCount(1);
    await expect(page.getByRole('table')).toHaveCount(1);
    await expect(page.getByRole('columnheader')).toHaveCount(5);
    await expect(page.getByText('Sin datos de cobertura verificados', { exact: false })).toBeVisible();
    await expect(page.locator('app-plataforma-gestion-mvp')).toHaveCount(0);
    await expect(page.locator('.directory-card')).toHaveCount(0);
    expect(unwanted).toEqual([]);
  });

  test('espera visible, boton deshabilitado y una unica solicitud de revalidacion', async ({ page }) => {
    await tokenLocal(page);
    await allowMockSession(page);
    await page.goto('/plataforma/cobertura-v5');
    const refresh = page.getByRole('button', { name: /Revalidar sesión/ });
    await expect(refresh).toBeEnabled();
    let resolveRequest: (() => void) | undefined;
    const requests: string[] = [];
    await page.route(sessionUrl, async route => {
      if (route.request().method() === 'OPTIONS') {
        await route.fulfill({ status: 204, headers: cors });
        return;
      }
      requests.push(route.request().method());
      await new Promise<void>(resolve => { resolveRequest = resolve; });
      await route.fulfill({
        status: 200, contentType: 'application/json', headers: cors,
        body: JSON.stringify({ success: true, data: {
          nombreUsuario: 'Cobertura UI simulada', rolNombre: 'Plataforma simulada',
          ambito: 'Plataforma', esAdministrador: true
        } })
      });
    });
    await refresh.click();
    await expect(refresh).toBeDisabled();
    await expect(refresh.locator('mat-spinner')).toBeVisible();
    await expect.poll(() => Boolean(resolveRequest)).toBe(true);
    expect(requests).toEqual(['GET']);
    resolveRequest?.();
    await expect(refresh).toBeEnabled();
    await expect(refresh.locator('mat-spinner')).toHaveCount(0);
    expect(requests).toEqual(['GET']);
  });
});
