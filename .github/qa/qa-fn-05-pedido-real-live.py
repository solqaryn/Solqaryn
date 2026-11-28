"""QA FN05 deployed draft checkout: exact synthetic fixtures, ERP read, no payment."""
import hashlib
import sys
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
def call(path, label, expected=200, token=None, tenant=None, body=None, method=None, base=proxy, form=False, idem=None, order_key=None):
    headers = {"Accept":"application/json"}
    if token: headers["Authorization"] = "Bearer " + token
    if tenant: headers["X-Empresa-Id"] = str(tenant)
    if idem is not None: headers["Idempotency-Key"] = idem
    if order_key is not None: headers["X-Storefront-Order-Key"] = order_key
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


from decimal import Decimal
require(expected_sha=="88dc7c095fd63474dfbbe5e7b5ad9d269b8ba5b4","Unexpected deployment baseline")
def n(query,label):
    value=sql(query,label)
    require(value.isdigit(),label+": expected numeric scalar")
    return int(value)
def store():
    matches=sql("""SELECT DISTINCT e.Id FROM Empresas e
WHERE e.Activa=1 AND EXISTS (SELECT 1 FROM EmpresaConfiguraciones c WHERE c.Activa=1
AND (BINARY LOWER(TRIM(e.Nombre))=BINARY LOWER(TRIM(c.NombreComercial))
OR BINARY LOWER(TRIM(e.Nombre))=BINARY LOWER(TRIM(c.NombreVisibleSistema)))) ORDER BY e.Id;""","configured storefront match").splitlines()
    active=sql("SELECT Id FROM Empresas WHERE Activa=1 ORDER BY Id;","active storefront owners").splitlines()
    require(all(i.isdigit() and int(i)>0 for i in matches+active),"Invalid storefront identity")
    if len(matches)==1: return int(matches[0]),True
    if len(active)==1: return int(active[0]),False
    raise RuntimeError("Public storefront owner not uniquely established; no fixtures created")
wait_ready(backend,"QA_CHECKOUT_RENDER_READY")
wait_ready(proxy,"QA_CHECKOUT_PROXY_READY")
require(sql("SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId='20261128163400_QaCotizacionEstadoV5';","V5 marker")=="1","V5 absent")
owner,matched=store()

