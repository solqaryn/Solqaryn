import { expect, test, type Page } from '@playwright/test';

// C01-03.M04.04: real server/login/GET with a controlled slow PUT response.
// Simulated 409 only tests UI concurrency: never count it as real backend CAS evidence.
const api = process.env['C1504_API_URL'];
const password = process.env['C1504_TEST_PASSWORD'];
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !password) {
  throw new Error('M04.04 requires GitHub-hosted MySQL/JWT/Chromium fixture.');
}
const target = '/configuracion/dinamica-v5';
const formSelector = 'form[aria-label="Configuración del tenant activo"]';

async function loginA(page: Page): Promise<string> {
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill('c1504_admin_a');
  await page.locator('input[formcontrolname="password"]').fill(password!);
  const pending = page.waitForResponse(r => r.url().endsWith('/auth/login') && r.request().method() === 'POST');
  await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
  const res = await pending;
  expect(res.status()).toBe(200);
  const token: unknown = (await res.json())?.data?.token;
  expect(typeof token).toBe('string');
  expect((token as string).split('.')).toHaveLength(3);
  // Real authenticated policy, not a forced UI flag or fake permission.
  const policy = await page.request.get(api + '/permisos/mis-permisos/empresa/1', {
    headers: { Authorization: 'Bearer ' + token }
  });
  expect(policy.status()).toBe(200);
  const granted = (await policy.json())?.data;
  expect(granted?.operacionMultiEmpresaRestringida).toBe(false);
  expect(granted?.esAdministrador).toBe(true);
  expect(granted?.permisos).toContain('Configuracion:Editar');
  const membership = await page.request.get(api + '/tenant-context/1', {
    headers: { Authorization: 'Bearer ' + token }
  });
  expect(membership.status()).toBe(200);
  await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '1'));
  await page.goto(target);
  await expect(page.locator(formSelector)).toBeVisible();
  return token as string;
}

