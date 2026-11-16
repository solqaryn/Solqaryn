import { expect, test, type Page } from '@playwright/test';

// Synthetic tenant verification is a UI fixture, not live JWT/RBAC or database validation.
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

async function fixture(page: Page, allowed: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0804-m0405-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', '71');
  });
  await page.route(/\/tenant-context\/71(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    await route.fulfill({
      status: allowed ? 200 : 403, headers: cors, contentType: 'application/json',
      body: JSON.stringify(allowed
        ? { success: true, data: { usuarioId: 7, empresaId: 71, rolId: 3, rolNombre: 'Lectura', esAdministrador: false } }
        : { success: false, message: 'DO_NOT_EXPOSE_TENANT_DETAILS' })
    });
  });
}

test('C08-04.M04.05 anonymous cannot see protected capability preview', async ({ page }) => {
  await page.goto('/extensiones-sectoriales');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.sector-readonly, .sector-card')).toHaveCount(0);
});

test('C08-04.M04.05 denied tenant cannot see previews or private errors', async ({ page }) => {
  await fixture(page, false);
  await page.goto('/extensiones-sectoriales');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.sector-card')).toHaveCount(0);
  await expect(page.getByText('DO_NOT_EXPOSE_TENANT_DETAILS')).toHaveCount(0);
});

test('C08-04.M04.05 landmarks and real Tab focus navigation without forced focus', async ({ page }) => {
  await fixture(page, true);
  await page.goto('/extensiones-sectoriales');
  const view = page.locator('main.sector-readonly');
  await expect(view).toHaveAttribute('aria-labelledby', 'sector-title');
  await expect(view.getByRole('heading', { name: 'Capacidades por rubro' })).toBeVisible();
  const nav = page.getByRole('navigation', { name: 'Navegación de extensiones sectoriales' });
  await expect(nav).toBeVisible();
  const link = nav.getByRole('link', { name: 'Volver al panel' });
  let tabReached = false;
  for (let i = 0; i < 60; i++) {
    await page.keyboard.press('Tab');
    if (await link.evaluate(el => document.activeElement === el)) {
      tabReached = true;
      break;
    }
  }
  expect(tabReached, 'link must be reached by real Tab starting from page').toBe(true);
  const ring = await link.evaluate(el => {
    const css = getComputedStyle(el);
    return { style: css.outlineStyle, width: parseFloat(css.outlineWidth) };
  });
  expect(ring.style).not.toBe('none');
  expect(ring.width).toBeGreaterThanOrEqual(3);
  const rect = await link.boundingBox();
  expect(rect).not.toBeNull();
  expect(rect!.height).toBeGreaterThanOrEqual(44);
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/\/perfil(?:\/|\?|$)/);
});

test('C08-04.M04.05 responsive 320 375 768 1280 avoids horizontal overflow', async ({ page }) => {
  await fixture(page, true);
  await page.goto('/extensiones-sectoriales');
  const preview = page.locator('.sector-readonly');
  await expect(preview).toBeVisible();
  await expect(preview.locator('.sector-card')).toHaveCount(4);
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth
    );
    expect(overflow, 'horizontal overflow at ' + width + 'px').toBeLessThanOrEqual(2);
    await expect(preview.getByRole('heading', { name: 'Ámbitos previstos, no autorizaciones' })).toBeVisible();
  }
});

test('C08-04.M04.05 both visual presets meet 4.5:1 tag text contrast', async ({ page }) => {
  await fixture(page, true);
  await page.goto('/extensiones-sectoriales');
  const root = page.locator('.sector-readonly');
  const tag = root.locator('.sector-tag').first();
  await expect(tag).toBeVisible();
  const ratio = async () => tag.evaluate(el => {
    const style = getComputedStyle(el);
    const channels = (value: string): number[] => {
      const matched = value.match(/\d+(?:\.\d+)?/g);
      if (!matched || matched.length < 3) throw new Error('Expected opaque CSS rgb color');
      return matched.slice(0, 3).map(Number);
    };
    const luminance = (value: string): number => {
      const rgb = channels(value).map(channel => {
        const s = channel / 255;
        return s <= 0.04045 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4;
      });
      return rgb[0] * 0.2126 + rgb[1] * 0.7152 + rgb[2] * 0.0722;
    };
    const a = luminance(style.color), b = luminance(style.backgroundColor);
    return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
  });
  expect(await ratio(), 'institutional tag contrast').toBeGreaterThanOrEqual(4.5);
  await root.evaluate(el => el.classList.add('sector-readonly--contraste'));
  expect(await ratio(), 'high-contrast tag color on dark preset').toBeGreaterThanOrEqual(4.5);
  await expect(root.getByRole('note')).toContainText('JWT');
});
