import { test, expect, type Page } from '@playwright/test';

// UI/browser regresion solo contra backend sintetico. La autorizacion real
// sigue bajo /plataforma/sesion en el servidor; esto NO prueba JWT/MySQL real.
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

async function syntheticToken(page: Page): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'v5-browser-ui-test-not-a-real-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
}

async function mockPlatformSession(page: Page, allowed: boolean): Promise<number[]> {
  const observed: number[] = [];
  await page.route(sessionUrl, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    observed.push(allowed ? 200 : 403);
    await route.fulfill({
      status: allowed ? 200 : 403,
      contentType: 'application/json',
      headers: cors,
      body: JSON.stringify(allowed
        ? { success: true, data: {
            nombreUsuario: 'V5 UI synthetic',
            rolNombre: 'Plataforma UI mock',
            ambito: 'Plataforma',
            esAdministrador: true
          } }
        : { success: false, message: 'DENEGADO', data: null })
    });
  });
  return observed;
}

test.describe('V5 C00-02/03 M04.01 - rutas de plataforma en Chromium real', () => {
  test.describe.configure({ retries: 0 });

  test('sin JWT bloquea ambas rutas sin consultar sesion ni dibujar contenido privado', async ({ page }) => {
    const observed = await mockPlatformSession(page, true);
    await page.goto('/plataforma/flujo-v5');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.getByRole('heading', { name: 'De la solicitud a la evidencia DEV' })).toHaveCount(0);
    await page.goto('/plataforma/cobertura-v5');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.getByRole('heading', { name: 'Cadena de trazabilidad' })).toHaveCount(0);
    expect(observed).toEqual([]);
  });

  test('token sintetico mas 403 del servidor bloquea el control plane', async ({ page }) => {
    await syntheticToken(page);
    const observed = await mockPlatformSession(page, false);
    await page.goto('/plataforma/cobertura-v5');
    await expect(page).toHaveURL(/\/login\?plataforma=denegado(?:&|$)/);
    await expect(page.getByRole('heading', { name: 'Cadena de trazabilidad' })).toHaveCount(0);
    expect(observed).toContain(403);
  });

  test('sesion global simulada autorizada muestra flujo estatico sin despacho', async ({ page }) => {
    await syntheticToken(page);
    const observed = await mockPlatformSession(page, true);
    await page.goto('/plataforma/flujo-v5');
    await expect(page.getByRole('heading', { name: 'De la solicitud a la evidencia DEV' })).toBeVisible();
    await expect(page.getByRole('list', { name: 'Secuencia contractual de estados' }).locator('li')).toHaveCount(4);
    await expect(page.getByText('Sin operaciones')).toBeVisible();
    await expect(page.getByText('No consulta PLAN_MAESTRO', { exact: false })).toBeVisible();
    expect(observed).toContain(200);
  });

  test('navegacion responsive a cobertura muestra matriz vacia sin endpoints operativos', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await syntheticToken(page);
    const observed = await mockPlatformSession(page, true);
    const unexpectedCalls: string[] = [];
    await page.route(/\/(?:bitacora|cola|plan-maestro|v5\/claims)(?:\/|\?|$)/i, route => {
      unexpectedCalls.push(route.request().url());
      return route.abort();
    });
    await page.goto('/plataforma/flujo-v5');
    await page.getByRole('link', { name: /Administracion|Administración/ }).click();
    await expect(page).toHaveURL(/\/plataforma$/);
    await page.getByRole('link', { name: /Cobertura V5/ }).click();
    await expect(page).toHaveURL(/\/plataforma\/cobertura-v5$/);
    await expect(page.getByRole('heading', { name: 'Cadena de trazabilidad' })).toBeVisible();
    await expect(page.getByRole('columnheader')).toHaveCount(5);
    await expect(page.getByText('Sin datos de cobertura verificados', { exact: false })).toBeVisible();
    await expect(page.getByText('Sin escritura')).toBeVisible();
    expect(unexpectedCalls).toEqual([]);
    expect(observed.length).toBeGreaterThanOrEqual(2);
  });
});

