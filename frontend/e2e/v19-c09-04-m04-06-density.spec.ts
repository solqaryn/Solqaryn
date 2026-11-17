import { expect, test, type Page } from '@playwright/test';

/** C09-04.M04.06: Chromium real; /plataforma/sesion sintetica, NO JWT ni proveedores. */
const url='/plataforma/mensajeria-multicanal-v19';
const sesionUrl=/\/plataforma\/sesion(?:\?|$)/;
const cors={'Access-Control-Allow-Origin':'http://127.0.0.1:4200',
  'Access-Control-Allow-Headers':'Authorization, Content-Type, X-Empresa-Id',
  'Access-Control-Allow-Methods':'GET, OPTIONS'};
async function token(page:Page){
  await page.addInitScript(()=>{
    localStorage.setItem('solqaryn_token','c0904-m0406-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
  });
}
async function sesion(page:Page,status=200){
  let count=0;
  await page.route(sesionUrl,async route=>{
    if(route.request().method()==='OPTIONS'){await route.fulfill({status:204,headers:cors});return;}
    count++;
    await route.fulfill({status,headers:cors,contentType:'application/json',
      body:JSON.stringify(status===200?
        {success:true,data:{nombreUsuario:'Admin',rolNombre:'Admin',
          ambito:'Plataforma',esAdministrador:true}}:
        {success:false,message:'TENANT_B_PRIVATE_TOKEN_NEVER_RENDER'})});
  });
  return ()=>count;
}
test.describe('C09-04.M04.06 ligereza y progresion segura',()=>{
  test.describe.configure({retries:0});
  test('320/375/768/1280 reflow, tres canales, sin overflow ni writes',async({page})=>{
    await token(page);const count=await sesion(page);
    const writes:string[]=[];
    page.on('request',r=>{if(!['GET','HEAD','OPTIONS'].includes(r.method()))writes.push(r.method());});
    for(const width of [320,375,768,1280]){
      await page.setViewportSize({width,height:812});
      await page.goto(url);
      await expect(page.getByTestId('c0904-canales-privados').locator('li')).toHaveCount(3);
      await expect(page.getByTestId('c0904-componentes-permitidos').locator('li')).toHaveCount(3);
      const columns=await page.locator('.canales').evaluate(el=>
        getComputedStyle(el).gridTemplateColumns.split(' ').filter(Boolean).length);
      expect(columns).toBe(width<=672?1:3);
      expect(await page.evaluate(()=>
        document.documentElement.scrollWidth-document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
    }
    expect(count()).toBeGreaterThan(0);expect(writes).toEqual([]);
  });
  test('GET retenido: contenido privado oculto hasta sesion autorizada',async({page})=>{
    await token(page);
    let release!:()=>void;const hold=new Promise<void>(resolve=>{release=resolve;});
    let calls=0;
    await page.route(sesionUrl,async route=>{
      if(route.request().method()==='OPTIONS'){await route.fulfill({status:204,headers:cors});return;}
      calls++;await hold;
      await route.fulfill({status:200,headers:cors,contentType:'application/json',
        body:JSON.stringify({success:true,data:{nombreUsuario:'Admin',rolNombre:'Admin',
          ambito:'Plataforma',esAdministrador:true}})});
    });
    try{
      await page.goto(url,{waitUntil:'domcontentloaded'});
      await expect.poll(()=>calls).toBeGreaterThan(0);
      await expect(page.getByTestId('c0904-canales-privados')).toHaveCount(0);
      release();
      await expect(page.getByTestId('c0904-canales-privados').locator('li')).toHaveCount(3);
    }finally{release();}
  });
  test('401,403,503: deniega tarjetas y oculta detalle privado',async({browser})=>{
    for(const status of [401,403,503]){
      const page=await browser.newPage();
      try{
        await token(page);const count=await sesion(page,status);
        const writes:string[]=[];
        page.on('request',r=>{if(!['GET','HEAD','OPTIONS'].includes(r.method()))writes.push(r.method());});
        await page.goto(url);
        await expect(page).toHaveURL(/\/login/);
        await expect(page.getByTestId('c0904-canales-privados')).toHaveCount(0);
        await expect(page.getByText('TENANT_B_PRIVATE_TOKEN_NEVER_RENDER')).toHaveCount(0);
        expect(count()).toBeGreaterThan(0);expect(writes).toEqual([]);
      }finally{await page.close();}
    }
  });
  test('tema local es reversible y no persiste ni envia mensajes',async({page})=>{
    await token(page);await sesion(page);
    const writes:string[]=[];
    page.on('request',r=>{if(!['GET','HEAD','OPTIONS'].includes(r.method()))writes.push(r.method());});
    await page.goto(url);
    const selector=page.getByTestId('c0904-tema-selector');
    await expect(page.getByTestId('c0904-tema-activo')).toContainText('Neutro');
    await selector.selectOption('azul');
    await expect(page.getByTestId('c0904-branding-config')).toHaveClass(/tema-azul/);
    await selector.selectOption('alto-contraste');
    await expect(page.getByTestId('c0904-tema-activo')).toContainText('Alto contraste');
    await page.reload();
    await expect(page.getByTestId('c0904-tema-activo')).toContainText('Neutro');
    await expect(page.getByTestId('c0904-canales-privados').locator('li')).toHaveCount(3);
    expect(writes).toEqual([]);
  });
});