# Dependency-aware cleanup. Only exact, run-owned identifiers are writable.
# No FOREIGN_KEY_CHECKS override, no broad prefix deletes, no audit deletion.
def cleanup_catalog(owner, prefix, recover=False):
    require(re.fullmatch(r"qafn5[0-9a-f]{12}",prefix) is not None,"Invalid fixture identity")
    a=prefix+"-A"; b=prefix+"-B"; s=prefix+"-S"; w=prefix+"-W"
    tariffs="'"+prefix+"-ZERO','"+prefix+"-PAID','"+prefix+"-FOREIGN'"
    def ids(query,label):
        rows=sql(query,label).splitlines()
        require(all(x.isdigit() and int(x)>0 for x in rows) and len(rows)==len(set(rows)), "Invalid fixture identifiers: "+label)
        return [int(x) for x in rows]
    def idlist(values):
        require(bool(values),"Empty fixture identifier list")
        return ",".join(map(str,values))
    def quoted(s):
        require(re.fullmatch(r"[A-Za-z0-9_]+",s) is not None,"Unexpected schema identifier")
        return "\x60"+s+"\x60"
    co=ids(f"SELECT Id FROM Empresas WHERE BINARY Nombre=BINARY '{b}';","fixture company")
    if not co:
        require(not recover,"Previous fixture company absent; establish recovery identity")
        require(n(f"SELECT (SELECT COUNT(*) FROM Productos WHERE Nombre IN ('{a}','{b}'))+(SELECT COUNT(*) FROM ProductoVariantes WHERE Sku IN ('{a}','{b}'))+(SELECT COUNT(*) FROM CostosEnvio WHERE Nombre IN ({tariffs}))+(SELECT COUNT(*) FROM Sucursales WHERE Codigo='{s}')+(SELECT COUNT(*) FROM Almacenes WHERE Codigo='{w}');","absent fixture graph")==0,"Incomplete fixture identity")
        print("QA_ORDER_CATALOG_EMPTY=PASS",flush=True)
        return
    require(len(co)==1 and co[0]!=owner,"Fixture company collision")
    eb=co[0]
    require(n(f"SELECT (SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario IN ('{prefix}-RA','{prefix}-RB'))+(SELECT COUNT(*) FROM Roles WHERE Nombre IN ('{prefix}-RA','{prefix}-RB'))+(SELECT COUNT(*) FROM Clientes WHERE Nombre='{prefix}-INV' OR Correo='{prefix}@example.invalid')+(SELECT COUNT(*) FROM UsuarioEmpresas WHERE EmpresaId={eb});","remaining authority or customer")==0,"Order or IAM cleanup incomplete")
    owned={"Empresas":[eb]}
    owned["Productos"]=ids(f"SELECT Id FROM Productos WHERE (EmpresaId={owner} AND BINARY Nombre=BINARY '{a}') OR (EmpresaId={eb} AND BINARY Nombre=BINARY '{b}');","fixture products")
    owned["ProductoVariantes"]=ids(f"""SELECT v.Id FROM ProductoVariantes v JOIN Productos p ON p.Id=v.ProductoId AND p.EmpresaId=v.EmpresaId
WHERE (p.EmpresaId={owner} AND BINARY p.Nombre=BINARY '{a}' AND BINARY v.Sku=BINARY '{a}') OR (p.EmpresaId={eb} AND BINARY p.Nombre=BINARY '{b}' AND BINARY v.Sku=BINARY '{b}');""","fixture variants")
    owned["Sucursales"]=ids(f"SELECT Id FROM Sucursales WHERE EmpresaId={owner} AND BINARY Codigo=BINARY '{s}' AND BINARY Nombre=BINARY '{s}';","fixture branch")
    owned["Almacenes"]=ids(f"SELECT a.Id FROM Almacenes a JOIN Sucursales s ON s.Id=a.SucursalId WHERE s.EmpresaId={owner} AND BINARY s.Codigo=BINARY '{s}' AND BINARY a.Codigo=BINARY '{w}' AND BINARY a.Nombre=BINARY '{w}';","fixture warehouse")
    owned["CostosEnvio"]=ids(f"SELECT Id FROM CostosEnvio WHERE (EmpresaId={owner} AND Nombre IN ('{prefix}-ZERO','{prefix}-PAID')) OR (EmpresaId={eb} AND Nombre='{prefix}-FOREIGN');","fixture tariffs")
    owned["ExistenciasVariante"]=ids(f"""SELECT e.Id FROM ExistenciasVariante e JOIN ProductoVariantes v ON v.Id=e.ProductoVarianteId AND v.EmpresaId=e.EmpresaId JOIN Productos p ON p.Id=v.ProductoId AND p.EmpresaId=v.EmpresaId JOIN Almacenes a ON a.Id=e.AlmacenId JOIN Sucursales s ON s.Id=a.SucursalId
WHERE e.EmpresaId={owner} AND p.Nombre='{a}' AND v.Sku='{a}' AND s.EmpresaId={owner} AND s.Codigo='{s}' AND a.Codigo='{w}';""","fixture stock")
    expected={"Productos":2,"ProductoVariantes":2,"Sucursales":1,"Almacenes":1,"CostosEnvio":3,"ExistenciasVariante":1}
    require(all(len(owned[t])==count for t,count in expected.items()),"Incomplete or ambiguous fixture graph; no catalog delete")
    if recover:
        require(n(f"SELECT COUNT(*) FROM PedidosVenta WHERE Id=3;","previous order cleanup")==0,"Previous draft still exists")
        require(n(f"SELECT COUNT(*) FROM Productos WHERE Id IN ({idlist(owned['Productos'])}) AND Cantidad=3 AND Costo=30 AND Precio=100 AND FechaCreacion >= '2026-11-28 19:31:00' AND FechaCreacion < '2026-11-28 19:31:15';","previous product shape")==2,"Previous product fingerprint mismatch")
        require(n(f"SELECT COUNT(*) FROM ProductoVariantes WHERE Id IN ({idlist(owned['ProductoVariantes'])}) AND Cantidad=3 AND Costo=30 AND Precio=100 AND EsTecnica=1;","previous variant shape")==2,"Previous variant fingerprint mismatch")
        require(n(f"SELECT COUNT(*) FROM ExistenciasVariante WHERE Id IN ({idlist(owned['ExistenciasVariante'])}) AND StockFisico=3 AND StockReservado=0 AND StockTransito=0;","previous stock shape")==1,"Previous stock fingerprint mismatch")
        require(n(f"SELECT COUNT(*) FROM CostosEnvio WHERE Id IN ({idlist(owned['CostosEnvio'])}) AND ((Nombre='{prefix}-ZERO' AND Monto=0) OR (Nombre='{prefix}-PAID' AND Monto=37.25) OR (Nombre='{prefix}-FOREIGN' AND Monto=999));","previous tariff shape")==3,"Previous tariff fingerprint mismatch")
    # A synthetic company may have received canonical default rows by normal QA
    # provisioning. Remove only the exact system-seeded shapes, never arbitrary
    # B-owned rows or settings. If anything else references B, retain it inactive.
    seed_window=" AND FechaCreacion >= '2026-11-28 19:31:00' AND FechaCreacion < '2026-11-28 19:34:00'" if recover else f" AND FechaCreacion >= (SELECT FechaCreacion FROM Empresas WHERE Id={eb})"
    owned["TipoClientes"]=ids(f"SELECT Id FROM TipoClientes WHERE EmpresaId={eb} AND Codigo='SIN_CLASIFICAR' AND EsSistema=1 AND Nombre='Sin clasificar' AND NombreNormalizado='SIN CLASIFICAR' AND Eliminado=0"+seed_window+";","canonical synthetic customer type")
    owned["MetodosPago"]=ids(f"""SELECT Id FROM MetodosPago WHERE EmpresaId={eb} AND CreadoPorNombreUsuario='SOLQARYN-F7' AND ActualizadoPorNombreUsuario='SOLQARYN-F7' AND Eliminado=0 AND Nombre=Codigo AND Tipo=Codigo AND JSON_UNQUOTE(JSON_EXTRACT(Metadata,'$.phase7CanonicalSeed'))='true' AND ((Codigo='Efectivo' AND Orden=10) OR (Codigo='Transferencia' AND Orden=20) OR (Codigo='Tarjeta' AND Orden=30) OR (Codigo='Otro' AND Orden=40))"""+seed_window+";","canonical synthetic payment methods")
    owned["ConfigEmpresas"]=ids(f"""SELECT Id FROM ConfigEmpresas WHERE EmpresaId={eb} AND Version=1 AND Moneda='HNL' AND ZonaHoraria='America/Tegucigalpa' AND ImpuestosJson='{{}}' AND EmisionJson='{{}}' AND CorreoConfigurado=0 AND CorreoRemitente IS NULL AND CorreoNombreRemitente IS NULL AND CorreoSecretoReferencia IS NULL"""+seed_window+";","empty synthetic company config")
    require(len(owned["TipoClientes"])<=1 and len(owned["MetodosPago"])<=4 and len(owned["ConfigEmpresas"])<=1,"Unexpected seeded rows")
    order=["ExistenciasVariante","ProductoVariantes","Productos","CostosEnvio","Almacenes","Sucursales","TipoClientes","MetodosPago","ConfigEmpresas"]
    targets=",".join("'"+x+"'" for x in ["Empresas"]+order)
    rows=sql(f"""SELECT REFERENCED_TABLE_NAME,TABLE_NAME,CONSTRAINT_NAME,COLUMN_NAME,REFERENCED_COLUMN_NAME
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA=DATABASE() AND REFERENCED_TABLE_SCHEMA=DATABASE() AND REFERENCED_TABLE_NAME IN ({targets}) ORDER BY REFERENCED_TABLE_NAME,TABLE_NAME,CONSTRAINT_NAME,ORDINAL_POSITION;""","actual FK catalog").splitlines()
    edges={}
    for row in rows:
        parent,child,fk,col,refcol=row.split("\t")
        for val in (parent,child,fk,col,refcol): quoted(val)
        edges.setdefault((parent,child,fk),[]).append((col,refcol))
    require(bool(edges) and any(p=="Empresas" for p,_,_ in edges),"Missing FK catalog")
    def join(cols):
        return " AND ".join("c."+quoted(col)+"=p."+quoted(refcol) for col,refcol in cols)
    def observed(external):
        queries=[]
        for (parent,child,fk),cols in edges.items():
            if not owned.get(parent): continue
            if external and parent=="Empresas": continue
            outside=""
            if external and owned.get(child): outside=" AND c.Id NOT IN ("+idlist(owned[child])+")"
            queries.append(f"SELECT '{parent}|{child}|{fk}',COUNT(*) FROM {quoted(child)} c JOIN {quoted(parent)} p ON {join(cols)} WHERE p.Id IN ({idlist(owned[parent])})"+outside)
        found=[]
        for i in range(0,len(queries),24):
            for row in sql(" UNION ALL ".join(queries[i:i+24])+";","FK reference counts").splitlines():
                tag,count=row.split("\t")
                require(count.isdigit(),"Invalid FK count")
                if int(count): found.append((tag,int(count)))
        return found
    for tag,count in observed(False): print(f"QA_ORDER_CLEANUP_FK_PRESENT={tag};rows={count}",flush=True)
    unexpected=observed(True)
    for tag,count in unexpected: print(f"QA_ORDER_CLEANUP_FK_EXTERNAL={tag};rows={count}",flush=True)
    require(not unexpected,"Unexpected external reference to fixture; preserve referenced data")
    def guards(parent):
        parts=[]
        for (p,child,fk),cols in edges.items():
            if p!=parent: continue
            require(child!=parent,"Self reference requires explicit reviewed cleanup")
            parts.append(f"NOT EXISTS (SELECT 1 FROM {quoted(child)} c WHERE {join(cols)})")
        return (" AND "+" AND ".join(parts)) if parts else ""
    # Lock only the identified rows; guard every delete against ALL inbound FKs,
    # including cascades, to avoid deleting records created by another actor.
    statements=["START TRANSACTION;"]
    for table in ["Empresas"]+order:
        if owned[table]: statements.append(f"SELECT Id FROM {quoted(table)} WHERE Id IN ({idlist(owned[table])}) FOR UPDATE;")
    statements.append(f"UPDATE Empresas SET Activa=0 WHERE Id={eb} AND BINARY Nombre=BINARY '{b}';")
    for table in order:
        if owned[table]:
            statements.append(f"DELETE p FROM {quoted(table)} p WHERE p.Id IN ({idlist(owned[table])})"+guards(table)+";")
    statements.append(f"DELETE p FROM Empresas p WHERE p.Id={eb} AND BINARY p.Nombre=BINARY '{b}'"+guards("Empresas")+";")
    statements.append("COMMIT;")
    sql("\n".join(statements),"dependency-ordered guarded fixture cleanup")
    for table in order:
        if owned[table]: require(n(f"SELECT COUNT(*) FROM {quoted(table)} WHERE Id IN ({idlist(owned[table])});","verify removal "+table)==0,"Referenced fixture remains: "+table)
    require(n(f"SELECT COUNT(*) FROM Empresas WHERE Id={eb} AND Activa=1;","company disabled")==0,"Synthetic company remains active")
    require(n(f"SELECT COUNT(*) FROM UsuarioEmpresas WHERE EmpresaId={eb};","company access revoked")==0,"Synthetic company retains access")
    remaining=observed(False)
    for tag,count in remaining: print(f"QA_ORDER_CLEANUP_FK_RETAINED={tag};rows={count}",flush=True)
    # The only intentional residue is an inactive company required by audit.
    require(all(tag.split("|")[0]=="Empresas" and tag.split("|")[1]=="RegistrosAuditoria" for tag,_ in remaining),"Unexpected references retained; synthetic company disabled, review required")
    left=n(f"SELECT COUNT(*) FROM Empresas WHERE Id={eb};","company retention")
    require(left in (0,1) and (left==0 or bool(remaining)),"Unexplained company retention")
    require(store()[0]==owner,"Storefront ownership changed during cleanup")
    print("QA_ORDER_CATALOG_CLEANUP=PASS inactive_audit_company="+str(left),flush=True)

