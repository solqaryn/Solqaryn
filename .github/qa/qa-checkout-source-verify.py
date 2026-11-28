"""Read-only parity of live QA with the verified checkout backup. No writes."""
import hashlib
import json
import os
import pathlib
import re
import secrets
import subprocess
import sys

def require(ok, reason):
    if not ok:
        raise RuntimeError(reason)

require(os.environ.get("CI")=="true" and os.environ.get("GITHUB_REF")=="refs/heads/qa","QA-only rehearsal")
require(sys.argv[1:] in (["source-baseline"],["source-after"]),"Explicit source verification required")
require(os.environ.get("GITHUB_REPOSITORY")=="solqaryn/Solqaryn" and os.environ.get("GITHUB_RUN_ATTEMPT")=="1","QA one-shot only")
require(os.environ.get("DB_HOST")=="solqaryn-mysql-solqaryn.h.aivencloud.com" and os.environ.get("DB_PORT")=="14402" and os.environ.get("DB_NAME")=="solqaryn_qa" and os.environ.get("DB_USER")=="solqaryn_qa_user","QA binding mismatch")
db="solqaryn_qa"
password=os.environ.get("DB_PASSWORD")
require(bool(password),"QA authentication required")
env={k:v for k,v in os.environ.items() if k not in ("DB_PASSWORD","BACKUP_PASSPHRASE","MYSQL_PWD")}
env["MYSQL_PWD"]=password
args=["mysql","--protocol=TCP","--ssl-mode=REQUIRED","--init-command=SET SESSION TRANSACTION READ ONLY","--connect-timeout=12","-h",os.environ["DB_HOST"],"-P","14402","-u","solqaryn_qa_user","--batch","--raw","--skip-column-names",db]
def run(sql, label, expected=None):
    p=subprocess.run(args,input=sql,text=True,capture_output=True,env=env,timeout=90)
    if expected is not None:
        m=re.search(r"ERROR ([0-9]+)",p.stderr)
        require(p.returncode!=0 and m and int(m.group(1)) in expected,label+": expected rejection not observed")
        return []
    if p.returncode:
        m=re.search(r"ERROR ([0-9]+)",p.stderr)
        raise RuntimeError(label+": SQL error "+(m.group(1) if m else "unknown"))
    return p.stdout.splitlines()
def scalar(sql,label):
    rows=run(sql,label)
    require(len(rows)==1,label+": expected one row")
    return rows[0]
def q(v):
    require(re.fullmatch(r"[A-Za-z0-9_]+",v) is not None,"Invalid identifier")
    return "\x60"+v+"\x60"
def cols(xs): return ", ".join(q(x) for x in xs)
require(scalar("SELECT DATABASE()","target DB")==db,"Wrong database")
require(scalar("SELECT SUBSTRING_INDEX(CURRENT_USER(),'@',1)","target user")=="solqaryn_qa_user","Wrong user")
require(re.fullmatch(r"8\.4\.11(?:-.*)?",scalar("SELECT VERSION()","target version")) is not None,"Wrong MySQL version")


def columns(t):
    return run(f"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='{t}' ORDER BY ORDINAL_POSITION","columns "+t)
def names():
    return run("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME","tables")
def fingerprint(t,c,where=""):
    projection=", ".join(f"IF({q(x)} IS NULL,'N',CONCAT('V',HEX(CAST({q(x)} AS BINARY))))" for x in c)
    rows=run(f"SELECT {projection} FROM {q(t)} {where}","fingerprint "+t)
    return [len(rows),hashlib.sha256(("\n".join(sorted(rows))+"\n").encode()).hexdigest()]
def schema():
    result={}
    for label,sql in {
        "columns":"SELECT TABLE_NAME,COLUMN_NAME,ORDINAL_POSITION,COLUMN_TYPE,IS_NULLABLE,COLUMN_DEFAULT,EXTRA,GENERATION_EXPRESSION,CHARACTER_SET_NAME,COLLATION_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND NOT(TABLE_NAME='PedidosVenta' AND COLUMN_NAME IN ('CostoEnvio','MonedaCheckout','TarifaEnvioCheckout','EntregaCheckoutJson')) ORDER BY TABLE_NAME,ORDINAL_POSITION",
        "fks":"SELECT k.TABLE_NAME,k.CONSTRAINT_NAME,k.ORDINAL_POSITION,k.COLUMN_NAME,k.REFERENCED_TABLE_NAME,k.REFERENCED_COLUMN_NAME,r.DELETE_RULE,r.UPDATE_RULE FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE k JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS r ON r.CONSTRAINT_SCHEMA=k.CONSTRAINT_SCHEMA AND r.TABLE_NAME=k.TABLE_NAME AND r.CONSTRAINT_NAME=k.CONSTRAINT_NAME WHERE k.TABLE_SCHEMA=DATABASE() AND k.REFERENCED_TABLE_NAME IS NOT NULL ORDER BY k.TABLE_NAME,k.CONSTRAINT_NAME,k.ORDINAL_POSITION",
        "indexes":"SELECT TABLE_NAME,INDEX_NAME,SEQ_IN_INDEX,COLUMN_NAME,NON_UNIQUE,INDEX_TYPE,SUB_PART,EXPRESSION FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA=DATABASE() ORDER BY TABLE_NAME,INDEX_NAME,SEQ_IN_INDEX",
        "checks":"SELECT t.TABLE_NAME,t.CONSTRAINT_NAME,t.ENFORCED,c.CHECK_CLAUSE FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS t JOIN INFORMATION_SCHEMA.CHECK_CONSTRAINTS c ON c.CONSTRAINT_SCHEMA=t.CONSTRAINT_SCHEMA AND c.CONSTRAINT_NAME=t.CONSTRAINT_NAME WHERE t.CONSTRAINT_SCHEMA=DATABASE() AND t.CONSTRAINT_TYPE='CHECK' AND NOT(t.TABLE_NAME='PedidosVenta' AND t.CONSTRAINT_NAME='CK_PedidosVenta_Checkout_Atomico') ORDER BY t.TABLE_NAME,t.CONSTRAINT_NAME",
        "triggers":"SELECT TRIGGER_NAME,EVENT_OBJECT_TABLE,EVENT_MANIPULATION,ACTION_TIMING,ACTION_STATEMENT FROM INFORMATION_SCHEMA.TRIGGERS WHERE TRIGGER_SCHEMA=DATABASE() ORDER BY TRIGGER_NAME",
        "tables":"SELECT TABLE_NAME,ENGINE,TABLE_COLLATION FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME"
    }.items():
        result[label]=hashlib.sha256(("\n".join(run(sql,label))+"\n").encode()).hexdigest()
    return result

