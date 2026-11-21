import { expect, test, type Page } from '@playwright/test';

// V22 M05.06: real Chromium UI; tenant API/JWT are synthetic, not a live auth proof.
const cors = { 'Access-Control-Allow-Origin':'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers':'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods':'GET, OPTIONS' };
const product={id:31,nombre:'Taladro profesional',marca:'Marca A',modelo:'Modelo B',
  categoriaId:5,categoriaNombre:'Herramientas',cantidad:12,costo:30,precio:99.95,
  precioMinimo:99.95,precioMaximo:99.95,umbralStockBajo:2,tieneStockBajo:false,
  estaAgotado:false,estadoInventario:'Disponible',activo:true,imagenes:[],
  totalImagenes:0,variantes:[],totalVariantes:0,usaVariantes:false,
  fechaCreacion:'2026-11-21T00:00:00Z',fechaActualizacion:'2026-11-21T00:00:00Z'};
const good={items:[product],totalCount:1,page:1,pageSize:10,totalPages:1};
type Scenario={wait?:()=>Promise<void>,products?:(n:number)=>unknown};
async function setup(page:Page,scenario:Scenario={}){
  const queries:string[]=[],writes:string[]=[];
  await page.addInitScript(()=>{
    localStorage.setItem('solqaryn_token','M0506_SYNTHETIC_NOT_SIGNED_JWT');
    localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id','71');
  });
  page.on('request',req=>{
    const u=new URL(req.url());
    if((u.pathname.startsWith('/api/')||['5000','5001','5005'].includes(u.port))&&
      !['GET','HEAD','OPTIONS'].includes(req.method()))
      writes.push(req.method()+' '+u.pathname);
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
        zonaHoraria:'America/Tegucigalpa',impuestosJson:'{}',
        emisionJson:'{}',correoConfigurado:false,version:1,plantillasCorreo:[]};
    else if(path.endsWith('/empresa-configuracion/publica')||path.endsWith('/empresa-configuracion'))
      data={id:0,nombreVisibleSistema:'SOLQARYN',nombreComercial:'SOLQARYN'};
    else if(path.endsWith('/tema-visual'))data={};
    else if(path.endsWith('/productos')){
      if(req.method()!=='GET')status=405;
      else{queries.push(url.search);if(scenario.wait)await scenario.wait();
        data=scenario.products?.(queries.length)??good;}
    }else if(/\/(categorias|colores|tallas|marcas|modelos)(?:\/|$)/.test(path))data=[];
    else status=404;
    await route.fulfill({status,headers:cors,contentType:'application/json',
      body:JSON.stringify(status===200?{success:true,data}:{success:false,message:'DENEGADO_SINTETICO'})});
  });
  return {queries,writes};
}
test('M05.06 320/375/768/1280 no overflow or redundant initial request',async({page})=>{
  const io=await setup(page);await page.setViewportSize({width:320,height:840});
  await page.goto('/productos');
  await expect(page.locator('.catalog-success')).toContainText('Catálogo cargado: 1 productos.');
  for(const width of [320,375,768,1280]){
    await page.setViewportSize({width,height:840});
    const card=page.locator('.products-card');await expect(card).toBeVisible();
    const m=await card.evaluate(el=>({client:el.clientWidth,scroll:el.scrollWidth}));
    expect(m.scroll-m.client,'overflow '+width).toBeLessThanOrEqual(2);
    if(width<=768){await expect(page.locator('.cards-mobile .producto-card')).toHaveCount(1);
      await expect(page.locator('.table-scroll')).toBeHidden();}
    else{await expect(page.locator('.table-desktop tbody tr')).toHaveCount(1);
      await expect(page.locator('.table-scroll')).toBeVisible();}
  }
  expect(io.queries).toHaveLength(1);
  expect(new URLSearchParams(io.queries[0]).get('pageSize')).toBe('10');
  expect(io.writes).toEqual([]);
});
test('M05.06 real keyboard Tab reaches labeled search without .focus()',async({page})=>{
  const io=await setup(page);await page.goto('/productos');
  await expect(page.locator('.catalog-success')).toContainText('Catálogo cargado: 1 productos.');
  const search=page.getByRole('textbox',{name:'Buscar productos'});
  let reached=false;
  for(let i=0;i<130;i++){await page.keyboard.press('Tab');
    if(await search.evaluate(el=>document.activeElement===el)){reached=true;break;}}
  expect(reached).toBe(true);await expect(search).toBeFocused();
  expect(await search.evaluate(el=>el.matches(':focus-visible'))).toBe(true);
  await search.fill('Taladro');await expect.poll(()=>io.queries.length).toBeGreaterThan(1);
  expect(io.queries.some(q=>new URLSearchParams(q).get('search')==='Taladro')).toBe(true);
  expect(io.writes).toEqual([]);
});
test('M05.06 delayed GET shows honest live loading, never premature success',async({page})=>{
  let release!:()=>void;const gate=new Promise<void>(resolve=>{release=resolve;});
  const io=await setup(page,{wait:()=>gate});
  try{await page.goto('/productos');
    await expect(page.locator('.loading[role="status"]')).toContainText('Cargando productos...');
    await expect(page.locator('.catalog-success')).toHaveCount(0);}
  finally{release();}
  await expect(page.locator('.catalog-success')).toContainText('Catálogo cargado: 1 productos.');
  expect(io.queries).toHaveLength(1);expect(io.writes).toEqual([]);
});
test('M05.06 malformed HTTP 200 fails closed; retry only GET',async({page})=>{
  const io=await setup(page,{products:n=>n===1?{...good,totalCount:'INVALID'}:good});
  await page.goto('/productos');
  await expect(page.locator('.catalog-error[role="alert"]')).toContainText('No se pudo cargar el catálogo');
  await expect(page.locator('.catalog-success')).toHaveCount(0);
  await page.getByRole('button',{name:'Reintentar carga'}).click();
  await expect(page.locator('.catalog-success')).toContainText('Catálogo cargado: 1 productos.');
  expect(io.queries).toHaveLength(2);expect(io.writes).toEqual([]);
});
test('M05.06 contrast WCAG 4.5 and negative mutant',async({page})=>{
  await setup(page);await page.goto('/productos');
  await expect(page.getByRole('heading',{name:'Productos',level:1})).toBeVisible();
  const ratio=await page.locator('.page-title').evaluate(el=>{
    const rgb=(s:string)=>{const v=s.match(/[\d.]+/g)?.map(Number)??[];
      if(v.length<3)throw Error('Unmeasurable CSS');return [v[0],v[1],v[2],v[3]??1];};
    const lum=(v:number[])=>v.slice(0,3).map(c=>{const x=c/255;
      return x<=.04045?x/12.92:((x+.055)/1.055)**2.4;})
      .reduce((a,c,i)=>a+c*[.2126,.7152,.0722][i],0);
    const contrast=(a:number[],b:number[])=>
      (Math.max(lum(a),lum(b))+.05)/(Math.min(lum(a),lum(b))+.05);
    let bg=[255,255,255,1];
    for(let p:Element|null=el;p;p=p.parentElement){
      const c=rgb(getComputedStyle(p).backgroundColor);if(c[3]===1){bg=c;break;}}
    return{real:contrast(rgb(getComputedStyle(el).color),bg),
      bad:contrast([190,190,190],[255,255,255])};
  });
  expect(ratio.real).toBeGreaterThanOrEqual(4.5);
  expect(ratio.bad).toBeLessThan(4.5);
});
