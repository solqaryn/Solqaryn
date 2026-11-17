import { expect, test, type Page } from '@playwright/test';

// C05-01.M05.05 adversarial sobre Angular local con endpoint de sesión sintético.
// No acredita middleware JWT, RBAC, revocación o aislamiento de tenants en backend.
async function sesionAdversarial(page: Page, status: number, data: unknown, mensaje = 'PRIVATE_CATALOG_INTERNAL') {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0501m0505-forged-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers });
      return;
    }
    await route.fulfill({
      status,
      headers,
      contentType: 'application/json',
      body: JSON.stringify({ success: status === 200, data, message: mensaje })
    });
  });
}

async function exigirDenegacion(page: Page) {
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('details.design-system-preview')).toHaveCount(0);
  await expect(page.locator('.design-catalog .token-item, .design-catalog .brand-choice')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_CATALOG_INTERNAL')).toHaveCount(0);
}

test('C05-01.M05.05 respuesta 200 forjada como empresa no concede acceso global', async ({ page }) => {
  await sesionAdversarial(page, 200, {
    nombreUsuario: 'Admin Empresa', rolNombre: 'Admin', ambito: 'Empresa', esAdministrador: true
  });
  await exigirDenegacion(page);
});

test('C05-01.M05.05 respuesta 200 sin permiso de administrador no revela tokens', async ({ page }) => {
  await sesionAdversarial(page, 200, {
    nombreUsuario: 'Usuario Plataforma', rolNombre: 'Lector', ambito: 'Plataforma', esAdministrador: false
  });
  await exigirDenegacion(page);
});

test('C05-01.M05.05 403 con diagnóstico privado permanece opaco y no ejecuta escrituras', async ({ page }) => {
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  await sesionAdversarial(page, 403, null);
  await exigirDenegacion(page);
  expect(writes, 'rechazo 403 no desencadena mutaciones por la vista').toEqual([]);
});
