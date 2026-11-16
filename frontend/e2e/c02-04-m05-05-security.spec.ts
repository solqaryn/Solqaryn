import { expect, test, type Page } from '@playwright/test';

/**
 * C02-04.M05.05: Chromium real; servidor /plataforma/sesion SIMULADO.
 * No acredita JWT HTTP, MFA, IDOR/BOLA de API ni DB/tenant A-B reales.
 */
const endpoint = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
const platform = {
  success: true,
  data: { nombreUsuario: 'Fixture', rolNombre: 'Global', ambito: 'Plataforma', esAdministrador: true }
};
const privateMessage = 'SYNTHETIC_PRIVATE_SECURITY_DIAGNOSTIC';

async function fakeToken(page: Page, role = 'Administrador') {
  await page.addInitScript((rol) => {
    localStorage.setItem('solqaryn_token', 'SYNTHETIC_NOT_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_rol', rol);
    localStorage.setItem('solqaryn_empresa_id', 'EMPRESA_B_FORGED');
  }, role);
}

async function mockSession(
  page: Page, respond: () => { status: 200 | 401 | 403 | 503; body: object }
) {
  let gets = 0;
  await page.route(endpoint, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors }); return;
    }
    gets++;
    const { status, body } = respond();
    await route.fulfill({ status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(body) });
  });
  return () => gets;
}

test.describe('C02-04.M05.05 rechazo y revocación fail-closed (browser con API simulada)', () => {
  test.describe.configure({ retries: 0 });

  test('anónimo sin credenciales no ejecuta GET de sesión ni revela controles', async ({ page }) => {
    const gets = await mockSession(page, () => ({ status: 200, body: platform }));
    await page.goto('/plataforma/consola-global-v5');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.locator('.control-cards li')).toHaveCount(0);
    expect(gets()).toBe(0);
  });

  test('token local caducado no autoriza por rol ni produce GET', async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('solqaryn_token', 'EXPIRED_FIXTURE_NOT_JWT');
      localStorage.setItem('solqaryn_expira_en', '2000-01-01T00:00:00Z');
      localStorage.setItem('solqaryn_rol', 'Administrador');
    });
    const gets = await mockSession(page, () => ({ status: 200, body: platform }));
    await page.goto('/plataforma/consola-global-v5');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.locator('.control-cards li')).toHaveCount(0);
    expect(gets()).toBe(0);
  });

  test('rol administrador de empresa y EmpresaId falsificado no suplantan ámbito plataforma', async ({ page }) => {
    await fakeToken(page);
    const gets = await mockSession(page, () => ({ status: 200,
      body: { success: true, data: { nombreUsuario: 'Tenant Admin', rolNombre: 'Administrador',
        ambito: 'Empresa', esAdministrador: true, empresaId: 'EMPRESA_A' },
        message: privateMessage } }));
    await page.goto('/plataforma/consola-global-v5');
    await expect(page).toHaveURL(/\/login\?plataforma=no-disponible(?:&|$)/);
    await expect(page.locator('.control-cards li')).toHaveCount(0);
    await expect(page.getByText(privateMessage)).toHaveCount(0);
    expect(gets()).toBe(1);
  });

  test('200 sin esAdministrador=true se rechaza aunque ámbito sea Plataforma', async ({ page }) => {
    await fakeToken(page);
    await mockSession(page, () => ({ status: 200, body: {
      success: true, data: { nombreUsuario: 'Invitado', rolNombre: 'Global',
        ambito: 'Plataforma', esAdministrador: false } } }));
    await page.goto('/plataforma/consola-global-v5');
    await expect(page).toHaveURL(/\/login\?plataforma=no-disponible(?:&|$)/);
    await expect(page.locator('.control-cards li')).toHaveCount(0);
  });

  test('200 success=false con datos privilegiados se rechaza sin confiar en data', async ({ page }) => {
    await fakeToken(page);
    await mockSession(page, () => ({ status: 200,
      body: { ...platform, success: false, message: privateMessage } }));
    await page.goto('/plataforma/consola-global-v5');
    await expect(page).toHaveURL(/\/login\?plataforma=no-disponible(?:&|$)/);
    await expect(page.locator('.control-cards li')).toHaveCount(0);
    await expect(page.getByText(privateMessage)).toHaveCount(0);
  });

  for (const status of [401, 403, 503] as const) {
    test('HTTP simulado ' + status + ' falla cerrado y oculta diagnóstico', async ({ page }) => {
      await fakeToken(page);
      const gets = await mockSession(page, () => ({
        status, body: { success: false, data: null, message: privateMessage }
      }));
      await page.goto('/plataforma/consola-global-v5');
      // El interceptor global cierra la sesión ante 401 y redirige a /login
      // sin query; 403 corresponde a denegación de plataforma y 503 a no disponibilidad.
      // En los tres casos no deben mostrarse tarjetas ni diagnósticos.
      await expect(page).toHaveURL(status === 401
        ? /\/login$/
        : status === 403
          ? /\/login\?plataforma=denegado(?:&|$)/
          : /\/login\?plataforma=no-disponible(?:&|$)/);
      await expect(page.locator('.control-cards li')).toHaveCount(0);
      await expect(page.getByText(privateMessage)).toHaveCount(0);
      expect(gets()).toBe(1);
    });
  }

  test('revocación 403 posterior a acceso positivo elimina tarjetas inmediatamente', async ({ page }) => {
    await fakeToken(page);
    let allowed = true;
    const gets = await mockSession(page, () => allowed
      ? { status: 200, body: platform }
      : { status: 403, body: { success: false, data: null, message: privateMessage } });
    await page.goto('/plataforma/consola-global-v5');
    await expect(page.locator('.control-cards li')).toHaveCount(4);
    allowed = false;
    await page.getByRole('button', { name: 'Revalidar sesión global' }).click();
    await expect(page.locator('.control-cards li')).toHaveCount(0);
    await expect(page.getByRole('alert')).toContainText('Permiso global no confirmado');
    await expect(page.getByText(privateMessage)).toHaveCount(0);
    expect(gets()).toBe(2);
  });

  test('sesión positiva de lectura no emite POST/PUT/PATCH/DELETE ni grants', async ({ page }) => {
    await fakeToken(page);
    const writes: string[] = [];
    page.on('request', req => {
      if (!['GET','HEAD','OPTIONS'].includes(req.method())) writes.push(req.method());
    });
    const gets = await mockSession(page, () => ({ status: 200, body: platform }));
    await page.goto('/plataforma/consola-global-v5');
    await expect(page.getByRole('heading', { name: 'Consola global auditada' })).toBeVisible();
    await expect(page.locator('.control-cards li')).toHaveCount(4);
    await expect(page.locator('.control-plane form, .control-plane input')).toHaveCount(0);
    expect(gets()).toBe(1);
    expect(writes).toEqual([]);
  });
});
