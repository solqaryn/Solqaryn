import { expect, test, type Page } from '@playwright/test';

const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
type Options = {
  tenantAllowed?: boolean;
  roleAllowed?: boolean;
  subscriptionMissing?: boolean;
  holdSubscription?: Promise<void>;
  limitsUnavailable?: () => boolean;
};
async function statesFixture(page: Page, options: Options = {}) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'SYNTHETIC_NOT_SIGNED_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', '1');
  });
  await page.route('**/*', async route => {
    const request = route.request(), url = new URL(request.url()), path = url.pathname;
    if (request.method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    let status = 200;
    let data: unknown = null;
    if (/\/tenant-context\/1$/.test(path)) {
      if (options.tenantAllowed === false) status = 403;
      else data = { usuarioId: 1, empresaId: 1, rolId: 1, rolNombre: 'Administrador', esAdministrador: true };
    } else if (/\/permisos\/mis-permisos\/empresa\/1$/.test(path)) {
      const allowed = options.roleAllowed !== false;
      data = {
        esAdministrador: false, operacionMultiEmpresaRestringida: false,
        permisos: allowed ? ['Configuracion:Ver'] : [],
        capacidadesHabilitadas: allowed ? ['Configuracion:Ver'] : [],
        rutasHabilitadas: allowed ? ['configuracion'] : []
      };
    } else if (/\/empresa-configuracion$/.test(path)) {
      data = { id: 1, nombreComercial: 'Empresa A', nombreVisibleSistema: 'SOLQARYN',
        moneda: 'HNL', zonaHoraria: 'America/Tegucigalpa', formatoFecha: 'dd/MM/yyyy' };
    } else if (/\/tema-visual$/.test(path)) {
      data = {};
    } else if (/\/saas\/tenants\/1\/suscripcion$/.test(path)) {
      if (options.holdSubscription) await options.holdSubscription;
      if (options.subscriptionMissing) status = 404;
      else data = { id: 7, planCodigo: 'BASE', planNombre: 'Plan Base',
        estado: 'ACTIVA', inicioUtc: '2026-01-01T00:00:00Z', finUtc: null };
    } else if (/\/saas\/tenants\/1\/limites$/.test(path)) {
      if (options.limitsUnavailable?.()) status = 503;
      else data = { items: [{ clave: 'USUARIOS', valorMaximo: 10 }], total: 1, pagina: 1, tamanoPagina: 50 };
    } else if (/\/saas\/tenants\/1\/modulos\/[^/]+\/entitlement$/.test(path)) {
      data = { habilitado: true, motivo: 'INCLUIDO' };
    } else if (path.includes('/api/') || ['5000','5001'].includes(url.port)) {
      status = 404;
    } else {
      await route.continue();
      return;
    }
    await route.fulfill({
      status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data }
        : { success: false, message: 'Solicitud de consulta denegada o no disponible.' })
    });
  });
}

test('C04-01.M04.02 spinner de carga pendiente y resultado verificable en Chromium', async ({ page }) => {
  let release!: () => void;
  const pending = new Promise<void>(resolve => { release = resolve; });
  await statesFixture(page, { holdSubscription: pending });
  await page.goto('/configuracion');
  const card = page.locator('.saas-card');
  await expect(card.getByText('Cargando plan y límites…')).toBeVisible();
  await expect(card.getByRole('heading', { name: 'Plan Base' })).toHaveCount(0);
  release();
  await expect(card.getByRole('heading', { name: 'Plan Base' })).toBeVisible();
  await expect(card.getByRole('heading', { name: 'Límites del plan' })).toBeVisible();
  await expect(card.getByText('Cargando plan y límites…')).toHaveCount(0);
});

test('C04-01.M04.02 suscripción ausente da vacío sin ofrecer alta no autorizada', async ({ page }) => {
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET','HEAD','OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  await statesFixture(page, { subscriptionMissing: true });
  await page.goto('/configuracion');
  const card = page.locator('.saas-card');
  await expect(card.getByText('Este tenant aún no tiene una suscripción')).toBeVisible();
  await expect(card.getByText('Necesitas el permiso', { exact: false })).toBeVisible();
  await expect(card.getByRole('button', { name: 'Iniciar suscripción' })).toHaveCount(0);
  await expect(card.getByRole('heading', { name: 'Plan Base' })).toHaveCount(0);
  expect(writes).toEqual([]);
});

test('C04-01.M04.02 error 503 al consultar límites y recuperación con Reintentar', async ({ page }) => {
  let limitsDown = true;
  await statesFixture(page, { limitsUnavailable: () => limitsDown });
  await page.goto('/configuracion');
  const card = page.locator('.saas-card');
  await expect(card.getByRole('alert')).toContainText('Solicitud de consulta denegada o no disponible.');
  await expect(card.getByRole('heading', { name: 'Plan Base' })).toHaveCount(0);
  limitsDown = false;
  await card.getByRole('button', { name: 'Reintentar' }).click();
  await expect(card.getByRole('heading', { name: 'Plan Base' })).toBeVisible();
  await expect(card.getByRole('alert')).toHaveCount(0);
  await expect(page).toHaveURL(/\/configuracion(?:\?|$)/);
});

test('C04-01.M04.02 falta de tenant o rol protege los estados y el plan', async ({ context }) => {
  for (const options of [{ tenantAllowed: false }, { roleAllowed: false }]) {
    const page = await context.newPage();
    await statesFixture(page, options);
    await page.goto('/configuracion');
    await expect(page).toHaveURL(/\/(?:login|perfil)(?:\?|$)/);
    await expect(page.getByRole('heading', { name: 'Plan Base' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Iniciar suscripción' })).toHaveCount(0);
    await page.close();
  }
});
