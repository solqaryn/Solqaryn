"""QA-only live authentication probe with least-privilege, isolated fixtures."""
import json
import os
import re
import secrets
import signal
import subprocess
import urllib.error
import urllib.request

def require(ok, label):
    if not ok:
        raise RuntimeError(label)

def stopped(*_):
    raise RuntimeError("QA probe interrupted")
signal.signal(signal.SIGTERM, stopped)
signal.signal(signal.SIGINT, stopped)
require(os.environ.get("CI") == "true" and os.environ.get("GITHUB_REF") == "refs/heads/qa", "QA scope required")
require(os.environ.get("DB_NAME") == "solqaryn_qa" and os.environ.get("DB_USER") == "solqaryn_qa_user", "QA database identity required")
require(all(os.environ.get(k) for k in ("DB_HOST","DB_PORT","DB_PASSWORD")), "QA DB connection missing")
require(os.environ["DB_PORT"].isdigit(), "Invalid port")
env = dict(os.environ, MYSQL_PWD=os.environ["DB_PASSWORD"])
args = ["mysql","--protocol=TCP","--ssl-mode=REQUIRED","-h",os.environ["DB_HOST"],"-P",os.environ["DB_PORT"],"-u",os.environ["DB_USER"],os.environ["DB_NAME"],"--batch","--skip-column-names","--raw"]
def sql(statement, stage):
    result = subprocess.run(args,input=statement,text=True,capture_output=True,env=env,timeout=45)
    require(result.returncode==0,"SQL stage failed: "+stage)
    return result.stdout.strip()
require(sql("SELECT DATABASE(), SUBSTRING_INDEX(CURRENT_USER(),'@',1);","identity").split("\t")==["solqaryn_qa","solqaryn_qa_user"],"QA database identity mismatch")
run, attempt = os.environ["GITHUB_RUN_ID"], os.environ["GITHUB_RUN_ATTEMPT"]
require(run.isdigit() and attempt.isdigit(),"Invalid run identity")
prefix="qa_ci04_"+run+"_"+attempt+"_"+secrets.token_hex(6)
a,b=prefix+"_a",prefix+"_b"
require(len(b)<=50,"Fixture name too long")
names=f"('{a}','{b}')"
for table,column in (("Empresas","Nombre"),("Roles","Nombre"),("Usuarios","NombreUsuario")):
    require(sql(f"SELECT COUNT(*) FROM {table} WHERE {column} IN {names};","collision")=="0","Fixture name collision")
perm=sql("SELECT Id FROM Permisos WHERE Codigo='SUCURSALES.VER' AND Ambito=1 AND Activo=1 AND Eliminado=0;","permission")
require(perm.isdigit(),"Unique active Sucursales.Ver grant required")
passwords=[secrets.token_urlsafe(36),secrets.token_urlsafe(36)]
def bcrypt(name,password):
    r=subprocess.run(["htpasswd","-niBC","12",name],input=password+"\n",text=True,capture_output=True,timeout=20)
    require(r.returncode==0,"BCrypt generation failed")
    h=r.stdout.strip().split(":",1)[1]
    require(re.fullmatch(r"\$2[aby]\$12\$[./A-Za-z0-9]{53}",h) is not None,"Invalid BCrypt")
    return h
hashes=[bcrypt(a,passwords[0]),bcrypt(b,passwords[1])]
proxy="https://solqarynqa-nu.vercel.app/api"
backend="https://solqaryn.onrender.com"
class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self,*args,**kwargs):
        return None
opener=urllib.request.build_opener(NoRedirect())
def call(base,path,label,expected=200,token=None,tenant=None,body=None):
    headers={"Accept":"application/json"}
    if token: headers["Authorization"]="Bearer "+token
    if tenant: headers["X-Empresa-Id"]=str(tenant)
    if body is not None: headers["Content-Type"]="application/json"
    req=urllib.request.Request(base+path,data=json.dumps(body).encode() if body is not None else None,headers=headers)
    try:
        r=opener.open(req,timeout=40)
    except urllib.error.HTTPError as error:
        r=error
    with r:
        status=r.status
        require(status==expected,f"{label}: expected HTTP {expected}, got {status}")
        if base==proxy: require(r.headers.get("X-Solqaryn-Environment")=="QA",label+": wrong proxy environment")
        data=json.load(r) if expected==200 else None
    print(label+"=PASS",flush=True)
    return data
