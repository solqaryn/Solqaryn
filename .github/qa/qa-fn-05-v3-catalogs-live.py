"""QA V3 deployed catalog ownership and CRUD through real authenticated API; only isolated fixture companies."""
import json
import csv
import io
import tempfile
import pathlib
import zipfile
import xml.etree.ElementTree as ET
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
    if result.returncode != 0:
        # Do not print raw SQL, error text or connection details.
        diagnostic = re.search(r"ERROR ([0-9]{4})(?: \(([A-Z0-9]{5})\))?", result.stderr)
        code = diagnostic.group(1) if diagnostic else "unknown"
        state = diagnostic.group(2) if diagnostic and diagnostic.group(2) else "unknown"
        raise RuntimeError(f"SQL stage failed: {stage}; MySQL code={code}; SQLSTATE={state}")
    return result.stdout.strip()
require(sql("SELECT DATABASE(), SUBSTRING_INDEX(CURRENT_USER(),'@',1);","identity").split("\t")==["solqaryn_qa","solqaryn_qa_user"],"QA database identity mismatch")
expected_sha = os.environ.get("EXPECTED_DEPLOYED_SHA", "")
require(re.fullmatch(r"[0-9a-f]{40}", expected_sha) is not None, "Expected deployment SHA required")
# The caller must independently verify this SHA via Render deploy metadata
# before and after the run. This is not a server-reported revision.
print("EXPECTED_RENDER_SHA=" + expected_sha, flush=True)
print("RENDER_SHA_REQUIRES_EXTERNAL_ATTESTATION", flush=True)


require(expected_sha == "77ca9b08b971566218d3a705d69a8f30bdeec9ab", "Unexpected deployed SHA")
from decimal import Decimal
prefix = "qa5" + secrets.token_hex(5)
a,b,operator,platform = (prefix+x for x in ("A","B","O","P"))
fixture_names=(a,b,operator)
names="("+",".join("'"+x+"'" for x in fixture_names)+")"
for table,column in (("Empresas","Nombre"),("Roles","Nombre"),("Usuarios","NombreUsuario")):
    require(sql(f"SELECT COUNT(*) FROM {table} WHERE {column} IN {names};","fixture collision")=="0","Fixture collision")
def grant(code):
    require(re.fullmatch(r"[A-Z.]+",code) is not None,"Unexpected grant")
    value=sql(f"SELECT Id FROM Permisos WHERE Codigo='{code}' AND Ambito=1 AND Activo=1 AND Eliminado=0;","shipping grant")
    require(value.isdigit(),"Missing unique active shipping permission")
    return int(value)
modules=("CATEGORIAS","MARCAS","MODELOS","COLORES","TALLAS","TIPOSCLIENTES","METODOSPAGO","DESCUENTOS","IMPUESTOS")
grants=tuple(grant(m+"."+action) for m in modules for action in ("VER","CREAR","EDITAR","ACTIVAR","DESACTIVAR","ELIMINARLOGICO"))
def new_password():
    return "Qa!7" + secrets.token_urlsafe(32)
passwords = {n: new_password() for n in (a,b,operator,platform)}
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



root="/costos-envio"
name=prefix+"-tarifa"
city=prefix+"-ciudad"
coverage={"ciudad":city}
def payload(amount):
    return {"nombre":name,"ciudad":city,"monto":amount,"prioridad":0,"activo":True,"esPredeterminado":False}
def api(path,label,expected=200,token=None,tenant=None,body=None,method="GET"):
    return call(path,"QA_FN05_ENVIO_"+label,expected,token=token,tenant=tenant,body=body,method=method)
def amount(item,expected):
    require(Decimal(str(item["monto"]))==Decimal(str(expected)),"Unexpected tariff amount")
