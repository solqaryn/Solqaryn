import { expect, test, type Page } from '@playwright/test';

// C05-01.M05.04: navegador Angular real con sesión HTTP sintética; no JWT/tenant live.
async function sesionLocal(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0501m0504-synthetic-not-jwt');
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
    await route.fulfill({
      status: 200,
      headers,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        data: { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true }
      })
    });
  });
}

test('C05-01.M05.04 presenta catálogo progresivo completo sin overflow ni escritura', async ({ page }) => {
  await sesionLocal(page);
  const writes: string[] = [];
  page.on('request', r => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method());
  });
  await page.goto('/plataforma');
  const details = page.locator('details.design-system-preview');
  const summary = details.locator('summary');
  const content = details.locator('.design-catalog');
  await expect(details).not.toHaveAttribute('open', '');
  await expect(content).toBeHidden();
  await summary.click();
  await expect(details).toHaveAttribute('open', '');
  await expect(content.getByRole('heading', { name: 'Tokens y componentes de referencia' })).toBeVisible();
  await expect(content.locator('.token-item')).toHaveCount(4);
  await expect(content.locator('.state-example')).toHaveCount(5);
  await expect(content.locator('.brand-choice')).toHaveCount(2);
  await expect(content.locator('form, input, select, textarea')).toHaveCount(0);
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await expect(content.locator('.token-item')).toHaveCount(4);
    const overflow = await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, 'sin scroll horizontal a ' + width).toBeLessThanOrEqual(2);
  }
  await summary.click();
  await expect(details).not.toHaveAttribute('open', '');
  await expect(content).toBeHidden();
  expect(writes, 'catálogo informativo sin escrituras HTTP').toEqual([]);
});

test('C05-01.M05.04 acepta selección local de presets, identifica estado y no duplica cambios', async ({ page }) => {
  await sesionLocal(page);
  const writes: string[] = [];
  page.on('request', r => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method());
  });
  await page.goto('/plataforma');
  await page.locator('details.design-system-preview summary').click();
  const catalog = page.locator('.design-catalog');
  const institucional = catalog.getByRole('button', { name: 'Institucional' });
  const alto = catalog.getByRole('button', { name: 'Contraste alto' });
  await expect(institucional).toHaveAttribute('aria-pressed', 'true');
  await expect(alto).toHaveAttribute('aria-pressed', 'false');
  await alto.click();
  await expect(alto).toHaveAttribute('aria-pressed', 'true');
  await expect(institucional).toHaveAttribute('aria-pressed', 'false');
  const visual = catalog.locator('.brand-preview');
  await expect(visual).toContainText('Contraste alto');
  await expect(visual).toContainText('Componente de referencia');
  await expect(visual).toHaveClass(/brand-preview-contrast/);
  const stable = await visual.textContent();
  await alto.click();
  await expect(visual).toHaveText(stable ?? '');
  await institucional.click();
  await expect(institucional).toHaveAttribute('aria-pressed', 'true');
  await expect(visual).toContainText('Institucional');
  await expect(visual).not.toHaveClass(/brand-preview-contrast/);
  expect(writes, 'cambio visual no modifica backend').toEqual([]);
});
