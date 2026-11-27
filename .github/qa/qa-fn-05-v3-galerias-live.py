"""QA V3 real category-gallery operations; synthetic tenant identities only."""
import struct
import zlib
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


require(expected_sha == "dea277293408751b1b49416a8164e1392e80a483", "Unexpected deployed SHA")
from decimal import Decimal
prefix = "qag" + secrets.token_hex(5)
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
modules=("CATEGORIAS",)
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




def png(rgb):
    def chunk(kind, payload):
        return struct.pack(">I", len(payload)) + kind + payload + struct.pack(">I", zlib.crc32(kind + payload) & 0xffffffff)
    row = b"\x00" + bytes(rgb) * 32
    return (b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB",32,32,8,2,0,0,0))
            + chunk(b"IDAT", zlib.compress(row * 32)) + chunk(b"IEND",b""))

def gallery_write(route, label, token, tenant, version, order, files=(), expected=200):
    boundary = "qa-gallery-" + secrets.token_hex(16)
    body = []
    for name,value in [("Version",version), *[("Orden",x) for x in order]]:
        body.append(f'--{boundary}\r\nContent-Disposition: form-data; name="{name}"\r\n\r\n{value}\r\n'.encode())
    for i,content in enumerate(files):
        body.append(f'--{boundary}\r\nContent-Disposition: form-data; name="Archivos"; filename="qa-{i}.png"\r\nContent-Type: image/png\r\n\r\n'.encode()+content+b"\r\n")
    raw = b"".join(body) + f"--{boundary}--\r\n".encode()
    headers = {"Accept":"application/json","Content-Type":"multipart/form-data; boundary="+boundary}
    if token: headers["Authorization"]="Bearer "+token
    if tenant: headers["X-Empresa-Id"]=str(tenant)
    request=urllib.request.Request(proxy+route,data=raw,headers=headers,method="PUT")
    try:
        response=opener.open(request,timeout=50)
    except urllib.error.HTTPError as error:
        response=error
    with response:
        statuses=(expected,) if isinstance(expected,int) else expected
        require(response.status in statuses,f"{label}: expected HTTP {statuses}, got {response.status}")
        require(response.headers.get("X-Solqaryn-Environment")=="QA",label+": wrong proxy environment")
        value=data(json.load(response)) if response.status==200 else None
    print("QA_GALLERY_"+label+"=PASS",flush=True)
    return value

def gallery_read(route,label,token=None,tenant=None,expected=200):
    response=call(route,"QA_GALLERY_"+label,expected,token=token,tenant=tenant)
    return data(response) if expected==200 else None

def image_ids(gallery):
    require(isinstance(gallery,dict) and isinstance(gallery.get("version"),str) and isinstance(gallery.get("imagenes"),list),"Invalid gallery contract")
    images=gallery["imagenes"]
    ids=[i["id"] for i in images]
    require(all(isinstance(i,str) and re.fullmatch(r"[0-9a-fA-F]{32}",i) for i in ids) and len(set(ids))==len(ids),"Invalid gallery image IDs")
    require(all(isinstance(i.get("url"),str) and i["url"].startswith("https://") for i in images),"Invalid gallery image URL")
    return ids

def persisted(eid,cid,gallery):
    version=gallery["version"]
    require(re.fullmatch(r"[0-9a-fA-F]{32}",version) is not None,"Invalid version")
    require(sql(f"SELECT COUNT(*) FROM CategoriasGalerias WHERE EmpresaId={eid} AND CategoriaId={cid} AND CategoriaVerificadaId={cid} AND Version='{version}' AND JSON_LENGTH(ImagenesJson)={len(image_ids(gallery))};","gallery persisted")== "1","Gallery persistence or ownership mismatch")
    raw=sql(f"SELECT ImagenesJson FROM CategoriasGalerias WHERE EmpresaId={eid} AND CategoriaId={cid};","gallery order")
    stored=json.loads(raw)
    require([i["Id"] for i in stored]==image_ids(gallery),"Persisted image order mismatch")


