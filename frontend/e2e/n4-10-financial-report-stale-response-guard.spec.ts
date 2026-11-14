import { expect, Page, test } from '@playwright/test';

const ADMIN_USERNAME = process.env['PHASE7_ADMIN_USERNAME'] ?? 'e2e_admin';
const ADMIN_PASSWORD = process.env['PHASE7_ADMIN_PASSWORD'] ?? 'E2E.Admin#2026!';

async function loginUi(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="nombreUsuario"]').fill(ADMIN_USERNAME);
  await page.locator('input[formcontrolname="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL(url => url.pathname !== '/login', { timeout: 20_000 });
}

function resumen(usuariosActivos: number) {
  return {
    data: {
      usuariosTotales: usuariosActivos + 3,
      usuariosActivos,
      usuariosPrivilegiados: 1,
      rolesTotales: 2,
      rolesActivos: 2,
      rolesSinPermisos: 0,
      permisosCatalogados: 10,
      eventosAuditoria: 4,
      eventosExitosos: 4,
      eventosRechazados: 0,
      eventosConError: 0,
      actividadPorModulo: [],
      alertas: []
    }
  };
}

const auditoria = {
  data: { total: 0, exitosos: 0, rechazados: 0, conError: 0, usuariosUnicos: 0, porModulo: [] }
};

test.describe('N4.10.A — stale response guard de reportes administrativos', () => {
  test('una respuesta lenta de un rango anterior no sobrescribe la selección más reciente', async ({ page }) => {
    await loginUi(page);

    await page.route('**/reportes-administrativos/**', async route => {
      const url = new URL(route.request().url());
      const path = url.pathname;
      const desde = url.searchParams.get('desde');

      if (path.endsWith('/usuarios-accesos') || path.endsWith('/roles-permisos')) {
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ data: [] }) });
        return;
      }

      if (path.endsWith('/auditoria-resumen')) {
        if (desde === '2026-01-01') await new Promise(resolve => setTimeout(resolve, 350));
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(auditoria) });
        return;
      }

      if (path.endsWith('/resumen')) {
        if (desde === '2026-01-01') {
          await new Promise(resolve => setTimeout(resolve, 350));
          await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(resumen(101)) });
          return;
        }

        const value = desde === '2026-02-01' ? 202 : 50;
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(resumen(value)) });
        return;
      }

      await route.continue();
    });

    await page.goto('/auditoria');
    await expect(page.getByRole('heading', { name: 'Reportes administrativos' })).toBeVisible();
    await expect(page.locator('.metric-card').first()).toContainText('50');

    const desdeInput = page.getByLabel('Desde');
    const hastaInput = page.getByLabel('Hasta');

    await desdeInput.fill('2026-01-01');
    await hastaInput.fill('2026-01-31');

    const firstRequest = page.waitForRequest(request => {
      const url = new URL(request.url());
      return url.pathname.endsWith('/reportes-administrativos/resumen') && url.searchParams.get('desde') === '2026-01-01';
    });

    await page.getByRole('button', { name: 'Actualizar' }).click();
    await firstRequest;

    await desdeInput.fill('2026-02-01');
    await hastaInput.fill('2026-02-28');

    await expect(page.locator('.metric-card').first()).toContainText('202', { timeout: 5_000 });
    await page.waitForTimeout(450);
    await expect(page.locator('.metric-card').first()).toContainText('202');
    await expect(page.locator('.metric-card').first()).not.toContainText('101');
  });
});
