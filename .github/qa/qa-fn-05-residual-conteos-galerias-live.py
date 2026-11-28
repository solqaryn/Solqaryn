"""Directed QA live boundary and conteos transition probes. Only unique synthetic tenants."""
import sys
import hashlib
mode = sys.argv[1] if len(sys.argv) == 2 else ""
if mode not in ("gallery", "conteos"):
    raise RuntimeError("Explicit QA residual probe required")
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
import urllib.parse
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


require(expected_sha == "cf3b75c8de9289838fac8db68cbe45662de38b4a", "Unexpected deployed SHA")
from decimal import Decimal
prefix = "qar" + secrets.token_hex(5)
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
grants=tuple(grant("CATEGORIAS."+action) for action in ("VER","CREAR","EDITAR","ACTIVAR","DESACTIVAR","ELIMINARLOGICO")) if mode=="gallery" else tuple(grant("MOVIMIENTOSINVENTARIO."+action) for action in ("VER","CREAR","EDITAR","CAMBIARESTADO","CERRAR","APROBAR","ANULAR"))
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

def gallery_write(route, label, token, tenant, version, order, files=(), expected=200, base=proxy):
    require(base in (proxy, backend), "Unexpected gallery endpoint")
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
    request=urllib.request.Request(base+route,data=raw,headers=headers,method="PUT")
    try:
        response=opener.open(request,timeout=50)
    except urllib.error.HTTPError as error:
        response=error
    with response:
        statuses=(expected,) if isinstance(expected,int) else expected
        require(response.status in statuses,f"{label}: expected HTTP {statuses}, got {response.status}")
        if base == proxy:
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
    require([i["Url"] for i in stored]==[i["url"] for i in gallery["imagenes"]],"Persisted URL mismatch")
    tenant_folder=f"solqaryn_qa/solqaryn/productos/empresas/{eid}/"
    for image in stored:
        public_id=image["PublicId"]
        url=urllib.parse.urlsplit(image["Url"])
        path=urllib.parse.unquote(url.path)
        require(isinstance(public_id,str) and public_id.startswith(tenant_folder),"Cloudinary publicId outside verified QA tenant")
        require(url.scheme=="https" and url.hostname=="res.cloudinary.com" and url.port in (None,443)
                and not url.username and not url.password and "/image/upload/" in path
                and "/"+public_id+"." in path,"Cloudinary URL outside verified QA tenant")
    print("QA_GALLERY_STORAGE_LOCATORS_VERIFIED=PASS",flush=True)
    return [hashlib.sha256(i["PublicId"].encode("utf-8")).hexdigest().upper() for i in stored]


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
    denial=(400,403,404,409,422)
    if mode=="gallery":
        def png_size(size):
            image=png((24,90,164))
            def chunk(kind,payload):
                return struct.pack(">I",len(payload))+kind+payload+struct.pack(">I",zlib.crc32(kind+payload)&0xffffffff)
            remaining=size-len(image)
            chunks=[image[:-12]]
            while remaining:
                part=min(remaining,1024*1024)
                if 0<remaining-part<15:
                    part-=15-(remaining-part)
                require(part>=15,"Invalid PNG fixture size")
                chunks.append(chunk(b"tEXt",b"Q\x00"+b"A"*(part-14)))
                remaining-=part
            raw=b"".join(chunks)+image[-12:]
            require(len(raw)==size,"PNG fixture exact size mismatch")
            return raw
        limit=15*1024*1024
        exact=png_size(limit)
        over=png_size(limit+1)
        for token,eid,side in identities:
            name=prefix+"-"+side+"-limite"
            item=data(call("/categorias","QA_RESIDUAL_GALLERY_CREATE_"+side,201,token=token,tenant=eid,body={"nombre":name,"activa":True},method="POST"))
            cid=item["id"]
            require(type(cid) is int and cid>0,"Invalid category ID")
            require(sql(f"SELECT COUNT(*) FROM Categorias WHERE Id={cid} AND EmpresaId={eid} AND Nombre='{name}';","category ownership")=="1","Category ownership mismatch")
            route=f"/categorias/{cid}/imagenes"
            owned.append((token,eid,cid,route,name))
            initial=gallery_read(route,"BOUNDARY_EMPTY_"+side,token,eid)
            require(image_ids(initial)==[],"Fixture gallery not empty")
            gallery_write(route,"API_15_MIB_PLUS_ONE_REJECTED_"+side,token,eid,initial["version"],["n:0"],[over],expected=(400,422),base=backend)
            require(gallery_read(route,"BOUNDARY_UNCHANGED_"+side,token,eid)==initial,"Oversize upload mutated gallery")
            result=gallery_write(route,"API_EXACT_15_MIB_"+side,token,eid,initial["version"],["n:0"],[exact],base=backend)
            require(len(image_ids(result))==1,"Exact-boundary image not persisted")
            receipts=persisted(eid,cid,result)
            require(len(receipts)==1,"Expected exact-boundary receipt reference")
            require(gallery_read(route,"BOUNDARY_API_READBACK_"+side,token,eid)==result,"Boundary readback mismatch")
            cleared=gallery_write(route,"BOUNDARY_API_CLEAR_"+side,token,eid,result["version"],[])
            require(image_ids(cleared)==[],"Fixture clear failed")
            persisted(eid,cid,cleared)
            print("QA_GALLERY_EXPECT_DELETE_API_"+side+"="+receipts[0],flush=True)
            with tempfile.TemporaryDirectory(prefix="solqaryn-qa-gallery-") as tmp:
                good=pathlib.Path(tmp)/"exact.png"
                bad=pathlib.Path(tmp)/"oversize.png"
                good.write_bytes(exact)
                bad.write_bytes(over)
                spec={"base":"https://solqarynqa-nu.vercel.app","user":a if side=="A" else b,"password":passwords[a if side=="A" else b],"tenant":eid,"side":side,"category":cid,"name":name,"good":str(good),"bad":str(bad)}
                browser=subprocess.run(["node",".github/qa/qa-fn-05-gallery-boundary-browser.mjs"],input=json.dumps(spec),text=True,capture_output=True,timeout=180)
                for line in browser.stdout.splitlines():
                    if re.fullmatch(r"QA_GALLERY_UI_[A-Z0-9_]+=(?:PASS|FAIL|[0-9]{1,9})",line):
                        print(line,flush=True)
                require(browser.returncode==0,"Authentic gallery boundary browser failed")
            state=gallery_read(route,"BOUNDARY_UI_READBACK_"+side,token,eid)
            require(len(image_ids(state))==1,"Optimized browser upload did not persist")
            receipts=persisted(eid,cid,state)
            require(len(receipts)==1,"Expected browser receipt reference")
            print("QA_GALLERY_EXPECT_DELETE_UI_"+side+"="+receipts[0],flush=True)
        print("QA_GALLERY_BOUNDARY_API_AND_BROWSER_AB=PASS",flush=True)
        print("QA_GALLERY_PHYSICAL_DELETE_REQUIRES_RENDER_RECEIPTS",flush=True)
    else:
        # The product/warehouse/stock seed follows the existing QA checkout fixture
        # contract. Only the two newly created companies are touched. Stock is
        # never confirmed/adjusted, and their document history is preserved.
        fixtures={}
        for token,eid,side in identities:
            name=a if side=="A" else b
            require(sql(f"SELECT COUNT(*) FROM Empresas WHERE Id={eid} AND Nombre='{name}' AND Activa=1;","conteos fixture owner")=="1","conteos fixture ownership mismatch")
            require(sql(f"SELECT COUNT(*) FROM Productos WHERE EmpresaId={eid};","conteos fixture empty")=="0","conteos fixture not empty")
            key=prefix+side
            values=sql(f"""START TRANSACTION;
INSERT INTO Sucursales (EmpresaId,Codigo,Nombre,Activa,Eliminado,ZonaHoraria,FechaCreacion,FechaActualizacion) VALUES ({eid},'{key}','{key}',1,0,'America/Tegucigalpa',UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @s=LAST_INSERT_ID();
INSERT INTO Almacenes (SucursalId,Codigo,Nombre,Tipo,Activo,Eliminado,FechaCreacion,FechaActualizacion) VALUES (@s,'{key}','{key}',1,1,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @w=LAST_INSERT_ID();
INSERT INTO Productos (EmpresaId,Nombre,Marca,Modelo,TipoInventario,Cantidad,Costo,Precio,UmbralStockBajo,Activo,EsDestacado,Eliminado,FechaCreacion,FechaActualizacion) VALUES ({eid},'{key}','','',1,3,30,100,1,1,0,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @p=LAST_INSERT_ID();
INSERT INTO ProductoVariantes (EmpresaId,ProductoId,Sku,Cantidad,UmbralStockBajo,Costo,Precio,Activo,Eliminado,EsTecnica,FechaCreacion,FechaActualizacion) VALUES ({eid},@p,'{key}',3,1,30,100,1,0,1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @v=LAST_INSERT_ID();
INSERT INTO ExistenciasVariante (EmpresaId,ProductoVarianteId,AlmacenId,StockFisico,StockReservado,StockTransito,StockMinimo,FechaCreacion,FechaActualizacion) VALUES ({eid},@v,@w,3,0,0,1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
COMMIT;
SELECT @w,@p,@v;""","conteos fixture seed").split("\t")
            require(len(values)==3 and all(x.isdigit() and int(x)>0 for x in values),"Invalid conteos fixture IDs")
            warehouse,product,variant=map(int,values)
            require(sql(f"SELECT COUNT(*) FROM ExistenciasVariante e JOIN Almacenes al ON al.Id=e.AlmacenId JOIN Sucursales s ON s.Id=al.SucursalId WHERE e.EmpresaId={eid} AND s.EmpresaId={eid} AND e.AlmacenId={warehouse} AND e.ProductoVarianteId={variant} AND e.StockFisico=3;","physical fixture")=="1","Stock ownership mismatch")
            fixtures[side]=(warehouse,product,variant)
        root="/conteos-inventario"
        def payload(side,other=None):
            w,p,v=fixtures[side]
            return {"tipo":1,"almacenId":w,"esCiego":False,"observaciones":prefix+"-"+side,"productoVarianteIds":[fixtures[other][2] if other else v]}
        def request(path,label,token,eid,body=None,method=None,expected=200):
            result=call(root+path,"QA_COUNT_"+label,expected,token=token,tenant=eid,body=body,method=method)
            return data(result) if expected in (200,201) else None
        def persisted_count(eid,ident,state,variant=None,captured=None):
            require(sql(f"SELECT COUNT(*) FROM ConteosInventario WHERE EmpresaId={eid} AND Id={ident} AND Estado={state};","count persisted")=="1","Count state or ownership mismatch")
            if variant is not None:
                where="CantidadContada IS NULL" if captured is None else f"CantidadContada={captured}"
                require(sql(f"SELECT COUNT(*) FROM ConteoInventarioDetalles WHERE EmpresaId={eid} AND ConteoInventarioId={ident} AND ProductoVarianteId={variant} AND StockEsperadoSnapshot=3 AND SnapshotMaterializado=1 AND {where};","count detail")=="1","Count detail ownership or snapshot mismatch")
        def assert_state(doc,state):
            require(doc.get("estadoNombre")==state,"Unexpected conteos state")
        counts={}
        for token,eid,side in identities:
            own=payload(side)
            other="B" if side=="A" else "A"
            request("","FOREIGN_VARIANT_DENIED_"+side,token,eid,payload(side,other),"POST",denial)
            doc=request("","CREATE_"+side,token,eid,own,"POST",201)
            ident=doc["id"]
            require(type(ident) is int and ident>0,"Invalid count ID")
            assert_state(doc,"Borrador")
            persisted_count(eid,ident,1,fixtures[side][2])
            own["observaciones"]+="-editado"
            edited=request("/"+str(ident),"EDIT_DRAFT_"+side,token,eid,own,"PUT")
            assert_state(edited,"Borrador")
            require(edited.get("observaciones")==own["observaciones"],"Draft edit not reflected")
            persisted_count(eid,ident,1,fixtures[side][2])
            counts[side]=ident
        require(counts["A"]!=counts["B"],"Shared count identity")
        details={}
        for token,eid,side in identities:
            ident=counts[side]
            other="B" if side=="A" else "A"
            request("/"+str(counts[other]),"FOREIGN_READ_DENIED_"+side,token,eid,expected=(403,404))
            request("/"+str(counts[other])+"/iniciar","FOREIGN_START_DENIED_"+side,token,eid,method="POST",expected=(403,404))
            state=request("/"+str(ident),"DETAIL_"+side,token,eid)
            assert_state(state,"Borrador")
            require(len(state["detalles"])==1 and state["detalles"][0]["productoVarianteId"]==fixtures[side][2],"Unexpected count line")
            detail=state["detalles"][0]["id"]
            require(type(detail) is int and detail>0,"Invalid detail ID")
            details[side]=detail
            listed=request("?page=1&pageSize=20","LIST_"+side,token,eid)
            require({x["id"] for x in listed["items"]}=={ident},"List leaked another tenant")
        request("/"+str(counts["B"]),"OPERATOR_DENIED",to,eb,expected=403)
        request("/"+str(counts["B"])+"/iniciar","OPERATOR_WRITE_DENIED",to,eb,method="POST",expected=403)
        for token,eid,side in identities:
            ident=counts[side]
            own=fixtures[side][2]
            other="B" if side=="A" else "A"
            path="/"+str(ident)
            def persisted_start():
                return sql(f"SELECT DATE_FORMAT(FechaInicio,'%Y-%m-%dT%H:%i:%s.%f'),IniciadoPorUsuarioId,DATE_FORMAT(FechaActualizacion,'%Y-%m-%dT%H:%i:%s.%f'),Estado FROM ConteosInventario WHERE EmpresaId={eid} AND Id={ident};","start physical readback")
            def utc_value(value):
                require(isinstance(value,str) and re.fullmatch(r"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:Z|\+00:00)?",value) is not None,"Unexpected UTC timestamp contract")
                return datetime.datetime.fromisoformat(value.replace("Z","+00:00")).replace(tzinfo=datetime.timezone.utc)
            started=request(path+"/iniciar","START_"+side,token,eid,method="POST")
            assert_state(started,"EnProceso")
            persisted_count(eid,ident,2,own)
            before=persisted_start()
            parts=before.split("\t")
            require(len(parts)==4 and parts[1]==str(ua if side=="A" else ub) and parts[3]=="2","Start physical evidence incomplete")
            precision=sql("SELECT DATETIME_PRECISION FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='ConteosInventario' AND COLUMN_NAME='FechaInicio';","timestamp precision")
            require(precision=="6","Unexpected physical timestamp precision")
            persisted_time=utc_value(parts[0])
            require(abs(utc_value(started.get("fechaInicio"))-persisted_time)<=datetime.timedelta(microseconds=1),"First response differs from persisted timestamp beyond MySQL precision")
            readback=request(path,"START_READBACK_"+side,token,eid)
            require(utc_value(readback.get("fechaInicio"))==persisted_time,"Persisted start readback mismatch")
            repeated=request(path+"/iniciar","START_IDEMPOTENT_"+side,token,eid,method="POST")
            assert_state(repeated,"EnProceso")
            require(utc_value(repeated.get("fechaInicio"))==persisted_time,"Repeated start returned another persisted timestamp")
            require(persisted_start()==before,"Repeated start mutated persisted timestamp, actor or state")
            print("QA_COUNT_IDEMPOTENT_PHYSICAL_"+side+"=PASS",flush=True)
            request(path+"/cerrar","INCOMPLETE_CLOSE_DENIED_"+side,token,eid,method="POST",expected=denial)
            request(path+"/detalles/"+str(details[other])+"/captura","FOREIGN_LINE_DENIED_"+side,token,eid,{"cantidadContada":99},"PUT",denial)
            persisted_count(eid,ident,2,own)
            quantity=4 if side=="A" else 3
            if side=="A":
                captured=request(path+"/detalles/"+str(details[side])+"/captura","CAPTURE_SINGLE_A",token,eid,{"cantidadContada":quantity},"PUT")
            else:
                request(path+"/detalles/captura-lote","FOREIGN_BATCH_DENIED_B",token,eid,{"lineas":[{"detalleId":details[side],"cantidadContada":3},{"detalleId":details[other],"cantidadContada":99}]},"PUT",denial)
                persisted_count(eid,ident,2,own)
                captured=request(path+"/detalles/captura-lote","CAPTURE_BATCH_B",token,eid,{"lineas":[{"detalleId":details[side],"cantidadContada":quantity}]},"PUT")
            require(captured["detalles"][0]["cantidadContada"]==quantity,"Capture not reflected")
            persisted_count(eid,ident,2,own,quantity)
            closed=request(path+"/cerrar","CLOSE_"+side,token,eid,method="POST")
            assert_state(closed,"Cerrado")
            require(closed["detalles"][0]["diferencia"]==quantity-3,"Closed difference mismatch")
            persisted_count(eid,ident,3,own,quantity)
            approved=request(path+"/aprobar","APPROVE_"+side,token,eid,method="POST")
            assert_state(approved,"Aprobado")
            persisted_count(eid,ident,4,own,quantity)
            request(path+"/cancelar","APPROVED_CANCEL_DENIED_"+side,token,eid,{"motivo":"Prueba QA"},"POST",denial)
            if side=="A":
                adjustment=request(path+"/generar-ajuste","ADJUSTMENT_DRAFT_A",token,eid,method="POST")
                aid=adjustment["id"]
                require(type(aid) is int and aid>0,"Invalid adjustment draft")
                require(sql(f"SELECT COUNT(*) FROM AjustesInventario WHERE EmpresaId={eid} AND Id={aid} AND Estado='Borrador';","draft persisted")=="1","Adjustment was not a draft")
                require(sql(f"SELECT COUNT(*) FROM ConteoInventarioDetalles WHERE EmpresaId={eid} AND Id={details[side]} AND AjusteInventarioId={aid};","draft link")=="1","Adjustment link missing")
                repeated=request(path+"/generar-ajuste","ADJUSTMENT_IDEMPOTENT_A",token,eid,method="POST")
                require(repeated["id"]==aid,"Repeated request created another adjustment")
            else:
                request(path+"/generar-ajuste","ZERO_DIFFERENCE_DENIED_B",token,eid,method="POST",expected=denial)
            cancel_doc=request("","CREATE_CANCEL_FIXTURE_"+side,token,eid,payload(side),"POST",201)
            cancel_id=cancel_doc["id"]
            require(type(cancel_id) is int and cancel_id>0 and cancel_id!=ident,"Invalid cancel fixture ID")
            cancelled=request("/"+str(cancel_id)+"/cancelar","CANCEL_"+side,token,eid,{"motivo":"Fixture QA"},"POST")
            assert_state(cancelled,"Cancelado")
            persisted_count(eid,cancel_id,5)
            warehouse=fixtures[side][0]
            require(sql(f"SELECT COUNT(*) FROM ExistenciasVariante WHERE EmpresaId={eid} AND AlmacenId={warehouse} AND ProductoVarianteId={own} AND StockFisico=3 AND StockReservado=0 AND StockTransito=0;","stock invariant")=="1","Count/draft mutated stock")
            require(int(sql(f"SELECT COUNT(*) FROM RegistrosAuditoria WHERE EmpresaId={eid} AND ReferenciaId={ident} AND Entidad='ConteoInventario' AND Resultado='Exito';","count audit"))>=4,"Count audit absent")
            print("QA_COUNT_STOCK_UNCHANGED_"+side+"=PASS",flush=True)
        print("QA_COUNT_AUTHENTICATED_TRANSITIONS_AB=PASS",flush=True)