def recover_previous_cleanup(owner):
    # Unique candidate: exact prior creation window, exact two login audit
    # descriptions and full correlated commercial graph; never delete by LIKE.
    rows=sql("""SELECT e.Id,e.Nombre FROM Empresas e WHERE e.Nombre REGEXP '^qafn5[0-9a-f]{12}-B# A new B company is active only if A's configured identity is explicit and stable.
# No modification of the public configuration or any existing business row.
prefix="qafn5"+secrets.token_hex(6)
print("QA_ORDER_FIXTURE_PREFIX="+prefix,flush=True)
pa=prefix+"-A"; pb=prefix+"-B"; branch=prefix+"-S"; warehouse=prefix+"-W"
t0=prefix+"-ZERO"; t1=prefix+"-PAID"; tb=prefix+"-FOREIGN"
d=prefix+"-D"; c0=prefix+"-C0"; c1=prefix+"-C1"; cx=prefix+"-CX"; z=prefix+"-Z"; mode=prefix+"-M"
require(n("SELECT COUNT(*) FROM Descuentos WHERE Activo=1 AND Eliminado=0;","public promotion baseline")==0,"Active promotion rules require an independent expected-price oracle")
for table,field,values in (("Empresas","Nombre",[pb]),("Sucursales","Codigo",[branch]),("Almacenes","Codigo",[warehouse]),("Productos","Nombre",[pa,pb]),("ProductoVariantes","Sku",[pa,pb]),("CostosEnvio","Nombre",[t0,t1,tb])):
    literal=",".join("'"+x+"'" for x in values)
    require(n(f"SELECT COUNT(*) FROM {table} WHERE {field} IN ({literal});","fixture collision "+table)==0,"Fixture collision")
def product(e,name):
    return f"""INSERT INTO Productos (EmpresaId,Nombre,Marca,Modelo,TipoInventario,Cantidad,Costo,Precio,UmbralStockBajo,Activo,EsDestacado,Eliminado,FechaCreacion,FechaActualizacion)
VALUES ({e},'{name}','','',1,3,30,100,1,1,0,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));"""
def variant(e,p,name):
    return f"""INSERT INTO ProductoVariantes (EmpresaId,ProductoId,Sku,Cantidad,UmbralStockBajo,Costo,Precio,Activo,Eliminado,EsTecnica,FechaCreacion,FechaActualizacion)
VALUES ({e},{p},'{name}',3,1,30,100,1,0,1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));"""
def tariff(e,name,city,money):
    return f"""INSERT INTO CostosEnvio (EmpresaId,Nombre,Departamento,Ciudad,Zona,Modalidad,Monto,Prioridad,EsPredeterminado,Activo,Eliminado,FechaCreacion,FechaActualizacion)
VALUES ({e},'{name}','{d}','{city}','{z}','{mode}',{money},0,0,1,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));"""

require(sql("SELECT VERSION();","version").startswith("8.4.11"),"MySQL version mismatch")
require(sql("SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId='20261128183000_QaCheckoutPedidoV6';","V6 marker")=="1","V6 not applied")
require(sql("SELECT COUNT(*) FROM __EFMigrationsHistory;","history")=="115","Unexpected migration history")
require(matched,"Storefront owner must remain explicit with active B")
currency=sql("SELECT Moneda FROM EmpresaConfiguraciones WHERE Activa=1;","configured currency")
require(re.fullmatch(r"[A-Z]{3}",currency) is not None,"Currency not unique or invalid")
guest=prefix+"-INV"
email=prefix+"@example.invalid"
namea=prefix+"-RA"; nameb=prefix+"-RB"
key=secrets.token_hex(32); badkey=secrets.token_hex(32); wrongkey=secrets.token_hex(32)
def digest(k): return "checkout-"+hashlib.sha256(("invitado:"+k).encode()).hexdigest()
scoped=digest(key); rejected=digest(badkey)
keys="'"+scoped+"','"+rejected+"'"
require(n(f"SELECT COUNT(*) FROM Clientes WHERE Nombre='{guest}' OR Correo='{email}';","guest collision")==0,"Guest collision")
require(n(f"SELECT COUNT(*) FROM PedidosVenta WHERE IdempotencyKey IN ({keys});","key collision")==0,"Order collision")
require(n(f"SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario IN ('{namea}','{nameb}');","user collision")==0,"User collision")
require(n(f"SELECT COUNT(*) FROM Roles WHERE Nombre IN ('{namea}','{nameb}');","role collision")==0,"Role collision")
grant=n("SELECT Id FROM Permisos WHERE Modulo=5 AND Accion=1 AND Ambito=1 AND Activo=1 AND Eliminado=0;","unique sales view grant")
require(grant>0,"Missing sales read grant")
def hashed(name,pw):
    r=subprocess.run(["htpasswd","-niBC","12",name],input=pw+"\n",text=True,capture_output=True,timeout=20)
    require(r.returncode==0,"BCrypt failed")
    h=r.stdout.strip().split(":",1)[1]
    require(re.fullmatch(r"\$2[aby]\$12\$[./A-Za-z0-9]{53}",h) is not None,"Invalid BCrypt")
    return h
passwords={name:"Qa!7"+secrets.token_urlsafe(32) for name in (namea,nameb)}
hashes={name:hashed(name,passwords[name]) for name in passwords}
created=False
iam=False
order_attempted=False
try:
    # All newly inserted rows are uniquely labeled, and all mutations are
    # in the QA database. No existing product, warehouse, tariff or IAM touched.
    created=True
    values=sql(f"""START TRANSACTION;
