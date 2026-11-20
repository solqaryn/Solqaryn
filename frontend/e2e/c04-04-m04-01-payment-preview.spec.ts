import { expect,test,type Page } from '@playwright/test';
const url='/plataforma/enlaces-cobro-simulacion-v22';
const headers={'Access-Control-Allow-Origin':'http://127.0.0.1:4200','Access-Control-Allow-Headers':'Authorization, Content-Type, X-Empresa-Id','Access-Control-Allow-Methods':'GET, OPTIONS'};
async function fixtureSesion(page:Page,status:200|403) {
  await page.addInitScript(()=>{
    localStorage.setItem('solqaryn_token','UI_FIXTURE_NOT_SIGNED_JWT');
    localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/,async r=>{
    if(r.request().method()==='OPTIONS'){await r.fulfill({status:204,headers});return;}
    await r.fulfill({status,headers,contentType:'application/json',body:JSON.stringify(status===200?
      {success:true,data:{nombreUsuario:'Fixture',rolNombre:'Plataforma',ambito:'Plataforma',esAdministrador:true}}:
      {success:false,message:'PRIVATE_PSP_SECRET'})});
  });
}
test('C04-04.M04.01 anonymous and 403 do not display private preview',async({page})=>{
  await page.goto(url);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByTestId('c0404-payment-private')).toHaveCount(0);
  await fixtureSesion(page,403);await page.goto(url);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByText('PRIVATE_PSP_SECRET')).toHaveCount(0);
});
test('C04-04.M04.01 synthetic UI: no writes, responsive, keyboard',async({page})=>{
  await fixtureSesion(page,200);
  const writes:string[]=[];
  page.on('request',r=>{if(!['GET','HEAD','OPTIONS'].includes(r.method()))writes.push(r.method());});
  await page.goto(url);
  const panel=page.getByTestId('c0404-payment-private');
  await expect(panel).toBeVisible();
  await expect(panel.locator('[data-testid^="c0404-step-"]')).toHaveCount(3);
  await expect(panel.getByText(/SIN_CONEXION/)).toHaveCount(4);
  await expect(panel.locator('form,input,button,textarea,select')).toHaveCount(0);
  for(const width of [320,375,768,1280]){
    await page.setViewportSize({width,height:800});
    const overflow=await page.evaluate(()=>document.documentElement.scrollWidth-document.documentElement.clientWidth);
    expect(overflow,'overflow '+width).toBeLessThanOrEqual(2);
  }
  expect(writes).toEqual([]);
  await page.keyboard.press('Tab');
  await expect(page.getByRole('link',{name:'Volver a plataforma'})).toBeFocused();
});
