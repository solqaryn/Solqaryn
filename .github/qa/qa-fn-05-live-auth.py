"""QA-FN-05 live administrative probe; only run-owned ephemeral rows."""
import json
import datetime
import time
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
require(os.environ.get("DB_HOST") == "solqaryn-mysql-solqaryn.h.aivencloud.com" and os.environ.get("DB_PORT") == "14402" and bool(os.environ.get("DB_PASSWORD")), "QA database binding mismatch")
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
    require(count.isdigit() and owned == "1", "Commercial ownership not certified: " + table)
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
commercial_grants = tuple(grant(mod + "." + action) for mod in ("PRODUCTOS", "CLIENTES", "PROVEEDORES") for action in ("VER", "CREAR", "EDITAR", "ACTIVAR", "DESACTIVAR", "ELIMINARLOGICO"))
configuration_grants = tuple(grant(code) for code in ("CONFIGURACION.VER", "CONFIGURACION.CREAR", "CONFIGURACION.EDITAR", "DASHBOARD.VER", "AUDITORIA.VER"))
plan_code = (prefix + "_PLAN").upper()
require(sql(f"SELECT COUNT(*) FROM Planes WHERE Codigo='{plan_code}';", "test plan collision") == "0", "Fixture plan collision")

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

def wait_ready(base, label, budget=180):
    # Only idempotent health GETs may be retried. Never retry mutations.
    deadline = time.monotonic() + budget
    attempts = 0
    while time.monotonic() < deadline:
        attempts += 1
        status = "transport"
        try:
            req = urllib.request.Request(base + "/health/ready", headers={"Accept":"application/json"}, method="GET")
            try:
                response = opener.open(req, timeout=min(45, max(1, deadline-time.monotonic())))
            except urllib.error.HTTPError as error:
                response = error
            with response:
                status = response.status
                if status == 200:
                    if base == proxy:
                        require(response.headers.get("X-Solqaryn-Environment") == "QA", label+": wrong proxy environment")
                    payload = json.load(response)
                    require(payload.get("status") == "ready" and payload.get("database") == "connected", label+": unexpected readiness payload")
                    print(label+"=PASS", flush=True)
                    return
                require(status in (502, 503, 504), f"{label}: unexpected HTTP {status}")
        except (urllib.error.URLError, TimeoutError):
            pass
        print(f"{label}_READINESS_RETRY attempt={attempts} status={status}",flush=True)
        remaining = deadline-time.monotonic()
        if remaining > 0:
            time.sleep(min(8,remaining))
    raise RuntimeError(f"{label}: readiness budget exhausted")