created=False
try:
    wait_ready(backend,"QA_FN05_ENVIO_RENDER_READY")
    wait_ready(proxy,"QA_FN05_ENVIO_PROXY_READY")
    # All identities are synthetic and unique; no existing role or permission is changed.
    statements=["START TRANSACTION;"]
    for suffix,n in (("a",a),("b",b)):
        statements.extend([f"INSERT INTO Empresas (Nombre,Activa,FechaCreacion,FechaActualizacion) VALUES ('{n}',1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));",f"SET @e{suffix}=LAST_INSERT_ID();"])
    for suffix,n,company,admin in (("a",a,"a",1),("b",b,"b",1),("o",operator,"b",0)):
        statements.extend([f"INSERT INTO Roles (Nombre,NombreNormalizado,Descripcion,EsSistema,EsAdministrador,Ambito,EmpresaId,Activo,Eliminado,FechaCreacion) VALUES ('{n}','{n.upper()}','Fixture QA FN05 envios',0,{admin},1,@e{company},1,0,UTC_TIMESTAMP(6));",f"SET @r{suffix}=LAST_INSERT_ID();",f"INSERT INTO Usuarios (NombreUsuario,NombreCompleto,PasswordHash,RolId,Activo,Bloqueado,Eliminado,FechaCreacion) VALUES ('{n}','{n}','{hashes[n]}',@r{suffix},1,0,0,UTC_TIMESTAMP(6));",f"SET @u{suffix}=LAST_INSERT_ID();",f"INSERT INTO UsuarioEmpresas (UsuarioId,EmpresaId,RolId,Activa,FechaCreacion,FechaActualizacion) VALUES (@u{suffix},@e{company},@r{suffix},1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));"])
        if admin:
            statements.extend(f"INSERT INTO RolPermisos (RolId,PermisoId) VALUES (@r{suffix},{p});" for p in grants)
    statements.extend(["COMMIT;","SELECT @ea,@eb,@ua,@ub,@uo,@ra,@rb,@ro;"])
    created=True
    ids=sql("\n".join(statements),"create shipping fixtures").split("\t")
    require(len(ids)==8 and all(x.isdigit() and int(x)>0 for x in ids),"Missing fixture ids")
    ea,eb,ua,ub,uo,ra,rb,ro=map(int,ids)
    require(ea!=eb and len({ua,ub,uo})==3 and len({ra,rb,ro})==3,"Fixture identity collision")
    for eid,n in ((ea,a),(eb,b)):
        require(sql(f"SELECT COUNT(*) FROM Empresas WHERE Id={eid} AND Nombre='{n}' AND Activa=1;","fixture owner")=="1","Unexpected fixture company")
        require(sql(f"SELECT COUNT(*) FROM CostosEnvio WHERE EmpresaId={eid};","fixture empty")=="0","Nonempty fixture")
    ta=login(a,passwords[a],"QA_V3_CATALOG_LOGIN_A")
    tb=login(b,passwords[b],"QA_V3_CATALOG_LOGIN_B")
    to=login(operator,passwords[operator],"QA_V3_CATALOG_LOGIN_OPERATOR")
    def invoke(route,tag,token=None,tenant=None,body=None,method="GET",expected=200):
        return call(route,"QA_V3_CATALOG_"+tag,expected,token=token,tenant=tenant,body=body,method=method)
    records={}
    contexts=((ta,ea,"A"),(tb,eb,"B"))
    table_specs=(("categorias","Categorias","activa"),("marcas","Marcas","activo"),("modelos","Modelos","activo"),
                 ("colores","Colores","activo"),("tallas","Tallas","activo"),("tipo-clientes","TipoClientes","activo"),
                 ("metodos-pago","MetodosPago","activo"),("descuentos","Descuentos","activo"),("impuestos","Impuestos","activo"))
    deny=(400,403,404,409,422)
    for route,table,state in table_specs:
        root="/"+route
        def payload(side,edited=False):
            item={"nombre":prefix+"-"+table+("-E" if edited else "")}
            if route=="modelos": item["catalogoPadreId"]=records["Marcas"][side]
            if route=="colores": item["codigoVisual"]="#2B8A3E"
            if route=="tallas": item["nombre"]="M" if edited else "S"
            if route=="tipo-clientes": item.update({"colorHex":"#FFFFFF","activo":True,"esPredeterminado":False})
            if route=="metodos-pago": item.update({"codigo":prefix.upper(),"tipo":"Efectivo","activo":True})
            if route=="descuentos": item.update({"codigoPromocional":prefix.upper(),"tipo":"Porcentaje","valor":6 if edited else 5,"categoriaIds":[records["Categorias"][side]]})
            if route=="impuestos": item.update({"codigo":prefix.upper(),"tipo":"Porcentaje","tasa":16 if edited else 15,"operaciones":["Venta","Compra"],"categoriaIds":[records["Categorias"][side]]})
            return item
        invoke(root,table.upper()+"_ANON",expected=401)
        invoke(root,table.upper()+"_OPERATOR_DENIED",to,eb,expected=403)
        invoke(root,table.upper()+"_FOREIGN_SELECTOR",ta,eb,expected=403)
        records[table]={}
        for token,eid,side in contexts:
            created_item=data(invoke(root,table.upper()+"_CREATE_"+side,token,eid,payload(side),method="POST",expected=201))
            rid=created_item.get("id")
            require(type(rid) is int and rid>0 and created_item.get(state) is True,"Invalid catalog creation")
            records[table][side]=rid
            require(sql(f"SELECT COUNT(*) FROM {table} WHERE Id={rid} AND EmpresaId={eid};","persisted owner")=="1","Owner not persisted")
        require(records[table]["A"]!=records[table]["B"],"Shared catalog ID")
        for token,eid,side in contexts:
            other="B" if side=="A" else "A"
            rid,foreign=records[table][side],records[table][other]
            own=root+"/"+str(rid); cross=root+"/"+str(foreign)
            rows=data(invoke(root,table.upper()+"_LIST_"+side,token,eid))
            require(isinstance(rows,list) and rid in {x["id"] for x in rows} and foreign not in {x["id"] for x in rows},"Catalog list isolation")
            require(data(invoke(own,table.upper()+"_READ_"+side,token,eid))["id"]==rid,"Own read mismatch")
            invoke(cross,table.upper()+"_FOREIGN_READ_"+side,token,eid,expected=(403,404))
            edited=payload(side,True)
            amended=data(invoke(own,table.upper()+"_EDIT_"+side,token,eid,edited,"PUT"))
            require(amended.get("nombre")==edited["nombre"],"Own edit mismatch")
            invoke(cross,table.upper()+"_FOREIGN_EDIT_"+side,token,eid,edited,"PUT",deny)
            invoke(cross+"/desactivar",table.upper()+"_FOREIGN_DISABLE_"+side,token,eid,method="PATCH",expected=deny)
            invoke(cross,table.upper()+"_FOREIGN_DELETE_"+side,token,eid,method="DELETE",expected=deny)
            if route=="modelos":
                invalid=dict(edited,catalogoPadreId=records["Marcas"][other])
                invoke(own,table.upper()+"_FOREIGN_BRAND_DENIED_"+side,token,eid,invalid,"PUT",deny)
            if route in ("descuentos","impuestos"):
                invalid=dict(edited,categoriaIds=[records["Categorias"][other]])
                invoke(own,table.upper()+"_FOREIGN_CATEGORY_DENIED_"+side,token,eid,invalid,"PUT",deny)
                intact=data(invoke(own,table.upper()+"_LINK_INTACT_"+side,token,eid))
                require(intact["categoriaIds"]==[records["Categorias"][side]],"Foreign relation modified")
                link_table="DescuentoCategorias" if route=="descuentos" else "ImpuestoCategorias"
                fk="DescuentoId" if route=="descuentos" else "ImpuestoId"
                require(sql(f"SELECT COUNT(*) FROM {link_table} WHERE {fk}={rid} AND CategoriaId={records['Categorias'][side]} AND EmpresaId={eid};","scoped relation")=="1","Relation ownership missing")
            for action,active in (("desactivar",False),("activar",True)):
                changed=data(invoke(own+"/"+action,table.upper()+"_"+action.upper()+"_"+side,token,eid,method="PATCH"))
                require(changed.get(state) is active,"State transition mismatch")
        for token,eid,side in contexts:
            rid=records[table][side]
            item=data(invoke(root+"/"+str(rid),table.upper()+"_INTACT_"+side,token,eid))
            require(item.get("nombre")==payload(side,True)["nombre"] and item.get(state) is True,"Cross-tenant mutation changed record")
        if route=="metodos-pago":
            for token,eid,side in contexts:
                rows=data(invoke(root+"/activos",table.upper()+"_ACTIVE_"+side,token,eid))
                require(records[table][side] in {x["id"] for x in rows} and records[table]["B" if side=="A" else "A"] not in {x["id"] for x in rows},"Active payment list leaked")
            rows=data(invoke(root+"/activos","PAYMENT_MEMBER_READ_WITHOUT_WRITE",to,eb))
            require(records[table]["B"] in {x["id"] for x in rows} and records[table]["A"] not in {x["id"] for x in rows},"Membership read scope mismatch")
            invoke(root,"PAYMENT_MEMBER_WRITE_DENIED",to,eb,payload("B"),"POST",403)
        print("QA_V3_CATALOG_"+table.upper()+"_AB=PASS",flush=True)
    for route,table,state in reversed(table_specs):
        for token,eid,side in contexts:
            rid=records[table][side]
            invoke("/"+route+"/"+str(rid),table.upper()+"_DELETE_"+side,token,eid,method="DELETE")
            active_column="Activa" if table=="Categorias" else "Activo"
            require(sql(f"SELECT COUNT(*) FROM {table} WHERE Id={rid} AND EmpresaId={eid} AND {active_column}=0;","logical delete")=="1","Logical deletion not persisted")
    print("QA_V3_CATALOG_CRUD_AB=PASS",flush=True)
