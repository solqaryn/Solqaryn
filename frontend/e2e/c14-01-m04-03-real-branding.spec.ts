import { expect, test, type Page } from '@playwright/test';

// V21 C14-01.M04.03: JWT, membresias y API reales en MySQL efimero.
// Solo el proveedor de branding publico se intercepta para fijar la marca
// y probar la UI deterministamente; NO se acredita el proveedor real.
const api = process.env['C1504_API_URL'];
const password = process.env['C1504_TEST_PASSWORD'];
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !password) {
  throw new Error('Branding R2 requiere CI aislada, API MySQL y clave efimera.');
}

async function loginReal(page: Page, username: string): Promise<string> {
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill(username);
  await page.locator('input[formcontrolname="password"]').fill(password!);
  const pending = page.waitForResponse(r =>
    r.url().endsWith('/auth/login') && r.request().method() === 'POST');
  await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
  const response = await pending;
  expect(response.status(), 'Login real de la API efimera').toBe(200);
  const token: unknown = (await response.json())?.data?.token;
  expect(typeof token).toBe('string');
  expect((token as string).split('.')).toHaveLength(3);
  return token as string;
}

async function mockSoloMarcaPublica(page: Page, nombre: string, logo: string): Promise<void> {
  await page.route(/\/empresa-configuracion\/publica(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers }); return;
    }
    expect(route.request().method()).toBe('GET');
    await route.fulfill({
      status: 200, headers, contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        data: {
          nombreComercial: nombre, nombreVisibleSistema: nombre,
          logoUrl: logo, descripcionSistema: 'Identidad publica de ' + nombre,
          mensajeLogin: 'Acceso de ' + nombre,
          mostrarCopyright: true, usarAnioAutomaticoCopyright: false,
          copyright: '© C14 Entorno CI'
        }
      })
    });
  });
}

async function ingresarEmpresa(page: Page, user: string, id: number, nombre: string): Promise<void> {
  const token = await loginReal(page, user);
  const headers = { Authorization: 'Bearer ' + token };
  const permitted = await page.request.get(api + '/tenant-context/' + id, { headers });
  expect(permitted.status(), 'Tenant propio comprobado por servidor real').toBe(200);
  const foreign = await page.request.get(api + '/tenant-context/' + (id === 1 ? 2 : 1), { headers });
  expect([403, 404], 'El servidor deniega tenant ajeno').toContain(foreign.status());
  await page.evaluate(tenantId =>
    localStorage.setItem('solqaryn_empresa_solicitada_id', String(tenantId)), id);
  const checked = page.waitForResponse(r =>
    r.url().includes('/tenant-context/' + id) && r.request().method() === 'GET');
  await page.goto('/perfil');
  expect((await checked).status()).toBe(200);
  await expect(page.getByRole('heading', { name: 'Mi perfil' })).toBeVisible();
  await expect(page.locator('#main-sidebar .brand > span').first()).toHaveText(nombre);
}

test.describe('C14-01.M04.03 - UI marca empresarial con JWT real y proveedor de branding simulado', () => {
  test.describe.configure({ retries: 0 });

  test('A: tenant real, marca dinamica en shell responsive y aislamiento negativo B', async ({ page }) => {
    test.setTimeout(150_000);
    const logo = 'https://assets.example.invalid/c14-a.svg';
    await mockSoloMarcaPublica(page, 'Marca CI Empresa A', logo);
    await ingresarEmpresa(page, 'c1504_admin_a', 1, 'Marca CI Empresa A');
    for (const width of [375, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      await expect(page.locator('#main-sidebar .brand > span').first()).toHaveText('Marca CI Empresa A');
      await expect(page.locator('#main-sidebar .brand img.brand-logo')).toHaveAttribute('src', logo);
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
      await expect(page.getByText('Marca CI Empresa B')).toHaveCount(0);
    }
  });

  test('B: JWT de otra membresia no hereda marca A ni acceso a empresa 1', async ({ page }) => {
    test.setTimeout(150_000);
    const logo = 'https://assets.example.invalid/c14-b.svg';
    await mockSoloMarcaPublica(page, 'Marca CI Empresa B', logo);
    await ingresarEmpresa(page, 'c1504_admin_b', 2, 'Marca CI Empresa B');
    await expect(page.locator('#main-sidebar .brand img.brand-logo')).toHaveAttribute('src', logo);
    await expect(page.getByText('Marca CI Empresa A')).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'Abrir consola global de SOLQARYN' })).toBeVisible();
  });

  test('plataforma global: JWT real no hereda marca empresarial interceptada', async ({ page }) => {
    test.setTimeout(150_000);
    await mockSoloMarcaPublica(page, 'Marca Privada A', 'https://assets.example.invalid/private-a.svg');
    const token = await loginReal(page, 'c1504_platform_only');
    const verified = await page.request.get(api + '/plataforma/sesion', {
      headers: { Authorization: 'Bearer ' + token }
    });
    expect(verified.status()).toBe(200);
    await page.goto('/plataforma/auditoria-ux-v5');
    await expect(page).toHaveURL(/\/plataforma\/auditoria-ux-v5$/);
    await expect(page.locator('#main-sidebar .brand > span').first()).toHaveText('SOLQARYN');
    await expect(page.locator('#main-sidebar .brand img.brand-logo'))
      .not.toHaveAttribute('src', /private-a/);
    await expect(page.getByText('Marca Privada A')).toHaveCount(0);
  });

  test('proveedor publico autentico: shell muestra branding servido sin interceptacion', async ({ page }) => {
    test.setTimeout(150_000);
    // Integracion REAL: no se intercepta /empresa-configuracion/publica.
    const live = await page.request.get(api + '/empresa-configuracion/publica');
    expect(live.status(), 'Configuracion publica real MySQL efimero').toBe(200);
    const payload: unknown = await live.json();
    expect(payload).toMatchObject({ success: true, data: expect.any(Object) });
    const data = (payload as { data: { nombreVisibleSistema?: string; nombreComercial?: string; logoUrl?: string | null } }).data;
    const expectedName = data.nombreVisibleSistema || data.nombreComercial || 'SOLQARYN';
    expect(expectedName.trim().length).toBeGreaterThan(0);

    await loginReal(page, 'c1504_admin_a');
    await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '1'));
    const checked = page.waitForResponse(r =>
      r.url().includes('/tenant-context/1') && r.request().method() === 'GET');
    await page.goto('/perfil');
    expect((await checked).status()).toBe(200);
    await expect(page.getByRole('heading', { name: 'Mi perfil' })).toBeVisible();
    await expect(page.locator('#main-sidebar .brand > span').first()).toHaveText(expectedName);
    if (data.logoUrl?.trim()) {
      await expect(page.locator('#main-sidebar .brand img.brand-logo'))
        .toHaveAttribute('src', data.logoUrl);
    }
    // Limite: /publica es config global, no certifica marca aislada por tenant.
  });

});
