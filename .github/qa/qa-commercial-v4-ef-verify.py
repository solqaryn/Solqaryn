"""V4 physical candidate. Only for disposable loopback MySQL, never live."""
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
require(sys.argv[1:] in (["capture"],["verify"]),"Explicit local-only target required")
db="solqaryn_qa_restore_ephemeral"
password=os.environ.get("LOCAL_MYSQL_PASSWORD")
require(password=="solqaryn-ci-qa-disposable-only","Disposable credential required")
env={k:v for k,v in os.environ.items() if k not in ("DB_PASSWORD","BACKUP_PASSPHRASE","MYSQL_PWD")}
env["MYSQL_PWD"]=password
args=["mysql","--protocol=TCP","--connect-timeout=12","-h","127.0.0.1","-P","3306","-u","root","--batch","--raw","--skip-column-names",db]
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
require(scalar("SELECT SUBSTRING_INDEX(CURRENT_USER(),'@',1)","target user")=="root","Wrong user")
require(scalar("SELECT VERSION()","target version").startswith("8.4."),"Wrong MySQL version")

require(not any(os.environ.get(k) for k in ("DB_HOST","DB_PORT","DB_NAME","DB_USER","DB_PASSWORD","BACKUP_PASSPHRASE")), "Live credentials must be removed")
m=json.loads(pathlib.Path(".github/qa/qa-commercial-v4-ef-manifest.json").read_text())
tables=m["tableNames"]
edges=m["edges"]
require(len(set(tables))==8 and len(edges)==17 and len(m["keys"])==2,"Manifest mismatch")
def columns(t):
    return run(f"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='{t}' ORDER BY ORDINAL_POSITION","columns "+t)
def names():
    return run("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME","tables")
def fingerprint(t,c,where=""):
    projection=", ".join(f"IF({q(x)} IS NULL,'N',CONCAT('V',HEX(CAST({q(x)} AS BINARY))))" for x in c)
    rows=run(f"SELECT {projection} FROM {q(t)} {where}","fingerprint "+t)
    return [len(rows),hashlib.sha256(("\n".join(sorted(rows))+"\n").encode()).hexdigest()]
def fks():
    return sorted(run("SELECT CONCAT(k.TABLE_NAME,'|',k.CONSTRAINT_NAME,'|',k.COLUMN_NAME,'|',k.REFERENCED_TABLE_NAME,'|',k.REFERENCED_COLUMN_NAME,'|',r.DELETE_RULE,'|',r.UPDATE_RULE) FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE k JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS r ON r.CONSTRAINT_SCHEMA=k.CONSTRAINT_SCHEMA AND r.TABLE_NAME=k.TABLE_NAME AND r.CONSTRAINT_NAME=k.CONSTRAINT_NAME WHERE k.TABLE_SCHEMA=DATABASE() AND k.REFERENCED_TABLE_NAME IS NOT NULL","FK topology"))
def index(t,ix,c,unique):
    actual=run(f"SELECT COLUMN_NAME,NON_UNIQUE FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='{t}' AND INDEX_NAME='{ix}' ORDER BY SEQ_IN_INDEX","index "+ix)
    require(actual==[x+"\t"+("0" if unique else "1") for x in c],"Index mismatch "+ix)
def fk(t,name,c,parent,pc,cascade=False):
    actual=run(f"SELECT COLUMN_NAME,REFERENCED_TABLE_NAME,REFERENCED_COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='{t}' AND CONSTRAINT_NAME='{name}' ORDER BY ORDINAL_POSITION","FK "+name)
    require(actual==[f"{a}\t{parent}\t{b}" for a,b in zip(c,pc)],"FK mismatch "+name)
    rule=scalar(f"SELECT DELETE_RULE FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND TABLE_NAME='{t}' AND CONSTRAINT_NAME='{name}'","delete rule")
    require(rule=="CASCADE" if cascade else rule in ("RESTRICT","NO ACTION"),"Delete rule mismatch "+name)
fp=pathlib.Path(os.environ["RUNNER_TEMP"])/"qa-v4-fingerprints.json"
if sys.argv[1]=="capture":
    require(scalar("SELECT COUNT(*) FROM __EFMigrationsHistory","history")=="112","Baseline history mismatch")
    require(scalar("SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId='20261126223000_QaCommercialOwnershipV3'","V3 marker")=="1","V3 missing")
    orig={t:columns(t) for t in names()}
    require(len(orig)==138 and all("EmpresaId" not in orig[t] for t in tables),"Baseline drift")
    fp.write_text(json.dumps({"columns":orig,"data":{t:fingerprint(t,c) for t,c in orig.items()},"fks":fks()}))
    fp.chmod(0o600)
    print("QA_V4_EF_BASELINE=PASS",flush=True)
else:
    orig=json.loads(fp.read_text())
    require(names()==sorted(orig["columns"]),"Table set drift")
    require(scalar("SELECT COUNT(*) FROM __EFMigrationsHistory","history")=="113","Unexpected history")
    require(scalar("SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId='20261128150000_QaCommercialOwnershipV4'","V4 marker")=="1","Migration missing")
    for t,c in orig["columns"].items():
        where="WHERE MigrationId<>'20261128150000_QaCommercialOwnershipV4'" if t=="__EFMigrationsHistory" else ""
        require(fingerprint(t,c,where)==orig["data"][t],"Historical data changed "+t)
    require(set(orig["fks"])<=set(fks()),"Original FK drift")
    for t in tables:
        require(scalar(f"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='{t}' AND COLUMN_NAME='EmpresaId' AND IS_NULLABLE='YES' AND DATA_TYPE='int'","owner "+t)=="1","Owner mismatch "+t)
        index(t,"UX_QACT_"+t+"_OwnerId",["EmpresaId","Id"],True)
        fk(t,"FK_QACT_"+t+"_Empresa",["EmpresaId"],"Empresas",["Id"])
        require(scalar(f"SELECT COUNT(*) FROM {q(t)} WHERE EmpresaId IS NOT NULL","legacy "+t)=="0","Legacy owner inferred "+t)
    for e in edges:
        index(e["child"],e["ix"],e["cc"],False)
        fk(e["child"],e["fk"],e["cc"],e["parent"],e["pp"],e["cascade"])
    for k in m["keys"]:
        index(k["table"],k["index"],["EmpresaId"]+k["cols"],True)
    print("QA_V4_EF_EXACT_REHEARSAL=PASS owner_tables=8 scoped_edges=17 historical_data=preserved history=113",flush=True)
    print("QA_ORIGINAL_DB_WRITES=0",flush=True)