def auth(name,password,label):
    j=call(proxy,"/auth/login",label,body={"nombreUsuario":name,"password":password})
    require(j.get("success") is True and isinstance(j.get("data",{}).get("token"),str),label+": JWT missing")
    return j["data"]["token"]

# Never change a pre-existing enterprise, user, role or grant. Both identities
# are non-admins, owned by new empty test tenants. Only A gets one read grant.
created=False
try:
    ready=call(proxy,"/health/ready","QA_PROXY_READY")
    require(ready.get("status")=="ready" and ready.get("database")=="connected","QA DB not ready")
    statements=["START TRANSACTION;"]
    for suffix,name,h in (("a",a,hashes[0]),("b",b,hashes[1])):
        statements += [
          f"INSERT INTO Empresas (Nombre,Activa,FechaCreacion,FechaActualizacion) VALUES ('{name}',1,UTC_TIMESTAMP(),UTC_TIMESTAMP());",
          f"SET @e{suffix}=LAST_INSERT_ID();",
          f"INSERT INTO Roles (Nombre,NombreNormalizado,Descripcion,EsSistema,EsAdministrador,Ambito,EmpresaId,Activo,Eliminado,FechaCreacion) VALUES ('{name}','{name.upper()}','Fixture temporal CI QA',0,0,1,@e{suffix},1,0,UTC_TIMESTAMP());",
          f"SET @r{suffix}=LAST_INSERT_ID();",
          f"INSERT INTO Usuarios (NombreUsuario,NombreCompleto,PasswordHash,RolId,Activo,Bloqueado,Eliminado,FechaCreacion) VALUES ('{name}','{name}','{h}',@r{suffix},1,0,0,UTC_TIMESTAMP());",
          f"SET @u{suffix}=LAST_INSERT_ID();",
          f"INSERT INTO UsuarioEmpresas (UsuarioId,EmpresaId,RolId,Activa,FechaCreacion,FechaActualizacion) VALUES (@u{suffix},@e{suffix},@r{suffix},1,UTC_TIMESTAMP(),UTC_TIMESTAMP());"]
    statements += [f"INSERT INTO RolPermisos (RolId,PermisoId) VALUES (@ra,{perm});","COMMIT;","SELECT @ea,@eb,@ua,@ub,@ra,@rb;"]
    created=True
    ids=sql("\n".join(statements),"create isolated test identities").split("\t")
    require(len(ids)==6 and all(x.isdigit() and int(x)>0 for x in ids),"Fixture readback missing")
    ea,eb,ua,ub,ra,rb=map(int,ids)
    require(ea!=eb and ua!=ub and ra!=rb,"Fixture identity collision")
    print("QA_EPHEMERAL_IDENTITIES=PASS",flush=True)
    call(proxy,f"/tenant-context/{ea}","QA_ANONYMOUS_TENANT",401)
    call(proxy,"/sucursales/activas","QA_ANONYMOUS_RESOURCE",401)
    ta,tb=auth(a,passwords[0],"QA_LOGIN_A"),auth(b,passwords[1],"QA_LOGIN_B")
    for label,tenant,user,role,token in (("A",ea,ua,ra,ta),("B",eb,ub,rb,tb)):
        j=call(proxy,f"/tenant-context/{tenant}","QA_TENANT_"+label,token=token,tenant=tenant)
        d=j.get("data") or {}
        require(j.get("success") is True and d.get("empresaId")==tenant and d.get("usuarioId")==user and d.get("rolId")==role and d.get("esAdministrador") is False,"Tenant identity mismatch "+label)
    own=call(proxy,"/sucursales/activas","QA_RBAC_ALLOWED",token=ta,tenant=ea)
    require(own.get("success") is True and own.get("data")==[],"Empty test tenant leaked data")
    print("QA_TENANT_DATA_ISOLATED=PASS",flush=True)
    call(proxy,"/sucursales/activas","QA_RBAC_DENIED",403,token=tb,tenant=eb)
    call(proxy,f"/tenant-context/{eb}","QA_CROSS_TENANT_A_B",403,token=ta,tenant=eb)
    call(proxy,f"/tenant-context/{ea}","QA_CROSS_TENANT_B_A",403,token=tb,tenant=ea)
    call(proxy,"/sucursales/activas","QA_CROSS_TENANT_RESOURCE",403,token=ta,tenant=eb)
    j=call(backend,f"/tenant-context/{ea}","QA_RENDER_DIRECT",token=ta,tenant=ea)
    require(j.get("data",{}).get("empresaId")==ea and j["data"].get("rolId")==ra,"Render/proxy identity mismatch")
    print("QA_PROXY_RENDER_IDENTITY=PASS",flush=True)
