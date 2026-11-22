import {expect,test,type Page} from '@playwright/test';
const api=process.env['C1504_API_URL'];
const pass=process.env['C1504_TEST_PASSWORD'];
if(process.env['CI']!=='true'||api!=='http://127.0.0.1:5005'||!pass){
  throw new Error('C01-03.M04.03 requires GitHub-hosted real JWT/MySQL8.4/Chromium');
}
async function login(page:Page,user:string){
  await page.goto('/login');
  await page.getByRole('textbox',{name:'Nombre de usuario'}).fill(user);
  await page.locator('input[formcontrolname="password"]').fill(pass!);
  const p=page.waitForResponse(r=>r.url().endsWith('/auth/login')&&r.request().method()==='POST');
  await page.getByRole('button',{name:/Ingresar al sistema/}).click();
  const resp=await p;
  expect(resp.status()).toBe(200);
  const jwt:unknown=(await resp.json())?.data?.token;
  expect(typeof jwt).toBe('string');
  expect((jwt as string).split('.')).toHaveLength(3);
  return jwt as string;
}
test.describe('C01-03.M04.03 verified tenant branding actual server',()=>{
  test.describe.configure({retries:0});
  test('anonymous cannot see brand',async({page})=>{
    await page.goto('/configuracion/dinamica-v5');
    await expect(page).toHaveURL(/login/);
    await expect(page.getByTestId('c0103-verified-brand')).toHaveCount(0);
  });
  for(const [user,tenant,name,other] of [
    ['c1504_admin_a',1,'Empresa A efimera C15','Empresa B efimera C15'],
    ['c1504_admin_b',2,'Empresa B efimera C15','Empresa A efimera C15']
  ] as const){
    test(user+' shows only authenticated brand at five viewports',async({page})=>{
      test.setTimeout(240_000);
      const jwt=await login(page,user);
      const membership=await page.request.get(api+'/tenant-context/'+tenant,{headers:{Authorization:'Bearer '+jwt}});
      expect(membership.status()).toBe(200);
      await page.evaluate(id=>localStorage.setItem('solqaryn_empresa_solicitada_id',String(id)),tenant);
      await page.goto('/configuracion/dinamica-v5');
      const brand=page.getByTestId('c0103-verified-brand');
      await expect(brand).toHaveText(name);
      await expect(brand).toHaveAttribute('aria-label','Marca verificada de la empresa');
      await expect(page.getByText(other,{exact:true})).toHaveCount(0);
      for(const width of [320,390,768,1280,1440]){
        await page.setViewportSize({width,height:900});
        await expect(brand).toBeVisible();
        const overflow=await page.evaluate(()=>document.documentElement.scrollWidth-document.documentElement.clientWidth);
        expect(overflow,'body overflow at '+width).toBeLessThanOrEqual(2);
        await page.screenshot({path:'test-results/c0103-m0403-'+user+'-'+width+'.png',fullPage:true});
      }
      await page.keyboard.press('Tab');
      expect(await page.evaluate(()=>document.activeElement!==document.body)).toBe(true);
    });
  }
  test('A signed JWT cannot fetch B brand using forged header',async({page})=>{
    const jwt=await login(page,'c1504_admin_a');
    const x=await page.request.get(api+'/empresa-configuracion/tenant/2',{
      headers:{Authorization:'Bearer '+jwt,'X-Empresa-Id':'2'}
    });
    expect([403,404]).toContain(x.status());
  });
  test('forged JWT signature rejected for private brand endpoint',async({page})=>{
    const jwt=await login(page,'c1504_admin_a'),parts=jwt.split('.');
    parts[2]=(parts[2][0]==='A'?'B':'A')+parts[2].slice(1);
    const x=await page.request.get(api+'/empresa-configuracion/tenant/1',{
      headers:{Authorization:'Bearer '+parts.join('.')}
    });
    expect(x.status()).toBe(401);
  });

  test('A authenticated HTTP response delayed across logout cannot replace B brand',async({page})=>{
    test.setTimeout(240_000);
    const jwtA=await login(page,'c1504_admin_a');
    const membershipA=await page.request.get(api+'/tenant-context/1',{headers:{Authorization:'Bearer '+jwtA}});
    expect(membershipA.status()).toBe(200);
    await page.evaluate(()=>localStorage.setItem('solqaryn_empresa_solicitada_id','1'));

    // The response bytes come from the real ASP.NET/MySQL endpoint, not a mocked API.
    // Only delivery to the browser is adversarially delayed during an identity switch.
    let announceA!:()=>void;
    const sawA=new Promise<void>(resolve=>{announceA=resolve;});
    let releaseA!:()=>void;
    const pendingA=new Promise<void>(resolve=>{releaseA=resolve;});
    let capturedA=0;
    await page.route(/\/empresa-configuracion\/tenant\/1(?:\?|$)/,async route=>{
      const response=await route.fetch();
      if(response.status()!==200)throw new Error('Real A read must succeed before race');
      capturedA++;
      announceA();
      await pendingA;
      try{
        await route.fulfill({response});
      }catch{
        // Angular may cancel the obsolete A HTTP request on navigation/logout.
        // Cancellation is safer than delivery and must not resurrect A identity.
      }
    });

    await page.goto('/configuracion/dinamica-v5',{waitUntil:'domcontentloaded'});
    await sawA;
    await page.getByRole('button',{name:'Cerrar sesión'}).click();
    await expect(page).toHaveURL(/\/login(?:\?|$)/);
    const jwtB=await login(page,'c1504_admin_b');
    const membershipB=await page.request.get(api+'/tenant-context/2',{headers:{Authorization:'Bearer '+jwtB}});
    expect(membershipB.status()).toBe(200);
    await page.evaluate(()=>localStorage.setItem('solqaryn_empresa_solicitada_id','2'));
    await page.goto('/configuracion/dinamica-v5');
    const brand=page.getByTestId('c0103-verified-brand');
    await expect(brand).toHaveText('Empresa B efimera C15');
    releaseA();
    await expect(brand).toHaveText('Empresa B efimera C15');
    await expect(page.getByText('Empresa A efimera C15',{exact:true})).toHaveCount(0);
    expect(capturedA).toBeGreaterThan(0);
    await page.screenshot({path:'test-results/c0103-m0403-B-after-delayed-A.png',fullPage:true});
  });

  test('same signed JWT A to B switch rejects a delayed A brand response',async({page})=>{
    test.setTimeout(240_000);
    const jwt=await login(page,'c0103_admin_ab');
    const memberships=await page.request.get(api+'/tenant-context/empresas',{
      headers:{Authorization:'Bearer '+jwt}
    });
    expect(memberships.status()).toBe(200);
    const companies:unknown=(await memberships.json())?.data;
    expect(Array.isArray(companies)).toBe(true);
    expect((companies as {empresaId:number}[]).map(x=>x.empresaId).sort()).toEqual([1,2]);
    for(const id of [1,2]){
      const context=await page.request.get(api+'/tenant-context/'+id,{
        headers:{Authorization:'Bearer '+jwt}
      });
      expect(context.status()).toBe(200);
    }

    const select=page.getByRole('combobox',{name:'Empresa autorizada'});
    await expect(select).toBeVisible();
    await select.selectOption({label:'Empresa A efimera C15'});
    await page.getByRole('button',{name:'Entrar a la empresa'}).click();
    await expect(page).not.toHaveURL(/\\/login(?:[?#]|$)/);
    const switcher=page.getByRole('button',{name:'Cambiar de empresa sin cerrar sesión'});
    await expect(switcher).toBeVisible();

    let sawA!:()=>void;
    const capturedA=new Promise<void>(resolve=>{sawA=resolve;});
    let releaseA!:()=>void;
    const heldA=new Promise<void>(resolve=>{releaseA=resolve;});
    let settledA!:()=>void;
    const settled=new Promise<void>(resolve=>{settledA=resolve;});
    let responseCount=0;
    await page.route(/\\/empresa-configuracion\\/tenant\\/1(?:\\?|$)/,async route=>{
      try{
        const real=await route.fetch();
        expect(real.status(),'real signed JWT A read').toBe(200);
        responseCount++;
        sawA();
        await heldA;
        try{await route.fulfill({response:real});}
        catch{ /* canceled stale request is safe after a real server response */ }
      }finally{settledA();}
    });

    await page.goto('/configuracion/dinamica-v5',{waitUntil:'domcontentloaded'});
    await capturedA;
    await expect(switcher).toBeVisible();
    await switcher.click();
    await expect(page).toHaveURL(/\\/login(?:[?#]|$)/);
    const selectB=page.getByRole('combobox',{name:'Empresa autorizada'});
    await expect(selectB).toBeVisible();
    await selectB.selectOption({label:'Empresa B efimera C15'});
    await page.getByRole('button',{name:'Entrar a la empresa'}).click();
    await expect(page).not.toHaveURL(/\\/login(?:[?#]|$)/);
    const currentJwt=await page.evaluate(()=>localStorage.getItem('solqaryn_token'));
    expect(currentJwt,'same principal and JWT, no logout or new login').toBe(jwt);
    await page.goto('/configuracion/dinamica-v5');
    const brand=page.getByTestId('c0103-verified-brand');
    await expect(brand).toHaveText('Empresa B efimera C15');
    releaseA();
    await settled;
    await expect(brand).toHaveText('Empresa B efimera C15');
    await expect(page.getByText('Empresa A efimera C15',{exact:true})).toHaveCount(0);
    expect(responseCount).toBe(1);
    await page.screenshot({path:'test-results/c0103-m0403-same-jwt-A-to-B-stale-rejection.png',fullPage:true});
  });

});
