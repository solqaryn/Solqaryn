// P1-003.B: certificación HTTP/JWT y MySQL LIVE, solo en SOLQARYN DEV.
// El backend crea/revoca únicamente sesiones de soporte y su auditoría;
// nunca escribe datos comerciales, roles, empresas, membresías ni contraseñas.
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {execFileSync} from 'node:child_process';
import {request as httpsRequest} from 'node:https';

const ORIGIN = 'https://solqaryn-api-dev-fxx8.onrender.com';
const PRIMARY = 2;
const FOREIGN = 3;
const TAG = 'P1003B_DEV_LIVE_SUPPORT';
const fail = (code) => { throw Error('P1003B_NO_GO=' + code); };
const required = (value, code) => { if (!value) fail(code); return value; };
const num = (value, code) => { const n=Number(value); if (!Number.isSafeInteger(n) || n<=0) fail(code); return n; };
const hash = (token) => createHash('sha256').update(token, 'ascii').digest('hex');
const dataOf = (json) => json?.data ?? json?.Data ?? json;
const field = (json,key) => {
  const obj=dataOf(json); const capital=key[0].toUpperCase()+key.slice(1);
  return obj?.[key] ?? obj?.[capital];
};

function selfTest() {
  assert.equal(hash('abc').length,64);
  assert.equal(num('2','int'),2);
  assert.equal(field({data:{sessionId:4}},'sessionId'),4);
  assert.equal(ORIGIN,'https://solqaryn-api-dev-fxx8.onrender.com');
  console.log('P1003B_CONTRACT_SELF_TEST=PASS');
}

function preflightEnv() {
  required(process.env.GITHUB_REPOSITORY === 'solqaryn/Solqaryn', 'REPOSITORY');
  required(process.env.GITHUB_REF === 'refs/heads/dev', 'BRANCH');
  required(process.env.API_ORIGIN === ORIGIN, 'ORIGIN');
  required(process.env.DB_HOST === 'solqaryn-mysql-solqaryn.h.aivencloud.com', 'DB_HOST');
  required(process.env.DB_PORT === '14402', 'DB_PORT');
  required(process.env.DB_NAME === 'solqaryn_dev', 'DB_NAME');
  required(process.env.DB_USER === 'solqaryn_dev_user', 'DB_USER');
  for (const key of ['DB_PASSWORD','SUPPORT_USERNAME','SUPPORT_PASSWORD','BUSINESS_USERNAME','BUSINESS_PASSWORD']) {
    required(typeof process.env[key] === 'string' && process.env[key].length>0, 'MISSING_' + key);
  }
}

function query(sql) {
  // SQL entrante es constante o se compone solo de enteros validados/hex UTF8.
  const args=['--protocol=TCP','-h',process.env.DB_HOST,'-P',process.env.DB_PORT,
    '-u',process.env.DB_USER,'--ssl-mode=REQUIRED','--connect-timeout=12',
    process.env.DB_NAME,'--batch','--skip-column-names','-e',sql];
  try {
    return execFileSync('mysql',args,{
      encoding:'utf8',timeout:30000,maxBuffer:65536,
      env:{...process.env,MYSQL_PWD:process.env.DB_PASSWORD}
    }).trim();
  } catch { fail('MYSQL_READONLY_QUERY'); }
}

function count(sql,code) {
  const result=query(sql);
  if (!/^\d+$/.test(result)) fail(code);
  return Number(result);
}

