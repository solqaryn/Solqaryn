import {expect,test,type Page} from '@playwright/test';
const route='/plataforma/eventos-outbox-v22';
const cors={'Access-Control-Allow-Origin':'http://127.0.0.1:4200','Access-Control-Allow-Headers':'Authorization, Content-Type, X-Empresa-Id','Access-Control-Allow-Methods':'GET, OPTIONS'};
async function session(page:Page,status:200|403){
  await page.addInitScript(()=>{
    localStorage.setItem('solqaryn_token','UI_FIXTURE_NOT_REAL_SIGNED_JWT');
    localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/,async r=>{
    if(r.request().method()==='OPTIONS'){await r.fulfill({status:204,headers:cors});return;}
    await r.fulfill({status,headers:cors,contentType:'application/json',body:JSON.stringify(status===200?
      {success:true,data:{nombreUsuario:'DO_NOT_DISPLAY_ME',rolNombre:'Plataforma',ambito:'Plataforma',esAdministrador:true}}:
      {success:false,message:'DO_NOT_DISPLAY_ME'})});
  });
}
test('C11-03.M04.03 Chromium branding local responsive sin escrituras',async({page})=>{
  await session(page,200);
  const writes:string[]=[];
  page.on('request',r=>{if(!['GET','HEAD','OPTIONS'].includes(r.method()))writes.push(r.method());});
  await page.goto(route);
  await expect(page.getByTestId('c1103-outbox-private')).toBeVisible();
  await expect(page.getByTestId('c1103-outbox-view')).toHaveAttribute('data-tema','base');
  await expect(page.getByTestId('c1103-branding-preset')).toContainText('SOLQARYN');
  for(const width of [320,375,768,1280]){
    await page.setViewportSize({width,height:800});
    expect(await page.evaluate(()=>document.documentElement.scrollWidth-document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
  }
  await expect(page.getByText('DO_NOT_DISPLAY_ME')).toHaveCount(0);
  expect(writes).toEqual([]);
});
test('C11-03.M04.03 anonimo y 403 sin branding privado',async({page})=>{
  await page.goto(route);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByTestId('c1103-branding-preset')).toHaveCount(0);
  await session(page,403);await page.goto(route);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByTestId('c1103-branding-preset')).toHaveCount(0);
  await expect(page.getByText('DO_NOT_DISPLAY_ME')).toHaveCount(0);
});
