import { expect, test, type Page } from '@playwright/test';

const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};

async function sesionSintetica(page: Page, permitir: boolean) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0804-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', '71');
  });
  await page.route(/\/tenant-context\/71(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      return route.fulfill({ status: 204, headers: cors });
    }
    if (!permitir) {
      return route.fulfill({
        status: 403, headers: cors, contentType: 'application/json',
        body: JSON.stringify({ success: false })
      });
    }
    return route.fulfill({
      status: 200, headers: cors, contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        data: { usuarioId: 7, empresaId: 71, rolId: 3, rolNombre: 'Lectura', esAdministrador: false }
      })
    });
  });
}

test('C08-04.M04.04 - tenant 403 no puede interactuar con catálogos ni formularios', async ({ page }) => {
  await sesionSintetica(page, false);
  await page.goto('/extensiones-sectoriales');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.sector-card')).toHaveCount(0);
  await expect(page.locator('.sector-readonly button, .sector-readonly form')).toHaveCount(0);
});

test('C08-04.M04.04 - Tab real alcanza el enlace informativo y Enter navega', async ({ page }) => {
  await sesionSintetica(page, true);
  await page.goto('/extensiones-sectoriales');
  const volver = page.getByRole('link', { name: 'Volver al panel' });
  await expect(volver).toBeVisible();
  let alcanzado = false;
  for (let i = 0; i < 60; i++) {
    await page.keyboard.press('Tab');
    if (await volver.evaluate(el => document.activeElement === el)) {
      alcanzado = true;
      break;
    }
  }
  expect(alcanzado).toBe(true);
  await expect(volver).toHaveAttribute('href', '/dashboard');
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/\/dashboard(?:\/|\?|$)/);
});

test('C08-04.M04.04 - mensajes seguros y ausencia de acciones duplicables de módulos', async ({ page }) => {
  await sesionSintetica(page, true);
  const mutaciones: string[] = [];
  const solicitudesModulos: string[] = [];
  page.on('request', request => {
    const url = new URL(request.url());
    if (/\/(?:sector-modules|capabilities|modulos-sectoriales)(?:\/|\?|$)/i.test(url.pathname)) {
      solicitudesModulos.push(url.pathname);
    }
    if (!['GET', 'OPTIONS', 'HEAD'].includes(request.method())) {
      mutaciones.push(request.method() + ' ' + url.pathname);
    }
  });
  await page.goto('/extensiones-sectoriales');
  const vista = page.locator('.sector-readonly');
  await expect(vista).toBeVisible();
  await expect(page.getByRole('status', { name: 'Estado de las capacidades' })).toContainText('Sin módulos conectados');
  await expect(vista).toContainText('No activa módulos ni modifica permisos');
  await expect(page.locator('.sector-card')).toHaveCount(4);
  await expect(vista.locator('form, button, input, select, textarea, [aria-pressed]')).toHaveCount(0);
  expect(solicitudesModulos).toHaveLength(0);
  expect(mutaciones).toHaveLength(0);
});
