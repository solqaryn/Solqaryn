"""QA FN05 shipping live certificate, using isolated, run-owned tenant fixtures."""
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


require(expected_sha == "7ef2a8181939e57363c374cae498bde2dc122f95", "Unexpected deployed SHA")
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
grants=(grant("FACTURACION.VER"),grant("FACTURACION.ADMINISTRAR"))
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
    ta=login(a,passwords[a],"QA_FN05_ENVIO_LOGIN_A")
    tb=login(b,passwords[b],"QA_FN05_ENVIO_LOGIN_B")
    to=login(operator,passwords[operator],"QA_FN05_ENVIO_LOGIN_OPERATOR")
    api(root+"/administracion","ANON_DENIED",401)
    api(root+"/administracion","OPERATOR_DENIED",403,to,eb)
    api(root,"OPERATOR_CREATE_DENIED",403,to,eb,payload(7),"POST")
    api(root+"/administracion","FOREIGN_HEADER_DENIED",403,ta,eb)
    tariffs=[]
    for label,token,eid,price in (("A",ta,ea,0),("B",tb,eb,37)):
        api(root+"/predeterminado",label+"_NO_DEFAULT",404,token,eid)
        item=data(api(root,label+"_CREATE",201,token,eid,payload(price),"POST"))
        rid=item["id"]
        require(type(rid) is int and rid>0,"Invalid tariff ID")
        amount(item,price)
        require(item["nombre"]==name and item["activo"] is True,"Fixture response mismatch")
        require(sql(f"SELECT COUNT(*) FROM CostosEnvio WHERE Id={rid} AND EmpresaId={eid};","tariff ownership")=="1","Owner mismatch")
        tariffs.append((label,token,eid,rid,price))
    require(tariffs[0][3]!=tariffs[1][3],"Shared tariff ID")
    for i,(label,token,eid,rid,price) in enumerate(tariffs):
        own=root+"/"+str(rid)
        other=root+"/"+str(tariffs[1-i][3])
        rows=data(api(root+"/administracion",label+"_LIST",token=token,tenant=eid))
        require({x["id"] for x in rows}=={rid},"Tenant list leaked")
        amount(data(api(own,label+"_READ",token=token,tenant=eid)),price)
        api(other,label+"_FOREIGN_READ",404,token,eid)
        api(other,label+"_FOREIGN_UPDATE",404,token,eid,payload(99),"PUT")
        api(other+"/estado",label+"_FOREIGN_STATE",404,token,eid,{"activo":False},"PATCH")
        api(other,label+"_FOREIGN_DELETE",404,token,eid,method="DELETE")
        api(other+"/historial",label+"_FOREIGN_HISTORY",404,token,eid)
        resolved=data(api(root+"/resolver",label+"_RESOLVE",token=token,tenant=eid,body=coverage,method="POST"))
        require(resolved["id"]==rid,"Resolved foreign tariff")
        amount(resolved,price)
        api(root+"/resolver",label+"_NO_COVERAGE",404,token,eid,{"ciudad":prefix+"-otra"},"POST")
        changed=price+5
        amount(data(api(own,label+"_UPDATE",token=token,tenant=eid,body=payload(changed),method="PUT")),changed)
        amount(data(api(own,label+"_UPDATE_READBACK",token=token,tenant=eid)),changed)
        api(own+"/estado",label+"_DISABLE",token=token,tenant=eid,body={"activo":False},method="PATCH")
        require(data(api(own,label+"_DISABLE_READBACK",token=token,tenant=eid))["activo"] is False,"Disable not persisted")
        api(root+"/resolver",label+"_DISABLED_NOT_RESOLVED",404,token,eid,coverage,"POST")
        api(own+"/estado",label+"_ENABLE",token=token,tenant=eid,body={"activo":True},method="PATCH")
        require(data(api(own,label+"_ENABLE_READBACK",token=token,tenant=eid))["activo"] is True,"Enable not persisted")
        amount(data(api(root+"/resolver",label+"_RESOLVE_AGAIN",token=token,tenant=eid,body=coverage,method="POST")),changed)
        require(int(sql(f"SELECT COUNT(*) FROM RegistrosAuditoria WHERE EmpresaId={eid} AND Entidad='CostoEnvio' AND ReferenciaId={rid};","scoped shipping audit"))>=4,"Missing audit events")
        data(api(own+"/historial",label+"_HISTORY",token=token,tenant=eid))
    for label,token,eid,rid,price in tariffs:
        own=root+"/"+str(rid)
        amount(data(api(own,label+"_FINAL_READBACK",token=token,tenant=eid)),price+5)
        require(sql(f"SELECT COUNT(*) FROM CostosEnvio WHERE Id={rid} AND EmpresaId={eid} AND Activo=1 AND Eliminado=0;","final owner read-back")=="1","Unexpected persisted mutation")
        api(own,label+"_DELETE",token=token,tenant=eid,method="DELETE")
        api(own,label+"_DELETED_READ",404,token,eid)
        require(sql(f"SELECT COUNT(*) FROM CostosEnvio WHERE Id={rid} AND EmpresaId={eid} AND Activo=0 AND Eliminado=1;","deleted read-back")=="1","Soft-delete not persisted")
    print("QA_FN05_ENVIO_CRUD_AB=PASS",flush=True)
finally:
    if created:
        # Revoke only synthetic identities and tariffs. Preserve audit evidence.
        sql(f"""START TRANSACTION;
UPDATE CostosEnvio c JOIN Empresas e ON e.Id=c.EmpresaId SET c.Activo=0,c.EsPredeterminado=0 WHERE e.Nombre IN {names};
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
+(SELECT COUNT(*) FROM CostosEnvio c JOIN Empresas e ON e.Id=c.EmpresaId WHERE e.Nombre IN {names} AND c.Activo=1);""","shipping cleanup readback")=="0","Fixture access not revoked")
        print("QA_FN05_ENVIO_FIXTURES_REVOKED=PASS",flush=True)
print("QA_FN05_ENVIO_RUNTIME=PASS",flush=True)