owned=[]
created=False
try:
    wait_ready(backend,"QA_GALLERY_RENDER_READY")
    wait_ready(proxy,"QA_GALLERY_PROXY_READY")
    # All identities are synthetic and unique; no existing role or permission is changed.
    statements=["START TRANSACTION;"]
    for suffix,n in (("a",a),("b",b)):
        statements.extend([f"INSERT INTO Empresas (Nombre,Activa,FechaCreacion,FechaActualizacion) VALUES ('{n}',1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));",f"SET @e{suffix}=LAST_INSERT_ID();"])
    for suffix,n,company,admin in (("a",a,"a",1),("b",b,"b",1),("o",operator,"b",0)):
        statements.extend([f"INSERT INTO Roles (Nombre,NombreNormalizado,Descripcion,EsSistema,EsAdministrador,Ambito,EmpresaId,Activo,Eliminado,FechaCreacion) VALUES ('{n}','{n.upper()}','Fixture QA galerias',0,{admin},1,@e{company},1,0,UTC_TIMESTAMP(6));",f"SET @r{suffix}=LAST_INSERT_ID();",f"INSERT INTO Usuarios (NombreUsuario,NombreCompleto,PasswordHash,RolId,Activo,Bloqueado,Eliminado,FechaCreacion) VALUES ('{n}','{n}','{hashes[n]}',@r{suffix},1,0,0,UTC_TIMESTAMP(6));",f"SET @u{suffix}=LAST_INSERT_ID();",f"INSERT INTO UsuarioEmpresas (UsuarioId,EmpresaId,RolId,Activa,FechaCreacion,FechaActualizacion) VALUES (@u{suffix},@e{company},@r{suffix},1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));"])
        if admin:
            statements.extend(f"INSERT INTO RolPermisos (RolId,PermisoId) VALUES (@r{suffix},{p});" for p in grants)
    statements.extend(["COMMIT;","SELECT @ea,@eb,@ua,@ub,@uo,@ra,@rb,@ro;"])
    created=True
    ids=sql("\n".join(statements),"create gallery fixtures").split("\t")
    require(len(ids)==8 and all(x.isdigit() and int(x)>0 for x in ids),"Missing fixture ids")
    ea,eb,ua,ub,uo,ra,rb,ro=map(int,ids)
    require(ea!=eb and len({ua,ub,uo})==3 and len({ra,rb,ro})==3,"Fixture identity collision")
    for eid,n in ((ea,a),(eb,b)):
        require(sql(f"SELECT COUNT(*) FROM Empresas WHERE Id={eid} AND Nombre='{n}' AND Activa=1;","fixture owner")=="1","Unexpected fixture company")
        require(sql(f"SELECT COUNT(*) FROM Categorias WHERE EmpresaId={eid};","fixture empty")=="0","Nonempty fixture")
    ta=login(a,passwords[a],"QA_GALLERY_LOGIN_A")
    tb=login(b,passwords[b],"QA_GALLERY_LOGIN_B")
    to=login(operator,passwords[operator],"QA_GALLERY_LOGIN_OPERATOR")

    identities=((ta,ea,"A"),(tb,eb,"B"))
    current={}
    for token,eid,side in identities:
        name=prefix+"-"+side+"-galeria"
        item=data(call("/categorias","QA_GALLERY_CREATE_"+side,201,token=token,tenant=eid,body={"nombre":name,"activa":True},method="POST"))
        cid=item["id"]
        require(type(cid) is int and cid>0,"Invalid category ID")
        require(sql(f"SELECT COUNT(*) FROM Categorias WHERE Id={cid} AND EmpresaId={eid} AND Nombre='{name}';","category ownership")=="1","Category not owned by fixture")
        route=f"/categorias/{cid}/imagenes"
        owned.append((token,eid,cid,route,name))
        current[side]=(route,cid)
        initial=gallery_read(route,"EMPTY_"+side,token,eid)
        require(initial["version"]=="" and image_ids(initial)==[],"New gallery must be empty")
    require(current["A"][1]!=current["B"][1],"Shared category ID")
    a_route,a_id=current["A"]
    b_route,b_id=current["B"]
    gallery_read(a_route,"ANONYMOUS",expected=401)
    gallery_read(a_route,"OPERATOR_DENIED",to,eb,403)
    gallery_read(a_route,"FOREIGN_SELECTOR",ta,eb,403)
    gallery_write(b_route,"OPERATOR_WRITE_DENIED",to,eb,"",[],expected=403)
    denial=(400,403,404,409,422)
    snapshots={}
    for token,eid,side in identities:
        route,cid=current[side]
        other="B" if side=="A" else "A"
        foreign,_=current[other]
        gallery_read(foreign,"FOREIGN_READ_"+side,token,eid,(403,404))
        gallery_write(foreign,"FOREIGN_WRITE_"+side,token,eid,"",[],expected=(403,404))
        gallery_write(route,"SIX_IMAGES_DENIED_"+side,token,eid,"",["n:"+str(i) for i in range(6)],[png((20,40,60))]*6,expected=denial)
        gallery_write(route,"FAKE_IMAGE_DENIED_"+side,token,eid,"",["n:0"],[b"not a png"],expected=denial)
        require(image_ids(gallery_read(route,"STILL_EMPTY_"+side,token,eid))==[],"Rejected write mutated gallery")
        photos=[png((i*31, 45, 180 if side=="A" else 90)) for i in range(5)]
        uploaded=gallery_write(route,"UPLOAD_FIVE_"+side,token,eid,"",["n:"+str(i) for i in range(5)],photos)
        ids=image_ids(uploaded)
        require(len(ids)==5,"Expected five uploaded images")
        persisted(eid,cid,uploaded)
        require(gallery_read(route,"READBACK_"+side,token,eid)==uploaded,"Gallery readback mismatch")
        ordered=gallery_write(route,"REORDER_"+side,token,eid,uploaded["version"],["e:"+i for i in reversed(ids)])
        require(image_ids(ordered)==list(reversed(ids)) and ordered["version"]!=uploaded["version"],"Gallery order or version mismatch")
        persisted(eid,cid,ordered)
        gallery_write(route,"STALE_VERSION_"+side,token,eid,uploaded["version"],["e:"+i for i in ids],expected=denial)
        gallery_write(route,"DUPLICATE_REFERENCE_"+side,token,eid,ordered["version"],["e:"+ids[0]]*2,expected=denial)
        require(gallery_read(route,"REJECTED_WRITES_INTACT_"+side,token,eid)==ordered,"Rejected write changed gallery")
        snapshots[side]=ordered
    require(not set(image_ids(snapshots["A"])).intersection(image_ids(snapshots["B"])),"Gallery image IDs shared")
    for token,eid,side in identities:
        route,cid=current[side]
        own=snapshots[side]
        foreign=snapshots["B" if side=="A" else "A"]
        gallery_write(route,"FOREIGN_IMAGE_DENIED_"+side,token,eid,own["version"],["e:"+image_ids(foreign)[0]],expected=denial)
        require(gallery_read(route,"CROSS_INTACT_"+side,token,eid)==own,"Foreign image modified own gallery")
        retained=image_ids(own)[1:]
        removed=gallery_write(route,"REMOVE_ONE_"+side,token,eid,own["version"],["e:"+i for i in retained])
        require(image_ids(removed)==retained,"Image removal mismatch")
        persisted(eid,cid,removed)
        require(int(sql(f"SELECT COUNT(*) FROM RegistrosAuditoria WHERE EmpresaId={eid} AND ReferenciaId={cid} AND Entidad='CategoriaGaleria' AND Resultado='Exito';","gallery audit"))>=3,"Gallery audit missing")
    print("QA_GALLERY_OPERATIONS_AB=PASS",flush=True)
