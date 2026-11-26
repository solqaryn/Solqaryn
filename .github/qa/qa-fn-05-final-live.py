"""QA FN05 live report and platform certificate. QA only; isolated fixtures."""
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

require(expected_sha == "54ece72064137f9e47307a3e5873285c7fc97355", "Unexpected deployed SHA")

prefix="qa5"+secrets.token_hex(5)
a,b,operator,platform,root_role,root_company,root_renamed,root_denied=(prefix+x for x in ("A","B","O","P","R","C","D","N"))
names="("+",".join("'"+x+"'" for x in (a,b,operator,platform,root_role,root_company,root_renamed,root_denied))+")"
for table,column in (("Empresas","Nombre"),("Roles","Nombre"),("Usuarios","NombreUsuario")):
    require(sql(f"SELECT COUNT(*) FROM {table} WHERE {column} IN {names};","fixture collision")=="0","Fixture collision")
def grant(code,ambito=1):
    require(re.fullmatch(r"[A-Z.]+",code) is not None,"Unexpected grant")
    value=sql(f"SELECT Id FROM Permisos WHERE Codigo='{code}' AND Ambito={ambito} AND Activo=1 AND Eliminado=0;","grant "+code)
    require(value.isdigit(),"Missing unique active permission "+code)
    return int(value)
tenant_grants=tuple(grant(x) for x in ("REPORTESADMINISTRATIVOS.VER","REPORTESADMINISTRATIVOS.EXPORTAR","CONFIGURACION.VER","CONFIGURACION.EDITAR","CONFIGURACION.CREAR","DASHBOARD.VER","AUDITORIA.VER","USUARIOS.VER","ROLES.VER","PERMISOS.ADMINISTRAR","PRODUCTOS.VER","CLIENTES.VER","PROVEEDORES.VER"))
operator_grants=tuple(grant(x) for x in ("REPORTESADMINISTRATIVOS.VER","REPORTESADMINISTRATIVOS.EXPORTAR","DASHBOARD.VER","AUDITORIA.VER","CONFIGURACION.VER"))
platform_grants=tuple(grant(x) for x in ("CONFIGURACION.VER","DASHBOARD.VER"))
actions=(("Ver",1),("Crear",2),("Editar",3),("Activar",8),("Desactivar",9))
require(sql("SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Permisos' AND COLUMN_NAME IN ('Modulo','Accion','Ambito') AND DATA_TYPE='int';","permission storage contract")=="3","Permission storage mismatch")
catalog=["START TRANSACTION;"]
for action,number in actions:
    code="CONFIGURACIONPLATAFORMA."+action.upper()
    exact=f"Codigo='{code}' AND Modulo=32 AND Accion={number} AND Ambito=2 AND EsSistema=1 AND Activo=1 AND Eliminado=0"
    require(sql(f"SELECT COUNT(*) FROM Permisos WHERE (Codigo='{code}' OR (Modulo=32 AND Accion={number})) AND NOT ({exact});","catalog collision")=="0","Existing global permission conflicts")
    catalog.append(f"INSERT INTO Permisos (Codigo,Nombre,Modulo,Accion,Ambito,EsSistema,Activo,Eliminado,FechaCreacion) SELECT '{code}','ConfiguracionPlataforma - {action}',32,{number},2,1,1,0,UTC_TIMESTAMP(6) WHERE NOT EXISTS (SELECT 1 FROM Permisos WHERE Codigo='{code}');")
catalog.append("COMMIT;")
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


def report_bytes(path,token,tenant,fmt):
    req=urllib.request.Request(proxy+path,headers={"Accept":"*/*","Authorization":"Bearer "+token,"X-Empresa-Id":str(tenant)},method="GET")
    try:
        response=opener.open(req,timeout=60)
    except urllib.error.HTTPError as exc:
        raise RuntimeError("Export HTTP "+str(exc.code)) from None
    with response:
        require(response.status==200 and response.headers.get("X-Solqaryn-Environment")=="QA","Export response scope/status mismatch")
        expected={"csv":"text/csv","xlsx":"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","pdf":"application/pdf"}[fmt]
        require(response.headers.get_content_type()==expected,"Wrong export MIME")
        disposition=response.headers.get("Content-Disposition","").lower()
        require("attachment" in disposition and "."+fmt in disposition,"Wrong export filename")
        raw=response.read(8*1024*1024+1)
        require(0<len(raw)<=8*1024*1024,"Invalid export size")
        return raw
