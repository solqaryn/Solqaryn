import { expect, test, type Page } from '@playwright/test';

const qaUrl = '/plataforma/qa-global-v19';
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

/** Only a synthetic browser fixture: NEVER evidence of real JWT/RBAC or tenant isolation. */
async function installFixture(page: Page, status: 200 | 403 = 200): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1401-m0404-synthetic-not-jwt');
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
        ? { success: true, data: { nombreUsuario: 'Synthetic QA', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'SECRET_TENANT_B_PRIVATE_DSN' })
    });
  });
}

test.describe('C14-01.M04.04 - navegacion y acciones seguras en QA privado', () => {
  test.describe.configure({ retries: 0 });

  test('anonimo y sesion 403: redireccion, mensaje privado oculto y sin filtracion', async ({ page }) => {
    await page.goto(qaUrl);
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.getByTestId('c1401-qa-global')).toHaveCount(0);
    await installFixture(page, 403);
    await page.goto(qaUrl);
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.getByTestId('c1401-qa-global')).toHaveCount(0);
    await expect(page.getByText('SECRET_TENANT_B_PRIVATE_DSN')).toHaveCount(0);
  });

  test('mensaje de evidencia pendiente y ausencia real de formularios o aprobaciones', async ({ page }) => {
    await installFixture(page);
    const writes: string[] = [];
    page.on('request', request => {
      if (page.url().includes(qaUrl) && !['GET', 'HEAD', 'OPTIONS'].includes(request.method()))
        writes.push(request.method());
    });
    await page.goto(qaUrl);
    await expect(page.getByRole('heading', { name: 'Consulta de QA automático y cobertura global' })).toBeVisible();
    await expect(page.locator('main.qa-global .qa-controles li')).toHaveCount(5);
    await expect(page.locator('main.qa-global .pendiente')).toHaveCount(5);
    await expect(page.locator('main.qa-global button, main.qa-global form, main.qa-global input, main.qa-global textarea, main.qa-global select')).toHaveCount(0);
    await expect(page.getByRole('note').first()).toContainText('SUCCESS');
    const source = page.getByRole('link', { name: /Consultar GitHub Actions/ });
    await expect(source).toHaveAttribute('target', '_blank');
    await expect(source).toHaveAttribute('rel', /noopener/);
    expect(writes).toEqual([]);
  });

  test('Tab real + Enter y navegacion repetida no exponen acciones mutantes desde el panel', async ({ page }) => {
    await installFixture(page);
    const writesFromQa: string[] = [];
    page.on('request', request => {
      if (page.url().includes(qaUrl) && !['GET', 'HEAD', 'OPTIONS'].includes(request.method()))
        writesFromQa.push(request.method());
    });
    await page.goto(qaUrl);
    const back = page.getByRole('link', { name: 'Volver a plataforma' });
    await expect(back).toHaveAttribute('href', '/plataforma');
    let focused = false;
    for (let attempt = 0; attempt < 110; attempt++) {
      await page.keyboard.press('Tab');
      if (await back.evaluate(el => document.activeElement === el)) {
        focused = true;
        break;
      }
    }
    expect(focused, 'Volver a plataforma debe alcanzarse usando Tab real').toBe(true);
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/plataforma\/?(?:\?|$)/);
    await page.goto(qaUrl);
    await expect(page.getByRole('heading', { name: 'Consulta de QA automático y cobertura global' })).toBeVisible();
    await page.getByRole('link', { name: 'Volver a plataforma' }).click();
    await expect(page).toHaveURL(/\/plataforma\/?(?:\?|$)/);
    expect(writesFromQa).toEqual([]);
  });
});
