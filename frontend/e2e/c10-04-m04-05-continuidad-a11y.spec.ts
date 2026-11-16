import { expect, test, type Page } from '@playwright/test';

/** C10-04.M04.05: Chromium real, sesion y JWT sinteticos; no certifica backend ni pentest tenant. */
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function syntheticToken(page: Page): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c10-04-m04-05-not-a-real-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
}
async function session(page: Page, response: () => number): Promise<void> {
  await page.route(sessionUrl, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    const status = response();
    await route.fulfill({
      status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: { nombreUsuario: 'Sintetico', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, data: null, message: 'PRIVADO_NO_RENDERIZAR' })
    });
  });
}

test('anonimo no renderiza controles, encabezado privado ni acciones internas', async ({ page }) => {
  await page.goto('/plataforma/continuidad-operativa-v5');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.continuidad .controles li')).toHaveCount(0);
  await expect(page.getByRole('heading', { name: 'Controles de recuperación y aislamiento' })).toHaveCount(0);
});

test('403 sintetico no revela controles, datos ni estados privados', async ({ page }) => {
  await syntheticToken(page);
  await session(page, () => 403);
  await page.goto('/plataforma/continuidad-operativa-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.continuidad .controles li')).toHaveCount(0);
  await expect(page.getByText('PRIVADO_NO_RENDERIZAR')).toHaveCount(0);
});

test('ancho 320 768 1280: lectura, etiquetas, cuatro elementos y cero scroll horizontal', async ({ page }) => {
  await syntheticToken(page);
  await session(page, () => 200);
  for (const width of [320, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await page.goto('/plataforma/continuidad-operativa-v5');
    await expect(page.getByRole('heading', { name: 'Controles de recuperación y aislamiento' })).toBeVisible();
    await expect(page.locator('ol.controles')).toHaveAttribute('aria-labelledby', 'controles-title');
    await expect(page.locator('ol.controles li')).toHaveCount(4);
    await expect(page.getByRole('button', { name: 'Revalidar sesión de continuidad' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Regresar a plataforma' })).toHaveAttribute('href', '/plataforma');
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow).toBeLessThanOrEqual(2);
  }
});

test('foco visible con teclado, contraste legible y modo de contraste alto sin acciones de riesgo', async ({ page }) => {
  await syntheticToken(page);
  await session(page, () => 200);
  await page.emulateMedia({ contrast: 'more', reducedMotion: 'reduce' });
  await page.goto('/plataforma/continuidad-operativa-v5');
  const button = page.getByRole('button', { name: 'Revalidar sesión de continuidad' });
  // Comprobar acceso real por teclado; focus() programatico ocultaria defectos.
  let reachableWithTab = false;
  for (let attempt = 0; attempt < 35; attempt++) {
    await page.keyboard.press('Tab');
    if (await button.evaluate(element => element === document.activeElement)) {
      reachableWithTab = true;
      break;
    }
  }
  expect(reachableWithTab).toBe(true);
  await expect(button).toBeFocused();
  const style = await button.evaluate(node => {
    const computed = getComputedStyle(node);
    return {
      width: parseFloat(computed.outlineWidth),
      style: computed.outlineStyle,
      outlineColor: computed.outlineColor,
      backgroundColor: computed.backgroundColor
    };
  });
  expect(style.style).toBe('solid');
  // Quitar prefers-contrast:more debe fallar, pues la regla base usa 3px.
  expect(style.width).toBeGreaterThanOrEqual(4);
  const luminance = (color: string): number => {
    const channels = color.match(/\d+(?:\.\d+)?/g)?.slice(0, 3).map(Number);
    if (!channels || channels.length !== 3) throw new Error('Color CSS no evaluable: ' + color);
    const linear = channels.map(value => {
      const channel = value / 255;
      return channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4;
    });
    return linear[0] * 0.2126 + linear[1] * 0.7152 + linear[2] * 0.0722;
  };
  const foreground = luminance(style.outlineColor);
  const background = luminance(style.backgroundColor);
  expect((Math.max(foreground, background) + 0.05) / (Math.min(foreground, background) + 0.05)).toBeGreaterThanOrEqual(3);
  await expect(page.locator('.continuidad input, .continuidad textarea, .continuidad form')).toHaveCount(0);
});

test('error 503 posterior revoca presentacion y no filtra payload de backend', async ({ page }) => {
  await syntheticToken(page);
  let status = 200;
  await session(page, () => status);
  await page.goto('/plataforma/continuidad-operativa-v5');
  await expect(page.locator('ol.controles li')).toHaveCount(4);
  status = 503;
  await page.getByRole('button', { name: 'Revalidar sesión de continuidad' }).click();
  await expect(page.getByText('No fue posible validar la sesión. El acceso sigue restringido.')).toBeVisible();
  await expect(page.locator('ol.controles li')).toHaveCount(0);
  await expect(page.getByText('PRIVADO_NO_RENDERIZAR')).toHaveCount(0);
});
