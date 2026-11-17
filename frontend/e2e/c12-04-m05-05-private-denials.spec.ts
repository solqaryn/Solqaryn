import { expect, test, type Page } from '@playwright/test';

// C12-04.M05.05: Angular real con sesion HTTP sintética, NO JWT/MySQL/restore real.
const sesion = /\/plataforma\/sesion(?:\?|$)/;
const headers = {
  'Access-Control-Allow-Origin': 'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods': 'GET, OPTIONS'
};
const global = { nombreUsuario:'Operador', rolNombre:'Administrador', ambito:'Plataforma', esAdministrador:true };

async function tokenSintetico(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token','c1204-m0505-sintetico');
    localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
  });
}
async function preparar(page: Page, respuestas: { status:number; data?:object; secreto?:string }[]) {
  let gets=0;
  const writes: string[]=[];
  page.on('request',r => {
    if (!['GET','HEAD','OPTIONS'].includes(r.method())) writes.push(r.method());
  });
  await page.route(sesion,async route => {
    if (route.request().method()==='OPTIONS') {
      await route.fulfill({status:204,headers}); return;
    }
    const r=respuestas[Math.min(gets,respuestas.length-1)];
    gets++;
    await route.fulfill({
      status:r.status,headers,contentType:'application/json',
      body:JSON.stringify({
        success:r.status===200, data:r.data??null,
        message:r.secreto??'PRIVATE_DSN_NEVER_RENDER_C1204'
      })
    });
  });
  return {gets:()=>gets,writes};
}
async function denegado(page: Page,writes: string[]) {
  await expect(page).toHaveURL(/\/login/);
  await expect(page.locator('[data-testid=c1204-cutover-private]')).toHaveCount(0);
  await expect(page.locator('[data-testid=c1204-m0403-branding]')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_DSN_NEVER_RENDER_C1204')).toHaveCount(0);
  expect(writes).toEqual([]);
}

test.describe('C12-04.M05.05 rechazos de cutover privado',()=>{
  test.describe.configure({retries:0});
  test('anonimo redirigido sin GET ni controles',async({page})=>{
    const x=await preparar(page,[{status:200,data:global}]);
    await page.goto('/plataforma/mysql-cutover-readonly-v5');
    await denegado(page,x.writes);
    expect(x.gets()).toBe(0);
  });
  test('401 token sintetico deniega',async({page})=>{
    await tokenSintetico(page);
    const x=await preparar(page,[{status:401}]);
    await page.goto('/plataforma/mysql-cutover-readonly-v5');
    await denegado(page,x.writes);
    expect(x.gets()).toBe(1);
  });
  test('403 oculta secreto y redirige con denegacion',async({page})=>{
    await tokenSintetico(page);
    const x=await preparar(page,[{status:403,secreto:'PRIVATE_DSN_NEVER_RENDER_C1204'}]);
    await page.goto('/plataforma/mysql-cutover-readonly-v5');
    await denegado(page,x.writes);
    await expect(page).toHaveURL(/plataforma=denegado/);
    expect(x.gets()).toBe(1);
  });
  test('200 empresa administradora no es plataforma',async({page})=>{
    await tokenSintetico(page);
    const x=await preparar(page,[{status:200,data:{...global,ambito:'Empresa'}}]);
    await page.goto('/plataforma/mysql-cutover-readonly-v5');
    await denegado(page,x.writes);
    expect(x.gets()).toBe(1);
  });
  test('200 plataforma no admin deniega',async({page})=>{
    await tokenSintetico(page);
    const x=await preparar(page,[{status:200,data:{...global,esAdministrador:false}}]);
    await page.goto('/plataforma/mysql-cutover-readonly-v5');
    await denegado(page,x.writes);
    expect(x.gets()).toBe(1);
  });
  test('200 identidad vacia deniega',async({page})=>{
    await tokenSintetico(page);
    const x=await preparar(page,[{status:200,data:{...global,nombreUsuario:'   '}}]);
    await page.goto('/plataforma/mysql-cutover-readonly-v5');
    await denegado(page,x.writes);
    expect(x.gets()).toBe(1);
  });
  test('503 revalidando oculta contenido y no escribe',async({page})=>{
    await tokenSintetico(page);
    const x=await preparar(page,[
      {status:200,data:global},
      {status:503,secreto:'PRIVATE_DSN_NEVER_RENDER_C1204'}
    ]);
    await page.goto('/plataforma/mysql-cutover-readonly-v5');
    await expect(page.locator('.controles li')).toHaveCount(4);
    await page.getByRole('button',{name:'Revalidar sesión de plataforma'}).click();
    await expect(page.locator('[data-testid=c1204-m0402-state]')).toContainText('Error');
    await expect(page.locator('.controles li')).toHaveCount(0);
    await expect(page.locator('[data-testid=c1204-m0403-branding]')).toHaveCount(0);
    await expect(page.getByText('PRIVATE_DSN_NEVER_RENDER_C1204')).toHaveCount(0);
    expect(x.gets()).toBe(2);
    expect(x.writes).toEqual([]);
  });
});
