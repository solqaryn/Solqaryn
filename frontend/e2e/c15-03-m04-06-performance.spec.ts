import { expect, test, type Page } from '@playwright/test';
// C15-03.M04.06: navegador real, respuesta HTTP de sesión simulada; NO prueba JWT/tenancy backend.
type Respuesta = { estado: number; demoraMs?: number };
const headers = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function sesion(page: Page, respuesta: () => Respuesta): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'synthetic-browser-not-a-signed-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers });
      return;
    }
    if (route.request().method() !== 'GET') {
      await route.abort();
      return;
    }
    const { estado, demoraMs } = respuesta();
    if (demoraMs) await new Promise<void>(resolve => setTimeout(resolve, demoraMs));
    await route.fulfill({
      status: estado, headers, contentType: 'application/json',
      body: JSON.stringify(estado === 200
        ? { success: true, data: {
          nombreUsuario: 'Auditor', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true
        } }
        : { success: false, message: 'SENSITIVE_SERVER_TRACE_DO_NOT_RENDER' })
    });
  });
}
test('densidad: 320/768/1280 sin overflow y columnas reales 1/2/2', async ({ page }) => {
  await sesion(page, () => ({ estado: 200 }));
  for (const ancho of [320, 768, 1280]) {
    await page.setViewportSize({ width: ancho, height: 800 });
    await page.goto('/plataforma/auditoria-tenancy-v5');
    const tarjetas = page.locator('ol.fronteras li');
    await expect(tarjetas).toHaveCount(4);
    const geometria = await page.locator('ol.fronteras').evaluate(lista => ({
      columnas: getComputedStyle(lista).gridTemplateColumns.trim().split(/\s+/).length,
      desborde: document.documentElement.scrollWidth - document.documentElement.clientWidth
    }));
    expect(geometria.columnas).toBe(ancho <= 720 ? 1 : 2);
    expect(geometria.desborde).toBeLessThanOrEqual(2);
  }
});
test('caso adversarial: CSS con columna fija invalida la prueba de desborde', async ({ page }) => {
  await sesion(page, () => ({ estado: 200 }));
  await page.setViewportSize({ width: 320, height: 800 });
  await page.goto('/plataforma/auditoria-tenancy-v5');
  await expect(page.locator('ol.fronteras li')).toHaveCount(4);
  const medir = () => page.evaluate(() =>
    document.documentElement.scrollWidth - document.documentElement.clientWidth);
  expect(await medir()).toBeLessThanOrEqual(2);
  await page.addStyleTag({ content: 'ol.fronteras { grid-template-columns: 650px !important; }' });
  expect(await medir(), 'Un CSS defectuoso debe disparar el oráculo').toBeGreaterThan(2);
});
test('revalidación lenta oculta las cuatro fronteras y deshabilita doble acción', async ({ page }) => {
  let lenta = false;
  await sesion(page, () => ({ estado: 200, demoraMs: lenta ? 900 : 0 }));
  await page.goto('/plataforma/auditoria-tenancy-v5');
  await expect(page.locator('ol.fronteras li')).toHaveCount(4);
  lenta = true;
  const boton = page.getByRole('button', { name: 'Revalidar sesión de auditoría de tenancy' });
  await boton.click();
  await expect(page.locator('.tenancy-audit .estado [role="status"]'))
    .toContainText('Revalidando autorización privada');
  await expect(page.locator('ol.fronteras li')).toHaveCount(0);
  await expect(boton).toBeDisabled();
  await expect(page.locator('ol.fronteras li')).toHaveCount(4);
  await expect(boton).toBeEnabled();
});
test('HTTP 503 posterior bloquea contenido sin diagnóstico interno ni escrituras', async ({ page }) => {
  let fallar = false;
  const mutaciones: string[] = [];
  page.on('request', request => {
    if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method()))
      mutaciones.push(request.method() + ' ' + new URL(request.url()).pathname);
  });
  await sesion(page, () => ({ estado: fallar ? 503 : 200, demoraMs: fallar ? 150 : 0 }));
  await page.goto('/plataforma/auditoria-tenancy-v5');
  await expect(page.locator('ol.fronteras li')).toHaveCount(4);
  fallar = true;
  await page.getByRole('button', { name: 'Revalidar sesión de auditoría de tenancy' }).click();
  await expect(page.locator('.tenancy-audit .estado [role="alert"]'))
    .toContainText('No fue posible revalidar la sesión');
  await expect(page.locator('ol.fronteras li')).toHaveCount(0);
  await expect(page.getByText('SENSITIVE_SERVER_TRACE_DO_NOT_RENDER')).toHaveCount(0);
  expect(mutaciones).toEqual([]);
});
test('DOMContentLoaded y nota NO_MEDIDO en viewport móvil y escritorio', async ({ page }) => {
  await sesion(page, () => ({ estado: 200 }));
  for (const ancho of [320, 1280]) {
    await page.setViewportSize({ width: ancho, height: 800 });
    await page.goto('/plataforma/auditoria-tenancy-v5');
    await expect(page.locator('ol.fronteras li')).toHaveCount(4);
    const milisegundos = await page.evaluate(() => {
      const nav = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming | undefined;
      return nav ? nav.domContentLoadedEventEnd - nav.startTime : -1;
    });
    expect(milisegundos).toBeGreaterThan(0);
    expect(milisegundos).toBeLessThan(15000);
    await expect(page.getByRole('note')).toContainText('NO_MEDIDO');
    await expect(page.locator('.tenancy-audit form, .tenancy-audit input')).toHaveCount(0);
  }
});
