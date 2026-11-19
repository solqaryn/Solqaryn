import { expect, test, type Page } from '@playwright/test';

// Contrato de aceptación UI: HTTP/JWT/tenant sintéticos, NO finanzas ni persistencia reales.
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
const item = (id:number) => ({
  id, cotizacionId:12, clienteId:9, clienteNombreSnapshot:'Cliente de prueba',
  clienteDocumentoSnapshot:'DOC-TEST', estado:2, total:100.5,
  detalles:[{id:70,productoId:11,productoVarianteId:21,
    productoNombreSnapshot:'Articulo de prueba',cantidad:2,precioUnitario:50.25,total:100.5}]
});
async function escenario(page:Page, autorizar=true) {
  const mutaciones:string[] = [];
  page.on('request',req=>{
    const path=new URL(req.url()).pathname;
    if(/\/pedidos-venta(?:\/\d+)?$/.test(path) &&
       !['GET','OPTIONS'].includes(req.method()))mutaciones.push(req.method());
  });
  await page.addInitScript(()=>{
    localStorage.setItem('solqaryn_token','c0704-m0504-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id','71');
  });
  await page.route(/\/tenant-context\/71(?:\?|$)/,async route=>{
    if(route.request().method()==='OPTIONS')return route.fulfill({status:204,headers:cors});
    await route.fulfill({status:200,headers:cors,contentType:'application/json',
      body:JSON.stringify({success:true,data:{
        usuarioId:7,empresaId:71,rolId:3,rolNombre:'Lectura',esAdministrador:false
      }})});
  });
  await page.route(/\/permisos\/mis-permisos\/empresa\/71(?:\?|$)/,async route=>{
    if(route.request().method()==='OPTIONS')return route.fulfill({status:204,headers:cors});
    const permisos=autorizar?['Ventas:Ver']:[];
    await route.fulfill({status:200,headers:cors,contentType:'application/json',
      body:JSON.stringify({success:true,data:{
        permisos,capacidadesHabilitadas:permisos,
        rutasHabilitadas:autorizar?['pedidos-venta']:[],
        operacionMultiEmpresaRestringida:true,esAdministrador:false
      }})});
  });
  await page.route(/\/pedidos-venta(?:\/47)?(?:\?|$)/,async route=>{
    if(route.request().resourceType()==='document')return route.continue();
    if(route.request().method()==='OPTIONS')return route.fulfill({status:204,headers:cors});
    if(route.request().method()!=='GET')return route.abort();
    const url=new URL(route.request().url());
    const data=url.pathname.endsWith('/pedidos-venta/47')?item(47):(()=>{
      const pageNumber=Number(url.searchParams.get('page')||1);
      return {items:[item(pageNumber===2?48:47)],page:pageNumber,
        pageSize:10,totalCount:15,totalPages:2};
    })();
    await route.fulfill({status:200,headers:cors,contentType:'application/json',
      body:JSON.stringify({success:true,data})});
  });
  return mutaciones;
}

test('C07-04.M05.04 aceptacion: listado y paginacion muestran IDs reales del contrato',async({page})=>{
  const writes=await escenario(page);
  await page.goto('/pedidos-venta');
  await expect(page.locator('table tbody tr')).toHaveCount(1);
  await expect(page.getByText('PED-47')).toBeVisible();
  await page.getByRole('button',{name:'Siguiente'}).click();
  await expect(page.getByText('PED-48')).toBeVisible();
  await expect(page.getByText('PED-47')).toHaveCount(0);
  await page.getByRole('button',{name:'Anterior'}).click();
  await expect(page.getByText('PED-47')).toBeVisible();
  expect(writes).toEqual([]);
});

test('C07-04.M05.04 aceptacion: lector ve detalle sin ejecutar acciones financieras',async({page})=>{
  const writes=await escenario(page);
  await page.setViewportSize({width:320,height:800});
  await page.goto('/pedidos-venta');
  await page.getByRole('link',{name:'Ver pedido'}).click();
  await expect(page).toHaveURL(/\/pedidos-venta\/47$/);
  await expect(page.getByRole('heading',{name:'Pedido PED-47'})).toBeVisible();
  await expect(page.getByText('COT-12')).toBeVisible();
  await expect(page.getByText('Articulo de prueba')).toBeVisible();
  await expect(page.getByRole('button',{name:'Confirmar y reservar'})).toHaveCount(0);
  await expect(page.getByRole('button',{name:'Anular'})).toHaveCount(0);
  expect(writes).toEqual([]);
});

test('C07-04.M05.04 frontera: falta Ventas:Ver no habilita listado privado',async({page})=>{
  const writes=await escenario(page,false);
  await page.goto('/pedidos-venta');
  await expect(page).not.toHaveURL(/\/pedidos-venta(?:\?|$)/);
  await expect(page.getByText('PED-47')).toHaveCount(0);
  expect(writes).toEqual([]);
});
