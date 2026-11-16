import { expect, test, type Page } from '@playwright/test';

/** C14-04.M04.05: revisión Chromium con sesión SINTÉTICA, sin JWT real ni PROD. */
async function mockPlatform(page: Page, allow: boolean): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'readiness-a11y-synthetic');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers }); return;
    }
    await route.fulfill({
      status: allow ? 200 : 403, headers, contentType: 'application/json',
      body: JSON.stringify(allow
        ? { success: true, data: { nombreUsuario: 'Synthetic', rolNombre: 'Admin',
            ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'READINESS_INTERNAL_NEVER_EXPOSE' })
    });
  });
}

test('C14-04.M04.05 acceso anónimo y 403 mantienen panel oculto', async ({ page }) => {
  await page.goto('/plataforma/flujo-v5');
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByTestId('release-readiness-contract')).toHaveCount(0);
  await mockPlatform(page, false);
  await page.goto('/plataforma/flujo-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.getByTestId('release-readiness-contract')).toHaveCount(0);
  await expect(page.getByText('READINESS_INTERNAL_NEVER_EXPOSE')).toHaveCount(0);
});

test('C14-04.M04.05 320/375/768/1280 con cuatro puertas sin overflow horizontal', async ({ page }) => {
  await mockPlatform(page, true);
  for (const width of [320, 375, 768, 1280]) {
    await page.setViewportSize({ width, height: 800 });
    await page.goto('/plataforma/flujo-v5');
    const panel = page.getByTestId('release-readiness-contract');
    await expect(panel.getByRole('heading', { name: 'Preparación de salida · evidencia pendiente' })).toBeVisible();
    await expect(panel.locator('ol li')).toHaveCount(4);
    const overflow = await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, `desbordamiento a ${width}px`).toBeLessThanOrEqual(2);
  }
});

test('C14-04.M04.05 semántica, labels y cero falsos PASS o controles mutadores', async ({ page }) => {
  await mockPlatform(page, true);
  await page.goto('/plataforma/flujo-v5');
  const panel = page.getByTestId('release-readiness-contract');
  await expect(panel).toHaveAttribute('aria-labelledby', 'release-readiness-title');
  await expect(panel.getByRole('list', { name: 'Puertas de preparación no certificadas' })).toBeVisible();
  await expect(panel.locator('ol li')).toHaveCount(4);
  await expect(panel.getByText('NO_MEDIDO')).toHaveCount(1);
  await expect(panel.getByText('SIN_VERIFICAR')).toHaveCount(3);
  await expect(panel.locator('[role="note"]')).toContainText('Ninguna puerta autoriza QA');
  await expect(panel.locator('button, input, textarea, form, a, [tabindex]:not([tabindex="-1"])')).toHaveCount(0);
  await expect(panel).not.toContainText('PASS');
  await page.keyboard.press('Tab');
  expect(await panel.evaluate(el => el.contains(document.activeElement))).toBe(false);
});

test('C14-04.M04.05 contraste del texto de estado frente al fondo >= 4.5:1', async ({ page }) => {
  await mockPlatform(page, true);
  await page.goto('/plataforma/flujo-v5');
  const ratio = await page.getByTestId('release-readiness-contract').getByText('NO_MEDIDO').evaluate(el => {
    const rgb = (value: string): number[] => (value.match(/[\d.]+/g) ?? []).map(Number);
    const L = (values: number[]): number => {
      const [r, g, b] = values.slice(0, 3).map(v => {
        const c = v / 255;
        return c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
      });
      return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    };
    const fg = rgb(getComputedStyle(el).color);
    let bg = [255, 255, 255];
    for (let n: Element | null = el; n; n = n.parentElement) {
      const color = rgb(getComputedStyle(n).backgroundColor);
      if (color.length >= 3 && (color.length < 4 || color[3] >= 1)) {
        bg = color;
        break;
      }
    }
    const a = L(fg), b = L(bg);
    return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
  });
  expect(ratio).toBeGreaterThanOrEqual(4.5);
});
