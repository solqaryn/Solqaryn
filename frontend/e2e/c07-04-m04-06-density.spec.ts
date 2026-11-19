import { expect, test, type Page, type Route } from '@playwright/test';

// Pruebas browser reales con respuestas simuladas. No certifican JWT firmado ni MySQL.
const headers = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
const vacio = { success: true, data: {
  items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0
}};
async function ok(route: Route) {
  await route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(vacio)});
}
async function contexto(page: Page, pedidos: (route: Route) => Promise<void>) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token','c0704-m0406-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id','71');
  });
  await page.route(/\/tenant-context\/71(?:\?|$)/, async route => {
    if(route.request().method()==='OPTIONS')return route.fulfill({status:204,headers});
    await route.fulfill({status:200,headers,contentType:'application/json',
      body:JSON.stringify({success:true,data:{
        usuarioId:7,empresaId:71,rolId:3,rolNombre:'Lectura',esAdministrador:false
      }})});
  });
  await page.route(/\/permisos\/mis-permisos\/empresa\/71(?:\?|$)/, async route => {
    if(route.request().method()==='OPTIONS')return route.fulfill({status:204,headers});
    await route.fulfill({status:200,headers,contentType:'application/json',
      body:JSON.stringify({success:true,data:{
        permisos:['Ventas:Ver'],capacidadesHabilitadas:['Ventas:Ver'],
        rutasHabilitadas:['pedidos-venta'],operacionMultiEmpresaRestringida:true,esAdministrador:false
      }})});
  });
  await page.route(/\/pedidos-venta(?:\?|$)/, async route => {
    if(route.request().resourceType()==='document')return route.continue();
    if(route.request().method()==='OPTIONS')return route.fulfill({status:204,headers});
    if(route.request().method()!=='GET')return route.abort();
    await pedidos(route);
  });
}

test('C07-04.M04.06 densidad: 320/768/1280 y scroll contenido', async ({page}) => {
  await contexto(page,ok);
  for(const width of [320,768,1280]) {
    await page.setViewportSize({width,height:800});
    await page.goto('/pedidos-venta');
    const panel=page.locator('.pedidos-readiness-v5');
    await expect(panel).toBeVisible();
    await expect(panel.locator('ol > li')).toHaveCount(4);
    const layout=await page.evaluate(()=>{
      const grid=document.querySelector('.pedidos-readiness-cards');
      const table=document.querySelector('.table');
      if(!grid||!table)throw Error('No existe superficie para medir densidad');
      return {
        overflow:document.documentElement.scrollWidth-document.documentElement.clientWidth,
        columns:getComputedStyle(grid).gridTemplateColumns.trim().split(/\s+/).length,
        localScroll:table.scrollWidth>=table.clientWidth
      };
    });
    expect(layout.overflow,'overflow global '+width).toBeLessThanOrEqual(2);
    expect(layout.columns,'columnas '+width).toBe(width<=704?1:2);
    expect(layout.localScroll).toBe(true);
  }
});

test('C07-04.M04.06 carga progresiva: spinner visible mientras GET retenido', async ({page}) => {
  let release!:()=>void;
  const gate=new Promise<void>(resolve=>{release=resolve;});
  let calls=0;
  await contexto(page,async route=>{calls++;await gate;await ok(route);});
  await page.goto('/pedidos-venta',{waitUntil:'domcontentloaded'});
  await expect(page.locator('mat-spinner')).toBeVisible();
  expect(calls).toBeGreaterThanOrEqual(1);
  release();
  await expect(page.getByText('Sin resultados.')).toBeVisible();
  await expect(page.locator('mat-spinner')).toHaveCount(0);
});

test('C07-04.M04.06 negativo 503 y reintento recobran estado sin POST', async ({page}) => {
  let calls=0;
  await contexto(page,async route=>{
    calls++;
    if(calls===1) {
      await route.fulfill({status:503,headers,contentType:'application/json',
        body:JSON.stringify({success:false,message:'Temporalmente indisponible'})});
    } else await ok(route);
  });
  await page.goto('/pedidos-venta');
  await expect(page.getByRole('alert')).toContainText('No fue posible cargar los pedidos');
  await page.getByRole('button',{name:'Reintentar'}).click();
  await expect(page.getByText('Sin resultados.')).toBeVisible();
  expect(calls).toBe(2);
});