test.describe('V24 M04.04 form: real GET, guarded duplicate mutation, error and navigation', () => {
  test.describe.configure({ retries: 0 });

  test('double submit under an outstanding PUT sends precisely one request and recovers from conflict', async ({ page }) => {
    test.setTimeout(180_000);
    await loginA(page);
    let putCount = 0;
    await page.route('**/empresa-configuracion/tenant/1', async route => {
      if (route.request().method() !== 'PUT') {
        await route.continue();
        return;
      }
      putCount++;
      await new Promise(resolve => setTimeout(resolve, 900));
      await route.fulfill({ status: 409, contentType: 'application/json', body: '{}' });
    });
    const form = page.locator(formSelector);
    const button = page.getByRole('button', { name: 'Guardar configuración tenant' });
    await button.click();
    await expect(form).toHaveAttribute('aria-busy', 'true');
    await form.evaluate(el => el.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true })));
    await expect(page.getByText(/La configuración cambió en otra sesión/)).toBeVisible();
    expect(putCount, 'no duplicate PUT even with bypassed disabled button').toBe(1);
    await expect(button).toBeDisabled();
    await page.getByRole('button', { name: 'Recargar versión' }).click();
    await expect(form).toHaveAttribute('aria-busy', 'false');
    await expect(page.getByText(/La configuración cambió en otra sesión/)).toHaveCount(0);
  });

  test('invalid JSON is rejected client-side before any PUT; navigation stays guarded', async ({ page }) => {
    await loginA(page);
    let putCount = 0;
    await page.route('**/empresa-configuracion/tenant/1', async route => {
      if (route.request().method() === 'PUT') putCount++;
      await route.continue();
    });
    const form = page.locator(formSelector);
    await page.locator('textarea[formcontrolname="impuestosJson"]').fill('{invalid');
    await form.evaluate(el => el.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true })));
    await expect(page.getByText('La política de impuestos debe ser JSON válido.')).toBeVisible();
    expect(putCount, 'invalid JSON must not reach API').toBe(0);
    await expect(page.getByRole('link', { name: 'Ver todos los ajustes' })).toHaveAttribute('href', '/configuracion');
    await page.getByRole('link', { name: 'Ver todos los ajustes' }).click();
    await expect(page).toHaveURL(/\/configuracion$/);
    await page.keyboard.press('Tab');
    expect(await page.evaluate(() => document.activeElement?.tagName)).not.toBe('BODY');
  });
  for (const httpStatus of [403, 500] as const) {
    test('mutation HTTP ' + httpStatus + ' never renders untrusted backend details', async ({ page }) => {
      await loginA(page);
      const sentinel = 'PRIVATE_SERVER_STACK_SQL_TENANT_B_JWT_NEVER_RENDER';
      let capturedPuts = 0;
      await page.route('**/empresa-configuracion/tenant/1', async route => {
        if (route.request().method() !== 'PUT') {
          await route.continue();
          return;
        }
        capturedPuts++;
        await route.fulfill({
          status: httpStatus,
          contentType: 'application/json',
          body: JSON.stringify({
            message: sentinel,
            stackTrace: 'SqlException: ' + sentinel,
            tenantId: 2
          })
        });
      });
      const save = page.getByRole('button', { name: 'Guardar configuración tenant' });
      await expect(save).toBeEnabled();
      await save.click();
      await expect(page.getByText('No se pudo guardar la configuración del tenant activo.', { exact: true }))
        .toBeVisible();
      await expect(page.locator('body')).not.toContainText(sentinel);
      await expect(page.getByRole('form', { name: 'Configuración del tenant activo' })).toHaveCount(0);
      expect(capturedPuts, 'the real authenticated edit flow must issue exactly one PUT').toBe(1);
    });
  }


  test('real signed-JWT MySQL stale version is denied and original config restored', async ({ page }) => {
    test.setTimeout(180_000);
    const token = await loginA(page);
    const headers = { Authorization: 'Bearer ' + token };
    const endpoint = api + '/empresa-configuracion/tenant/1';
    const initial = await page.request.get(endpoint, { headers });
    expect(initial.status()).toBe(200);
    const prior = (await initial.json())?.data as {
      empresaId: number; nombre: string; rtn: string | null; direccion: string | null;
      moneda: string; zonaHoraria: string; impuestosJson: string; emisionJson: string;
      correoRemitente: string | null; correoNombreRemitente: string | null; version: number;
    };
    expect(prior.empresaId).toBe(1);
    expect(Number.isInteger(prior.version)).toBe(true);
    const original = {
      nombre: prior.nombre, rtn: prior.rtn, direccion: prior.direccion,
      moneda: prior.moneda, zonaHoraria: prior.zonaHoraria,
      impuestosJson: prior.impuestosJson, emisionJson: prior.emisionJson,
      correoRemitente: prior.correoRemitente, correoNombreRemitente: prior.correoNombreRemitente,
      version: prior.version
    };
    let modified = false;
    try {
      const newName = prior.nombre.slice(0, 145) + ' CI-CAS-real';
      const first = await page.request.put(endpoint, { headers, data: { ...original, nombre: newName } });
      modified = first.status() === 200;
      expect(first.status(), 'authorized PUT with fresh version').toBe(200);
      const stale = await page.request.put(endpoint, {
        headers, data: { ...original, nombre: newName + '-stale' }
      });
      expect([409, 412], 'real server must deny stale version; not simulated HTTP 409')
        .toContain(stale.status());
      const after = await page.request.get(endpoint, { headers });
      expect(after.status()).toBe(200);
      const afterData = (await after.json())?.data;
      expect(afterData.empresaId).toBe(1);
      expect(afterData.nombre).toBe(newName);
      expect(afterData.version).toBeGreaterThan(prior.version);
      const foreign = await page.request.get(api + '/empresa-configuracion/tenant/2', { headers });
      expect([403, 404], 'signed company A JWT cannot read B').toContain(foreign.status());
    } finally {
      if (modified) {
        const current = await page.request.get(endpoint, { headers });
        expect(current.status()).toBe(200);
        const persisted = (await current.json())?.data;
        const restore = await page.request.put(endpoint, {
          headers, data: { ...original, version: persisted.version }
        });
        expect(restore.status(), 'compensating versioned restore in ephemeral MySQL').toBe(200);
        const readback = await page.request.get(endpoint, { headers });
        expect(readback.status()).toBe(200);
        const restored = (await readback.json())?.data;
        expect(restored.nombre).toBe(prior.nombre);
        expect(restored.empresaId).toBe(1);
      }
    }
  });

  for (const injected of ['foreign-tenant', 'missing-data'] as const) {
    test('logo POST rejects HTTP 200 response with ' + injected + ' and no false success', async ({ page }) => {
      test.setTimeout(180_000);
      const token = await loginA(page); // Real signed-JWT + persisted editor grant, not a mocked login.
      const currentResponse = await page.request.get(api + '/empresa-configuracion/tenant/1', {
        headers: { Authorization: 'Bearer ' + token }
      });
      expect(currentResponse.status()).toBe(200);
      const originalTenant = (await currentResponse.json())?.data;
      expect(originalTenant?.empresaId).toBe(1);
      const sentinel = 'FOREIGN_TENANT_B_NEVER_RENDER_OR_ACKNOWLEDGE';
      let intercepted = 0;
      // Only the logo POST response is injected; login, permission and GET are real.
      // This is a client fail-closed negative, NOT backend tenant-isolation certification.
      await page.route('**/empresa-configuracion/tenant/1/logo', async route => {
        if (route.request().method() !== 'POST') {
          await route.continue();
          return;
        }
        intercepted++;
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            data: injected === 'foreign-tenant'
              ? { ...originalTenant, empresaId: 2, nombre: sentinel }
              : null
          })
        });
      });
      await page.locator('input[type="file"][accept="image/*"]').setInputFiles({
        name: 'isolated-test.png',
        mimeType: 'image/png',
        buffer: Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO9dG4kAAAAASUVORK5CYII=', 'base64')
      });
      await expect(page.locator('[data-config-state="invalid"]')).toBeVisible();
      await expect(page.getByRole('alert')).toContainText(
        'La respuesta no corresponde a la empresa verificada.'
      );
      await expect(page.locator('body')).not.toContainText(sentinel);
      await expect(page.getByText('Logo del tenant actualizado.', { exact: true })).toHaveCount(0);
      expect(intercepted, 'upload must issue only one POST to the current tenant').toBe(1);
    });
  }

});
