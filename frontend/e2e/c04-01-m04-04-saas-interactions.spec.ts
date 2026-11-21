import { expect, test, type Page, type Route } from '@playwright/test';

const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id, Idempotency-Key',
  'Access-Control-Allow-Methods': 'GET, POST, OPTIONS'
};

interface FixtureOptions {
  tenantAllowed?: boolean;
  roleAllowed?: boolean;
  canCreate?: boolean;
  hasSubscription?: boolean;
  subscriptionUnavailable?: () => boolean;
  onOnboarding?: (route: Route) => Promise<void>;
}

async function isolatedSaasFixture(page: Page, options: FixtureOptions = {}) {
  const opts = {
    tenantAllowed: true,
    roleAllowed: true,
    canCreate: false,
    hasSubscription: true,
    subscriptionUnavailable: undefined as (() => boolean) | undefined,
    ...options
  };
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'SYNTHETIC_NOT_A_SIGNED_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', '1');
  });
  await page.route('**/*', async route => {
    const request = route.request();
    const url = new URL(request.url());
    const path = url.pathname;
    if (request.method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    if (request.method() === 'POST' && /\/saas\/tenants\/1\/onboarding$/.test(path)) {
      if (!opts.onOnboarding) {
        await route.fulfill({ status: 403, headers: cors, contentType: 'application/json',
          body: JSON.stringify({ success: false, message: 'Onboarding no autorizado en fixture.' }) });
      } else {
        await opts.onOnboarding(route);
      }
      return;
    }
    let status = 200;
    let data: unknown = null;
    if (/\/tenant-context\/1$/.test(path)) {
      if (!opts.tenantAllowed) status = 403;
      else data = { usuarioId: 1, empresaId: 1, rolId: 1,
        rolNombre: 'Administrador', esAdministrador: true };
    } else if (/\/permisos\/mis-permisos\/empresa\/1$/.test(path)) {
      const permissions = opts.roleAllowed
        ? (opts.canCreate ? ['Configuracion:Ver', 'Configuracion:Crear'] : ['Configuracion:Ver'])
        : [];
      data = { esAdministrador: opts.canCreate, operacionMultiEmpresaRestringida: false,
        permisos: permissions, capacidadesHabilitadas: permissions,
        rutasHabilitadas: opts.roleAllowed ? ['configuracion'] : [] };
    } else if (/\/empresa-configuracion$/.test(path)) {
      data = { id: 1, nombreComercial: 'Empresa A', nombreVisibleSistema: 'SOLQARYN',
        moneda: 'HNL', zonaHoraria: 'America/Tegucigalpa', formatoFecha: 'dd/MM/yyyy' };
    } else if (/\/tema-visual$/.test(path)) {
      data = {};
    } else if (/\/saas\/tenants\/1\/suscripcion$/.test(path)) {
      if (opts.subscriptionUnavailable?.()) status = 503;
      else if (!opts.hasSubscription) status = 404;
      else data = { id: 7, planCodigo: 'BASE', planNombre: 'Plan Base',
        estado: 'ACTIVA', inicioUtc: '2026-01-01T00:00:00Z', finUtc: null };
    } else if (/\/saas\/tenants\/1\/limites$/.test(path)) {
      const found = !url.searchParams.has('clave') || url.searchParams.get('clave') === 'USUARIOS';
      data = { items: found ? [{ clave: 'USUARIOS', valorMaximo: 10 }] : [],
        total: found ? 1 : 0, pagina: 1, tamanoPagina: 50 };
    } else if (/\/saas\/tenants\/1\/modulos\/[^/]+\/entitlement$/.test(path)) {
      data = { habilitado: true, motivo: 'INCLUIDO' };
    } else if (path.includes('/api/') || ['5000', '5001'].includes(url.port)) {
      status = 404;
    } else {
      await route.continue();
      return;
    }
    await route.fulfill({ status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200
        ? { success: true, data }
        : { success: false, message: 'No se pudo completar la consulta segura.' }) });
  });
}

