// V22 C01-02.M04.01: ruta real read-only; JWT firmado, API .NET 10, MySQL 8.4 y Chromium CI; sin mocks HTTP ni permisos inferidos.
import { expect, test, type Page } from '@playwright/test';

// C01-02.M04.01: autorización de plataforma y denegación tenant A/B.
// Se ejecuta exclusivamente sobre API .NET, JWT firmado, MySQL 8.4 y Chromium CI.
// Prohibido interceptar /plataforma/sesion, falsificar usuarios o respuestas HTTP.
const api = process.env['C1504_API_URL'];
const password = process.env['C1504_TEST_PASSWORD'];
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !password) {
  throw new Error('R2: requiere backend real, MySQL efímero y credencial CI.');
}
const ruta = '/plataforma/capacidades-v19';

async function loginReal(page: Page, usuario: string): Promise<string> {
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill(usuario);
  await page.locator('input[formcontrolname="password"]').fill(password!);
  const wait = page.waitForResponse(response =>
    response.url().endsWith('/auth/login') && response.request().method() === 'POST');
  await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
  const response = await wait;
  expect(response.status()).toBe(200);
  const jwt: unknown = (await response.json())?.data?.token;
  expect(typeof jwt).toBe('string');
  expect((jwt as string).split('.')).toHaveLength(3);
  return jwt as string;
}

test.describe('V22 C01-02.M04.01 - catalogo real con JWT firmado', () => {
  test.describe.configure({ retries: 0 });

  test('plataforma global JWT sin tenant: catalogo informativo y permisos NO concedidos', async ({ page }) => {
    test.setTimeout(150_000);
    const jwt = await loginReal(page, 'c1504_platform_only');
    const session = await page.request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + jwt }
    });
    expect(session.status()).toBe(200);
    const verified = page.waitForResponse(response =>
      response.url().includes('/plataforma/sesion') && response.request().method() === 'GET');
    await page.goto(ruta);
    expect((await verified).status()).toBe(200);
    await expect(page.getByRole('heading', { name: 'Catálogo global de módulos y capacidades' })).toBeVisible();
    await expect(page.getByTestId('c0102-listado').locator('li').first()).toBeVisible();
    await expect(page.getByTestId('c0102-listado')).toContainText('SIN_TENANT_VERIFICADO');
    await expect(page.getByTestId('c0102-listado').locator('li').first()).toBeVisible();
    await expect(page.locator('[data-evidence-state="SENAL_CLIENTE_NO_AUTORITATIVA"]')).toHaveCount(0);
  });

  for (const usuario of ['c1504_admin_a', 'c1504_admin_b']) {
    test(usuario + ': rol empresarial sin grant global recibe 403 y sin capacidades', async ({ page }) => {
      test.setTimeout(150_000);
      const jwt = await loginReal(page, usuario);
      const session = await page.request.get(api + '/plataforma/sesion', {
        headers: { Authorization: 'Bearer ' + jwt }
      });
      expect(session.status()).toBe(403);
      const denial = page.waitForResponse(response =>
        response.url().includes('/plataforma/sesion') && response.request().method() === 'GET');
      await page.goto(ruta);
      expect((await denial).status()).toBe(403);
      await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
      await expect(page.getByTestId('c0102-listado')).toHaveCount(0);
      await expect(page.getByRole('heading', { name: 'Catálogo global de módulos y capacidades' })).toHaveCount(0);
    });
  }

  test('JWT A con header de tenant B no concede membresía ni rol de plataforma', async ({ page }) => {
    test.setTimeout(150_000);
    const jwt = await loginReal(page, 'c1504_admin_a');
    const headers = { Authorization: 'Bearer ' + jwt, 'X-Empresa-Id': '2' };
    const foreign = await page.request.get(api + '/tenant-context/2', { headers });
    expect([403, 404], 'IDOR/BOLA empresarial B debe denegarse').toContain(foreign.status());
    const global = await page.request.get(api + '/plataforma/sesion', { headers });
    expect(global.status()).toBe(403);
    await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '2'));
    await page.goto(ruta);
    await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
    await expect(page.getByTestId('c0102-listado')).toHaveCount(0);
  });

  test('credenciales ausentes y JWT adulterado fallan cerrado al consultar plataforma', async ({ page }) => {
    test.setTimeout(150_000);
    const anonymous = await page.request.get(api + '/plataforma/sesion');
    expect([401, 403]).toContain(anonymous.status());
    const jwt = await loginReal(page, 'c1504_platform_only');
    const [head, body, signature] = jwt.split('.');
    const altered = [head, body, (signature[0] === 'A' ? 'B' : 'A') + signature.slice(1)].join('.');
    const invalid = await page.request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + altered }
    });
    expect([401, 403]).toContain(invalid.status());
  });
});
