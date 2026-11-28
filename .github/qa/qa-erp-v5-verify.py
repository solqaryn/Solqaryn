"""V5 exact constraint rehearsal, exclusively on disposable loopback MySQL."""
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
        "columns":"SELECT TABLE_NAME,COLUMN_NAME,ORDINAL_POSITION,COLUMN_TYPE,IS_NULLABLE,COLUMN_DEFAULT,EXTRA,GENERATION_EXPRESSION,CHARACTER_SET_NAME,COLLATION_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() ORDER BY TABLE_NAME,ORDINAL_POSITION",
        "fks":"SELECT k.TABLE_NAME,k.CONSTRAINT_NAME,k.ORDINAL_POSITION,k.COLUMN_NAME,k.REFERENCED_TABLE_NAME,k.REFERENCED_COLUMN_NAME,r.DELETE_RULE,r.UPDATE_RULE FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE k JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS r ON r.CONSTRAINT_SCHEMA=k.CONSTRAINT_SCHEMA AND r.TABLE_NAME=k.TABLE_NAME AND r.CONSTRAINT_NAME=k.CONSTRAINT_NAME WHERE k.TABLE_SCHEMA=DATABASE() AND k.REFERENCED_TABLE_NAME IS NOT NULL ORDER BY k.TABLE_NAME,k.CONSTRAINT_NAME,k.ORDINAL_POSITION",
        "indexes":"SELECT TABLE_NAME,INDEX_NAME,SEQ_IN_INDEX,COLUMN_NAME,NON_UNIQUE,INDEX_TYPE,SUB_PART,EXPRESSION FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA=DATABASE() ORDER BY TABLE_NAME,INDEX_NAME,SEQ_IN_INDEX",
        "checks":"SELECT t.TABLE_NAME,t.CONSTRAINT_NAME,t.ENFORCED,c.CHECK_CLAUSE FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS t JOIN INFORMATION_SCHEMA.CHECK_CONSTRAINTS c ON c.CONSTRAINT_SCHEMA=t.CONSTRAINT_SCHEMA AND c.CONSTRAINT_NAME=t.CONSTRAINT_NAME WHERE t.CONSTRAINT_SCHEMA=DATABASE() AND t.CONSTRAINT_TYPE='CHECK' AND NOT(t.TABLE_NAME='Cotizaciones' AND t.CONSTRAINT_NAME='CK_Cotizaciones_Estado') ORDER BY t.TABLE_NAME,t.CONSTRAINT_NAME",
        "triggers":"SELECT TRIGGER_NAME,EVENT_OBJECT_TABLE,EVENT_MANIPULATION,ACTION_TIMING,ACTION_STATEMENT FROM INFORMATION_SCHEMA.TRIGGERS WHERE TRIGGER_SCHEMA=DATABASE() ORDER BY TRIGGER_NAME",
        "tables":"SELECT TABLE_NAME,ENGINE,TABLE_COLLATION FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME"
    }.items():
        result[label]=hashlib.sha256(("\n".join(run(sql,label))+"\n").encode()).hexdigest()
    return result
def check(expected):
    actual=scalar("SELECT CONCAT(t.ENFORCED,':',c.CHECK_CLAUSE) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS t JOIN INFORMATION_SCHEMA.CHECK_CONSTRAINTS c ON c.CONSTRAINT_SCHEMA=t.CONSTRAINT_SCHEMA AND c.CONSTRAINT_NAME=t.CONSTRAINT_NAME WHERE t.CONSTRAINT_SCHEMA=DATABASE() AND t.TABLE_NAME='Cotizaciones' AND t.CONSTRAINT_NAME='CK_Cotizaciones_Estado' AND t.CONSTRAINT_TYPE='CHECK'","quote constraint")
    normalized=re.sub(r"[\s`()]","",actual).lower()
    require(normalized=="yes:estadoin"+expected,"Quote check differs from exact expected definition")
marker="20261128163400_QaCotizacionEstadoV5"
fp=pathlib.Path(os.environ["RUNNER_TEMP"])/"qa-erp-v5-fingerprints.json"
require(scalar("SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId='20261128150000_QaCommercialOwnershipV4'","V4 marker")=="1","V4 missing")
require(scalar("SELECT COUNT(*) FROM Cotizaciones WHERE Estado IS NULL OR Estado NOT IN (1,2,3,4,5)","quote states")=="0","Historical quote state outside model; no inferred remap")
if sys.argv[1]=="capture":
    require(scalar("SELECT COUNT(*) FROM __EFMigrationsHistory","history")=="113","Baseline history mismatch")
    require(scalar(f"SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId='{marker}'","V5 marker")=="0","V5 already applied")
    check("0,1,2,3,4")
    orig={t:columns(t) for t in names()}
    require(len(orig)==138,"Baseline table count mismatch")
    fp.write_text(json.dumps({"columns":orig,"data":{t:fingerprint(t,c) for t,c in orig.items()},"schema":schema()}))
    fp.chmod(0o600)
    print("QA_ERP_V5_RESTORE_BASELINE=PASS",flush=True)
else:
    orig=json.loads(fp.read_text())
    require(names()==sorted(orig["columns"]),"Table set drift")
    require(scalar("SELECT COUNT(*) FROM __EFMigrationsHistory","history")=="114","Unexpected history")
    require(scalar(f"SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId='{marker}'","V5 marker")=="1","V5 missing")
    check("1,2,3,4,5")
    require(schema()==orig["schema"],"Unapproved structural drift")
    for t,c in orig["columns"].items():
        require(columns(t)==c,"Column set changed "+t)
        where=f"WHERE MigrationId<>'{marker}'" if t=="__EFMigrationsHistory" else ""
        require(fingerprint(t,c,where)==orig["data"][t],"Historical data changed "+t)
    require(int(scalar("SELECT COUNT(*) FROM Cotizaciones WHERE Estado=3","accepted quote"))>0,"No accepted quote fixture")
    require(run("START TRANSACTION; UPDATE Cotizaciones SET Estado=5 WHERE Estado=3 LIMIT 1; SELECT ROW_COUNT(); ROLLBACK;","converted state")==["1"],"State 5 was not accepted")
    for invalid in (0,6):
        run(f"START TRANSACTION; UPDATE Cotizaciones SET Estado={invalid} WHERE Estado=3 LIMIT 1; ROLLBACK;","invalid state",expected=(3819,))
    require(fingerprint("Cotizaciones",orig["columns"]["Cotizaciones"])==orig["data"]["Cotizaciones"],"Rehearsal did not roll back")
    print("QA_ERP_V5_EXACT_REHEARSAL=PASS history=114 historical_data=preserved iam=preserved other_constraints=preserved state_5=accepted state_0_6=rejected",flush=True)
    print("QA_ORIGINAL_DB_WRITES=0",flush=True)
