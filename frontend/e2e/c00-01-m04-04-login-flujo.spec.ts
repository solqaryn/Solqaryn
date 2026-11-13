import { test, expect, Page } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';
const EMPRESA_ID = process.env['PHASE7_EMPRESA_ID'];

async function completarTenantSiAplica(page: Page): Promise<void> {
  await expect.poll(async () => {
    if (new URL(page.url()).pathname !== '/login') return true;
    return (await page.getByLabel('ID de empresa').count()) > 0;
  }, { timeout: 20_000 }).toBe(true);

  if (new URL(page.url()).pathname !== '/login') return;
  if (!EMPRESA_ID) throw new Error('El runner E2E no configuró PHASE7_EMPRESA_ID para la selección server-side.');
  await page.getByLabel('ID de empresa').fill(EMPRESA_ID);
  await page.getByRole('button', { name: 'Entrar a la empresa', exact: true }).click();
  await page.waitForURL(url => url.pathname !== '/login', { timeout: 20_000 });
}

test.describe('V5 C00-01.M04.04: formulario, navegación, errores y doble acción', () => {
  test.describe.configure({ mode: 'serial', retries: 0 });

  test('deniega navegación privada anónima y no envía formulario incompleto', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login(?:$|[?#])/);
    await expect(page.locator('aside[aria-label="Menú principal"]')).toHaveCount(0);

    const enviar = page.getByRole('button', { name: 'Ingresar al sistema', exact: true });
    await expect(enviar).toBeDisabled();
    await page.locator('input[formcontrolname="nombreUsuario"]').fill('usuario');
    await expect(enviar).toBeDisabled();
    await page.locator('input[formcontrolname="password"]').fill('clave');
    await expect(enviar).toBeEnabled();
  });

  test('credenciales inválidas muestran error accesible sin detalles internos', async ({ page }) => {
    await page.goto('/login');
    await page.locator('input[formcontrolname="nombreUsuario"]').fill('e2e_usuario_inexistente_v5');
    await page.locator('input[formcontrolname="password"]').fill('Clave.Invalida#2026!');
    const respuesta = page.waitForResponse(response =>
      /\/auth\/login(?:[?#]|$)/.test(response.url()) && response.request().method() === 'POST'
    );
    const enviar = page.getByRole('button', { name: 'Ingresar al sistema', exact: true });
    await enviar.click();
    const http = await respuesta;
    expect(http.status()).toBeGreaterThanOrEqual(400);
    await expect(page.getByRole('alert')).toBeVisible();
    const mensaje = await page.getByRole('alert').innerText();
    expect(mensaje).not.toMatch(/stack trace|connectionstrings|sql exception|system\.[a-z]+exception|jwt secret/i);
    await expect(enviar).toBeEnabled();
    await expect(page).toHaveURL(/\/login(?:$|[?#])/);
  });

  test('submit concurrente emite sólo un POST real y navega tras autenticar tenant', async ({ page }) => {
    let solicitudes = 0;
    let liberar: (() => void) | undefined;
    await page.route('**/auth/login', async route => {
      solicitudes += 1;
      if (solicitudes === 1) {
        await new Promise<void>(resolve => { liberar = resolve; });
      }
      await route.continue();
    });

    await page.goto('/login');
    const usuario = page.locator('input[formcontrolname="nombreUsuario"]');
    const clave = page.locator('input[formcontrolname="password"]');
    const enviar = page.getByRole('button', { name: 'Ingresar al sistema', exact: true });
    await usuario.fill(ADMIN_USERNAME);
    await clave.fill(ADMIN_PASSWORD);

    try {
      await enviar.click();
      await expect.poll(() => solicitudes, { timeout: 20_000 }).toBe(1);
      await expect(enviar).toBeDisabled();
      await clave.press('Enter');
      await page.waitForTimeout(150);
      expect(solicitudes).toBe(1);
    } finally {
      liberar?.();
    }

    await completarTenantSiAplica(page);
    expect(solicitudes).toBe(1);
    await expect(page).not.toHaveURL(/\/login(?:$|[?#])/);
  });
});
