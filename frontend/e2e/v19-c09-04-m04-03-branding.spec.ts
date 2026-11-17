import { expect, test, type Page } from '@playwright/test';

// Frontend/Chromium real; /plataforma/sesion SIMULADA. Nunca validar JWT, MySQL ni proveedores con este test.
const url='/plataforma/mensajeria-multicanal-v19';
const cors={
  'Access-Control-Allow-Origin':'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers':'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods':'GET, OPTIONS'
};
async function sesion(page:Page, status=200, data?:object) {
  await page.addInitScript(()=>{
    localStorage.setItem('solqaryn_token','c0904m0403-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/,async route=>{
    if(route.request().method()==='OPTIONS') {
      await route.fulfill({status:204,headers:cors});return;
    }
    await route.fulfill({status,headers:cors,contentType:'application/json',
      body:JSON.stringify(status===200?
        {success:true,data:data??{nombreUsuario:'Admin',rolNombre:'Administrador',ambito:'Plataforma',esAdministrador:true}}:
        {success:false,message:'PRIVADO_TENANT_B_NUNCA_VISIBLE'})});
  });
}

test('admin simulado alterna temas cerrados, sin POST ni envios a terceros',async({page})=>{
  await sesion(page);
  const writes:string[]=[];
  page.on('request',r=>{if(!['GET','HEAD','OPTIONS'].includes(r.method())) writes.push(r.url());});
  await page.goto(url);
  const panel=page.getByTestId('c0904-branding-config');
  const selector=page.getByTestId('c0904-tema-selector');
  await expect(panel).toBeVisible();
  await expect(selector.locator('option')).toHaveCount(3);
  await selector.selectOption('alto-contraste');
  await expect(panel).toHaveClass(/tema-alto-contraste/);
  await expect(page.getByTestId('c0904-tema-activo')).toContainText('Alto contraste');
  await selector.selectOption('azul');
  await expect(panel).toHaveClass(/tema-azul/);
  await expect(page.getByTestId('c0904-canales-privados').locator('li')).toHaveCount(3);
  await expect(page.getByText('NO OPERATIVO')).toBeVisible();
  expect(writes).toEqual([]);
});

test('sesion fallida y empresa no pueden ver branding ni datos privados',async({browser})=>{
  for(const scenario of [{status:403,data:undefined},{status:200,data:{nombreUsuario:'Empresa',rolNombre:'Admin',ambito:'Empresa',esAdministrador:true}}]){
    const page=await browser.newPage();
    await sesion(page,scenario.status,scenario.data);
    await page.goto(url);
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByTestId('c0904-branding-config')).toHaveCount(0);
    await expect(page.getByText('PRIVADO_TENANT_B_NUNCA_VISIBLE')).toHaveCount(0);
    await page.close();
  }
});

test('select accesible por Tab real y responsive 320/375/768/1280 sin overflow',async({page})=>{
  await sesion(page);
  for(const width of [320,375,768,1280]) {
    await page.setViewportSize({width,height:812});
    await page.goto(url);
    await expect(page.getByTestId('c0904-tema-selector')).toBeVisible();
    expect(await page.evaluate(()=>document.documentElement.scrollWidth-document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
  }
  const selector=page.getByTestId('c0904-tema-selector');
  let alcanzado=false;
  for(let i=0;i<80;i++){
    await page.keyboard.press('Tab');
    if(await selector.evaluate(el=>document.activeElement===el)){alcanzado=true;break;}
  }
  expect(alcanzado).toBe(true);
  await expect(selector).toHaveCSS('outline-style','solid');
  await page.keyboard.press('ArrowDown');
  await expect(selector).toHaveValue('alto-contraste');
});
