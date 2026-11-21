import { expect, test, type Page } from '@playwright/test';

const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
interface BrandingFixture {
  tenantId?: 1 | 2;
  brand?: string;
  plan?: string;
  denied?: boolean;
  inventario?: 'INCLUIDO' | 'NO_INCLUIDO' | 'NO_DISPONIBLE';
}
async function fixture(page: Page, options: BrandingFixture = {}) {
  const tenantId = options.tenantId ?? 1;
  const brand = options.brand ?? 'Empresa Norte';
  const plan = options.plan ?? 'Plan Norte';
  const ent = options.inventario ?? 'INCLUIDO';
  await page.addInitScript(id => {
    localStorage.setItem('solqaryn_token', 'BRANDING_FIXTURE_NOT_SIGNED_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', String(id));
  }, tenantId);
  await page.route('**/*', async route => {
    const request = route.request();
    const url = new URL(request.url());
    const path = url.pathname;
    if (request.method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    let status = 200;
    let data: unknown = null;
    if (/\/tenant-context\/\d+$/.test(path)) {
      if (options.denied || !path.endsWith('/' + tenantId)) status = 403;
      else data = { usuarioId: tenantId, empresaId: tenantId, rolId: 1,
        rolNombre: 'Administrador', esAdministrador: true };
    } else if (/\/permisos\/mis-permisos\/empresa\/\d+$/.test(path)) {
      data = { esAdministrador: false, operacionMultiEmpresaRestringida: false,
        permisos: ['Configuracion:Ver'], capacidadesHabilitadas: ['Configuracion:Ver'],
        rutasHabilitadas: ['configuracion'] };
    } else if (/\/empresa-configuracion$/.test(path)) {
      data = { id: tenantId, nombreComercial: brand, nombreVisibleSistema: brand,
        copyright: brand, moneda: 'HNL', zonaHoraria: 'America/Tegucigalpa', formatoFecha: 'dd/MM/yyyy' };
    } else if (/\/tema-visual$/.test(path)) {
      data = {};
    } else if (/\/saas\/tenants\/\d+\/suscripcion$/.test(path)) {
      if (!path.endsWith('/' + tenantId + '/suscripcion')) status = 403;
      else data = { id: tenantId + 10, planCodigo: 'DYNAMIC', planNombre: plan,
        estado: 'ACTIVA', inicioUtc: '2026-01-01T00:00:00Z', finUtc: null };
    } else if (/\/saas\/tenants\/\d+\/limites$/.test(path)) {
      data = { items: [{ clave: 'USUARIOS', valorMaximo: tenantId * 10 }],
        total: 1, pagina: 1, tamanoPagina: 50 };
    } else if (/\/saas\/tenants\/\d+\/modulos\/[^/]+\/entitlement$/.test(path)) {
      const moduleKey = decodeURIComponent(path.split('/').at(-2) ?? '');
      if (moduleKey === 'INVENTARIO' && ent === 'NO_DISPONIBLE') status = 503;
      else data = { habilitado: moduleKey === 'INVENTARIO' ? ent === 'INCLUIDO' : true,
        motivo: moduleKey === 'INVENTARIO' && ent === 'NO_INCLUIDO' ? 'FUERA_DEL_PLAN' : 'INCLUIDO' };
    } else if (path.includes('/api/') || ['5000', '5001'].includes(url.port)) {
      status = 404;
    } else {
      await route.continue();
      return;
    }
    await route.fulfill({ status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200 ? { success: true, data }
        : { success: false, message: 'DENIAL_DETAILS_NEVER_EXPOSE' }) });
  });
}

test('C04-01.M04.03 marca y plan dinámicos de tenant A/B aislados en Angular real', async ({ context }) => {
  const tenants = [
    { tenantId: 1 as const, brand: 'Empresa Norte', plan: 'Plan Norte' },
    { tenantId: 2 as const, brand: 'Empresa Sur', plan: 'Plan Sur' }
  ];
  for (const data of tenants) {
    const page = await context.newPage();
    await fixture(page, data);
    await page.goto('/configuracion');
    const card = page.locator('.saas-card');
    await expect(card.getByRole('heading', { name: data.plan })).toBeVisible();
    await expect(page.locator('input[formControlName="nombreComercial"]')).toHaveValue(data.brand);
    await expect(card.getByRole('heading', { name: data.tenantId === 1 ? 'Plan Sur' : 'Plan Norte' })).toHaveCount(0);
    await page.close();
  }
});

test('C04-01.M04.03 entitlement deshabilitado no ofrece componente habilitado', async ({ page }) => {
  await fixture(page, { inventario: 'NO_INCLUIDO' });
  await page.goto('/configuracion');
  const card = page.locator('.saas-card');
  await expect(card.getByRole('heading', { name: 'Plan Norte' })).toBeVisible();
  const entitlementTile = card.locator('.module-tile').filter({ hasText: 'Inventario' });
  await expect(entitlementTile.getByRole('button')).toBeDisabled();
  await expect(entitlementTile.getByText('Inventario no está incluido en el plan actual.')).toBeVisible();
  const sales = card.locator('.module-tile').filter({ hasText: 'Ventas' });
  await expect(sales.getByRole('button')).toBeEnabled();
});

test('C04-01.M04.03 proveedor de entitlement indisponible falla cerrado', async ({ page }) => {
  await fixture(page, { inventario: 'NO_DISPONIBLE' });
  await page.goto('/configuracion');
  const entitlementTile = page.locator('.saas-card .module-tile').filter({ hasText: 'Inventario' });
  await expect(entitlementTile.getByRole('button')).toBeDisabled();
  await expect(entitlementTile.getByText('Inventario no pudo validarse y permanece deshabilitado de forma segura.')).toBeVisible();
  await expect(page.getByText('DENIAL_DETAILS_NEVER_EXPOSE')).toHaveCount(0);
});

test('C04-01.M04.03 tenant denegado oculta marca y plan sin escrituras', async ({ page }) => {
  const writes: string[] = [];
  page.on('request', request => {
    if (!['GET','HEAD','OPTIONS'].includes(request.method())) writes.push(request.method());
  });
  await fixture(page, { tenantId: 2, denied: true, brand: 'SECRETO_SUR', plan: 'PLAN_PRIVADO_SUR' });
  await page.goto('/configuracion');
  await expect(page).toHaveURL(/\/(?:login|perfil)(?:\?|$)/);
  await expect(page.getByText('SECRETO_SUR')).toHaveCount(0);
  await expect(page.getByText('PLAN_PRIVADO_SUR')).toHaveCount(0);
  expect(writes).toEqual([]);
});
