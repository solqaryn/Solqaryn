import { expect, test, type Page } from '@playwright/test';
const ruta = '/plataforma/mysql-cutover-readonly-v5';
const headers = {
  'Access-Control-Allow-Origin':'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers':'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods':'GET, OPTIONS'
};
async function preparar(page:Page, falloPosterior=false, latenciaPosterior=0) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token','m0506-falso-no-jwt');
    localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
  });
  let gets=0;
  const writes:string[]=[];
  page.on('request',r => {
    if (!['GET','HEAD','OPTIONS'].includes(r.method())) writes.push(r.method());
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/,async route => {
    if(route.request().method()==='OPTIONS'){
      await route.fulfill({status:204,headers});return;
    }
    gets++;
    if(gets>1 && latenciaPosterior>0)
      await new Promise(resolve=>setTimeout(resolve,latenciaPosterior));
    const status=gets>1 && falloPosterior ? 503 : 200;
    await route.fulfill({status,headers,contentType:'application/json',
      body:JSON.stringify(status===200
        ? {success:true,data:{nombreUsuario:'QA sintético',rolNombre:'Administrador',
            ambito:'Plataforma',esAdministrador:true}}
        : {success:false,data:null,message:'PRIVATE_MYSQL_DSN_NEVER_RENDER'})});
  });
  return {gets:()=>gets,writes};
}
test('M05.06 regresion de cuatro controles no operativos y cero mutaciones',async({page})=>{
  const s=await preparar(page);
  await page.goto(ruta);
  await expect(page.getByRole('heading',{name:'Controles de preparación para cutover'})).toBeVisible();
  await expect(page.locator('[data-testid=c1204-cutover-private] .controles li')).toHaveCount(4);
  await expect(page.locator('.controles')).toContainText('SIN_VERIFICAR');
  await expect(page.locator('.controles')).toContainText('NO_MEDIDO');
  await expect(page.locator('.controles')).toContainText('NO_AUTORIZADO');
  await expect(page.locator('.cutover input, .cutover form, .cutover select')).toHaveCount(0);
  expect(s.gets()).toBe(1);expect(s.writes).toEqual([]);
});
test('M05.06 Tab real, outline, 44px, contraste y cuatro anchos',async({page})=>{
  const s=await preparar(page);await page.goto(ruta);
  const boton=page.getByRole('button',{name:'Revalidar sesión de plataforma'});
  let alcanzado=false;
  for(let i=0;i<40;i++){
    await page.keyboard.press('Tab');
    if(await boton.evaluate(n=>document.activeElement===n)){alcanzado=true;break;}
  }
  expect(alcanzado,'Tab desde el inicio').toBe(true);
  await expect(boton).toHaveCSS('outline-style','solid');
  expect(await boton.evaluate(n=>n.getBoundingClientRect().height)).toBeGreaterThanOrEqual(44);
  await page.keyboard.press('Enter');
  await expect(page.locator('[data-testid=c1204-m0402-state]')).toContainText('Éxito');
  await page.getByRole('button',{name:'Alto contraste'}).click();
  const colores=await page.locator('.cutover.contraste-alto').evaluate(n=>{
    const s=getComputedStyle(n);return [s.color,s.backgroundColor];
  });
  expect(colores).toEqual(['rgb(255, 255, 255)','rgb(16, 25, 39)']);
  for(const width of [320,375,768,1280]){
    await page.setViewportSize({width,height:800});
    await expect(page.locator('.controles li')).toHaveCount(4);
    expect(await page.evaluate(()=>
      document.documentElement.scrollWidth-document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
  }
  expect(s.gets()).toBe(2);expect(s.writes).toEqual([]);
});
test('M05.06 GET revalidacion lento: estado aria-busy sin duplicar solicitudes',async({page})=>{
  const s=await preparar(page,false,450);
  await page.goto(ruta);
  await page.getByRole('button',{name:'Revalidar sesión de plataforma'}).click();
  await expect(page.locator('[data-testid=c1204-m0402-panel]')).toHaveAttribute('aria-busy','true');
  await expect(page.getByRole('button',{name:'Revalidar sesión de plataforma'})).toHaveCount(0);
  await expect(page.locator('[data-testid=c1204-m0402-state]')).toContainText('Éxito');
  await expect(page.locator('[data-testid=c1204-m0402-panel]')).toHaveAttribute('aria-busy','false');
  expect(s.gets()).toBe(2);expect(s.writes).toEqual([]);
});
test('M05.06 revalidacion 503: oculta detalles privados y DSN',async({page})=>{
  const s=await preparar(page,true);await page.goto(ruta);
  await expect(page.locator('.controles li')).toHaveCount(4);
  await page.getByRole('button',{name:'Revalidar sesión de plataforma'}).click();
  await expect(page.locator('[data-testid=c1204-m0402-state]')).toContainText('Error');
  await expect(page.locator('.controles li')).toHaveCount(0);
  await expect(page.locator('[data-testid=c1204-m0403-branding]')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_MYSQL_DSN_NEVER_RENDER')).toHaveCount(0);
  expect(s.gets()).toBe(2);expect(s.writes).toEqual([]);
});