function mysqlPreflight() {
  required(query('SELECT DATABASE()') === 'solqaryn_dev','DATABASE_IDENTITY');
  required(query("SELECT SUBSTRING_INDEX(CURRENT_USER(),'@',1)") === 'solqaryn_dev_user','DATABASE_PRINCIPAL');
  const active=count('SELECT COUNT(*) FROM Empresas WHERE Id IN (2,3) AND Activa=1','TENANTS');
  required(active===2,'TWO_ACTIVE_TENANTS');
  const encoded=Buffer.from(process.env.SUPPORT_USERNAME,'utf8').toString('hex');
  const user="CONVERT(UNHEX('"+encoded+"') USING utf8mb4)";
  const platform=count(
    'SELECT COUNT(*) FROM UsuarioRolesPlataforma g JOIN Usuarios u ON u.Id=g.UsuarioId '+
    'JOIN Roles r ON r.Id=g.RolId WHERE BINARY u.NombreUsuario = BINARY '+user+
    ' AND u.Activo=1 AND u.Bloqueado=0 AND u.Eliminado=0 AND g.Activa=1 '+
    'AND r.Activo=1 AND r.Eliminado=0 AND r.EsAdministrador=1 '+
    'AND r.Ambito=2 AND r.EmpresaId IS NULL','PLATFORM_IDENTITY');
  required(platform===1,'SUPPORT_CREDENTIAL_NOT_PLATFORM_ADMIN');
  const membership=count(
    'SELECT COUNT(*) FROM UsuarioEmpresas ue JOIN Usuarios u ON ue.UsuarioId=u.Id '+
    'WHERE BINARY u.NombreUsuario = BINARY '+user+
    ' AND ue.EmpresaId IN (2,3) AND ue.Activa=1','MEMBERSHIP_CHECK');
  required(membership===0,'OPERATOR_HAS_COMMERCIAL_MEMBERSHIP');
  const schema=count("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES "+
    "WHERE TABLE_SCHEMA='solqaryn_dev' AND TABLE_NAME='PlataformaSesionesSoporte'",
    'SESSION_SCHEMA');
  required(schema===1,'SUPPORT_SCHEMA_MISSING');
  console.log('P1003B_PLATFORM_AND_TENANT_PREFLIGHT=PASS');
}

async function call(path,method='GET',jwt='',headers={},payload=undefined) {
  const url=new URL(path,ORIGIN);
  required(url.origin===ORIGIN&&url.protocol==='https:','UNTRUSTED_ORIGIN');
  const body=payload===undefined?null:JSON.stringify(payload);
  const h={'Accept':'application/json','User-Agent':'SOLQARYN-P1003B-CERT',
    'Cache-Control':'no-store',...headers};
  if (jwt) h.Authorization='Bearer '+jwt;
  if (body!==null) {h['Content-Type']='application/json';h['Content-Length']=Buffer.byteLength(body);}
  return new Promise((resolve,reject)=>{
    const req=httpsRequest(url,{method,headers:h,timeout:30000},res=>{
      let chunks='';res.setEncoding('utf8');
      res.on('data',v=>{chunks+=v;if(chunks.length>250000) req.destroy(Error('RESPONSE_TOO_BIG'));});
      res.on('end',()=>{let json=null;try{json=JSON.parse(chunks);}catch{}
        resolve({status:res.statusCode,headers:res.headers,json});
      });
    });
    req.on('timeout',()=>req.destroy(Error('REQUEST_TIMEOUT')));
    req.on('error',()=>reject(Error('NETWORK_OR_TIMEOUT')));
    req.end(body??undefined);
  });
}

async function login(username,password,label) {
  const res=await call('/auth/login','POST','',{},{
    nombreUsuario:username,password
  });
  required(res.status===200,'LOGIN_'+label+'_HTTP_'+res.status);
  const token=field(res.json,'token');
  required(typeof token==='string'&&token.length>30,'LOGIN_'+label+'_TOKEN');
  return token;
}

const expectStatus=(response,status,step)=>{
  required(response.status===status,step+'_HTTP_'+response.status);
};

async function createSession(jwt,module,reason) {
  const res=await call('/plataforma/soporte/sesiones','POST',jwt,{},{
    empresaId:PRIMARY,modulo:module,motivo:reason,duracionMinutos:1
  });
  expectStatus(res,201,'SESSION_CREATE');
  required(String(res.headers['cache-control']||'').includes('no-store'),'CREATE_CACHE');
  const id=num(field(res.json,'sessionId'),'SESSION_ID');
  const token=field(res.json,'token');
  required(typeof token==='string'&&/^[A-Za-z0-9_-]{43}$/.test(token),'SESSION_TOKEN_SHAPE');
  const expires=Date.parse(field(res.json,'expiresUtc'));
  required(Number.isFinite(expires)&&expires>Date.now()&&expires-Date.now()<75000,'SESSION_EXPIRY');
  return {id,token,expires,revoked:false};
}

async function revoke(jwt,s) {
  const res=await call('/plataforma/soporte/sesiones/'+s.id,'DELETE',jwt);
  expectStatus(res,200,'SUPPORT_REVOKE');
  s.revoked=true;
}

function supportHeaders(s,company) {
  return {'X-Empresa-Id':String(company),'X-Solqaryn-Support-Token':s.token};
}

