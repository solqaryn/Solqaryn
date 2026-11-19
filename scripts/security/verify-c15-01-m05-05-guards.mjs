#!/usr/bin/env node
// C15-01.M05.05 R2: read-only source regression checks and adversarial mutations.
// This is NOT signed JWT/RBAC/tenant A/B or API security certification.
import { readFileSync } from 'node:fs';
const paths={
  routes:'frontend/src/app/app.routes.ts',
  auth:'frontend/src/app/core/guards/auth.guard.ts',
  permission:'frontend/src/app/core/guards/permiso.guard.ts',
  platform:'frontend/src/app/core/guards/plataforma.guard.ts'
};
const read=Object.fromEntries(Object.entries(paths).map(([k,p])=>[k,readFileSync(p,'utf8')]));
function must(x,reason){if(!x)throw Error('C15-01.M05.05 SECURITY FAIL: '+reason);}
function routeLine(src,path){
  const match=src.split('\n').find(line=>line.includes("path: '"+path+"'"));
  must(!!match,'protected route missing: '+path);
  return match;
}
export function checkSources(s) {
  const platformRoutes=s.routes.split('\n').filter(x=>/\bpath:\s*'plataforma\//.test(x));
  must(platformRoutes.length>=20,'platform route census shrank unexpectedly');
  for(const line of platformRoutes)must(line.includes('canActivate: [plataformaGuard]'),
    'unguarded platform path: '+line.match(/path:\s*'([^']+)'/)?.[1]);
  const sensitive={
    dashboard:'Dashboard',usuarios:'Usuarios',ventas:'Ventas',compras:'Compras',
    'inventario/movimientos':'MovimientosInventario',configuracion:'Configuracion'
  };
  for(const [path,modulo] of Object.entries(sensitive)){
    const line=routeLine(s.routes,path);
    must(line.includes('canActivate: [authGuard, permisoGuard]'),'missing auth/permiso guard: '+path);
    must(line.includes("modulo: '"+modulo+"'"),'wrong module permission: '+path);
    must(line.includes("accion: 'Ver'"),'wrong action for '+path);
  }
  must(/path:\s*'centro-reportes'\s*,\s*canActivate:\s*\[authGuard\]/.test(s.routes),
    'report parent missing auth guard');
  must(s.auth.includes('tenantContext.tieneContextoVerificado()') &&
    s.auth.includes('tenantContext.seleccionarEmpresa(empresaSolicitadaId)') &&
    s.auth.includes("router.parseUrl('/login')"),'tenant revalidation or fail-closed missing');
  must(s.permission.includes('permisosRuntime.puede(modulo, accion)') &&
    s.permission.includes('permisosRuntime.rutaHabilitada(rutaCompleta)') &&
    s.permission.includes('catchError('),'permission route/capability or failure reject missing');
  must(s.platform.includes('auth.isAuthenticated()') &&
    s.platform.includes('plataforma.obtener()') &&
    s.platform.includes('error?.status === 403') &&
    s.platform.includes("router.createUrlTree(['/login']"),'platform session or 403 reject missing');
  return {platformRouteCount:platformRoutes.length,erpProtectedRoutes:Object.keys(sensitive).length,
    reportParentGuarded:true,tenantSessionChecked:true,routePermissionChecked:true};
}
function selfTest() {
  const baseline=checkSources(read);
  const cases=[
    ['platform route guard removed',s=>{s.routes=s.routes.replace("path: 'plataforma/cobertura-v5', canActivate: [plataformaGuard]","path: 'plataforma/cobertura-v5'");}],
    ['forged public platform route',s=>{s.routes=s.routes.replace("export const routes: Routes = [","export const routes: Routes = [\n { path: 'plataforma/no-guard-forgery', loadComponent: () => null },");}],
    ['erp auth guard removed',s=>{s.routes=s.routes.replace("path: 'dashboard', canActivate: [authGuard, permisoGuard]","path: 'dashboard'");}],
    ['erp module role changed',s=>{s.routes=s.routes.replace("modulo: 'Usuarios', accion: 'Ver'","modulo: 'Publico', accion: 'Ver'");}],
    ['route authorization bypassed',s=>{s.permission=s.permission.replace('permisosRuntime.rutaHabilitada(rutaCompleta)','true');}],
    ['tenant revalidation removed',s=>{s.auth=s.auth.replace('tenantContext.seleccionarEmpresa(empresaSolicitadaId)','tenantContext.fakeEmpresa(empresaSolicitadaId)');}],
    ['platform 403 denial removed',s=>{s.platform=s.platform.replace('error?.status === 403','false');}],
    ['error denial removed',s=>{s.permission=s.permission.replace('catchError(', 'noCatchError(');}]
  ];
  let caught=0;
  for(const [name,mutate] of cases) {
    const x={...read}; mutate(x);
    must(Object.keys(read).some(k=>read[k]!==x[k]),'mutation did nothing: '+name);
    let rejected=false;try{checkSources(x)}catch{rejected=true;}
    must(rejected,'unsafe source mutation escaped: '+name);
    caught++;
  }
  console.log('C15-01.M05.05 POSITIVE STATIC PASS '+JSON.stringify(baseline));
  console.log('C15-01.M05.05 NEGATIVE STATIC PASS '+caught+'/'+cases.length+
    ' guard, role, tenant context, route bypass and denial mutations rejected');
  console.log('LIMIT: no live HTTP/JWT signature, tenant A/B, authorization backend or MySQL isolation tested');
}
if(process.argv.includes('--self-test'))selfTest();
else console.log('C15-01.M05.05 STATIC PASS '+JSON.stringify(checkSources(read)));
