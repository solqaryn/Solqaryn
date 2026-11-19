import { expect, test, type Page } from '@playwright/test';

// C13-04.M05.05: ampliación adversarial de rutas globales con autorización verdadera.
// Se ejecuta exclusivamente sobre API .NET, JWT firmado, MySQL 8.4 y Chromium CI.
// Prohibido interceptar /plataforma/sesion, falsificar usuarios o respuestas HTTP.
const api = process.env['C1504_API_URL'];
const password = process.env['C1504_TEST_PASSWORD'];
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !password) {
  throw new Error('R2: requiere backend real, MySQL efímero y credencial CI.');
}
const ruta = '/plataforma/inventario-rutas-v19';

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


test.describe('V21 C13-04.M05.05 - seguridad negativa de rutas con JWT real', () => {
  test.describe.configure({ retries: 0 });

  test('plataforma JWT legítimo visualiza catálogo sin enlaces enumerables ni escrituras', async ({ page }) => {
    test.setTimeout(150_000);
    const jwt = await loginReal(page, 'c1504_platform_only');
    const session = await page.request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + jwt }
    });
    expect(session.status()).toBe(200);
    const writes: string[] = [];
    page.on('request', r => {
      if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method());
    });
    const proof = page.waitForResponse(response =>
      response.url().includes('/plataforma/sesion') && response.request().method() === 'GET');
    await page.goto(ruta);
    expect((await proof).status()).toBe(200);
    await expect(page.getByTestId('c1304-route-catalogo')).toBeVisible();
    const links = await page.locator('.rutas a').evaluateAll(nodes =>
      nodes.map(n => (n as HTMLAnchorElement).getAttribute('href') ?? ''));
    expect(links.length, 'El catálogo positivo debe enumerar alguna ruta segura').toBeGreaterThan(0);
    expect(links.every(href => href.startsWith('/') && !href.startsWith('//') &&
      !href.includes(':') && !href.includes('*'))).toBe(true);
    expect(writes).toEqual([]);
  });

  for (const usuario of ['c1504_admin_a', 'c1504_admin_b']) {
    test(usuario + ' carece de grant plataforma, HTTP 403 y ningún catálogo', async ({ page }) => {
      test.setTimeout(150_000);
      const jwt = await loginReal(page, usuario);
      const session = await page.request.get(api + '/plataforma/sesion', {
        headers: { Authorization: 'Bearer ' + jwt }
      });
      expect(session.status()).toBe(403);
      const denied = page.waitForResponse(response =>
        response.url().includes('/plataforma/sesion') && response.request().method() === 'GET');
      await page.goto(ruta);
      expect((await denied).status()).toBe(403);
      await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
      await expect(page.getByTestId('c1304-route-catalogo')).toHaveCount(0);
    });
  }

  test('JWT de A y header de B no permiten IDOR/BOLA ni catálogo global', async ({ page }) => {
    test.setTimeout(150_000);
    const jwt = await loginReal(page, 'c1504_admin_a');
    const headers = { Authorization: 'Bearer ' + jwt, 'X-Empresa-Id': '2' };
    const foreign = await page.request.get(api + '/tenant-context/2', { headers });
    expect([403, 404], 'Empresa B no pertenece al usuario A').toContain(foreign.status());
    const global = await page.request.get(api + '/plataforma/sesion', { headers });
    expect(global.status()).toBe(403);
    await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '2'));
    await page.goto(ruta);
    await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
    await expect(page.getByTestId('c1304-route-catalogo')).toHaveCount(0);
  });

  test('anónimo y JWT adulterado son rechazados sin catálogo ni detalles', async ({ page }) => {
    test.setTimeout(150_000);
    const anonymous = await page.request.get(api + '/plataforma/sesion');
    expect([401, 403]).toContain(anonymous.status());
    await page.goto(ruta);
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.getByTestId('c1304-route-catalogo')).toHaveCount(0);
    const jwt = await loginReal(page, 'c1504_platform_only');
    const [head, body, sig] = jwt.split('.');
    const forged = [head, body, (sig[0] === 'A' ? 'B' : 'A') + sig.slice(1)].join('.');
    const invalid = await page.request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + forged }
    });
    expect([401, 403]).toContain(invalid.status());
  });

  test('revocar membresía en MySQL real invalida tenant A y nunca concede plataforma', async ({ page }) => {
    test.setTimeout(150_000);
    const jwt = await loginReal(page, 'c1504_admin_a');
    const { execFileSync } = await import('node:child_process');
    const container = execFileSync('bash', ['-lc', "docker ps --filter ancestor=mysql:8.4 --format '{{.ID}}' | head -n 1"], { encoding: 'utf8' }).trim();
    expect(container).not.toBe('');
    const sql = (query: string) => execFileSync('docker', [
      'exec', container, 'mysql', '-uroot', '-proot', 'solqaryn_c1504_real_ci', '-Nse', query
    ], { encoding: 'utf8' }).trim();
    const membership = "SELECT COUNT(*) FROM UsuarioEmpresas ue JOIN Usuarios u ON u.Id=ue.UsuarioId WHERE u.NombreUsuario='c1504_admin_a' AND ue.EmpresaId=1 AND ue.Activa=TRUE";
    expect(Number(sql(membership))).toBe(1);
    const restore = "UPDATE UsuarioEmpresas ue JOIN Usuarios u ON u.Id=ue.UsuarioId SET ue.Activa=TRUE,ue.FechaActualizacion=UTC_TIMESTAMP(6) WHERE u.NombreUsuario='c1504_admin_a' AND ue.EmpresaId=1";
    try {
      sql("UPDATE UsuarioEmpresas ue JOIN Usuarios u ON u.Id=ue.UsuarioId SET ue.Activa=FALSE,ue.FechaActualizacion=UTC_TIMESTAMP(6) WHERE u.NombreUsuario='c1504_admin_a' AND ue.EmpresaId=1");
      expect(Number(sql(membership))).toBe(0);
      const result = await page.request.get(api + '/tenant-context/1', {
        headers: { Authorization: 'Bearer ' + jwt, 'X-Empresa-Id': '1' }
      });
      expect([403, 404], 'Membresía revocada debe negar acceso de forma persistente').toContain(result.status());
      const global = await page.request.get(api + '/plataforma/sesion', {
        headers: { Authorization: 'Bearer ' + jwt }
      });
      expect(global.status()).toBe(403);
    } finally {
      sql(restore);
    }
    expect(Number(sql(membership))).toBe(1);
  });
});