INSERT INTO Empresas (Nombre,Activa,FechaCreacion,FechaActualizacion) VALUES ('{pb}',{1 if matched else 0},UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @eb=LAST_INSERT_ID();
INSERT INTO Sucursales (EmpresaId,Codigo,Nombre,Activa,Eliminado,ZonaHoraria,FechaCreacion,FechaActualizacion)
VALUES ({owner},'{branch}','{branch}',1,0,'America/Tegucigalpa',UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @s=LAST_INSERT_ID();
INSERT INTO Almacenes (SucursalId,Codigo,Nombre,Tipo,Activo,Eliminado,FechaCreacion,FechaActualizacion)
VALUES (@s,'{warehouse}','{warehouse}',1,1,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @w=LAST_INSERT_ID();
{product(owner,pa)}
SET @pa=LAST_INSERT_ID();
{variant(owner,"@pa",pa)}
SET @va=LAST_INSERT_ID();
INSERT INTO ExistenciasVariante (EmpresaId,ProductoVarianteId,AlmacenId,StockFisico,StockReservado,StockTransito,StockMinimo,FechaCreacion,FechaActualizacion)
VALUES ({owner},@va,@w,3,0,0,1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
{product("@eb",pb)}
SET @pb=LAST_INSERT_ID();
{variant("@eb","@pb",pb)}
SET @vb=LAST_INSERT_ID();
{tariff(owner,t0,c0,0)}
{tariff(owner,t1,c1,37.25)}
{tariff("@eb",tb,c0,999)}
COMMIT;
SELECT @eb,@pa,@va,@pb,@vb;""","create checkout fixtures").split("\t")
    require(len(values)==5 and all(v.isdigit() and int(v)>0 for v in values),"Invalid fixture IDs")
    eb,pid,vid,foreign_pid,foreign_vid=map(int,values)
    require(eb!=owner and pid!=foreign_pid and vid!=foreign_vid,"Fixture scope collision")
    require(store()[0]==owner,"Public storefront authority changed")

    checkout={"items":[{"productoId":pid,"productoVarianteId":vid,"unidades":2}],"envio":{"departamento":d,"ciudad":c1,"zona":z,"modalidad":mode}}
    q=data(call("/tienda/checkout/validar","QA_ORDER_QUOTE",body=checkout,method="POST"))
    money=lambda x: Decimal(str(x))
    require(money(q["subtotal"])==200 and money(q["costoEnvio"])==Decimal("37.25") and money(q["total"])==Decimal("237.25") and q["tarifaEnvio"]==t1,"Quote totals mismatch")
    require(len(q["lineas"])==1 and q["lineas"][0]["productoId"]==pid and q["lineas"][0]["productoVarianteId"]==vid and money(q["lineas"][0]["precioUnitario"])==100 and q["lineas"][0]["unidades"]==2,"Quote line mismatch")
    print("QA_ORDER_SERVER_PRICES=PASS",flush=True)
    foreign={"items":[{"productoId":foreign_pid,"productoVarianteId":foreign_vid,"unidades":1}],"envio":checkout["envio"]}
    call("/tienda/checkout/validar","QA_ORDER_FOREIGN_PRODUCT_DENIED",409,body=foreign,method="POST")
    order={"nombre":guest,"telefono":"00000000","correo":email,"notas":prefix+" synthetic QA; no fulfillment","checkout":checkout,"subtotalEsperado":200,"envioEsperado":37.25,"monedaEsperada":currency,"lineasEsperadas":[{"productoId":pid,"productoVarianteId":vid,"unidades":2,"precioUnitario":100}]}
    path="/tienda/cuenta/pedidos/checkout"
    order_attempted=True
    altered=dict(order,envioEsperado=1)
    call(path,"QA_ORDER_TAMPER_REJECTED",409,body=altered,method="POST",order_key=badkey)
    require(n(f"SELECT COUNT(*) FROM PedidosVenta WHERE IdempotencyKey='{rejected}';","rejected order")==0 and n(f"SELECT COUNT(*) FROM Clientes WHERE Nombre='{guest}' OR Correo='{email}';","rejected guest")==0,"Rejected request mutated data")
    receipt=data(call(path,"QA_ORDER_CREATE",201,body=order,method="POST",order_key=key))
    oid=receipt.get("id")
    require(isinstance(oid,int) and oid>0,"Missing order identifier")
    def verify_receipt(v):
        require(v["id"]==oid and v["estado"]=="Borrador" and money(v["subtotal"])==200 and money(v["costoEnvio"])==Decimal("37.25") and money(v["total"])==Decimal("237.25") and v["moneda"]==currency and v["tarifaEnvio"]==t1,"Receipt values mismatch")
        line=v["lineas"]
        require(len(line)==1 and line[0]["productoId"]==pid and line[0]["productoVarianteId"]==vid and money(line[0]["cantidad"])==2 and money(line[0]["precioUnitario"])==100 and money(line[0]["total"])==200,"Receipt line mismatch")
    verify_receipt(receipt)
    verify_receipt(data(call(path,"QA_ORDER_REPLAY",body=order,method="POST",order_key=key)))
    call(path,"QA_ORDER_KEY_CONFLICT",409,body=altered,method="POST",order_key=key)
    # AuthLogin has a minimum limit of 5/min; never retry a mutation.
    time.sleep(65)
    verify_receipt(data(call(path+"/"+str(oid),"QA_ORDER_RECEIPT",order_key=key)))
    call(path+"/"+str(oid),"QA_ORDER_NO_KEY_DENIED",401)
    call(path+"/"+str(oid),"QA_ORDER_WRONG_KEY_DENIED",404,order_key=wrongkey)
    row=sql(f"""SELECT p.Id,p.ClienteId,p.Estado,p.CostoEnvio,p.MonedaCheckout,p.TarifaEnvioCheckout
FROM PedidosVenta p JOIN Clientes c ON c.Id=p.ClienteId
WHERE p.EmpresaId={owner} AND p.IdempotencyKey='{scoped}' AND p.CotizacionId IS NULL
AND CHAR_LENGTH(p.IdempotencyFingerprint)=64 AND JSON_VALID(p.EntregaCheckoutJson)=1
AND p.CreadoPorNombreUsuario='storefront-invitado' AND p.ClienteNombreSnapshot='{guest}'
AND c.EmpresaId={owner} AND c.Nombre='{guest}' AND c.Correo='{email}' AND c.Activo=1;""","durable order").split("\t")
    require(len(row)==6 and row[0]==str(oid) and row[1].isdigit() and row[2]=="1" and money(row[3])==Decimal("37.25") and row[4]==currency and row[5]==t1,"Persistent order mismatch")
    cid=int(row[1])
    require(n(f"SELECT COUNT(*) FROM PedidosVenta WHERE IdempotencyKey IN ({keys});","order uniqueness")==1,"Duplicate order")
    require(n(f"SELECT COUNT(*) FROM Clientes WHERE Nombre='{guest}' OR Correo='{email}';","client uniqueness")==1,"Duplicate guest")
    require(n(f"SELECT COUNT(*) FROM PedidoVentaDetalles WHERE PedidoVentaId={oid};","line cardinality")==1,"Duplicate detail")
    require(n(f"SELECT COUNT(*) FROM PedidoVentaDetalles WHERE PedidoVentaId={oid} AND ProductoId={pid} AND ProductoVarianteId={vid} AND Cantidad=2 AND PrecioUnitario=100 AND ProductoSkuSnapshot='{pa}';","durable detail")==1,"Persistent detail mismatch")
    require(n(f"SELECT COUNT(*) FROM ExistenciasVariante WHERE EmpresaId={owner} AND ProductoVarianteId={vid} AND StockFisico=3 AND StockReservado=0 AND StockTransito=0;","stock preservation")==1,"Draft modified stock")
    # No linked reservations, preparations, invoices, cash or other downstream rows.
    for parent,ident,allowed in (("PedidosVenta",oid,{"PedidoVentaDetalles"}),("Clientes",cid,{"PedidosVenta"})):
        refs=sql(f"""SELECT DISTINCT TABLE_NAME,COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA=DATABASE() AND REFERENCED_TABLE_SCHEMA=DATABASE() AND REFERENCED_TABLE_NAME='{parent}' AND REFERENCED_COLUMN_NAME='Id';""","downstream reference catalog").splitlines()
        require(bool(refs),"Missing FK metadata")
        for refrow in refs:
            table,column=refrow.split("\t")
            require(re.fullmatch(r"[A-Za-z0-9_]+",table) and re.fullmatch(r"[A-Za-z0-9_]+",column),"Invalid catalog identifier")
            if table not in allowed:
                require(n(f"SELECT COUNT(*) FROM {table} WHERE {column}={ident};","downstream "+table)==0,"Unexpected linked effect: "+table)
    print(f"QA_ORDER_PERSISTENCE=PASS order_id={oid} detail_count=1 stock_unchanged=YES",flush=True)
    # Read-only ERP identities are temporary, scoped to the exact A/B companies.
    iam=True
    statements=["START TRANSACTION;"]
    for name,company in ((namea,owner),(nameb,eb)):
        statements.extend([f"INSERT INTO Roles (Nombre,NombreNormalizado,Descripcion,EsSistema,EsAdministrador,Ambito,EmpresaId,Activo,Eliminado,FechaCreacion) VALUES ('{name}','{name.upper()}','QA checkout read only',0,0,1,{company},1,0,UTC_TIMESTAMP(6));","SET @r=LAST_INSERT_ID();",f"INSERT INTO RolPermisos (RolId,PermisoId) VALUES (@r,{grant});",f"INSERT INTO Usuarios (NombreUsuario,NombreCompleto,PasswordHash,RolId,Activo,Bloqueado,Eliminado,FechaCreacion) VALUES ('{name}','{name}','{hashes[name]}',@r,1,0,0,UTC_TIMESTAMP(6));","SET @u=LAST_INSERT_ID();",f"INSERT INTO UsuarioEmpresas (UsuarioId,EmpresaId,RolId,Activa,FechaCreacion,FechaActualizacion) VALUES (@u,{company},@r,1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));"])
    statements.append("COMMIT;")
    sql("\n".join(statements),"create exact read-only ERP users")
    time.sleep(65)
    def login(name,label):
        v=data(call("/auth/login",label,body={"nombreUsuario":name,"password":passwords[name]}))
        token=v.get("token")
        require(isinstance(token,str) and len(token)>20,"No ERP JWT")
        return token
    ta=login(namea,"QA_ORDER_ERP_LOGIN_A"); tb=login(nameb,"QA_ORDER_ERP_LOGIN_B")
    erp="/pedidos-venta/"+str(oid)
    got=data(call(erp,"QA_ORDER_ERP_READ_A",token=ta,tenant=owner))
    require(got["id"]==oid and got["clienteId"]==cid and got["estado"] in (1,"Borrador") and got["cotizacionId"] is None and money(got["subtotal"])==200 and money(got["costoEnvio"])==Decimal("37.25") and money(got["total"])==Decimal("237.25") and got["monedaCheckout"]==currency and got["tarifaEnvioCheckout"]==t1 and len(got["detalles"])==1,"ERP order snapshot mismatch")
    call(erp,"QA_ORDER_ERP_B_DENIED",404,token=tb,tenant=eb)
    call(erp,"QA_ORDER_ERP_TENANT_SPOOF_DENIED",403,token=tb,tenant=owner)
    call(erp,"QA_ORDER_ERP_ANONYMOUS_DENIED",401)
    require(store()[0]==owner,"Storefront authority changed")
    print("QA_ORDER_DEPLOYED_FUNCTIONAL=PASS",flush=True)
finally:
    if iam:
        names=f"('{namea}','{nameb}')"
        sql(f"""START TRANSACTION;
UPDATE Usuarios SET Activo=0,Bloqueado=1 WHERE NombreUsuario IN {names};
UPDATE UsuarioEmpresas ue JOIN Usuarios u ON ue.UsuarioId=u.Id SET ue.Activa=0 WHERE u.NombreUsuario IN {names};
DELETE rp FROM RolPermisos rp JOIN Roles r ON r.Id=rp.RolId WHERE r.Nombre IN {names};
UPDATE Roles SET Activo=0 WHERE Nombre IN {names};
COMMIT;""","revoke exact ERP fixture access")
        require(n(f"SELECT (SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario IN {names} AND Activo=1)+(SELECT COUNT(*) FROM Roles WHERE Nombre IN {names} AND Activo=1)+(SELECT COUNT(*) FROM RolPermisos rp JOIN Roles r ON r.Id=rp.RolId WHERE r.Nombre IN {names});","revocation")==0,"Fixture authority remains")
        sql(f"""START TRANSACTION;
DELETE ue FROM UsuarioEmpresas ue JOIN Usuarios u ON u.Id=ue.UsuarioId WHERE u.NombreUsuario IN {names};
DELETE FROM Usuarios WHERE NombreUsuario IN {names};
DELETE FROM Roles WHERE Nombre IN {names};
COMMIT;""","remove exact ERP fixture identities")
        require(n(f"SELECT (SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario IN {names})+(SELECT COUNT(*) FROM Roles WHERE Nombre IN {names});","IAM cleanup")==0,"IAM fixture remains")
        print("QA_ORDER_ERP_IAM_CLEANUP=PASS",flush=True)
    if order_attempted:
        # If a non-draft or unexpected order exists, fail closed; never force-delete it.
        require(n(f"""SELECT COUNT(*) FROM PedidosVenta p LEFT JOIN Clientes c ON c.Id=p.ClienteId
WHERE p.IdempotencyKey IN ({keys}) AND NOT (p.EmpresaId={owner} AND p.Estado=1 AND p.CotizacionId IS NULL AND p.CreadoPorNombreUsuario='storefront-invitado' AND p.ClienteNombreSnapshot='{guest}' AND c.EmpresaId={owner} AND c.Nombre='{guest}' AND c.Correo='{email}');""","cleanup ownership")==0,"Unsafe order cleanup; retain for review")
        sql(f"""START TRANSACTION;
DELETE p FROM PedidosVenta p JOIN Clientes c ON c.Id=p.ClienteId WHERE p.EmpresaId={owner} AND p.Estado=1 AND p.CotizacionId IS NULL AND p.IdempotencyKey IN ({keys}) AND p.CreadoPorNombreUsuario='storefront-invitado' AND p.ClienteNombreSnapshot='{guest}' AND c.EmpresaId={owner} AND c.Nombre='{guest}' AND c.Correo='{email}';
DELETE c FROM Clientes c LEFT JOIN PedidosVenta p ON p.ClienteId=c.Id WHERE c.EmpresaId={owner} AND c.Nombre='{guest}' AND c.Correo='{email}' AND c.CreadoPorNombreUsuario='storefront-invitado' AND p.Id IS NULL;
COMMIT;""","remove only exact draft and synthetic guest")
        require(n(f"SELECT COUNT(*) FROM PedidosVenta WHERE IdempotencyKey IN ({keys});","order cleanup")==0 and n(f"SELECT COUNT(*) FROM Clientes WHERE Nombre='{guest}' OR Correo='{email}';","guest cleanup")==0,"Order fixture remains")
        print("QA_ORDER_DRAFT_CLEANUP=PASS",flush=True)
    if created:
        cleanup_catalog(owner,prefix)
print("QA_ORDER_REAL_DEPLOYED_CERTIFICATE=PASS",flush=True)

AND e.FechaCreacion >= '2026-11-28 19:31:00' AND e.FechaCreacion < '2026-11-28 19:31:15'
AND EXISTS (SELECT 1 FROM RegistrosAuditoria r WHERE r.Fecha >= '2026-11-28 19:33:35' AND r.Fecha < '2026-11-28 19:34:00' AND BINARY r.Descripcion=BINARY CONCAT('Inicio de sesion exitoso para ',CHAR(39),LEFT(e.Nombre,17),'-RA',CHAR(39),'.'))
AND EXISTS (SELECT 1 FROM RegistrosAuditoria r WHERE r.Fecha >= '2026-11-28 19:33:35' AND r.Fecha < '2026-11-28 19:34:00' AND BINARY r.Descripcion=BINARY CONCAT('Inicio de sesion exitoso para ',CHAR(39),LEFT(e.Nombre,17),'-RB',CHAR(39),'.'));""","identify failed run 46181529540").splitlines()
    require(len(rows)==1,"Prior run fixture identity not uniquely established; zero catalog deletes")
    ident,name=rows[0].split("\t")
    require(ident.isdigit() and int(ident)!=owner and re.fullmatch(r"qafn5[0-9a-f]{12}-B",name) is not None,"Invalid prior fixture")
    prefix=name[:-2]
    print("QA_ORDER_RECOVERY_IDENTITY=PASS run=46181529540 prefix="+prefix+" company="+ident,flush=True)
    cleanup_catalog(owner,prefix,recover=True)
    print("QA_ORDER_FAILED_RUN_CLEANUP_RECOVERY=PASS",flush=True)

if sys.argv[1:]==["--recover-run-46181529540"]:
    require(matched,"Explicit storefront owner required for recovery")
    require(sql("SELECT VERSION();","version").startswith("8.4.11"),"MySQL version mismatch")
    require(sql("SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId='20261128183000_QaCheckoutPedidoV6';","V6 marker")=="1","V6 not applied")
    require(sql("SELECT COUNT(*) FROM __EFMigrationsHistory;","history")=="115","Unexpected migration history")
    recover_previous_cleanup(owner)
    sys.exit(0)
require(not sys.argv[1:],"Unexpected mode")
# A new B company is active only if A's configured identity is explicit and stable.
# No modification of the public configuration or any existing business row.
prefix="qafn5"+secrets.token_hex(6)
pa=prefix+"-A"; pb=prefix+"-B"; branch=prefix+"-S"; warehouse=prefix+"-W"
t0=prefix+"-ZERO"; t1=prefix+"-PAID"; tb=prefix+"-FOREIGN"
d=prefix+"-D"; c0=prefix+"-C0"; c1=prefix+"-C1"; cx=prefix+"-CX"; z=prefix+"-Z"; mode=prefix+"-M"
require(n("SELECT COUNT(*) FROM Descuentos WHERE Activo=1 AND Eliminado=0;","public promotion baseline")==0,"Active promotion rules require an independent expected-price oracle")
for table,field,values in (("Empresas","Nombre",[pb]),("Sucursales","Codigo",[branch]),("Almacenes","Codigo",[warehouse]),("Productos","Nombre",[pa,pb]),("ProductoVariantes","Sku",[pa,pb]),("CostosEnvio","Nombre",[t0,t1,tb])):
    literal=",".join("'"+x+"'" for x in values)
    require(n(f"SELECT COUNT(*) FROM {table} WHERE {field} IN ({literal});","fixture collision "+table)==0,"Fixture collision")
def product(e,name):
    return f"""INSERT INTO Productos (EmpresaId,Nombre,Marca,Modelo,TipoInventario,Cantidad,Costo,Precio,UmbralStockBajo,Activo,EsDestacado,Eliminado,FechaCreacion,FechaActualizacion)
VALUES ({e},'{name}','','',1,3,30,100,1,1,0,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));"""
def variant(e,p,name):
    return f"""INSERT INTO ProductoVariantes (EmpresaId,ProductoId,Sku,Cantidad,UmbralStockBajo,Costo,Precio,Activo,Eliminado,EsTecnica,FechaCreacion,FechaActualizacion)
VALUES ({e},{p},'{name}',3,1,30,100,1,0,1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));"""
def tariff(e,name,city,money):
    return f"""INSERT INTO CostosEnvio (EmpresaId,Nombre,Departamento,Ciudad,Zona,Modalidad,Monto,Prioridad,EsPredeterminado,Activo,Eliminado,FechaCreacion,FechaActualizacion)
VALUES ({e},'{name}','{d}','{city}','{z}','{mode}',{money},0,0,1,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));"""

require(sql("SELECT VERSION();","version").startswith("8.4.11"),"MySQL version mismatch")
require(sql("SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId='20261128183000_QaCheckoutPedidoV6';","V6 marker")=="1","V6 not applied")
require(sql("SELECT COUNT(*) FROM __EFMigrationsHistory;","history")=="115","Unexpected migration history")
require(matched,"Storefront owner must remain explicit with active B")
currency=sql("SELECT Moneda FROM EmpresaConfiguraciones WHERE Activa=1;","configured currency")
require(re.fullmatch(r"[A-Z]{3}",currency) is not None,"Currency not unique or invalid")
guest=prefix+"-INV"
email=prefix+"@example.invalid"
namea=prefix+"-RA"; nameb=prefix+"-RB"
key=secrets.token_hex(32); badkey=secrets.token_hex(32); wrongkey=secrets.token_hex(32)
def digest(k): return "checkout-"+hashlib.sha256(("invitado:"+k).encode()).hexdigest()
scoped=digest(key); rejected=digest(badkey)
keys="'"+scoped+"','"+rejected+"'"
require(n(f"SELECT COUNT(*) FROM Clientes WHERE Nombre='{guest}' OR Correo='{email}';","guest collision")==0,"Guest collision")
require(n(f"SELECT COUNT(*) FROM PedidosVenta WHERE IdempotencyKey IN ({keys});","key collision")==0,"Order collision")
require(n(f"SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario IN ('{namea}','{nameb}');","user collision")==0,"User collision")
require(n(f"SELECT COUNT(*) FROM Roles WHERE Nombre IN ('{namea}','{nameb}');","role collision")==0,"Role collision")
grant=n("SELECT Id FROM Permisos WHERE Modulo=5 AND Accion=1 AND Ambito=1 AND Activo=1 AND Eliminado=0;","unique sales view grant")
require(grant>0,"Missing sales read grant")
def hashed(name,pw):
    r=subprocess.run(["htpasswd","-niBC","12",name],input=pw+"\n",text=True,capture_output=True,timeout=20)
    require(r.returncode==0,"BCrypt failed")
    h=r.stdout.strip().split(":",1)[1]
    require(re.fullmatch(r"\$2[aby]\$12\$[./A-Za-z0-9]{53}",h) is not None,"Invalid BCrypt")
    return h
passwords={name:"Qa!7"+secrets.token_urlsafe(32) for name in (namea,nameb)}
hashes={name:hashed(name,passwords[name]) for name in passwords}
created=False
iam=False
order_attempted=False
try:
    # All newly inserted rows are uniquely labeled, and all mutations are
    # in the QA database. No existing product, warehouse, tariff or IAM touched.
    created=True
    values=sql(f"""START TRANSACTION;
INSERT INTO Empresas (Nombre,Activa,FechaCreacion,FechaActualizacion) VALUES ('{pb}',{1 if matched else 0},UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @eb=LAST_INSERT_ID();
INSERT INTO Sucursales (EmpresaId,Codigo,Nombre,Activa,Eliminado,ZonaHoraria,FechaCreacion,FechaActualizacion)
VALUES ({owner},'{branch}','{branch}',1,0,'America/Tegucigalpa',UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @s=LAST_INSERT_ID();
INSERT INTO Almacenes (SucursalId,Codigo,Nombre,Tipo,Activo,Eliminado,FechaCreacion,FechaActualizacion)
VALUES (@s,'{warehouse}','{warehouse}',1,1,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @w=LAST_INSERT_ID();
{product(owner,pa)}
SET @pa=LAST_INSERT_ID();
{variant(owner,"@pa",pa)}
SET @va=LAST_INSERT_ID();
INSERT INTO ExistenciasVariante (EmpresaId,ProductoVarianteId,AlmacenId,StockFisico,StockReservado,StockTransito,StockMinimo,FechaCreacion,FechaActualizacion)
VALUES ({owner},@va,@w,3,0,0,1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
{product("@eb",pb)}
SET @pb=LAST_INSERT_ID();
{variant("@eb","@pb",pb)}
SET @vb=LAST_INSERT_ID();
{tariff(owner,t0,c0,0)}
{tariff(owner,t1,c1,37.25)}
{tariff("@eb",tb,c0,999)}
COMMIT;
SELECT @eb,@pa,@va,@pb,@vb;""","create checkout fixtures").split("\t")
    require(len(values)==5 and all(v.isdigit() and int(v)>0 for v in values),"Invalid fixture IDs")
    eb,pid,vid,foreign_pid,foreign_vid=map(int,values)
    require(eb!=owner and pid!=foreign_pid and vid!=foreign_vid,"Fixture scope collision")
    require(store()[0]==owner,"Public storefront authority changed")

    checkout={"items":[{"productoId":pid,"productoVarianteId":vid,"unidades":2}],"envio":{"departamento":d,"ciudad":c1,"zona":z,"modalidad":mode}}
    q=data(call("/tienda/checkout/validar","QA_ORDER_QUOTE",body=checkout,method="POST"))
    money=lambda x: Decimal(str(x))
    require(money(q["subtotal"])==200 and money(q["costoEnvio"])==Decimal("37.25") and money(q["total"])==Decimal("237.25") and q["tarifaEnvio"]==t1,"Quote totals mismatch")
    require(len(q["lineas"])==1 and q["lineas"][0]["productoId"]==pid and q["lineas"][0]["productoVarianteId"]==vid and money(q["lineas"][0]["precioUnitario"])==100 and q["lineas"][0]["unidades"]==2,"Quote line mismatch")
    print("QA_ORDER_SERVER_PRICES=PASS",flush=True)
    foreign={"items":[{"productoId":foreign_pid,"productoVarianteId":foreign_vid,"unidades":1}],"envio":checkout["envio"]}
    call("/tienda/checkout/validar","QA_ORDER_FOREIGN_PRODUCT_DENIED",409,body=foreign,method="POST")
    order={"nombre":guest,"telefono":"00000000","correo":email,"notas":prefix+" synthetic QA; no fulfillment","checkout":checkout,"subtotalEsperado":200,"envioEsperado":37.25,"monedaEsperada":currency,"lineasEsperadas":[{"productoId":pid,"productoVarianteId":vid,"unidades":2,"precioUnitario":100}]}
    path="/tienda/cuenta/pedidos/checkout"
    order_attempted=True
    altered=dict(order,envioEsperado=1)
    call(path,"QA_ORDER_TAMPER_REJECTED",409,body=altered,method="POST",order_key=badkey)
    require(n(f"SELECT COUNT(*) FROM PedidosVenta WHERE IdempotencyKey='{rejected}';","rejected order")==0 and n(f"SELECT COUNT(*) FROM Clientes WHERE Nombre='{guest}' OR Correo='{email}';","rejected guest")==0,"Rejected request mutated data")
    receipt=data(call(path,"QA_ORDER_CREATE",201,body=order,method="POST",order_key=key))
    oid=receipt.get("id")
    require(isinstance(oid,int) and oid>0,"Missing order identifier")
    def verify_receipt(v):
        require(v["id"]==oid and v["estado"]=="Borrador" and money(v["subtotal"])==200 and money(v["costoEnvio"])==Decimal("37.25") and money(v["total"])==Decimal("237.25") and v["moneda"]==currency and v["tarifaEnvio"]==t1,"Receipt values mismatch")
        line=v["lineas"]
        require(len(line)==1 and line[0]["productoId"]==pid and line[0]["productoVarianteId"]==vid and money(line[0]["cantidad"])==2 and money(line[0]["precioUnitario"])==100 and money(line[0]["total"])==200,"Receipt line mismatch")
    verify_receipt(receipt)
    verify_receipt(data(call(path,"QA_ORDER_REPLAY",body=order,method="POST",order_key=key)))
    call(path,"QA_ORDER_KEY_CONFLICT",409,body=altered,method="POST",order_key=key)
    # AuthLogin has a minimum limit of 5/min; never retry a mutation.
    time.sleep(65)
    verify_receipt(data(call(path+"/"+str(oid),"QA_ORDER_RECEIPT",order_key=key)))
    call(path+"/"+str(oid),"QA_ORDER_NO_KEY_DENIED",401)
    call(path+"/"+str(oid),"QA_ORDER_WRONG_KEY_DENIED",404,order_key=wrongkey)
    row=sql(f"""SELECT p.Id,p.ClienteId,p.Estado,p.CostoEnvio,p.MonedaCheckout,p.TarifaEnvioCheckout
FROM PedidosVenta p JOIN Clientes c ON c.Id=p.ClienteId
WHERE p.EmpresaId={owner} AND p.IdempotencyKey='{scoped}' AND p.CotizacionId IS NULL
AND CHAR_LENGTH(p.IdempotencyFingerprint)=64 AND JSON_VALID(p.EntregaCheckoutJson)=1
AND p.CreadoPorNombreUsuario='storefront-invitado' AND p.ClienteNombreSnapshot='{guest}'
AND c.EmpresaId={owner} AND c.Nombre='{guest}' AND c.Correo='{email}' AND c.Activo=1;""","durable order").split("\t")
    require(len(row)==6 and row[0]==str(oid) and row[1].isdigit() and row[2]=="1" and money(row[3])==Decimal("37.25") and row[4]==currency and row[5]==t1,"Persistent order mismatch")
    cid=int(row[1])
    require(n(f"SELECT COUNT(*) FROM PedidosVenta WHERE IdempotencyKey IN ({keys});","order uniqueness")==1,"Duplicate order")
    require(n(f"SELECT COUNT(*) FROM Clientes WHERE Nombre='{guest}' OR Correo='{email}';","client uniqueness")==1,"Duplicate guest")
    require(n(f"SELECT COUNT(*) FROM PedidoVentaDetalles WHERE PedidoVentaId={oid};","line cardinality")==1,"Duplicate detail")
    require(n(f"SELECT COUNT(*) FROM PedidoVentaDetalles WHERE PedidoVentaId={oid} AND ProductoId={pid} AND ProductoVarianteId={vid} AND Cantidad=2 AND PrecioUnitario=100 AND ProductoSkuSnapshot='{pa}';","durable detail")==1,"Persistent detail mismatch")
    require(n(f"SELECT COUNT(*) FROM ExistenciasVariante WHERE EmpresaId={owner} AND ProductoVarianteId={vid} AND StockFisico=3 AND StockReservado=0 AND StockTransito=0;","stock preservation")==1,"Draft modified stock")
    # No linked reservations, preparations, invoices, cash or other downstream rows.
    for parent,ident,allowed in (("PedidosVenta",oid,{"PedidoVentaDetalles"}),("Clientes",cid,{"PedidosVenta"})):
        refs=sql(f"""SELECT DISTINCT TABLE_NAME,COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA=DATABASE() AND REFERENCED_TABLE_SCHEMA=DATABASE() AND REFERENCED_TABLE_NAME='{parent}' AND REFERENCED_COLUMN_NAME='Id';""","downstream reference catalog").splitlines()
        require(bool(refs),"Missing FK metadata")
        for refrow in refs:
            table,column=refrow.split("\t")
            require(re.fullmatch(r"[A-Za-z0-9_]+",table) and re.fullmatch(r"[A-Za-z0-9_]+",column),"Invalid catalog identifier")
            if table not in allowed:
                require(n(f"SELECT COUNT(*) FROM {table} WHERE {column}={ident};","downstream "+table)==0,"Unexpected linked effect: "+table)
    print(f"QA_ORDER_PERSISTENCE=PASS order_id={oid} detail_count=1 stock_unchanged=YES",flush=True)
    # Read-only ERP identities are temporary, scoped to the exact A/B companies.
    iam=True
    statements=["START TRANSACTION;"]
    for name,company in ((namea,owner),(nameb,eb)):
        statements.extend([f"INSERT INTO Roles (Nombre,NombreNormalizado,Descripcion,EsSistema,EsAdministrador,Ambito,EmpresaId,Activo,Eliminado,FechaCreacion) VALUES ('{name}','{name.upper()}','QA checkout read only',0,0,1,{company},1,0,UTC_TIMESTAMP(6));","SET @r=LAST_INSERT_ID();",f"INSERT INTO RolPermisos (RolId,PermisoId) VALUES (@r,{grant});",f"INSERT INTO Usuarios (NombreUsuario,NombreCompleto,PasswordHash,RolId,Activo,Bloqueado,Eliminado,FechaCreacion) VALUES ('{name}','{name}','{hashes[name]}',@r,1,0,0,UTC_TIMESTAMP(6));","SET @u=LAST_INSERT_ID();",f"INSERT INTO UsuarioEmpresas (UsuarioId,EmpresaId,RolId,Activa,FechaCreacion,FechaActualizacion) VALUES (@u,{company},@r,1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));"])
    statements.append("COMMIT;")
    sql("\n".join(statements),"create exact read-only ERP users")
    time.sleep(65)
    def login(name,label):
        v=data(call("/auth/login",label,body={"nombreUsuario":name,"password":passwords[name]}))
        token=v.get("token")
        require(isinstance(token,str) and len(token)>20,"No ERP JWT")
        return token
    ta=login(namea,"QA_ORDER_ERP_LOGIN_A"); tb=login(nameb,"QA_ORDER_ERP_LOGIN_B")
    erp="/pedidos-venta/"+str(oid)
    got=data(call(erp,"QA_ORDER_ERP_READ_A",token=ta,tenant=owner))
    require(got["id"]==oid and got["clienteId"]==cid and got["estado"] in (1,"Borrador") and got["cotizacionId"] is None and money(got["subtotal"])==200 and money(got["costoEnvio"])==Decimal("37.25") and money(got["total"])==Decimal("237.25") and got["monedaCheckout"]==currency and got["tarifaEnvioCheckout"]==t1 and len(got["detalles"])==1,"ERP order snapshot mismatch")
    call(erp,"QA_ORDER_ERP_B_DENIED",404,token=tb,tenant=eb)
    call(erp,"QA_ORDER_ERP_TENANT_SPOOF_DENIED",403,token=tb,tenant=owner)
    call(erp,"QA_ORDER_ERP_ANONYMOUS_DENIED",401)
    require(store()[0]==owner,"Storefront authority changed")
    print("QA_ORDER_DEPLOYED_FUNCTIONAL=PASS",flush=True)
finally:
    if iam:
        names=f"('{namea}','{nameb}')"
        sql(f"""START TRANSACTION;
UPDATE Usuarios SET Activo=0,Bloqueado=1 WHERE NombreUsuario IN {names};
UPDATE UsuarioEmpresas ue JOIN Usuarios u ON ue.UsuarioId=u.Id SET ue.Activa=0 WHERE u.NombreUsuario IN {names};
DELETE rp FROM RolPermisos rp JOIN Roles r ON r.Id=rp.RolId WHERE r.Nombre IN {names};
UPDATE Roles SET Activo=0 WHERE Nombre IN {names};
COMMIT;""","revoke exact ERP fixture access")
        require(n(f"SELECT (SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario IN {names} AND Activo=1)+(SELECT COUNT(*) FROM Roles WHERE Nombre IN {names} AND Activo=1)+(SELECT COUNT(*) FROM RolPermisos rp JOIN Roles r ON r.Id=rp.RolId WHERE r.Nombre IN {names});","revocation")==0,"Fixture authority remains")
        sql(f"""START TRANSACTION;
DELETE ue FROM UsuarioEmpresas ue JOIN Usuarios u ON u.Id=ue.UsuarioId WHERE u.NombreUsuario IN {names};
DELETE FROM Usuarios WHERE NombreUsuario IN {names};
DELETE FROM Roles WHERE Nombre IN {names};
COMMIT;""","remove exact ERP fixture identities")
        require(n(f"SELECT (SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario IN {names})+(SELECT COUNT(*) FROM Roles WHERE Nombre IN {names});","IAM cleanup")==0,"IAM fixture remains")
        print("QA_ORDER_ERP_IAM_CLEANUP=PASS",flush=True)
    if order_attempted:
        # If a non-draft or unexpected order exists, fail closed; never force-delete it.
        require(n(f"""SELECT COUNT(*) FROM PedidosVenta p LEFT JOIN Clientes c ON c.Id=p.ClienteId
WHERE p.IdempotencyKey IN ({keys}) AND NOT (p.EmpresaId={owner} AND p.Estado=1 AND p.CotizacionId IS NULL AND p.CreadoPorNombreUsuario='storefront-invitado' AND p.ClienteNombreSnapshot='{guest}' AND c.EmpresaId={owner} AND c.Nombre='{guest}' AND c.Correo='{email}');""","cleanup ownership")==0,"Unsafe order cleanup; retain for review")
        sql(f"""START TRANSACTION;
DELETE p FROM PedidosVenta p JOIN Clientes c ON c.Id=p.ClienteId WHERE p.EmpresaId={owner} AND p.Estado=1 AND p.CotizacionId IS NULL AND p.IdempotencyKey IN ({keys}) AND p.CreadoPorNombreUsuario='storefront-invitado' AND p.ClienteNombreSnapshot='{guest}' AND c.EmpresaId={owner} AND c.Nombre='{guest}' AND c.Correo='{email}';
DELETE c FROM Clientes c LEFT JOIN PedidosVenta p ON p.ClienteId=c.Id WHERE c.EmpresaId={owner} AND c.Nombre='{guest}' AND c.Correo='{email}' AND c.CreadoPorNombreUsuario='storefront-invitado' AND p.Id IS NULL;
COMMIT;""","remove only exact draft and synthetic guest")
        require(n(f"SELECT COUNT(*) FROM PedidosVenta WHERE IdempotencyKey IN ({keys});","order cleanup")==0 and n(f"SELECT COUNT(*) FROM Clientes WHERE Nombre='{guest}' OR Correo='{email}';","guest cleanup")==0,"Order fixture remains")
        print("QA_ORDER_DRAFT_CLEANUP=PASS",flush=True)
    if created:
        # Identify only the exact names/owners created above, even after partial
        # fixture creation or network failure. No broad delete of company data.
        sql(f"""START TRANSACTION;
DELETE e FROM ExistenciasVariante e JOIN ProductoVariantes v ON v.Id=e.ProductoVarianteId JOIN Productos p ON p.Id=v.ProductoId JOIN Almacenes a ON a.Id=e.AlmacenId JOIN Sucursales s ON s.Id=a.SucursalId
WHERE e.EmpresaId={owner} AND p.EmpresaId={owner} AND v.EmpresaId={owner} AND p.Nombre='{pa}' AND v.Sku='{pa}' AND a.Codigo='{warehouse}' AND s.EmpresaId={owner} AND s.Codigo='{branch}';
DELETE v FROM ProductoVariantes v JOIN Productos p ON p.Id=v.ProductoId LEFT JOIN Empresas e ON e.Id=p.EmpresaId
WHERE (p.EmpresaId={owner} AND v.EmpresaId={owner} AND p.Nombre='{pa}' AND v.Sku='{pa}') OR (e.Nombre='{pb}' AND v.EmpresaId=e.Id AND p.Nombre='{pb}' AND v.Sku='{pb}');
DELETE p FROM Productos p LEFT JOIN Empresas e ON e.Id=p.EmpresaId WHERE (p.EmpresaId={owner} AND p.Nombre='{pa}') OR (e.Nombre='{pb}' AND p.Nombre='{pb}');
DELETE c FROM CostosEnvio c LEFT JOIN Empresas e ON e.Id=c.EmpresaId WHERE (c.EmpresaId={owner} AND c.Nombre IN ('{t0}','{t1}')) OR (e.Nombre='{pb}' AND c.Nombre='{tb}');
DELETE a FROM Almacenes a JOIN Sucursales s ON s.Id=a.SucursalId WHERE a.Codigo='{warehouse}' AND s.EmpresaId={owner} AND s.Codigo='{branch}';
DELETE FROM Sucursales WHERE EmpresaId={owner} AND Codigo='{branch}';
DELETE e FROM Empresas e LEFT JOIN RegistrosAuditoria r ON r.EmpresaId=e.Id WHERE e.Nombre='{pb}' AND r.Id IS NULL;
UPDATE Empresas SET Activa=0 WHERE Nombre='{pb}';
COMMIT;""","remove only run-owned checkout fixtures")
        for table,field,values in (("Empresas","Nombre",[pb]),("Sucursales","Codigo",[branch]),("Almacenes","Codigo",[warehouse]),("Productos","Nombre",[pa,pb]),("ProductoVariantes","Sku",[pa,pb]),("CostosEnvio","Nombre",[t0,t1,tb])):
            literal=",".join("'"+x+"'" for x in values)
            require(n(f"SELECT COUNT(*) FROM {table} WHERE {field} IN ({literal})"+(" AND Activa=1" if table=="Empresas" else "")+";","fixture cleanup "+table)==0,"Fixture cleanup not verified")
        require(store()[0]==owner,"Storefront ownership changed during cleanup")
        print("QA_CHECKOUT_FIXTURES_REMOVED=PASS",flush=True)
print("QA_ORDER_REAL_DEPLOYED_CERTIFICATE=PASS",flush=True)
