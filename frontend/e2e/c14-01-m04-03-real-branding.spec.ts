import { expect, test, type Page } from '@playwright/test';

// V22 C14-01.M04.03: JWT, membresias y proveedor tenant privado reales en MySQL efimero.
// Unicamente la prueba negativa de consola global intercepta branding publico;
// A/B validan la API privada real con JWT, MySQL y Chromium.
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
  const tenantBrand = await page.request.get(api + '/empresa-configuracion/tenant/' + id, { headers });
  expect(tenantBrand.status(), 'Marca privada leida con JWT y MySQL reales').toBe(200);
  const brand = await tenantBrand.json();
  expect(brand?.data?.empresaId).toBe(id);
  expect(brand?.data?.nombre).toBe(nombre);
  const foreignBrand = await page.request.get(api + '/empresa-configuracion/tenant/' + (id === 1 ? 2 : 1), { headers });
  expect([403, 404], 'Marca tenant ajena denegada en API real').toContain(foreignBrand.status());
  const guessedSelector = await page.request.get(api + '/empresa-configuracion/tenant/' + id, {
    headers: { ...headers, 'X-Empresa-Id': String(id === 1 ? 2 : 1) }
  });
  expect([403, 404, 503], 'Un selector de tenant cruzado no autoriza marca').toContain(guessedSelector.status());
  const unauthenticatedBrand = await page.request.get(api + '/empresa-configuracion/tenant/' + id);
  expect(unauthenticatedBrand.status(), 'La marca privada exige JWT').not.toBe(200);
  await page.evaluate(tenantId =>
    localStorage.setItem('solqaryn_empresa_solicitada_id', String(tenantId)), id);
  const checked = page.waitForResponse(r =>
    r.url().includes('/tenant-context/' + id) && r.request().method() === 'GET');
  await page.goto('/perfil');
  expect((await checked).status()).toBe(200);
  await expect(page.getByRole('heading', { name: 'Mi perfil' })).toBeVisible();
  await expect(page.locator('#main-sidebar .brand > span').first()).toHaveText(nombre);
}

