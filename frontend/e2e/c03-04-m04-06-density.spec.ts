import {expect,test,type Page,type Route} from '@playwright/test';
const headers={'Access-Control-Allow-Origin':'http://127.0.0.1:4200','Access-Control-Allow-Headers':'Authorization, Content-Type, X-Empresa-Id','Access-Control-Allow-Methods':'GET, OPTIONS'};
const autorizado={success:true,data:{nombreUsuario:'Auditor',rolNombre:'Admin',ambito:'Plataforma',esAdministrador:true}};
async function responder(route:Route){await route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(autorizado)});}
async function sesion(page:Page,handler:(route:Route)=>Promise<void>=responder){
  await page.addInitScript(()=>{localStorage.setItem('solqaryn_token','c0304-m0406-synthetic-not-jwt');localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');});
  await page.route(/\/plataforma\/sesion(?:\?|$)/,async route=>{
    if(route.request().method()==='OPTIONS')return route.fulfill({status:204,headers});
    if(route.request().method()!=='GET')return route.abort();
    await handler(route);
  });
}
test('densidad efectiva y responsive sin overflow 320/768/1280',async({page})=>{
 await sesion(page);await page.setViewportSize({width:320,height:720});
 await page.goto('/plataforma/respuesta-incidentes-v5');
 const guia=page.locator('.incident-guide');const tarjetas=guia.locator('.incident-review li');
 await expect(tarjetas).toHaveCount(4);
 const antes=await tarjetas.first().evaluate(el=>parseFloat(getComputedStyle(el).paddingTop));
 await page.getByRole('button',{name:'Usar vista compacta'}).click();
 await expect(guia).toHaveAttribute('data-densidad','compacta');
 const despues=await tarjetas.first().evaluate(el=>parseFloat(getComputedStyle(el).paddingTop));
 expect(despues).toBeLessThan(antes);
 for(const width of [320,768,1280]){
   await page.setViewportSize({width,height:800});
   const m=await page.evaluate(()=>{const l=document.querySelector('.incident-review');if(!l)throw Error('Sin lista');return {overflow:document.documentElement.scrollWidth-document.documentElement.clientWidth,cols:getComputedStyle(l).gridTemplateColumns.trim().split(/\s+/).length};});
   expect(m.overflow).toBeLessThanOrEqual(2);
   expect(m.cols).toBe(width<=720?1:2);
 }
});
test('GET de sesion pendiente oculta guia hasta respuesta',async({page})=>{
 let retener=false;let liberar!:()=>void;const gate=new Promise<void>(res=>{liberar=res;});
 await sesion(page,async route=>{if(retener)await gate;await responder(route);});
 await page.goto('/plataforma/respuesta-incidentes-v5');
 await expect(page.locator('.incident-review li')).toHaveCount(4);
 retener=true;await page.getByRole('button',{name:'Revalidar sesión de plataforma'}).click();
 await expect(page.locator('.incident-guide .notice[role="status"]')).toContainText('Comprobando la sesión');
 await expect(page.locator('.incident-review li')).toHaveCount(0);
 liberar();await expect(page.locator('.incident-review li')).toHaveCount(4);
});
test('503 fail closed sin filtracion y sin escrituras',async({page})=>{
 let fallar=false;const mutaciones:string[]=[];
 page.on('request',req=>{if(/\/plataforma\/sesion(?:\?|$)/.test(req.url())&&!['GET','OPTIONS'].includes(req.method()))mutaciones.push(req.method());});
 await sesion(page,async route=>{if(!fallar)return responder(route);await route.fulfill({status:503,headers,contentType:'application/json',body:JSON.stringify({success:false,message:'INTERNAL_SECRET_DIAGNOSTIC'})});});
 await page.goto('/plataforma/respuesta-incidentes-v5');
 await expect(page.locator('.incident-review li')).toHaveCount(4);
 fallar=true;await page.getByRole('button',{name:'Revalidar sesión de plataforma'}).click();
 await expect(page.locator('.incident-guide .notice[role="alert"]')).toContainText('No se pudo comprobar la sesión');
 await expect(page.locator('.incident-review li')).toHaveCount(0);
 await expect(page.getByText('INTERNAL_SECRET_DIAGNOSTIC')).toHaveCount(0);
 expect(mutaciones).toEqual([]);
});
test('DOMContentLoaded y primera pintura locales medidos, RTO/RPO no medidos',async({page})=>{
 await sesion(page);
 for(const width of [320,1280]){
  await page.setViewportSize({width,height:800});
  await page.goto('/plataforma/respuesta-incidentes-v5');
  await expect(page.locator('.incident-review li')).toHaveCount(4);
  const m=await page.evaluate(()=>{const nav=performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming|undefined;const fcp=performance.getEntriesByType('paint').find(x=>x.name==='first-contentful-paint');return {dcl:nav?.domContentLoadedEventEnd??null,fcp:fcp?.startTime??null};});
  expect(m.dcl).not.toBeNull();expect(m.dcl!).toBeGreaterThan(0);expect(m.dcl!).toBeLessThan(15000);
  expect(m.fcp).not.toBeNull();expect(m.fcp!).toBeGreaterThan(0);expect(m.fcp!).toBeLessThan(15000);
  await expect(page.getByRole('note')).toContainText('NO_MEDIDO');
 }
});
