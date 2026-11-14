import { test as base } from '@playwright/test';

/**
 * Fixture común para specs UI que usan tokens sintéticos y mockean su backend.
 *
 * El runtime productivo exige que cada navegación protegida revalide el tenant
 * contra /tenant-context/{id}. Las specs puramente UI históricas usan tokens
 * opacos (no JWT) porque no ejercitan autenticación real; sin esta adaptación
 * el guard fail-closed las envía a /login antes de alcanzar sus mocks.
 *
 * Sólo los tokens inequívocamente sintéticos reciben un contexto tenant mockeado.
 * Los JWT reales y las peticiones sin bearer continúan al backend para que las
 * suites de seguridad/aislamiento sigan validando autorización real.
 */
export const test = base.extend({
  page: async ({ page }, use) => {
    await page.route('**/tenant-context/*', async (route) => {
      if (route.request().method() !== 'GET') {
        await route.continue();
        return;
      }

      const authorization = route.request().headers()['authorization'] ?? '';
      const token = authorization.replace(/^Bearer\s+/i, '').trim();
      const isSyntheticToken = token.length > 0 && token.split('.').length !== 3;

      if (!isSyntheticToken) {
        await route.continue();
        return;
      }

      const url = new URL(route.request().url());
      const rawEmpresaId = url.pathname.split('/').filter(Boolean).at(-1) ?? '';
      const empresaId = Number.parseInt(rawEmpresaId, 10);

      if (!Number.isInteger(empresaId) || empresaId <= 0) {
        await route.continue();
        return;
      }

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          message: 'Contexto tenant E2E verificado',
          data: {
            usuarioId: 1,
            empresaId,
            rolId: 1,
            rolNombre: 'E2E',
            esAdministrador: false
          }
        })
      });
    });

    await use(page);
  }
});

export * from '@playwright/test';
