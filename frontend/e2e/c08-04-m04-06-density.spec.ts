import {expect,test,type Page} from '@playwright/test';
const cors={'Access-Control-Allow-Origin':'http://127.0.0.1:4200','Access-Control-Allow-Headers':'Authorization, Content-Type, X-Empresa-Id','Access-Control-Allow-Methods':'GET, OPTIONS'};
async function fixture(page:Page,allowed:boolean){
 await page.addInitScript(()=>{
  localStorage.setItem('solqaryn_token','C0804_SYNTHETIC_NOT_JWT');
  localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
  localStorage.setItem('solqaryn_empresa_solicitada_id','71');
 });
 await page.route(/\/tenant-context\/71(?:\?|$)/,async route=>{
  if(route.request().method()==='OPTIONS'){await route.fulfill({status:204,headers:cors});return;}
  await route.fulfill({status:allowed?200:403,headers:cors,contentType:'application/json',
   body:JSON.stringify(allowed?{success:true,data:{usuarioId:7,empresaId:71,rolId:3,rolNombre:'Lectura',esAdministrador:false}}:{success:false,message:'SECRET_SYNTHETIC_DSN'})});
 });
}
test.describe('C08-04.M04.06 Chromium real con tenant simulado',()=>{
 test.describe.configure({retries:0});test.use({extraHTTPHeaders:{}});
 test('rejilla adaptativa y sin overflow en 320 375 768 1280',async({page})=>{
  await fixture(page,true);
  for(const width of [320,375,768,1280]){
   await page.setViewportSize({width,height:820});await page.goto('/extensiones-sectoriales');
   const view=page.locator('main.sector-readonly');
   await expect(view).toBeVisible();await expect(view.locator('.sector-card')).toHaveCount(4);
   await expect(view.locator('[aria-label="Estado de las capacidades"]')).toHaveAttribute('aria-busy','false');
   const data=await view.evaluate(root=>{
    const grid=root.querySelector('.sector-grid')!;
    const cards=[...root.querySelectorAll('.sector-card')];
    return {cols:getComputedStyle(grid).gridTemplateColumns.trim().split(/\s+/).length,
     overflow:document.documentElement.scrollWidth-document.documentElement.clientWidth,
     right:Math.max(...cards.map(x=>x.getBoundingClientRect().right)),
     viewport:document.documentElement.clientWidth};
   });
   expect(data.overflow).toBeLessThanOrEqual(2);expect(data.right).toBeLessThanOrEqual(data.viewport+2);
   if(width<=375)expect(data.cols).toBe(1);
   if(width>=768)expect(data.cols).toBeGreaterThanOrEqual(2);
   await expect(view.locator('img,video,iframe,form,input,button')).toHaveCount(0);
  }
 });
 test('vista privada informativa no emite mutaciones ni llama modulo',async({page})=>{
  await fixture(page,true);
  const writes:string[]=[],modules:string[]=[];
  page.on('request',req=>{
   const path=new URL(req.url()).pathname;
   if(['POST','PUT','PATCH','DELETE'].includes(req.method()))writes.push(path);
   if(/\/(?:sector-modules|capabilities|modulos-sectoriales)(?:\/|$)/i.test(path))modules.push(path);
  });
  await page.goto('/extensiones-sectoriales');
  await expect(page.getByRole('heading',{name:'Capacidades por rubro'})).toBeVisible();
  await expect(page.locator('.sector-card')).toHaveCount(4);
  expect(writes).toEqual([]);expect(modules).toEqual([]);
 });
 test('403 simulado oculta tarjetas y diagnostico privado',async({page})=>{
  await fixture(page,false);await page.goto('/extensiones-sectoriales');
  await expect(page).toHaveURL(/\/login(?:\?|$)/);
  await expect(page.locator('.sector-card,.sector-grid')).toHaveCount(0);
  await expect(page.getByText('SECRET_SYNTHETIC_DSN')).toHaveCount(0);
 });
});