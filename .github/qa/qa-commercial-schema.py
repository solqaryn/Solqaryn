"""Read-only exact QA ownership schema certification; no row values or credentials."""
import hashlib
import os
import pathlib
import re
import subprocess
import sys

def require(ok, reason):
    if not ok:
        raise RuntimeError(reason)

require(os.environ.get("CI") == "true" and os.environ.get("GITHUB_REF") == "refs/heads/qa", "QA only")
require(len(sys.argv) == 2 and sys.argv[1] in ("--live", "--local"), "Explicit read-only target required")
live = sys.argv[1] == "--live"
if live:
    require(os.environ.get("DB_HOST") == "solqaryn-mysql-solqaryn.h.aivencloud.com" and
            os.environ.get("DB_PORT") == "14402" and os.environ.get("DB_NAME") == "solqaryn_qa" and
            os.environ.get("DB_USER") == "solqaryn_qa_user", "QA binding mismatch")
    host, port, user, database = (os.environ[k] for k in ("DB_HOST", "DB_PORT", "DB_USER", "DB_NAME"))
    password = os.environ["DB_PASSWORD"]
    tls = ["--ssl-mode=REQUIRED"]
else:
    host, port, user, database = "127.0.0.1", "3306", "root", "solqaryn_qa_restore_ephemeral"
    password = os.environ["LOCAL_MYSQL_PASSWORD"]
    tls = []
require(bool(password), "Missing database authentication")
args = ["mysql", "--protocol=TCP", "--connect-timeout=12", *tls,
        "-h", host, "-P", port, "-u", user, "--batch", "--raw", "--skip-column-names", database]
env = dict(os.environ, MYSQL_PWD=password)
def query(sql):
    r = subprocess.run(args, input=sql, text=True, capture_output=True, env=env, timeout=60)
    require(r.returncode == 0, "Read-only metadata query failed")
    return r.stdout.splitlines()
def scalar(sql):
    rows = query(sql)
    require(len(rows) == 1, "Expected one metadata row")
    return rows[0]
require(scalar("SELECT DATABASE()") == database, "Database identity mismatch")
require(scalar("SELECT SUBSTRING_INDEX(CURRENT_USER(),'@',1)") == user, "Database user mismatch")
version = scalar("SELECT VERSION()")
require(version.startswith("8.4.11" if live else "8.4."), "MySQL patch mismatch")

# The immutable C# migration contract is the authority; parse only its literal
# descriptors, not arbitrary SQL or external configuration.
source = pathlib.Path("backend/src/Infrastructure/Persistence/QaCommercialOwnershipV1.cs").read_text()
tables = re.findall(r'new\(typeof\(\w+\), "([A-Za-z0-9_]+)"\)', source)
natural = re.findall(r'new\("([A-Za-z0-9_]+)", "([A-Za-z0-9_]+)", "([A-Za-z0-9_]+)"\)', source)
pattern = r'new\("([A-Za-z0-9_]+)", new\[\] \{ ([^}]+) \}, "([A-Za-z0-9_]+)", new\[\] \{ ([^}]+) \}, "([A-Za-z0-9_]+)", "([A-Za-z0-9_]+)", (true|false)\)'
edges = [(ch, re.findall(r'"(\w+)"', cols), par, re.findall(r'"(\w+)"', pcols), ix, fk, cascade == "true")
         for ch, cols, par, pcols, ix, fk, cascade in re.findall(pattern, source)]
require(len(tables) == len(set(tables)) == 27 and len(edges) == 48 and len(natural) == 15, "Migration manifest mismatch")
marker = "20261126170000_QaCommercialOwnershipV1"
require(scalar(f"SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId='{marker}'") == "1", "Ownership migration not recorded")
require(scalar("SELECT COUNT(*) FROM __EFMigrationsHistory") == "110", "Unexpected migration history")
require(scalar("SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId IN ('20261006111818_OracleBaseline','20261125212000_CategoriasGalerias')") == "2", "Baseline marker missing")

