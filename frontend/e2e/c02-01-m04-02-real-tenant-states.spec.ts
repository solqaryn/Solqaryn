import { expect, test, type Page } from '@playwright/test';

// C02-01.M04.02: API .NET, JWT firmado, MySQL 8.4 y Angular reales en runner CI.
// No interceptar HTTP, fabricar JWT ni reutilizar fixtures sintéticas.
const api = process.env['C1504_API_URL'];
const password = process.env['C1504_TEST_PASSWORD'];
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !password) {
  throw new Error('La aceptación tenant R2 requiere CI aislada con API y MySQL reales.');
}

async function loginReal(page: Page, username: string): Promise<string> {
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill(username);
  await page.locator('input[formcontrolname="password"]').fill(password!);
  const pending = page.waitForResponse(response =>
    response.url().endsWith('/auth/login') && response.request().method() === 'POST');
  await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
  const response = await pending;
  expect(response.status(), 'Login respaldado por servidor real').toBe(200);
  const jwt: unknown = (await response.json())?.data?.token;
  expect(typeof jwt).toBe('string');
  expect((jwt as string).split('.')).toHaveLength(3);
  return jwt as string;
}

async function assertMembership(page: Page, jwt: string, ownId: number, foreignId: number) {
  const headers = { Authorization: 'Bearer ' + jwt };
  const list = await page.request.get(api + '/tenant-context/empresas', { headers });
  expect(list.status()).toBe(200);
  const data = await list.json();
  expect(data.success).toBe(true);
  expect(Array.isArray(data.data)).toBe(true);
  const ids = (data.data as { empresaId: number }[]).map(item => item.empresaId);
  expect(ids).toContain(ownId);
  expect(ids, 'No filtrar empresa ajena desde JWT real').not.toContain(foreignId);
  const allowed = await page.request.get(api + '/tenant-context/' + ownId, { headers });
  expect(allowed.status()).toBe(200);
  expect((await allowed.json())?.data?.empresaId).toBe(ownId);
  const denied = await page.request.get(api + '/tenant-context/' + foreignId, { headers });
  expect([403, 404]).toContain(denied.status());
}

test.describe('V21 C02-01.M04.02 - membresías y selector con JWT auténtico', () => {
  test.describe.configure({ retries: 0 });

  test('usuario A: solo membresía A, B prohibida, perfil Angular autorizado', async ({ page }) => {
    test.setTimeout(150_000);
    const jwt = await loginReal(page, 'c1504_admin_a');
    await assertMembership(page, jwt, 1, 2);
    await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '1'));
    const pending = page.waitForResponse(r =>
      r.url().includes('/tenant-context/1') && r.request().method() === 'GET');
    await page.goto('/perfil');
    expect((await pending).status()).toBe(200);
    await expect(page.getByRole('heading', { name: 'Mi perfil' })).toBeVisible();
  });

  test('usuario B: solo membresía B, A prohibida, perfil Angular autorizado', async ({ page }) => {
    test.setTimeout(150_000);
    const jwt = await loginReal(page, 'c1504_admin_b');
    await assertMembership(page, jwt, 2, 1);
    await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '2'));
    const pending = page.waitForResponse(r =>
      r.url().includes('/tenant-context/2') && r.request().method() === 'GET');
    await page.goto('/perfil');
    expect((await pending).status()).toBe(200);
    await expect(page.getByRole('heading', { name: 'Mi perfil' })).toBeVisible();
  });

  test('tenant falso B con JWT A: HTTP denegado y perfil privado no se muestra', async ({ page }) => {
    test.setTimeout(150_000);
    await loginReal(page, 'c1504_admin_a');
    await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '2'));
    const denial = page.waitForResponse(r =>
      r.url().includes('/tenant-context/2') && r.request().method() === 'GET');
    await page.goto('/perfil');
    expect([403, 404]).toContain((await denial).status());
    await expect(page).toHaveURL(/\/login(?:[?#]|$)/);
    await expect(page.getByRole('heading', { name: 'Mi perfil' })).toHaveCount(0);
  });

  test('JWT adulterado y acceso sin token no obtienen contexto empresarial', async ({ page }) => {
    test.setTimeout(150_000);
    const jwt = await loginReal(page, 'c1504_admin_a');
    const [head, body, sig] = jwt.split('.');
    const altered = [head, body, (sig[0] === 'A' ? 'B' : 'A') + sig.slice(1)].join('.');
    const invalid = await page.request.get(api + '/tenant-context/1', {
      headers: { Authorization: 'Bearer ' + altered }
    });
    expect([401, 403]).toContain(invalid.status());
    const anonymous = await page.request.get(api + '/tenant-context/1');
    expect([401, 403]).toContain(anonymous.status());
  });
});