def inspect_report(raw,fmt,own,foreign,label):
    if fmt=="csv":
        rows=list(csv.reader(io.StringIO(raw.decode("utf-8-sig"))))
        require(len(rows)>1 and len(rows[0])>=8 and all(len(row)==len(rows[0]) for row in rows),"CSV shape mismatch "+label)
        cells=[v for row in rows for v in row]
        require(own in cells and all(not any(x in v for x in foreign) for v in cells),"CSV isolation mismatch "+label)
    elif fmt=="xlsx":
        ns={"s":"http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
        with zipfile.ZipFile(io.BytesIO(raw)) as z:
            require(z.testzip() is None and "xl/workbook.xml" in z.namelist(),"Invalid XLSX")
            shared=[]
            if "xl/sharedStrings.xml" in z.namelist():
                shared=["".join(si.itertext()) for si in ET.fromstring(z.read("xl/sharedStrings.xml")).findall("s:si",ns)]
            cells=[]
            for name in z.namelist():
                if not re.fullmatch(r"xl/worksheets/sheet[0-9]+\.xml",name): continue
                sheet=ET.fromstring(z.read(name))
                require(not sheet.findall(".//s:f",ns),"Unexpected formula in XLSX")
                for c in sheet.findall(".//s:c",ns):
                    val=c.findtext("s:v",default="",namespaces=ns)
                    if c.get("t")=="s": val=shared[int(val)]
                    elif c.get("t")=="inlineStr": val="".join(c.find("s:is",ns).itertext())
                    cells.append(val)
            require(own in cells and all(not any(x in v for x in foreign) for v in cells),"XLSX isolation mismatch "+label)
    else:
        require(raw.startswith(b"%PDF-") and b"%%EOF" in raw[-1024:],"Invalid PDF "+label)
        result=subprocess.run(["pdftotext","-raw","-","-"],input=raw,capture_output=True,timeout=45)
        require(result.returncode==0,"PDF extraction failed "+label)
        compact=re.sub(r"\s+","",result.stdout.decode("utf-8"))
        require(own in compact and all(x not in compact for x in foreign),"PDF isolation mismatch "+label)
    print(label+"=PASS",flush=True)

created=False
try:
    wait_ready(backend,"QA_FN05_FINAL_RENDER_READY")
    wait_ready(proxy,"QA_FN05_FINAL_PROXY_READY")
    public_before=data(call("/empresa-configuracion/publica","QA_FN05_PUBLIC_IDENTITY"))
    theme_before=data(call("/tema-visual","QA_FN05_PUBLIC_THEME"))
    # Only append the five canonical catalog rows if missing. No existing grant changes.
    sql("\n".join(catalog),"append platform catalog")
    pg={action:grant("CONFIGURACIONPLATAFORMA."+action.upper(),2) for action,_ in actions}
    require(sql("SELECT COUNT(*) FROM Permisos WHERE Modulo=32 AND Ambito=2 AND Activo=1 AND Eliminado=0;","platform catalog")=="5","Unexpected global catalog")
    print("QA_FN05_PLATFORM_CATALOG=PASS",flush=True)
    statements=["START TRANSACTION;"]
    for suffix,name in (("a",a),("b",b)):
        statements.extend([f"INSERT INTO Empresas (Nombre,Activa,FechaCreacion,FechaActualizacion) VALUES ('{name}',1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));",f"SET @e{suffix}=LAST_INSERT_ID();"])
    for suffix,name,company,admin,ambito in (("a",a,"a",1,1),("b",b,"b",1,1),("o",operator,"b",0,1),("p",platform,"a",0,1),("r",root_role,None,0,2)):
        tenant=f"@e{company}" if company else "NULL"
        statements.extend([f"INSERT INTO Roles (Nombre,NombreNormalizado,Descripcion,EsSistema,EsAdministrador,Ambito,EmpresaId,Activo,Eliminado,FechaCreacion) VALUES ('{name}','{name.upper()}','Fixture QA FN05',0,{admin},{ambito},{tenant},1,0,UTC_TIMESTAMP(6));",f"SET @r{suffix}=LAST_INSERT_ID();"])
    for suffix,name,company in (("a",a,"a"),("b",b,"b"),("o",operator,"b"),("p",platform,"a")):
        statements.extend([f"INSERT INTO Usuarios (NombreUsuario,NombreCompleto,PasswordHash,RolId,Activo,Bloqueado,Eliminado,FechaCreacion) VALUES ('{name}','{name}','{hashes[name]}',@r{suffix},1,0,0,UTC_TIMESTAMP(6));",f"SET @u{suffix}=LAST_INSERT_ID();",f"INSERT INTO UsuarioEmpresas (UsuarioId,EmpresaId,RolId,Activa,FechaCreacion,FechaActualizacion) VALUES (@u{suffix},@e{company},@r{suffix},1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));"])
    for suffix,grants in (("a",tenant_grants),("b",tenant_grants),("o",operator_grants),("p",platform_grants),("r",tuple(pg.values()))):
        statements.extend(f"INSERT INTO RolPermisos (RolId,PermisoId) VALUES (@r{suffix},{p});" for p in grants)
    statements.extend(["INSERT INTO UsuarioRolesPlataforma (UsuarioId,RolId,Activa,FechaCreacion,FechaActualizacion) VALUES (@up,@rr,1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));","COMMIT;","SELECT @ea,@eb,@ua,@ub,@uo,@up,@ra,@rb,@ro,@rp,@rr;"])
    created=True
    ids=sql("\n".join(statements),"create isolated fixtures").split("\t")
    require(len(ids)==11 and all(x.isdigit() and int(x)>0 for x in ids),"Missing fixture ids")
    ea,eb,ua,ub,uo,up,ra,rb,ro,rp,rr=map(int,ids)
    require(ea!=eb and len({ua,ub,uo,up})==4 and len({ra,rb,ro,rp,rr})==5,"Fixture identity collision")
    ta=login(a,passwords[a],"QA_FN05_FINAL_LOGIN_A")
    tb=login(b,passwords[b],"QA_FN05_FINAL_LOGIN_B")
    to=login(operator,passwords[operator],"QA_FN05_FINAL_LOGIN_OPERATOR")
    tp=login(platform,passwords[platform],"QA_FN05_FINAL_LOGIN_PLATFORM")
    for token,tenant,other,uid,name,foreign,suffix,own_users,own_roles in ((ta,ea,eb,ua,a,(b,operator,root_role),"A",{ua,up},{ra,rp}),(tb,eb,ea,ub,b,(a,platform,root_role),"B",{ub,uo},{rb,ro})):
        caps=data(call("/empresa-configuracion/plataforma/capacidades","QA_FN05_TENANT_CAPS_"+suffix,token=token,tenant=tenant))
        require(all(caps.get(x) is False for x in ("ver","crear","editar","activar","desactivar")),"Tenant unexpectedly has global authority")
        call("/empresa-configuracion","QA_FN05_GLOBAL_READ_DENIED_"+suffix,403,token=token,tenant=tenant)
        call("/empresas/plataforma","QA_FN05_ROOT_DIRECTORY_DENIED_"+suffix,403,token=token,tenant=tenant)
        directory=data(call("/empresas","QA_FN05_DIRECTORY_"+suffix,token=token,tenant=tenant))
        require(len(directory)==1 and directory[0].get("id")==tenant,"Tenant directory leaked")
        call(f"/empresas/{other}","QA_FN05_DIRECTORY_FOREIGN_"+suffix,404,token=token,tenant=tenant)
        path=f"/empresa-configuracion/tenant/{tenant}"
        current=data(call(path,"QA_FN05_REPORT_EVENT_READ_"+suffix,token=token,tenant=tenant))
        payload={k:current[k] for k in ("nombre","rtn","direccion","moneda","zonaHoraria","impuestosJson","emisionJson","correoRemitente","correoNombreRemitente","version") if k in current}
        payload["direccion"]="QA report "+name
        result=data(call(path,"QA_FN05_REPORT_EVENT_WRITE_"+suffix,token=token,tenant=tenant,body=payload,method="PUT"))
        require(result.get("empresaId")==tenant and result.get("direccion")==payload["direccion"],"Report fixture event failed")
        audit=sql(f"SELECT COUNT(*) FROM RegistrosAuditoria WHERE EmpresaId={tenant} AND UsuarioId={uid} AND AmbitoAutoridad=1 AND Entidad='ConfigEmpresa' AND ReferenciaId={tenant};","report audit ownership")
        require(audit.isdigit() and int(audit)>0,"Missing scoped audit event")
        users=data(call("/reportes-administrativos/usuarios-accesos","QA_FN05_REPORT_USERS_"+suffix,token=token,tenant=tenant))
        require({x["usuarioId"] for x in users}==own_users,"Report users scope mismatch")
        roles=data(call("/reportes-administrativos/roles-permisos","QA_FN05_REPORT_ROLES_"+suffix,token=token,tenant=tenant))
        require(own_roles<={x["rolId"] for x in roles} and not any(any(x in row["rol"] for x in foreign) for row in roles),"Report role scope mismatch")
        require(all(not any("ConfiguracionPlataforma" in item for item in row.get("permisos",[])) for row in roles),"Global grant leaked")
        summary=data(call("/reportes-administrativos/resumen","QA_FN05_REPORT_SUMMARY_"+suffix,token=token,tenant=tenant))
        require(summary.get("usuariosTotales")==len(own_users) and summary.get("eventosAuditoria",0)>0,"Report summary mismatch")
        audit_summary=data(call("/reportes-administrativos/auditoria-resumen","QA_FN05_REPORT_AUDIT_SUMMARY_"+suffix,token=token,tenant=tenant))
        require(audit_summary.get("total",0)>0,"Missing audit summary")
        call("/reportes-administrativos/usuarios-accesos","QA_FN05_REPORT_FOREIGN_"+suffix,403,token=token,tenant=other)
        for report in ("usuarios","roles","auditoria"):
            for fmt in ("csv","xlsx","pdf"):
                path=f"/reportes-administrativos/exportar/{report}?formato={fmt}"
                inspect_report(report_bytes(path,token,tenant,fmt),fmt,name,foreign,f"QA_FN05_EXPORT_{suffix}_{report.upper()}_{fmt.upper()}")
                call(path,f"QA_FN05_EXPORT_FOREIGN_{suffix}_{report.upper()}_{fmt.upper()}",403,token=token,tenant=other)
    for endpoint in ("resumen","usuarios-accesos","roles-permisos","auditoria-resumen"):
        call("/reportes-administrativos/"+endpoint,"QA_FN05_REPORT_OPERATOR_DENIED_"+endpoint.upper(),403,token=to,tenant=eb)
    for fmt in ("csv","xlsx","pdf"):
        call("/reportes-administrativos/exportar/usuarios?formato="+fmt,"QA_FN05_EXPORT_OPERATOR_DENIED_"+fmt.upper(),403,token=to,tenant=eb)
        call("/reportes-administrativos/exportar/usuarios?formato="+fmt,"QA_FN05_EXPORT_ANONYMOUS_DENIED_"+fmt.upper(),401)
    call("/reportes-administrativos/exportar/usuarios?formato=invalid","QA_FN05_EXPORT_INVALID_DENIED",400,token=ta,tenant=ea)
    print("QA_FN05_REPORT_EXPORTS_AB=PASS",flush=True)

    caps=data(call("/empresa-configuracion/plataforma/capacidades","QA_FN05_PLATFORM_CAPS",token=tp,tenant=ea))
    require(all(caps.get(x) is True for x in ("ver","crear","editar","activar","desactivar")),"Explicit platform authority missing")
    data(call("/empresa-configuracion","QA_FN05_PLATFORM_PRIVATE_READ",token=tp,tenant=ea))
    directory=data(call("/empresas/plataforma","QA_FN05_PLATFORM_DIRECTORY",token=tp,tenant=ea))
    require({ea,eb}<={x["id"] for x in directory},"Platform directory missing fixture companies")
    root=data(call("/empresas","QA_FN05_PLATFORM_CREATE",201,token=tp,tenant=ea,body={"nombre":root_company}))
    er=root.get("id")
    require(type(er) is int and er>0 and er not in (ea,eb) and root.get("nombre")==root_company,"Invalid root fixture")
    call(f"/empresas/{er}","QA_FN05_PLATFORM_TENANT_EDIT_DENIED",403,token=ta,tenant=ea,body={"nombre":root_renamed},method="PUT")
    call(f"/empresas/{er}/desactivar","QA_FN05_PLATFORM_TENANT_DISABLE_DENIED",403,token=tb,tenant=eb,body={},method="PATCH")
    call("/empresas","QA_FN05_PLATFORM_TENANT_CREATE_DENIED",403,token=ta,tenant=ea,body={"nombre":root_denied})
    before=data(call(f"/empresas/plataforma/{er}","QA_FN05_PLATFORM_ROOT_INTACT",token=tp,tenant=ea))
    require(before.get("nombre")==root_company and before.get("activa") is True,"Denied root mutation changed fixture")
    updated=data(call(f"/empresas/{er}","QA_FN05_PLATFORM_EDIT",token=tp,tenant=ea,body={"nombre":root_renamed},method="PUT"))
    require(updated.get("nombre")==root_renamed,"Platform edit not persisted")
    for action,state in (("desactivar",False),("activar",True)):
        result=data(call(f"/empresas/{er}/{action}","QA_FN05_PLATFORM_"+action.upper(),token=tp,tenant=ea,body={},method="PATCH"))
        require(result.get("activa") is state,"Root state mismatch")
    require(sql(f"SELECT COUNT(*) FROM Empresas WHERE Id={er} AND Nombre='{root_renamed}' AND Activa=1;","root readback")=="1","Root mutation missing")
    sql(f"DELETE FROM RolPermisos WHERE RolId={rr} AND PermisoId IN ({','.join(str(pg[x]) for x in ('Crear','Editar','Activar','Desactivar'))});","revoke only run-owned platform write grants")
    caps=data(call("/empresa-configuracion/plataforma/capacidades","QA_FN05_PLATFORM_READONLY",token=tp,tenant=ea))
    require(caps.get("ver") is True and all(caps.get(x) is False for x in ("crear","editar","activar","desactivar")),"Platform revocation failed")
    call(f"/empresas/{er}","QA_FN05_PLATFORM_REVOKED_WRITE",403,token=tp,tenant=ea,body={"nombre":root_company},method="PUT")
    print("QA_FN05_PLATFORM_AUTHORITY=PASS",flush=True)

    with tempfile.TemporaryDirectory(prefix="qa-fn05-final-") as out:
        spec={"base":proxy.removesuffix("/api"),"out":out,"a":{"user":a,"password":passwords[a],"tenant":ea},"b":{"user":b,"password":passwords[b],"tenant":eb},"p":{"user":platform,"password":passwords[platform],"tenant":ea},"o":{"user":operator,"password":passwords[operator],"tenant":eb}}
        # No DB secret inheritance by Chromium.
        ui_env={k:v for k,v in os.environ.items() if k not in ("DB_PASSWORD","MYSQL_PWD","GH_TOKEN")}
        ui=subprocess.run(["node",".github/qa/qa-fn-05-final-ui.mjs"],input=json.dumps(spec),text=True,capture_output=True,timeout=360,env=ui_env)
        for line in (ui.stdout+"\n"+ui.stderr).splitlines():
            if re.fullmatch(r"QA_FN05_UI_[A-Z0-9_]+=PASS|QA_FN05_UI_FAILED_STAGE=[A-Z0-9_]+;CLASS=[A-Za-z0-9_]+",line): print(line,flush=True)
        require(ui.returncode==0,"Authenticated browser failed")
        for suffix,own,foreign in (("A",a,(b,operator,root_role)),("B",b,(a,platform,root_role))):
            for fmt in ("csv","xlsx","pdf"):
                path=pathlib.Path(out)/(suffix+"-usuarios."+fmt)
                require(path.is_file(),"Missing browser download")
                inspect_report(path.read_bytes(),fmt,own,foreign,f"QA_FN05_UI_DOWNLOAD_{suffix}_{fmt.upper()}")
    require(data(call("/empresa-configuracion/publica","QA_FN05_PUBLIC_IDENTITY_AFTER"))==public_before,"Global identity changed")
    require(data(call("/tema-visual","QA_FN05_PUBLIC_THEME_AFTER"))==theme_before,"Global theme changed")
    sql(f"UPDATE UsuarioRolesPlataforma SET Activa=0,FechaActualizacion=UTC_TIMESTAMP(6) WHERE UsuarioId={up} AND RolId={rr};","revoke ephemeral platform assignment")
    call("/empresa-configuracion","QA_FN05_PLATFORM_ASSIGNMENT_REVOKED",403,token=tp,tenant=ea)
    print("QA_FN05_FINAL_RUNTIME=PASS",flush=True)
finally:
    if created:
        sql(f"""START TRANSACTION;
UPDATE Usuarios SET Activo=0 WHERE NombreUsuario IN {names};
UPDATE UsuarioEmpresas ue JOIN Usuarios u ON ue.UsuarioId=u.Id SET ue.Activa=0 WHERE u.NombreUsuario IN {names};
UPDATE UsuarioRolesPlataforma ur JOIN Usuarios u ON u.Id=ur.UsuarioId SET ur.Activa=0 WHERE u.NombreUsuario IN {names};
DELETE rp FROM RolPermisos rp JOIN Roles r ON r.Id=rp.RolId WHERE r.Nombre IN {names};
UPDATE Roles SET Activo=0 WHERE Nombre IN {names};
UPDATE Empresas SET Activa=0 WHERE Nombre IN {names};
COMMIT;""","revoke run-owned authority")
        require(sql(f"""SELECT (SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario IN {names} AND Activo=1)
+(SELECT COUNT(*) FROM UsuarioRolesPlataforma ur JOIN Usuarios u ON u.Id=ur.UsuarioId WHERE u.NombreUsuario IN {names} AND ur.Activa=1)
+(SELECT COUNT(*) FROM UsuarioEmpresas ue JOIN Usuarios u ON u.Id=ue.UsuarioId WHERE u.NombreUsuario IN {names} AND ue.Activa=1)
+(SELECT COUNT(*) FROM RolPermisos rp JOIN Roles r ON r.Id=rp.RolId WHERE r.Nombre IN {names})
+(SELECT COUNT(*) FROM Empresas WHERE Nombre IN {names} AND Activa=1);""","revoke readback")=="0","Fixture access not revoked")
        print("QA_FN05_FINAL_REVOKE=PASS",flush=True)
        sql(f"""START TRANSACTION;
DELETE ur FROM UsuarioRolesPlataforma ur JOIN Usuarios u ON u.Id=ur.UsuarioId WHERE u.NombreUsuario IN {names};
DELETE ue FROM UsuarioEmpresas ue JOIN Usuarios u ON u.Id=ue.UsuarioId WHERE u.NombreUsuario IN {names};
DELETE c FROM ConfigEmpresas c JOIN Empresas e ON e.Id=c.EmpresaId WHERE e.Nombre IN {names};
DELETE FROM Usuarios WHERE NombreUsuario IN {names};
DELETE FROM Roles WHERE Nombre IN {names};
DELETE e FROM Empresas e LEFT JOIN RegistrosAuditoria r ON r.EmpresaId=e.Id WHERE e.Nombre IN {names} AND r.Id IS NULL;
COMMIT;""","cleanup exact run-owned identities and tenant config")
        require(sql(f"""SELECT (SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario IN {names})
+(SELECT COUNT(*) FROM Roles WHERE Nombre IN {names})
+(SELECT COUNT(*) FROM Empresas WHERE Nombre IN {names} AND Activa=1);""","cleanup readback")=="0","Fixture cleanup incomplete")
        print("QA_FN05_FINAL_CLEANUP=PASS",flush=True)
print("QA_FN05_FINAL_REPORT_PLATFORM_UI=PASS",flush=True)