test.describe('C00-02.M04.05 - responsive, teclado, foco y contraste V5', () => {
  test.describe.configure({ retries: 0 });

  test('flujo de solo lectura conserva cuatro pasos sin overflow y admite Tab en movil y escritorio', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await syntheticToken(page);
    const responses = await mockPlatformSession(page, true);
    await page.goto('/plataforma/flujo-v5');
    await expect(page.getByRole('heading', { name: 'De la solicitud a la evidencia DEV' })).toBeVisible();
    const steps = page.getByRole('list', { name: 'Secuencia contractual de estados' }).locator('li');
    await expect(steps).toHaveCount(4);
    for (const width of [390, 1280]) {
      await page.setViewportSize({ width, height: 844 });
      const overflow = await page.evaluate(() =>
        Math.max(document.body.scrollWidth, document.documentElement.scrollWidth) - window.innerWidth);
      expect(overflow).toBeLessThanOrEqual(1);
      for (const step of await steps.all()) {
        const rect = await step.boundingBox();
        expect(rect).not.toBeNull();
        expect(rect!.width).toBeGreaterThan(120);
        expect(rect!.x + rect!.width).toBeLessThanOrEqual(width + 1);
      }
    }
    await page.setViewportSize({ width: 390, height: 844 });
    const refresh = page.getByRole('button', { name: /Revalidar sesión/ });
    const administration = page.getByRole('link', { name: /Administración/ });
    await refresh.focus();
    await expect(refresh).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(administration).toBeFocused();
    expect(responses).toContain(200);
  });

  test('cobertura mantiene tabla semantica desplazable mediante teclado sin exponer datos', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await syntheticToken(page);
    await mockPlatformSession(page, true);
    await page.goto('/plataforma/cobertura-v5');
    const region = page.getByRole('region', { name: 'Columnas del contrato de cobertura' });
    await expect(region).toBeVisible();
    await expect(page.getByRole('columnheader')).toHaveCount(5);
    const dimensions = await region.evaluate(element => ({
      client: element.clientWidth, content: element.scrollWidth,
      tabindex: element.getAttribute('tabindex')
    }));
    expect(dimensions.tabindex).toBe('0');
    expect(dimensions.content).toBeGreaterThan(dimensions.client);
    await region.focus();
    await expect(region).toBeFocused();
    await page.keyboard.press('ArrowRight');
    await expect.poll(() => region.evaluate(element => element.scrollLeft)).toBeGreaterThan(0);
    await expect(page.getByText('Sin datos de cobertura verificados', { exact: false })).toBeVisible();
  });

  test('titulo de la superficie de contrato cumple contraste de texto normal en tema disponible', async ({ page }) => {
    await syntheticToken(page);
    await mockPlatformSession(page, true);
    await page.goto('/plataforma/flujo-v5');
    const ratio = await page.locator('.v5-card h2').evaluate(element => {
      const rgb = (value: string): number[] => (value.match(/[\d.]+/g) ?? []).slice(0, 3).map(Number);
      const luminosidad = (channels: number[]): number => {
        const linear = channels.map(channel => {
          const s = channel / 255;
          return s <= 0.04045 ? s / 12.92 : Math.pow((s + 0.055) / 1.055, 2.4);
        });
        return 0.2126 * linear[0] + 0.7152 * linear[1] + 0.0722 * linear[2];
      };
      const foreground = rgb(getComputedStyle(element).color);
      const background = rgb(getComputedStyle(element.closest('mat-card') ?? document.body).backgroundColor);
      if (foreground.length !== 3 || background.length !== 3) return 0;
      const a = luminosidad(foreground), b = luminosidad(background);
      return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
    });
    expect(ratio).toBeGreaterThanOrEqual(4.5);
  });
});

