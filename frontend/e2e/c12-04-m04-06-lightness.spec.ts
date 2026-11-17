import { expect, test, type Page } from '@playwright/test';

async function preparar(page:Page, config:{hold?:boolean;secondStatus?:number}={}) {
  await page.addInitScript(()=>{
    localStorage.setItem('solqaryn_token','test-c1204-06');
    localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
  });
  let getCount=0;
  let release:(()=>void)|undefined;
  const writes:string[]=[];
  page.on('request',r=>{if(!['GET','HEAD','OPTIONS'].includes(r.method()))writes.push(r.method());});
  await page.route(/\/plataforma\/sesion(?:\?|$)/,async route=>{
    const headers={'Access-Control-Allow-Origin':'http://127.0.0.1:4200','Access-Control-Allow-Headers':'Authorization, Content-Type, X-Empresa-Id','Access-Control-Allow-Methods':'GET, OPTIONS'};
    if(route.request().method()==='OPTIONS'){await route.fulfill({status:204,headers});return;}
    getCount++;
    if(getCount===2&&config.hold)await new Promise<void>(resolve=>{release=resolve;});
    const status=getCount===2?(config.secondStatus??200):200;
    await route.fulfill({status,headers,contentType:'application/json',body:JSON.stringify(status===200?
      {success:true,data:{nombreUsuario:'Prueba',rolNombre:'Global',ambito:'Plataforma',esAdministrador:true}}:
      {success:false,data:null,message:'PRIVATE_DSN_DO_NOT_LEAK'})});
  });
  return {count:()=>getCount,writes,release:()=>{if(!release)throw new Error('GET pendiente inexistente');release();}};
}
test('M04.06 cuatro tarjetas, cuatro anchuras sin overflow y boton 44px',async({page})=>{
  const x=await preparar(page);
  await page.goto('/plataforma/mysql-cutover-readonly-v5');
  for(const width of [320,375,768,1280]){
    await page.setViewportSize({width,height:800});
    await expect(page.locator('.controles li')).toHaveCount(4);
    expect(await page.evaluate(()=>document.documentElement.scrollWidth-document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
    expect(await page.getByRole('button',{name:'Revalidar sesión de plataforma'}).evaluate(el=>el.getBoundingClientRect().height)).toBeGreaterThanOrEqual(44);
  }
  expect(x.writes).toEqual([]);
});
test('M04.06 progreso: contenido oculto con GET pendiente, luego recuperado',async({page})=>{
  const x=await preparar(page,{hold:true});
  await page.goto('/plataforma/mysql-cutover-readonly-v5');
  await expect(page.locator('.controles li')).toHaveCount(4);
  await page.getByRole('button',{name:'Revalidar sesión de plataforma'}).click();
  await expect.poll(x.count).toBe(2);
  await expect(page.locator('[data-testid=c1204-m0402-panel]')).toHaveAttribute('aria-busy','true');
  await expect(page.locator('.controles li')).toHaveCount(0);
  await expect(page.getByRole('button',{name:'Revalidar sesión de plataforma'})).toHaveCount(0);
  x.release();
  await expect(page.locator('[data-testid=c1204-m0402-panel]')).toHaveAttribute('aria-busy','false');
  await expect(page.locator('.controles li')).toHaveCount(4);
  expect(x.writes).toEqual([]);
});
test('M04.06 503 opaco: no secreto, tarjetas, reintentos ni escrituras',async({page})=>{
  const x=await preparar(page,{secondStatus:503});
  await page.goto('/plataforma/mysql-cutover-readonly-v5');
  await page.getByRole('button',{name:'Revalidar sesión de plataforma'}).click();
  await expect(page.locator('[data-testid=c1204-m0402-state]')).toContainText('Error');
  await expect(page.locator('.controles li')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_DSN_DO_NOT_LEAK')).toHaveCount(0);
  expect(x.count()).toBe(2);
  expect(x.writes).toEqual([]);
});
