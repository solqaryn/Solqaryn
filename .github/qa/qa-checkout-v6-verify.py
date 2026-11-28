"""V6 exact checkout rehearsal, exclusively on disposable loopback MySQL."""
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
# --skip-extended-insert yields one complete INSERT per row. Read only
# table names and counts from the very same transaction-consistent SQL dump.
# Never query the mutable live database again to compare this snapshot.
def dump_counts(lines):
    structures, counts = set(), {}
    active = None
    complete = False
    for line in lines:
        line = line.rstrip(b"\r\n")
        table = re.fullmatch(rb"CREATE TABLE \x60([A-Za-z0-9_]+)\x60 \(", line)
        section = re.fullmatch(rb"-- Dumping data for table \x60([A-Za-z0-9_]+)\x60", line)
        if table:
            active = None
            name = table[1].decode("ascii")
            require(name not in structures, "Duplicate dump table structure")
            structures.add(name)
        elif section:
            name = section[1].decode("ascii")
            require(name in structures and name not in counts, "Invalid dump data section")
            counts[name] = 0
            active = name
        elif line.startswith(b"UNLOCK TABLES;") or line.startswith(b"-- Table structure for table ") or line.startswith(b"-- Dumping routines") or line.startswith(b"-- Dumping events"):
            active = None
        elif active is not None and line.startswith(b"INSERT INTO "):
            expected = b"INSERT INTO " + bytes([96]) + active.encode("ascii") + bytes([96]) + b" VALUES ("
            require(line.startswith(expected) and line.endswith(b");"), "Invalid single-row dump statement")
            counts[active] += 1
        elif line.startswith(b"-- Dump completed on "):
            complete = True
            active = None
    require(complete and structures == set(counts) and len(counts) > 0, "Incomplete dump structure/data sections")
    return counts

if sys.argv[1:] == ["self-test-dump"]:
    fake = [
        b"CREATE TABLE \x60A\x60 (\n", b"-- Dumping data for table \x60A\x60\n",
        b"INSERT INTO \x60A\x60 VALUES (1,'text, (value);');\n",
        b"INSERT INTO \x60A\x60 VALUES (2,'escaped\\nnewline');\n",
        b"UNLOCK TABLES;\n", b"CREATE TABLE \x60B\x60 (\n",
        b"-- Dumping data for table \x60B\x60\n", b"UNLOCK TABLES;\n",
        b"-- Dump completed on 2026-11-28\n",
    ]
    require(dump_counts(fake) == {"A": 2, "B": 0}, "Dump count self-test")
    try:
        dump_counts(fake[:-1])
    except RuntimeError:
        pass
    else:
        raise RuntimeError("Truncated dump must be rejected")
    print("QA_DUMP_MANIFEST_SELF_TEST=PASS", flush=True)
    sys.exit(0)

if sys.argv[1:2] == ["dump-manifest"]:
    require(len(sys.argv) == 4, "Explicit dump and manifest paths required")
    folder = pathlib.Path(os.environ["RUNNER_TEMP"]) / "solqaryn-qa-oracle-backup"
    dump = pathlib.Path(sys.argv[2])
    manifest = pathlib.Path(sys.argv[3])
    require(dump == folder / "qa.sql" and manifest == folder / "qa-rowcounts.json", "Unexpected backup path")
    with dump.open("rb") as stream:
        counts = dump_counts(stream)
    require(len(counts) == 138, "Snapshot table count mismatch")
    manifest.write_text(json.dumps(counts, sort_keys=True))
    manifest.chmod(0o600)
    print("QA_DUMP_SNAPSHOT_MANIFEST=PASS tables=138", flush=True)
    sys.exit(0)

