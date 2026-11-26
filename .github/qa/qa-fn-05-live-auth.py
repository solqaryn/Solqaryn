"""QA-FN-05 live administrative probe; only run-owned ephemeral rows."""
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
expected_sha = os.environ.get("EXPECTED_DEPLOYED_SHA", "")
require(re.fullmatch(r"[0-9a-f]{40}", expected_sha) is not None, "Expected deployment SHA required")
# The caller must independently verify this SHA via Render deploy metadata
# before and after the run. This is not a server-reported revision.
print("EXPECTED_RENDER_SHA=" + expected_sha, flush=True)
print("RENDER_SHA_REQUIRES_EXTERNAL_ATTESTATION", flush=True)

# Read-only physical preflight: aggregate counts only, never infer ownership
# from row IDs, creator, single active company, or historical business data.
for table in ("Productos", "ProductoVariantes", "Clientes", "Proveedores", "Compras", "Ventas", "MovimientosInventario"):
    exists = sql(f"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='{table}';", "commercial table identity")
    require(exists == "1", "Required commercial table missing: " + table)
    owned = sql(f"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='{table}' AND COLUMN_NAME='EmpresaId';", "commercial owner column")
    count = sql(f"SELECT COUNT(*) FROM `{table}`;", "aggregate row count")
    require(count.isdigit() and owned in ("0", "1"), "Invalid commercial preflight result")
    if owned == "1":
        nulls = sql(f"SELECT COUNT(*) FROM `{table}` WHERE EmpresaId IS NULL;", "unowned count")
        require(nulls.isdigit(), "Invalid unowned count")
        print(f"QA_COMMERCIAL_PREFLIGHT {table} owner_column=YES rows={count} unowned={nulls}", flush=True)
    else:
        print(f"QA_COMMERCIAL_PREFLIGHT {table} owner_column=NO rows={count} ownership=NOT_CERTIFIED", flush=True)
print("QA_COMMERCIAL_PREFLIGHT_READ_ONLY=PASS", flush=True)

run, attempt = os.environ["GITHUB_RUN_ID"], os.environ["GITHUB_RUN_ATTEMPT"]
require(run.isdigit() and attempt.isdigit(), "Invalid run identity")
prefix = "qa_fn05_" + run + "_" + attempt + "_" + secrets.token_hex(6)
a, b, target, shared = (prefix + "_" + x for x in ("a", "b", "t", "s"))
require(len(shared) <= 50, "Fixture name too long")
names = "(" + ",".join("'" + x + "'" for x in (a, b, target, shared)) + ")"
for table, column in (("Empresas","Nombre"),("Roles","Nombre"),("Usuarios","NombreUsuario")):
    require(sql(f"SELECT COUNT(*) FROM {table} WHERE {column} IN {names};", "collision") == "0", "Fixture collision")

def grant(code):
    p = sql(f"SELECT Id FROM Permisos WHERE Codigo='{code}' AND Ambito=1 AND Activo=1 AND Eliminado=0;", "required active grant " + code)
    require(p.isdigit(), "Missing unique enterprise grant: " + code)
    return int(p)

view_user = grant("USUARIOS.VER")
edit_user = grant("USUARIOS.EDITAR")
reset_user = grant("USUARIOS.RESTABLECERCONTRASENA")
view_role = grant("ROLES.VER")
matrix = grant("PERMISOS.ADMINISTRAR")

def new_password():
    return "Qa!7" + secrets.token_urlsafe(32)
passwords = {n: new_password() for n in (a, b, target, shared)}
new_target = new_password()
def bcrypt(name, password):
    r = subprocess.run(["htpasswd","-niBC","12",name], input=password+"\n", text=True,capture_output=True,timeout=20)
    require(r.returncode == 0, "BCrypt generation failed")
    h = r.stdout.strip().split(":",1)[1]
    require(re.fullmatch(r"\$2[aby]\$12\$[./A-Za-z0-9]{53}", h) is not None, "Invalid BCrypt")
    return h
hashes = {n: bcrypt(n, p) for n, p in passwords.items()}

proxy = "https://solqarynqa-nu.vercel.app/api"
backend = "https://solqaryn.onrender.com"
class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self,*args,**kwargs):
        return None