def digest(rows):
    return hashlib.sha256(("\n".join(rows) + "\n").encode()).hexdigest()
expected_tables = "36b725a682b43d744509c8fd27a6ae54b6017d1c46e1301ee796f86f760e7489"
expected_fks = "79f67fec9b5761f66cf93dbbcab3a7e7cfd769e132019e720e51d0317ef93b3c"
expected_indexes = "3f91e8d03e2700329472bb3ee678c0f3d680495217d4cb4da48291351b3b90ec"
base_tables = query("SELECT CONCAT(TABLE_NAME,'|',ENGINE,'|',COALESCE(TABLE_COLLATION,'')) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_TYPE='BASE TABLE' AND TABLE_NAME<>'CategoriasGalerias' ORDER BY TABLE_NAME")
require(digest(base_tables) == expected_tables, "Baseline table topology changed")
new_fks = {(t, "FK_QACT_"+t+"_Empresa") for t in tables} | {(e[0], e[5]) for e in edges}
new_indexes = {(t, "UX_QACT_"+t+"_OwnerId") for t in tables} | {(e[0], e[4]) for e in edges} | {("ProductoVariantes","UX_QACT_Variante_Owner_Product_Id")}
natural_indexes = {(t, ix): col for t, col, ix in natural}
fk_rows = query("SELECT CONCAT(TABLE_NAME,'|',CONSTRAINT_NAME,'|',REFERENCED_TABLE_NAME,'|',UNIQUE_CONSTRAINT_NAME) FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND TABLE_NAME<>'CategoriasGalerias' ORDER BY TABLE_NAME,CONSTRAINT_NAME")
filtered_fks = [r for r in fk_rows if tuple(r.split("|")[:2]) not in new_fks]
require(digest(filtered_fks) == expected_fks, "Unapproved FK topology delta")
idx_rows = query("SELECT CONCAT(TABLE_NAME,'|',INDEX_NAME,'|',SEQ_IN_INDEX,'|',COALESCE(COLUMN_NAME,''),'|',NON_UNIQUE,'|',INDEX_TYPE) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME<>'CategoriasGalerias' ORDER BY TABLE_NAME,INDEX_NAME,SEQ_IN_INDEX")
normalized = []
for row in idx_rows:
    t, ix, ordinal, col, nonunique, kind = row.split("|")
    if (t, ix) in new_indexes:
        continue
    if (t, ix) in natural_indexes:
        if ordinal == "1":
            require(col == "EmpresaUnicidad" and nonunique == "0", "Scoped uniqueness prefix mismatch")
            continue
        require(ordinal == "2" and col == natural_indexes[(t, ix)] and nonunique == "0", "Scoped uniqueness key mismatch")
        ordinal = "1"
    normalized.append("|".join((t, ix, ordinal, col, nonunique, kind)))
require(digest(normalized) == expected_indexes, "Unapproved index topology delta")

def index(t, ix, cols, unique):
    result = query(f"SELECT CONCAT(SEQ_IN_INDEX,':',COLUMN_NAME,':',NON_UNIQUE) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='{t}' AND INDEX_NAME='{ix}' ORDER BY SEQ_IN_INDEX")
    require(result == [f"{i+1}:{col}:{0 if unique else 1}" for i,col in enumerate(cols)], "Index mismatch: "+t+"."+ix)
def fk(t, name, cols, parent, pcols, rule):
    result = query(f"SELECT CONCAT(k.ORDINAL_POSITION,':',k.COLUMN_NAME,':',k.REFERENCED_TABLE_NAME,':',k.REFERENCED_COLUMN_NAME,':',r.DELETE_RULE) FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE k JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS r ON r.CONSTRAINT_SCHEMA=k.CONSTRAINT_SCHEMA AND r.TABLE_NAME=k.TABLE_NAME AND r.CONSTRAINT_NAME=k.CONSTRAINT_NAME WHERE k.TABLE_SCHEMA=DATABASE() AND k.TABLE_NAME='{t}' AND k.CONSTRAINT_NAME='{name}' ORDER BY k.ORDINAL_POSITION")
    require(result == [f"{i+1}:{col}:{parent}:{pcols[i]}:{rule}" for i,col in enumerate(cols)], "FK mismatch: "+t+"."+name)
