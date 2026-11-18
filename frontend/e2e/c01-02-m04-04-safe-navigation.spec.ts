import { expect, test, type Page } from '@playwright/test';

const catalogUrl = '/plataforma/capacidades-v19';
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

/** Synthetic browser fixture only: not a JWT or backend RBAC/tenant test. */
async function installFixture(page: Page, status: 200 | 403 = 200): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0102-m0404-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(sessionUrl, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    await route.fulfill({
      status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: {
            nombreUsuario: 'Synthetic Catalog', rolNombre: 'Plataforma',
            ambito: 'Plataforma', esAdministrador: true
          } }
        : { success: false, message: 'PRIVATE_TENANT_B_SECRET_SHOULD_NOT_RENDER' })
    });
  });
}

test.describe('C01-02.M04.04 - navegacion y mensajes de capabilities', () => {
  test.describe.configure({ retries: 0 });

  test('anonimo o sesion 403: catalogo privado y error opaco', async ({ page }) => {
    await page.goto(catalogUrl);
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.getByRole('heading', { name: 'Catálogo global de módulos y capacidades' })).toHaveCount(0);
    await installFixture(page, 403);
    await page.goto(catalogUrl);
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.locator('[data-testid="c0102-listado"]')).toHaveCount(0);
    await expect(page.getByText('PRIVATE_TENANT_B_SECRET_SHOULD_NOT_RENDER')).toHaveCount(0);
  });

  test('catalogo informativo: sin formularios ni autoridad de servidor inferida', async ({ page }) => {
    await installFixture(page);
    const mutating: string[] = [];
    page.on('request', r => {
      if (page.url().includes(catalogUrl) && !['GET', 'HEAD', 'OPTIONS'].includes(r.method()))
        mutating.push(r.method() + ' ' + r.url());
    });
    await page.goto(catalogUrl);
    await expect(page.getByRole('heading', { name: 'Catálogo global de módulos y capacidades' })).toBeVisible();
    await expect(page.getByRole('note')).toContainText('autorización efectiva');
    expect(await page.locator('[data-testid="c0102-listado"] li').count()).toBeGreaterThan(0);
    await expect(page.locator('[data-evidence-state="SENAL_CLIENTE_NO_AUTORITATIVA"]')).toHaveCount(0);
    await expect(page.locator('main.catalogo form, main.catalogo button, main.catalogo input, main.catalogo select, main.catalogo textarea')).toHaveCount(0);
    expect(mutating).toEqual([]);
  });

  test('Tab y Enter reales; navegacion repetida sin peticiones mutantes', async ({ page }) => {
    await installFixture(page);
    const mutating: string[] = [];
    page.on('request', r => {
      if (page.url().includes(catalogUrl) && !['GET', 'HEAD', 'OPTIONS'].includes(r.method()))
        mutating.push(r.method() + ' ' + r.url());
    });
    await page.goto(catalogUrl);
    const back = page.getByRole('link', { name: 'Volver a plataforma' });
    await expect(back).toHaveAttribute('href', '/plataforma');
    let reached = false;
    for (let i = 0; i < 110; i++) {
      await page.keyboard.press('Tab');
      if (await back.evaluate(element => document.activeElement === element)) {
        reached = true;
        break;
      }
    }
    expect(reached, 'Tab debe alcanzar el enlace sin focus programatico').toBe(true);
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/plataforma\/?(?:\?|$)/);
    await page.goto(catalogUrl);
    await expect(page.getByRole('heading', { name: 'Catálogo global de módulos y capacidades' })).toBeVisible();
    await back.click();
    await expect(page).toHaveURL(/\/plataforma\/?(?:\?|$)/);
    expect(mutating).toEqual([]);
  });
});
