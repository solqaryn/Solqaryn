import { expect, test, type Page } from '@playwright/test';
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function uiFixture(page: Page, tenantAllowed = true, roleAllowed = true) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'SYNTHETIC_FIXTURE_NOT_SIGNED_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', '1');
  });
  await page.route('**/*', async route => {
    const req = route.request(), url = new URL(req.url()), path = url.pathname;
    if (req.method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }
    let data: unknown = null;
    let status = 200;
    if (/\/tenant-context\/1$/.test(path)) {
      if (!tenantAllowed) status = 403;
      else data = { usuarioId: 1, empresaId: 1, rolId: 1, rolNombre: 'Administrador', esAdministrador: true };
    } else if (/\/permisos\/mis-permisos\/empresa\/1$/.test(path)) {
      data = { esAdministrador: true, operacionMultiEmpresaRestringida: false,
        permisos: roleAllowed ? ['Configuracion:Ver'] : [],
        capacidadesHabilitadas: roleAllowed ? ['Configuracion:Ver'] : [],
        rutasHabilitadas: roleAllowed ? ['configuracion'] : [] };
    } else if (/\/empresa-configuracion$/.test(path)) {
      data = { id: 1, nombreComercial: 'Empresa A', nombreVisibleSistema: 'SOLQARYN',
        moneda: 'HNL', zonaHoraria: 'America/Tegucigalpa', formatoFecha: 'dd/MM/yyyy' };
    } else if (/\/tema-visual$/.test(path)) {
      data = {};
    } else if (/\/saas\/tenants\/1\/suscripcion$/.test(path)) {
      data = { id: 7, planCodigo: 'BASE', planNombre: 'Plan Base',
        estado: 'ACTIVA', inicioUtc: '2026-01-01T00:00:00Z', finUtc: null };
    } else if (/\/saas\/tenants\/1\/limites$/.test(path)) {
      data = { items: [{ clave: 'USUARIOS', valorMaximo: 10 }], total: 1, pagina: 1, tamanoPagina: 50 };
    } else if (/\/saas\/tenants\/1\/modulos\/[^/]+\/entitlement$/.test(path)) {
      data = { habilitado: true, motivo: 'INCLUIDO' };
    } else if (path.includes('/api/') || ['5000','5001'].includes(url.port)) {
      status = 404;
    } else {
      await route.continue();
      return;
    }
    const body = status === 200 ? { success: true, data } : { success: false, message: 'PRIVATE_DENIAL_DETAILS' };
    await route.fulfill({ status, headers: cors, contentType: 'application/json', body: JSON.stringify(body) });
  });
}
async function openPage(page: Page) {
  await uiFixture(page);
  await page.goto('/configuracion');
  await expect(page.getByRole('heading', { name: 'Plan y suscripción', level: 2 })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Plan Base', level: 3 })).toBeVisible();
}
test('C04-01.M04.05 Chromium responsive and zoom 200 percent', async ({ page }) => {
  await openPage(page);
  const card = page.locator('.saas-card');
  for (const zoom of [100, 200]) {
    await page.evaluate(z => document.documentElement.style.fontSize = String(z) + '%', zoom);
    for (const width of [320, 375, 768, 1280]) {
      await page.setViewportSize({ width, height: 860 });
      await expect(card).toBeVisible();
      const bounds = await card.evaluate(el => ({ scroll: el.scrollWidth, client: el.clientWidth }));
      expect(bounds.scroll - bounds.client, 'overflow ' + width + '/' + zoom).toBeLessThanOrEqual(2);
    }
  }
});
test('C04-01.M04.05 Chromium genuine Tab, labels, CSS contrast', async ({ page }) => {
  await openPage(page);
  const input = page.getByRole('textbox', { name: 'Clave' });
  await expect(input).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Límites del plan' })).toBeVisible();
  const ratio = await page.locator('#saas-title').evaluate(el => {
    const rgb = (x: string) => (x.match(/[\d.]+/g) ?? []).slice(0, 4).map(Number);
    const fg = rgb(getComputedStyle(el).color);
    let bg = [255, 255, 255];
    for (let e: Element | null = el; e; e = e.parentElement) {
      const b = rgb(getComputedStyle(e).backgroundColor);
      if (b.length >= 3 && (b[3] === undefined || b[3] === 1)) { bg = b.slice(0,3); break; }
    }
    const lum = (v: number[]) => {
      const c = v.slice(0,3).map(n => {
        const x = n / 255;
        return x <= .04045 ? x / 12.92 : Math.pow((x + .055) / 1.055, 2.4);
      });
      return c[0] * .2126 + c[1] * .7152 + c[2] * .0722;
    };
    const a = lum(fg), b = lum(bg);
    return (Math.max(a,b) + .05) / (Math.min(a,b) + .05);
  });
  expect(ratio, 'real computed contrast').toBeGreaterThanOrEqual(4.5);
  await page.locator('.saas-card').getByRole('button', { name: 'Actualizar', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Plan Base' })).toBeVisible();
  let focused = false;
  for (let i = 0; i < 12; i++) {
    await page.keyboard.press('Tab');
    if (await input.evaluate(e => document.activeElement === e)) { focused = true; break; }
  }
  expect(focused, 'actual Tab reaches labeled input').toBe(true);
});
test('C04-01.M04.05 synthetic denied tenant and permission do not leak plan', async ({ page }) => {
  await uiFixture(page, false, true);
  await page.goto('/configuracion');
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByText('Plan Base')).toHaveCount(0);
  await uiFixture(page, true, false);
  await page.goto('/configuracion');
  await expect(page).not.toHaveURL(/\/configuracion(?:\?|$)/);
  await expect(page.getByText('Plan Base')).toHaveCount(0);
});
test('C04-01.M04.05 filter remains read-only', async ({ page }) => {
  const writes: string[] = [];
  page.on('request', req => {
    if (!['GET','HEAD','OPTIONS'].includes(req.method())) writes.push(req.method());
  });
  await openPage(page);
  await page.getByRole('textbox', { name: 'Clave' }).fill('USUARIOS');
  await page.getByRole('button', { name: 'Filtrar' }).click();
  await expect(page.getByText('USUARIOS', { exact: true }).last()).toBeVisible();
  expect(writes).toEqual([]);
});
