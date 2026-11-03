// P1-005.B independent LIVE DEV certification; only idempotent same-value edit
// of existing QA administrator. No new user/role, SQL DML, or permission grants.
import assert from 'node:assert/strict';
import {execFileSync} from 'node:child_process';
import {request as httpsRequest} from 'node:https';

const ORIGIN='https://solqaryn-api-dev-fxx8.onrender.com';
const DB={host:'solqaryn-mysql-solqaryn.h.aivencloud.com',
  port:'14402',name:'solqaryn_dev',user:'solqaryn_dev_user'};
const fail=code=>{throw Error('P1005B_NO_GO='+code);};
const must=(ok,code)=>{if(!ok)fail(code);};
const num=(value,code)=>{const n=Number(value);must(Number.isSafeInteger(n)&&n>0,code);return n;};
const jsonData=x=>x?.data??x?.Data??x;
const field=(x,key)=>jsonData(x)?.[key]??jsonData(x)?.[key[0].toUpperCase()+key.slice(1)];
const literal=v=>"CONVERT(UNHEX('"+Buffer.from(String(v),'utf8').toString('hex')+"') USING utf8mb4)";
const blocked=x=>x.status===403||x.status===404;
const headersFor=(jwt,tenant,body)=>({
  Accept:'application/json','Cache-Control':'no-store',
  'User-Agent':'SOLQARYN-P1005B-LIVE',...(jwt?{Authorization:'Bearer '+jwt}:{}),
  ...(tenant?{'X-Empresa-Id':String(tenant)}:{}),
  ...(body?{'Content-Type':'application/json','Content-Length':Buffer.byteLength(body)}:{})
});
function selfTest(){
  assert.equal(num('4','SCOPE'),4);
  assert.equal(field({data:{id:5}},'id'),5);
  assert.equal(literal('a'),"CONVERT(UNHEX('61') USING utf8mb4)");
  assert(blocked({status:403}));assert(blocked({status:404}));
  assert(!blocked({status:200}));assert(!blocked({status:503}));
  assert.equal(headersFor('test',2,null)['X-Empresa-Id'],'2');
  assert.equal(ORIGIN,'https://solqaryn-api-dev-fxx8.onrender.com');
  console.log('P1005B_OFFLINE_CONTRACT=PASS');
}
function env(){
  must(process.env.GITHUB_REPOSITORY==='solqaryn/Solqaryn','REPO');
  must(process.env.GITHUB_REF==='refs/heads/dev','BRANCH');
  must(/^[0-9a-f]{40}$/.test(process.env.GITHUB_SHA||''),'SHA');
  must(process.env.API_ORIGIN===ORIGIN,'API_ORIGIN');
  for(const [k,v] of Object.entries(DB)){
    const name={host:'DB_HOST',port:'DB_PORT',name:'DB_NAME',user:'DB_USER'}[k];
    must(process.env[name]===v,'DB_IDENTITY_'+k);
  }
  must(process.env.A_USERNAME==='admin_prueba_a','ACTOR_A');
  must(process.env.B_USERNAME==='admin_prueba_b','ACTOR_B');
  for(const n of ['DB_PASSWORD','A_PASSWORD','B_PASSWORD'])
    must(typeof process.env[n]==='string'&&process.env[n].length>0,'MISSING_'+n);
}
function sql(query){
  const args=['--protocol=TCP','-h',process.env.DB_HOST,'-P',process.env.DB_PORT,
    '-u',process.env.DB_USER,'--ssl-mode=REQUIRED','--connect-timeout=12',
    process.env.DB_NAME,'--batch','--skip-column-names','-e',query];
  try{return execFileSync('mysql',args,{encoding:'utf8',timeout:30000,
    maxBuffer:8192,env:{...process.env,MYSQL_PWD:process.env.DB_PASSWORD}}).trim();}
  catch{fail('MYSQL_SELECT_FAILED');}
}
function count(query){const v=sql(query);must(/^\d+$/.test(v),'SQL_COUNT_INVALID');return Number(v);}
function identity(name,empresa){
  const clause=' BINARY u.NombreUsuario=BINARY '+literal(name)+
    ' AND u.Activo=1 AND u.Bloqueado=0 AND u.Eliminado=0';
  must(count('SELECT COUNT(*) FROM Usuarios u WHERE '+clause)===1,'ACTOR_NOT_UNIQUE');
  const uid=num(sql('SELECT u.Id FROM Usuarios u WHERE '+clause+' LIMIT 1'),'ACTOR_ID');
  must(count('SELECT COUNT(*) FROM UsuarioEmpresas m WHERE m.UsuarioId='+uid)===1,
    'ACTOR_NOT_EXCLUSIVE_TENANT');
  must(count('SELECT COUNT(*) FROM UsuarioEmpresas m JOIN Roles r ON r.Id=m.RolId '+
    'WHERE m.UsuarioId='+uid+' AND m.EmpresaId='+empresa+' AND m.Activa=1 '+
    'AND r.EsAdministrador=1 AND r.Activo=1 AND r.Eliminado=0 '+
    'AND r.Ambito<>2')===1,'ACTOR_NOT_TENANT_ADMIN');
  must(count('SELECT COUNT(*) FROM UsuarioRolesPlataforma WHERE UsuarioId='+uid+
    ' AND Activa=1')===0,'ACTOR_HAS_PLATFORM_AUTH');
  const role=num(sql('SELECT RolId FROM UsuarioEmpresas WHERE UsuarioId='+uid+
    ' AND EmpresaId='+empresa+' AND Activa=1 LIMIT 1'),'ACTOR_ROLE');
  return {uid,role,empresa,name};
}
function preflight(){
  must(sql('SELECT DATABASE()')===DB.name,'WRONG_DATABASE');
  must(sql("SELECT SUBSTRING_INDEX(CURRENT_USER(),'@',1)")===DB.user,'WRONG_DB_USER');
  must(count('SELECT COUNT(*) FROM Empresas WHERE Id IN (2,3) AND Activa=1')===2,
    'TWO_TENANTS_REQUIRED');
  const a=identity(process.env.A_USERNAME,2),b=identity(process.env.B_USERNAME,3);
  must(a.uid!==b.uid&&a.role!==b.role,'TENANT_SEPARATION_INVALID');
  const platformRole=num(sql('SELECT Id FROM Roles WHERE Ambito=2 '+
    'AND EsSistema=1 AND EsAdministrador=1 AND EmpresaId IS NULL '+
    'ORDER BY Id LIMIT 1'),'PLATFORM_ROLE');
  must(platformRole!==a.role&&platformRole!==b.role,'PLATFORM_ROLE_CONFLICT');
  console.log('P1005B_MYSQL_TWO_TENANT_PREFLIGHT=PASS');
  return {a,b,platformRole};
}
async function call(path,method='GET',jwt='',tenant=0,payload){
  const url=new URL(path,ORIGIN);
  must(url.origin===ORIGIN&&url.protocol==='https:','URL');
  const body=payload===undefined?null:JSON.stringify(payload);
  return new Promise((resolve,reject)=>{
    const req=httpsRequest(url,{method,headers:headersFor(jwt,tenant,body),timeout:30000},res=>{
      let raw='';res.setEncoding('utf8');
      res.on('data',x=>{raw+=x;if(raw.length>250000)req.destroy(Error('TOO_MUCH_DATA'));});
      res.on('end',()=>{let json=null;try{json=JSON.parse(raw);}catch{}
        resolve({status:res.statusCode,json,headers:res.headers});});
    });
    req.on('timeout',()=>req.destroy(Error('TIMEOUT')));
    req.on('error',()=>reject(Error('NETWORK_OR_TIMEOUT')));
    req.end(body??undefined);
  });
}
const expect=(response,status,stage)=>must(response.status===status,stage+'_HTTP_'+response.status);
async function login(user,pass,label){
  const r=await call('/auth/login','POST','',0,{nombreUsuario:user,password:pass});
  expect(r,200,'LOGIN_'+label);
  const jwt=field(r.json,'token');
  must(typeof jwt==='string'&&jwt.length>30,'JWT_'+label);
  return jwt;
}
function readRole(rows,target,tenant){
  must(Array.isArray(rows),'ROLES_SHAPE_'+tenant);
  must(rows.some(x=>Number(x.id)===target.role),'OWN_ROLE_MISSING_'+tenant);
  must(rows.every(x=>x.empresaId==null||Number(x.empresaId)===tenant),
    'CROSS_TENANT_ROLES_EXPOSED_'+tenant);
}
function noStore(r,label){must(String(r.headers['cache-control']||'').includes('no-store'),
  'CACHE_LEAK_'+label);}