for table in tables:
    require(scalar(f"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='{table}' AND COLUMN_NAME='EmpresaId' AND DATA_TYPE='int' AND IS_NULLABLE='YES'") == "1", "Owner column mismatch: "+table)
    index(table, "UX_QACT_"+table+"_OwnerId", ["EmpresaId","Id"], True)
    fk(table, "FK_QACT_"+table+"_Empresa", ["EmpresaId"], "Empresas", ["Id"], "RESTRICT")
index("ProductoVariantes","UX_QACT_Variante_Owner_Product_Id", ["EmpresaId","ProductoId","Id"], True)
for ch, cols, par, pcols, ix, name, cascade in edges:
    index(ch, ix, cols, False)
    fk(ch, name, cols, par, pcols, "CASCADE" if cascade else "RESTRICT")
for table,col,ix in natural:
    require(scalar(f"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='{table}' AND COLUMN_NAME='EmpresaUnicidad' AND DATA_TYPE='int' AND IS_NULLABLE='NO' AND EXTRA LIKE '%STORED GENERATED%' AND LOWER(REPLACE(REPLACE(GENERATION_EXPRESSION,'`',''),' ',''))='coalesce(empresaid,0)'") == "1", "Generated tenant key missing")
    index(table,ix,["EmpresaUnicidad",col],True)

# Preserve the previous gallery and typed-inventario gates exactly.
require(query("SELECT CONCAT(COLUMN_NAME,':',COLUMN_TYPE,':',IS_NULLABLE) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='CategoriasGalerias' ORDER BY COLUMN_NAME") == ["CategoriaId:int:NO","EmpresaId:int:NO","ImagenesJson:longtext:NO","Version:varchar(32):NO"], "Gallery columns mismatch")
require(query("SELECT CONCAT(INDEX_NAME,':',SEQ_IN_INDEX,':',COLUMN_NAME,':',NON_UNIQUE) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='CategoriasGalerias' ORDER BY INDEX_NAME,SEQ_IN_INDEX") == ["IX_CategoriasGalerias_CategoriaId:1:CategoriaId:1","PRIMARY:1:EmpresaId:0","PRIMARY:2:CategoriaId:0"], "Gallery indexes mismatch")
require(query("SELECT CONCAT(k.COLUMN_NAME,':',k.REFERENCED_TABLE_NAME,':',k.REFERENCED_COLUMN_NAME,':',r.DELETE_RULE) FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE k JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS r ON r.CONSTRAINT_SCHEMA=k.CONSTRAINT_SCHEMA AND r.CONSTRAINT_NAME=k.CONSTRAINT_NAME AND r.TABLE_NAME=k.TABLE_NAME WHERE k.TABLE_SCHEMA=DATABASE() AND k.TABLE_NAME='CategoriasGalerias' ORDER BY k.COLUMN_NAME") == ["CategoriaId:Categorias:Id:RESTRICT","EmpresaId:Empresas:Id:RESTRICT"], "Gallery FKs mismatch")
require(scalar("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TRIGGERS WHERE TRIGGER_SCHEMA=DATABASE() AND TRIGGER_NAME IN ('TR_MovimientosInventario_N06_OrigenTipado_BI','TR_MovimientosInventario_N06_OrigenTipado_BU')") == "2", "inventario triggers missing")
require(scalar("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND TABLE_NAME='MovimientosInventario' AND CONSTRAINT_TYPE='CHECK' AND CONSTRAINT_NAME='CK_MovimientosInventario_OrigenTipado_Exclusivo_N06'") == "1", "inventario CHECK missing")
print("QA_COMMERCIAL_SCHEMA_EXACT=PASS tables=27 composite_fks=48 natural_keys=15 baseline_preserved=YES")
print("QA_DATABASE_WRITES=0")