finally:
    if created:
        # Revoke only synthetic identities and tariffs. Preserve audit evidence.
        sql(f"""START TRANSACTION;
UPDATE Descuentos d JOIN Empresas e ON e.Id=d.EmpresaId SET d.Activo=0 WHERE e.Nombre IN {names};
UPDATE Impuestos i JOIN Empresas e ON e.Id=i.EmpresaId SET i.Activo=0 WHERE e.Nombre IN {names};
UPDATE Usuarios SET Activo=0 WHERE NombreUsuario IN {names};
UPDATE UsuarioEmpresas ue JOIN Usuarios u ON ue.UsuarioId=u.Id SET ue.Activa=0 WHERE u.NombreUsuario IN {names};
DELETE rp FROM RolPermisos rp JOIN Roles r ON r.Id=rp.RolId WHERE r.Nombre IN {names};
UPDATE Roles SET Activo=0 WHERE Nombre IN {names};
UPDATE Empresas SET Activa=0 WHERE Nombre IN {names};
COMMIT;""","revoke run-owned shipping authority")
        require(sql(f"""SELECT (SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario IN {names} AND Activo=1)
+(SELECT COUNT(*) FROM UsuarioEmpresas ue JOIN Usuarios u ON ue.UsuarioId=u.Id WHERE u.NombreUsuario IN {names} AND ue.Activa=1)
+(SELECT COUNT(*) FROM RolPermisos rp JOIN Roles r ON r.Id=rp.RolId WHERE r.Nombre IN {names})
+(SELECT COUNT(*) FROM Roles WHERE Nombre IN {names} AND Activo=1)
+(SELECT COUNT(*) FROM Empresas WHERE Nombre IN {names} AND Activa=1)
+(SELECT COUNT(*) FROM Descuentos d JOIN Empresas e ON e.Id=d.EmpresaId WHERE e.Nombre IN {names} AND d.Activo=1)
+(SELECT COUNT(*) FROM Impuestos i JOIN Empresas e ON e.Id=i.EmpresaId WHERE e.Nombre IN {names} AND i.Activo=1);""","shipping cleanup readback")=="0","Fixture access not revoked")
        print("QA_FN05_ENVIO_FIXTURES_REVOKED=PASS",flush=True)
print("QA_V3_CATALOG_LIVE_CERTIFICATE=PASS",flush=True)
