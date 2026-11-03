// P1-005.B LIVE DEV certification. Existing enterprises/users are test data.
// Actor credentials and roles remain unchanged. A short-lived user created via
// the tenant API proves role transitions, then is restored and soft-deleted.
// All SQL access is SELECT; never log passwords, JWTs or secret values.
import assert from 'node:assert/strict';
import {randomBytes} from 'node:crypto';
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
  const digest=membershipDigestQuery({uid:2},{uid:3});
  assert.equal(digest, 'SELECT CONCAT(COUNT(*),'+literal(':')+
    ',COALESCE(SUM(Activa),0),'+literal(':')+
    ',COALESCE(SUM(RolId),0)) FROM UsuarioEmpresas WHERE UsuarioId IN (2,3)');
  assert(!digest.includes('\":\"'));

  assert(blocked({status:403}));assert(blocked({status:404}));
  assert(!blocked({status:200}));assert(!blocked({status:400}));
  assert(!blocked({status:401}));assert(!blocked({status:503}));
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
  must(a.uid!==b.uid,'TENANT_SEPARATION_INVALID');
  // No exigir roles personalizados: las plantillas empresariales son válidas.
  // Un rol privado existe únicamente cuando tiene EmpresaId del tenant.
  const tenantOwnedRole=id=>{
    const raw=sql('SELECT COALESCE(MIN(Id),0) FROM Roles WHERE EmpresaId='+id+
      ' AND Ambito=1 AND Activo=1 AND Eliminado=0');
    must(/^\d+$/.test(raw),'TENANT_ROLE_SQL_INVALID');
    return Number(raw);
  };
  const roleAowned=tenantOwnedRole(2),roleBowned=tenantOwnedRole(3);
  console.log('P1005B_TENANT_PRIVATE_ROLES_A='+Number(roleAowned>0)+
    ' B='+Number(roleBowned>0));
  // No require a pre-existing non-admin user: the three DEV logins are
  // reserved for their real A/B/platform boundary tests. Create a dedicated
  // temporary Vendedor via the API for role assignment and revocation.
  const targetRole=num(sql('SELECT MIN(Id) FROM Roles WHERE Ambito=1 '+
    'AND Activo=1 AND Eliminado=0 AND EsAdministrador=0 '+
    'AND (EmpresaId=2 OR (EmpresaId IS NULL AND EsSistema=1))'),
    'TENANT_NONADMIN_ROLE_REQUIRED');
  // DEV seeds only Administrador and Vendedor by default. Use the existing
  // enterprise administrator template as the distinct transition role, never
  // a global/platform role. The temporary user never receives a JWT.
  const alternateRole=a.role;
  must(alternateRole!==targetRole,'TENANT_ROLE_TRANSITION_NOT_DISTINCT');
  must(count('SELECT COUNT(*) FROM Roles WHERE Id='+alternateRole+
    ' AND Ambito=1 AND EsAdministrador=1 AND Activo=1 AND Eliminado=0 '+
    'AND (EmpresaId=2 OR (EmpresaId IS NULL AND EsSistema=1))')===1,
    'TENANT_ADMIN_ROLE_REQUIRED');
  const platformRole=num(sql('SELECT Id FROM Roles WHERE Ambito=2 '+
    'AND EsSistema=1 AND EsAdministrador=1 AND EmpresaId IS NULL '+
    'ORDER BY Id LIMIT 1'),'PLATFORM_ROLE');
  must(platformRole!==a.role&&platformRole!==b.role,'PLATFORM_ROLE_CONFLICT');
  console.log('P1005B_MYSQL_TWO_TENANT_PREFLIGHT=PASS');
  return {a,b,platformRole,roleAowned,roleBowned,targetRole,alternateRole};
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
  const roleIds=rows.map(x=>num(x.id,'ROLE_ROW_ID_'+tenant));
  if(roleIds.length)must(count('SELECT COUNT(*) FROM Roles WHERE Id IN ('+
    roleIds.join(',')+') AND Ambito=2')===0,'GLOBAL_ROLE_LEAK_'+tenant);
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
function membershipDigestQuery(a,b){
  // Aiven DEV may have ANSI_QUOTES: double quotes are identifier delimiters.
  // Generate UTF-8 SQL literals, never use ":" as a SQL string literal.
  return 'SELECT CONCAT(COUNT(*),'+literal(':')+',COALESCE(SUM(Activa),0),'+
    literal(':')+',COALESCE(SUM(RolId),0)) FROM UsuarioEmpresas WHERE UsuarioId IN ('+
    num(a.uid,'MEMBERSHIP_UID_A')+','+num(b.uid,'MEMBERSHIP_UID_B')+')';
}
function membershipDigest(a,b){return sql(membershipDigestQuery(a,b));}
async function run(){
  env();const {a,b,platformRole,roleAowned,roleBowned,targetRole,alternateRole}=preflight();
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
    [await call('/usuarios/'+a.uid,'GET',jwtB,3),'B_READ_USER_A']])
    must(blocked(r),label+'_HTTP_'+r.status);
  // Una plantilla compartida NO es un rol privado del tenant B.
  // El negativo HTTP cross-role solo aplica si hay un rol realmente propio.
  if(roleBowned>0){
    const denied=await call('/roles/'+roleBowned,'GET',jwtA,2);
    must(blocked(denied),'A_READ_PRIVATE_ROLE_B_HTTP_'+denied.status);
    console.log('P1005B_CROSS_ROLE_B_HTTP=PASS');
  }else console.log('P1005B_CROSS_ROLE_B_HTTP=NOT_APPLICABLE_NO_PRIVATE_ROLE');
  if(roleAowned>0){
    const denied=await call('/roles/'+roleAowned,'GET',jwtB,3);
    must(blocked(denied),'B_READ_PRIVATE_ROLE_A_HTTP_'+denied.status);
    console.log('P1005B_CROSS_ROLE_A_HTTP=PASS');
  }else console.log('P1005B_CROSS_ROLE_A_HTTP=NOT_APPLICABLE_NO_PRIVATE_ROLE');
  expect(await call('/plataforma/sesion','GET',jwtA,2),403,'NO_GLOBAL_A');
  expect(await call('/plataforma/sesion','GET',jwtB,3),403,'NO_GLOBAL_B');
  console.log('P1005B_HTTP_JWT_A_B_ISOLATION=PASS');

  // Denied POST uses an already-existing username: if RBAC breaks, uniqueness
  // still prevents creation. A non-403 status is NO_GO, never accepted as PASS.
  // Nombre ya existente: nunca crear un usuario nuevo si fallara RBAC.
  const duplicate=userPayload(ownA.json,a.role);
  const safeBaseline=await call('/usuarios','POST',jwtA,2,{
    ...duplicate,password:'QATest-P1005B-No-Write-2026!'
  });
  // Con rol válido, la única respuesta admisible es 400 por nombre duplicado,
  // nunca 201 (creación) ni 403 (falta de autoridad para evaluar la frontera).
  expect(safeBaseline,400,'OWN_ROLE_DUPLICATE_NOT_CREATED');
  if(roleBowned>0){
    const denyRoleB=await call('/usuarios','POST',jwtA,2,{
      ...duplicate,rolId:roleBowned,password:'QATest-P1005B-No-Write-2026!'
    });
    expect(denyRoleB,403,'A_CANNOT_ASSIGN_B_ROLE');
    console.log('P1005B_FOREIGN_ROLE_ASSIGNMENT_HTTP=PASS');
  }else console.log('P1005B_FOREIGN_ROLE_ASSIGNMENT_HTTP=NOT_APPLICABLE_NO_PRIVATE_ROLE');
  const global=await call('/usuarios','POST',jwtA,2,{
    ...duplicate,rolId:platformRole,password:'QATest-P1005B-No-Write-2026!'
  });
  expect(global,403,'A_CANNOT_ASSIGN_PLATFORM_ROLE');
  // The scope-aware GetById in UsuariosController.Update returns 404 for
  // identities hidden from the actor. A 403 is also an authorized rejection;
  // neither must be mistaken for a real edit or a network/server failure.
  // Readback proves the cross-tenant negative did not change B in MySQL.
  const crossSql='SELECT CONCAT_WS(\':\',HEX(u.NombreUsuario),'+
    'HEX(u.NombreCompleto),u.RolId,u.Activo,u.Bloqueado,u.Eliminado,'+
    'COALESCE(HEX(CAST(u.FechaActualizacion AS CHAR)),\'NONE\'),m.RolId,m.Activa) '+
    'FROM Usuarios u JOIN UsuarioEmpresas m ON m.UsuarioId=u.Id '+
    'WHERE u.Id='+b.uid+' AND m.EmpresaId=3';
  const crossBefore=sql(crossSql);
  must(crossBefore.length>0,'B_CROSS_BASELINE_MISSING');
  const crossUpdate=await call('/usuarios/'+b.uid,'PUT',jwtA,2,userPayload(ownB.json,a.role));
  must(blocked(crossUpdate),'A_CANNOT_EDIT_B_HTTP_'+crossUpdate.status);
  must(sql(crossSql)===crossBefore,'A_CANNOT_EDIT_B_STATE_CHANGED');
  console.log('P1005B_CROSS_TENANT_EDIT_DENIED_MYSQL=PASS');
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
  // One-time, single-tenant fixture via normal API. The three existing DEV
  // identities are preserved; the generated password is never logged/stored.
  const qaUsername='qa_p1005b_'+process.env.GITHUB_SHA.slice(0,8)+'_'+randomBytes(7).toString('hex');
  const qaPassword='P1005b!'+randomBytes(20).toString('hex')+'Aa9!';
  const qaOriginal={nombreUsuario:qaUsername,
    nombreCompleto:'Certificacion automatizada P1-005.B DEV',
    rol:'Vendedor',rolId:targetRole};
  const qaByName=()=>{
    const raw=sql('SELECT COALESCE(MIN(Id),0) FROM Usuarios WHERE '+
      'BINARY NombreUsuario=BINARY '+literal(qaUsername));
    must(/^\\d+$/.test(raw),'QA_LOOKUP_INVALID');
    return Number(raw);
  };
  const roleRead=id=>[
    sql('SELECT RolId FROM Usuarios WHERE Id='+id),
    sql('SELECT RolId FROM UsuarioEmpresas WHERE UsuarioId='+id+
      ' AND EmpresaId=2 AND Activa=1')
  ];
  must(qaByName()===0,'QA_NAME_COLLISION');
  let targetId=0,primaryError=null;
  const cleanupErrors=[];
  try{
    // Create returns HTTP 200 in UsuariosController. The JWT of actor A is
    // still required and the backend enforces Crear and AsignarRol.
    const created=await call('/usuarios','POST',jwtA,2,
      {...qaOriginal,password:qaPassword});
    expect(created,200,'QA_CREATE');
    targetId=num(field(created.json,'id'),'QA_CREATED_ID');
    must(qaByName()===targetId,'QA_CREATED_SQL_ID_MISMATCH');
    must(count('SELECT COUNT(*) FROM Usuarios WHERE Id='+targetId+
      ' AND Activo=1 AND Eliminado=0')===1,'QA_NOT_ACTIVE');
    must(count('SELECT COUNT(*) FROM UsuarioEmpresas WHERE UsuarioId='+targetId+
      ' AND EmpresaId=2 AND Activa=1 AND RolId='+targetRole)===1,
      'QA_MEMBERSHIP_NOT_DURABLE');
    must(count('SELECT COUNT(*) FROM UsuarioEmpresas WHERE UsuarioId='+targetId)===1,
      'QA_SHARED_MEMBERSHIP');
    must(count('SELECT COUNT(*) FROM UsuarioRolesPlataforma WHERE UsuarioId='+targetId)===0,
      'QA_PLATFORM_GRANT');
    const qaGet=await call('/usuarios/'+targetId,'GET',jwtA,2);
    expect(qaGet,200,'QA_USER_GET');
    must(field(qaGet.json,'nombreUsuario')===qaUsername,'QA_USER_IDENTITY');
    must(roleRead(targetId).every(v=>Number(v)===targetRole),'QA_INITIAL_ROLE');
    const auditQuery='SELECT COUNT(*) FROM RegistrosAuditoria WHERE '+
      'UsuarioId='+a.uid+' AND ReferenciaId='+targetId+
      ' AND Accion='+literal('Editar')+" AND Resultado='Exito'";
    const auditBefore=count(auditQuery);
    const assign=await call('/usuarios/'+targetId,'PUT',jwtA,2,
      {...qaOriginal,rolId:alternateRole});
    expect(assign,200,'ASSIGN_TENANT_ROLE_A');
    must(roleRead(targetId).every(v=>Number(v)===alternateRole),
      'ASSIGNMENT_NOT_DURABLE');
    must(count('SELECT COUNT(*) FROM UsuarioRolesPlataforma WHERE UsuarioId='+targetId)===0,
      'QA_PLATFORM_ESCALATION');
    const auditGrantAfter=count(auditQuery);
    must(auditGrantAfter>auditBefore,'GRANT_AUDIT_NOT_DURABLE');
    const revoke=await call('/usuarios/'+targetId,'PUT',jwtA,2,qaOriginal);
    expect(revoke,200,'RESTORE_TENANT_ROLE_A');
    must(roleRead(targetId).every(v=>Number(v)===targetRole),
      'REVOCATION_NOT_DURABLE');
    must(count(auditQuery)>auditGrantAfter,'REVOCATION_AUDIT_NOT_DURABLE');
    console.log('P1005B_ROLE_ASSIGNMENT_REVOCATION_MYSQL=PASS');
  }catch(e){primaryError=e;}
  finally{
    // An ambiguous network failure after POST can still have committed the
    // user: recover by the prechecked unique name, without changing actors.
    let observedId=targetId;
    try{
      const found=qaByName();
      if(targetId>0&&found>0)must(found===targetId,'QA_ID_CHANGED');
      observedId=found||targetId;
    }catch{cleanupErrors.push('QA_LOOKUP_UNVERIFIED');}
    if(observedId>0){
      let owned=false;
      try{
        must(count('SELECT COUNT(*) FROM Usuarios WHERE Id='+observedId+
          ' AND BINARY NombreUsuario=BINARY '+literal(qaUsername))===1,
          'QA_USERNAME_MISMATCH');
        must(count('SELECT COUNT(*) FROM UsuarioEmpresas WHERE UsuarioId='+observedId+
          ' AND EmpresaId=2')===1,'QA_WRONG_TENANT');
        must(count('SELECT COUNT(*) FROM UsuarioEmpresas WHERE UsuarioId='+observedId)===1,
          'QA_SHARED_AFTER_CREATE');
        must(count('SELECT COUNT(*) FROM UsuarioRolesPlataforma WHERE UsuarioId='+observedId)===0,
          'QA_PLATFORM_AFTER_CREATE');
        owned=true;
      }catch{cleanupErrors.push('QA_OWNERSHIP_UNVERIFIED');}
      if(owned){
        try{
          if(count('SELECT COUNT(*) FROM Usuarios WHERE Id='+observedId+
            ' AND Activo=1 AND Eliminado=0')===1){
            // Restore first even after an ambiguous assign/revoke response.
            try{
              if(!roleRead(observedId).every(v=>Number(v)===targetRole)){
                const rescue=await call('/usuarios/'+observedId,'PUT',jwtA,2,qaOriginal);
                expect(rescue,200,'QA_EMERGENCY_RESTORE');
                must(roleRead(observedId).every(v=>Number(v)===targetRole),
                  'QA_EMERGENCY_RESTORE_UNVERIFIED');
                console.log('P1005B_EMERGENCY_ROLE_RESTORE=PASS');
              }
            }catch{cleanupErrors.push('QA_ROLE_RESTORE_UNVERIFIED');}
            // Always attempt deletion of the owned, newly created identity,
            // including when restoration failed, to eliminate active access.
            try{
              const del=await call('/usuarios/'+observedId,'DELETE',jwtA,2);
              expect(del,200,'QA_DELETE');
            }catch{cleanupErrors.push('QA_LOGICAL_DELETE_FAILED');}
          }
          must(count('SELECT COUNT(*) FROM Usuarios WHERE Id='+observedId+
            ' AND Activo=0 AND Eliminado=1')===1,'QA_DELETE_SQL_UNVERIFIED');
          console.log('P1005B_QA_FIXTURE_LOGICAL_DELETE=PASS');
        }catch{cleanupErrors.push('QA_FINAL_STATE_UNVERIFIED');}
      }
    }
  }
  if(cleanupErrors.length){
    console.error('P1005B_NO_GO='+cleanupErrors.join(','));
    fail('QA_CLEANUP_UNVERIFIED');
  }
  if(primaryError)throw primaryError;
  must(membershipDigest(a,b)===beforeMembership,'ACTOR_MEMBERSHIPS_CHANGED');
  must(count('SELECT COUNT(*) FROM UsuarioRolesPlataforma WHERE UsuarioId IN ('+
    a.uid+','+b.uid+') AND Activa=1')===0,'ACTOR_PLATFORM_ESCALATION');
  console.log('P1005B_LIVE_DEV_HTTP_JWT_MYSQL=PASS');
}
if(process.argv.includes('--self-test'))selfTest();
else run().catch(e=>{console.error(String(e?.message||'P1005B_NO_GO=UNEXPECTED'));process.exitCode=1;});
