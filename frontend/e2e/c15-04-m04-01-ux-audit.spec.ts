import { expect, test, type Page } from '@playwright/test';

// HTTP sintetico en navegador real. La autorizacion JWT/DB live no se certifica.
async function plataformaSynthetic(page: Page, allow: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1504m0401-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers });
      return;
    }
    await route.fulfill({ status: allow ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(allow
        ? { success: true, data: {
            nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma',
            esAdministrador: true
          } }
        : { success: false, data: null, message: 'PRIVATE_UX_AUDIT_SECRET' })
    });
  });
}

test('C15-04.M04.01 sin sesion no accede a ruta de auditoria privada', async ({ page }) => {
  await page.goto('/plataforma/auditoria-ux-v5');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('[data-testid="ux-audit-v19"]')).toHaveCount(0);
});

test('C15-04.M04.01 navega desde consola a cinco superficies NO_MEDIDO, sin escrituras', async ({ page }) => {
  await plataformaSynthetic(page, true);
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  await page.goto('/plataforma');
  await page.getByRole('link', { name: 'Auditoría UX' }).click();
  await expect(page).toHaveURL(/\/plataforma\/auditoria-ux-v5$/);
  const view = page.locator('[data-testid="ux-audit-v19"]');
  await expect(view.getByRole('heading', { name: 'Auditoría UX, accesibilidad y rendimiento' })).toBeVisible();
  await expect(view.locator('[data-testid="ux-surface"]')).toHaveCount(5);
  await expect(view.locator('[data-testid="ux-surface"][data-state="NO_MEDIDO"]')).toHaveCount(5);
  await expect(view.getByText('SIN_VERIFICAR')).toHaveCount(2);
  await expect(view.locator('form, input, button, select, textarea')).toHaveCount(0);
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await expect(view.locator('[data-testid="ux-surface"]')).toHaveCount(5);
    const overflow = await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, 'sin desplazamiento horizontal a ' + width).toBeLessThanOrEqual(2);
  }
  expect(writes).toEqual([]);
  await view.getByRole('link', { name: 'Volver a administración global' }).click();
  await expect(page).toHaveURL(/\/plataforma$/);
});

test('C15-04.M04.01 403 impide la auditoria y oculta diagnostico privado', async ({ page }) => {
  await plataformaSynthetic(page, false);
  await page.goto('/plataforma/auditoria-ux-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('[data-testid="ux-audit-v19"]')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_UX_AUDIT_SECRET')).toHaveCount(0);
});