opener = urllib.request.build_opener(NoRedirect())
def call(path, label, expected=200, token=None, tenant=None, body=None, method=None, base=proxy):
    headers = {"Accept":"application/json"}
    if token: headers["Authorization"] = "Bearer " + token
    if tenant: headers["X-Empresa-Id"] = str(tenant)
    if body is not None: headers["Content-Type"] = "application/json"
    req = urllib.request.Request(base+path, data=json.dumps(body).encode() if body is not None else None,
                                 headers=headers, method=method)
    try:
        r = opener.open(req, timeout=40)
    except urllib.error.HTTPError as error:
        r = error
    with r:
        statuses = (expected,) if isinstance(expected,int) else expected
        require(r.status in statuses, f"{label}: expected HTTP {statuses}, got {r.status}")
        if base == proxy:
            require(r.headers.get("X-Solqaryn-Environment") == "QA", label+": wrong proxy environment")
        data = json.load(r) if r.status == 200 else None
    print(label+"=PASS", flush=True)
    return data

def data(j):
    require(isinstance(j,dict) and j.get("success") is True, "Missing success envelope")
    return j.get("data")

def login(name, password, label, expected=200):
    j = call("/auth/login",label,expected=expected,body={"nombreUsuario":name,"password":password})
    if expected == 200:
        token = data(j).get("token")
        require(isinstance(token,str) and len(token)>20, "JWT missing")
        return token
    return None

def user_update(name, role, password):
    return {"nombreUsuario":name,"nombreCompleto":name,"rolId":role,"nuevaPassword":password}

