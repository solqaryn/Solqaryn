import { expect, test, type Page } from '@playwright/test';

// JWT/tenant/HTTP son sinteticos y NO prueban acceso real ni operaciones financieras.
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
async function escenario(page: Page, permitir = true) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c0704-branding-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', '71');
  });
  await page.route(/\/tenant-context\/71(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({status:204,headers:cors});
    return route.fulfill({status:200,headers:cors,contentType:'application/json',body:JSON.stringify({
      success:true,data:{usuarioId:7,empresaId:71,rolId:3,rolNombre:'Lectura',esAdministrador:false}
    })});
  });
  await page.route(/\/permisos\/mis-permisos\/empresa\/71(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') return route.fulfill({status:204,headers:cors});
    return route.fulfill({status:200,headers:cors,contentType:'application/json',body:JSON.stringify({
      success:true,data:{permisos:permitir?['Ventas:Ver']:[],
        capacidadesHabilitadas:permitir?['Ventas:Ver']:[],
        rutasHabilitadas:permitir?['pedidos-venta']:[],
        operacionMultiEmpresaRestringida:true,esAdministrador:false}
    })});
  });
  await page.route(/\/pedidos-venta(?:\?|$)/, async route => {
    if (route.request().resourceType() === 'document') return route.continue();
    if (route.request().method() === 'OPTIONS') return route.fulfill({status:204,headers:cors});
    if (route.request().method() !== 'GET') return route.abort();
    return route.fulfill({status:200,headers:cors,contentType:'application/json',body:JSON.stringify({
      success:true,data:{items:[],page:1,pageSize:10,totalCount:0,totalPages:0}
    })});
  });
}

test('C07-04.M04.03 anónimo no ve controles de marca', async ({page}) => {
  await page.goto('/pedidos-venta');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.pedidos-branding-v5')).toHaveCount(0);
});
test('C07-04.M04.03 privado: Signals, contraste y densidad a 320, sin escrituras', async ({page}) => {
  await escenario(page,true);
  await page.setViewportSize({width:320,height:740});
  const writes: string[] = [];
  page.on('request', req => {
    if (/\/pedidos-venta(?:\/|\?|$)/.test(req.url()) && !['GET','OPTIONS'].includes(req.method()))
      writes.push(req.method());
  });
  await page.goto('/pedidos-venta');
  const p = page.locator('.pedidos-branding-v5');
  await expect(p).toBeVisible();
  await expect(p).toHaveAttribute('data-densidad','comoda');
  await p.getByRole('button',{name:'Vista compacta'}).click();
  await expect(p).toHaveAttribute('data-densidad','compacta');
  await p.getByRole('button',{name:'Contraste alto'}).click();
  await expect(p).toHaveAttribute('data-contraste','alto');
  expect(await p.locator('button').count()).toBe(2);
  expect(await page.evaluate(() => document.documentElement.scrollWidth -
    document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
  expect(writes).toEqual([]);
});
test('C07-04.M04.03 denegación Ventas:Ver no exhibe branding ni botones', async ({page}) => {
  await escenario(page,false);
  await page.goto('/pedidos-venta');
  await expect(page).not.toHaveURL(/\/pedidos-venta(?:\?|$)/);
  await expect(page.locator('.pedidos-branding-v5')).toHaveCount(0);
});