finally:
    try:
        # Only records created and verified in this invocation may be cleared.
        # Remove through the service before logically deleting their category;
        # do not delete storage paths directly or mutate other gallery rows.
        cleanup_failures=0
        for token,eid,cid,route,name in owned:
            try:
                require(sql(f"SELECT COUNT(*) FROM Categorias WHERE Id={cid} AND EmpresaId={eid} AND Nombre='{name}';","cleanup owner")=="1","Fixture ownership changed")
                state=gallery_read(route,"CLEANUP_READ",token,eid)
                if image_ids(state):
                    cleared=gallery_write(route,"CLEANUP_EMPTY",token,eid,state["version"],[])
                    require(image_ids(cleared)==[],"Gallery cleanup failed")
                    persisted(eid,cid,cleared)
                call(f"/categorias/{cid}","QA_GALLERY_CATEGORY_DELETE",200,token=token,tenant=eid,method="DELETE")
                require(sql(f"SELECT COUNT(*) FROM Categorias WHERE Id={cid} AND EmpresaId={eid} AND Activa=0 AND Eliminada=1;","category cleanup")=="1","Fixture category still active")
            except Exception:
                cleanup_failures+=1
                print("QA_GALLERY_FIXTURE_CLEANUP_FAILED",flush=True)
        require(cleanup_failures==0,"One or more fixture galleries require reconciliation")
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

print("QA_RESIDUAL_"+mode.upper()+"_CERTIFICATE=PASS",flush=True)