created = False
try:
    ready = call("/health/ready", "QA_FN05_PROXY_READY")
    require(ready.get("status")=="ready" and ready.get("database")=="connected", "QA not ready")
    ready_direct = call("/health/ready","QA_FN05_RENDER_READY",base=backend)
    require(ready_direct.get("status")=="ready" and ready_direct.get("database")=="connected", "Render not ready")
    statements = ["START TRANSACTION;"]
    for suffix, name in (("a",a),("b",b)):
        statements += [
          f"INSERT INTO Empresas (Nombre,Activa,FechaCreacion,FechaActualizacion) VALUES ('{name}',1,UTC_TIMESTAMP(),UTC_TIMESTAMP());",
          f"SET @e{suffix}=LAST_INSERT_ID();"]
    for suffix, name, company, admin in (("a",a,"a",1),("b",b,"b",0),("t",target,"a",0)):
        statements += [
          f"INSERT INTO Roles (Nombre,NombreNormalizado,Descripcion,EsSistema,EsAdministrador,Ambito,EmpresaId,Activo,Eliminado,FechaCreacion) VALUES ('{name}','{name.upper()}','Fixture temporal QA FN05',0,{admin},1,@e{company},1,0,UTC_TIMESTAMP());",
          f"SET @r{suffix}=LAST_INSERT_ID();"]
    for suffix, name, role, company in (("a",a,"a","a"),("b",b,"b","b"),("t",target,"t","a"),("s",shared,"t","a")):
        statements += [
          f"INSERT INTO Usuarios (NombreUsuario,NombreCompleto,PasswordHash,RolId,Activo,Bloqueado,Eliminado,FechaCreacion) VALUES ('{name}','{name}','{hashes[name]}',@r{role},1,0,0,UTC_TIMESTAMP());",
          f"SET @u{suffix}=LAST_INSERT_ID();",
          f"INSERT INTO UsuarioEmpresas (UsuarioId,EmpresaId,RolId,Activa,FechaCreacion,FechaActualizacion) VALUES (@u{suffix},@e{company},@r{role},1,UTC_TIMESTAMP(),UTC_TIMESTAMP());"]
    # Inactive second membership still makes the identity shared.
    statements += [
       "INSERT INTO UsuarioEmpresas (UsuarioId,EmpresaId,RolId,Activa,FechaCreacion,FechaActualizacion) VALUES (@us,@eb,@rb,0,UTC_TIMESTAMP(),UTC_TIMESTAMP());"]
    for role, grants in (("a",(view_user,edit_user,view_role,matrix)),("b",(view_user,view_role))):
        for p in grants:
            statements.append(f"INSERT INTO RolPermisos (RolId,PermisoId) VALUES (@r{role},{p});")
    statements += ["COMMIT;","SELECT @ea,@eb,@ua,@ub,@ut,@us,@ra,@rb,@rt;"]
    created = True
    ids = sql("\n".join(statements), "create isolated administrative fixtures").split("\t")
    require(len(ids)==9 and all(x.isdigit() and int(x)>0 for x in ids),"Fixture readback missing")
    ea,eb,ua,ub,ut,us,ra,rb,rt = map(int,ids)
    require(ea!=eb and len({ua,ub,ut,us})==4 and len({ra,rb,rt})==3,"Fixture identity collision")
    print("QA_FN05_EPHEMERAL_IDENTITIES=PASS",flush=True)
    ta = login(a,passwords[a],"QA_FN05_LOGIN_A")
    tb = login(b,passwords[b],"QA_FN05_LOGIN_B")
    login(target,passwords[target],"QA_FN05_TARGET_LOGIN_BEFORE")

    call("/usuarios","QA_FN05_ANONYMOUS",401)
    rows_a = data(call("/usuarios","QA_FN05_USERS_A",token=ta,tenant=ea))
    rows_b = data(call("/usuarios","QA_FN05_USERS_B",token=tb,tenant=eb))
    require({x["id"] for x in rows_a}=={ua,ut,us},"Unexpected identities in A")
    require({x["id"] for x in rows_b}=={ub,us},"Unexpected identities in B")
    require(next(x for x in rows_a if x["id"]==us)["rolId"]==rt,"Shared identity A role mismatch")
    require(next(x for x in rows_b if x["id"]==us)["rolId"]==rb and next(x for x in rows_b if x["id"]==us)["activo"] is False,"Shared identity B inactive membership mismatch")
    page = data(call("/usuarios/paginado?page=1&pageSize=1","QA_FN05_PAGED_A",token=ta,tenant=ea))
    require(page.get("totalCount")==3 and len(page.get("items",[]))==1,"Tenant pagination mismatch")
    call(f"/usuarios/{ub}","QA_FN05_FOREIGN_USER",404,token=ta,tenant=ea)
    call("/usuarios","QA_FN05_FOREIGN_SELECTOR",403,token=ta,tenant=eb)
    own = data(call(f"/usuarios/{us}/empresas","QA_FN05_MEMBERSHIPS_A",token=ta,tenant=ea))
    require(len(own)==1 and own[0].get("empresaId")==ea,"Foreign memberships exposed")
    roles_a = data(call("/roles","QA_FN05_ROLES_A",token=ta,tenant=ea))
    roles_b = data(call("/roles","QA_FN05_ROLES_B",token=tb,tenant=eb))
    require({ra,rt}.issubset({r["id"] for r in roles_a}) and rb not in {r["id"] for r in roles_a},"Foreign role exposed A")
    require(rb in {r["id"] for r in roles_b} and not {ra,rt}.intersection({r["id"] for r in roles_b}),"Foreign role exposed B")
    call(f"/roles/{rb}","QA_FN05_FOREIGN_ROLE",404,token=ta,tenant=ea)
    call(f"/permisos/matriz/{rt}","QA_FN05_MATRIX_OWN",token=ta,tenant=ea)
    call(f"/permisos/matriz/{rb}","QA_FN05_MATRIX_FOREIGN",(400,403,404,422),token=ta,tenant=ea)
    call(f"/permisos/matriz/{rb}","QA_FN05_MATRIX_DENIED",403,token=tb,tenant=eb)
    direct = data(call(f"/usuarios/{ut}","QA_FN05_RENDER_DIRECT_USER",token=ta,tenant=ea,base=backend))
    require(direct.get("id")==ut and direct.get("rolId")==rt,"Render/proxy target mismatch")
    call(f"/usuarios/{ut}","QA_FN05_RESET_WITHOUT_GRANT",403,token=ta,tenant=ea,
         body=user_update(target,rt,new_target),method="PUT")
    require(sql(f"SELECT PasswordHash FROM Usuarios WHERE Id={ut} AND NombreUsuario='{target}';","hash before grant")==hashes[target],"Unauthorized hash change")
    sql(f"INSERT INTO RolPermisos (RolId,PermisoId) SELECT Id,{reset_user} FROM Roles WHERE Id={ra} AND Nombre='{a}' AND EmpresaId={ea};","grant only fixture A")
    call(f"/usuarios/{ut}","QA_FN05_SAME_PASSWORD_REJECTED",(400,422),token=ta,tenant=ea,
         body=user_update(target,rt,passwords[target]),method="PUT")
    call(f"/usuarios/{us}","QA_FN05_SHARED_PASSWORD_DENIED",403,token=ta,tenant=ea,
         body=user_update(shared,rt,new_target),method="PUT")
    call(f"/usuarios/{ub}","QA_FN05_FOREIGN_PASSWORD_DENIED",403,token=ta,tenant=ea,
         body=user_update(b,rb,new_target),method="PUT")
    call(f"/usuarios/{ut}","QA_FN05_RESET_OWN",token=ta,tenant=ea,
         body=user_update(target,rt,new_target),method="PUT")
    login(target,passwords[target],"QA_FN05_OLD_PASSWORD_REJECTED",401)
    login(target,new_target,"QA_FN05_NEW_PASSWORD_AUTHENTICATES")
    require(sql(f"SELECT PasswordHash FROM Usuarios WHERE Id={us} AND NombreUsuario='{shared}';","shared hash")==hashes[shared],"Shared hash changed")
    require(sql(f"SELECT PasswordHash FROM Usuarios WHERE Id={ub} AND NombreUsuario='{b}';","foreign hash")==hashes[b],"Foreign hash changed")
    actual_hash = sql(f"SELECT PasswordHash FROM Usuarios WHERE Id={ut} AND NombreUsuario='{target}';","new hash")
    require(actual_hash!=hashes[target] and re.fullmatch(r"\$2[aby]\$12\$[./A-Za-z0-9]{53}",actual_hash) is not None,"Password hash was not safely changed")
    audit = sql(f"SELECT ValoresNuevos FROM RegistrosAuditoria WHERE EmpresaId={ea} AND UsuarioId={ua} AND ReferenciaId={ut} AND Modulo='Usuarios' AND Accion='Editar' AND Entidad='Usuario';","tenant reset audit")
    require(json.loads(audit).get("PasswordRestablecida") is True,"Tenant audit missing")
    require(all(v not in audit for v in (passwords[target],new_target,hashes[target],actual_hash)),"Credential present in audit")
    print("QA_FN05_RESET_AUDIT=PASS",flush=True)
