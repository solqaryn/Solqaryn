// SOLQARYN P1-004.B — DEV live JWT/API/MySQL certification (no new users/memberships).
// Writes are limited to the explicit delegation API and its audit; final revoke is mandatory.
import assert from 'node:assert/strict';
import {execFileSync} from 'node:child_process';
import {request as httpsRequest} from 'node:https';

const ORIGIN='https://solqaryn-api-dev-fxx8.onrender.com';
const EXPECTED_DB={host:'solqaryn-mysql-solqaryn.h.aivencloud.com',port:'14402',
  name:'solqaryn_dev',user:'solqaryn_dev_user'};
const ROLE='DELEGADO_PLATAFORMA_CONSULTA';
const PERM='PLATAFORMA.DELEGACION.EMPRESAS_VER';
const fail=code=>{throw Error('P1004B_NO_GO='+code);};
const must=(condition,code)=>{if(!condition)fail(code);};
const positive=(x,code)=>{const n=Number(x);must(Number.isSafeInteger(n)&&n>0,code);return n;};
const dataOf=json=>json?.data??json?.Data??json;
const field=(obj,key)=>dataOf(obj)?.[key]??dataOf(obj)?.[key[0].toUpperCase()+key.slice(1)];
const literal=value=>"CONVERT(UNHEX('"+Buffer.from(value,'utf8').toString('hex')+"') USING utf8mb4)";
const reasonSuffix=()=>String(process.env.GITHUB_SHA||'').slice(0,12);

