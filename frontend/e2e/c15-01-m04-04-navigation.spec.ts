import { expect, test, type Page } from '@playwright/test';

/** C15-01.M04.04: pruebas reales Chromium sobre sesión SIMULADA, no autorización backend. */
async function session(page: Page, status: 200 | 403): Promise<void> {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1501-m0404-synthetic');
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
      status, headers, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: { nombreUsuario: 'Admin synthetic',
            rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, message: 'PRIVATE_AUDIT_TRACE_NEVER_RENDER' })
    });
  });
}

test('C15-01.M04.04 anónimo y sesión 403 deniegan toda navegación privada', async ({ page }) => {
  await page.goto('/plataforma/trazabilidad-cobertura-v5');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.coverage-list li')).toHaveCount(0);
  await session(page, 403);
  await page.goto('/plataforma/trazabilidad-cobertura-v5');
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByText('PRIVATE_AUDIT_TRACE_NEVER_RENDER')).toHaveCount(0);
  await expect(page.locator('.coverage-list li')).toHaveCount(0);
});

test('C15-01.M04.04 mensajes exactos no confunden NO_MEDIDO con aprobación', async ({ page }) => {
  await session(page, 200);
  await page.goto('/plataforma/trazabilidad-cobertura-v5');
  await expect(page.getByRole('heading', { name: 'Sin certificación automática' })).toBeVisible();
  await expect(page.locator('.coverage-shell [role="status"]')).toContainText('NO_MEDIDO');
  await expect(page.locator('.coverage-shell [aria-label="Evidencia no verificada"]')).toHaveCount(4);
  await expect(page.locator('.coverage-shell [role="note"]')).toContainText('No hay operaciones');
  await expect(page.locator('.coverage-shell button, .coverage-shell form')).toHaveCount(0);
});

test('C15-01.M04.04 navegación Enter por teclado vuelve sin acción de escritura', async ({ page }) => {
  const writes: string[] = [];
  page.on('request', r => {
    if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(r.method())) writes.push(r.url());
  });
  await session(page, 200);
  await page.goto('/plataforma/trazabilidad-cobertura-v5');
  const back = page.getByRole('link', { name: 'Regresar a administración de plataforma' });
  await expect(back).toBeVisible();
  await back.focus();
  await expect(back).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/\/plataforma(?:\/|$)/);
  expect(writes, 'navegación read-only no envía mutaciones').toHaveLength(0);
});

test('C15-01.M04.04 no hay formulario o botón que permita doble envío', async ({ page }) => {
  await session(page, 200);
  await page.goto('/plataforma/trazabilidad-cobertura-v5');
  await expect(page.locator('.coverage-shell button, .coverage-shell form, .coverage-shell input, .coverage-shell textarea, .coverage-shell [role="button"]')).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Regresar a administración de plataforma' })).toHaveCount(1);
  await expect(page.locator('.coverage-list li')).toHaveCount(4);
});
