import {test,expect,type Page} from '@playwright/test';
import {readFileSync} from 'node:fs';import {resolve} from 'node:path';
const inventory=JSON.parse(readFileSync(resolve(process.cwd(),'../docs/v5/c02-04-m05-01-routes.json'),'utf8')) as {schemaVersion:string;routes:Array<{id:string;path:string}>};
const cors={'Access-Control-Allow-Origin':'http://127.0.0.1:4200','Access-Control-Allow-Headers':'Authorization, Content-Type, X-Empresa-Id','Access-Control-Allow-Methods':'GET, OPTIONS'};
async function synthetic(page:Page,status:200|403|503){
 await page.addInitScript(()=>{localStorage.setItem('solqaryn_token','fixture-not-jwt');localStorage.setItem('solqaryn_expira_en','2099-01-01T00:00:00Z');});
 let gets=0;
 await page.route(/\/plataforma\/sesion(?:\?|$)/,async route=>{
  if(route.request().method()==='OPTIONS')return route.fulfill({status:204,headers:cors});
  gets++;
  return route.fulfill({status,headers:cors,contentType:'application/json',body:JSON.stringify(status===200
   ?{success:true,data:{nombreUsuario:'Fixture',rolNombre:'Global',ambito:'Plataforma',esAdministrador:true}}
   :{success:false,data:null,message:'SECRET_TEST_DIAGNOSTIC'})});
 });
 return ()=>gets;
}
test.describe('C02-04.M05.01 actual Chromium inventory, MOCK backend',()=>{
 test.describe.configure({retries:0});
 test('13 unique versioned IDs',()=>{
  expect(inventory.schemaVersion).toBe('C02-04.M05.01/v18.1');
  expect(inventory.routes).toHaveLength(13);
  expect(new Set(inventory.routes.map(x=>x.id)).size).toBe(13);
 });
 for(const item of inventory.routes){
  test(item.id+' anonymous blocked',async({page})=>{
   await page.goto('/'+item.path);
   await expect(page).toHaveURL(/\/login(?:\?|$)/);
   console.log(item.id+' ANON_BROWSER_DENIED');
  });
  test(item.id+' mocked 403 blocked',async({page})=>{
   const gets=await synthetic(page,403);
   await page.goto('/'+item.path);
   await expect(page).toHaveURL(/\/login\?plataforma=denegado(?:&|$)/);
   expect(gets()).toBeGreaterThanOrEqual(1);
   await expect(page.getByText('SECRET_TEST_DIAGNOSTIC')).toHaveCount(0);
   console.log(item.id+' MOCK_403_BROWSER_DENIED');
  });
 }
 test('GV07 mocked 200 read-only',async({page})=>{
  const gets=await synthetic(page,200);
  await page.goto('/plataforma/consola-global-v5');
  await expect(page.getByRole('heading',{name:'Consola global auditada'})).toBeVisible();
  await expect(page.locator('.control-cards li')).toHaveCount(4);
  await expect(page.getByText(/JWT HTTP y auditoría durable: NO_MEDIDO/)).toBeVisible();
  expect(gets()).toBeGreaterThanOrEqual(1);
 });
 test('GV07 mocked 503 fail-closed',async({page})=>{
  const gets=await synthetic(page,503);
  await page.goto('/plataforma/consola-global-v5');
  await expect(page).toHaveURL(/\/login\?plataforma=no-disponible(?:&|$)/);
  expect(gets()).toBeGreaterThanOrEqual(1);
  await expect(page.getByText('SECRET_TEST_DIAGNOSTIC')).toHaveCount(0);
 });
});