function userPayload(row,role){
  const obj=jsonData(row);
  must(obj&&typeof obj.nombreUsuario==='string'&&
    typeof obj.nombreCompleto==='string','USER_DTO');
  return {nombreUsuario:obj.nombreUsuario,nombreCompleto:obj.nombreCompleto,
    rol:obj.rol||'Vendedor',rolId:role};
}
function membershipDigest(a,b){
  return sql('SELECT CONCAT(COUNT(*),":",COALESCE(SUM(Activa),0),":",'+
    'COALESCE(SUM(RolId),0)) FROM UsuarioEmpresas WHERE UsuarioId IN ('+
    a.uid+','+b.uid+')');
}
async function run(){
  env();const {a,b,platformRole}=preflight();
  expect(await call('/health/ready'),200,'RENDER_READY');
  const jwtA=await login(process.env.A_USERNAME,process.env.A_PASSWORD,'A');
  const jwtB=await login(process.env.B_USERNAME,process.env.B_PASSWORD,'B');
  const anonymous=await call('/usuarios','GET','',2);
  must(anonymous.status!==200,'ANON_ADMIN_ALLOWED');
  const ra=await call('/roles','GET',jwtA,2),rb=await call('/roles','GET',jwtB,3);
  expect(ra,200,'ROLES_A');expect(rb,200,'ROLES_B');
  readRole(jsonData(ra.json),a,2);readRole(jsonData(rb.json),b,3);
  const ownA=await call('/usuarios/'+a.uid,'GET',jwtA,2);
  const ownB=await call('/usuarios/'+b.uid,'GET',jwtB,3);
  expect(ownA,200,'OWN_A');expect(ownB,200,'OWN_B');
  noStore(ownA,'USERS_A');noStore(ownB,'USERS_B');
  for(const [r,label] of [
    [await call('/usuarios/'+b.uid,'GET',jwtA,2),'A_READ_USER_B'],
    [await call('/usuarios/'+a.uid,'GET',jwtB,3),'B_READ_USER_A'],
    [await call('/roles/'+b.role,'GET',jwtA,2),'A_READ_ROLE_B'],
    [await call('/roles/'+a.role,'GET',jwtB,3),'B_READ_ROLE_A']])
    must(blocked(r),label+'_HTTP_'+r.status);
  expect(await call('/plataforma/sesion','GET',jwtA,2),403,'NO_GLOBAL_A');
  expect(await call('/plataforma/sesion','GET',jwtB,3),403,'NO_GLOBAL_B');
  console.log('P1005B_HTTP_JWT_A_B_ISOLATION=PASS');

  // Denied POST uses an already-existing username: if RBAC breaks, uniqueness
  // still prevents creation. A non-403 status is NO_GO, never accepted as PASS.
  const duplicate=userPayload(ownA.json,b.role);
  const denyRoleB=await call('/usuarios','POST',jwtA,2,{
    ...duplicate,password:'QATest-P1005B-No-Write-2026!'
  });
  expect(denyRoleB,403,'A_CANNOT_ASSIGN_B_ROLE');
  const global=await call('/usuarios','POST',jwtA,2,{
    ...duplicate,rolId:platformRole,password:'QATest-P1005B-No-Write-2026!'
  });
  expect(global,403,'A_CANNOT_ASSIGN_PLATFORM_ROLE');
  const crossUpdate=await call('/usuarios/'+b.uid,'PUT',jwtA,2,userPayload(ownB.json,b.role));
  expect(crossUpdate,403,'A_CANNOT_EDIT_B');
  const beforeMembership=membershipDigest(a,b);
  const auditBefore=count('SELECT COUNT(*) FROM RegistrosAuditoria WHERE UsuarioId='+a.uid+
    ' AND Entidad='+literal('Usuario')+' AND ReferenciaId='+a.uid+
    ' AND Accion='+literal('Editar')+" AND Resultado='Exito'");
  const original=userPayload(ownA.json,a.role);
  const update=await call('/usuarios/'+a.uid,'PUT',jwtA,2,original);
  expect(update,200,'OWN_A_IDEMPOTENT_EDIT');
  must(Number(field(update.json,'rolId'))===a.role,'ROL_RETURN_MISMATCH');
  must(membershipDigest(a,b)===beforeMembership,'MEMBERSHIPS_CHANGED');
  must(count('SELECT COUNT(*) FROM UsuarioRolesPlataforma WHERE UsuarioId IN ('+
    a.uid+','+b.uid+') AND Activa=1')===0,'PLATFORM_ESCALATION');
  const auditAfter=count('SELECT COUNT(*) FROM RegistrosAuditoria WHERE UsuarioId='+a.uid+
    ' AND Entidad='+literal('Usuario')+' AND ReferenciaId='+a.uid+
    ' AND Accion='+literal('Editar')+" AND Resultado='Exito'");
  must(auditAfter>auditBefore,'AUDIT_NOT_DURABLE');
  console.log('P1005B_OWN_ROLE_IDEMPOTENT_EDIT_AUDIT=PASS');
  console.log('P1005B_LIVE_DEV_HTTP_JWT_MYSQL=PASS');
}
if(process.argv.includes('--self-test'))selfTest();
else run().catch(e=>{console.error(String(e?.message||'P1005B_NO_GO=UNEXPECTED'));process.exitCode=1;});
