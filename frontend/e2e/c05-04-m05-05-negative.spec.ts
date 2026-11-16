import { expect, test, type Page } from '@playwright/test';
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function session(page: Page, status: 200 | 403 = 200) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'fixture-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });
    return route.fulfill({ status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: { nombreUsuario: 'Fixture', rolNombre: 'Global', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'PRIVATE_SESSION_SENTINEL' }) });
  });
}
function spyWrites(page: Page) {
  const writes: string[] = [];
  page.on('request', request => { if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method()); });
  return writes;
}
test.describe('C05-04.M05.05 - denegaciones y errores UI, HTTP simulado', () => {
  test.describe.configure({ retries: 0 });
  test('anonimo no recibe directorio privado', async ({ page }) => {
    const writes = spyWrites(page);
    await page.goto('/plataforma');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.locator('.ui-lightness-v5')).toHaveCount(0);
    await expect(page.locator('table[aria-label="Empresas registradas"]')).toHaveCount(0);
    expect(writes).toEqual([]);
  });
  test('sesion 403 niega la pantalla antes de leer datos', async ({ page }) => {
    await session(page, 403);
    let reads = 0;
    await page.route(/\/plataforma\/empresas(?:\?|$)/, async route => {
      reads++;
      return route.fulfill({ status: 200, headers: cors, contentType: 'application/json',
        body: JSON.stringify({ success: true, data: { items: [{ id: 1, nombre: 'PRIVATE_TENANT_SENTINEL' }], total: 1 } }) });
    });
    await page.goto('/plataforma');
    await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
    await expect(page.locator('.ui-lightness-v5')).toHaveCount(0);
    await expect(page.getByText('PRIVATE_TENANT_SENTINEL')).toHaveCount(0);
    await expect(page.getByText('PRIVATE_SESSION_SENTINEL')).toHaveCount(0);
    expect(reads).toBe(0);
  });
  test('directorio 403 no revela detalles ni declara éxito', async ({ page }) => {
    await session(page);
    const writes = spyWrites(page);
    await page.route(/\/plataforma\/empresas(?:\?|$)/, async route => {
      if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });
      return route.fulfill({ status: 403, headers: cors, contentType: 'application/json',
        body: JSON.stringify({ success: false, message: 'PRIVATE_ACCESS_SENTINEL' }) });
    });
    await page.goto('/plataforma');
    await expect.poll(async () => (await page.getByTestId('c0504-permission').count()) > 0 || /\/login/.test(page.url())).toBe(true);
    await expect(page.getByTestId('c0504-success')).toHaveCount(0);
    await expect(page.getByText('PRIVATE_ACCESS_SENTINEL')).toHaveCount(0);
    await expect(page.locator('table[aria-label="Empresas registradas"] tbody tr')).toHaveCount(0);
    expect(writes).toEqual([]);
  });
  test('revocacion 403 no conserva filas de consulta previa', async ({ page }) => {
    await session(page);
    let deny = false;
    let reads = 0;
    const writes = spyWrites(page);
    await page.route(/\/plataforma\/empresas(?:\?|$)/, async route => {
      if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });
      reads++;
      return route.fulfill({ status: deny ? 403 : 200, headers: cors, contentType: 'application/json',
        body: JSON.stringify(deny
          ? { success: false, message: 'REVOKED_PRIVATE_SENTINEL' }
          : { success: true, data: { items: [{ id: 45, nombre: 'TENANT_A_PRIVATE_SENTINEL', activa: true, usuariosActivos: 1 }], pagina: 1, tamano: 25, total: 1, puedeCrear: false } }) });
    });
    await page.goto('/plataforma');
    await expect(page.getByText('TENANT_A_PRIVATE_SENTINEL')).toBeVisible();
    deny = true;
    await page.getByRole('button', { name: 'Actualizar directorio' }).click();
    await expect(page.getByText('TENANT_A_PRIVATE_SENTINEL')).toHaveCount(0);
    await expect(page.getByText('REVOKED_PRIVATE_SENTINEL')).toHaveCount(0);
    await expect(page.getByTestId('c0504-success')).toHaveCount(0);
    expect(reads).toBe(2);
    expect(writes).toEqual([]);
  });
  test('fallo 503 no conserva fila anterior ni diagnóstico interno', async ({ page }) => {
    await session(page);
    let fail = false;
    await page.route(/\/plataforma\/empresas(?:\?|$)/, async route => {
      if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });
      return route.fulfill({ status: fail ? 503 : 200, headers: cors, contentType: 'application/json',
        body: JSON.stringify(fail
          ? { success: false, message: 'PRIVATE_PROVIDER_TRACE_SENTINEL' }
          : { success: true, data: { items: [{ id: 2, nombre: 'STALE_TENANT_SENTINEL', activa: true, usuariosActivos: 1 }], pagina: 1, tamano: 25, total: 1, puedeCrear: false } }) });
    });
    await page.goto('/plataforma');
    await expect(page.getByText('STALE_TENANT_SENTINEL')).toBeVisible();
    fail = true;
    await page.getByRole('button', { name: 'Actualizar directorio' }).click();
    await expect(page.getByTestId('c0504-error')).toBeVisible();
    await expect(page.getByText('STALE_TENANT_SENTINEL')).toHaveCount(0);
    await expect(page.getByText('PRIVATE_PROVIDER_TRACE_SENTINEL')).toHaveCount(0);
    await expect(page.getByTestId('c0504-success')).toHaveCount(0);
  });
});
