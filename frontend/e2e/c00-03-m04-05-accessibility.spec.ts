import { test, expect, type Page } from '@playwright/test';

// C00-03.M04.05: browser real para accesibilidad de la superficie de cobertura.
// /plataforma/sesion se simula exclusivamente en UI. No certifica JWT, RBAC,
// datos empresariales ni autorizacion de backend/MySQL en ambiente real.
const sessionUrl = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

async function tokenDePrueba(page: Page): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c00-03-browser-not-a-real-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
}

async function simularSesion(page: Page, permitir: boolean): Promise<number[]> {
  const responses: number[] = [];
  await page.route(sessionUrl, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    const status = permitir ? 200 : 403;
    responses.push(status);
    await route.fulfill({
      status,
      headers: cors,
      contentType: 'application/json',
      body: JSON.stringify(permitir
        ? { success: true, data: {
            nombreUsuario: 'Solo test visual', rolNombre: 'Plataforma UI simulada',
            ambito: 'Plataforma', esAdministrador: true
          } }
        : { success: false, message: 'DENEGADO', data: null })
    });
  });
  return responses;
}

test.describe('C00-03.M04.05 - accesibilidad de cobertura V5 con Chromium real', () => {
  test.describe.configure({ retries: 0 });

  test('sin token la ruta privada no revela tabla ni identidad', async ({ page }) => {
    const respuestas = await simularSesion(page, true);
    await page.goto('/plataforma/cobertura-v5');
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    await expect(page.getByRole('heading', { name: 'Cadena de trazabilidad' })).toHaveCount(0);
    expect(respuestas).toEqual([]);
  });

  test('con 403 de sesion simulada se deniega y no renderiza cobertura', async ({ page }) => {
    await tokenDePrueba(page);
    const respuestas = await simularSesion(page, false);
    await page.goto('/plataforma/cobertura-v5');
    await expect(page).toHaveURL(/\/login\?plataforma=denegado(?:&|$)/);
    await expect(page.getByRole('heading', { name: 'Cadena de trazabilidad' })).toHaveCount(0);
    expect(respuestas).toContain(403);
  });

  test('movil y escritorio: sin overflow exterior, tabla accesible y desplazamiento por teclado', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await tokenDePrueba(page);
    const respuestas = await simularSesion(page, true);
    await page.goto('/plataforma/cobertura-v5');
    await expect(page.getByRole('heading', { name: 'Cadena de trazabilidad' })).toBeVisible();
    const region = page.getByRole('region', { name: 'Columnas del contrato de cobertura' });
    await expect(region).toBeVisible();
    await expect(page.getByRole('columnheader')).toHaveCount(5);
    await expect(page.getByText('Sin datos de cobertura verificados', { exact: false })).toBeVisible();
    for (const width of [390, 1280]) {
      await page.setViewportSize({ width, height: 844 });
      const overflow = await page.evaluate(() =>
        Math.max(document.body.scrollWidth, document.documentElement.scrollWidth) - window.innerWidth);
      expect(overflow).toBeLessThanOrEqual(1);
    }
    await page.setViewportSize({ width: 390, height: 844 });
    const dimensions = await region.evaluate(element => ({
      client: element.clientWidth, content: element.scrollWidth,
      tabIndex: element.getAttribute('tabindex')
    }));
    expect(dimensions.tabIndex).toBe('0');
    expect(dimensions.content).toBeGreaterThan(dimensions.client);
    await region.focus();
    await expect(region).toBeFocused();
    await page.keyboard.press('ArrowRight');
    await expect.poll(() => region.evaluate(el => el.scrollLeft)).toBeGreaterThan(0);
    expect(respuestas).toContain(200);
  });

  test('labels, foco y contraste: controles identificables sin permisos operativos', async ({ page }) => {
    await tokenDePrueba(page);
    await simularSesion(page, true);
    await page.goto('/plataforma/cobertura-v5');
    await expect(page.getByRole('columnheader', { name: 'Requisito' })).toBeVisible();
    await expect(page.getByRole('columnheader', { name: 'Evidencia' })).toBeVisible();
    await expect(page.getByRole('link', { name: /Administración/ })).toBeVisible();
    const refresh = page.getByRole('button', { name: /Revalidar sesión/ });
    await refresh.focus();
    await expect(refresh).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByRole('link', { name: /Administración/ })).toBeFocused();
    const ratio = await page.locator('.v5-card h2').evaluate(element => {
      const rgb = (value: string): number[] => (value.match(/[\d.]+/g) ?? []).slice(0, 3).map(Number);
      const luminance = (channels: number[]): number => {
        const linear = channels.map(value => {
          const normalized = value / 255;
          return normalized <= 0.04045
            ? normalized / 12.92
            : Math.pow((normalized + 0.055) / 1.055, 2.4);
        });
        return 0.2126 * linear[0] + 0.7152 * linear[1] + 0.0722 * linear[2];
      };
      const fg = rgb(getComputedStyle(element).color);
      const bg = rgb(getComputedStyle(element.closest('mat-card') ?? document.body).backgroundColor);
      if (fg.length !== 3 || bg.length !== 3) return 0;
      const a = luminance(fg), b = luminance(bg);
      return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
    });
    expect(ratio).toBeGreaterThanOrEqual(4.5);
  });
  test('C00-03.M05.04 - aceptacion positiva de cobertura sin escritura ni certificacion ficticia', async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 800 });
    await tokenDePrueba(page);
    const respuestas = await simularSesion(page, true);
    const forbidden: string[] = [];
    await page.route(/\/(?:v5\/claims|plan-maestro|bitacora|cola)(?:\/|\?|$)/i, route => {
      forbidden.push(route.request().url());
      return route.abort();
    });

    await page.goto('/plataforma/cobertura-v5');
    await expect(page.getByRole('heading', { name: 'Cadena de trazabilidad' })).toBeVisible();
    await expect(page.getByRole('table')).toHaveCount(1);
    await expect(page.getByRole('columnheader')).toHaveCount(5);
    await expect(page.getByText('Sin datos de cobertura verificados', { exact: false })).toBeVisible();
    await expect(page.getByRole('status').filter({ hasText: 'Permiso de consulta confirmado' })).toBeVisible();

    const beforeRefresh = respuestas.length;
    const refresh = page.getByRole('button', { name: /Revalidar sesión/ });
    await expect(refresh).toBeEnabled();
    await refresh.click();
    await expect.poll(() => respuestas.length).toBeGreaterThan(beforeRefresh);
    await expect(page.getByRole('status').filter({ hasText: 'Permiso de consulta confirmado' })).toBeVisible();
    await expect(page.getByRole('table')).toHaveCount(1);
    await expect(page.getByText('Sin datos de cobertura verificados', { exact: false })).toBeVisible();
    expect(forbidden).toEqual([]);
    expect(respuestas.every(status => status === 200)).toBe(true);
  });

});
