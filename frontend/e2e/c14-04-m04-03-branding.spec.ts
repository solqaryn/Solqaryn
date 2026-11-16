import { expect, test, type Page } from '@playwright/test';

/** Sesión de plataforma simulada: no prueba JWT, MySQL, DNS, backups ni PROD. */
async function mockPlatform(page: Page, status: 200 | 403) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token','c1404-branding-synthetic-not-jwt');
    localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    const headers = {
      'Access-Control-Allow-Origin':'http://127.0.0.1:4200',
      'Access-Control-Allow-Headers':'Authorization, Content-Type, X-Empresa-Id',
      'Access-Control-Allow-Methods':'GET, OPTIONS'
    };
    if (route.request().method() === 'OPTIONS') return route.fulfill({status:204,headers});
    return route.fulfill({status,headers,contentType:'application/json',
      body:JSON.stringify(status===200
        ? {success:true,data:{nombreUsuario:'Admin',rolNombre:'Admin',
            ambito:'Plataforma',esAdministrador:true}}
        : {success:false,data:null,message:'PRIVATE_BRANDING_DIAGNOSTIC'})});
  });
}

test('C14-04.M04.03 anónimo no accede a preferencias release', async ({page}) => {
  await page.goto('/plataforma/flujo-v5');
  await expect(page).toHaveURL(/\/login(?:\?.*)?$/);
  await expect(page.getByTestId('release-branding-v17')).toHaveCount(0);
});
test('C14-04.M04.03 autoridad sintética alterna presets a 320 sin HTTP mutador', async ({page}) => {
  await mockPlatform(page,200);
  await page.setViewportSize({width:320,height:720});
  const mutations: string[]=[];
  page.on('request',req => {
    if (!['GET','OPTIONS'].includes(req.method())) mutations.push(req.method());
  });
  await page.goto('/plataforma/flujo-v5');
  const p=page.getByTestId('release-branding-v17');
  await expect(p).toBeVisible();
  await expect(p).toHaveAttribute('data-density','comoda');
  await p.getByRole('button',{name:'Densidad compacta'}).click();
  await expect(p).toHaveAttribute('data-density','compacta');
  await p.getByRole('button',{name:'Contraste alto'}).click();
  await expect(p).toHaveAttribute('data-contrast','alto');
  await expect(p).toContainText('NO_MEDIDO');
  expect(await page.evaluate(() => document.documentElement.scrollWidth-
    document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
  expect(mutations).toEqual([]);
});
test('C14-04.M04.03 denegación 403 no revela controles ni diagnóstico', async ({page}) => {
  await mockPlatform(page,403);
  await page.goto('/plataforma/flujo-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=denegado/);
  await expect(page.getByTestId('release-branding-v17')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_BRANDING_DIAGNOSTIC')).toHaveCount(0);
});
