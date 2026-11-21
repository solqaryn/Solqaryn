import { expect, test, type Page } from '@playwright/test';

// Angular/Chromium real. API, tenant, RBAC and JWT fixtures synthetic, never backend certification.
const cors = { 'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS' };
interface Options { tenant?: boolean; role?: boolean; create?: boolean; failFirst?: boolean; }
async function fixture(page: Page, options: Options = {}) {
  const p = { tenant:true, role:true, create:false, failFirst:false, ...options };
  let reads=0;
  const writes:string[]=[];
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token','SYNTHETIC_NOT_SIGNED_JWT');
    localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id','71');
  });
  page.on('request', r => {
    const u=new URL(r.url());
    if ((u.pathname.includes('/api/') || ['5000','5001','5005'].includes(u.port))
        && !['GET','HEAD','OPTIONS'].includes(r.method())) writes.push(r.method()+' '+u.pathname);
  });
  await page.route('**/*', async route => {
    const r=route.request(), u=new URL(r.url()), path=u.pathname;
    if (r.resourceType()==='document') return route.continue();
    if (r.method()==='OPTIONS') return route.fulfill({status:204,headers:cors});
    let status=200, data:unknown=null;
    if (/\/tenant-context\/71$/.test(path)) {
      if (!p.tenant) status=403;
      else data={ usuarioId:7,empresaId:71,rolId:3,rolNombre:'Lector',esAdministrador:false };
    } else if (/\/permisos\/mis-permisos\/empresa\/71$/.test(path)) {
      const perms=p.role?['Productos:Ver',...(p.create?['Productos:Crear']:[])]:[];
      data={ esAdministrador:false, operacionMultiEmpresaRestringida:true,
        permisos:perms,capacidadesHabilitadas:perms,rutasHabilitadas:p.role?['productos','productos/nuevo']:[] };
    } else if (/\/empresa-configuracion$/.test(path)) {
      data={ id:71,nombreComercial:'Catálogo de prueba',nombreVisibleSistema:'SOLQARYN',
        moneda:'HNL',zonaHoraria:'America/Tegucigalpa',formatoFecha:'dd/MM/yyyy' };
    } else if (/\/tema-visual$/.test(path)) data={};
    else if (/\/productos(?:\/|$)/.test(path)) {
      reads++;
      if (r.method()!=='GET') status=405;
      else if (p.failFirst && reads===1) status=503;
      else data={items:[],totalCount:0,page:1,pageSize:10,totalPages:0};
    } else if (/\/(categorias|colores|tallas|marcas|modelos)(?:\/|$)/.test(path)) data=[];
    else if (path.includes('/api/') || ['5000','5001','5005'].includes(u.port)) status=404;
    else return route.continue();
    await route.fulfill({status,headers:cors,contentType:'application/json',
      body:JSON.stringify(status===200?{success:true,data}:{success:false,message:'PRIVATE_INTERNAL_TENANT_DETAIL'})});
  });
  return {reads:()=>reads,writes};
}
test('C07-01.M04.04 búsqueda y doble limpieza sin escribir datos',async({page})=>{
  const io=await fixture(page);
  await page.goto('/productos');
  await expect(page.getByRole('heading',{name:'Productos',level:1})).toBeVisible();
  const search=page.getByRole('textbox',{name:'Buscar productos'});
  const before=io.reads();
  await search.fill('doble accion segura');
  await expect.poll(io.reads).toBeGreaterThan(before);
  const clear=page.getByRole('button',{name:'Limpiar filtros'});
  await clear.click();
  await clear.click();
  await expect(search).toHaveValue('');
  await expect(page).toHaveURL(/\/productos(?:\?|$)/);
  expect(io.writes).toEqual([]);
});
test('C07-01.M04.04 navegación a crear requiere permiso y no hace POST',async({page})=>{
  const io=await fixture(page,{create:true});
  await page.goto('/productos');
  await expect(page.getByRole('link',{name:'Nuevo producto'})).toBeVisible();
  await page.getByRole('link',{name:'Nuevo producto'}).click();
  await expect(page).toHaveURL(/\/productos\/nuevo(?:\?|$)/);
  expect(io.writes).toEqual([]);
});
test('C07-01.M04.04 503 no filtra detalle privado y reintenta',async({page})=>{
  const io=await fixture(page,{failFirst:true});
  await page.goto('/productos');
  await expect(page.getByRole('heading',{name:'Productos',level:1})).toBeVisible();
  await expect(page.getByText('No se encontraron productos con los filtros seleccionados.').first()).toBeVisible();
  await expect(page.getByText('PRIVATE_INTERNAL_TENANT_DETAIL')).toHaveCount(0);
  const before=io.reads();
  await page.getByRole('button',{name:'Limpiar filtros'}).click();
  await expect.poll(io.reads).toBeGreaterThan(before);
  expect(io.writes).toEqual([]);
});
test('C07-01.M04.04 anónimo, tenant y rol denegados bloquean formulario',async({context})=>{
  const anonymous=await context.newPage();
  await anonymous.goto('/productos');
  await expect(anonymous).toHaveURL(/\/login(?:\?|$)/);
  await expect(anonymous.getByRole('link',{name:'Nuevo producto'})).toHaveCount(0);
  await anonymous.close();
  for(const options of [{tenant:false},{role:false}]){
    const page=await context.newPage();
    const io=await fixture(page,options);
    await page.goto('/productos');
    await expect(page).not.toHaveURL(/\/productos(?:\?|$)/);
    await expect(page.getByRole('link',{name:'Nuevo producto'})).toHaveCount(0);
    expect(io.writes).toEqual([]);
    await page.close();
  }
});
