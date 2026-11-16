import { test, expect, type Page } from '@playwright/test';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

/**
 * C02-04.M05.04: positive browser acceptance of the global route catalogue.
 * The session endpoint below is synthetic, NOT an HTTP JWT/RBAC/MySQL
 * integration certificate, grant, or production entitlement.
 */
const catalog = JSON.parse(
  readFileSync(resolve(process.cwd(), '../docs/v5/c02-04-m05-01-routes.json'), 'utf8')
) as {schemaVersion: string; routes: Array<{id: string; path: string}>};

const titles: Record<string,string> = {
  GV01: 'Administración global',
  GV02: 'Cobertura V5',
  GV03: 'Ciclo V5',
  GV04: 'Controles de ejecución y evidencia',
  GV05: 'Controles de recuperación y aislamiento',
  GV06: 'Placement compartido y dedicado',
  GV07: 'Consola global auditada',
  GV08: 'Fronteras de aislamiento y evidencia',
  GV09: 'Decisiones y controles V5',
  GV10: 'Respuesta, revocación y recuperación segura',
  GV11: 'Verificaciones de acceso y abuso',
  GV12: 'Revisión de cobertura del plan',
  GV13: 'Diagnóstico y señales seguras'
};

const endpoint = /\/plataforma\/sesion(?:\?|$)/;
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function syntheticGlobalSession(page: Page, status: 200 | 403 = 200) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'SYNTHETIC_NOT_A_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  let gets = 0;
  await page.route(endpoint, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    gets++;
    await route.fulfill({
      status,
      headers: cors,
      contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data: { nombreUsuario: 'Synthetic global admin',
            rolNombre: 'Global', ambito: 'Plataforma', esAdministrador: true } }
        : { success: false, data: null, message: 'PRIVATE_C0204_M0504_DIAGNOSTIC' })
    });
  });
  return () => gets;
}

test.describe('C02-04.M05.04 positive catalogue in real Chromium (synthetic server)', () => {
  test.describe.configure({ retries: 0 });
  test.use({ extraHTTPHeaders: {} });

  test('catalog has exactly 13 unique, known, explicit routes', () => {
    expect(catalog.schemaVersion).toBe('C02-04.M05.01/v18.1');
    expect(catalog.routes).toHaveLength(13);
    expect(new Set(catalog.routes.map(x => x.id)).size).toBe(13);
    expect(new Set(catalog.routes.map(x => x.path)).size).toBe(13);
    expect(catalog.routes.map(x => x.id).sort()).toEqual(Object.keys(titles).sort());
  });

  for (const item of catalog.routes) {
    test(item.id + ' granted synthetic session displays its real private heading, read-only', async ({page}) => {
      const gets = await syntheticGlobalSession(page);
      const writes: string[] = [];
      page.on('request', req => {
        if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(req.method())) {
          writes.push(req.method() + ' ' + req.url());
        }
      });
      await page.goto('/' + item.path);
      await expect.poll(() => new URL(page.url()).pathname).toBe('/' + item.path);
      await expect(page.getByRole('heading', {level: 1}).first()).toContainText(titles[item.id]);
      await expect.poll(gets).toBeGreaterThanOrEqual(1);
      await expect(page.getByText('PRIVATE_C0204_M0504_DIAGNOSTIC')).toHaveCount(0);
      expect(writes, 'positive read-only navigation must not mutate the ERP').toEqual([]);
    });
  }

  test('global console positively revalidates only after second GET, 320px', async ({page}) => {
    const gets = await syntheticGlobalSession(page);
    await page.setViewportSize({width: 320, height: 800});
    await page.goto('/plataforma/consola-global-v5');
    await expect(page.locator('.control-cards li')).toHaveCount(4);
    await page.getByRole('button', {name: 'Revalidar sesión global'}).click();
    await expect(page.locator('.control-cards li')).toHaveCount(4);
    expect(gets()).toBe(2);
    expect(await page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth
    )).toBeLessThanOrEqual(2);
  });

  test('server 403 synthetic denies positive access without private diagnostic', async ({page}) => {
    const gets = await syntheticGlobalSession(page, 403);
    await page.goto('/plataforma/consola-global-v5');
    await expect(page).toHaveURL(/\/login\?plataforma=denegado(?:&|$)/);
    await expect(page.locator('.control-cards li')).toHaveCount(0);
    await expect(page.getByText('PRIVATE_C0204_M0504_DIAGNOSTIC')).toHaveCount(0);
    expect(gets()).toBe(1);
  });
});
