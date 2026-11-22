import { expect, test } from '@playwright/test';

// C01-03.M04.04 R2: signed JWT + physical MySQL multi-company mode must
// remain READ ONLY. No mock permissions or write access to company B.
const api = process.env['C1504_API_URL'];
const password = process.env['C1504_TEST_PASSWORD'];
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !password) {
  throw new Error('M04.04 requires GitHub-hosted signed JWT MySQL 8.4 fixture.');
}

test('multi-company signed JWT keeps Configuracion:Editar denied in DOM and API', async ({ page }) => {
  test.setTimeout(90_000);
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill('c1504_admin_a');
  await page.locator('input[formcontrolname="password"]').fill(password!);
  const pending = page.waitForResponse(r => r.url().endsWith('/auth/login') && r.request().method() === 'POST');
  await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
  const response = await pending;
  expect(response.status()).toBe(200);
  const token: unknown = (await response.json())?.data?.token;
  expect(typeof token).toBe('string');
  expect((token as string).split('.')).toHaveLength(3);

  const policy = await page.request.get(api + '/permisos/mis-permisos/empresa/1', {
    headers: { Authorization: 'Bearer ' + token }
  });
  expect(policy.status()).toBe(200);
  const granted = (await policy.json())?.data;
  expect(granted?.operacionMultiEmpresaRestringida).toBe(true);
  expect(granted?.capacidadesHabilitadas).not.toContain('Configuracion:Editar');

  await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '1'));
  await page.goto('/configuracion/dinamica-v5');
  const form = page.getByRole('form', { name: 'Configuración del tenant activo' });
  await expect(form).toBeVisible();
  await expect(form.getByRole('button', { name: 'Guardar configuración tenant' })).toHaveCount(0);
  await expect(page.getByText(/Vista de solo lectura/)).toBeVisible();
  await expect(form.locator('input[formcontrolname="nombre"]')).toBeDisabled();

  let mutationRequests = 0;
  page.on('request', request => {
    if (request.method() === 'PUT' && /\/empresa-configuracion\/tenant\//.test(request.url())) {
      mutationRequests++;
    }
  });
  await form.evaluate(el => el.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true })));
  expect(mutationRequests, 'read-only policy must prevent even synthetic DOM submission').toBe(0);

  const foreign = await page.request.get(api + '/empresa-configuracion/tenant/2', {
    headers: { Authorization: 'Bearer ' + token, 'X-Empresa-Id': '2' }
  });
  expect([403, 404]).toContain(foreign.status());
});
