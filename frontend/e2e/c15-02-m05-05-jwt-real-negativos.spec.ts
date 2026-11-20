import { expect, test, type Page } from '@playwright/test';

// Solo GitHub Actions con ASP.NET Core + MySQL 8.4 efimeros. No usa page.route.
const api = process.env['C1504_API_URL'];
const clave = process.env['C1504_TEST_PASSWORD'];
const route = '/plataforma/revision-arquitectura-v20';
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !clave) {
  throw new Error('C15-02.M05.05 requiere API JWT y MySQL 8.4 aislados en CI.');
}

async function ingresar(page: Page, usuario: string): Promise<string> {
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill(usuario);
  await page.locator('input[formcontrolname="password"]').fill(clave!);
  const espera = page.waitForResponse(r => r.url() ===
    'http://localhost:5005/auth/login' && r.request().method() === 'POST');
  await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
  const respuesta = await espera;
  expect(respuesta.status()).toBe(200);
  const data = await respuesta.json();
  const jwt: unknown = data?.data?.token;
  expect(typeof jwt).toBe('string');
  expect((jwt as string).split('.')).toHaveLength(3);
  return jwt as string;
}

test.describe('C15-02.M05.05 JWT real y frontera de plataforma/tenant A B', () => {
  test.describe.configure({ retries: 0 });

  test('control positivo: concesion global real habilita exactamente cinco referencias locales', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_platform_only');
    const servidor = await request.get(api! + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + jwt }
    });
    expect(servidor.status()).toBe(200);
    const sesion = page.waitForResponse(r =>
      r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
    await page.goto(route);
    expect((await sesion).status()).toBe(200);
    await expect(page).toHaveURL(/\/plataforma\/revision-arquitectura-v20$/);
    await expect(page.getByTestId('architecture-item')).toHaveCount(5);
    await expect(page.getByText('NO_VERIFICADO')).toHaveCount(5);
  });

  for (const [usuario, empresa] of [['c1504_admin_a', 1], ['c1504_admin_b', 2]] as const) {
    test('miembro solo empresa ' + empresa + ': sin rol global rechaza API y pantalla', async ({ page, request }) => {
      const jwt = await ingresar(page, usuario);
      const empresaPropia = await request.get(api! + '/tenant-context/' + empresa, {
        headers: { Authorization: 'Bearer ' + jwt }
      });
      expect(empresaPropia.status()).toBe(200);
      const sesionPrivada = await request.get(api! + '/plataforma/sesion', {
        headers: { Authorization: 'Bearer ' + jwt }
      });
      expect(sesionPrivada.status()).toBe(403);
      const comprobacion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      await page.goto(route);
      expect((await comprobacion).status()).toBe(403);
      await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
      await expect(page.getByTestId('architecture-item')).toHaveCount(0);
      await expect(page.getByText('NO_VERIFICADO')).toHaveCount(0);
    });
  }

  test('JWT real con firma alterada falla en API y en guard del navegador', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_platform_only');
    const partes = jwt.split('.');
    const firma = partes[2];
    const alterada = [partes[0], partes[1], (firma[0] === 'A' ? 'B' : 'A') + firma.slice(1)].join('.');
    const noAutorizado = await request.get(api! + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + alterada }
    });
    expect([401, 403]).toContain(noAutorizado.status());
    await page.evaluate(token => localStorage.setItem('solqaryn_token', token), alterada);
    await page.goto(route);
    await expect(page).toHaveURL(/\/login(?:[?#]|$)/);
    await expect(page.getByTestId('architecture-item')).toHaveCount(0);
  });

  test('anonimo sin token: HTTP real 401 y sin referencias privadas', async ({ page, request }) => {
    const servidor = await request.get(api! + '/plataforma/sesion');
    expect(servidor.status()).toBe(401);
    await page.goto(route);
    await expect(page).toHaveURL(/\/login(?:[?#]|$)/);
    await expect(page.getByTestId('architecture-item')).toHaveCount(0);
  });
});