marker="20261128183000_QaCheckoutPedidoV6"
fields={"CostoEnvio","MonedaCheckout","TarifaEnvioCheckout","EntregaCheckoutJson"}
fp=pathlib.Path(os.environ["RUNNER_TEMP"])/"qa-checkout-v6-fingerprints.json"
before=sys.argv[1]=="source-baseline"
orig=json.loads(fp.read_text())
require(names()==sorted(orig["columns"]) and len(names())==138,"Table set drift")
require(scalar("SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId='20261128163400_QaCotizacionEstadoV5'","Previous marker")=="1","Previous certified migration missing")
require(scalar("SELECT COUNT(*) FROM __EFMigrationsHistory","history")==("114" if before else "115"),"Unexpected history")
require(scalar(f"SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId='{marker}'","migration marker")==("0" if before else "1"),"Checkout marker mismatch")
require(schema()==orig["schema"],"Unapproved structural drift")
for t,c in orig["columns"].items():
    actual=columns(t)
    require(([x for x in actual if x not in fields] if t=="PedidosVenta" and not before else actual)==c,"Historical columns changed "+t)
    where=f"WHERE MigrationId<>'{marker}'" if t=="__EFMigrationsHistory" and not before else ""
    require(fingerprint(t,c,where)==orig["data"][t],"Historical data changed "+t)
if before:
    require(not fields.intersection(columns("PedidosVenta")),"Checkout columns already present")
    require(scalar("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='PedidosVenta' AND CONSTRAINT_NAME='CK_PedidosVenta_Checkout_Atomico'","constraint absence")=="0","Unexpected existing checkout constraint")
else:
    actual=run("SELECT COLUMN_NAME,COLUMN_TYPE,IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='PedidosVenta' AND COLUMN_NAME IN ('CostoEnvio','MonedaCheckout','TarifaEnvioCheckout','EntregaCheckoutJson')","new columns")
    require(set(actual)=={"CostoEnvio\tdecimal(18,2)\tYES","MonedaCheckout\tvarchar(3)\tYES","TarifaEnvioCheckout\tvarchar(120)\tYES","EntregaCheckoutJson\tlongtext\tYES"},"V6 fields differ")
    check=scalar("SELECT CONCAT(t.ENFORCED,':',c.CHECK_CLAUSE) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS t JOIN INFORMATION_SCHEMA.CHECK_CONSTRAINTS c ON c.CONSTRAINT_SCHEMA=t.CONSTRAINT_SCHEMA AND c.CONSTRAINT_NAME=t.CONSTRAINT_NAME WHERE t.CONSTRAINT_SCHEMA=DATABASE() AND t.TABLE_NAME='PedidosVenta' AND t.CONSTRAINT_NAME='CK_PedidosVenta_Checkout_Atomico'","checkout constraint")
    normalize=lambda x: re.sub(r"[\s`()]","",x).lower()
    require(normalize(check)==normalize("YES:((`CostoEnvio` IS NULL AND `MonedaCheckout` IS NULL AND `TarifaEnvioCheckout` IS NULL AND `EntregaCheckoutJson` IS NULL) OR (`CostoEnvio` IS NOT NULL AND `CostoEnvio` >= 0 AND `MonedaCheckout` IS NOT NULL AND CHAR_LENGTH(`MonedaCheckout`) = 3 AND `TarifaEnvioCheckout` IS NOT NULL AND `EntregaCheckoutJson` IS NOT NULL AND `CotizacionId` IS NULL))"),"Constraint differs")
    require(scalar("SELECT COUNT(*) FROM PedidosVenta WHERE CostoEnvio IS NOT NULL OR MonedaCheckout IS NOT NULL OR TarifaEnvioCheckout IS NOT NULL OR EntregaCheckoutJson IS NOT NULL","historical checkout values")=="0","Historical orders changed")
require(scalar("SELECT COUNT(*) FROM INFORMATION_SCHEMA.PROCESSLIST WHERE DB=DATABASE() AND ID<>CONNECTION_ID()","other sessions")=="0","Concurrent QA sessions detected")
print("QA_CHECKOUT_SOURCE_"+("BASELINE" if before else "AFTER")+"=PASS history="+("114" if before else "115")+" historical_data=preserved iam=preserved other_constraints=preserved",flush=True)
print("QA_CHECKOUT_SOURCE_VERIFICATION_READ_ONLY=PASS",flush=True)
