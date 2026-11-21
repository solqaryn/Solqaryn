import { expect, test, type Page } from '@playwright/test';
// Real Angular/Chromium; tenant API/JWT below are deliberately synthetic, not backend proof.
const cors = { 'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS' };
async function setup(page: Page, tenant = true, access = true) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'C0701_SYNTHETIC_NOT_SIGNED_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', '71');
  });
  await page.route('**/*', async route => {
    const r = route.request(), url = new URL(r.url()), path = url.pathname;
    if (r.resourceType() === 'document') return route.continue();
    if (r.method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });
    let data: unknown = null, status = 200;
    if (/\/tenant-context\/71$/.test(path)) {
      if (!tenant) status = 403;
      else data = { usuarioId: 7, empresaId: 71, rolId: 3, rolNombre: 'Lectura', esAdministrador: false };
    } else if (/\/permisos\/mis-permisos\/empresa\/71$/.test(path)) {
      data = { esAdministrador: false, operacionMultiEmpresaRestringida: true,
        permisos: access ? ['Productos:Ver'] : [],
        capacidadesHabilitadas: access ? ['Productos:Ver'] : [],
        rutasHabilitadas: access ? ['productos'] : [] };
    } else if (/\/empresa-configuracion$/.test(path)) {
      data = { id: 71, nombreComercial: 'Catalogo de prueba', nombreVisibleSistema: 'SOLQARYN',
        moneda: 'HNL', zonaHoraria: 'America/Tegucigalpa', formatoFecha: 'dd/MM/yyyy' };
    } else if (/\/tema-visual$/.test(path)) data = {};
    else if (/\/productos(?:\/|$)/.test(path)) {
      if (r.method() !== 'GET') status = 405;
      else data = { items: [], totalCount: 0, page: 1, pageSize: 10, totalPages: 0 };
    } else if (/\/(categorias|colores|tallas|marcas|modelos)(?:\/|$)/.test(path)) data = [];
    else if (path.includes('/api/') || ['5000','5001','5005'].includes(url.port)) status = 404;
    else return route.continue();
    await route.fulfill({ status, headers: cors, contentType: 'application/json',
      body: JSON.stringify(status === 200 ? { success: true, data } : { success: false, message: 'DENEGADO' }) });
  });
}
async function open(page: Page) {
  await setup(page);
  await page.goto('/productos');
  await expect(page.getByRole('heading', { name: 'Productos', level: 1 })).toBeVisible();
  await expect(page.getByRole('textbox', { name: 'Buscar productos' })).toBeVisible();
}
test('C07-01.M04.05 real Chromium responsive catalog', async ({ page }) => {
  await open(page);
  for (const width of [320,375,768,1280]) {
    await page.setViewportSize({ width, height: 860 });
    const card = page.locator('.products-card');
    await expect(card).toBeVisible();
    const m = await card.evaluate(el => ({ client: el.clientWidth, scroll: el.scrollWidth }));
    expect(m.scroll - m.client, 'overflow at ' + width).toBeLessThanOrEqual(2);
    if (width <= 768) {
      await expect(page.locator('.cards-mobile')).toBeVisible();
      await expect(page.locator('.table-scroll')).toBeHidden();
    } else await expect(page.locator('.table-scroll')).toBeVisible();
  }
});
test('C07-01.M04.05 real keyboard Tab and accessible label', async ({ page }) => {
  await open(page);
  const input = page.getByRole('textbox', { name: 'Buscar productos' });
  let hit = false;
  for (let i=0;i<130;i++) {
    await page.keyboard.press('Tab');
    if (await input.evaluate(el => document.activeElement === el)) { hit = true; break; }
  }
  expect(hit, 'Tab reaches labeled filter without direct focus').toBe(true);
  await expect(input).toBeFocused();
  expect(await input.evaluate(el => el.matches(':focus-visible'))).toBe(true);
});
test('C07-01.M04.05 computed WCAG contrast and negative mutant', async ({ page }) => {
  await open(page);
  const ratio = await page.locator('.page-title').evaluate(el => {
    const rgb = (s: string) => {
      const v = s.match(/[\d.]+/g)?.map(Number) ?? [];
      if (v.length < 3) throw Error('Unmeasurable CSS');
      return [v[0],v[1],v[2],v[3] ?? 1];
    };
    const lum = (v: number[]) => v.slice(0,3).map(c => {
      const x=c/255; return x <= .04045 ? x/12.92 : ((x+.055)/1.055)**2.4;
    }).reduce((a,c,i)=>a+c*[.2126,.7152,.0722][i],0);
    const calc = (a: number[], b: number[]) =>
      (Math.max(lum(a),lum(b))+.05)/(Math.min(lum(a),lum(b))+.05);
    let bg=[255,255,255,1];
    for (let p: Element|null=el;p;p=p.parentElement) {
      const c=rgb(getComputedStyle(p).backgroundColor);
      if (c[3]===1) { bg=c; break; }
    }
    return { real:calc(rgb(getComputedStyle(el).color),bg), bad:calc([190,190,190],[255,255,255]) };
  });
  expect(ratio.real).toBeGreaterThanOrEqual(4.5);
  expect(ratio.bad,'mutant contrast must be rejected').toBeLessThan(4.5);
});
test('C07-01.M04.05 no catalog for anonymous, denied tenant or denied role', async ({ page }) => {
  await page.goto('/productos');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.getByRole('heading',{name:'Productos',level:1})).toHaveCount(0);
  await setup(page,false,true);
  await page.goto('/productos');
  await expect(page).not.toHaveURL(/\/productos(?:\?|$)/);
  await setup(page,true,false);
  await page.goto('/productos');
  await expect(page).not.toHaveURL(/\/productos(?:\?|$)/);
});
