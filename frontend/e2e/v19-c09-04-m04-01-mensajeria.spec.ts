import { expect, test, type Page } from '@playwright/test';

// Browser real con sesión HTTP SIMULADA, no JWT, SMTP, WhatsApp ni MySQL.
const url='/plataforma/mensajeria-multicanal-v19';
const cors={'Access-Control-Allow-Origin':'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers':'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods':'GET, OPTIONS'};
async function sesion(page:Page, status=200, data?:object) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token','c0904-test-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if(route.request().method()==='OPTIONS') {await route.fulfill({status:204,headers:cors});return;}
    await route.fulfill({status,headers:cors,contentType:'application/json',body:JSON.stringify(
      status===200?{success:true,data:data??{nombreUsuario:'Mock',rolNombre:'Admin',ambito:'Plataforma',esAdministrador:true}}
      :{success:false,message:'SECRET_TENANT_B_NO_LEAK'})});
  });
}
test('anónimo y 403 sintético quedan fuera, sin filtrar secreto',async({page})=>{
  await page.goto(url);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByTestId('c0904-canales-privados')).toHaveCount(0);
  await sesion(page,403);
  await page.goto(url);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByText('SECRET_TENANT_B_NO_LEAK')).toHaveCount(0);
});
test('empresa y plataforma no-admin denegadas aunque haya token sintético',async({browser})=>{
  for(const data of [{nombreUsuario:'A',rolNombre:'Admin',ambito:'Empresa',esAdministrador:true},
    {nombreUsuario:'B',rolNombre:'Operador',ambito:'Plataforma',esAdministrador:false}]){
    const page=await browser.newPage();
    await sesion(page,200,data);
    await page.goto(url);
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByTestId('c0904-canales-privados')).toHaveCount(0);
    await page.close();
  }
});
test('admin simulado solo lectura sin escrituras, canales ni endpoints nuevos',async({page})=>{
  await sesion(page);
  const writes:string[]=[];
  page.on('request',r=>{if(!['GET','HEAD','OPTIONS'].includes(r.method()))writes.push(r.method());});
  await page.goto(url);
  await expect(page.getByTestId('c0904-canales-privados').locator('li')).toHaveCount(3);
  await expect(page.getByText('NO OPERATIVO')).toBeVisible();
  await expect(page.locator('.mensajeria form,.mensajeria input,.mensajeria button,.mensajeria textarea')).toHaveCount(0);
  expect(writes).toEqual([]);
});
test('320 a 1280 sin overflow y enlace accesible por Tab real desde inicio',async({page})=>{
  await sesion(page);
  for(const width of [320,375,768,1280]){
    await page.setViewportSize({width,height:812});await page.goto(url);
    await expect(page.getByTestId('c0904-canales-privados')).toBeVisible();
    expect(await page.evaluate(()=>document.documentElement.scrollWidth-document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
  }
  const volver=page.getByRole('link',{name:'Regresar a plataforma'});
  let alcanzado=false;
  for(let i=0;i<80;i++){await page.keyboard.press('Tab');if(await volver.evaluate(el=>document.activeElement===el)){alcanzado=true;break;}}
  expect(alcanzado).toBe(true);
  await expect(volver).toHaveCSS('outline-style','solid');
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/\/plataforma(?:\?.*)?$/);
});
