import { expect, test, type Page } from '@playwright/test';

// C00-04.M04.06: real Chromium UI checks with a synthetic platform session.
// This never certifies backend JWT, tenant isolation, production SLO or MySQL.
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
const allowed = JSON.stringify({
  success: true,
  data: { nombreUsuario: 'Solo prueba visual', rolNombre: 'Plataforma',
    ambito: 'Plataforma', esAdministrador: true }
});
async function syntheticSession(page: Page): Promise<number[]> {
  const calls: number[] = [];
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'C0004-M0406-NOT-REAL-JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(sessionUrl, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors }); return;
    }
    calls.push(200);
    await route.fulfill({ status: 200, headers: cors, contentType: 'application/json', body: allowed });
  });
  return calls;
}

test.describe('C00-04.M04.06 - densidad y carga progresiva de gobierno V5', () => {
  test.describe.configure({ retries: 0 });

  test('anónimo permanece fuera de la superficie privada y no obtiene pasos', async ({ page }) => {
    await page.goto('/plataforma/gobierno-cambios-v5');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.locator('.steps li')).toHaveCount(0);
    await expect(page.getByRole('heading', { name: 'Decisiones y controles V5' })).toHaveCount(0);
  });

  test('320/390/768/1280: densidad 1 o 2 columnas sin desbordamiento ni pasos perdidos', async ({ page }) => {
    const calls = await syntheticSession(page);
    await page.goto('/plataforma/gobierno-cambios-v5');
    await expect(page.getByRole('heading', { name: 'Decisiones y controles V5' })).toBeVisible();
    await expect(page.locator('.steps li')).toHaveCount(4);
    await expect(page.getByText('Sin datos institucionales conectados', { exact: false }).first()).toBeVisible();
    for (const width of [320, 390, 768, 1280]) {
      await page.setViewportSize({ width, height: 820 });
      const metrics = await page.evaluate(() => {
        const grid = document.querySelector<HTMLElement>('.steps');
        if (!grid) throw new Error('grid missing');
        return {
          overflow: Math.max(document.body.scrollWidth, document.documentElement.scrollWidth) - window.innerWidth,
          columns: getComputedStyle(grid).gridTemplateColumns.split(/\s+/).filter(Boolean).length
        };
      });
      expect(metrics.overflow, 'horizontal overflow at width ' + width).toBeLessThanOrEqual(2);
      expect(metrics.columns, 'density at width ' + width).toBe(width <= 390 ? 1 : 2);
      for (const step of await page.locator('.steps li').all()) {
        const box = await step.boundingBox();
        expect(box).not.toBeNull();
        expect(box!.width).toBeGreaterThan(100);
        expect(box!.x + box!.width).toBeLessThanOrEqual(width + 2);
      }
    }
    expect(calls, 'resizing must not issue another platform session GET').toHaveLength(1);
  });

  test('revalidación progresiva mantiene una sola petición durante espera externa', async ({ page }) => {
    await syntheticSession(page);
    await page.goto('/plataforma/gobierno-cambios-v5');
    let release: (() => void) | undefined;
    const wait = new Promise<void>(resolve => { release = resolve; });
    let pendingGets = 0;
    await page.route(sessionUrl, async route => {
      if (route.request().method() === 'OPTIONS') {
        await route.fulfill({ status: 204, headers: cors }); return;
      }
      pendingGets++;
      await wait;
      await route.fulfill({ status: 200, headers: cors, contentType: 'application/json', body: allowed });
    });
    const refresh = page.getByRole('button', { name: 'Revalidar sesión de plataforma' });
    await refresh.click();
    await expect(refresh).toBeDisabled();
    await expect(page.locator('.state-banner [role="status"]')).toContainText('Verificando permisos');
    await expect.poll(() => pendingGets).toBe(1);
    expect(pendingGets).toBe(1);
    release?.();
    await expect(refresh).toBeEnabled();
    await expect(page.locator('.state-banner [role="status"]')).toContainText('revalidada correctamente');
    await expect(page.locator('.steps li')).toHaveCount(4);
    expect(pendingGets).toBe(1);
  });

  test('503 falla cerrado, no filtra respuesta ni reintenta sin una acción explícita', async ({ page }) => {
    await syntheticSession(page);
    await page.goto('/plataforma/gobierno-cambios-v5');
    let failureGets = 0;
    await page.route(sessionUrl, async route => {
      if (route.request().method() === 'OPTIONS') {
        await route.fulfill({ status: 204, headers: cors }); return;
      }
      failureGets++;
      await route.fulfill({ status: 503, headers: cors, contentType: 'application/json',
        body: JSON.stringify({ success: false, message: 'SECRET_PROVIDER_ERROR_DO_NOT_LEAK' }) });
    });
    const refresh = page.getByRole('button', { name: 'Revalidar sesión de plataforma' });
    await refresh.click();
    await expect(page.getByRole('alert')).toContainText('No fue posible verificar');
    await expect(page.locator('.steps li')).toHaveCount(0);
    await expect(page.getByText('SECRET_PROVIDER_ERROR_DO_NOT_LEAK')).toHaveCount(0);
    expect(failureGets).toBe(1);
    await page.route(sessionUrl, async route => {
      if (route.request().method() === 'OPTIONS') {
        await route.fulfill({ status: 204, headers: cors }); return;
      }
      await route.fulfill({ status: 200, headers: cors, contentType: 'application/json', body: allowed });
    });
    await refresh.click();
    await expect(page.locator('.state-banner [role="status"]')).toContainText('revalidada correctamente');
    await expect(page.locator('.steps li')).toHaveCount(4);
    expect(failureGets, 'a failed request must not loop').toBe(1);
  });

  test('presentación de solo lectura no envía mutaciones ni solicita PLAN/COLA/BITACORA', async ({ page }) => {
    const calls = await syntheticSession(page);
    const mutations: string[] = [];
    const forbidden: string[] = [];
    page.on('request', request => {
      if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) mutations.push(request.method());
    });
    await page.route(/\/(?:plan-maestro|bitacora|cola|v5\/claims)(?:\/|\?|$)/i, async route => {
      forbidden.push(route.request().url());
      await route.abort();
    });
    await page.goto('/plataforma/gobierno-cambios-v5');
    await expect(page.locator('.steps li')).toHaveCount(4);
    await expect(page.getByText('Solo lectura · sin autoridad operativa')).toBeVisible();
    await page.setViewportSize({ width: 375, height: 820 });
    await expect(page.locator('.steps li')).toHaveCount(4);
    expect(mutations).toEqual([]);
    expect(forbidden).toEqual([]);
    expect(calls).toHaveLength(1);
  });
});
