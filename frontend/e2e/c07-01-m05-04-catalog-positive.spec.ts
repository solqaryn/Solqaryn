import { expect, test, type Page } from '@playwright/test';

// C07-01.M05.04: real Angular/Chromium positive catalog acceptance, synthetic API and JWT.
// No actual backend HTTP, database, signed token or tenant authorization certified.
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
const item = {
  id:31, nombre:'Taladro profesional', marca:'Marca A', modelo:'Modelo B',
  categoriaId:5, categoriaNombre:'Herramientas',
  cantidad:12, costo:30.00, precio:99.95, precioMinimo:99.95, precioMaximo:99.95,
  umbralStockBajo:2, tieneStockBajo:false, estaAgotado:false,
  estadoInventario:'Disponible', activo:true, imagenes:[], totalImagenes:0,
  variantes:[], totalVariantes:0, usaVariantes:false,
  fechaCreacion:'2026-11-21T00:00:00Z', fechaActualizacion:'2026-11-21T00:00:00Z'
};
async function setup(page:Page) {
  const queries:string[] = [], writes:string[]=[];
  await page.addInitScript(()=>{
    localStorage.setItem('solqaryn_token','SYNTHETIC_C0701_M0504_NOT_SIGNED_JWT');
    localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id','71');
  });
  page.on('request',request=>{
    const url=new URL(request.url());
    if((url.pathname.startsWith('/api/') || ['5000','5001','5005'].includes(url.port)) &&
       !['GET','HEAD','OPTIONS'].includes(request.method()))
      writes.push(request.method()+' '+url.pathname);
  });
  await page.route('**/*',async route=>{
    const req=route.request(),url=new URL(req.url()),path=url.pathname;
    if(req.resourceType()==='document')return route.continue();
    if(req.method()==='OPTIONS')return route.fulfill({status:204,headers:cors});
    if(!path.startsWith('/api/')&&!['5000','5001','5005'].includes(url.port))
      return route.continue();
    let data:unknown=null,status=200;
    if(path.endsWith('/tenant-context/empresas'))data=[{empresaId:71,nombre:'Comercio A'}];
    else if(path.endsWith('/tenant-context/71'))
      data={usuarioId:7,empresaId:71,rolId:3,rolNombre:'Lector',esAdministrador:false};
    else if(path.endsWith('/permisos/mis-permisos/empresa/71'))
      data={esAdministrador:false,operacionMultiEmpresaRestringida:true,
        permisos:['Productos:Ver'],capacidadesHabilitadas:['Productos:Ver'],rutasHabilitadas:['productos']};
    else if(path.endsWith('/empresa-configuracion/tenant/71'))
      data={empresaId:71,nombre:'Comercio A',moneda:'HNL',
        zonaHoraria:'America/Tegucigalpa',impuestosJson:'{}',emisionJson:'{}',
        correoConfigurado:false,version:1,plantillasCorreo:[]};
    else if(path.endsWith('/empresa-configuracion/publica')||path.endsWith('/empresa-configuracion'))
      data={id:0,nombreVisibleSistema:'SOLQARYN',nombreComercial:'SOLQARYN'};
    else if(path.endsWith('/tema-visual'))data={};
    else if(path.endsWith('/productos')){
      if(req.method()!=='GET')status=405;
      else{
        queries.push(url.search);
        data={items:[item],totalCount:1,page:1,pageSize:10,totalPages:1};
      }
    }else if(/\/(categorias|colores|tallas|marcas|modelos)(?:\/|$)/.test(path))data=[];
    else status=404;
    await route.fulfill({status,headers:cors,contentType:'application/json',
      body:JSON.stringify(status===200?{success:true,data}:{success:false,message:'DENEGADO_SINTETICO'})});
  });
  return {queries,writes};
}

test('C07-01.M05.04 desktop accepts real product card totals and HNL prices read-only',async({page})=>{
  await page.setViewportSize({width:1280,height:800});
  const io=await setup(page);
  await page.goto('/productos');
  await expect(page.getByRole('heading',{name:'Productos',level:1})).toBeVisible();
  await expect(page.locator('.catalog-success')).toContainText('Catálogo cargado: 1 productos.');
  const row=page.locator('.table-desktop tbody tr');
  await expect(row).toHaveCount(1);
  await expect(row.locator('.product-name')).toHaveText('Taladro profesional');
  await expect(row).toContainText('Herramientas');
  await expect(row).toContainText('99.95');
  await expect(page.getByText('Modo consulta: no tienes permisos para modificar productos.')).toBeVisible();
  expect(new URLSearchParams(io.queries[0]).get('page')).toBe('1');
  expect(new URLSearchParams(io.queries[0]).get('pageSize')).toBe('10');
  expect(io.writes).toEqual([]);
});

test('C07-01.M05.04 positive product mobile card survives 375-to-1280 layout',async({page})=>{
  await page.setViewportSize({width:375,height:812});
  const io=await setup(page);
  await page.goto('/productos');
  await expect(page.locator('.catalog-success')).toContainText('Catálogo cargado: 1 productos.');
  await expect(page.locator('.cards-mobile .producto-card')).toHaveCount(1);
  await expect(page.locator('.cards-mobile .producto-card')).toContainText('Taladro profesional');
  await expect(page.locator('.table-scroll')).toBeHidden();
  const overflow=await page.evaluate(()=>document.documentElement.scrollWidth-window.innerWidth);
  expect(overflow).toBeLessThanOrEqual(2);
  await page.setViewportSize({width:1280,height:800});
  await expect(page.locator('.table-desktop tbody tr')).toHaveCount(1);
  await expect(page.locator('.table-scroll')).toBeVisible();
  expect(io.writes).toEqual([]);
});

test('C07-01.M05.04 user search and filter reset keep product and readonly GET',async({page})=>{
  const io=await setup(page);
  await page.goto('/productos');
  await expect(page.locator('.catalog-success')).toContainText('Catálogo cargado: 1 productos.');
  const search=page.getByRole('textbox',{name:'Buscar productos'});
  await search.fill('Taladro');
  await expect.poll(()=>io.queries.length).toBeGreaterThan(1);
  await expect(page.locator('.catalog-success')).toContainText('Catálogo cargado: 1 productos.');
  expect(io.queries.some(q=>new URLSearchParams(q).get('search')==='Taladro')).toBe(true);
  await page.getByRole('button',{name:'Limpiar filtros'}).click();
  await expect(search).toHaveValue('');
  await expect(page.locator('.table-desktop tbody tr')).toHaveCount(1);
  expect(io.writes).toEqual([]);
});
