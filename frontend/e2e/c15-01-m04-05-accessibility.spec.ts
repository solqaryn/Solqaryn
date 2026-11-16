import { expect, test, type Page } from '@playwright/test';

/** C15-01.M04.05: navegador real, sesión sintética; NO JWT HTTP ni auditoría productiva. */
async function setSession(page: Page, mode: 'authorized' | 'forbidden' | 'wrong-scope'): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1501-m0405-synthetic-not-jwt');
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
    if (mode === 'forbidden') {
      await route.fulfill({
        status: 403, headers, contentType: 'application/json',
        body: JSON.stringify({ success: false, message: 'PRIVATE_TRACE_NEVER_RENDER' })
      });
      return;
    }
    await route.fulfill({
      status: 200, headers, contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        data: {
          nombreUsuario: 'Test Synthetic',
          rolNombre: mode === 'wrong-scope' ? 'Empresa' : 'Admin',
          ambito: mode === 'wrong-scope' ? 'Empresa' : 'Plataforma',
          esAdministrador: mode === 'authorized'
        }
      })
    });
  });
}

test('C15-01.M04.05 anónimo, 403 y ámbito Empresa no exponen dimensiones', async ({ page }) => {
  await page.goto('/plataforma/trazabilidad-cobertura-v5');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.coverage-list li')).toHaveCount(0);
  for (const mode of ['forbidden', 'wrong-scope'] as const) {
    await setSession(page, mode);
    await page.goto('/plataforma/trazabilidad-cobertura-v5');
    await expect(page).toHaveURL(/\/login/);
    await expect(page.locator('.coverage-list li')).toHaveCount(0);
    await expect(page.getByText('PRIVATE_TRACE_NEVER_RENDER')).toHaveCount(0);
  }
});

test('C15-01.M04.05 responsive 320/375/768/1280 sin desbordamiento', async ({ page }) => {
  await setSession(page, 'authorized');
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await page.goto('/plataforma/trazabilidad-cobertura-v5');
    await expect(page.getByRole('heading', { name: 'Revisión de cobertura del plan' })).toBeVisible();
    await expect(page.locator('.coverage-list li')).toHaveCount(4);
    const overflow = await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, `overflow horizontal en ${width}px`).toBeLessThanOrEqual(2);
  }
});

test('C15-01.M04.05 semántica y labels, cero acciones ni resultado fabricado', async ({ page }) => {
  await setSession(page, 'authorized');
  await page.goto('/plataforma/trazabilidad-cobertura-v5');
  await expect(page.locator('main[aria-labelledby="coverage-title"]')).toHaveCount(1);
  await expect(page.locator('section[aria-labelledby="coverage-dimensions"]')).toHaveCount(1);
  await expect(page.locator('.coverage-shell [role="status"]')).toContainText('NO_MEDIDO');
  await expect(page.locator('.unverified[aria-label="Evidencia no verificada"]')).toHaveCount(4);
  await expect(page.locator('.coverage-shell button, .coverage-shell form, .coverage-shell input')).toHaveCount(0);
  await expect(page.getByRole('note')).toContainText('No hay operaciones');
});

test('C15-01.M04.05 teclado Tab llega a regreso, foco 3px y blanco 44px', async ({ page }) => {
  await setSession(page, 'authorized');
  await page.goto('/plataforma/trazabilidad-cobertura-v5');
  const link = page.getByRole('link', { name: 'Regresar a administración de plataforma' });
  await expect(link).toBeVisible();
  let reached = false;
  for (let i = 0; i < 90; i++) {
    await page.keyboard.press('Tab');
    if (await link.evaluate(el => document.activeElement === el)) {
      reached = true;
      break;
    }
  }
  expect(reached, 'link alcanzable desde Tab real').toBe(true);
  const style = await link.evaluate(el => {
    const rect = el.getBoundingClientRect();
    return { width: rect.width, height: rect.height,
      outline: parseFloat(getComputedStyle(el).outlineWidth) };
  });
  expect(style.width).toBeGreaterThanOrEqual(44);
  expect(style.height).toBeGreaterThanOrEqual(44);
  expect(style.outline).toBeGreaterThanOrEqual(3);
});

test('C15-01.M04.05 contraste computado de texto principal >= 4.5:1', async ({ page }) => {
  await setSession(page, 'authorized');
  await page.goto('/plataforma/trazabilidad-cobertura-v5');
  const ratio = await page.locator('.coverage-shell [role="status"]').evaluate(el => {
    const channels = (css: string): number[] =>
      (css.match(/[\d.]+/g) ?? []).slice(0, 4).map(Number);
    const luminance = (c: number[]): number => {
      const rgb = c.slice(0, 3).map(v => {
        const s = v / 255;
        return s <= 0.04045 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4;
      });
      return 0.2126 * rgb[0] + 0.7152 * rgb[1] + 0.0722 * rgb[2];
    };
    const fg = channels(getComputedStyle(el).color);
    let bg = [255, 255, 255];
    for (let n: Element | null = el; n; n = n.parentElement) {
      const candidate = channels(getComputedStyle(n).backgroundColor);
      if (candidate.length >= 3 && (candidate.length < 4 || candidate[3] >= 1)) {
        bg = candidate;
        break;
      }
    }
    const a = luminance(fg), b = luminance(bg);
    return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
  });
  expect(ratio).toBeGreaterThanOrEqual(4.5);
});
