import { expect, test, type Page } from '@playwright/test';

/** C15-05.M04.06 — Chromium real con API de sesión simulada. No acredita JWT/DB/PROD. */
type EstadoHTTP = 200 | 403 | 503;
const rutaSesion = /\/plataforma\/sesion(?:\?|$)/;

async function simularSesion(
  page: Page,
  responder: (numero: number) => EstadoHTTP,
  retener?: (numero: number) => Promise<void>
): Promise<() => number> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c15-m0406-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  let llamadas = 0;
  await page.route(rutaSesion, async route => {
    const headers = {
      'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods': 'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers });
      return;
    }
    llamadas += 1;
    if (retener) await retener(llamadas);
    const status = responder(llamadas);
    await route.fulfill({
      status, headers, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: { nombreUsuario: 'Prueba', rolNombre: 'Plataforma',
            ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, data: null, message: 'PRIVATE_C15_M0406_NEVER_EXPOSE' })
    });
  });
  return () => llamadas;
}

test('C15-05.M04.06: densidad acotada 320/390/768/1280, sin falso PASS ni acciones de aprobación', async ({ page }) => {
  const llamadas = await simularSesion(page, () => 200);
  await page.emulateMedia({ reducedMotion: 'reduce' });
  for (const width of [320, 390, 768, 1280]) {
    await page.setViewportSize({ width, height: 840 });
    await page.goto('/plataforma/auditoria-operativa-v5');
    const principal = page.locator('main.audit-view');
    await expect(principal.getByRole('heading', { name: 'Controles de ejecución y evidencia' })).toBeVisible();
    await expect(principal.locator('ol.controls > li')).toHaveCount(4);
    await expect(principal.locator('form, input, textarea')).toHaveCount(0);
    await expect(principal.getByRole('button')).toHaveCount(1);
    await expect(principal).toContainText('0 métricas verificadas');
    await expect(principal).not.toContainText('A5_PASS');
    const overflow = await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(overflow, 'overflow horizontal ' + width).toBeLessThanOrEqual(2);
  }
  expect(llamadas()).toBeGreaterThanOrEqual(4);
});

test('C15-05.M04.06: espera progresiva accesible, UI privada oculta y rechazo de doble acción', async ({ page }) => {
  let liberar!: () => void;
  const retenida = new Promise<void>(resolve => { liberar = resolve; });
  const llamadas = await simularSesion(page, () => 200, async n => {
    if (n === 2) await retenida;
  });
  await page.goto('/plataforma/auditoria-operativa-v5');
  const boton = page.getByRole('button', { name: 'Revalidar sesión de auditoría' });
  await expect(page.locator('ol.controls > li')).toHaveCount(4);
  try {
    await boton.click();
    await expect.poll(() => llamadas()).toBe(2);
    await expect(boton).toBeDisabled();
    await expect(page.getByRole('status')).toContainText('Verificando permisos con el servidor');
    await expect(page.locator('ol.controls > li')).toHaveCount(0);
    expect(llamadas()).toBe(2);
  } finally {
    liberar();
  }
  await expect(page.getByRole('status')).toContainText('Sesión de plataforma revalidada');
  await expect(page.locator('ol.controls > li')).toHaveCount(4);
  await expect(boton).toBeEnabled();
});

test('C15-05.M04.06: error 503 limpia carga, oculta privados y permite reintento', async ({ page }) => {
  const llamadas = await simularSesion(page, n => n === 2 ? 503 : 200);
  await page.goto('/plataforma/auditoria-operativa-v5');
  const boton = page.getByRole('button', { name: 'Revalidar sesión de auditoría' });
  await boton.click();
  await expect(page.getByRole('alert')).toContainText('No fue posible verificar la sesión');
  await expect(page.locator('ol.controls > li')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_C15_M0406_NEVER_EXPOSE')).toHaveCount(0);
  await expect(boton).toBeEnabled();
  await boton.click();
  await expect(page.getByRole('status')).toContainText('Sesión de plataforma revalidada');
  await expect(page.locator('ol.controls > li')).toHaveCount(4);
  expect(llamadas()).toBe(3);
});

test('C15-05.M04.06: revocación 403 no recupera controles privados ni filtra diagnóstico', async ({ page }) => {
  const llamadas = await simularSesion(page, n => n === 1 ? 200 : 403);
  await page.goto('/plataforma/auditoria-operativa-v5');
  const boton = page.getByRole('button', { name: 'Revalidar sesión de auditoría' });
  await boton.click();
  await expect(page.getByRole('alert')).toContainText('Sin permiso de plataforma confirmado');
  await expect(page.locator('ol.controls > li')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_C15_M0406_NEVER_EXPOSE')).toHaveCount(0);
  expect(llamadas()).toBe(2);
});
