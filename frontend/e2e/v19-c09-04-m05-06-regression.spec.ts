import {expect, test, type Page} from '@playwright/test';
/** C09-04.M05.06: Angular real/Chromium, sesion sintetica. No JWT firmado ni proveedor. */
const target='/plataforma/mensajeria-multicanal-v19';
const sessionPattern=/\/plataforma\/sesion(?:\?|$)/;
const cors={'Access-Control-Allow-Origin':'http://127.0.0.1:4200',
 'Access-Control-Allow-Headers':'Authorization, Content-Type, X-Empresa-Id',
 'Access-Control-Allow-Methods':'GET, OPTIONS'};
async function setup(page:Page, initial=200){
  await page.addInitScript(()=>{
    localStorage.setItem('solqaryn_token','m0506-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
  });
  let status=initial,reads=0;const writes:string[]=[];
  page.on('request',r=>{if(!['GET','HEAD','OPTIONS'].includes(r.method()))writes.push(r.method());});
  await page.route(sessionPattern,async r=>{
    if(r.request().method()==='OPTIONS'){await r.fulfill({status:204,headers:cors});return;}
    reads++;
    await r.fulfill({status,headers:cors,contentType:'application/json',
      body:JSON.stringify(status===200
        ? {success:true,data:{nombreUsuario:'Admin',rolNombre:'Admin',ambito:'Plataforma',esAdministrador:true}}
        : {success:false,message:'TENANT_B_PRIVATE_NEVER_RENDER'})});
  });
  return{setStatus:(x:number)=>{status=x;},reads:()=>reads,writes};
}
test.describe('C09-04.M05.06 regresion rendimiento a11y',()=>{
 test.describe.configure({retries:0});
 test('320/375/768/1280, DOM acotado y ausencia de escrituras',async({page})=>{
   const s=await setup(page);
   for(const width of [320,375,768,1280]){
     await page.setViewportSize({width,height:812});await page.goto(target);
     const main=page.locator('main[aria-labelledby="c0904-titulo"]');
     await expect(main).toBeVisible();
     await expect(page.getByTestId('c0904-canales-privados').locator('li')).toHaveCount(3);
     await expect(page.getByRole('heading',{level:1,name:'Mensajería multicanal'})).toBeVisible();
     const m=await main.evaluate(el=>({nodes:el.querySelectorAll('*').length,
       overflow:document.documentElement.scrollWidth-document.documentElement.clientWidth,
       elapsed:(performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming|undefined)?.domContentLoadedEventEnd??0}));
     expect(m.nodes).toBeGreaterThan(5);expect(m.nodes).toBeLessThan(350);
     expect(m.overflow).toBeLessThanOrEqual(2);
     expect(m.elapsed).toBeGreaterThan(0);expect(m.elapsed).toBeLessThan(20_000);
   }
   expect(s.reads()).toBeGreaterThanOrEqual(4);expect(s.writes).toEqual([]);
 });
 test('Tab real, outline y destino tactil, Enter navega',async({page})=>{
   const s=await setup(page);await page.setViewportSize({width:375,height:812});
   await page.goto(target);const link=page.getByRole('link',{name:'Regresar a plataforma'});
   let found=false;
   for(let i=0;i<80;i++){await page.keyboard.press('Tab');
     if(await link.evaluate(el=>document.activeElement===el)){found=true;break;}}
   expect(found).toBe(true);
   const m=await link.evaluate(el=>{const css=getComputedStyle(el),r=el.getBoundingClientRect();
     return{style:css.outlineStyle,width:parseFloat(css.outlineWidth),w:r.width,h:r.height};});
   expect(m.style).not.toBe('none');expect(m.width).toBeGreaterThanOrEqual(2);
   expect(m.w).toBeGreaterThanOrEqual(44);expect(m.h).toBeGreaterThanOrEqual(44);
   await page.keyboard.press('Enter');await expect(page).toHaveURL(/\/plataforma(?:\?.*)?$/);
   expect(s.writes).toEqual([]);
 });
 test('revocacion HTTP403 tras cambiar tema oculta privada',async({page})=>{
   const s=await setup(page);await page.goto(target);
   await expect(page.getByTestId('c0904-canales-privados').locator('li')).toHaveCount(3);
   await page.getByTestId('c0904-tema-selector').selectOption('alto-contraste');
   await expect(page.getByTestId('c0904-tema-activo')).toContainText('Alto contraste');
   s.setStatus(403);await page.reload();await expect(page).toHaveURL(/\/login/);
   await expect(page.getByTestId('c0904-canales-privados')).toHaveCount(0);
   await expect(page.getByText('TENANT_B_PRIVATE_NEVER_RENDER')).toHaveCount(0);
   expect(s.reads()).toBeGreaterThanOrEqual(2);expect(s.writes).toEqual([]);
 });
 test('HTTP503 inicial deniega sin fuga ni mutacion',async({page})=>{
   const s=await setup(page,503);await page.goto(target);
   await expect(page).toHaveURL(/\/login/);
   await expect(page.getByTestId('c0904-canales-privados')).toHaveCount(0);
   await expect(page.getByText('TENANT_B_PRIVATE_NEVER_RENDER')).toHaveCount(0);
   expect(s.reads()).toBeGreaterThan(0);expect(s.writes).toEqual([]);
 });
});
