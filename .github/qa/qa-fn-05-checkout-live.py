"""QA FN05 public quote live, synthetic products/tariffs only; no orders or payments."""
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


from decimal import Decimal
require(expected_sha=="0d8d89570ccd507996a44c59efa062ad9d757c64","Unexpected deployment baseline")
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
created=False
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
    def payload(p,v,city):
        return {"items":[{"productoId":p,"productoVarianteId":v,"unidades":2,"precioUnitario":1}],"envio":{"departamento":d,"ciudad":city,"zona":z,"modalidad":mode,"empresaId":eb,"monto":999},"empresaId":eb,"subtotal":1,"costoEnvio":999,"total":1}
    def quote(base,city,expected_cost,expected_name,label,tenant=None):
        result=data(call("/tienda/checkout/validar","QA_CHECKOUT_"+label,body=payload(pid,vid,city),method="POST",base=base,tenant=tenant))
        require(Decimal(str(result["subtotal"]))==Decimal("200"),"Server subtotal mismatch")
        got=result.get("costoEnvio")
        require((got is None and expected_cost is None) or (got is not None and expected_cost is not None and Decimal(str(got))==expected_cost),"Server tariff mismatch")
        if expected_name is not None:
            require(result.get("tarifaEnvio")==expected_name,"Wrong selected tariff")
        elif expected_cost is None:
            require(result.get("tarifaEnvio") is None,"Unquoted shipping named a tariff")
        require(Decimal(str(result["total"]))==Decimal("200")+(expected_cost or Decimal(0)),"Server total mismatch")
        line=result["lineas"]
        require(len(line)==1 and line[0]["productoId"]==pid and line[0]["productoVarianteId"]==vid and line[0]["unidades"]==2 and Decimal(str(line[0]["precioUnitario"]))==Decimal("100") and Decimal(str(line[0]["total"]))==Decimal("200"),"Server line mismatch")
        require(re.fullmatch(r"[0-9a-f]{32}",result["validacionId"]) is not None,"Missing validation ID")
        expires=datetime.datetime.fromisoformat(result["expiraUtc"].replace("Z","+00:00"))
        now=datetime.datetime.now(datetime.timezone.utc)
        require(now<expires<=now+datetime.timedelta(minutes=11),"Quote expiration mismatch")
        print("QA_CHECKOUT_"+label+"_VALUES=PASS",flush=True)
    # Positive quote on both deployment surfaces; the foreign tariff must not win.
    quote(backend,c0,Decimal(0),t0,"RENDER_ZERO")
    quote(proxy,c0,Decimal(0),t0,"PROXY_ZERO")
    quote(proxy,c1,Decimal("37.25"),t1,"PROXY_PAID")
    # A non-matching unique coverage is tested against existing general fallback.
    # Never disable an existing tariff merely to fabricate a no-quote outcome.
    fallback=sql(f"""SELECT Monto FROM CostosEnvio WHERE EmpresaId={owner} AND Activo=1 AND Eliminado=0
AND (VigenteDesde IS NULL OR VigenteDesde<=UTC_TIMESTAMP(6))
AND (VigenteHasta IS NULL OR VigenteHasta>=UTC_TIMESTAMP(6))
AND (Departamento IS NULL OR TRIM(Departamento)='' OR LOWER(TRIM(Departamento))=LOWER('{d}'))
AND (Ciudad IS NULL OR TRIM(Ciudad)='' OR LOWER(TRIM(Ciudad))=LOWER('{cx}'))
AND (Zona IS NULL OR TRIM(Zona)='' OR LOWER(TRIM(Zona))=LOWER('{z}'))
AND (Modalidad IS NULL OR TRIM(Modalidad)='' OR LOWER(TRIM(Modalidad))=LOWER('{mode}'))
ORDER BY ((Departamento IS NOT NULL AND TRIM(Departamento)<>'')+(Ciudad IS NOT NULL AND TRIM(Ciudad)<>'')+(Zona IS NOT NULL AND TRIM(Zona)<>'')+(Modalidad IS NOT NULL AND TRIM(Modalidad)<>'')) DESC,Prioridad,Id LIMIT 1;""","independent coverage fallback")
    quote(proxy,cx,Decimal(fallback) if fallback else None,None,"PROXY_FALLBACK" if fallback else "PROXY_NO_QUOTE")
    call("/tienda/checkout/validar","QA_CHECKOUT_FOREIGN_PRODUCT_DENIED",409,body=payload(foreign_pid,foreign_vid,c0),method="POST")
    call("/tienda/checkout/validar","QA_CHECKOUT_FOREIGN_VARIANT_DENIED",409,body=payload(pid,foreign_vid,c0),method="POST")
    call("/tienda/checkout/validar","QA_CHECKOUT_EMPTY_DENIED",400,body={"items":[]},method="POST")
    too_many=payload(pid,vid,c0); too_many["items"][0]["unidades"]=4
    call("/tienda/checkout/validar","QA_CHECKOUT_STOCK_DENIED",409,body=too_many,method="POST")
    require(n(f"SELECT COUNT(*) FROM ExistenciasVariante WHERE EmpresaId={owner} AND ProductoVarianteId={vid} AND StockFisico=3 AND StockReservado=0 AND StockTransito=0;","quote non-mutating stock")==1,"Quote mutated stock")
    # Test the exact synthetic variant only, while its denormalized quantity
    # remains 3. Neither zero stock nor missing authoritative rows may sell.
    sql(f"UPDATE ExistenciasVariante SET StockFisico=0 WHERE EmpresaId={owner} AND ProductoVarianteId={vid} AND StockFisico=3 AND StockReservado=0;","synthetic zero inventory")
    require(n(f"SELECT COUNT(*) FROM ExistenciasVariante WHERE EmpresaId={owner} AND ProductoVarianteId={vid} AND StockFisico=0;","zero fixture")==1,"Zero fixture not established")
    call("/tienda/checkout/validar","QA_CHECKOUT_ZERO_AUTHORITATIVE_DENIED",409,body=payload(pid,vid,c0),method="POST")
    sql(f"DELETE FROM ExistenciasVariante WHERE EmpresaId={owner} AND ProductoVarianteId={vid} AND StockFisico=0 AND StockReservado=0 AND StockTransito=0;","remove synthetic inventory source")
    require(n(f"SELECT COUNT(*) FROM ExistenciasVariante WHERE EmpresaId={owner} AND ProductoVarianteId={vid};","missing source")==0,"Inventory source remains")
    require(n(f"SELECT COUNT(*) FROM ProductoVariantes WHERE EmpresaId={owner} AND Id={vid} AND Cantidad=3;","denormalized fixture")==1,"Legacy quantity changed")
    call("/tienda/checkout/validar","QA_CHECKOUT_MISSING_AUTHORITATIVE_DENIED",409,body=payload(pid,vid,c0),method="POST")
    require(store()[0]==owner,"Public storefront authority changed")
    print("QA_CHECKOUT_LIVE=PASS",flush=True)
finally:
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
DELETE FROM Empresas WHERE Nombre='{pb}';
COMMIT;""","remove only run-owned checkout fixtures")
        for table,field,values in (("Empresas","Nombre",[pb]),("Sucursales","Codigo",[branch]),("Almacenes","Codigo",[warehouse]),("Productos","Nombre",[pa,pb]),("ProductoVariantes","Sku",[pa,pb]),("CostosEnvio","Nombre",[t0,t1,tb])):
            literal=",".join("'"+x+"'" for x in values)
            require(n(f"SELECT COUNT(*) FROM {table} WHERE {field} IN ({literal});","fixture cleanup "+table)==0,"Fixture cleanup not verified")
        require(store()[0]==owner,"Storefront ownership changed during cleanup")
        print("QA_CHECKOUT_FIXTURES_REMOVED=PASS",flush=True)
print("QA_CHECKOUT_PUBLIC_QUOTE_CERTIFICATE=PASS",flush=True)
