import { expect, test, type Page } from '@playwright/test';

// C05-04.M04.05 — browser regression, synthetic session only (not JWT/MySQL evidence).
const headers = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

async function session(page: Page, allow: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0504-m0405-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers });
      return;
    }
    await route.fulfill({
      status: allow ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(allow
        ? { success: true, data: { nombreUsuario: 'A11y sintético', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'PRIVATE_TENANT_MESSAGE_MUST_NOT_RENDER' })
    });
  });
  await page.route(/\/plataforma\/empresas(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers });
      return;
    }
    await route.fulfill({
      status: 200, headers, contentType: 'application/json',
      body: JSON.stringify({ success: true, data: { items: [], total: 0, pagina: 1, tamano: 25, puedeCrear: false } })
    });
  });
}

test('C05-04.M04.05 sin token: no existe superficie privada', async ({ page }) => {
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.ui-lightness-v5')).toHaveCount(0);
});

test('C05-04.M04.05 autoridad 403: sin panel ni mensaje de tenant', async ({ page }) => {
  await session(page, false);
  await page.goto('/plataforma');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.locator('.ui-lightness-v5')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_TENANT_MESSAGE_MUST_NOT_RENDER')).toHaveCount(0);
});

test('C05-04.M04.05 teclado, nombres accesibles, foco y estado compacto', async ({ page }) => {
  await session(page, true);
  await page.goto('/plataforma');
  const summary = page.locator('.ui-lightness-v5 > summary');
  await expect(summary).toBeVisible();
  // Alcanzar el summary mediante Tab reales: .focus() no demuestra accesibilidad.
  let summaryReached = false;
  for (let step = 0; step < 100; step++) {
    await page.keyboard.press('Tab');
    if (await summary.evaluate(el => el === document.activeElement)) {
      summaryReached = true;
      break;
    }
  }
  expect(summaryReached, 'summary debe ser alcanzable con teclado sin JS focus').toBe(true);
  await page.keyboard.press('Enter');
  const panel = page.locator('.ui-lightness-v5');
  await expect(panel.getByRole('heading', { name: 'Presupuesto de ligereza visual' })).toBeVisible();
  await expect(panel.locator('.ui-lightness-v5__checks li')).toHaveCount(3);
  await expect(summary).toBeFocused();
  const focusStyle = await summary.evaluate(el => {
    const css = getComputedStyle(el);
    return { width: Number.parseFloat(css.outlineWidth), style: css.outlineStyle };
  });
  expect(focusStyle.style).not.toBe('none');
  expect(focusStyle.width).toBeGreaterThanOrEqual(2);
  await page.keyboard.press('Tab');
  const toggle = panel.getByRole('button', { name: 'Reducir detalles' });
  await expect(toggle).toBeFocused();
  await expect(toggle).toHaveAttribute('aria-pressed', 'false');
  await page.keyboard.press('Enter');
  await expect(panel.locator('.ui-lightness-v5__checks li')).toHaveCount(0);
  await expect(panel.getByRole('button', { name: 'Ver criterios' })).toHaveAttribute('aria-pressed', 'true');
  await page.keyboard.press('Enter');
  await expect(panel.locator('.ui-lightness-v5__checks li')).toHaveCount(3);
});

test('C05-04.M04.05 320/375/768/1280: foco, labels y cero overflow', async ({ page }) => {
  await session(page, true);
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await page.goto('/plataforma');
    const summary = page.locator('.ui-lightness-v5 > summary');
    await expect(summary).toBeVisible();
    await summary.click();
    const panel = page.locator('.ui-lightness-v5');
    await expect(panel.getByRole('heading', { name: 'Presupuesto de ligereza visual' })).toBeVisible();
    await expect(panel.getByRole('button', { name: 'Reducir detalles' })).toBeVisible();
    expect(await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth),
      'desbordamiento horizontal en ' + width).toBeLessThanOrEqual(2);
  }
  expect(writes, 'la superficie visual es solo de consulta').toEqual([]);
});

test('C05-04.M04.05 contraste calculado de texto real contra fondos compuestos', async ({ page }) => {
  await session(page, true);
  await page.goto('/plataforma');
  await page.locator('.ui-lightness-v5 > summary').click();
  const targets = page.locator('.ui-lightness-v5 > summary, .ui-lightness-v5__checks strong, .ui-lightness-v5__checks span');
  await expect(targets).toHaveCount(7);
  const results = await targets.evaluateAll(elements => {
    function rgba(value: string) {
      const parts = value.match(/[\d.]+/g)?.map(Number) ?? [];
      return { rgb: parts.slice(0, 3), alpha: parts[3] ?? 1 };
    }
    function lightness(rgb: number[]) {
      const c = rgb.map(v => {
        const x = v / 255;
        return x <= 0.04045 ? x / 12.92 : Math.pow((x + 0.055) / 1.055, 2.4);
      });
      return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2];
    }
    return elements.map(el => {
      const fg = rgba(getComputedStyle(el).color).rgb;
      const layers: Array<{ rgb: number[]; alpha: number }> = [];
      let cursor: Element | null = el;
      while (cursor) {
        layers.push(rgba(getComputedStyle(cursor).backgroundColor));
        cursor = cursor.parentElement;
      }
      let bg = [255, 255, 255];
      for (const layer of layers.reverse()) {
        if (layer.rgb.length < 3) continue;
        bg = layer.rgb.map((v, i) => v * layer.alpha + bg[i] * (1 - layer.alpha));
      }
      const values = [lightness(fg), lightness(bg)].sort((a, b) => b - a);
      return { label: el.textContent?.trim().slice(0, 60), ratio: (values[0] + 0.05) / (values[1] + 0.05) };
    });
  });
  for (const item of results) {
    expect(item.ratio, 'WCAG AA 4.5:1 ' + item.label).toBeGreaterThanOrEqual(4.5);
  }
});