finally:
    try:
        # Only records created and verified in this invocation may be cleared.
        # Remove through the service before logically deleting their category;
        # do not delete storage paths directly or mutate other gallery rows.
        for token,eid,cid,route,name in owned:
            require(sql(f"SELECT COUNT(*) FROM Categorias WHERE Id={cid} AND EmpresaId={eid} AND Nombre='{name}';","cleanup owner")=="1","Fixture ownership changed")
            state=gallery_read(route,"CLEANUP_READ",token,eid)
            if image_ids(state):
                cleared=gallery_write(route,"CLEANUP_EMPTY",token,eid,state["version"],[])
                require(image_ids(cleared)==[],"Gallery cleanup failed")
                persisted(eid,cid,cleared)
            call(f"/categorias/{cid}","QA_GALLERY_CATEGORY_DELETE",200,token=token,tenant=eid,method="DELETE")
            require(sql(f"SELECT COUNT(*) FROM Categorias WHERE Id={cid} AND EmpresaId={eid} AND Activa=0;","category cleanup")=="1","Fixture category still active")
    finally:
        if created:
            sql(f"""START TRANSACTION;
UPDATE Usuarios SET Activo=0 WHERE NombreUsuario IN {names};
UPDATE UsuarioEmpresas ue JOIN Usuarios u ON ue.UsuarioId=u.Id SET ue.Activa=0 WHERE u.NombreUsuario IN {names};
DELETE rp FROM RolPermisos rp JOIN Roles r ON r.Id=rp.RolId WHERE r.Nombre IN {names};
UPDATE Roles SET Activo=0 WHERE Nombre IN {names};
UPDATE Empresas SET Activa=0 WHERE Nombre IN {names};
COMMIT;""","revoke gallery fixtures")
            require(sql(f"""SELECT (SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario IN {names} AND Activo=1)
+(SELECT COUNT(*) FROM UsuarioEmpresas ue JOIN Usuarios u ON ue.UsuarioId=u.Id WHERE u.NombreUsuario IN {names} AND ue.Activa=1)
+(SELECT COUNT(*) FROM RolPermisos rp JOIN Roles r ON r.Id=rp.RolId WHERE r.Nombre IN {names})
+(SELECT COUNT(*) FROM Roles WHERE Nombre IN {names} AND Activo=1)
+(SELECT COUNT(*) FROM Empresas WHERE Nombre IN {names} AND Activa=1);""","gallery cleanup readback")=="0","Fixture authority not revoked")
            print("QA_GALLERY_FIXTURES_REVOKED=PASS",flush=True)
print("QA_GALLERY_LIVE_CERTIFICATE=PASS",flush=True)