finally:
    if created:
        # Revoke all run-owned access before deleting fixtures. No audit deletion.
        sql(f"""START TRANSACTION;
UPDATE Usuarios SET Activo=0 WHERE NombreUsuario IN {names};
UPDATE UsuarioEmpresas ue JOIN Usuarios u ON ue.UsuarioId=u.Id SET ue.Activa=0 WHERE u.NombreUsuario IN {names};
DELETE rp FROM RolPermisos rp JOIN Roles r ON rp.RolId=r.Id WHERE r.Nombre IN {names};
UPDATE Roles SET Activo=0 WHERE Nombre IN {names};
UPDATE Empresas SET Activa=0 WHERE Nombre IN {names};
COMMIT;""","revoke")
        require(sql(f"""SELECT (SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario IN {names} AND Activo=1)
+(SELECT COUNT(*) FROM Empresas WHERE Nombre IN {names} AND Activa=1)
+(SELECT COUNT(*) FROM Roles WHERE Nombre IN {names} AND Activo=1)
+(SELECT COUNT(*) FROM RolPermisos rp JOIN Roles r ON rp.RolId=r.Id WHERE r.Nombre IN {names})
+(SELECT COUNT(*) FROM UsuarioEmpresas ue JOIN Usuarios u ON ue.UsuarioId=u.Id WHERE u.NombreUsuario IN {names} AND ue.Activa=1);""","revoke readback")=="0","Fixture revocation incomplete")
        print("QA_EPHEMERAL_REVOKE=PASS",flush=True)
        # An audit FK may retain a disabled test company. Never remove audit
        # history to force cleanup. Delete the company only if unreferenced.
        sql(f"""START TRANSACTION;
DELETE ue FROM UsuarioEmpresas ue JOIN Usuarios u ON ue.UsuarioId=u.Id WHERE u.NombreUsuario IN {names};
DELETE FROM Usuarios WHERE NombreUsuario IN {names};
DELETE FROM Roles WHERE Nombre IN {names};
DELETE e FROM Empresas e LEFT JOIN RegistrosAuditoria r ON r.EmpresaId=e.Id WHERE e.Nombre IN {names} AND r.Id IS NULL;
COMMIT;""","cleanup")
        require(sql(f"""SELECT (SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario IN {names})
+(SELECT COUNT(*) FROM Roles WHERE Nombre IN {names})
+(SELECT COUNT(*) FROM Empresas WHERE Nombre IN {names} AND Activa=1);""","cleanup readback")=="0","Fixture cleanup incomplete")
        retained=sql(f"SELECT COUNT(*) FROM Empresas WHERE Nombre IN {names} AND Activa=0;","audit retention")
        print("QA_INACTIVE_TENANTS_RETAINED_FOR_AUDIT="+retained,flush=True)
        print("QA_EPHEMERAL_CLEANUP=PASS",flush=True)
print("QA_CI_04_LIVE_AUTH=PASS",flush=True)
