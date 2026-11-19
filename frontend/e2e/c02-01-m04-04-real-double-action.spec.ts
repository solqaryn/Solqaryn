import { expect, test, type Page } from '@playwright/test';

// C02-01.M04.04: ninguna interceptacion simula respuestas JWT/API/MySQL.
// El retraso de route.continue() preserva la llamada autentica al backend del runner.
const api = process.env['C1504_API_URL'];
const password = process.env['C1504_TEST_PASSWORD'];
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !password) {
  throw new Error('C02-01.M04.04 exige API .NET10, JWT firmado y MySQL 8.4 efimero.');
}

async function ingresar(page: Page, usuario: string): Promise<string> {
  await page.addInitScript(() => localStorage.removeItem('solqaryn_empresa_solicitada_id'));
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill(usuario);
  await page.locator('input[formcontrolname="password"]').fill(password!);
  const pending = page.waitForResponse(response =>
    response.url().endsWith('/auth/login') && response.request().method() === 'POST');
  await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
  const response = await pending;
  expect(response.status()).toBe(200);
  const jwt: unknown = (await response.json())?.data?.token;
  expect(typeof jwt).toBe('string');
  expect((jwt as string).split('.')).toHaveLength(3);
  return jwt as string;
}

test.describe('V21 C02-01.M04.04 — doble accion autentica tenant', () => {
  test.describe.configure({ retries: 0 });

  test('N2: doble submit durante GET real confirma solo una vez, sin error ni bucle', async ({ page }) => {
    test.setTimeout(150_000);
    const jwt = await ingresar(page, 'c0201_admin_ab');
    const memberships = await page.request.get(api + '/tenant-context/empresas', {
      headers: { Authorization: 'Bearer ' + jwt }
    });
    expect(memberships.status()).toBe(200);
    expect((await memberships.json()).data.map((e: { empresaId: number }) => e.empresaId).sort())
      .toEqual([1, 2]);

    const selector = page.locator('app-tenant-selector');
    const select = selector.locator('select#empresa-id');
    await expect(select).toBeVisible();
    await expect(select.locator('option')).toHaveCount(3);
    await select.selectOption({ label: 'Empresa A efimera C15' });

    let selectionGets = 0;
    page.on('request', req => {
      if (req.method() === 'GET' && /\/tenant-context\/1(?:\?|$)/.test(req.url())) selectionGets++;
    });
    await page.route(/\/tenant-context\/1(?:\?|$)/, async route => {
      if (route.request().method() === 'GET') {
        await new Promise(resolve => setTimeout(resolve, 850));
      }
      await route.continue(); // NO fulfill() ni mock: API real firmada.
    });

    const confirmation = page.waitForResponse(response =>
      response.request().method() === 'GET' && /\/tenant-context\/1(?:\?|$)/.test(response.url()));
    await selector.locator('button[type="submit"]').click();
    await expect(selector.getByText('Verificando acceso con el servidor…')).toBeVisible();
    // Simula teclado/automatizacion que reenvia el formulario mientras el GET sigue pendiente.
    await selector.locator('form').evaluate(form => {
      form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    });
    expect(selectionGets, 'segunda accion no crea segundo GET ni pierde el contexto').toBe(1);
    await expect(selector.getByText('Selecciona una empresa de tus membresías activas.')).toHaveCount(0);
    await expect(page).toHaveURL(/\/login(?:[?#]|$)/);
    const accepted = await confirmation;
    expect(accepted.status()).toBe(200);
    expect((await accepted.json())?.data?.empresaId).toBe(1);
    await expect(page).not.toHaveURL(/\/login(?:[?#]|$)/, { timeout: 30_000 });
    await expect(page.getByText('Selecciona una empresa de tus membresías activas.')).toHaveCount(0);
  });

  test('N2: submit sin empresa elegida falla cerrado y no consulta tenant ajeno', async ({ page }) => {
    test.setTimeout(150_000);
    await ingresar(page, 'c0201_admin_ab');
    const selector = page.locator('app-tenant-selector');
    await expect(selector.locator('select#empresa-id')).toBeVisible();
    await expect(selector.locator('select#empresa-id option')).toHaveCount(3);
    let contextGets = 0;
    page.on('request', req => {
      if (req.method() === 'GET' && /\/tenant-context\/\d+(?:\?|$)/.test(req.url())) contextGets++;
    });
    await expect(selector.locator('button[type="submit"]')).toBeDisabled();
    await selector.locator('form').evaluate(form => {
      form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    });
    await expect(selector.getByRole('alert')).toContainText('Selecciona una empresa');
    expect(contextGets, 'sin empresa valida no existe seleccion autenticada').toBe(0);
    await expect(page).toHaveURL(/\/login(?:[?#]|$)/);
  });

  test('JWT A: forzar tenant B desde cliente produce 403/404 real sin elevar sesion', async ({ page }) => {
    test.setTimeout(150_000);
    const jwt = await ingresar(page, 'c1504_admin_a');
    const denied = await page.request.get(api + '/tenant-context/2', {
      headers: { Authorization: 'Bearer ' + jwt, 'X-Empresa-Id': '2' }
    });
    expect([403, 404], 'JWT A jamas habilita empresa B').toContain(denied.status());
    await expect(page.getByRole('button', { name: 'Cambiar de empresa sin cerrar sesión' })).toHaveCount(0);
  });
});
