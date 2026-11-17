import { expect, test, type Page } from '@playwright/test';

// Prueba el proveedor CSS y Angular reales con sesion HTTP sintetica, no JWT.
async function sesion(page: Page, allow: boolean): Promise<() => number> {
  let consultas = 0;
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0501m0503-synthetic-not-jwt');
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
    consultas++;
    await route.fulfill({
      status: allow ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(allow
        ? { success: true, data: {
            nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma',
            esAdministrador: true
          } }
        : { success: false, data: null, message: 'PRIVATE_PROVIDER_TOKEN' })
    });
  });
  return () => consultas;
}

test('C05-01.M05.03 swatches aplican cuatro variables CSS globales reales', async ({ page }) => {
  await sesion(page, true);
  await page.goto('/plataforma');
  await page.locator('details.design-system-preview summary').click();
  const muestras = await page.locator('.design-catalog .token-item').evaluateAll(items => {
    const names = ['--color-primary', '--color-accent', '--color-success', '--color-danger'];
    return items.map((item, index) => {
      const variable = names[index];
      const probe = document.createElement('div');
      probe.style.backgroundColor = 'var(' + variable + ')';
      document.body.appendChild(probe);
      const swatch = item.querySelector<HTMLElement>('.swatch');
      const actual = swatch ? getComputedStyle(swatch).backgroundColor : '';
      const expected = getComputedStyle(probe).backgroundColor;
      probe.remove();
      return {
        name: item.querySelector('code')?.textContent?.trim() ?? '',
        actual, expected,
        declared: getComputedStyle(document.documentElement).getPropertyValue(variable).trim()
      };
    });
  });
  expect(muestras.map(m => m.name)).toEqual([
    '--color-primary', '--color-accent', '--color-success', '--color-danger'
  ]);
  expect(new Set(muestras.map(m => m.actual)).size).toBe(4);
  for (const m of muestras) {
    expect(m.declared).toMatch(/^#[0-9a-fA-F]{3,8}$/);
    expect(m.actual).toBe(m.expected);
    expect(m.actual).toMatch(/^rgb/);
    expect(m.actual).not.toBe('rgba(0, 0, 0, 0)');
  }
});

test('C05-01.M05.03 Signal y CSS cambian juntos y restauran sin nuevas consultas', async ({ page }) => {
  const consultas = await sesion(page, true);
  const writes: string[] = [];
  page.on('request', r => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method());
  });
  await page.goto('/plataforma');
  await page.locator('details.design-system-preview summary').click();
  const c = page.locator('.design-catalog');
  const original = c.getByRole('button', { name: 'Institucional' });
  const contraste = c.getByRole('button', { name: 'Contraste alto' });
  const vista = c.locator('.brand-preview');
  const before = await vista.evaluate(e => getComputedStyle(e).backgroundColor);
  await contraste.click();
  await expect(contraste).toHaveAttribute('aria-pressed', 'true');
  await expect(vista).toHaveClass(/brand-preview-contrast/);
  const after = await vista.evaluate(e => getComputedStyle(e).backgroundColor);
  expect(after).not.toBe(before);
  await contraste.click();
  expect(await vista.evaluate(e => getComputedStyle(e).backgroundColor)).toBe(after);
  await original.click();
  await expect(original).toHaveAttribute('aria-pressed', 'true');
  await expect(vista).not.toHaveClass(/brand-preview-contrast/);
  expect(await vista.evaluate(e => getComputedStyle(e).backgroundColor)).toBe(before);
  expect(consultas()).toBe(1);
  expect(writes).toEqual([]);
});

test('C05-01.M05.03 proveedor denegado 403 no entrega tokens ni diagnostico', async ({ page }) => {
  await sesion(page, false);
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.design-catalog .swatch, .brand-choice, .brand-preview')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_PROVIDER_TOKEN')).toHaveCount(0);
});
