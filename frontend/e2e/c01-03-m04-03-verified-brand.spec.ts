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
});
