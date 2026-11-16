import { expect, test, type Page } from '@playwright/test';

/** API y token sintéticos; no certifican JWT real ni datos productivos. */
async function mockSession(page: Page, allow: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c05-01-m04-01-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    return route.fulfill({ status: allow ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(allow ?
        { success: true, data: { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } } :
        { success: false, data: null, message: 'PRIVATE_DESIGN_TOKEN_SECRET' }) });
  });
}

test('sin token no se expone la biblioteca de plataforma', async ({ page }) => {
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.design-catalog .token-item')).toHaveCount(0);
});

test('403 sintético deniega tokens privados sin filtrar el mensaje de proveedor', async ({ page }) => {
  await mockSession(page, false);
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.design-catalog .token-item')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_DESIGN_TOKEN_SECRET')).toHaveCount(0);
});

test('biblioteca read-only abre por teclado y es responsive en 320/768/1280', async ({ page }) => {
  await mockSession(page, true);
  await page.goto('/plataforma');
  const summary = page.locator('details.design-system-preview summary');
  await expect(summary).toBeVisible();
  await summary.focus();
  await page.keyboard.press('Enter');
  const catalog = page.locator('.design-catalog');
  await expect(catalog.getByRole('heading', { name: 'Tokens y componentes de referencia' })).toBeVisible();
  await expect(catalog.locator('.token-item')).toHaveCount(4);
  await expect(catalog.locator('form, input, button:not(.brand-choice)')).toHaveCount(0);
  for (const width of [320, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await expect(catalog.locator('.token-item')).toHaveCount(4);
    const result = await page.evaluate(() => ({
      overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
      visibleColors: document.querySelectorAll('.design-catalog .swatch').length
    }));
    expect(result.visibleColors).toBe(4);
    expect(result.overflow, 'sin overflow a ' + width).toBeLessThanOrEqual(2);
  }
});

test('C05-01.M04.02 cinco estados sin acciones y sin overflow 320/768/1280', async ({ page }) => {
  await mockSession(page, true);
  await page.goto('/plataforma');
  await page.locator('details.design-system-preview summary').click();
  const catalog = page.locator('.design-catalog');
  await expect(catalog.getByRole('heading', { name: 'Estados de interfaz' })).toBeVisible();
  await expect(catalog.locator('.state-example')).toHaveCount(5);
  await expect(catalog.locator('.state-catalog form, .state-catalog input, .state-catalog button')).toHaveCount(0);
  await expect(catalog.getByText('Permiso insuficiente')).toBeVisible();
  for (const width of [320, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await expect(catalog.locator('.state-example')).toHaveCount(5);
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, 'desbordamiento horizontal a ' + width).toBeLessThanOrEqual(2);
  }
});

test('C05-01.M04.02 deniega toda muestra de estados ante 403 de plataforma', async ({ page }) => {
  await mockSession(page, false);
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/plataforma=denegado$/);
  await expect(page.locator('.state-example')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_DESIGN_TOKEN_SECRET')).toHaveCount(0);
});

test('C05-01.M04.03 selector de branding local y responsive 320/768/1280', async ({ page }) => {
  await mockSession(page, true);
  await page.goto('/plataforma');
  await page.locator('details.design-system-preview summary').click();
  const catalog = page.locator('.design-catalog');
  await expect(catalog.locator('.brand-choice')).toHaveCount(2);
  await expect(catalog.locator('.brand-preview')).toContainText('Institucional');
  await catalog.getByRole('button', { name: 'Contraste alto' }).click();
  await expect(catalog.locator('.brand-preview-contrast')).toBeVisible();
  await expect(catalog.locator('.brand-preview')).toContainText('Componente de referencia');
  await expect(catalog.locator('.brand-catalog form, .brand-catalog input')).toHaveCount(0);
  for (const width of [320, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await expect(catalog.locator('.brand-preview-contrast')).toBeVisible();
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, 'sin desbordamiento a ' + width).toBeLessThanOrEqual(2);
  }
});

test('C05-01.M04.03 403 sintético no expone branding privado', async ({ page }) => {
  await mockSession(page, false);
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/plataforma=denegado$/);
  await expect(page.locator('.brand-preview, .brand-choice')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_DESIGN_TOKEN_SECRET')).toHaveCount(0);
});


test('C05-01.M04.04 navega con Tab y Enter sin escrituras ni duplicación', async ({ page }) => {
  await mockSession(page, true);
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  await page.goto('/plataforma');
  await page.locator('details.design-system-preview summary').click();
  const contraste = page.getByRole('button', { name: 'Contraste alto' });
  for (let intento = 0; intento < 20; intento++) {
    if (await contraste.evaluate(elemento => elemento === document.activeElement)) break;
    await page.keyboard.press('Tab');
  }
  await expect(contraste).toBeFocused();
  await page.keyboard.press('Enter');
  const vista = page.locator('.design-catalog .brand-preview');
  await expect(vista).toContainText('Contraste alto');
  await expect(contraste).toHaveAttribute('aria-pressed', 'true');
  const texto = await vista.textContent();
  await page.keyboard.press('Enter');
  await expect(vista).toHaveText(texto ?? '');
  expect(writes, 'sin POST/PATCH/PUT/DELETE al seleccionar').toEqual([]);
  await expect(page.locator('.brand-catalog form, .brand-catalog input')).toHaveCount(0);
});

test('C05-01.M04.04 denegación 403 impide acciones y no filtra error privado', async ({ page }) => {
  await mockSession(page, false);
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/plataforma=denegado$/);
  await expect(page.locator('.brand-catalog button, .brand-preview')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_DESIGN_TOKEN_SECRET')).toHaveCount(0);
});

test('C05-01.M04.05 contraste, etiquetas, foco y teclado', async ({ page }) => {
  await mockSession(page, true);
  await page.goto('/plataforma');
  await page.locator('details.design-system-preview summary').click();
  const catalog = page.locator('.design-catalog');
  const choices = catalog.getByRole('group', { name: 'Seleccionar una vista previa permitida' });
  await expect(choices).toHaveAttribute('aria-describedby', 'design-brand-actions-help');
  await expect(page.locator('#design-brand-actions-help')).toBeVisible();
  const button = catalog.getByRole('button', { name: 'Contraste alto' });
  await button.focus();
  await expect(button).toBeFocused();
  const visual = await button.evaluate(element => {
    const style = getComputedStyle(element);
    const luminance = (color: string) => {
      const channels = (color.match(/[\d.]+/g) ?? []).slice(0, 3).map(Number);
      if (channels.length !== 3) return 0;
      const linear = channels.map(value => {
        const c = value / 255;
        return c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
      });
      return linear[0] * 0.2126 + linear[1] * 0.7152 + linear[2] * 0.0722;
    };
    const a = luminance(style.color);
    const b = luminance(style.backgroundColor);
    return {
      outline: style.outlineStyle,
      outlineWidth: parseFloat(style.outlineWidth),
      contrast: (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05),
      targetHeight: element.getBoundingClientRect().height
    };
  });
  expect(visual.outline).not.toBe('none');
  expect(visual.outlineWidth).toBeGreaterThanOrEqual(3);
  expect(visual.contrast).toBeGreaterThanOrEqual(4.5);
  expect(visual.targetHeight).toBeGreaterThanOrEqual(44);
  await button.press('Enter');
  await expect(button).toHaveAttribute('aria-pressed', 'true');
  await expect(catalog.locator('[role="status"]')).toContainText('Contraste alto');
  await expect(catalog.locator('.brand-catalog form, .brand-catalog input')).toHaveCount(0);
});

test('C05-01.M04.05 responsive y colores forzados conservan control seguro', async ({ page }) => {
  await mockSession(page, true);
  await page.goto('/plataforma');
  await page.locator('details.design-system-preview summary').click();
  const catalog = page.locator('.design-catalog');
  const button = catalog.getByRole('button', { name: 'Contraste alto' });
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await expect(button).toBeVisible();
    await expect(catalog.locator('.token-item')).toHaveCount(4);
    await expect(catalog.locator('.state-example')).toHaveCount(5);
    const overflow = await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, 'sin overflow horizontal a ' + width).toBeLessThanOrEqual(2);
  }
  await page.emulateMedia({ forcedColors: 'active', contrast: 'more', reducedMotion: 'reduce' });
  await button.focus();
  await expect(button).toBeFocused();
  const forced = await button.evaluate(element => {
    const computed = getComputedStyle(element);
    return { border: computed.borderStyle, outline: computed.outlineStyle };
  });
  expect(forced.border).not.toBe('none');
  expect(forced.outline).not.toBe('none');
  await button.press('Enter');
  await expect(button).toHaveAttribute('aria-pressed', 'true');
  await expect(catalog.locator('.brand-preview')).toContainText('Contraste alto');
});

