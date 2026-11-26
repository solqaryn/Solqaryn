import {createRequire} from 'node:module';
import {resolve,join} from 'node:path';
const require=createRequire(resolve('frontend/package.json'));
const {chromium}=require('playwright');
const {expect}=require('@playwright/test');
const chunks=[];
for await (const chunk of process.stdin) chunks.push(chunk);
const spec=JSON.parse(Buffer.concat(chunks).toString('utf8'));
if(spec.base!=='https://solqarynqa-nu.vercel.app'||process.env.GITHUB_REF!=='refs/heads/qa') throw new Error('QA browser scope');
const browser=await chromium.launch({headless:true});
let stage='START';
function mark(label){stage=label;console.log('QA_FN05_UI_'+label+'=PASS');}
async function login(identity,viewport){
  const context=await browser.newContext({viewport,acceptDownloads:true});
  const page=await context.newPage();
  page.setDefaultTimeout(20000);
  await page.goto(spec.base+'/login');
  await page.locator('input[autocomplete="username"]').fill(identity.user);
  await page.locator('input[autocomplete="current-password"]').fill(identity.password);
  await page.locator('button[type="submit"]').click();
  await page.locator('#empresa-id').fill(String(identity.tenant));
  await page.getByRole('button',{name:'Entrar a la empresa'}).click();
  await page.waitForURL(url=>url.pathname!=='/login',{timeout:30000});
  return {context,page};
}
async function route(page,path,heading,api){
  stage='ROUTE_'+path.toUpperCase().replace(/[^A-Z0-9]+/g,'_');
  const read=page.waitForResponse(r=>r.url().startsWith(spec.base+'/api/'+api)&&r.request().method()==='GET'&&r.status()===200,{timeout:30000});
  await Promise.all([read,page.goto(spec.base+path)]);
  await expect(page.getByRole('heading',{name:heading,exact:true})).toBeVisible();
}
async function overflow(page){
  await expect.poll(()=>page.evaluate(()=>Math.max(document.documentElement.scrollWidth,document.body.scrollWidth)-document.documentElement.clientWidth),{timeout:5000}).toBeLessThanOrEqual(2);
}
async function selectReportTab(page,name){
  const group=page.locator('mat-tab-group.reports-tabs');
  const tab=group.getByRole('tab',{name,exact:true});
  const next=group.locator('.mat-mdc-tab-header-pagination-after');
  await expect(tab).toBeAttached();
  // A tab can exist in the DOM while its center is clipped by Material's mobile header.
  // Use the same pagination control a touch user uses, never force a click.
  const centerVisible=()=>tab.evaluate(el=>{
    const clip=el.closest('.mat-mdc-tab-label-container');
    if(!clip) return true;
    const rect=el.getBoundingClientRect(), viewport=clip.getBoundingClientRect();
    const center=rect.left+rect.width/2;
    return rect.width>0 && center>=viewport.left+1 && center<=viewport.right-1;
  });
  if(!await centerVisible() && await next.isVisible()){
    await expect(next).not.toHaveClass(/mat-mdc-tab-header-pagination-disabled/);
    await next.click();
    await expect.poll(centerVisible,{timeout:5000}).toBe(true);
  }
  await tab.click();
  await expect(tab).toHaveAttribute('aria-selected','true');
}
async function tableOverflow(page,table){
  await expect(table).toBeVisible();
  const wrap=table.locator('..');
  await expect.poll(()=>wrap.evaluate(el=>{
    const rect=el.getBoundingClientRect(), width=document.documentElement.clientWidth;
    return rect.width>0 && rect.left>=-2 && rect.right<=width+2 &&
      (el.scrollWidth<=el.clientWidth+2 || ['auto','scroll'].includes(getComputedStyle(el).overflowX));
  }),{timeout:5000}).toBe(true);
  await overflow(page);
}
async function config(page,identity,global){
  await route(page,'/configuracion','Configuración de la empresa','empresa-configuracion/plataforma/capacidades');
  const tenantName=identity.tenant===spec.a.tenant?spec.a.user:spec.b.user;
  await expect(page.locator('app-empresa-configuracion-tenant-card input[formcontrolname="nombre"]')).toHaveValue(tenantName);
  const heading=page.getByRole('heading',{name:'Identidad global de la plataforma',exact:true});
  if(global){
    await expect(heading).toBeVisible();
    await expect(page.getByRole('heading',{name:'Empresas',exact:true})).toBeVisible();
    await expect(page.getByRole('button',{name:'Guardar configuración',exact:true})).toHaveCount(0);
  }else{
    await expect(heading).toHaveCount(0);
    await expect(page.locator('app-empresa-administracion-card')).toHaveCount(0);
  }
  await overflow(page);
}
async function reports(page,identity,suffix,foreign){
  await route(page,'/centro-reportes/administrativos','Reportes administrativos','reportes-administrativos/usuarios-accesos');
  const table=page.getByRole('table',{name:'Reporte de usuarios y accesos'});
  await expect(table.getByText(identity.user,{exact:true}).first()).toBeVisible();
  await expect(table.getByText(foreign,{exact:true})).toHaveCount(0);
  stage='USERS_OVERFLOW_'+suffix;
  await tableOverflow(page,table);
  mark(stage);
  for(const [fmt,label] of [['csv','CSV'],['xlsx','Excel'],['pdf','Descargar reporte de usuarios en PDF']]){
    stage='DOWNLOAD_'+suffix+'_'+fmt.toUpperCase();
    const button=page.locator('.export-actions').getByRole('button',{name:label,exact:fmt==='pdf'});
    const [download]=await Promise.all([page.waitForEvent('download',{timeout:60000}),button.click()]);
    if(!download.suggestedFilename().toLowerCase().endsWith('.'+fmt)) throw new Error('Download extension');
    await download.saveAs(join(spec.out,suffix+'-usuarios.'+fmt));
    if(await download.failure()) throw new Error('Download failed');
    mark(stage);
  }
  stage='ROLES_TAB_'+suffix;
  await selectReportTab(page,'Roles y permisos');
  mark(stage);
  stage='ROLES_CONTENT_'+suffix;
  const roles=page.getByRole('table',{name:'Reporte de roles y permisos',exact:true});
  await expect(roles.getByText(identity.user,{exact:true})).toBeVisible();
  await expect(roles.getByText(foreign,{exact:true})).toHaveCount(0);
  mark(stage);
  stage='ROLES_OVERFLOW_'+suffix;
  await tableOverflow(page,roles);
  mark(stage);
  stage='AUDIT_TAB_'+suffix;
  await selectReportTab(page,'Actividad de auditoría');
  mark(stage);
  stage='AUDIT_CONTENT_'+suffix;
  const audit=page.getByRole('table',{name:'Actividad de auditoría por módulo',exact:true});
  await expect(audit).toBeVisible();
  mark(stage);
  stage='AUDIT_OVERFLOW_'+suffix;
  await tableOverflow(page,audit);
  mark(stage);
}
try{
  for(const [suffix,identity,foreign,viewport] of [['A',spec.a,spec.b.user,{width:1440,height:900}],['B',spec.b,spec.a.user,{width:390,height:844}]]){
    stage='LOGIN_'+suffix;
    const {context,page}=await login(identity,viewport);
    mark(stage);
    for(const [path,heading,api] of [['/dashboard','Dashboard de empresa','dashboard/resumen'],['/productos','Productos','productos'],['/clientes','Clientes','clientes'],['/proveedores','Proveedores','proveedores'],['/usuarios','Usuarios','usuarios'],['/roles','Roles','roles'],['/permisos','Permisos por rol','permisos'],['/auditoria','Bitácora detallada','auditoria']]){
      await route(page,path,heading,api);await overflow(page);mark(stage+'_'+suffix);
    }
    await config(page,identity,false);mark('TENANT_CONFIG_'+suffix);
    await reports(page,identity,suffix,foreign);mark('REPORTS_'+suffix);
    await context.close();
  }
  stage='PLATFORM_LOGIN';
  const p=await login(spec.p,{width:1440,height:900});mark('PLATFORM_LOGIN');
  stage='PLATFORM_READONLY';
  await config(p.page,spec.p,true);mark('PLATFORM_READONLY');await p.context.close();
  stage='OPERATOR_LOGIN';
  const o=await login(spec.o,{width:1280,height:800});mark('OPERATOR_LOGIN');
  stage='OPERATOR_REPORT_MENU_DENIED';
  await o.page.goto(spec.base+'/centro-reportes');
  await expect(o.page.getByRole('heading',{name:'Centro de Reportes',exact:true})).toBeVisible();
  await expect(o.page.getByRole('link',{name:'Reportes administrativos',exact:true})).toHaveCount(0);
  await o.context.close();mark('OPERATOR_REPORT_MENU_DENIED');mark('AUTHENTICATED_ROUTES');
}catch(error){console.error('QA_FN05_UI_FAILED_STAGE='+stage+';CLASS='+String(error?.name??'Error'));process.exitCode=1;}
finally{await browser.close();}
