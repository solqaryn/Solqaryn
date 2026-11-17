import { expect, test, type Page } from '@playwright/test';

// C15-04.M04.06: navegador Chromium real, autoridad de sesion simulada.
// NO prueba JWT firmado, servidor multitenant, WCAG total ni Core Web Vitals productivos.
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
const permitted = { success: true, data: {
  nombreUsuario: 'Persona simulada', rolNombre: 'Plataforma',
  ambito: 'Plataforma', esAdministrador: true
} };

async function prepararSesion(page: Page, status = 200) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1504m0406-sintetico-no-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(sessionUrl, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors }); return;
    }
    await route.fulfill({ status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200 ? permitted : {
        success: false, message: 'PRIVATE_PROVIDER_DSN_NEVER_RENDER'
      })
    });
  });
}

test.describe('C15-04.M04.06 densidad, carga progresiva y ligereza', () => {
  test.describe.configure({ retries: 0 });
  test('anonimo y fallo 503 no presentan datos ni controles de auditoria', async ({ page }) => {
    const writes: string[] = [];
    page.on('request', r => {
      if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method());
    });
    await page.goto('/plataforma/auditoria-ux-v5');
    await expect(page).toHaveURL(/\/login/);
    await expect(page.locator('[data-testid="ux-surface"]')).toHaveCount(0);
    await prepararSesion(page, 503);
    await page.goto('/plataforma/auditoria-ux-v5');
    await expect(page).toHaveURL(/\/login/);
    await expect(page.locator('[data-testid="ux-surface"]')).toHaveCount(0);
    await expect(page.getByText('PRIVATE_PROVIDER_DSN_NEVER_RENDER')).toHaveCount(0);
    expect(writes).toEqual([]);
  });

  test('presupuesto de densidad y overflow a 320, 375, 768 y 1280 px', async ({ page }) => {
    await prepararSesion(page);
    const writes: string[] = [];
    page.on('request', r => {
      if (!['GET', 'HEAD', 'OPTIONS'].includes(r.method())) writes.push(r.method());
    });
    for (const width of [320, 375, 768, 1280]) {
      await page.setViewportSize({ width, height: 812 });
      await page.goto('/plataforma/auditoria-ux-v5');
      const view = page.locator('[data-testid="ux-audit-v19"]');
      await expect(view).toBeVisible();
      const cards = view.locator('[data-testid="ux-surface"]');
      await expect(cards).toHaveCount(5);
      await expect(view.locator('[data-state="NO_MEDIDO"]')).toHaveCount(5);
      const metrics = await view.evaluate(el => ({
        text: el.textContent?.trim().length ?? 0,
        overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
        inputs: el.querySelectorAll('form,input,button,select,textarea').length
      }));
      expect(metrics.text, 'empty audit on ' + width).toBeGreaterThan(100);
      expect(metrics.text, 'density text budget on ' + width).toBeLessThanOrEqual(2500);
      expect(metrics.overflow, 'horizontal overflow on ' + width).toBeLessThanOrEqual(2);
      expect(metrics.inputs, 'readonly audit on ' + width).toBe(0);
    }
    expect(writes).toEqual([]);
  });

  test('carga progresiva: antes del GET autorizado no renderiza superficie privada', async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('solqaryn_token', 'c1504m0406-sintetico-no-jwt');
      localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    });
    let release!: () => void;
    const pending = new Promise<void>(resolve => { release = resolve; });
    let requests = 0;
    await page.route(sessionUrl, async route => {
      if (route.request().method() === 'OPTIONS') {
        await route.fulfill({ status: 204, headers: cors }); return;
      }
      requests++;
      await pending;
      await route.fulfill({ status: 200, headers: cors,
        contentType: 'application/json', body: JSON.stringify(permitted) });
    });
    try {
      await page.goto('/plataforma/auditoria-ux-v5');
      await expect.poll(() => requests).toBe(1);
      await expect(page.locator('[data-testid="ux-audit-v19"]')).toHaveCount(0);
    } finally { release(); }
    await expect(page.locator('[data-testid="ux-surface"]')).toHaveCount(5);
    expect(requests).toBe(1);
  });
});