finally:
    if created:
        sql(f"""START TRANSACTION;
UPDATE Usuarios SET Activo=0 WHERE NombreUsuario IN {names};
UPDATE UsuarioEmpresas ue JOIN Usuarios u ON ue.UsuarioId=u.Id SET ue.Activa=0 WHERE u.NombreUsuario IN {names};
DELETE rp FROM RolPermisos rp JOIN Roles r ON rp.RolId=r.Id WHERE r.Nombre IN {names};
UPDATE Roles SET Activo=0 WHERE Nombre IN {names};
UPDATE Empresas SET Activa=0 WHERE Nombre IN {names};
COMMIT;""","revoke only run-owned access")
        require(sql(f"""SELECT (SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario IN {names} AND Activo=1)
+(SELECT COUNT(*) FROM Empresas WHERE Nombre IN {names} AND Activa=1)
+(SELECT COUNT(*) FROM Roles WHERE Nombre IN {names} AND Activo=1)
+(SELECT COUNT(*) FROM RolPermisos rp JOIN Roles r ON rp.RolId=r.Id WHERE r.Nombre IN {names})
+(SELECT COUNT(*) FROM UsuarioEmpresas ue JOIN Usuarios u ON ue.UsuarioId=u.Id WHERE u.NombreUsuario IN {names} AND ue.Activa=1);""","revocation verification")=="0","Fixture revocation incomplete")
        print("QA_FN05_EPHEMERAL_REVOKE=PASS",flush=True)
        sql(f"""START TRANSACTION;
DELETE ue FROM UsuarioEmpresas ue JOIN Usuarios u ON ue.UsuarioId=u.Id WHERE u.NombreUsuario IN {names};
DELETE FROM Usuarios WHERE NombreUsuario IN {names};
DELETE FROM Roles WHERE Nombre IN {names};
DELETE e FROM Empresas e LEFT JOIN RegistrosAuditoria r ON r.EmpresaId=e.Id WHERE e.Nombre IN {names} AND r.Id IS NULL;
COMMIT;""","cleanup only run-owned fixtures, preserve audit")
        require(sql(f"""SELECT (SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario IN {names})
+(SELECT COUNT(*) FROM Roles WHERE Nombre IN {names})
+(SELECT COUNT(*) FROM Empresas WHERE Nombre IN {names} AND Activa=1);""","cleanup verification")=="0","Fixture cleanup incomplete")
        print("QA_FN05_EPHEMERAL_CLEANUP=PASS",flush=True)
print("QA_FN05_LIVE_ADMINISTRATION=PASS",flush=True)
