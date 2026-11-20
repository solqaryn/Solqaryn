import { expect, test, type Page } from '@playwright/test';
const ruta='/plataforma/enlaces-cobro-simulacion-v22';
const cors={'Access-Control-Allow-Origin':'http://127.0.0.1:4200','Access-Control-Allow-Headers':'Authorization, Content-Type, X-Empresa-Id','Access-Control-Allow-Methods':'GET, OPTIONS'};
/** This is a UI-only session fixture, NOT a signed JWT, provider or HTTP security certification. */
async function sesionSoloUi(page:Page,status:200|403){
  await page.addInitScript(()=>{
    localStorage.setItem('solqaryn_token','C0404_M0404_SYNTHETIC_NOT_SIGNED_JWT');
    localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/,async handler=>{
    if(handler.request().method()==='OPTIONS'){await handler.fulfill({status:204,headers:cors});return;}
    await handler.fulfill({status,headers:cors,contentType:'application/json',
      body:JSON.stringify(status===200?
        {success:true,data:{nombreUsuario:'Fixture',rolNombre:'Plataforma',ambito:'Plataforma',esAdministrador:true}}:
        {success:false,message:'PRIVATE_PSP_SECRET_NEVER_DISPLAY'})});
  });
}
test('C04-04.M04.04 messages and zero payment forms',async({page})=>{
  await sesionSoloUi(page,200);
  const writes:string[]=[];page.on('request',r=>{if(!['GET','HEAD','OPTIONS'].includes(r.method()))writes.push(r.method());});
  await page.goto(ruta);
  const panel=page.getByTestId('c0404-payment-private');
  await expect(panel).toBeVisible();
  await expect(page.getByRole('heading',{name:'Enlaces de cobro'})).toHaveCount(1);
  await expect(panel.getByTestId('c0404-state-empty')).toHaveAttribute('role','status');
  await expect(panel.getByText(/SIN_CONEXION/)).toHaveCount(4);
  await expect(panel.locator('[data-testid^="c0404-step-"]')).toHaveCount(3);
  await expect(panel.locator('form,input,button,select,textarea,iframe')).toHaveCount(0);
  expect(writes).toEqual([]);
});
test('C04-04.M04.04 two UI navigations do not create duplicate mutating requests',async({page})=>{
  await sesionSoloUi(page,200);
  const writes:string[]=[];page.on('request',r=>{if(!['GET','HEAD','OPTIONS'].includes(r.method()))writes.push(r.method()+' '+r.url());});
  for(let i=0;i<2;i++){
    await page.goto(ruta);
    await expect(page.getByTestId('c0404-payment-private')).toBeVisible();
    const back=page.getByRole('link',{name:'Volver a plataforma'});
    await expect(back).toHaveAttribute('href','/plataforma');
    await expect(page.locator('form,input,button,select,textarea')).toHaveCount(0);
    await back.click();
    await expect(page).not.toHaveURL(/enlaces-cobro-simulacion-v22/);
  }
  expect(writes).toEqual([]);
});
test('C04-04.M04.04 anonymous and 403 hide private messages',async({page})=>{
  await page.goto(ruta);await expect(page).toHaveURL(/\/login/);
  await expect(page.getByTestId('c0404-payment-private')).toHaveCount(0);
  await sesionSoloUi(page,403);await page.goto(ruta);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByText('PRIVATE_PSP_SECRET_NEVER_DISPLAY')).toHaveCount(0);
  await expect(page.getByTestId('c0404-payment-private')).toHaveCount(0);
});
