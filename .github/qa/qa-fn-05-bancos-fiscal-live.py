"""QA banking and fiscal residual probes. Internal ledger and synthetic documents only."""
import sys
import hashlib
mode = sys.argv[1] if len(sys.argv) == 2 else ""
if mode not in ("bancos", "fiscal", "erp"):
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


require(expected_sha == "1c933a47f7c82f08a4c812e79d134fa7ce41f733", "Unexpected deployed SHA")
from decimal import Decimal
prefix = "qaf" + secrets.token_hex(5)
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
grant_codes = (
    ("FINANZAS.VER", "FINANZAS.CREAR", "FINANZAS.IMPORTAR", "FINANZAS.CERRAR", "FINANZAS.REABRIR") if mode == "bancos"
    else ("CATEGORIAS.CREAR", "METODOSPAGO.CREAR", "DESCUENTOS.CREAR", "DESCUENTOS.EDITAR",
          "IMPUESTOS.CREAR", "IMPUESTOS.EDITAR", "PRODUCTOS.VER", "COMPRAS.VER", "COMPRAS.CREAR",
          "COMPRAS.CONFIRMAR", "VENTAS.VER", "VENTAS.CREAR", "VENTAS.CONFIRMAR"))
if mode == "erp":
    grant_codes += ("VENTAS.APROBAR", "FACTURACION.VER", "FACTURACION.APLICAR", "FACTURACION.ANULAR")
grants = tuple(grant(code) for code in grant_codes)

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
        raw = r.read() if r.status in (200, 201) else b""
        data = json.loads(raw) if raw else None
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