function verifyDb(sessions) {
  const ids=sessions.map(s=>num(s.id,'SESSION_ID')).join(',');
  const record=count('SELECT COUNT(*) FROM PlataformaSesionesSoporte '+
    'WHERE Id IN ('+ids+') AND RevocadaUtc IS NOT NULL AND CHAR_LENGTH(TokenHash)=64',
    'SESSION_READBACK');
  required(record===sessions.length,'SUPPORT_REVOCATION_NOT_PERSISTED');
  for (const s of sessions) {
    const ok=count("SELECT COUNT(*) FROM PlataformaSesionesSoporte "+
      "WHERE Id="+s.id+" AND TokenHash='"+hash(s.token)+"'",'TOKEN_HASH_READBACK');
    required(ok===1,'TOKEN_NOT_HASHED_DURABLY');
  }
  const audits=count("SELECT COUNT(*) FROM RegistrosAuditoria "+
    "WHERE Entidad='PlataformaSesionSoporte' AND ReferenciaId IN ("+ids+")",
    'AUDIT_READBACK');
  required(audits>=6,'SUPPORT_AUDIT_NOT_DURABLE');
  console.log('P1003B_MYSQL_SESSION_REVOCATION_AND_AUDIT=PASS');
}

async function run() {
  preflightEnv();
  mysqlPreflight();
  const ready=await call('/health/ready');
  expectStatus(ready,200,'RENDER_DEV_READY');
  const jwt=await login(process.env.SUPPORT_USERNAME,process.env.SUPPORT_PASSWORD,'PLATFORM');
  const platform=await call('/plataforma/sesion','GET',jwt);
  expectStatus(platform,200,'LIVE_PLATFORM_AUTHORITY');
  const business=await login(process.env.BUSINESS_USERNAME,process.env.BUSINESS_PASSWORD,'BUSINESS');
  expectStatus(await call('/plataforma/sesion','GET',business),403,'BUSINESS_PLATFORM_DENIAL');
  expectStatus(await call('/plataforma/soporte/sesiones','POST',business,{},{
    empresaId:PRIMARY,modulo:2,motivo:'P1003B negative company operator',duracionMinutos:1
  }),403,'BUSINESS_SUPPORT_DENIAL');
  console.log('P1003B_HTTP_JWT_PLATFORM_ROLE=PASS');

  const sessions=[];
  try {
    const products=await createSession(jwt,2,'P1003B DEV validated read-only Productos');
    sessions.push(products);
    expectStatus(await call('/productos','GET',jwt,supportHeaders(products,PRIMARY)),200,'OWN_PRODUCTS');
    expectStatus(await call('/productos','GET',jwt,supportHeaders(products,FOREIGN)),403,'CROSS_COMPANY');
    expectStatus(await call('/categorias','GET',jwt,supportHeaders(products,PRIMARY)),403,'CROSS_MODULE');
    expectStatus(await call('/usuarios','GET',jwt,supportHeaders(products,PRIMARY)),403,'FORBIDDEN_ROUTE');
    expectStatus(await call('/productos','POST',jwt,supportHeaders(products,PRIMARY)),403,'FORBIDDEN_VERB');
    await revoke(jwt,products);
    expectStatus(await call('/productos','GET',jwt,supportHeaders(products,PRIMARY)),403,'REVOKED_REPLAY');

    const categories=await createSession(jwt,3,'P1003B DEV validated read-only Categorias');
    sessions.push(categories);
    expectStatus(await call('/categorias/activas','GET',jwt,supportHeaders(categories,PRIMARY)),200,'OWN_CATEGORIES');
    const wait=Math.max(0,categories.expires-Date.now()+1800);
    required(wait<75000,'EXPIRY_WAIT_INVALID');
    await new Promise(resolve=>setTimeout(resolve,wait));
    expectStatus(await call('/categorias/activas','GET',jwt,supportHeaders(categories,PRIMARY)),403,'EXPIRED_TOKEN');
    await revoke(jwt,categories);
    verifyDb(sessions);
    console.log('P1003B_READ_ONLY_CROSS_TENANT_MODULE_EXPIRY_REVOKE=PASS');
    console.log('P1003B_LIVE_DEV_HTTP_JWT_MYSQL=PASS');
  } finally {
    for (const s of sessions) if (!s.revoked) {
      try { await revoke(jwt,s); } catch {
        console.log('P1003B_CLEANUP_NOT_VERIFIED');
      }
    }
  }
}
if (process.argv.includes('--self-test')) selfTest();
else run().catch(e=>{console.error(String(e?.message||'P1003B_NO_GO=UNEXPECTED'));process.exitCode=1;});
