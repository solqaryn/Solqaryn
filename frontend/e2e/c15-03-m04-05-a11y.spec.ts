import { expect, test, type Page } from '@playwright/test';
// C15-03.M04.05: browser real; sesion y token sinteticos, no JWT/tenant backend.
const url = /\/plataforma\/sesion(?:\?|$)/;
async function grant(page: Page, allowed: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'synthetic-browser-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(url, async route => {
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
      status: allowed ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(allowed ? {
        success: true, data: {
          nombreUsuario: 'Sintetico', rolNombre: 'Admin',
          ambito: 'Plataforma', esAdministrador: true
        }
      } : { success: false, data: null, message: 'DENEGADO' })
    });
  });
}
test('anonimo sin token no ve atajo ni matriz', async ({ page }) => {
  await page.goto('/plataforma/auditoria-tenancy-v5');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByRole('link', { name: 'Saltar a los cuatro frentes de revisión' })).toHaveCount(0);
});
test('403 sintetico deniega matriz y atajo', async ({ page }) => {
  await grant(page, false);
  await page.goto('/plataforma/auditoria-tenancy-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.fronteras li')).toHaveCount(0);
});
test('grant simulado: foco teclado, etiquetas, enlace y ausencia de acciones', async ({ page }) => {
  await grant(page, true);
  await page.goto('/plataforma/auditoria-tenancy-v5');
  const skip = page.getByRole('link', { name: 'Saltar a los cuatro frentes de revisión' });
  await expect(skip).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Cuatro frentes de comprobación futura' })).toHaveAttribute('tabindex', '-1');
  await expect(page.locator('ol.fronteras')).toHaveAttribute('aria-labelledby', 'fronteras-title');
  await expect(page.locator('.fronteras li')).toHaveCount(4);
  await expect(page.locator('.tenancy-audit form, .tenancy-audit input, .tenancy-audit textarea')).toHaveCount(0);
  await expect(page.locator('.tenancy-audit button')).toHaveCount(1);
  await expect(page.getByRole('button', { name: 'Revalidar sesión de auditoría de tenancy' })).toBeVisible();
  // El enlace debe ser alcanzable por teclado desde el foco inicial sin .focus().
  let alcanzadoPorTab = false;
  for (let intento = 0; intento < 45; intento++) {
    await page.keyboard.press('Tab');
    if (await skip.evaluate(elemento => elemento === document.activeElement)) {
      alcanzadoPorTab = true;
      break;
    }
  }
  expect(alcanzadoPorTab, 'Skip-link alcanzable con Tab real').toBe(true);
  await expect(skip).toBeFocused();
  const outline = await skip.evaluate(el => {
    const css = getComputedStyle(el);
    return { width: parseFloat(css.outlineWidth), style: css.outlineStyle };
  });
  expect(outline.style).toBe('solid');
  expect(outline.width).toBeGreaterThanOrEqual(3);
  await page.keyboard.press('Enter');
  await expect(page.getByRole('heading', { name: 'Cuatro frentes de comprobación futura' })).toBeFocused();
  await expect(page).toHaveURL(/\/plataforma\/auditoria-tenancy-v5(?:#fronteras-title)?$/);
});
test('320/768 px: sin overflow y con etiquetas visibles', async ({ page }) => {
  await grant(page, true);
  for (const width of [320, 768]) {
    await page.setViewportSize({ width, height: 800 });
    await page.goto('/plataforma/auditoria-tenancy-v5');
    await expect(page.locator('.fronteras li')).toHaveCount(4);
    await expect(page.getByRole('link', { name: 'Regresar a administración de plataforma' })).toBeVisible();
    expect(await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
  }
});

test('forced-colors: contorno de foco alcanza al menos 3:1 frente al fondo', async ({ page }) => {
  await grant(page, true);
  await page.emulateMedia({ forcedColors: 'active', reducedMotion: 'reduce' });
  await page.goto('/plataforma/auditoria-tenancy-v5');
  const skip = page.getByRole('link', { name: 'Saltar a los cuatro frentes de revisión' });
  let alcanzado = false;
  for (let intento = 0; intento < 45; intento++) {
    await page.keyboard.press('Tab');
    if (await skip.evaluate(elemento => elemento === document.activeElement)) {
      alcanzado = true;
      break;
    }
  }
  expect(alcanzado, 'Contorno evaluado con foco real de Tab').toBe(true);
  const relacion = await skip.evaluate(elemento => {
    if (!matchMedia('(forced-colors: active)').matches) return 0;
    const luminosidad = (color: string): number | null => {
      const canales = color.match(/[0-9]+(?:[.][0-9]+)?/g)?.map(Number) ?? [];
      if (canales.length < 3 || canales.length > 4 || canales.some(v => !Number.isFinite(v))) return null;
      if (canales.length === 4 && canales[3] < 1) return null;
      const canalesLineales = canales.slice(0, 3).map(v => {
        const srgb = v / 255;
        return srgb <= 0.04045 ? srgb / 12.92 : ((srgb + 0.055) / 1.055) ** 2.4;
      });
      return canalesLineales[0] * 0.2126 + canalesLineales[1] * 0.7152 + canalesLineales[2] * 0.0722;
    };
    const contorno = getComputedStyle(elemento);
    // Comparar colores de la misma superficie sólida: la sección padre puede
    // heredar un fondo transparente bajo emulación forced-colors en Chromium.
    if (contorno.outlineStyle === 'none' || parseFloat(contorno.outlineWidth) < 3) return 0;
    const a = luminosidad(contorno.outlineColor);
    const b = luminosidad(contorno.backgroundColor);
    if (a === null || b === null) return 0;
    return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
  });
  expect(relacion, 'Contraste de foco forzado >= 3:1').toBeGreaterThanOrEqual(3);
});
