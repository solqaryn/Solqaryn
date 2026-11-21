import { expect, test, type Page } from '@playwright/test';

// Real Angular 22 and Chromium. Stubbed API/JWT/tenancy below do not prove
// backend signed-JWT, MySQL isolation or live provider authorization.
const cors = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
interface Options {
  denied?: boolean;
  readOnly?: boolean;
  malformed?: boolean;
  failFirst?: boolean;
  slow?: boolean;
}
async function fixture(page: Page, o: Options = {}) {
  const writes: string[] = [];
  let calls = 0, resume!: () => void;
  const slowGate = new Promise<void>(resolve => { resume = resolve; });
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'C0701_SYNTHETIC_NOT_SIGNED_JWT');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
    localStorage.setItem('solqaryn_empresa_solicitada_id', '71');
  });
  page.on('request', request => {
    const u = new URL(request.url());
    if ((u.pathname.includes('/api/') || ['5000','5001','5005'].includes(u.port))
      && !['GET','OPTIONS','HEAD'].includes(request.method())) writes.push(request.method() + ' ' + u.pathname);
  });
  await page.route('**/*', async route => {
    const r=route.request(),u=new URL(r.url()),path=u.pathname;
    if (r.resourceType()==='document') return route.continue();
    if (r.method()==='OPTIONS') return route.fulfill({status:204,headers:cors});
    let data: unknown = null,status=200;
    if (path.endsWith('/tenant-context/71')) {
      if (o.denied) status=403;
      else data={usuarioId:7,empresaId:71,rolId:3,rolNombre:'Lector',esAdministrador:false};
    } else if (path.endsWith('/tenant-context/empresas')) {
      data=[{empresaId:71,nombre:'Comercio A'}];
    } else if (path.endsWith('/permisos/mis-permisos/empresa/71')) {
      const perms=o.denied?[]:o.readOnly?['Productos:Ver']:['Productos:Ver','Productos:Crear'];
      data={esAdministrador:false,operacionMultiEmpresaRestringida:true,
        permisos:perms,capacidadesHabilitadas:perms,rutasHabilitadas:o.denied?[]:['productos']};
    } else if (path.endsWith('/empresa-configuracion/tenant/71')) {
      data={empresaId:71,nombre:'Comercio A',moneda:'HNL',zonaHoraria:'America/Tegucigalpa',
        impuestosJson:'{}',emisionJson:'{}',correoConfigurado:false,version:1,plantillasCorreo:[]};
    } else if (path.endsWith('/tema-visual')) data={};
    else if (/\/productos(?:\/|$)/.test(path)) {
      if (r.method()!=='GET') status=405;
      else {
        calls++;
        if (o.slow && calls===1) await slowGate;
        if (o.failFirst && calls===1) status=503;
        else data=o.malformed?{items:null,totalCount:'NOT_INTEGER'}:
          {items:[],totalCount:0,page:1,pageSize:10,totalPages:0};
      }
    } else if (/\/(categorias|colores|tallas|marcas|modelos)(?:\/|$)/.test(path)) data=[];
    else if (path.includes('/api/')||['5000','5001','5005'].includes(u.port)) status=404;
    else return route.continue();
    await route.fulfill({status,headers:cors,contentType:'application/json',
      body:JSON.stringify(status===200?{success:true,data}:{success:false,message:'PRIVATE_STACK_SECRET'})});
  });
  return {writes,calls:()=>calls,resume};
}
test('C07-01.M04.02 loading visible until real Angular receives HTTP success', async ({page}) => {
  const io=await fixture(page,{slow:true});
  await page.goto('/productos');
  await expect(page.getByRole('status',{name:'Cargando productos'})).toBeVisible();
  await expect(page.getByText('Cargando productos...')).toBeVisible();
  io.resume();
  await expect(page.getByText('Catálogo cargado: 0 productos.')).toBeVisible();
  await expect(page.getByText('No se encontraron productos con los filtros seleccionados.').first()).toBeVisible();
  expect(io.writes).toEqual([]);
});
test('C07-01.M04.02 503 shows safe error, never misleading empty, retry succeeds', async ({page}) => {
  const io=await fixture(page,{failFirst:true});
  await page.goto('/productos');
  await expect(page.getByRole('alert')).toContainText('No se pudo cargar el catálogo');
  await expect(page.getByText('No se encontraron productos con los filtros seleccionados.')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_STACK_SECRET')).toHaveCount(0);
  await page.getByRole('button',{name:'Reintentar carga'}).click();
  await expect(page.getByText('Catálogo cargado: 0 productos.')).toBeVisible();
  expect(io.calls()).toBeGreaterThanOrEqual(2);
  expect(io.writes).toEqual([]);
});
test('C07-01.M04.02 malformed 200 pagination fails closed and does not show stale data', async ({page}) => {
  const io=await fixture(page,{malformed:true});
  await page.goto('/productos');
  await expect(page.getByRole('alert')).toContainText('No se pudo cargar el catálogo');
  await expect(page.locator('.table-desktop tbody tr')).toHaveCount(0);
  expect(io.writes).toEqual([]);
});
test('C07-01.M04.02 read-only permissions cannot grant mutation; tenant denial hides catalog', async ({context}) => {
  const readonly=await context.newPage();
  const io=await fixture(readonly,{readOnly:true});
  await readonly.goto('/productos');
  await expect(readonly.getByText('Modo consulta: no tienes permisos para modificar productos.')).toBeVisible();
  await expect(readonly.getByRole('link',{name:'Nuevo producto'})).toHaveCount(0);
  expect(io.writes).toEqual([]);
  await readonly.close();
  const denied=await context.newPage();
  const deniedIo=await fixture(denied,{denied:true});
  await denied.goto('/productos');
  await expect(denied).not.toHaveURL(/\/productos(?:\?|$)/);
  await expect(denied.locator('.catalog-success')).toHaveCount(0);
  expect(deniedIo.writes).toEqual([]);
  await denied.close();
});