created=False
fixture_bank_code=prefix.upper()+"BANK"
fixture_bank_created=False
try:
    wait_ready(backend,"QA_BF_RENDER_READY")
    wait_ready(proxy,"QA_BF_PROXY_READY")
    # Health endpoints remain available during maintenance. Wait for actual
    # authenticated-route availability before creating any synthetic identities.
    deadline=time.monotonic()+600
    for base in (backend,proxy):
        while True:
            request=urllib.request.Request(base+"/cuentas-bancarias",headers={"Accept":"application/json"},method="GET")
            try:
                response=opener.open(request,timeout=30)
            except urllib.error.HTTPError as error:
                response=error
            except (urllib.error.URLError,TimeoutError):
                require(time.monotonic()<deadline,"QA traffic readiness timeout")
                time.sleep(5)
                continue
            with response:
                status=response.status
                if status==401:
                    if base==proxy:
                        require(response.headers.get("X-Solqaryn-Environment")=="QA","Incorrect proxy environment")
                    break
                require(status in (502,503,504),"Unexpected anonymous banking status "+str(status))
            require(time.monotonic()<deadline,"QA traffic readiness timeout")
            time.sleep(5)
    print("QA_BF_MAINTENANCE_OFF_ANONYMOUS_401=PASS",flush=True)
    require(sql("SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId='20261128150000_QaCommercialOwnershipV4';","V4 marker")=="1","V4 absent")
    # All identities are synthetic and unique; no existing role or permission is changed.
    statements=["START TRANSACTION;"]
    for suffix,n in (("a",a),("b",b)):
        statements.extend([f"INSERT INTO Empresas (Nombre,Activa,FechaCreacion,FechaActualizacion) VALUES ('{n}',1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));",f"SET @e{suffix}=LAST_INSERT_ID();"])
    for suffix,n,company,admin in (("a",a,"a",1),("b",b,"b",1),("o",operator,"b",0)):
        statements.extend([f"INSERT INTO Roles (Nombre,NombreNormalizado,Descripcion,EsSistema,EsAdministrador,Ambito,EmpresaId,Activo,Eliminado,FechaCreacion) VALUES ('{n}','{n.upper()}','Fixture QA bancos fiscal',0,{admin},1,@e{company},1,0,UTC_TIMESTAMP(6));",f"SET @r{suffix}=LAST_INSERT_ID();",f"INSERT INTO Usuarios (NombreUsuario,NombreCompleto,PasswordHash,RolId,Activo,Bloqueado,Eliminado,FechaCreacion) VALUES ('{n}','{n}','{hashes[n]}',@r{suffix},1,0,0,UTC_TIMESTAMP(6));",f"SET @u{suffix}=LAST_INSERT_ID();",f"INSERT INTO UsuarioEmpresas (UsuarioId,EmpresaId,RolId,Activa,FechaCreacion,FechaActualizacion) VALUES (@u{suffix},@e{company},@r{suffix},1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));"])
        if admin:
            statements.extend(f"INSERT INTO RolPermisos (RolId,PermisoId) VALUES (@r{suffix},{p});" for p in grants)
    statements.extend(["COMMIT;","SELECT @ea,@eb,@ua,@ub,@uo,@ra,@rb,@ro;"])
    created=True
    ids=sql("\n".join(statements),"create banking fiscal fixtures").split("\t")
    require(len(ids)==8 and all(x.isdigit() and int(x)>0 for x in ids),"Missing fixture ids")
    ea,eb,ua,ub,uo,ra,rb,ro=map(int,ids)
    require(ea!=eb and len({ua,ub,uo})==3 and len({ra,rb,ro})==3,"Fixture identity collision")
    for eid,n in ((ea,a),(eb,b)):
        require(sql(f"SELECT COUNT(*) FROM Empresas WHERE Id={eid} AND Nombre='{n}' AND Activa=1;","fixture owner")=="1","Unexpected fixture company")
        require(sql(f"SELECT COUNT(*) FROM Categorias WHERE EmpresaId={eid};","fixture empty")=="0","Nonempty fixture")
    ta=login(a,passwords[a],"QA_BF_LOGIN_A")
    tb=login(b,passwords[b],"QA_BF_LOGIN_B")
    to=login(operator,passwords[operator],"QA_BF_LOGIN_OPERATOR")

    identities=((ta,ea,"A"),(tb,eb,"B"))
    denied=(400,403,404,409,422)
    def ident(obj):
        require(isinstance(obj,dict) and type(obj.get("id")) is int and obj["id"]>0,"Missing persisted identity")
        return obj["id"]
    def number(obj,key,value):
        require(key in obj and Decimal(str(obj[key]))==Decimal(str(value)),"Incorrect "+key)
    def owned_record(table,rid,eid):
        require(table in ("CuentasBancarias","ConciliacionesBancarias","Categorias","Descuentos","Impuestos","MetodosPago","Compras","Ventas"),"Unexpected fixture table")
        require(sql(f"SELECT COUNT(*) FROM {table} WHERE Id={rid} AND EmpresaId={eid};","owned record")=="1","Tenant owner not persisted")
    def invoke(token,eid,side,path,label,body=None,expected=200,method="POST"):
        return call(path,"QA_BF_"+label+"_"+side,expected,token=token,tenant=eid,body=body,method=method)
    if mode=="bancos":
        # The catalog is global. Never activate or edit an existing institution.
        # Supply a clearly labelled, run-owned synthetic bank only if QA is empty.
        bank=sql("SELECT Id FROM Bancos WHERE Activo=1 AND Eliminado=0 ORDER BY Id LIMIT 1;","eligible bank catalog")
        if not bank:
            require(sql(f"SELECT COUNT(*) FROM Bancos WHERE Codigo='{fixture_bank_code}';","bank fixture collision")=="0","Bank fixture collision")
            fixture_bank_created=True
            bank=sql(f"""START TRANSACTION;
INSERT INTO Bancos (Codigo,Nombre,Activo,Eliminado,FechaCreacion,FechaActualizacion) VALUES ('{fixture_bank_code}','Institucion sintetica QA {prefix}',1,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @bank=LAST_INSERT_ID();
COMMIT;
SELECT @bank;""","provision synthetic QA bank")
            require(sql(f"SELECT COUNT(*) FROM Bancos WHERE Id={int(bank)} AND Codigo='{fixture_bank_code}' AND Activo=1 AND Eliminado=0;","bank fixture persisted")=="1","Bank fixture unavailable")
            print("QA_BF_SYNTHETIC_BANK_PROVISIONED=PASS",flush=True)
        require(bank.isdigit() and int(bank)>0,"No active bank catalog")
        accounts={}
        movements={}
        now=datetime.datetime.now(datetime.timezone.utc)
        first=now.replace(day=1).date().isoformat()
        last=(now.replace(day=28)+datetime.timedelta(days=4)).replace(day=1).date()-datetime.timedelta(days=1)
        for token,eid,side in identities:
            require(sql(f"SELECT COUNT(*) FROM CuentasBancarias WHERE EmpresaId={eid};","empty account fixture")=="0","Bank fixture not empty")
            created_account=invoke(token,eid,side,"/cuentas-bancarias","ACCOUNT_CREATE",{"bancoId":int(bank),"nombre":prefix+side,"numeroCuenta":prefix+side,"moneda":"HNL","saldoInicial":0},201)
            aid=ident(created_account)
            owned_record("CuentasBancarias",aid,eid)
            require(created_account.get("numeroCuenta")==prefix+side,"Wrong created account")
            recon=sql(f"""START TRANSACTION;
INSERT INTO ConciliacionesBancarias (EmpresaId,CuentaBancariaId,FechaInicio,FechaFin,SaldoInicialBanco,SaldoFinalBanco,Estado,Observaciones,FechaCreacion,FechaActualizacion) VALUES ({eid},{aid},'{first}','{last.isoformat()}',0,100,1,'{prefix+side}',UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @c=LAST_INSERT_ID();
COMMIT;
SELECT @c;""","create isolated reconciliation")
            require(recon.isdigit() and int(recon)>0,"Invalid reconciliation fixture")
            rid=int(recon)
            owned_record("ConciliacionesBancarias",rid,eid)
            accounts[side]=(aid,rid)
            payload={"cuentaId":aid,"monto":100,"referencia":prefix+side,"idempotencyKey":prefix+"-"+side+"-dep"}
            invoke(token,eid,side,"/operaciones-bancarias/deposito","DEPOSIT",payload)
            invoke(token,eid,side,"/operaciones-bancarias/deposito","DEPOSIT_REPLAY",payload)
            row=sql(f"SELECT Id,Monto FROM MovimientosFinancieros WHERE EmpresaId={eid} AND ReferenciaId={aid} AND ModuloOrigen='Bancos' AND Descripcion='IdempotencyKey: {payload['idempotencyKey']}';","deposit persisted").split("\t")
            require(len(row)==2 and row[0].isdigit() and Decimal(row[1])==100,"Deposit duplicated or amount mismatch")
            movements[side]=int(row[0])
            bad=dict(payload,monto=101)
            invoke(token,eid,side,"/operaciones-bancarias/deposito","DEPOSIT_CONFLICT",bad,expected=denied)
            require(sql(f"SELECT COUNT(*) FROM MovimientosFinancieros WHERE EmpresaId={eid} AND ReferenciaId={aid} AND ModuloOrigen='Bancos';","deposit invariant")=="1","Deposit conflict mutated ledger")
        for token,eid,side in identities:
            other="B" if side=="A" else "A"
            aid,rid=accounts[side]
            foreign=accounts[other][0]
            invoke(token,eid,side,"/cuentas-bancarias/"+str(foreign),"ACCOUNT_FOREIGN",expected=(403,404),method="GET")
            invoke(token,eid,side,"/operaciones-bancarias/deposito","DEPOSIT_FOREIGN",{"cuentaId":foreign,"monto":1,"referencia":prefix,"idempotencyKey":prefix+"-"+side+"-foreign"},expected=denied)
            payload={"cuentaBancariaId":aid,"idempotencyKey":prefix+"-"+side+"-imp","movimientos":[{"fechaOperacion":first+"T12:00:00Z","monto":100,"referenciaExterna":prefix+side,"descripcion":"Fixture QA","identificadorExternoTransaccion":prefix+side}]}
            result=invoke(token,eid,side,"/conciliaciones-bancarias/importaciones-estado-cuenta","IMPORT",payload)
            number(result,"movimientosImportados",1)
            number(result,"movimientosDuplicadosIgnorados",0)
            require(not result["errores"],"Import returned errors")
            again=invoke(token,eid,side,"/conciliaciones-bancarias/importaciones-estado-cuenta","IMPORT_REPLAY",payload)
            number(again,"movimientosImportados",0)
            number(again,"movimientosDuplicadosIgnorados",1)
            require(not again["errores"],"Duplicate import returned errors")
            line=sql(f"SELECT Id FROM MovimientosEstadoCuenta WHERE EmpresaId={eid} AND ConciliacionBancariaId={rid} AND IdempotencyKey='{prefix+side}' AND Monto=100;","import physical")
            require(line.isdigit() and int(line)>0,"Import duplicate or missing")
            cross=invoke(token,eid,side,"/conciliaciones-bancarias/importaciones-estado-cuenta","IMPORT_FOREIGN",dict(payload,cuentaBancariaId=foreign))
            number(cross,"movimientosImportados",0)
            require(bool(cross["errores"]),"Foreign import not denied")
            close={"cuentaBancariaId":aid,"mes":now.month,"anio":now.year,"saldoFinalEstadoCuenta":100,"idempotencyKey":prefix+"-"+side+"-close"}
            pending=invoke(token,eid,side,"/conciliaciones-bancarias/cierre-periodo","CLOSE_INCOMPLETE",close)
            require(pending["exitoso"] is False,"Incomplete reconciliation closed")
            require(sql(f"SELECT COUNT(*) FROM ConciliacionesBancarias WHERE Id={rid} AND EmpresaId={eid} AND Estado=1;","pending state")=="1","Rejected close changed draft")
            match={"cuentaBancariaId":aid,"idempotencyKey":prefix+"-"+side+"-match","matches":[{"movimientoInternoId":movements[other],"identificadorExternoTransaccion":prefix+side}]}
            rejected=invoke(token,eid,side,"/conciliaciones-bancarias/matches","MATCH_FOREIGN",match)
            number(rejected,"matchesExitosos",0)
            require(bool(rejected["errores"]),"Foreign movement not denied")
            require(sql(f"SELECT COUNT(*) FROM MatchesConciliacion WHERE MovimientoEstadoCuentaId={int(line)} AND EmpresaId={eid};","foreign match invariant")=="0","Foreign match persisted")
            match["matches"][0]["movimientoInternoId"]=movements[side]
            result=invoke(token,eid,side,"/conciliaciones-bancarias/matches","MATCH_OWN",match)
            number(result,"matchesExitosos",1)
            require(not result["errores"],"Own match failed")
            require(sql(f"SELECT COUNT(*) FROM MatchesConciliacion WHERE MovimientoEstadoCuentaId={int(line)} AND EmpresaId={eid} AND MovimientoFinancieroId={movements[side]} AND MontoAplicado=100;","match physical")=="1","Match not persisted")
            result=invoke(token,eid,side,"/conciliaciones-bancarias/cierre-periodo","CLOSE",close)
            require(result["exitoso"] is True and not result["diferenciasPendientes"],"Reconciliation did not close")
            require(sql(f"SELECT COUNT(*) FROM ConciliacionesBancarias WHERE Id={rid} AND EmpresaId={eid} AND Estado=3 AND SaldoFinalBanco=100;","close physical")=="1","Closed state not persisted")
            replay=invoke(token,eid,side,"/conciliaciones-bancarias/cierre-periodo","CLOSE_REPLAY",close)
            require(replay["exitoso"] is False,"Closed reconciliation allowed another close")
            for path,tag,amount in (("retiro","WITHDRAWAL",25),("comision","FEE",3),("interes","INTEREST",2)):
                key=prefix+"-"+side+"-"+path
                op={"cuentaId":aid,"monto":amount,"referencia":prefix+side,"idempotencyKey":key}
                invoke(token,eid,side,"/operaciones-bancarias/"+path,tag,op)
                invoke(token,eid,side,"/operaciones-bancarias/"+path,tag+"_REPLAY",op)
                require(sql(f"SELECT COUNT(*) FROM MovimientosFinancieros WHERE EmpresaId={eid} AND ReferenciaId={aid} AND ModuloOrigen='Bancos' AND Descripcion='IdempotencyKey: {key}' AND Monto={amount};","bank ledger")=="1","Bank operation missing or duplicated")
            destination=ident(invoke(token,eid,side,"/cuentas-bancarias","TRANSFER_ACCOUNT_CREATE",{"bancoId":int(bank),"nombre":prefix+side+"T","numeroCuenta":prefix+side+"T","moneda":"HNL","saldoInicial":0},201))
            owned_record("CuentasBancarias",destination,eid)
            key=prefix+"-"+side+"-transfer"
            transfer={"cuentaId":aid,"cuentaDestinoId":destination,"monto":10,"referencia":prefix+side,"idempotencyKey":key}
            invoke(token,eid,side,"/operaciones-bancarias/transferencia","TRANSFER",transfer)
            invoke(token,eid,side,"/operaciones-bancarias/transferencia","TRANSFER_REPLAY",transfer)
            for account,suffix in ((aid,"Egreso"),(destination,"Ingreso")):
                require(sql(f"SELECT COUNT(*) FROM MovimientosFinancieros WHERE EmpresaId={eid} AND ReferenciaId={account} AND ModuloOrigen='Bancos' AND Descripcion='IdempotencyKey: {key}-{suffix}' AND Monto=10;","transfer persisted")=="1","Transfer leg missing or duplicated")
            invoke(token,eid,side,"/operaciones-bancarias/transferencia","TRANSFER_CONFLICT",dict(transfer,monto=11),expected=denied)
            crosskey=prefix+"-"+side+"-transfer-foreign"
            invoke(token,eid,side,"/operaciones-bancarias/transferencia","TRANSFER_FOREIGN",dict(transfer,cuentaDestinoId=foreign,idempotencyKey=crosskey),expected=denied)
            require(sql(f"SELECT COUNT(*) FROM MovimientosFinancieros WHERE ModuloOrigen='Bancos' AND Descripcion IN ('IdempotencyKey: {crosskey}-Egreso','IdempotencyKey: {crosskey}-Ingreso');","foreign transfer invariant")=="0","Foreign transfer changed ledger")
            key=prefix+"-"+side+"-adjust"
            external=prefix+side+"AJ"
            adjust={"cuentaBancariaId":aid,"idempotencyKey":key,"diferencias":[{"identificadorExternoTransaccion":external,"diferenciaMonto":4,"motivo":"Ajuste sintetico QA"}]}
            for label in ("ADJUST","ADJUST_REPLAY"):
                adjustment=invoke(token,eid,side,"/conciliaciones-bancarias/ajustes",label,adjust)
                number(adjustment,"ajustesSolicitados",1)
                require(not adjustment["errores"],"Adjustment returned errors")
            require(sql(f"SELECT COUNT(*) FROM MovimientosFinancieros WHERE EmpresaId={eid} AND ReferenciaId={aid} AND ModuloOrigen='Bancos' AND Descripcion='IdempotencyKey: {key}-{external}' AND Monto=4;","adjustment persisted")=="1","Adjustment missing or duplicated")
            cross=invoke(token,eid,side,"/conciliaciones-bancarias/ajustes","ADJUST_FOREIGN",dict(adjust,cuentaBancariaId=foreign,idempotencyKey=key+"-foreign"))
            number(cross,"ajustesSolicitados",0)
            require(bool(cross["errores"]),"Foreign adjustment not denied")
            require(sql(f"SELECT COUNT(*) FROM MovimientosFinancieros WHERE ModuloOrigen='Bancos' AND Descripcion='IdempotencyKey: {key}-foreign-{external}';","foreign adjustment invariant")=="0","Foreign adjustment changed ledger")
            require(sql(f"SELECT COUNT(*) FROM CuentasBancarias WHERE Id={aid} AND EmpresaId={eid} AND SaldoInicial=0;","account opening balance")=="1","Opening balance changed")
            reopening={"cuentaBancariaId":aid,"mes":now.month,"anio":now.year,"motivoReapertura":"Correccion sintetica QA","idempotencyKey":prefix+"-"+side+"-reopen"}
            foreign_reopen=invoke(token,eid,side,"/conciliaciones-bancarias/reapertura-periodo","REOPEN_FOREIGN",dict(reopening,cuentaBancariaId=foreign))
            require(foreign_reopen["exitoso"] is False,"Foreign period reopened")
            result=invoke(token,eid,side,"/conciliaciones-bancarias/reapertura-periodo","REOPEN",reopening)
            require(result["exitoso"] is True,"Reopening failed")
            require(sql(f"SELECT COUNT(*) FROM ConciliacionesBancarias WHERE Id={rid} AND EmpresaId={eid} AND Estado=4 AND SaldoFinalBanco=100 AND Observaciones='{prefix+side}';","historical period")=="1","Original reconciliation not preserved")
            new_id=sql(f"SELECT Id FROM ConciliacionesBancarias WHERE EmpresaId={eid} AND CuentaBancariaId={aid} AND Estado=1 AND Id<>{rid} AND SaldoFinalBanco=100;","reopened period")
            require(new_id.isdigit() and int(new_id)>0,"Missing new draft")
            require(sql(f"SELECT COUNT(*) FROM MovimientosEstadoCuenta WHERE ConciliacionBancariaId={rid} AND EmpresaId={eid};","historical statement")=="1","Historical statement lost")
            require(sql(f"SELECT COUNT(*) FROM MatchesConciliacion WHERE MovimientoEstadoCuentaId={int(line)} AND EmpresaId={eid} AND MovimientoFinancieroId={movements[side]} AND MontoAplicado=100;","historical match")=="1","Historical match lost")
            require(sql(f"SELECT COUNT(*) FROM MovimientosEstadoCuenta WHERE ConciliacionBancariaId={int(new_id)} AND EmpresaId={eid};","new draft statement")=="0","Historical matches copied into draft")
            replay=invoke(token,eid,side,"/conciliaciones-bancarias/reapertura-periodo","REOPEN_REPLAY",reopening)
            require(replay["exitoso"] is False,"Reopening replay created another draft")
            require(sql(f"SELECT COUNT(*) FROM ConciliacionesBancarias WHERE EmpresaId={eid} AND CuentaBancariaId={aid};","reopen invariant")=="2","Reopening duplicated versions")
            print("QA_BF_REOPEN_"+side+"=PASS",flush=True)
            print("QA_BF_TRANSFER_ADJUST_"+side+"=PASS",flush=True)
            print("QA_BF_BANK_PERSISTENCE_"+side+"=PASS",flush=True)
        invoke(to,eb,"OP","/cuentas-bancarias/"+str(accounts["B"][0]),"ACCOUNT_DENIED",expected=403,method="GET")
        invoke(to,eb,"OP","/conciliaciones-bancarias/importaciones-estado-cuenta","IMPORT_DENIED",{"cuentaBancariaId":accounts["B"][0],"movimientos":[]},expected=403)
        invoke(to,eb,"OP","/conciliaciones-bancarias/reapertura-periodo","REOPEN_DENIED",{"cuentaBancariaId":accounts["B"][0],"mes":now.month,"anio":now.year,"motivoReapertura":"QA"},expected=403)
        print("QA_BF_BANK_IMPORT_MATCH_CLOSE_LEDGER_AB=PASS",flush=True)
    else:
        fixtures={}
        for token,eid,side in identities:
            require(sql(f"SELECT COUNT(*) FROM Productos WHERE EmpresaId={eid};","empty fiscal fixture")=="0","Fiscal fixture not empty")
            category=ident(data(invoke(token,eid,side,"/categorias","CATEGORY_CREATE",{"nombre":prefix+side,"activa":True},201)))
            owned_record("Categorias",category,eid)
            code=prefix.upper()
            method=ident(data(invoke(token,eid,side,"/metodos-pago","METHOD_CREATE",{"nombre":prefix+side,"codigo":code,"tipo":"Efectivo","activo":True},201)))
            owned_record("MetodosPago",method,eid)
            discount_rate=10 if side=="A" else 20
            tax_rate=15 if side=="A" else 10
            dp={"nombre":prefix+"-D-"+side,"codigoPromocional":code,"tipo":"Porcentaje","valor":discount_rate,"categoriaIds":[category]}
            tp={"nombre":prefix+"-I-"+side,"codigo":code,"tipo":"Porcentaje","tasa":tax_rate,"operaciones":["Venta","Compra"],"categoriaIds":[category]}
            discount=ident(data(invoke(token,eid,side,"/descuentos","DISCOUNT_CREATE",dp,201)))
            tax=ident(data(invoke(token,eid,side,"/impuestos","TAX_CREATE",tp,201)))
            owned_record("Descuentos",discount,eid)
            owned_record("Impuestos",tax,eid)
            key=prefix+side
            values=sql(f"""START TRANSACTION;
INSERT INTO Sucursales (EmpresaId,Codigo,Nombre,Activa,Eliminado,ZonaHoraria,FechaCreacion,FechaActualizacion) VALUES ({eid},'{key}','{key}',1,0,'America/Tegucigalpa',UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @s=LAST_INSERT_ID();
INSERT INTO Almacenes (SucursalId,Codigo,Nombre,Tipo,Activo,Eliminado,FechaCreacion,FechaActualizacion) VALUES (@s,'{key}','{key}',1,1,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @w=LAST_INSERT_ID();
INSERT INTO Productos (EmpresaId,CategoriaId,Nombre,Marca,Modelo,TipoInventario,Cantidad,Costo,Precio,UmbralStockBajo,Activo,EsDestacado,Eliminado,FechaCreacion,FechaActualizacion) VALUES ({eid},{category},'{key}','','',1,8,30,100,1,1,0,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @p=LAST_INSERT_ID();
INSERT INTO ProductoVariantes (EmpresaId,ProductoId,Sku,Cantidad,UmbralStockBajo,Costo,Precio,Activo,Eliminado,EsTecnica,FechaCreacion,FechaActualizacion) VALUES ({eid},@p,'{key}',8,1,30,100,1,0,1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @v=LAST_INSERT_ID();
INSERT INTO ExistenciasVariante (EmpresaId,ProductoVarianteId,AlmacenId,StockFisico,StockReservado,StockTransito,StockMinimo,FechaCreacion,FechaActualizacion) VALUES ({eid},@v,@w,8,0,0,1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
COMMIT;
SELECT @p,@v;""","create isolated fiscal product").split("\t")
            require(len(values)==2 and all(v.isdigit() and int(v)>0 for v in values),"Invalid fiscal product fixture")
            product,variant=map(int,values)
            require(sql(f"SELECT COUNT(*) FROM Productos p JOIN ProductoVariantes v ON v.ProductoId=p.Id WHERE p.EmpresaId={eid} AND v.EmpresaId={eid} AND p.Id={product} AND v.Id={variant} AND p.CategoriaId={category};","physical fiscal product")=="1","Fiscal fixture ownership mismatch")
            fixtures[side]=(category,method,discount,tax,dp,tp,product,variant,discount_rate,tax_rate)
        clients={}
        if mode == "erp":
            for token,eid,side in identities:
                key=prefix+side
                currency="HNL" if side=="A" else "USD"
                # Synthetic legal identity and explicitly scoped client. No
                # existing customer, configuration or payment instrument touched.
                # Validate all mandatory columns against the explicit synthetic
                # insert contract before creating the fixture.
                mandatory={
                    "ConfigEmpresas":{"EmpresaId","Moneda","ZonaHoraria","ImpuestosJson","EmisionJson","CorreoConfigurado","Version","FechaCreacion","FechaActualizacion"},
                    "TipoClientes":{"EmpresaId","Codigo","Nombre","NombreNormalizado","ColorHex","EsSistema","Activo","Eliminado","Orden","EsPredeterminado","FechaCreacion","FechaActualizacion"},
                    "Clientes":{"EmpresaId","TipoClienteId","Nombre","Activo","FechaCreacion","FechaActualizacion"}
                }
                for table,provided in mandatory.items():
                    required=set(sql(f"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='{table}' AND IS_NULLABLE='NO' AND COLUMN_DEFAULT IS NULL AND EXTRA NOT LIKE '%auto_increment%' AND EXTRA NOT LIKE '%GENERATED%';","fixture required columns").splitlines())
                    require(required<=provided,"Synthetic insert missing required columns in "+table+": "+",".join(sorted(required-provided)))
                client=sql(f"""START TRANSACTION;
UPDATE Empresas SET Rtn='{key.upper()}',Direccion='Direccion sintetica {key}' WHERE Id={eid} AND Nombre='{key}';
INSERT INTO ConfigEmpresas (EmpresaId,Moneda,ZonaHoraria,ImpuestosJson,EmisionJson,CorreoConfigurado,Version,FechaCreacion,FechaActualizacion) VALUES ({eid},'{currency}','America/Tegucigalpa','{{}}','{{}}',0,1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
INSERT INTO TipoClientes (EmpresaId,Codigo,Nombre,NombreNormalizado,ColorHex,EsSistema,Activo,Eliminado,Orden,EsPredeterminado,FechaCreacion,FechaActualizacion) VALUES ({eid},'{key}','{key}','{key.upper()}','#FFFFFF',0,1,0,0,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @tipo=LAST_INSERT_ID();
INSERT INTO Clientes (EmpresaId,TipoClienteId,Nombre,Activo,FechaCreacion,FechaActualizacion) VALUES ({eid},@tipo,'{key}',1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @cliente=LAST_INSERT_ID();
COMMIT;
SELECT @cliente;""","create isolated ERP customer")
                require(client.isdigit() and int(client)>0,"Invalid ERP client")
                clients[side]=int(client)
                owned_record("Clientes",int(client),eid)
            orders={}
            for token,eid,side in identities:
                product,variant=fixtures[side][6:8]
                quote={"clienteId":clients[side],"observaciones":"Solicitud sintetica QA","detalles":[{"productoId":product,"productoVarianteId":variant,"cantidad":2,"precioUnitario":100}]}
                other="B" if side=="A" else "A"
                invoke(token,eid,side,"/cotizaciones","QUOTE_FOREIGN_CLIENT",dict(quote,clienteId=clients[other]),expected=denied)
                qid=ident(data(invoke(token,eid,side,"/cotizaciones","QUOTE_CREATE",quote,201)))
                owned_record("Cotizaciones",qid,eid)
                invoke(token,eid,side,f"/cotizaciones/{qid}/enviar","QUOTE_SEND")
                invoke(token,eid,side,f"/cotizaciones/{qid}/aceptar","QUOTE_ACCEPT")
                invoke(token,eid,side,f"/cotizaciones/{qid}/convertir","QUOTE_CONVERT")
                invoke(token,eid,side,f"/cotizaciones/{qid}/convertir","QUOTE_CONVERT_REPLAY")
                pid=sql(f"SELECT Id FROM PedidosVenta WHERE EmpresaId={eid} AND CotizacionId={qid} AND ClienteId={clients[side]};","persisted order")
                require(pid.isdigit() and int(pid)>0,"Order missing or duplicated")
                require(sql(f"SELECT COUNT(*) FROM PedidoVentaDetalles WHERE EmpresaId={eid} AND PedidoVentaId={int(pid)} AND ProductoId={product} AND ProductoVarianteId={variant} AND Cantidad=2 AND PrecioUnitario=100;","order line")=="1","Order line mismatch")
                pedido=data(invoke(token,eid,side,f"/pedidos-venta/{pid}","ORDER_READ",method="GET"))
                number(pedido,"total",200)
                orders[side]=(qid,int(pid))
                print("QA_ERP_QUOTE_ORDER_"+side+"=PASS",flush=True)
            for token,eid,side in identities:
                qid,pid=orders["B" if side=="A" else "A"]
                invoke(token,eid,side,f"/cotizaciones/{qid}","QUOTE_FOREIGN_READ",expected=(403,404),method="GET")
                invoke(token,eid,side,f"/pedidos-venta/{pid}","ORDER_FOREIGN_READ",expected=(403,404),method="GET")
                invoke(token,eid,side,f"/cotizaciones/{qid}/convertir","QUOTE_FOREIGN_CONVERT",expected=denied)
            print("QA_ERP_QUOTE_ORDER_AB=PASS",flush=True)
        documents={}
        invoices={}
        for token,eid,side in identities:
            category,method,discount,tax,dp,tp,product,variant,dr,tr=fixtures[side]
            other="B" if side=="A" else "A"
            foreign=fixtures[other]
            expected_discount=Decimal(200)*dr/100
            expected_sale_tax=(Decimal(200)-expected_discount)*tr/100
            expected_sale_total=200-expected_discount+expected_sale_tax
            expected_purchase_tax=Decimal(60)*tr/100
            expected_purchase_total=60+expected_purchase_tax
            sale_line={"productoId":product,"productoVarianteId":variant,"cantidad":2,"precioUnitario":1}
            sale={"clienteNombre":"Cliente final","metodoPago":prefix.upper(),"estadoPago":"Pendiente","codigoPromocional":prefix.upper(),"envioExonerado":True,"motivoExoneracionEnvio":"Fixture QA","descuento":9999,"impuesto":9999,"detalles":[sale_line]}
            if mode == "erp":
                sale["clienteId"]=clients[side]
            foreign_sale=dict(sale,detalles=[dict(sale_line,productoId=foreign[6],productoVarianteId=foreign[7])])
            invoke(token,eid,side,"/ventas/calcular","SALE_FOREIGN_PRODUCT",foreign_sale,expected=denied)
            invoke(token,eid,side,"/ventas/calcular","SALE_FOREIGN_VARIANT",dict(sale,detalles=[dict(sale_line,productoVarianteId=foreign[7])]),expected=denied)
            preview=data(invoke(token,eid,side,"/ventas/calcular","SALE_PREVIEW",sale))
            number(preview,"importeBruto",200)
            number(preview,"totalDescuento",expected_discount)
            number(preview,"totalImpuesto",expected_sale_tax)
            number(preview,"total",expected_sale_total)
            require([d["descuentoId"] for d in preview["descuentosAplicados"]]==[discount],"Wrong discount tenant")
            require([i["impuestoId"] for i in preview["impuestosAplicados"]]==[tax],"Wrong tax tenant")
            purchase_preview={"detalles":[dict(sale_line,precioUnitario=30)]}
            calculated=data(invoke(token,eid,side,"/compras/calcular","PURCHASE_PREVIEW",purchase_preview))
            number(calculated,"totalDescuento",0)
            number(calculated,"totalImpuesto",expected_purchase_tax)
            number(calculated,"total",expected_purchase_total)
            require([i["impuestoId"] for i in calculated["impuestosAplicados"]]==[tax],"Wrong purchase tax tenant")
            purchase={"proveedorNombre":prefix+"-Proveedor-"+side,"metodoPago":prefix.upper(),"estadoPago":"Pendiente","descuento":9999,"impuesto":9999,"detalles":[{"productoId":product,"productoVarianteId":variant,"cantidad":2,"costoUnitario":30}]}
            bought=data(invoke(token,eid,side,"/compras","PURCHASE_DRAFT",purchase,201))
            vid=ident(data(invoke(token,eid,side,"/ventas","SALE_DRAFT",sale,201)))
            cid=ident(bought)
            owned_record("Compras",cid,eid)
            owned_record("Ventas",vid,eid)
            require(sql(f"SELECT COUNT(*) FROM HistorialUsoDescuentos WHERE EmpresaId={eid} AND VentaId={vid};","draft use history")=="0","Draft consumed discount")
            require(sql(f"SELECT COUNT(*) FROM HistorialAplicacionImpuestos WHERE EmpresaId={eid};","draft tax history")=="0","Draft generated fiscal history")
            confirmed=data(invoke(token,eid,side,"/compras/"+str(cid)+"/confirmar","PURCHASE_CONFIRM"))
            number(confirmed,"impuesto",expected_purchase_tax)
            number(confirmed,"total",expected_purchase_total)
            sold=data(invoke(token,eid,side,"/ventas/"+str(vid)+"/confirmar","SALE_CONFIRM"))
            number(sold,"descuento",expected_discount)
            number(sold,"impuesto",expected_sale_tax)
            number(sold,"total",expected_sale_total)
            require(sql(f"SELECT COUNT(*) FROM Ventas WHERE Id={vid} AND EmpresaId={eid} AND Estado='Confirmada' AND Descuento={expected_discount} AND Impuesto={expected_sale_tax} AND Total={expected_sale_total};","sale persisted")=="1","Sale monetary snapshot mismatch")
            require(sql(f"SELECT COUNT(*) FROM Compras WHERE Id={cid} AND EmpresaId={eid} AND Estado='Confirmada' AND Impuesto={expected_purchase_tax} AND Total={expected_purchase_total};","purchase persisted")=="1","Purchase monetary snapshot mismatch")
            require(sql(f"SELECT COUNT(*) FROM VentaDescuentos WHERE EmpresaId={eid} AND VentaId={vid} AND DescuentoId={discount} AND MontoAplicado={expected_discount};","discount snapshot")=="1","Discount snapshot missing")
            require(sql(f"SELECT COUNT(*) FROM VentaImpuestos WHERE EmpresaId={eid} AND VentaId={vid} AND ImpuestoId={tax} AND MontoAplicado={expected_sale_tax};","sale tax snapshot")=="1","Sale tax snapshot missing")
            require(sql(f"SELECT COUNT(*) FROM CompraImpuestos WHERE EmpresaId={eid} AND CompraId={cid} AND ImpuestoId={tax} AND MontoAplicado={expected_purchase_tax};","purchase tax snapshot")=="1","Purchase tax snapshot missing")
            def verify_history():
                require(sql(f"SELECT COUNT(*) FROM HistorialUsoDescuentos WHERE EmpresaId={eid} AND VentaId={vid} AND DescuentoId={discount} AND MontoAplicado={expected_discount};","discount history")=="1","Discount history missing or duplicated")
                require(sql(f"SELECT COUNT(*) FROM HistorialAplicacionImpuestos WHERE EmpresaId={eid} AND ImpuestoId={tax} AND ((DocumentoTipo='Venta' AND DocumentoId={vid} AND MontoAplicado={expected_sale_tax}) OR (DocumentoTipo='Compra' AND DocumentoId={cid} AND MontoAplicado={expected_purchase_tax}));","tax history")=="2","Tax history mismatch")
                require(sql(f"SELECT COUNT(*) FROM HistorialAplicacionImpuestos WHERE EmpresaId={eid};","total tax history")=="2","Unexpected tax history")
            verify_history()
            invoice_id=sold.get("facturaId")
            require(type(invoice_id) is int and invoice_id>0,"Linked invoice missing")
            require(sql(f"SELECT COUNT(*) FROM Facturas WHERE Id={invoice_id} AND EmpresaId={eid} AND VentaId={vid} AND Total={expected_sale_total} AND SaldoPendiente={expected_sale_total};","internal invoice monetary snapshot")=="1","Invoice amount or tenant mismatch")
            invoke(token,eid,side,"/ventas/"+str(vid)+"/confirmar","SALE_RECONFIRM_DENIED",expected=denied)
            invoke(token,eid,side,"/compras/"+str(cid)+"/confirmar","PURCHASE_RECONFIRM_DENIED",expected=denied)
            invoke(token,eid,side,"/descuentos/"+str(discount),"DISCOUNT_EDIT",dict(dp,valor=dr+1),method="PUT")
            invoke(token,eid,side,"/impuestos/"+str(tax),"TAX_EDIT",dict(tp,tasa=tr+1),method="PUT")
            again=data(invoke(token,eid,side,"/ventas/"+str(vid),"SALE_HISTORICAL",method="GET"))
            number(again,"descuento",expected_discount)
            number(again,"impuesto",expected_sale_tax)
            number(again,"total",expected_sale_total)
            again=data(invoke(token,eid,side,"/compras/"+str(cid),"PURCHASE_HISTORICAL",method="GET"))
            number(again,"impuesto",expected_purchase_tax)
            number(again,"total",expected_purchase_total)
            verify_history()
            if mode == "erp":
                key=prefix+side
                currency="HNL" if side=="A" else "USD"
                require(sql(f"SELECT COUNT(*) FROM Facturas WHERE Id={invoice_id} AND EmpresaId={eid} AND EmpresaNombre='{key}' AND EmpresaRTN='{key.upper()}' AND EmpresaDireccion='Direccion sintetica {key}' AND Moneda='{currency}';","invoice legal identity")=="1","Invoice fiscal identity mismatch")
                invoice=data(invoke(token,eid,side,f"/facturas/{invoice_id}","INVOICE_READ",method="GET"))
                number(invoice,"saldoPendiente",expected_sale_total)
                require(invoice["empresaNombre"]==key and invoice["empresaRTN"]==key.upper() and invoice["moneda"]==currency,"Invoice API identity mismatch")
                partial=40
                rest=expected_sale_total-partial
                payment={"monto":partial,"metodoPago":prefix.upper(),"referencia":key+"-P1","observaciones":"Cobro ERP sintetico QA sin cargo externo"}
                paid=data(invoke(token,eid,side,f"/facturas/{invoice_id}/pagos","PAYMENT_PARTIAL",payment))
                number(paid,"totalPagado",partial)
                number(paid,"saldoPendiente",rest)
                require(paid["estado"]=="ParcialmentePagada","Partial invoice state mismatch")
                require(sql(f"SELECT COUNT(*) FROM FacturaPagos WHERE EmpresaId={eid} AND FacturaId={invoice_id} AND Monto={partial} AND Anulado=0 AND MetodoPagoId={method};","partial payment physical")=="1","Partial payment missing")
                require(sql(f"SELECT COUNT(*) FROM Ventas WHERE Id={vid} AND EmpresaId={eid} AND EstadoPago='Parcial';","partial sale payment")=="1","Partial sale state mismatch")
                full=data(invoke(token,eid,side,f"/facturas/{invoice_id}/pagos","PAYMENT_BALANCE",dict(payment,monto=float(rest),referencia=key+"-P2")))
                number(full,"totalPagado",expected_sale_total)
                number(full,"saldoPendiente",0)
                require(full["estado"]=="Pagada","Paid invoice state mismatch")
                require(sql(f"SELECT COUNT(*) FROM MovimientosFinancieros WHERE EmpresaId={eid} AND VentaId={vid} AND ModuloOrigen='Venta' AND Estado='Pagado' AND Monto={expected_sale_total};","paid ledger")=="1","Ledger payment synchronization missing")
                require(sql(f"SELECT COUNT(*) FROM Ventas WHERE Id={vid} AND EmpresaId={eid} AND EstadoPago='Pagado';","paid sale")=="1","Sale payment synchronization missing")
                invoke(token,eid,side,f"/facturas/{invoice_id}/pagos","PAYMENT_PAID_REJECTED",dict(payment,monto=1,referencia=key+"-EXCESS"),expected=denied)
                pid=sql(f"SELECT Id FROM FacturaPagos WHERE EmpresaId={eid} AND FacturaId={invoice_id} AND Referencia='{key}-P2' AND Anulado=0;","balance payment identity")
                require(pid.isdigit(),"Balance payment missing or duplicated")
                cancelled=data(invoke(token,eid,side,f"/facturas/{invoice_id}/pagos/{pid}/anular","PAYMENT_REVERSE",{"motivo":"Reversion sintetica QA"}))
                number(cancelled,"totalPagado",partial)
                number(cancelled,"saldoPendiente",rest)
                require(cancelled["estado"]=="ParcialmentePagada","Reversal invoice state mismatch")
                require(sql(f"SELECT COUNT(*) FROM FacturaPagos WHERE EmpresaId={eid} AND FacturaId={invoice_id} AND Id={int(pid)} AND Anulado=1;","reversed payment history")=="1","Reversal lost history")
                require(sql(f"SELECT COUNT(*) FROM MovimientosFinancieros WHERE EmpresaId={eid} AND VentaId={vid} AND ModuloOrigen='Venta' AND Estado='Pendiente';","reversal ledger")=="1","Ledger not restored")
                require(sql(f"SELECT COUNT(*) FROM Ventas WHERE Id={vid} AND EmpresaId={eid} AND EstadoPago='Parcial';","reversed sale")=="1","Sale not restored")
                require(sql(f"SELECT COUNT(*) FROM FacturaPagos WHERE EmpresaId={eid} AND FacturaId={invoice_id};","payment count")=="2","Unexpected duplicate payment")
                invoices[side]=invoice_id
                print("QA_ERP_INVOICE_PAYMENT_"+side+"=PASS",flush=True)
            documents[side]=(vid,cid)
            print("QA_BF_FISCAL_SNAPSHOTS_HISTORY_"+side+"=PASS",flush=True)
        for token,eid,side in identities:
            other="B" if side=="A" else "A"
            vid,cid=documents[other]
            invoke(token,eid,side,"/ventas/"+str(vid),"SALE_FOREIGN_READ",expected=(403,404),method="GET")
            invoke(token,eid,side,"/compras/"+str(cid),"PURCHASE_FOREIGN_READ",expected=(403,404),method="GET")
        invoke(to,eb,"OP","/ventas/"+str(documents["B"][0]),"SALE_DENIED",expected=403,method="GET")
        print("QA_BF_FISCAL_SNAPSHOTS_HISTORY_AB=PASS",flush=True)
        if mode=="erp":
            for token,eid,side in identities:
                foreign_invoice=invoices["B" if side=="A" else "A"]
                invoke(token,eid,side,f"/facturas/{foreign_invoice}","INVOICE_FOREIGN_READ",expected=(403,404),method="GET")
                invoke(token,eid,side,f"/facturas/{foreign_invoice}/pagos","PAYMENT_FOREIGN",{"monto":1,"metodoPago":prefix.upper(),"referencia":prefix+"-FOREIGN"},expected=denied)
            invoke(to,eb,"OP",f"/facturas/{invoices['B']}/pagos","PAYMENT_OPERATOR_DENIED",{"monto":1,"metodoPago":prefix.upper()},expected=403)
            for side in ("A","B"):
                require(sql(f"SELECT COUNT(*) FROM FacturaPagos WHERE FacturaId={invoices[side]};","final payment count")=="2","Denied payment modified invoice")
            print("QA_ERP_INTERNAL_COLLECTION_AB=PASS",flush=True)

