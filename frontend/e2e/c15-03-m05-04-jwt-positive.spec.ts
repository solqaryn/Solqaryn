import { expect, test, type Page } from '@playwright/test';

/**
 * C15-03.M05.04 R2: aceptacion POSITIVA y fronteras A/B reales con JWT firmado
 * y API .NET 10 + MySQL 8.4 EFIMERO. No usa page.route ni tokens simulados.
 * No acredita pentest integral, archivos, jobs, sucursales ni QA/PROD.
 */
const api = process.env['C1504_API_URL'];
const password = process.env['C1504_TEST_PASSWORD'];
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !password) {
  throw new Error('C15-03.M05.04 exige GitHub CI aislada y credenciales efimeras');
}

async function ingresar(page: Page, usuario: string): Promise<string> {
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill(usuario);
  await page.locator('input[formcontrolname="password"]').fill(password!);
  const response = page.waitForResponse(r =>
    r.url() === 'http://localhost:5005/auth/login' && r.request().method() === 'POST');
  await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
  const actual = await response;
  expect(actual.status()).toBe(200);
  const token: unknown = (await actual.json())?.data?.token;
  expect(typeof token).toBe('string');
  expect((token as string).split('.')).toHaveLength(3);
  return token as string;
}

function vigilarMutaciones(page: Page) {
  const result: string[] = [];
  page.on('request', request => {
    if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method())) {
      result.push(request.method() + ' ' + request.url());
    }
  });
  return result;
}

test.describe('C15-03.M05.04 JWT real, MySQL descartable y aceptacion positiva', () => {
  test.describe.configure({ retries: 0 });

  test('rol global real ve cuatro fronteras sin resultados ficticios 320 y 1280', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    const writes = vigilarMutaciones(page);
    for (const width of [320, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      const sesion = page.waitForResponse(r =>
        r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
      await page.goto('/plataforma/auditoria-tenancy-v5');
      expect((await sesion).status()).toBe(200);
      await expect(page.getByRole('heading', { name: 'Fronteras de aislamiento y evidencia' })).toBeVisible();
      await expect(page.locator('section.tenancy-audit ol.fronteras > li')).toHaveCount(4);
      await expect(page.getByText('Estado de pentest, resultados A/B y métricas: NO_MEDIDO. QA/PROD no autorizados.')).toBeVisible();
      await expect(page.locator('section.tenancy-audit form, section.tenancy-audit input')).toHaveCount(0);
      const overflow = await page.evaluate(() =>
        document.documentElement.scrollWidth - document.documentElement.clientWidth);
      expect(overflow).toBeLessThanOrEqual(2);
    }
    expect(writes).toEqual([]);
  });

  test('revalidacion positiva usa GET autenticado real y mantiene lectura sin mutaciones', async ({ page }) => {
    await ingresar(page, 'c1504_platform_only');
    const writes = vigilarMutaciones(page);
    const inicial = page.waitForResponse(r =>
      r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
    await page.goto('/plataforma/auditoria-tenancy-v5');
    expect((await inicial).status()).toBe(200);
    const posterior = page.waitForResponse(r =>
      r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
    await page.getByRole('button', { name: 'Revalidar sesión de auditoría de tenancy' }).click();
    expect((await posterior).status()).toBe(200);
    await expect(page.getByText('Sesión revalidada. Solo guía de revisión, no pentest real.')).toBeVisible();
    await expect(page.locator('section.tenancy-audit ol.fronteras > li')).toHaveCount(4);
    expect(writes).toEqual([]);
  });

  test('empresa A puede leer su tenant real pero no B ni guia de autoridad global', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_admin_a');
    const headers = { Authorization: 'Bearer ' + jwt };
    const a = await request.get(api + '/tenant-context/1', { headers });
    expect(a.status()).toBe(200);
    expect((await a.json())?.data?.empresaId).toBe(1);
    const b = await request.get(api + '/tenant-context/2', { headers });
    expect([403, 404]).toContain(b.status());
    const sesion = page.waitForResponse(r =>
      r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
    await page.goto('/plataforma/auditoria-tenancy-v5');
    expect((await sesion).status()).toBe(403);
    await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
    await expect(page.locator('section.tenancy-audit')).toHaveCount(0);
  });

  test('empresa B puede leer su tenant real pero no A ni guia de autoridad global', async ({ page, request }) => {
    const jwt = await ingresar(page, 'c1504_admin_b');
    const headers = { Authorization: 'Bearer ' + jwt };
    const b = await request.get(api + '/tenant-context/2', { headers });
    expect(b.status()).toBe(200);
    expect((await b.json())?.data?.empresaId).toBe(2);
    const a = await request.get(api + '/tenant-context/1', { headers });
    expect([403, 404]).toContain(a.status());
    const sesion = page.waitForResponse(r =>
      r.url().includes('/plataforma/sesion') && r.request().method() === 'GET');
    await page.goto('/plataforma/auditoria-tenancy-v5');
    expect((await sesion).status()).toBe(403);
    await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
    await expect(page.locator('section.tenancy-audit')).toHaveCount(0);
  });
});