def call(path, label, expected=200, token=None, tenant=None, body=None, method=None, base=proxy, form=False, idem=None):
    headers = {"Accept":"application/json"}
    if token: headers["Authorization"] = "Bearer " + token
    if tenant: headers["X-Empresa-Id"] = str(tenant)
    if idem is not None: headers["Idempotency-Key"] = idem
    encoded = None
    if body is not None and form:
        # Mirror Angular FormData exactly; Vercel can parse urlencoded bodies
        # into objects which the proxy serializes as JSON under the old MIME.
        boundary = "qa-fn05-" + secrets.token_hex(24)
        chunks = []
        for key, value in body.items():
            require(re.fullmatch(r"[A-Za-z0-9_.\[\]]+", key) is not None, "Invalid form field")
            chunks.append(f'--{boundary}\r\nContent-Disposition: form-data; name="{key}"\r\n\r\n{value}\r\n'.encode("utf-8"))
        encoded = b"".join(chunks) + f"--{boundary}--\r\n".encode("ascii")
        headers["Content-Type"] = "multipart/form-data; boundary=" + boundary
    elif body is not None:
        headers["Content-Type"] = "application/json"
        encoded = json.dumps(body).encode()
    req = urllib.request.Request(base+path, data=encoded, headers=headers, method=method)
    try:
        r = opener.open(req, timeout=40)
    except urllib.error.HTTPError as error:
        r = error
    with r:
        statuses = (expected,) if isinstance(expected,int) else expected
        if r.status not in statuses:
            # Report status and validation field names only. No response values,
            # request data, tokens, credentials, SQL or user content in logs.
            diagnostic = {}
            try:
                diagnostic = json.loads(r.read(65536))
            except (ValueError, UnicodeError):
                pass
            fields = diagnostic.get("errors", {}) if isinstance(diagnostic, dict) else {}
            if isinstance(fields, dict):
                known = {"nombre","marca","modelo","tipoinventario","cantidad","costo","precio","umbralstockbajo","categoriaid","variantes","tipoclienteid","documento","identidadortn","direccion","moneda","zonahoraria","impuestosjson","emisionjson","version"}
                safe_fields = sorted(k for k in fields if isinstance(k,str) and k.lower() in known)
                print(f"{label}_VALIDATION_FIELDS={','.join(safe_fields)};TOTAL={len(fields)}",flush=True)
            raise RuntimeError(f"{label}: expected HTTP {statuses}, got {r.status}")
        if base == proxy:
            require(r.headers.get("X-Solqaryn-Environment") == "QA", label+": wrong proxy environment")
        data = json.load(r) if r.status in (200, 201) else None
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
    # Warm the verified QA backend first, then confirm the QA proxy.
    wait_ready(backend,"QA_FN05_RENDER_READY")
    wait_ready(proxy,"QA_FN05_PROXY_READY")
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
    for role, grants in (("a",(view_user,edit_user,view_role,matrix)+commercial_grants+configuration_grants),("b",(view_user,view_role)+commercial_grants+configuration_grants)):
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
    # The previous deployment passed every administrative assertion through
    # QA_FN05_RESET_AUDIT. Resume on the same immutable runtime code instead
    # of resetting passwords again. Full mode remains available for future changes.
    if os.environ.get("QA_FN05_MODE","full") == "commercial":
        require(expected_sha == "a3a730750920f56e0d4250b3d5a223f57da24c90","Prior administration evidence requires exact QA runtime with verified product-only diff")
        require(os.environ.get("QA_FN05_PRIOR_ADMIN_RUN") == "45872602673","Prior evidence required")
        print("QA_FN05_ADMINISTRATION_PRIOR_EVIDENCE=45872602673",flush=True)
    else:
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

    # Business actions over the real proxy; SQL checks persisted owner only.
    doc_number = str(1000000000000 + secrets.randbelow(9000000000000))
    for route, table, form in (("productos","Productos",True),("clientes","Clientes",False),("proveedores","Proveedores",False)):
        def payload(name):
            if route == "productos":
                return {"Nombre":name,"Marca":"","Modelo":"","TipoInventario":1,"EsDestacado":"false","Cantidad":0,"Costo":5,"Precio":10,"UmbralStockBajo":2}
            if route == "clientes":
                return {"nombre":name,"identidadORTN":doc_number}
            return {"nombre":name,"documento":doc_number}
        def fixture_id(name,token,tenant,suffix):
            item = data(call("/"+route,"QA_FN05_"+table.upper()+"_CREATE_"+suffix,201,token=token,tenant=tenant,body=payload(name),method="POST",form=form))
            require(isinstance(item,dict) and type(item.get("id")) is int and item["id"]>0,"Missing created identifier")
            return item["id"]
        ca,cb = fixture_id(a,ta,ea,"A"),fixture_id(b,tb,eb,"B")
        require(ca!=cb,"Catalog fixture identity collision")
        for item,name,token,tenant,foreign,other in ((ca,a,ta,ea,cb,eb),(cb,b,tb,eb,ca,ea)):
            tag = "QA_FN05_"+table.upper()+("_A" if tenant==ea else "_B")
            own = data(call(f"/{route}/{item}",tag+"_READ",token=token,tenant=tenant))
            require(own.get("id")==item and own.get("nombre")==name,"Owned record mismatch")
            require(sql(f"SELECT COUNT(*) FROM {table} WHERE Id={item} AND EmpresaId={tenant} AND Nombre='{name}';","persisted owner")=="1","Catalog owner mismatch")
            listed = data(call("/"+route,tag+"_LIST",token=token,tenant=tenant))
            rows = listed.get("items") if isinstance(listed,dict) else listed
            require(isinstance(rows,list) and {x["id"] for x in rows}=={item},"Catalog list is not isolated")
            call(f"/{route}/{foreign}",tag+"_FOREIGN_READ",(403,404),token=token,tenant=tenant)
            edit_payload = payload(name)
            if route == "productos":
                # The identical body must succeed for an owned product first.
                own_edit = data(call(f"/{route}/{item}",tag+"_VALID_OWN_EDIT",token=token,tenant=tenant,body=edit_payload,method="PUT",form=form))
                require(own_edit.get("id")==item and own_edit.get("nombre")==name,"Owned edit did not persist")
                owner_token = tb if tenant==ea else ta
                before = data(call(f"/{route}/{foreign}",tag+"_FOREIGN_OWNER_BEFORE",token=owner_token,tenant=other))
                call(f"/{route}/{foreign}",tag+"_FOREIGN_EDIT",404,token=token,tenant=tenant,body=edit_payload,method="PUT",form=form)
                after = data(call(f"/{route}/{foreign}",tag+"_FOREIGN_OWNER_AFTER",token=owner_token,tenant=other))
                require(before==after,"Rejected cross-tenant edit altered product or variants")
                print(tag+"_FOREIGN_EDIT_UNCHANGED=PASS",flush=True)
            else:
                call(f"/{route}/{foreign}",tag+"_FOREIGN_EDIT",(403,404),token=token,tenant=tenant,body=edit_payload,method="PUT",form=form)
            call(f"/{route}/{foreign}/desactivar",tag+"_FOREIGN_STATE",(403,404),token=token,tenant=tenant,method="PATCH")
            call(f"/{route}/{foreign}",tag+"_FOREIGN_DELETE",(403,404),token=token,tenant=tenant,method="DELETE")
            call("/"+route,tag+"_FOREIGN_SELECTOR",403,token=token,tenant=other)
        if route=="productos":
            va=sql(f"SELECT Id FROM ProductoVariantes WHERE ProductoId={ca} AND EmpresaId={ea} AND EsTecnica=1 AND Eliminado=0;","variant A")
            vb=sql(f"SELECT Id FROM ProductoVariantes WHERE ProductoId={cb} AND EmpresaId={eb} AND EsTecnica=1 AND Eliminado=0;","variant B")
            require(va.isdigit() and vb.isdigit() and va!=vb,"Technical variant owner missing")
            call(f"/productos/{cb}/variantes/{vb}","QA_FN05_VARIANT_FOREIGN_A",(403,404),token=ta,tenant=ea)
            call(f"/productos/{ca}/variantes/{va}","QA_FN05_VARIANT_FOREIGN_B",(403,404),token=tb,tenant=eb)
            print("QA_FN05_VARIANT_OWNERS=PASS",flush=True)
        else:
            call("/"+route,"QA_FN05_"+table.upper()+"_DUPLICATE_A",(400,409,422),token=ta,tenant=ea,body=payload(a+"_duplicate"),method="POST")
        amended=a+"_edited"
        changed=data(call(f"/{route}/{ca}","QA_FN05_"+table.upper()+"_EDIT_A",token=ta,tenant=ea,body=payload(amended),method="PUT",form=form))
        require(changed.get("nombre")==amended,"Catalog edit not persisted")
        for action,state in (("desactivar",False),("activar",True)):
            result=data(call(f"/{route}/{ca}/{action}","QA_FN05_"+table.upper()+"_"+action.upper(),token=ta,tenant=ea,method="PATCH"))
            require(result.get("activo") is state,"Catalog status mismatch")
        unchanged=data(call(f"/{route}/{cb}","QA_FN05_"+table.upper()+"_B_INTACT",token=tb,tenant=eb))
        require(unchanged.get("nombre")==b and unchanged.get("activo") is True,"Foreign record changed")
        call(f"/{route}/{ca}","QA_FN05_"+table.upper()+"_DELETE_A",token=ta,tenant=ea,method="DELETE")
        if route=="productos":
            call(f"/{route}/{ca}","QA_FN05_PRODUCTOS_DELETED_NOT_READABLE",404,token=ta,tenant=ea)
            call(f"/{route}/{ca}","QA_FN05_PRODUCTOS_DELETED_NOT_EDITABLE",404,token=ta,tenant=ea,body=payload(amended),method="PUT",form=form)
            require(sql(f"SELECT COUNT(*) FROM Productos WHERE Id={ca} AND EmpresaId={ea} AND Eliminado=1 AND Activo=0;","product logical delete")=="1","Product history not preserved")
        else:
            deleted=data(call(f"/{route}/{ca}","QA_FN05_"+table.upper()+"_INACTIVE_HISTORY",token=ta,tenant=ea))
            require(deleted.get("id")==ca and deleted.get("activo") is False,"Logical deletion must preserve inactive history")
            active=data(call(f"/{route}/activos","QA_FN05_"+table.upper()+"_ACTIVE_EXCLUSION",token=ta,tenant=ea))
            require(isinstance(active,list) and all(x["id"]!=ca for x in active),"Inactive record exposed as active")
            require(sql(f"SELECT COUNT(*) FROM {table} WHERE Id={ca} AND EmpresaId={ea} AND Activo=0;","logical delete readback")=="1","Inactive record history not preserved")
        print("QA_FN05_"+table.upper()+"_AB=PASS",flush=True)
    conf_a=data(call(f"/empresa-configuracion/tenant/{ea}","QA_FN05_CONFIG_A",token=ta,tenant=ea))
    conf_b=data(call(f"/empresa-configuracion/tenant/{eb}","QA_FN05_CONFIG_B",token=tb,tenant=eb))
    require(conf_a.get("empresaId")==ea and conf_b.get("empresaId")==eb and conf_a.get("nombre")==a and conf_b.get("nombre")==b,"Config scope mismatch")
    call(f"/empresa-configuracion/tenant/{eb}","QA_FN05_CONFIG_FOREIGN",403,token=ta,tenant=ea)
    config_payload={k:conf_a[k] for k in ("nombre","rtn","direccion","moneda","zonaHoraria","impuestosJson","emisionJson","correoRemitente","correoNombreRemitente","version") if k in conf_a}
    config_payload["direccion"]="QA fixture "+prefix
    updated=data(call(f"/empresa-configuracion/tenant/{ea}","QA_FN05_CONFIG_EDIT",token=ta,tenant=ea,body=config_payload,method="PUT"))
    require(updated.get("empresaId")==ea and updated.get("direccion")==config_payload["direccion"] and updated.get("version")==conf_a["version"]+1,"Config mutation mismatch")
    call(f"/empresa-configuracion/tenant/{eb}","QA_FN05_CONFIG_FOREIGN_EDIT",403,token=ta,tenant=ea,body=config_payload,method="PUT")
    require(data(call(f"/empresa-configuracion/tenant/{eb}","QA_FN05_CONFIG_B_INTACT",token=tb,tenant=eb))==conf_b,"Foreign config changed")
    print("QA_FN05_CONFIG_AB=PASS",flush=True)
    # Test-only counters and plan; no external billing or real numbering.
    kind=(prefix+"_SEQ").upper()
    sql(f"""START TRANSACTION;
INSERT INTO SecuenciasDocumento (EmpresaId,TipoDocumento,UltimoValor,Prefijo,LongitudNumero,Activa,FechaCreacion) VALUES ({ea},'{kind}',0,'QA-',6,1,UTC_TIMESTAMP()),({eb},'{kind}',0,'QA-',6,1,UTC_TIMESTAMP());
INSERT INTO Planes (Codigo,Nombre,Activo,FechaCreacion) VALUES ('{plan_code}','{prefix}',1,UTC_TIMESTAMP());
SET @plan=LAST_INSERT_ID();
INSERT INTO PlanLimites (PlanId,Clave,ValorMaximo) VALUES (@plan,'QA_TEST_LIMIT',7);
COMMIT;""","seed isolated sequence and test plan")
    for token,tenant,suffix in ((ta,ea,"A"),(tb,eb,"B")):
        current=data(call(f"/secuencias-documento?empresaId={tenant}&tipoDocumento={kind}","QA_FN05_SEQUENCE_"+suffix+"_READ",token=token,tenant=tenant))
        require(current.get("empresaId")==tenant and current.get("ultimoValor")==0,"Initial counter mismatch")
        seq=data(call("/secuencias-documento/siguiente","QA_FN05_SEQUENCE_"+suffix+"_RESERVE",token=token,tenant=tenant,body={"empresaId":tenant,"tipoDocumento":kind}))
        require(seq.get("empresaId")==tenant and seq.get("valor")==1,"Tenant counter not reserved")
    call(f"/secuencias-documento?empresaId={eb}&tipoDocumento={kind}","QA_FN05_SEQUENCE_FOREIGN",403,token=ta,tenant=ea)
    call("/secuencias-documento/siguiente","QA_FN05_SEQUENCE_FOREIGN_WRITE",403,token=ta,tenant=ea,body={"empresaId":eb,"tipoDocumento":kind})
    require(sql(f"SELECT COUNT(*) FROM SecuenciasDocumento WHERE EmpresaId IN ({ea},{eb}) AND TipoDocumento='{kind}' AND UltimoValor=1;","independent counter readback")=="2","Counter isolation mismatch")
    print("QA_FN05_SEQUENCE_AB=PASS",flush=True)
    begin=(datetime.datetime.now(datetime.timezone.utc)-datetime.timedelta(minutes=1)).isoformat()
    subs_payload={"planCodigo":plan_code,"inicioUtc":begin}
    subscription_ids=[]
    for token,tenant,suffix in ((ta,ea,"A"),(tb,eb,"B")):
        path=f"/saas/tenants/{tenant}"
        subscription=data(call(path+"/onboarding","QA_FN05_SUBSCRIPTION_"+suffix,token=token,tenant=tenant,body=subs_payload,idem=prefix))
        require(type(subscription.get("id")) is int and subscription["id"]>0 and subscription.get("planCodigo")==plan_code,"Subscription fixture mismatch")
        replay=data(call(path+"/onboarding","QA_FN05_SUBSCRIPTION_"+suffix+"_REPLAY",token=token,tenant=tenant,body=subs_payload,idem=prefix))
        require(replay.get("id")==subscription["id"],"Subscription idempotency mismatch")
        actual=data(call(path+"/suscripcion","QA_FN05_SUBSCRIPTION_"+suffix+"_READ",token=token,tenant=tenant))
        require(actual.get("id")==subscription["id"],"Subscription readback mismatch")
        limits=data(call(path+"/limites?clave=QA_TEST_LIMIT","QA_FN05_LIMITS_"+suffix,token=token,tenant=tenant))
        require(limits.get("total")==1 and limits.get("items")==[{"clave":"QA_TEST_LIMIT","valorMaximo":7}],"Subscription limit mismatch")
        subscription_ids.append(subscription["id"])
    require(len(set(subscription_ids))==2,"Subscription leaked across companies")
    call(f"/saas/tenants/{eb}/suscripcion","QA_FN05_SUBSCRIPTION_FOREIGN",403,token=ta,tenant=ea)
    call(f"/saas/tenants/{eb}/limites","QA_FN05_LIMITS_FOREIGN",403,token=ta,tenant=ea)
    print("QA_FN05_SUBSCRIPTIONS_AB=PASS",flush=True)
    for token,tenant,suffix in ((ta,ea,"A"),(tb,eb,"B")):
        data(call("/dashboard/resumen","QA_FN05_DASHBOARD_"+suffix,token=token,tenant=tenant))
        data(call("/auditoria","QA_FN05_AUDIT_"+suffix,token=token,tenant=tenant))
    print("QA_FN05_LIVE_COMMERCIAL=PASS",flush=True)
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
        # Preserve audit, revoke first, clean only run-owned fixture records.
        sql(f"""START TRANSACTION;
DELETE l FROM SuscripcionSaaSIdempotencia l JOIN Empresas e ON e.Id=l.EmpresaId JOIN Suscripciones s ON s.Id=l.SuscripcionId JOIN Planes p ON p.Id=s.PlanId WHERE e.Nombre IN {names} AND p.Codigo='{plan_code}';
DELETE s FROM Suscripciones s JOIN Empresas e ON e.Id=s.EmpresaId JOIN Planes p ON p.Id=s.PlanId WHERE e.Nombre IN {names} AND p.Codigo='{plan_code}';
DELETE l FROM PlanLimites l JOIN Planes p ON p.Id=l.PlanId WHERE p.Codigo='{plan_code}';
DELETE FROM Planes WHERE Codigo='{plan_code}';
DELETE s FROM SecuenciasDocumento s JOIN Empresas e ON e.Id=s.EmpresaId WHERE e.Nombre IN {names} AND s.TipoDocumento='{(prefix+"_SEQ").upper()}';
DELETE c FROM ConfigEmpresas c JOIN Empresas e ON e.Id=c.EmpresaId WHERE e.Nombre IN {names};
DELETE i FROM ProductoImagenes i JOIN Empresas e ON e.Id=i.EmpresaId WHERE e.Nombre IN {names};
DELETE v FROM ProductoVariantes v JOIN Empresas e ON e.Id=v.EmpresaId WHERE e.Nombre IN {names};
DELETE p FROM Productos p JOIN Empresas e ON e.Id=p.EmpresaId WHERE e.Nombre IN {names};
DELETE c FROM Clientes c JOIN Empresas e ON e.Id=c.EmpresaId WHERE e.Nombre IN {names};
DELETE p FROM Proveedores p JOIN Empresas e ON e.Id=p.EmpresaId WHERE e.Nombre IN {names};
COMMIT;""","cleanup isolated commercial fixture rows")
        print("QA_FN05_COMMERCIAL_CLEANUP=PASS",flush=True)
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
if os.environ.get("QA_FN05_MODE","full") == "full":
    print("QA_FN05_LIVE_ADMINISTRATION=PASS",flush=True)
print("QA_FN05_LIVE_RUN=PASS",flush=True)