finally:
    if created:
        sql(f"""START TRANSACTION;
UPDATE Descuentos d JOIN Empresas e ON e.Id=d.EmpresaId SET d.Activo=0 WHERE e.Nombre IN {names};
UPDATE Impuestos i JOIN Empresas e ON e.Id=i.EmpresaId SET i.Activo=0 WHERE e.Nombre IN {names};
UPDATE CuentasBancarias c JOIN Empresas e ON e.Id=c.EmpresaId SET c.Estado=2 WHERE e.Nombre IN {names};
UPDATE Bancos SET Activo=0,FechaActualizacion=UTC_TIMESTAMP(6) WHERE Codigo='{fixture_bank_code}' AND Nombre='Institucion sintetica QA {prefix}' AND {int(fixture_bank_created)}=1;
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
+(SELECT COUNT(*) FROM Empresas WHERE Nombre IN {names} AND Activa=1)
+(SELECT COUNT(*) FROM Descuentos d JOIN Empresas e ON e.Id=d.EmpresaId WHERE e.Nombre IN {names} AND d.Activo=1)
+(SELECT COUNT(*) FROM Impuestos i JOIN Empresas e ON e.Id=i.EmpresaId WHERE e.Nombre IN {names} AND i.Activo=1)
+(SELECT COUNT(*) FROM CuentasBancarias c JOIN Empresas e ON e.Id=c.EmpresaId WHERE e.Nombre IN {names} AND c.Estado=1)
+(SELECT COUNT(*) FROM Bancos WHERE Codigo='{fixture_bank_code}' AND Nombre='Institucion sintetica QA {prefix}' AND Activo=1 AND {int(fixture_bank_created)}=1);""","gallery cleanup readback")=="0","Fixture authority not revoked")
        print("QA_BF_FIXTURES_REVOKED=PASS",flush=True)

print("QA_BF_"+mode.upper()+"_CERTIFICATE=PASS",flush=True)
