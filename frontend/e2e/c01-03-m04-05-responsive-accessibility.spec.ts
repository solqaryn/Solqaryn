import { expect, test, type Page } from '@playwright/test';

// Real .NET10 backend, signed JWT, Angular and ephemeral MySQL8.4. Never mock authorization.
const api = process.env['C1504_API_URL'];
const password = process.env['C1504_TEST_PASSWORD'];
if (process.env['CI'] !== 'true' || api !== 'http://127.0.0.1:5005' || !password) {
  throw new Error('C01-03.M04.05 requires GitHub-hosted real API/JWT/MySQL/Chromium');
}
const target = '/configuracion/dinamica-v5';
const widths = [320, 390, 768, 1280, 1440] as const;

async function login(page: Page, username: string): Promise<string> {
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Nombre de usuario' }).fill(username);
  await page.locator('input[formcontrolname="password"]').fill(password!);
  const pending = page.waitForResponse(r => r.url().endsWith('/auth/login') &&
    r.request().method() === 'POST');
  await page.getByRole('button', { name: /Ingresar al sistema/ }).click();
  const res = await pending;
  expect(res.status(), 'actual authenticated JWT login').toBe(200);
  const jwt: unknown = (await res.json())?.data?.token;
  expect(typeof jwt).toBe('string');
  expect((jwt as string).split('.')).toHaveLength(3);
  return jwt as string;
}

async function verifyLayout(page: Page, width: number, phase: string): Promise<void> {
  const data = await page.evaluate(() => {
    const root = document.documentElement;
    const targets = ['h1', 'app-empresa-configuracion-tenant-card', '.surface-content'];
    const boxes = targets.map(name => {
      const node = document.querySelector(name);
      if (!(node instanceof HTMLElement)) return { name, exists: false, left: 0, right: 0 };
      const rect = node.getBoundingClientRect();
      return { name, exists: true, left: rect.left, right: rect.right };
    });
    return { scroll: root.scrollWidth, width: root.clientWidth, boxes };
  });
  expect(data.scroll, 'document overflow at '+phase+' width '+width).toBeLessThanOrEqual(data.width + 2);
  for (const box of data.boxes) {
    expect(box.exists, box.name+' present').toBe(true);
    expect(box.left, box.name+' left clipping').toBeGreaterThanOrEqual(-2);
    expect(box.right, box.name+' right clipping').toBeLessThanOrEqual(data.width + 2);
  }
}

async function verifyTab(page: Page): Promise<void> {
  // Real keyboard navigation. Do not call element.focus().
  let found = false;
  for (let n=0;n<18;n++) {
    await page.keyboard.press('Tab');
    const focused = await page.evaluate(() => {
      const el = document.activeElement;
      if (!(el instanceof HTMLElement)) return false;
      const rect = el.getBoundingClientRect();
      const style = getComputedStyle(el);
      return el !== document.body && rect.height>0 && rect.width>0 &&
        style.visibility!=='hidden' && style.display!=='none';
    });
    if (focused) {found=true;break;}
  }
  expect(found, 'Tab must focus a visible control').toBe(true);
}

test.describe('V24 C01-03.M04.05 actual tenant UI responsive and keyboard', () => {
  test.describe.configure({ retries: 0 });

  test('anonymous users cannot access guarded configuration', async ({ page }) => {
    await page.goto(target);
    await expect(page).toHaveURL(/login/);
    await expect(page.getByRole('heading', { name: 'Configuración dinámica' })).toHaveCount(0);
  });

  for (const [username, tenant] of [['c1504_admin_a',1],['c1504_admin_b',2]] as const) {
    test(username+' real A/B at five widths and zoom-stress', async ({ page }) => {
      test.setTimeout(240_000);
      const jwt = await login(page, username);
      const verified = await page.request.get(api+'/tenant-context/'+tenant, {
        headers:{Authorization:'Bearer '+jwt}
      });
      expect(verified.status()).toBe(200);
      await page.evaluate(id => localStorage.setItem('solqaryn_empresa_solicitada_id',String(id)),tenant);
      await page.goto(target);
      await expect(page.getByRole('heading',{name:'Configuración dinámica',exact:true})).toBeVisible();
      await expect(page.getByRole('heading',{name:'Configuración del tenant activo'})).toBeVisible();
      await expect(page.getByText('Empresa #'+tenant,{exact:false})).toBeVisible();
      await expect(page.getByText('Moneda ISO',{exact:true})).toBeVisible();
      await expect(page.getByText('Zona horaria IANA',{exact:true})).toBeVisible();
      await expect(page.getByRole('button',{name:/Recargar/})).toBeVisible();
      await expect(page.getByRole('link',{name:'Ver todos los ajustes'})).toBeVisible();
      await verifyTab(page);
      for (const width of widths) {
        await page.setViewportSize({width,height:900});
        await verifyLayout(page,width,'100pct');
        await page.screenshot({path:'test-results/c0103-m0405-'+username+'-'+width+'-100.png',fullPage:true});
      }
      // CSS zoom is only a 200% reflow stress PROXY, NOT native Chrome browser zoom.
      for (const width of [768,1280,1440]) {
        await page.setViewportSize({width,height:900});
        await page.evaluate(() => {document.documentElement.style.zoom='2';});
        await verifyLayout(page,width,'css-zoom-stress-200');
        await page.screenshot({path:'test-results/c0103-m0405-'+username+'-'+width+'-zoom-proxy.png',fullPage:true});
        await page.evaluate(() => {document.documentElement.style.zoom='';});
      }
    });
  }

  test('JWT company A cannot select B via forged header or storage',async ({page})=>{
    const jwt=await login(page,'c1504_admin_a');
    const rejected=await page.request.get(api+'/tenant-context/2',{
      headers:{Authorization:'Bearer '+jwt,'X-Empresa-Id':'2'}
    });
    expect([403,404]).toContain(rejected.status());
    await page.evaluate(()=>localStorage.setItem('solqaryn_empresa_solicitada_id','2'));
    await page.goto(target);
    await expect(page.getByRole('heading',{name:'Configuración dinámica'})).toHaveCount(0);
  });

  test('modified signed JWT rejected by real backend',async ({page})=>{
    const jwt=await login(page,'c1504_admin_a');
    const parts=jwt.split('.');
    parts[2]=(parts[2][0]==='A'?'B':'A')+parts[2].slice(1);
    const bad=await page.request.get(api+'/tenant-context/1',{
      headers:{Authorization:'Bearer '+parts.join('.')}
    });
    expect(bad.status()).toBe(401);
  });
});
