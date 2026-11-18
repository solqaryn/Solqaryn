import { expect, test, type Page } from '@playwright/test';

// C15-04.M05.04 R2: navegador, JWT, API y MySQL reales SOLO en CI efimera.
// No usa page.route, interceptores ni localStorage con tokens fabricados.
const api = process.env['C1504_API_URL'];
const clave = process.env['C1504_TEST_PASSWORD'];
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !clave) {
  throw new Error('Requiere CI isolada con API local y password efimero.');
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

test.describe('C15 UX R2 - JWT firmado en MySQL 8.4 descartable', () => {
  test.describe.configure({ retries: 0 });

  test('rol global ingresa a auditoria UX privada responsive', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    for (const width of [375, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      const documento = await page.goto('/plataforma/auditoria-ux-v5');
      expect(documento?.status()).toBe(200);
      expect((await sesion).status()).toBe(200);
      await expect(page).toHaveURL(/\/plataforma\/auditoria-ux-v5$/);
      await expect(page.locator('[data-testid="ux-audit-v19"]')).toBeVisible();
      await expect(page.locator('[data-testid="ux-surface"][data-state="NO_MEDIDO"]')).toHaveCount(5);
      await expect(page.getByRole('heading', { level: 1 })).toHaveCount(1);
      const ancho = await page.evaluate(() => document.documentElement.scrollWidth);
      expect(ancho).toBeLessThanOrEqual(width + 1);
    }
  });

  test('rol empresarial sin autoridad global: 403 y cero datos privados', async ({ page }) => {
    await ingresar(page, 'c1504_admin_a');
    const sesion = page.waitForResponse(r =>
      r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
    await page.goto('/plataforma/auditoria-ux-v5');
    expect((await sesion).status()).toBe(403);
    await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
    await expect(page.locator('[data-testid="ux-audit-v19"]')).toHaveCount(0);
    await expect(page.locator('[data-testid="ux-surface"]')).toHaveCount(0);
  });

  test('miembro A autorizado, B denegado al navegar detalle dinamico', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_admin_a');
    const headers = { Authorization: 'Bearer ' + jwt };
    const propia = await request.get(api + '/tenant-context/1', { headers });
    expect(propia.status()).toBe(200);
    expect((await propia.json())?.data?.empresaId).toBe(1);
    const ajena = await request.get(api + '/tenant-context/2', { headers });
    expect([403, 404]).toContain(ajena.status());

    await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '1'));
    const tenantA = page.waitForResponse(r =>
      r.url().includes('/tenant-context/1') && r.request().method() === 'GET');
    await page.goto('/perfil');
    expect((await tenantA).status()).toBe(200);
    await expect(page.getByRole('heading', { name: 'Mi perfil' })).toBeVisible();

    await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '2'));
    const tenantB = page.waitForResponse(r =>
      r.url().includes('/tenant-context/2') && r.request().method() === 'GET');
    await page.goto('/productos/999999/editar');
    expect([403, 404]).toContain((await tenantB).status());
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.getByRole('heading', { name: 'Mi perfil' })).toHaveCount(0);
  });

  test('JWT firmado con firma alterada pierde sesion y pantalla privada', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_platform_only');
    const valido = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + jwt }
    });
    expect(valido.status()).toBe(200);
    const partes = jwt.split('.');
    const firma = partes[2];
    const firmaMal = (firma[0] === 'A' ? 'B' : 'A') + firma.slice(1);
    const adulterado = [partes[0], partes[1], firmaMal].join('.');
    const invalido = await request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + adulterado }
    });
    expect([401, 403]).toContain(invalido.status());
    await page.evaluate(token => localStorage.setItem('solqaryn_token', token), adulterado);
    await page.goto('/plataforma/auditoria-ux-v5');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.locator('[data-testid="ux-audit-v19"]')).toHaveCount(0);
  });
});