test.describe('C00-02.M04.06 - densidad y carga progresiva segura V5', () => {
  test.describe.configure({ retries: 0 });

  test('presenta solo cuatro pasos y una tarjeta, sin cargar directorio ni operaciones', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await syntheticToken(page);
    await mockPlatformSession(page, true);
    const unwanted: string[] = [];
    await page.route(/\/(?:plan-maestro|bitacora|cola|directorio|empresas\/listar)(?:\/|\?|$)/i, route => {
      unwanted.push(route.request().url());
      return route.abort();
    });
    await page.goto('/plataforma/flujo-v5');
    await expect(page.getByRole('heading', { name: 'De la solicitud a la evidencia DEV' })).toBeVisible();
    await expect(page.locator('.v5-card')).toHaveCount(1);
    await expect(page.getByRole('list', { name: 'Secuencia contractual de estados' }).locator('li')).toHaveCount(4);
    await expect(page.locator('app-plataforma-gestion-mvp')).toHaveCount(0);
    await expect(page.locator('.directory-card')).toHaveCount(0);
    expect(unwanted).toEqual([]);
  });

  test('muestra espera progresiva y bloquea doble revalidacion sin exponer respuesta privada', async ({ page }) => {
    await syntheticToken(page);
    await mockPlatformSession(page, true);
    await page.goto('/plataforma/flujo-v5');
    const button = page.getByRole('button', { name: /Revalidar sesión/ });
    await expect(button).toBeEnabled();
    let release: (() => void) | undefined;
    const requests: string[] = [];
    await page.route(sessionUrl, async route => {
      if (route.request().method() === 'OPTIONS') {
        await route.fulfill({ status: 204, headers: cors });
        return;
      }
      requests.push(route.request().method());
      await new Promise<void>(resolve => { release = resolve; });
      await route.fulfill({
        status: 200, contentType: 'application/json', headers: cors,
        body: JSON.stringify({ success: true, data: {
          nombreUsuario: 'V5 UI synthetic', rolNombre: 'Plataforma UI mock',
          ambito: 'Plataforma', esAdministrador: true
        } })
      });
    });
    await button.click();
    await expect(button).toBeDisabled();
    await expect(page.getByRole('status').filter({ hasText: 'Verificando permiso de plataforma' })).toBeVisible();
    await expect.poll(() => Boolean(release)).toBe(true);
    expect(requests).toEqual(['GET']);
    release?.();
    await expect(button).toBeEnabled();
    await expect(page.getByText('Sesión de plataforma verificada para consulta de solo lectura')).toBeVisible();
    expect(requests).toEqual(['GET']);
  });
});

test.describe('C00-03.M04.04 - navegacion, mensajes y accion unica de cobertura V5', () => {
  test.describe.configure({ retries: 0 });

  test('navega desde cobertura a administracion y de regreso sin formularios ni escritura', async ({ page }) => {
    await syntheticToken(page);
    await mockPlatformSession(page, true);
    await page.goto('/plataforma/cobertura-v5');
    await expect(page.getByRole('heading', { name: 'Cadena de trazabilidad' })).toBeVisible();
    await expect(page.locator('form, input, textarea, select')).toHaveCount(0);
    await expect(page.getByRole('columnheader')).toHaveCount(5);
    await page.getByRole('link', { name: /Administración/ }).click();
    await expect(page).toHaveURL(/\/plataforma$/);
    await page.getByRole('link', { name: 'Cobertura V5' }).click();
    await expect(page).toHaveURL(/\/plataforma\/cobertura-v5$/);
    await expect(page.getByText('Sin datos de cobertura verificados', { exact: false })).toBeVisible();
  });

  test('revalidacion 403 es segura, single-flight y reintento autorizado expreso', async ({ page }) => {
    await syntheticToken(page);
    await mockPlatformSession(page, true);
    await page.goto('/plataforma/cobertura-v5');
    const revalidar = page.getByRole('button', { name: /Revalidar sesión/ });
    let liberar: (() => void) | undefined;
    let requests = 0;
    await page.route(sessionUrl, async route => {
      if (route.request().method() === 'OPTIONS') {
        await route.fulfill({ status: 204, headers: cors });
        return;
      }
      requests += 1;
      if (requests === 1) await new Promise<void>(resolve => { liberar = resolve; });
      await route.fulfill({
        status: requests === 1 ? 403 : 200,
        contentType: 'application/json', headers: cors,
        body: JSON.stringify(requests === 1
          ? { success: false, message: 'NO_EXHIBIR_CLAVE_PRIVADA', data: null }
          : { success: true, data: {
              nombreUsuario: 'V5 UI synthetic', rolNombre: 'Plataforma UI mock',
              ambito: 'Plataforma', esAdministrador: true
            } })
      });
    });
    await revalidar.click();
    await expect(revalidar).toBeDisabled();
    await expect.poll(() => Boolean(liberar)).toBe(true);
    expect(requests).toBe(1);
    liberar?.();
    await expect(revalidar).toBeEnabled();
    await expect(page.getByRole('alert')).toContainText('La sesión global dejó de estar autorizada');
    await expect(page.locator('body')).not.toContainText('NO_EXHIBIR_CLAVE_PRIVADA');
    expect(requests).toBe(1);
    await revalidar.click();
    await expect(page.getByRole('heading', { name: 'Cadena de trazabilidad' })).toBeVisible();
    await expect(page.getByRole('columnheader')).toHaveCount(5);
    expect(requests).toBe(2);
  });
});


