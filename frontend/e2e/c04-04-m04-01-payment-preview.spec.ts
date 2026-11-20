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


test('C04-04.M04.05 responsive zoom 200 percent, headings and no pay action',async({page})=>{
  await fixtureSesion(page,200);
  await page.goto(url);
  const panel=page.getByTestId('c0404-payment-private');
  await expect(panel).toBeVisible();
  await expect(page.getByRole('heading',{name:'Enlaces de cobro',level:1})).toHaveCount(1);
  await expect(panel.getByRole('heading',{name:'Etapas previstas',level:2})).toHaveCount(1);
  await expect(panel.locator('li h3')).toHaveCount(3);
  await page.evaluate(()=>{document.documentElement.style.fontSize='200%';});
  for(const width of [320,375,768,1280]){
    await page.setViewportSize({width,height:800});
    const size=await page.evaluate(()=>({scroll:document.documentElement.scrollWidth,client:document.documentElement.clientWidth}));
    expect(size.scroll-size.client,'overflow at 200% width='+width).toBeLessThanOrEqual(2);
    await expect(page.getByRole('link',{name:'Volver a plataforma'})).toBeVisible();
  }
  await expect(panel.locator('form,input,button,select,textarea,iframe')).toHaveCount(0);
});

test('C04-04.M04.05 keyboard and focus target visible without payment writes',async({page})=>{
  await fixtureSesion(page,200);
  const writes:string[]=[];
  page.on('request',request=>{if(!['GET','HEAD','OPTIONS'].includes(request.method()))writes.push(request.method());});
  await page.goto(url);
  // Route guard asynchronously verifies the platform session; wait for the private view before actual keyboard navigation.
  await expect(page.getByTestId('c0404-payment-private')).toBeVisible();
  await page.keyboard.press('Tab');
  const link=page.getByRole('link',{name:'Volver a plataforma'});
  await expect(link).toBeFocused();
  const focus=await link.evaluate(element=>{
    const style=getComputedStyle(element);
    const box=element.getBoundingClientRect();
    return {outline:parseFloat(style.outlineWidth),style:style.outlineStyle,width:box.width,height:box.height};
  });
  expect(focus.outline,'visible keyboard focus').toBeGreaterThanOrEqual(3);
  expect(focus.style).not.toBe('none');
  expect(focus.height,'minimum target height').toBeGreaterThanOrEqual(44);
  expect(focus.width,'minimum target width').toBeGreaterThanOrEqual(44);
  expect(writes).toEqual([]);
});

test('C04-04.M04.05 403 denied on narrow and desktop screens without error leak',async({page})=>{
  await fixtureSesion(page,403);
  for(const width of [320,1280]){
    await page.setViewportSize({width,height:800});
    await page.goto(url);
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByTestId('c0404-payment-private')).toHaveCount(0);
    await expect(page.getByText('PRIVATE_PSP_SECRET')).toHaveCount(0);
  }
});
