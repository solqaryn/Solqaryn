import { test, expect, type Page } from '@playwright/test';

const url = '/plataforma/experiencia-visual-v5';
async function simular(page: Page, ultimoStatus = 200) {
  await page.addInitScript(() => {
    localStorage.setItem('solqaryn_token', 'c1304-m0402-synthetic-only');
    localStorage.setItem('solqaryn_expira_en', '2099-01-01T00:00:00Z');
  });
  let consultas = 0;
  const mutaciones: string[] = [];
  page.on('request', request => {
    if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method())) mutaciones.push(request.method());
  });
  await page.route(/\/plataforma\/sesion(?:\?|$)/, async route => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status:204, headers:{'Access-Control-Allow-Origin':'http://127.0.0.1:4200',
        'Access-Control-Allow-Headers':'Authorization, Content-Type, X-Empresa-Id',
        'Access-Control-Allow-Methods':'GET, OPTIONS'} });
      return;
    }
    consultas++;
    const status = consultas === 1 ? 200 : ultimoStatus;
    await route.fulfill({ status, contentType:'application/json',
      headers:{'Access-Control-Allow-Origin':'http://127.0.0.1:4200'},
      body:JSON.stringify(status === 200
        ? {success:true,data:{nombreUsuario:'Synthetic',rolNombre:'Global',ambito:'Plataforma',esAdministrador:true}}
        : {success:false,message:'PRIVATE_TENANT_DSN'}) });
  });
  return {consultas:()=>consultas,mutaciones};
}

test('anonimo no accede al estado global ni a controles',async({page})=>{
  await page.goto(url);
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByTestId('c1304-m0402-panel')).toHaveCount(0);
});

test('sesion global sintetica: vacio -> exito solo de sesion, catalogo SIN_VERIFICAR',async({page})=>{
  const probe=await simular(page);
  await page.setViewportSize({width:320,height:812});
  await page.goto(url);
  await expect(page.getByTestId('c1304-m0402-state')).toContainText('Vacío');
  await page.getByTestId('c1304-m0402-revalidate').click();
  await expect(page.getByTestId('c1304-m0402-state')).toContainText('Éxito: sesión global');
  await expect(page.getByTestId('c1304-m0402-panel')).toContainText('SIN_VERIFICAR');
  expect(probe.consultas()).toBe(2);
  expect(probe.mutaciones).toHaveLength(0);
  expect(await page.evaluate(()=>document.documentElement.scrollWidth-document.documentElement.clientWidth)).toBeLessThanOrEqual(2);
  let found=false;
  for(let i=0;i<80;i++){
    await page.keyboard.press('Tab');
    if(await page.getByTestId('c1304-m0402-revalidate').evaluate(e=>document.activeElement===e)){found=true;break;}
  }
  expect(found).toBe(true);
});

test('503 sintetico al revalidar: mensaje opaco y controles privados ocultos',async({page})=>{
  await simular(page,503);
  await page.goto(url);
  await page.getByTestId('c1304-m0402-revalidate').click();
  await expect(page.getByTestId('c1304-m0402-state')).toContainText('Error');
  await expect(page.getByTestId('c1304-m0402-revalidate')).toHaveCount(0);
  await expect(page.locator('.visual-actions button')).toHaveCount(0);
  await expect(page.getByText('PRIVATE_TENANT_DSN')).toHaveCount(0);
});
