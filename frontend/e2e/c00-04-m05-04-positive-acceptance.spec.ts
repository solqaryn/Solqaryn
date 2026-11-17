import { expect, test, type Page } from '@playwright/test';

// C00-04.M05.04: real Chromium positive acceptance, mocked session wire.
// The mocked route is NOT a live JWT, tenant, RBAC or provider certification.
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
const allowed = {
  success: true,
  data: {
    nombreUsuario: 'Test global aislado',
    rolNombre: 'Plataforma',
    ambito: 'Plataforma',
    esAdministrador: true
  }
};

async function arrange(page: Page, platform: boolean = true) {
  const calls: string[] = [];
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'C0004-M0504-SYNTHETIC-NOT-JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(sessionUrl, async route => {
    const method = route.request().method();
    if (method === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    calls.push(method);
    const payload = platform ? allowed : {
      success: true,
      data: {
        nombreUsuario: 'Empresa ajena',
        rolNombre: 'Admin Empresa',
        ambito: 'Empresa',
        esAdministrador: false
      }
    };
    await route.fulfill({
      status: 200, headers: cors, contentType: 'application/json',
      body: JSON.stringify(payload)
    });
  });
  return calls;
}

test.describe('C00-04.M05.04 - aceptacion positiva del gobierno V5', () => {
  test.describe.configure({ retries: 0 });

  test('sesion global valida presenta cuatro controles exactos pero ninguna autoridad de ejecucion', async ({ page }) => {
    const calls = await arrange(page);
    await page.goto('/plataforma/gobierno-cambios-v5');
    await expect(page.getByRole('heading', { level: 1, name: 'Decisiones y controles V5' })).toBeVisible();
    await expect(page.locator('ol.steps > li h3')).toHaveText([
      'Alcance y dependencias',
      'Pruebas dirigidas',
      'Verificación DEV',
      'Revisión independiente A5'
    ]);
    await expect(page.getByRole('note')).toContainText('Contenido estático');
    await expect(page.getByText('Solo lectura · sin autoridad operativa')).toBeVisible();
    await expect(page.locator('form, input, [data-testid="approve"], [data-testid="deploy"]')).toHaveCount(0);
    await expect(page.getByText('Sin datos institucionales conectados', { exact: false }).first()).toBeVisible();
    expect(calls).toEqual(['GET']);
  });

  test('confirmacion positiva de revalidacion no transforma consulta en aprobacion', async ({ page }) => {
    const calls = await arrange(page);
    const methods: string[] = [];
    page.on('request', request => {
      if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method()))
        methods.push(request.method());
    });
    await page.goto('/plataforma/gobierno-cambios-v5');
    await page.getByRole('button', { name: 'Revalidar sesión de plataforma' }).click();
    await expect(page.locator('.state-banner [role="status"]')).toContainText('revalidada correctamente');
    await expect(page.locator('ol.steps > li')).toHaveCount(4);
    await expect(page.getByText('Solo lectura · sin autoridad operativa')).toBeVisible();
    expect(calls).toEqual(['GET', 'GET']);
    expect(methods).toEqual([]);
  });

  test('enlace explicito regresa a administracion de plataforma, no a ejecucion de cambio', async ({ page }) => {
    const calls = await arrange(page);
    await page.goto('/plataforma/gobierno-cambios-v5');
    const back = page.getByRole('link', { name: 'Volver a administración de plataforma' });
    await expect(back).toHaveAttribute('href', '/plataforma');
    await back.click();
    await expect(page).toHaveURL(/\/plataforma(?:\?|$)/);
    expect(calls.every(m => m === 'GET')).toBe(true);
  });

  test('respuesta 200 de administrador empresarial no concede vista global', async ({ page }) => {
    const calls = await arrange(page, false);
    await page.goto('/plataforma/gobierno-cambios-v5');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.locator('ol.steps > li')).toHaveCount(0);
    await expect(page.getByRole('heading', { name: 'Decisiones y controles V5' })).toHaveCount(0);
    expect(calls).toEqual(['GET']);
  });
});