test('C04-01.M04.04 filtro real Angular GET, mensaje de vacío y navegación conservada', async ({ page }) => {
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  await isolatedSaasFixture(page);
  await page.goto('/configuracion');
  await expect(page.getByRole('heading', { name: 'Plan y suscripción' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Plan Base' })).toBeVisible();
  const filter = page.getByRole('form', { name: 'Filtrar límites' });
  await filter.getByRole('textbox', { name: 'Clave' }).fill('SIN_COINCIDENCIA');
  await filter.getByRole('button', { name: 'Filtrar' }).click();
  await expect(page.getByText('No hay límites para el filtro indicado.')).toBeVisible();
  await expect(page).toHaveURL(/\/configuracion(?:\?|$)/);
  expect(writes, 'El filtro jamás ejecuta un POST ni un cobro').toEqual([]);
});

test('C04-01.M04.04 fallo HTTP 503 muestra alerta y reintento efectivo sin navegación insegura', async ({ page }) => {
  let subscriptionUnavailable = true;
  await isolatedSaasFixture(page, { subscriptionUnavailable: () => subscriptionUnavailable });
  await page.goto('/configuracion');
  const card = page.locator('.saas-card');
  await expect(card.getByRole('alert')).toContainText('No se pudo completar la consulta segura.');
  await expect(card.getByRole('heading', { name: 'Plan Base' })).toHaveCount(0);
  subscriptionUnavailable = false;
  await card.getByRole('button', { name: 'Reintentar' }).click();
  await expect(card.getByRole('heading', { name: 'Plan Base' })).toBeVisible();
  await expect(card.getByRole('alert')).toHaveCount(0);
  await expect(page).toHaveURL(/\/configuracion(?:\?|$)/);
});

test('C04-01.M04.04 denegaciones tenant y rol no muestran formulario ni plan privado', async ({ context }) => {
  for (const opt of [{ tenantAllowed: false }, { tenantAllowed: true, roleAllowed: false }]) {
    const page = await context.newPage();
    await isolatedSaasFixture(page, opt);
    await page.goto('/configuracion');
    await expect(page).toHaveURL(/\/(?:login|perfil)(?:\?|$)/);
    await expect(page.getByRole('heading', { name: 'Plan Base' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Iniciar suscripción' })).toHaveCount(0);
    await page.close();
  }
});

test('C04-01.M04.04 onboarding simulado bloquea doble clic y conserva Idempotency-Key', async ({ page }) => {
  let releases: (() => void) | undefined;
  const gate = new Promise<void>(resolve => { releases = resolve; });
  let writes = 0;
  const keys: string[] = [];
  const unexpectedPost: string[] = [];
  page.on('request', request => {
    if (request.method() === 'POST' && !/\/saas\/tenants\/1\/onboarding$/.test(new URL(request.url()).pathname)) {
      unexpectedPost.push(request.url());
    }
  });
  await isolatedSaasFixture(page, {
    hasSubscription: false,
    canCreate: true,
    onOnboarding: async route => {
      writes++;
      keys.push(route.request().headers()['idempotency-key'] ?? '');
      await gate;
      await route.fulfill({ status: 200, headers: cors, contentType: 'application/json',
        body: JSON.stringify({ success: true, data: {
          id: 8, planCodigo: 'BASE', planNombre: 'Plan Base',
          estado: 'ACTIVA', inicioUtc: '2026-01-01T00:00:00Z', finUtc: null
        } }) });
    }
  });
  await page.goto('/configuracion');
  const card = page.locator('.saas-card');
  await expect(card.getByText('Este tenant aún no tiene una suscripción')).toBeVisible();
  const submit = card.getByRole('button', { name: 'Iniciar suscripción' });
  await expect(submit).toBeDisabled();
  await card.getByRole('textbox', { name: 'Código del plan' }).fill('BASE');
  await expect(submit).toBeEnabled();
  await submit.click();
  await expect.poll(() => writes).toBe(1);
  await expect(submit).toBeDisabled();
  expect(keys[0]).toMatch(/^saas-ui-/);
  expect(unexpectedPost).toEqual([]);
  releases?.();
  await expect(card.getByRole('heading', { name: 'Plan Base' })).toBeVisible();
  expect(writes, 'Solo un onboarding simulado por interacción').toBe(1);
  await expect(page).toHaveURL(/\/configuracion(?:\?|$)/);
});