function selfTest(){
  assert.equal(positive('42','X'),42);
  assert.equal(field({data:{token:'synthetic'}},'token'),'synthetic');
  assert.equal(field({Data:{UsuarioId:3}},'usuarioId'),3);
  assert.equal(literal('abc'),"CONVERT(UNHEX('616263') USING utf8mb4)");
  assert.equal(ORIGIN,'https://solqaryn-api-dev-fxx8.onrender.com');
  console.log('P1004B_OFFLINE_CONTRACT=PASS');
}
function preflightEnv(){
  must(process.env.GITHUB_REPOSITORY==='solqaryn/Solqaryn','REPOSITORY');
  must(process.env.GITHUB_REF==='refs/heads/dev','BRANCH');
  must(/^[a-f0-9]{40}$/.test(process.env.GITHUB_SHA||''),'COMMIT_SHA');
  must(process.env.API_ORIGIN===ORIGIN,'API_ORIGIN');
  for(const [k,v] of Object.entries(EXPECTED_DB))
    must(process.env[{host:'DB_HOST',port:'DB_PORT',name:'DB_NAME',user:'DB_USER'}[k]]===v,'DB_'+k.toUpperCase());
  for(const k of ['DB_PASSWORD','PLATFORM_PASSWORD','BUSINESS_PASSWORD'])
    must(typeof process.env[k]==='string'&&process.env[k].length>0,'MISSING_'+k);
  must(process.env.PLATFORM_USERNAME==='javier','PLATFORM_IDENTITY');
  must(process.env.BUSINESS_USERNAME==='admin_prueba_a','BUSINESS_IDENTITY');
}
function sql(query){
  // Every variable in statements is a strict positive integer or escaped UTF8 hex.
  const args=['--protocol=TCP','-h',process.env.DB_HOST,'-P',process.env.DB_PORT,
    '-u',process.env.DB_USER,'--ssl-mode=REQUIRED','--connect-timeout=12',
    process.env.DB_NAME,'--batch','--skip-column-names','-e',query];
  try {return execFileSync('mysql',args,{
      encoding:'utf8',timeout:30000,maxBuffer:8192,
      env:{...process.env,MYSQL_PWD:process.env.DB_PASSWORD}
    }).trim();
  }catch{fail('MYSQL_SELECT_UNAVAILABLE');}
}
function count(query,code){const n=sql(query);must(/^\d+$/.test(n),code);return Number(n);}
function identity(name){
  const where='BINARY NombreUsuario=BINARY '+literal(name)+' AND Activo=1 AND Bloqueado=0 AND Eliminado=0';
  const n=count('SELECT COUNT(*) FROM Usuarios WHERE '+where,'IDENTITY_COUNT');
  must(n===1,'TEST_IDENTITY_NOT_UNIQUE');
  return positive(sql('SELECT Id FROM Usuarios WHERE '+where+' LIMIT 1'),'IDENTITY_ID');
}
function preflightDb(){
  must(sql('SELECT DATABASE()')===EXPECTED_DB.name,'WRONG_DATABASE');
  must(sql("SELECT SUBSTRING_INDEX(CURRENT_USER(),'@',1)")===EXPECTED_DB.user,'WRONG_DB_PRINCIPAL');
  must(count('SELECT COUNT(*) FROM Empresas WHERE Id IN (2,3) AND Activa=1','TWO_TENANTS')===2,'TWO_ACTIVE_TENANTS');
  const platformId=identity(process.env.PLATFORM_USERNAME);
  const targetId=identity(process.env.BUSINESS_USERNAME);
  must(platformId!==targetId,'IDENTITIES_MUST_DIFFER');
  must(count('SELECT COUNT(*) FROM UsuarioRolesPlataforma g JOIN Roles r ON r.Id=g.RolId '+
    'WHERE g.UsuarioId='+platformId+' AND g.Activa=1 AND r.Activo=1 AND r.Eliminado=0 '+
    'AND r.EsSistema=1 AND r.EsAdministrador=1 AND r.Ambito=2 AND r.EmpresaId IS NULL','SUPER_SCOPE')===1,'PLATFORM_SCOPE_MISSING');
  must(count('SELECT COUNT(*) FROM UsuarioRolesPlataforma WHERE UsuarioId='+targetId+' AND Activa=1','TARGET_SCOPE')===0,'BUSINESS_ALREADY_GLOBAL');
  const tenantMembership=count('SELECT COUNT(*) FROM UsuarioEmpresas WHERE UsuarioId='+targetId,'MEMBERSHIPS');
  must(tenantMembership>0,'TARGET_NOT_EXISTING_BUSINESS_USER');
  must(count('SELECT COUNT(*) FROM UsuarioEmpresas WHERE UsuarioId='+targetId+
    ' AND EmpresaId=2 AND Activa=1','TENANT_A')>=1,'BUSINESS_TENANT_A_REQUIRED');
  const role=count('SELECT COUNT(*) FROM Roles WHERE NombreNormalizado='+literal(ROLE),'ROLE_EXISTENCE');
  const permission=count('SELECT COUNT(*) FROM Permisos WHERE Codigo='+literal(PERM),'PERM_EXISTENCE');
  must(role<=1&&permission<=1,'GLOBAL_RBAC_AMBIGUOUS');
  console.log('P1004B_PLATFORM_TENANT_MYSQL_PREFLIGHT=PASS');
  return {platformId,targetId,tenantMembership};
}
async function call(path,method='GET',jwt='',payload=undefined){
  const url=new URL(path,ORIGIN);
  must(url.origin===ORIGIN&&url.protocol==='https:','ORIGIN_MISMATCH');
  const body=payload===undefined?null:JSON.stringify(payload);
  const h={'Accept':'application/json','User-Agent':'SOLQARYN-P1004B-LIVE',
    'Cache-Control':'no-store'};
  if(jwt)h.Authorization='Bearer '+jwt;
  if(body!==null){h['Content-Type']='application/json';h['Content-Length']=Buffer.byteLength(body);}
  return new Promise((resolve,reject)=>{
    const req=httpsRequest(url,{method,headers:h,timeout:30000},res=>{
      let raw='';res.setEncoding('utf8');
      res.on('data',s=>{raw+=s;if(raw.length>250000)req.destroy(Error('TOO_MUCH_DATA'));});
      res.on('end',()=>{
        let json=null;try{json=JSON.parse(raw);}catch{}
        resolve({status:res.statusCode,json,headers:res.headers});
      });
    });
    req.on('timeout',()=>req.destroy(Error('TIMEOUT')));
    req.on('error',()=>reject(Error('NETWORK_OR_TIMEOUT')));
    req.end(body??undefined);
  });
}
const expect=(response,status,step)=>must(response.status===status,step+'_HTTP_'+response.status);
async function login(name,pw,label){
  const r=await call('/auth/login','POST','',{nombreUsuario:name,password:pw});
  expect(r,200,'LOGIN_'+label);
  const token=field(r.json,'token');
  must(typeof token==='string'&&token.length>30,'JWT_'+label);
  return token;
}
function checkDirectory(response){
  expect(response,200,'DELEGATED_DIRECTORY');
  must(String(response.headers['cache-control']||'').includes('no-store'),'NO_STORE_MISSING');
  const rows=dataOf(response.json);
  must(Array.isArray(rows)&&rows.length>=2&&rows.length<=50,'DIRECTORY_SHAPE');
  must(rows.every(x=>x&&typeof x==='object'&&
    Object.keys(x).sort().join(',')==='empresaId,nombre'&&
    Number.isSafeInteger(x.empresaId)&&x.empresaId>0&&
    typeof x.nombre==='string'),'DIRECTORY_LEAK_OR_INVALID_DATA');
}
async function revoke(jwt,uid,reason){
  const result=await call('/plataforma/delegaciones/'+uid,'DELETE',jwt,{motivo:reason});
  expect(result,200,'REVOKE');
}
function readback({platformId,targetId,tenantMembership},grantReason,revokeReason){
  const roles=' FROM UsuarioRolesPlataforma g JOIN Roles r ON r.Id=g.RolId WHERE g.UsuarioId='+targetId+
    ' AND r.NombreNormalizado='+literal(ROLE);
  must(count('SELECT COUNT(*)'+roles+' AND g.Activa=0','REVOKED_SCOPE')===1,'REVOCATION_NOT_DURABLE');
  must(count('SELECT COUNT(*)'+roles+' AND g.Activa=1','NO_ACTIVE_SCOPE')===0,'DELEGATED_SCOPE_STILL_ACTIVE');
  must(count('SELECT COUNT(*) FROM UsuarioRolesPlataforma g JOIN Roles r ON r.Id=g.RolId '+
    'WHERE g.UsuarioId='+targetId+' AND g.Activa=1 AND r.EsSistema=1 AND r.Ambito=2','NO_SUPERADMIN')===0,'ACCIDENTAL_SUPERADMIN');
  must(count('SELECT COUNT(*) FROM RolPermisos rp JOIN Roles r ON r.Id=rp.RolId '+
    'JOIN Permisos p ON p.Id=rp.PermisoId WHERE r.NombreNormalizado='+literal(ROLE)+
    ' AND r.EsSistema=0 AND r.Ambito=2 AND p.Codigo='+literal(PERM)+
    ' AND p.Ambito=2 AND p.Modulo=13 AND p.Accion=20 AND p.Activo=1 AND p.Eliminado=0','EXACT_GRANT')===1,'GRANT_NOT_EXACT');
  must(count('SELECT COUNT(*) FROM UsuarioEmpresas WHERE UsuarioId='+targetId,'MEMBERSHIP_AFTER')===tenantMembership,'BUSINESS_MEMBERSHIP_MODIFIED');
  const audit=" FROM RegistrosAuditoria WHERE Entidad='UsuarioRolPlataforma' "+
    "AND AmbitoAutoridad=2 AND OrigenAutorizacion=2 AND EmpresaId IS NULL";
  must(count('SELECT COUNT(*)'+audit+' AND UsuarioId='+platformId+
    ' AND ReferenciaId='+targetId+' AND Accion=17 AND Motivo='+literal(grantReason)+
    " AND Resultado='Exito'",'GRANT_AUDIT')===1,'GRANT_AUDIT_NOT_DURABLE');
  must(count('SELECT COUNT(*)'+audit+' AND UsuarioId='+platformId+
    ' AND ReferenciaId='+targetId+' AND Accion=9 AND Motivo='+literal(revokeReason)+
    " AND Resultado='Exito'",'REVOKE_AUDIT')===1,'REVOKE_AUDIT_NOT_DURABLE');
  must(count('SELECT COUNT(*)'+audit+' AND UsuarioId='+targetId+
    " AND Accion=20 AND Resultado='Exito' AND Motivo="+literal('Lectura delegada explícita')+
    ' AND Fecha >= UTC_TIMESTAMP() - INTERVAL 30 MINUTE','READ_AUDIT')>=1,'READ_AUDIT_NOT_DURABLE');
  console.log('P1004B_MYSQL_REVOCATION_AND_DURABLE_AUDIT=PASS');
}
async function run(){
  preflightEnv();
  const context=preflightDb();
  const ready=await call('/health/ready');
  expect(ready,200,'RENDER_DEV_READY');
  const superJwt=await login(process.env.PLATFORM_USERNAME,process.env.PLATFORM_PASSWORD,'PLATFORM');
  const businessJwt=await login(process.env.BUSINESS_USERNAME,process.env.BUSINESS_PASSWORD,'BUSINESS');
  expect(await call('/plataforma/sesion','GET',superJwt),200,'SUPERADMIN');
  expect(await call('/plataforma/sesion','GET',businessJwt),403,'NO_PLATFORM_PRE_GRANT');
  // El push protegido puede iniciar antes de finalizar el despliegue en Render.
  // Reintentar SOLO la respuesta 503 específica del gate antiguo; jamás
  // interpretar un 503 como autorización correcta ni conceder grants antes.
  let before=await call('/plataforma/delegaciones/empresas','GET',businessJwt);
  for(let attempt=0;attempt<18 && before.status===503 &&
      before.json?.code==='TENANT_DATA_ISOLATION_REQUIRED';attempt++){
    if(attempt===0)console.log('P1004B_WAIT_EXACT_DEV_GATE_ROLLOUT=BOUNDED');
    await new Promise(resolve=>setTimeout(resolve,10000));
    before=await call('/plataforma/delegaciones/empresas','GET',businessJwt);
  }
  if(before.status===404)fail('DEV_RENDER_NOT_DEPLOYED_TO_P1004A');
  expect(before,403,'PRE_GRANT_DENIED');
  expect(await call('/plataforma/delegaciones','POST',businessJwt,
    {usuarioId:context.targetId,motivo:'P1004B negative business self grant'}),403,'BUSINESS_SELF_GRANT_DENIED');
  expect(await call('/plataforma/delegaciones','POST',superJwt,
    {usuarioId:context.platformId,motivo:'P1004B negative super self grant'}),403,'SUPER_SELF_GRANT_DENIED');
  console.log('P1004B_HTTP_JWT_NEGATIVE_PREFLIGHT=PASS');

  const grantReason='P1004B DEV grant '+reasonSuffix();
  const revokeReason='P1004B DEV revoke '+reasonSuffix();
  let granted=false,revoked=false;
  try{
    const grant=await call('/plataforma/delegaciones','POST',superJwt,
      {usuarioId:context.targetId,motivo:grantReason});
    expect(grant,201,'GRANT');
    must(String(grant.headers['cache-control']||'').includes('no-store'),'GRANT_CACHE');
    granted=true;
    must(Number(grant.json?.usuarioId)===context.targetId,'GRANT_USER_MISMATCH');
    must(grant.json?.capacidad===PERM,'WRONG_GRANT');
    checkDirectory(await call('/plataforma/delegaciones/empresas','GET',businessJwt));
    expect(await call('/plataforma/sesion','GET',businessJwt),403,'NO_SUPERADMIN_AFTER_GRANT');
    expect(await call('/plataforma/gestion/roles?empresaId=2','GET',businessJwt),403,'NO_GLOBAL_MANAGEMENT');
    expect(await call('/plataforma/gestion/empresas/3/membresias','GET',businessJwt),403,'NO_CROSS_TENANT_MANAGEMENT');
    expect(await call('/plataforma/delegaciones','POST',businessJwt,
      {usuarioId:context.targetId,motivo:'P1004B delegate may not elevate self'}),403,'DELEGATE_SELF_ESCALATION_DENIED');
    expect(await call('/plataforma/soporte/sesiones','POST',businessJwt,
      {empresaId:2,modulo:2,motivo:'P1004B no support escalation',duracionMinutos:1}),403,'NO_SUPPORT_GLOBAL');
    console.log('P1004B_LIMITED_DIRECTORY_AND_CROSS_SCOPE_DENIAL=PASS');
    await revoke(superJwt,context.targetId,revokeReason);
    revoked=true;
    expect(await call('/plataforma/delegaciones/empresas','GET',businessJwt),403,'REVOKED_JWT_DENIED');
    readback(context,grantReason,revokeReason);
    console.log('P1004B_LIVE_DEV_HTTP_JWT_MYSQL=PASS');
  }finally{
    if(granted&&!revoked){
      try{
        await revoke(superJwt,context.targetId,'P1004B emergency cleanup '+reasonSuffix());
        console.log('P1004B_CLEANUP_REVOKED=PASS');
      }catch{console.error('P1004B_NO_GO=CLEANUP_REVOCATION_UNVERIFIED');}
    }
  }
}
if(process.argv.includes('--self-test'))selfTest();
else run().catch(e=>{console.error(String(e?.message||'P1004B_NO_GO=UNEXPECTED'));process.exitCode=1;});