test.describe('C14-01.M04.03 - UI marca empresarial con JWT real y branding tenant privado', () => {
  test.describe.configure({ retries: 0 });

  test('A: tenant real, marca dinamica en shell responsive y aislamiento negativo B', async ({ page }) => {
    test.setTimeout(150_000);
    const logo = 'https://assets.example.invalid/c14-a.svg';
    await ingresarEmpresa(page, 'c1504_admin_a', 1, 'Empresa A efimera C15');
    for (const width of [375, 1280]) {
      await page.setViewportSize({ width, height: 840 });
      await expect(page.locator('#main-sidebar .brand > span').first()).toHaveText('Empresa A efimera C15');
      await expect(page.locator('#main-sidebar .brand img.brand-logo')).toHaveAttribute('src', logo);
      expect(await page.evaluate(() => document.documentElement.scrollWidth))
        .toBeLessThanOrEqual(width + 1);
      await expect(page.getByText('Empresa B efimera C15')).toHaveCount(0);
    }
  });

  test('B: JWT de otra membresia no hereda marca A ni acceso a empresa 1', async ({ page }) => {
    test.setTimeout(150_000);
    const logo = 'https://assets.example.invalid/c14-b.svg';
    await ingresarEmpresa(page, 'c1504_admin_b', 2, 'Empresa B efimera C15');
    await expect(page.locator('#main-sidebar .brand img.brand-logo')).toHaveAttribute('src', logo);
    await expect(page.getByText('Empresa A efimera C15')).toHaveCount(0);
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

  test('proveedor publico real: 503 fail-closed P0 ante dos empresas', async ({ page }) => {
    test.setTimeout(150_000);
    // El laboratorio tiene dos empresas aisladas. La configuracion publica global
    // carece de un tenant inequívoco y el guard P0 del backend rechaza su lectura.
    // Nunca deshabilitar ese guard para conseguir HTTP 200 en el test.
    const live = await page.request.get(api + '/empresa-configuracion/publica');
    expect(live.status(), 'Sin tenant publico univoco, P0 bloquea branding global').toBe(503);
    const body = await live.text();
    expect(body).not.toMatch(/Empresa A efimera C15|Empresa B efimera C15/);

    await loginReal(page, 'c1504_admin_a');
    await page.evaluate(() => localStorage.setItem('solqaryn_empresa_solicitada_id', '1'));
    const checked = page.waitForResponse(r =>
      r.url().includes('/tenant-context/1') && r.request().method() === 'GET');
    await page.goto('/perfil');
    expect((await checked).status()).toBe(200);
    await expect(page.getByRole('heading', { name: 'Mi perfil' })).toBeVisible();
    // P0 sigue negando /publica, pero una sesion tenant verificada debe usar
    // la fuente PRIVADA con JWT/RBAC real y MySQL, no volver a marca neutra.
    await expect(page.locator('#main-sidebar .brand > span').first()).toHaveText('Empresa A efimera C15');
    await expect(page.locator('#main-sidebar .brand img.brand-logo'))
      .toHaveAttribute('src', 'https://assets.example.invalid/c14-a.svg');
    await expect(page.getByText('Empresa B efimera C15')).toHaveCount(0);
  });

  test('C14-01.M04.06: densidad responsive y progresion privada real sin bloqueo del shell', async ({ page }) => {
    test.setTimeout(150_000);
    let lecturasTenant = 0;
    // Demorar la peticion REAL, sin fabricar payload ni sustituir MySQL/JWT.
    await page.route(/\/empresa-configuracion\/tenant\/1(?:\?|$)/, async route => {
      lecturasTenant++;
      await new Promise(resolve => setTimeout(resolve, 350));
      await route.continue();
    });
    await ingresarEmpresa(page, 'c1504_admin_a', 1, 'Empresa A efimera C15');
    expect(lecturasTenant, 'Una carga real de marca tenant en navegador').toBeGreaterThan(0);

    // Distintas densidades, sin scroll lateral, desbordamientos ni resultado
    // ficticio antes de terminar la carga. El contenido privado ya fue autorizado.
    for (const ancho of [320, 375, 768, 1280]) {
      await page.setViewportSize({ width: ancho, height: 800 });
      await expect(page.getByRole('heading', { name: 'Mi perfil' })).toBeVisible();
      await expect(page.locator('#main-sidebar .brand > span').first())
        .toHaveText('Empresa A efimera C15');
      const desbordamiento = await page.evaluate(() => document.documentElement.scrollWidth);
      expect(desbordamiento, 'Sin desbordamiento a ' + ancho + ' px')
        .toBeLessThanOrEqual(ancho + 1);
      await expect(page.getByText('Empresa B efimera C15')).toHaveCount(0);
    }
  });


  test('C14-01.M05.03: persisted provider survives independent JWT A/B sessions and reloads', async ({ browser }) => {
    test.setTimeout(180_000);
    const contextA = await browser.newContext();
    const contextB = await browser.newContext();
    const contextAagain = await browser.newContext();
    try {
      const pageA = await contextA.newPage();
      const pageB = await contextB.newPage();
      const pageAagain = await contextAagain.newPage();
      // Three distinct authentication flows hit the actual ASP.NET API and ephemeral MySQL.
      const tokenA = await loginReal(pageA, 'c1504_admin_a');
      const tokenB = await loginReal(pageB, 'c1504_admin_b');
      const tokenAagain = await loginReal(pageAagain, 'c1504_admin_a');
      const getPrivate = (page: Page, token: string, tenantId: number) =>
        page.request.get(api + '/empresa-configuracion/tenant/' + tenantId, {
          headers: { Authorization: 'Bearer ' + token }
        });
      const firstA = await getPrivate(pageA, tokenA, 1);
      const firstB = await getPrivate(pageB, tokenB, 2);
      expect(firstA.status(), 'A lee datos reales de Empresa 1 persistidos en MySQL').toBe(200);
      expect(firstB.status(), 'B lee datos reales de Empresa 2 persistidos en MySQL').toBe(200);
      const recordA = (await firstA.json())?.data;
      const recordB = (await firstB.json())?.data;
      expect(recordA?.empresaId).toBe(1);
      expect(recordB?.empresaId).toBe(2);
      expect(recordA?.nombre).toBe('Empresa A efimera C15');
      expect(recordB?.nombre).toBe('Empresa B efimera C15');
      expect(recordA?.logoUrl).toBe('https://assets.example.invalid/c14-a.svg');
      expect(recordB?.logoUrl).toBe('https://assets.example.invalid/c14-b.svg');
      expect((firstA.headers()['cache-control'] ?? '').toLowerCase()).toContain('no-store');
      expect((firstB.headers()['cache-control'] ?? '').toLowerCase()).toContain('no-store');
      // Cross-tenant reads cannot use either JWT, even after the other session is opened.
      const crossedA = await getPrivate(pageA, tokenA, 2);
      const crossedB = await getPrivate(pageB, tokenB, 1);
      expect([403, 404]).toContain(crossedA.status());
      expect([403, 404]).toContain(crossedB.status());
      const persistedAgain = await getPrivate(pageAagain, tokenAagain, 1);
      expect(persistedAgain.status(), 'Nuevo login conserva la marca persistida, sin cache de navegador').toBe(200);
      const recordAagain = (await persistedAgain.json())?.data;
      expect(recordAagain?.nombre).toBe(recordA?.nombre);
      expect(recordAagain?.logoUrl).toBe(recordA?.logoUrl);
      expect(recordAagain?.empresaId).toBe(1);
      expect(recordAagain?.nombre).not.toBe(recordB?.nombre);
      const anonymous = await pageAagain.request.get(api + '/empresa-configuracion/tenant/1');
      expect(anonymous.status()).not.toBe(200);
    } finally {
      await Promise.all([contextA.close(), contextB.close(), contextAagain.close()]);
    }
  });
});
