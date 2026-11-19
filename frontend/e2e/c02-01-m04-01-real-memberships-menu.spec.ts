import { expect, test, type Page } from '@playwright/test';

// C02-01.M04.01: navegador/Angular + API .NET10 + JWT firmados y MySQL8.4 efimero.
// No se interceptan rutas, no se simulan membresias, no hay credenciales reales.
const api = process.env['C1504_API_URL'];
const password = process.env['C1504_TEST_PASSWORD'];
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !password) {
  throw new Error('La prueba R2 del menu requiere runner CI con API y MySQL reales.');
}
const cambiar = 'Cambiar de empresa sin cerrar sesión';

async function login(page: Page, username: string): Promise<string> {
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill(username);
  await page.locator('input[formcontrolname="password"]').fill(password!);
  const pending = page.waitForResponse(r =>
    r.url().endsWith('/auth/login') && r.request().method() === 'POST');
  await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
  const response = await pending;
  expect(response.status()).toBe(200);
  const token: unknown = (await response.json())?.data?.token;
  expect(typeof token).toBe('string');
  expect((token as string).split('.')).toHaveLength(3);
  return token as string;
}

async function abrirPerfilConContextoReal(page: Page, empresaId: number) {
  await page.evaluate(id => localStorage.setItem('solqaryn_empresa_solicitada_id', String(id)), empresaId);
  const contexto = page.waitForResponse(r =>
    r.url().endsWith('/tenant-context/' + empresaId) && r.request().method() === 'GET');
  const membresias = page.waitForResponse(r =>
    r.url().endsWith('/tenant-context/empresas') && r.request().method() === 'GET');
  await page.goto('/perfil');
  expect((await contexto).status()).toBe(200);
  expect((await membresias).status()).toBe(200);
  await expect(page.getByRole('heading', { name: 'Mi perfil' })).toBeVisible();
}

test.describe('V21 C02-01.M04.01 - menu real de empresas JWT MySQL Chromium', () => {
  test.describe.configure({ retries: 0 });

  test('initial - usuario A con N1 oculta cambiar empresa', async ({ page }) => {
    test.setTimeout(150_000);
    const jwt = await login(page, 'c1504_admin_a');
    const response = await page.request.get(api + '/tenant-context/empresas', {
      headers: { Authorization: 'Bearer ' + jwt }
    });
    expect(response.status()).toBe(200);
    expect((await response.json()).data.map((e: {empresaId:number}) => e.empresaId)).toEqual([1]);
    await abrirPerfilConContextoReal(page, 1);
    await expect(page.getByRole('button', { name: cambiar })).toHaveCount(0);
  });

  test('initial - usuario A+B con N2 muestra cambiar empresa tras verificar JWT', async ({ page }) => {
    test.setTimeout(150_000);
    const jwt = await login(page, 'c0201_admin_ab');
    const response = await page.request.get(api + '/tenant-context/empresas', {
      headers: { Authorization: 'Bearer ' + jwt }
    });
    expect(response.status()).toBe(200);
    const ids = (await response.json()).data.map((e: {empresaId:number}) => e.empresaId);
    expect(ids.slice().sort()).toEqual([1, 2]);
    await abrirPerfilConContextoReal(page, 1);
    await expect(page.getByRole('button', { name: cambiar })).toBeVisible();
  });

  test('initial - usuario B con N1 oculta cambiar empresa', async ({ page }) => {
    test.setTimeout(150_000);
    await login(page, 'c1504_admin_b');
    await abrirPerfilConContextoReal(page, 2);
    await expect(page.getByRole('button', { name: cambiar })).toHaveCount(0);
  });

  test('initial - tenant B falsificado con JWT A denegado sin menu', async ({ page }) => {
    test.setTimeout(150_000);
    const jwt = await login(page, 'c1504_admin_a');
    const response = await page.request.get(api + '/tenant-context/2', {
      headers: { Authorization: 'Bearer ' + jwt, 'X-Empresa-Id': '2' }
    });
    expect([403, 404]).toContain(response.status());
    await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '2'));
    const denied = page.waitForResponse(r =>
      r.url().endsWith('/tenant-context/2') && r.request().method() === 'GET');
    await page.goto('/perfil');
    expect([403, 404]).toContain((await denied).status());
    await expect(page).toHaveURL(/\/login(?:[?#]|$)/);
    await expect(page.getByRole('button', { name: cambiar })).toHaveCount(0);
  });

  test('after revocation - usuario antes N2 ahora N1 oculta menu y B es prohibida', async ({ page }) => {
    test.setTimeout(150_000);
    const jwt = await login(page, 'c0201_admin_ab');
    const list = await page.request.get(api + '/tenant-context/empresas', {
      headers: { Authorization: 'Bearer ' + jwt }
    });
    expect(list.status()).toBe(200);
    expect((await list.json()).data.map((e: {empresaId:number}) => e.empresaId)).toEqual([1]);
    const forbidden = await page.request.get(api + '/tenant-context/2', {
      headers: { Authorization: 'Bearer ' + jwt }
    });
    expect([403, 404]).toContain(forbidden.status());
    await abrirPerfilConContextoReal(page, 1);
    await expect(page.getByRole('button', { name: cambiar })).toHaveCount(0);
  });
});
