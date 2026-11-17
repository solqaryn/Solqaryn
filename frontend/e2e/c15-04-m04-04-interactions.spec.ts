import { test, expect, type Page } from '@playwright/test';

// C15-04.M04.04: Angular/Chromium real; session response synthetic, NOT live JWT/RBAC/tenant proof.
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
const allowed = { success: true, data: {
  nombreUsuario: 'UX synthetica', rolNombre: 'Plataforma synthetic',
  ambito: 'Plataforma', esAdministrador: true
} };
async function syntheticSession(page: Page, permitted = true) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1504-m0404-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(sessionUrl, async handler => {
    if (handler.request().method() === 'OPTIONS') {
      await handler.fulfill({ status: 204, headers: cors }); return;
    }
    await handler.fulfill({
      status: permitted ? 200 : 403, headers: cors, contentType: 'application/json',
      body: JSON.stringify(permitted ? allowed : {
        success: false, message: 'SECRET_TENANT_B_MUST_NOT_DISPLAY'
      })
    });
  });
}
test.describe('C15-04.M04.04 UX formularios navegacion mensajes doble accion', () => {
  test.describe.configure({ retries: 0 });

  test('anonimo y 403 no muestran la superficie ni datos ajenos', async ({ page }) => {
    const writes: string[] = [];
    page.on('request', r => {
      if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method());
    });
    await page.goto('/plataforma/gobierno-cambios-v5');
    await expect(page).toHaveURL(/\/login/);
    await expect(page.locator('.steps li')).toHaveCount(0);
    await syntheticSession(page, false);
    await page.goto('/plataforma/gobierno-cambios-v5');
    await expect(page).toHaveURL(/\/login/);
    await expect(page.locator('.steps li')).toHaveCount(0);
    await expect(page.getByText('SECRET_TENANT_B_MUST_NOT_DISPLAY')).toHaveCount(0);
    expect(writes).toEqual([]);
  });

  test('login: vacio, usuario 51 y clave 129 deshabilitan envio sin POST', async ({ page }) => {
    const writes: string[] = [];
    page.on('request', r => {
      if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method());
    });
    await page.goto('/login');
    const user = page.locator('input[formcontrolname="nombreUsuario"]');
    const pass = page.locator('input[formcontrolname="password"]');
    const submit = page.getByRole('button', { name: 'Ingresar al sistema' });
    await expect(submit).toBeDisabled();
    await user.fill('x'.repeat(51));
    await pass.fill('password-test-not-real');
    await expect(submit).toBeDisabled();
    await user.fill('usuario-sintetico');
    await expect(submit).toBeEnabled();
    await pass.fill('p'.repeat(129));
    await expect(submit).toBeDisabled();
    await pass.fill('password-test-not-real');
    await expect(submit).toBeEnabled();
    await user.fill('');
    await user.blur();
    await expect(page.getByText('Ingresa tu nombre de usuario.')).toBeVisible();
    await pass.fill('');
    await pass.blur();
    await expect(page.getByText('Ingresa tu contraseña.')).toBeVisible();
    await expect(submit).toBeDisabled();
    expect(writes).toEqual([]);
  });

  test('Tab real alcanza retorno etiquetado y Enter navega sin escritura', async ({ page }) => {
    await syntheticSession(page, true);
    const writes: string[] = [];
    page.on('request', r => {
      if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method());
    });
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto('/plataforma/gobierno-cambios-v5');
    await expect(page.getByRole('heading', { name: 'Decisiones y controles V5' })).toBeVisible();
    const link = page.getByRole('link', { name: 'Volver a administración de plataforma' });
    let focused = false;
    for (let n = 0; n < 80; n++) {
      await page.keyboard.press('Tab');
      if (await link.evaluate(el => document.activeElement === el)) {
        focused = true; break;
      }
    }
    expect(focused).toBe(true);
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/plataforma(?:$|[?#])/);
    expect(writes).toEqual([]);
  });

  test('espera segura: un solo GET incluso tras accion repetida', async ({ page }) => {
    await syntheticSession(page);
    await page.goto('/plataforma/gobierno-cambios-v5');
    await expect(page.locator('.steps li')).toHaveCount(4);
    let release!: () => void;
    const pending = new Promise<void>(resolve => { release = resolve; });
    let requests = 0;
    await page.route(sessionUrl, async handler => {
      if (handler.request().method() === 'OPTIONS') {
        await handler.fulfill({ status: 204, headers: cors }); return;
      }
      requests++;
      await pending;
      await handler.fulfill({ status: 200, headers: cors, contentType: 'application/json',
        body: JSON.stringify(allowed) });
    });
    const button = page.getByRole('button', { name: 'Revalidar sesión de plataforma' });
    try {
      await button.click();
      await expect(button).toBeDisabled();
      await expect(page.locator('.state-banner [role="status"]')).toContainText('Verificando permisos');
      await expect(page.locator('.steps li')).toHaveCount(0);
      await expect.poll(() => requests).toBe(1);
      await button.evaluate(el => el.dispatchEvent(new MouseEvent('click', { bubbles: true })));
      expect(requests).toBe(1);
    } finally { release(); }
    await expect(button).toBeEnabled();
    await expect(page.locator('.steps li')).toHaveCount(4);
    expect(requests).toBe(1);
  });

  test('503 opaco y reintento exitoso: sin contenido protegido ni writes', async ({ page }) => {
    await syntheticSession(page);
    await page.goto('/plataforma/gobierno-cambios-v5');
    let fail = true, requests = 0;
    const writes: string[] = [];
    page.on('request', r => {
      if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method());
    });
    await page.route(sessionUrl, async handler => {
      if (handler.request().method() === 'OPTIONS') {
        await handler.fulfill({ status: 204, headers: cors }); return;
      }
      requests++;
      await handler.fulfill({ status: fail ? 503 : 200, headers: cors, contentType: 'application/json',
        body: JSON.stringify(fail ? { success: false, message: 'SECRET_PRIVATE_DSN' } : allowed) });
    });
    const button = page.getByRole('button', { name: 'Revalidar sesión de plataforma' });
    await button.click();
    await expect(page.getByRole('alert')).toContainText('No fue posible verificar la sesión');
    await expect(page.locator('.steps li')).toHaveCount(0);
    await expect(page.getByText('SECRET_PRIVATE_DSN')).toHaveCount(0);
    fail = false;
    await button.click();
    await expect(page.locator('.steps li')).toHaveCount(4);
    await expect(page.locator('.state-banner [role="status"]')).toContainText('revalidada correctamente');
    expect(requests).toBe(2);
    expect(writes).toEqual([]);
  });
});
