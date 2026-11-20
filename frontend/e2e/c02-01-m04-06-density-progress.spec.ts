import { expect, test } from '@playwright/test';

// C02-01.M04.06. Chromium real + Angular local efimero. HTTP de control simulado
// UNICAMENTE para UI/progreso/error; NO certifica firma JWT, RBAC ni MySQL.
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, POST, OPTIONS'
};

test.describe('V22 C02-01.M04.06 - densidad, progreso y ligereza de acceso', () => {
  test.describe.configure({ retries: 0 });

  test('320/375/768/1280: campos y acciones legibles sin desbordamiento ni carga privada', async ({ page }) => {
    for (const width of [320, 375, 768, 1280]) {
      await page.setViewportSize({ width, height: 850 });
      await page.goto('/login');
      await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
      const controls = [
        page.getByRole('textbox', { name: 'Nombre de usuario' }),
        page.getByLabel('Contraseña', { exact: true }),
        page.getByRole('button', { name: 'Mostrar contraseña' }),
        page.getByRole('button', { name: 'Ingresar al sistema', exact: true })
      ];
      for (const control of controls) {
        await expect(control).toBeVisible();
        const rect = await control.boundingBox();
        expect(rect, 'control visible a ' + width + 'px').not.toBeNull();
        expect(rect!.width).toBeGreaterThan(0);
        expect(rect!.x).toBeGreaterThanOrEqual(-2);
        expect(rect!.x + rect!.width).toBeLessThanOrEqual(width + 2);
      }
      const snapshot = await page.evaluate(() => {
        const navigation = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming | undefined;
        return {
          overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
          textLength: document.querySelector('form.login-card')?.textContent?.trim().length ?? 0,
          domReadyMs: navigation?.domContentLoadedEventEnd ?? -1,
          privateSurface: document.querySelectorAll('app-dashboard, app-perfil, app-tenant-selector').length
        };
      });
      expect(snapshot.overflow, 'sin overflow horizontal').toBeLessThanOrEqual(2);
      expect(snapshot.textLength, 'contenido de formulario no desmesurado').toBeGreaterThan(30);
      expect(snapshot.textLength, 'presupuesto orientativo de densidad de acceso').toBeLessThanOrEqual(2200);
      expect(snapshot.domReadyMs, 'métrica local medida, no SLA de producción').toBeGreaterThan(0);
      expect(snapshot.privateSurface, 'sin superficie privada antes de JWT').toBe(0);
    }
  });

  test('GET de disponibilidad retenido mantiene progreso, un intento y respuesta 503 opaca', async ({ page }) => {
    await page.goto('/login');
    await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill('test_only_no_real_user');
    await page.locator('input[formcontrolname="password"]').fill('not_a_real_password');
    let release!: () => void;
    const held = new Promise<void>(resolve => { release = resolve; });
    let healthGets = 0;
    let loginPosts = 0;
    await page.route(/\/health\/ready(?:\?|$)/, async route => {
      healthGets++;
      await held;
      await route.fulfill({ status: 200, headers: cors, contentType: 'application/json',
        body: JSON.stringify({ status: 'Ready' }) });
    });
    await page.route(/\/auth\/login(?:\?|$)/, async route => {
      if (route.request().method() === 'OPTIONS') {
        await route.fulfill({ status: 204, headers: cors });
        return;
      }
      loginPosts++;
      await route.fulfill({ status: 503, headers: cors, contentType: 'application/json',
        body: JSON.stringify({ message: 'PRIVATE_BACKEND_ERROR_NEVER_RENDER' }) });
    });
    const submit = page.locator('form.login-card button[type="submit"]');
    try {
      await submit.click();
      await expect.poll(() => healthGets).toBe(1);
      await expect(page.getByRole('status')).toContainText('Comprobando la disponibilidad');
      await expect(submit).toBeDisabled();
      await expect(submit.locator('mat-spinner')).toBeVisible();
      await page.locator('form.login-card').evaluate(form =>
        form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true })));
      expect(healthGets, 'doble submit no dispara segundo health').toBe(1);
      expect(loginPosts, 'ninguna contraseña antes del GET completado').toBe(0);
    } finally {
      release();
    }
    await expect.poll(() => loginPosts).toBe(1);
    await expect(page.getByRole('alert')).toContainText('El servidor está temporalmente no disponible');
    await expect(page.getByText('PRIVATE_BACKEND_ERROR_NEVER_RENDER')).toHaveCount(0);
    await expect(submit).toBeEnabled();
    await expect(page).toHaveURL(/\/login(?:[?#]|$)/);
    expect(await page.evaluate(() => localStorage.getItem('solqaryn_token'))).toBeNull();
  });

  test('empresa falsa en almacenamiento y acceso sin JWT no habilitan contexto privado', async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('solqaryn_empresa_solicitada_id', '999999');
      localStorage.removeItem('solqaryn_token');
    });
    await page.goto('/perfil');
    await expect(page).toHaveURL(/\/login(?:[?#]|$)/);
    await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
    await expect(page.locator('app-tenant-selector')).toHaveCount(0);
    await expect(page.getByRole('heading', { name: 'Mi perfil' })).toHaveCount(0);
  });
});