require(sys.argv[1:] in (["capture"],["verify"],["verify-restore"]),"Explicit local-only target required")
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
        "columns":"SELECT TABLE_NAME,COLUMN_NAME,ORDINAL_POSITION,COLUMN_TYPE,IS_NULLABLE,COLUMN_DEFAULT,EXTRA,GENERATION_EXPRESSION,CHARACTER_SET_NAME,COLLATION_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND NOT(TABLE_NAME='PedidosVenta' AND COLUMN_NAME IN ('CostoEnvio','MonedaCheckout','TarifaEnvioCheckout','EntregaCheckoutJson')) ORDER BY TABLE_NAME,ORDINAL_POSITION",
        "fks":"SELECT k.TABLE_NAME,k.CONSTRAINT_NAME,k.ORDINAL_POSITION,k.COLUMN_NAME,k.REFERENCED_TABLE_NAME,k.REFERENCED_COLUMN_NAME,r.DELETE_RULE,r.UPDATE_RULE FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE k JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS r ON r.CONSTRAINT_SCHEMA=k.CONSTRAINT_SCHEMA AND r.TABLE_NAME=k.TABLE_NAME AND r.CONSTRAINT_NAME=k.CONSTRAINT_NAME WHERE k.TABLE_SCHEMA=DATABASE() AND k.REFERENCED_TABLE_NAME IS NOT NULL ORDER BY k.TABLE_NAME,k.CONSTRAINT_NAME,k.ORDINAL_POSITION",
        "indexes":"SELECT TABLE_NAME,INDEX_NAME,SEQ_IN_INDEX,COLUMN_NAME,NON_UNIQUE,INDEX_TYPE,SUB_PART,EXPRESSION FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA=DATABASE() ORDER BY TABLE_NAME,INDEX_NAME,SEQ_IN_INDEX",
        "checks":"SELECT t.TABLE_NAME,t.CONSTRAINT_NAME,t.ENFORCED,c.CHECK_CLAUSE FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS t JOIN INFORMATION_SCHEMA.CHECK_CONSTRAINTS c ON c.CONSTRAINT_SCHEMA=t.CONSTRAINT_SCHEMA AND c.CONSTRAINT_NAME=t.CONSTRAINT_NAME WHERE t.CONSTRAINT_SCHEMA=DATABASE() AND t.CONSTRAINT_TYPE='CHECK' AND NOT(t.TABLE_NAME='PedidosVenta' AND t.CONSTRAINT_NAME='CK_PedidosVenta_Checkout_Atomico') ORDER BY t.TABLE_NAME,t.CONSTRAINT_NAME",
        "triggers":"SELECT TRIGGER_NAME,EVENT_OBJECT_TABLE,EVENT_MANIPULATION,ACTION_TIMING,ACTION_STATEMENT FROM INFORMATION_SCHEMA.TRIGGERS WHERE TRIGGER_SCHEMA=DATABASE() ORDER BY TRIGGER_NAME",
        "tables":"SELECT TABLE_NAME,ENGINE,TABLE_COLLATION FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME"
    }.items():
        result[label]=hashlib.sha256(("\n".join(run(sql,label))+"\n").encode()).hexdigest()
    return result
if sys.argv[1:] == ["verify-restore"]:
    manifest = pathlib.Path(os.environ["RUNNER_TEMP"]) / "solqaryn-qa-oracle-backup" / "qa-rowcounts.json"
    counts = json.loads(manifest.read_text())
    require(names() == sorted(counts) and len(counts) == 138, "Snapshot table set differs from restored copy")
    for table, expected in counts.items():
        require(type(expected) is int and expected >= 0, "Invalid snapshot row count")
        require(int(scalar("SELECT COUNT(*) FROM "+q(table), "restored row count")) == expected,
                "QA_RESTORE_ROWCOUNT_MISMATCH table="+table)
    print("QA_RESTORE_SNAPSHOT_COUNTS=PASS tables=138 rows_exact=YES", flush=True)
    sys.exit(0)

marker="20261128183000_QaCheckoutPedidoV6"
fields={"CostoEnvio","MonedaCheckout","TarifaEnvioCheckout","EntregaCheckoutJson"}
fp=pathlib.Path(os.environ["RUNNER_TEMP"])/"qa-checkout-v6-fingerprints.json"
require(scalar("SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId='20261128163400_QaCotizacionEstadoV5'","V5 marker")=="1","V5 missing")
if sys.argv[1]=="capture":
    require(scalar("SELECT COUNT(*) FROM __EFMigrationsHistory","history")=="114","Baseline history mismatch")
    require(not fields.intersection(columns("PedidosVenta")),"V6 fields already exist")
    require(scalar(f"SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId='{marker}'","V6 marker")=="0","V6 already applied")
    orig={t:columns(t) for t in names()}
    require(len(orig)==138,"Baseline table count mismatch")
    fp.write_text(json.dumps({"columns":orig,"data":{t:fingerprint(t,c) for t,c in orig.items()},"schema":schema()}))
    fp.chmod(0o600)
    print("QA_CHECKOUT_V6_RESTORE_BASELINE=PASS",flush=True)