test.describe('C00-03.M04.02 - estados seguros de cobertura V5', () => {
  test.describe.configure({ retries: 0 });

  test('muestra confirmacion de consulta y vacio honesto sin filas fabricadas', async ({ page }) => {
    await syntheticToken(page);
    const observed = await mockPlatformSession(page, true);
    await page.goto('/plataforma/cobertura-v5');
    await expect(page.getByRole('status').filter({ hasText: 'Permiso de consulta confirmado' })).toBeVisible();
    await expect(page.getByText('Sin datos de cobertura verificados', { exact: false })).toBeVisible();
    await expect(page.locator('.v5-coverage tbody tr')).toHaveCount(1);
    await expect(page.locator('.v5-coverage tbody td')).toHaveCount(1);
    await expect(page.getByText('Sin escritura')).toBeVisible();
    expect(observed).toContain(200);
  });

  test('revalidacion anuncia carga y deniega sin filtrar errores del servidor', async ({ page }) => {
    await syntheticToken(page);
    await mockPlatformSession(page, true);
    await page.goto('/plataforma/cobertura-v5');
    const button = page.getByRole('button', { name: /Revalidar sesión/ });
    let release: (() => void) | undefined;
    let requests = 0;
    await page.route(sessionUrl, async route => {
      if (route.request().method() === 'OPTIONS') {
        await route.fulfill({ status: 204, headers: cors });
        return;
      }
      requests++;
      await new Promise<void>(resolve => { release = resolve; });
      await route.fulfill({
        status: 403,
        contentType: 'application/json',
        headers: cors,
        body: JSON.stringify({ success: false, message: 'NO_MOSTRAR_SECRETO_DEL_BACKEND', data: null })
      });
    });
    await button.click();
    await expect(button).toBeDisabled();
    await expect(page.getByRole('status').filter({ hasText: 'Comprobando permiso de cobertura' })).toBeVisible();
    await expect.poll(() => Boolean(release)).toBe(true);
    expect(requests).toBe(1);
    release?.();
    await expect(button).toBeEnabled();
    await expect(page.getByRole('alert').filter({ hasText: 'La sesión global dejó de estar autorizada' })).toBeVisible();
    await expect(page.locator('body')).not.toContainText('NO_MOSTRAR_SECRETO_DEL_BACKEND');
    await expect(page.getByText('Identidad de plataforma verificada')).toHaveCount(0);
  });
});


test.describe('C00-03.M04.03 - identidad y contraste permitido de cobertura', () => {
  test.describe.configure({ retries: 0 });

  test('branding oficial mantiene tabla vacia y alterna modo de alto contraste solo en cobertura', async ({ page }) => {
    await syntheticToken(page);
    await mockPlatformSession(page, true);
    await page.goto('/plataforma/cobertura-v5');
    const button = page.getByRole('button', { name: 'Alto contraste de cobertura' });
    const card = page.locator('.v5-card');
    await expect(page.getByText('Identidad SOLQARYN')).toBeVisible();
    await expect(button).toHaveAttribute('aria-pressed', 'false');
    await expect(card).not.toHaveClass(/v5-coverage-contrast/);
    await button.click();
    await expect(button).toHaveAttribute('aria-pressed', 'true');
    await expect(card).toHaveClass(/v5-coverage-contrast/);
    await expect(card).toHaveCSS('background-color', 'rgb(11, 18, 32)');
    await expect(page.getByRole('columnheader')).toHaveCount(5);
    await expect(page.getByText('Sin datos de cobertura verificados', { exact: false })).toBeVisible();
    await button.click();
    await expect(button).toHaveAttribute('aria-pressed', 'false');
    await expect(card).not.toHaveClass(/v5-coverage-contrast/);
  });

  test('query no confiable no concede temas ni inyecta componentes en el DOM', async ({ page }) => {
    await syntheticToken(page);
    await mockPlatformSession(page, true);
    await page.goto('/plataforma/cobertura-v5?tema=javascript%3Aalert%281%29&contraste=true');
    await expect(page.getByRole('button', { name: 'Alto contraste de cobertura' })).toHaveAttribute('aria-pressed', 'false');
    await expect(page.locator('.v5-card')).not.toHaveClass(/v5-coverage-contrast/);
    await expect(page.getByText('Sin datos de cobertura verificados', { exact: false })).toBeVisible();
    await expect(page.locator('.v5-coverage tbody tr')).toHaveCount(1);
    await expect(page.locator('form, input, textarea, select')).toHaveCount(0);
  });
});
