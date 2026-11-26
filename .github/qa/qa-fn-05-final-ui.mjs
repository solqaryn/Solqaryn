import {createRequire} from 'node:module';
import {resolve,join} from 'node:path';
import {selectReportTab} from './qa-fn-05-report-tabs.mjs';
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
  try{
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
  }catch(error){await context.close();throw error;}
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
  stage='CONFIG_TENANT_'+(global?'PLATFORM':identity.tenant===spec.a.tenant?'A':'B');
  const tenantName=identity.tenant===spec.a.tenant?spec.a.user:spec.b.user;
  await expect(page.locator('app-empresa-configuracion-tenant-card input[formcontrolname="nombre"]')).toHaveValue(tenantName);
  const heading=page.getByRole('heading',{name:'Identidad global de la plataforma',exact:true});
  if(global){
    stage='PLATFORM_GLOBAL_VISIBILITY';
    await expect(heading).toBeVisible();
    await expect(page.getByRole('heading',{name:'Empresas',exact:true})).toBeVisible();
    stage='PLATFORM_WRITE_CONTROLS_DENIED';
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
let failures=0;
function recordFailure(error){
  failures++;
  const reason=/^TAB_(?:GEOMETRY_NO_PROGRESS|SELECTION_NOT_CONFIRMED|HIT_TARGET_BLOCKED|PAGER_UNAVAILABLE|PAGINATION_LIMIT)$/.test(error?.message??'')?error.message:'ASSERTION_OR_INTERACTION';
  const kind=/^[A-Za-z0-9_]+$/.test(error?.name??'')?error.name:'Error';
  console.error('QA_FN05_UI_FAILED_STAGE='+stage+';CLASS='+kind+';REASON='+reason);
}
async function scenario(identity,viewport,label,work){
  stage='LOGIN_'+label;
  let context;
  try{
    const session=await login(identity,viewport);
    context=session.context;
    mark(label==='A'||label==='B'?stage:label+'_LOGIN');
    await work(session.page);
  }catch(error){recordFailure(error);}
  finally{if(context) await context.close();}
}
try{
  for(const [suffix,identity,foreign,viewport] of [['A',spec.a,spec.b.user,{width:1440,height:900}],['B',spec.b,spec.a.user,{width:390,height:844}]]){
    await scenario(identity,viewport,suffix,async page=>{
      for(const [path,heading,api] of [['/dashboard','Dashboard de empresa','dashboard/resumen'],['/productos','Productos','productos'],['/clientes','Clientes','clientes'],['/proveedores','Proveedores','proveedores'],['/usuarios','Usuarios','usuarios'],['/roles','Roles','roles'],['/permisos','Permisos por rol','permisos'],['/auditoria','Bitácora detallada','auditoria']]){
        await route(page,path,heading,api);await overflow(page);mark(stage+'_'+suffix);
      }
      stage='TENANT_CONFIG_'+suffix;
      await config(page,identity,false);mark('TENANT_CONFIG_'+suffix);
      stage='REPORTS_'+suffix;
      await reports(page,identity,suffix,foreign);mark('REPORTS_'+suffix);
    });
  }
  await scenario(spec.p,{width:1440,height:900},'PLATFORM',async page=>{
    stage='PLATFORM_READONLY';
    await config(page,spec.p,true);mark('PLATFORM_READONLY');
  });
  await scenario(spec.o,{width:1280,height:800},'OPERATOR',async page=>{
    stage='OPERATOR_REPORT_MENU_DENIED';
    const grants=page.waitForResponse(r=>r.url().startsWith(spec.base+'/api/permisos/mis-permisos')&&r.request().method()==='GET'&&r.status()===200,{timeout:30000});
    await Promise.all([grants,page.goto(spec.base+'/centro-reportes')]);
    await expect(page.getByRole('heading',{name:'Centro de Reportes',exact:true})).toBeVisible();
    await expect(page.getByRole('status').filter({hasText:'No tienes reportes habilitados'})).toBeVisible();
    await expect(page.getByRole('link',{name:'Reportes administrativos',exact:true})).toHaveCount(0);
    mark('OPERATOR_REPORT_MENU_DENIED');
  });
  if(failures===0) mark('AUTHENTICATED_ROUTES');
  else process.exitCode=1;
}catch(error){recordFailure(error);process.exitCode=1;}
finally{await browser.close();}