else:
    orig=json.loads(fp.read_text())
    require(names()==sorted(orig["columns"]),"Table set drift")
    require(scalar("SELECT COUNT(*) FROM __EFMigrationsHistory","history")=="115","Unexpected history")
    require(scalar(f"SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId='{marker}'","V6 marker")=="1","V6 missing")
    require(schema()==orig["schema"],"Unapproved structural drift")
    for t,c in orig["columns"].items():
        actual=columns(t)
        require(([x for x in actual if x not in fields] if t=="PedidosVenta" else actual)==c,"Historical columns changed "+t)
        where=f"WHERE MigrationId<>'{marker}'" if t=="__EFMigrationsHistory" else ""
        require(fingerprint(t,c,where)==orig["data"][t],"Historical data changed "+t)
    actual=run("SELECT COLUMN_NAME,COLUMN_TYPE,IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='PedidosVenta' AND COLUMN_NAME IN ('CostoEnvio','MonedaCheckout','TarifaEnvioCheckout','EntregaCheckoutJson')","new columns")
    require(set(actual)=={"CostoEnvio\tdecimal(18,2)\tYES","MonedaCheckout\tvarchar(3)\tYES","TarifaEnvioCheckout\tvarchar(120)\tYES","EntregaCheckoutJson\tlongtext\tYES"},"V6 fields differ")
    check=scalar("SELECT CONCAT(t.ENFORCED,':',c.CHECK_CLAUSE) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS t JOIN INFORMATION_SCHEMA.CHECK_CONSTRAINTS c ON c.CONSTRAINT_SCHEMA=t.CONSTRAINT_SCHEMA AND c.CONSTRAINT_NAME=t.CONSTRAINT_NAME WHERE t.CONSTRAINT_SCHEMA=DATABASE() AND t.TABLE_NAME='PedidosVenta' AND t.CONSTRAINT_NAME='CK_PedidosVenta_Checkout_Atomico'","checkout constraint")
    normalize=lambda x: re.sub(r"[\s`()]","",x).lower()
    require(normalize(check)==normalize("YES:((`CostoEnvio` IS NULL AND `MonedaCheckout` IS NULL AND `TarifaEnvioCheckout` IS NULL AND `EntregaCheckoutJson` IS NULL) OR (`CostoEnvio` IS NOT NULL AND `CostoEnvio` >= 0 AND `MonedaCheckout` IS NOT NULL AND CHAR_LENGTH(`MonedaCheckout`) = 3 AND `TarifaEnvioCheckout` IS NOT NULL AND `EntregaCheckoutJson` IS NOT NULL AND `CotizacionId` IS NULL))"),"Constraint differs")
    require(scalar("SELECT COUNT(*) FROM PedidosVenta WHERE CostoEnvio IS NOT NULL OR MonedaCheckout IS NOT NULL OR TarifaEnvioCheckout IS NOT NULL OR EntregaCheckoutJson IS NOT NULL","historical checkout values")=="0","Historical orders changed")
    require(int(scalar("SELECT COUNT(*) FROM PedidosVenta","order fixture"))>0,"No historical order fixture")
    require(run("START TRANSACTION; UPDATE PedidosVenta SET CotizacionId=NULL,CostoEnvio=7.25,MonedaCheckout='HNL',TarifaEnvioCheckout='QA',EntregaCheckoutJson='{}' LIMIT 1; SELECT ROW_COUNT(); ROLLBACK;","valid snapshot")==["1"],"Valid checkout snapshot not accepted")
    run("START TRANSACTION; UPDATE PedidosVenta SET CostoEnvio=7.25 LIMIT 1; ROLLBACK;","partial snapshot",expected=(3819,))
    run("START TRANSACTION; UPDATE PedidosVenta SET CotizacionId=NULL,CostoEnvio=-1,MonedaCheckout='HNL',TarifaEnvioCheckout='QA',EntregaCheckoutJson='{}' LIMIT 1; ROLLBACK;","negative shipping",expected=(3819,))
    require(fingerprint("PedidosVenta",orig["columns"]["PedidosVenta"])==orig["data"]["PedidosVenta"],"Rehearsal did not roll back")
    print("QA_CHECKOUT_V6_EXACT_REHEARSAL=PASS history=115 historical_data=preserved iam=preserved other_constraints=preserved checkout=verified",flush=True)
    print("QA_ORIGINAL_DB_WRITES=0",flush=True)
