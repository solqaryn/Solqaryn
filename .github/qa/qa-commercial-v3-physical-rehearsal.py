"""V3 physical candidate. Only for disposable loopback MySQL, never live."""
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
require(sys.argv[1:]==["--local-rehearsal"],"Explicit local-only target required")
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
require(scalar("SELECT COUNT(*) FROM __EFMigrationsHistory","history")=="111","V2 history mismatch")
m=json.loads(pathlib.Path(".github/qa/qa-commercial-v3-physical-manifest.json").read_text())
tables=m["tables"]; edges=m["edges"]; unique=m["unique"]; extra=m["extra"]
require(len(set(tables))==len(tables)==26 and len(unique)==11 and len(edges)==48 and len(m["originalDeleteRules"])==len(edges),"Manifest mismatch")
affected=sorted(set(tables)|{x[0] for x in edges}|{x[2] for x in edges}|{"CategoriasGalerias","Roles","Usuarios","Permisos","RolPermisos","UsuarioEmpresas","UsuarioRolesPlataforma","Empresas"})
def columns(t):
    return run(f"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='{t}' ORDER BY ORDINAL_POSITION","column manifest")
original={t:columns(t) for t in affected}
require(all(original.values()),"Missing affected table")
require(all("EmpresaId" not in original[t] and "Id" in original[t] for t in tables),"Unexpected ownership baseline")
def digest(t,c):
    projection=", ".join(f"IF({q(x)} IS NULL,'N',CONCAT('V',HEX(CAST({q(x)} AS BINARY))))" for x in c)
    rows=run(f"SELECT {projection} FROM {q(t)}","preserve "+t)
    return hashlib.sha256(("\n".join(sorted(rows))+"\n").encode()).hexdigest()
before={t:digest(t,c) for t,c in original.items()}
def rowcounts():
    ts=run("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME","table list")
    return {t:scalar(f"SELECT COUNT(*) FROM {q(t)}","row count") for t in ts}
counts=rowcounts()
def fk_rows():
    return run("SELECT CONCAT(k.TABLE_NAME,'|',k.CONSTRAINT_NAME,'|',k.COLUMN_NAME,'|',k.REFERENCED_TABLE_NAME,'|',k.REFERENCED_COLUMN_NAME,'|',r.DELETE_RULE,'|',r.UPDATE_RULE) FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE k JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS r ON r.CONSTRAINT_SCHEMA=k.CONSTRAINT_SCHEMA AND r.TABLE_NAME=k.TABLE_NAME AND r.CONSTRAINT_NAME=k.CONSTRAINT_NAME WHERE k.TABLE_SCHEMA=DATABASE() AND k.REFERENCED_TABLE_NAME IS NOT NULL ORDER BY k.TABLE_NAME,k.CONSTRAINT_NAME,k.ORDINAL_POSITION","original FK topology and actions")
original_fks=set(fk_rows())
def fk_rule(child, cc, parent, pp):
    pairs=set(zip(cc[1:],pp[1:]))
    rows=run(f"SELECT k.CONSTRAINT_NAME,k.COLUMN_NAME,k.REFERENCED_COLUMN_NAME,r.DELETE_RULE FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE k JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS r ON r.CONSTRAINT_SCHEMA=k.CONSTRAINT_SCHEMA AND r.CONSTRAINT_NAME=k.CONSTRAINT_NAME AND r.TABLE_NAME=k.TABLE_NAME WHERE k.TABLE_SCHEMA=DATABASE() AND k.TABLE_NAME='{child}' AND k.REFERENCED_TABLE_NAME='{parent}' ORDER BY k.CONSTRAINT_NAME,k.ORDINAL_POSITION","existing relationship")
    matches={}
    for row in rows:
        fk,c,p,rule=row.split("\t")
        if fk.startswith(("FK_QACT_","FK_QAC2_","FK_QAC3_")): continue
        matches.setdefault(fk,[set(),rule])[0].add((c,p))
    rules={("RESTRICT" if v[1]=="NO ACTION" else v[1]) for v in matches.values() if v[0]==pairs}
    return next(iter(rules)) if len(rules)==1 else "MISSING_OR_AMBIGUOUS"

# Review every existing delete action before any DDL. Never turn a composite
# tenant FK into SET NULL: that would also null EmpresaId.
observed=[fk_rule(*edge) for edge in edges]
expected=m["originalDeleteRules"]
drift=[f"{i+1}:{edge[0]}->{edge[2]} expected={want} actual={got}" for i,(edge,want,got) in enumerate(zip(edges,expected,observed)) if got!=want]
require(not drift,"Original FK rule drift: "+"; ".join(drift))
rules=[]
for (child,cc,parent,pp),rule in zip(edges,observed):
    require(rule in ("CASCADE","RESTRICT","SET NULL"),"Unreviewed FK rule")
    if rule=="SET NULL":
        require(child=="Productos" and len(cc)==2 and parent in ("Categorias","Marcas","Modelos","Colores","Tallas"),"Unreviewed nullable catalog edge")
        require(scalar(f"SELECT IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='{child}' AND COLUMN_NAME='{cc[1]}'","nullable original reference")=="YES","SET NULL source is not optional")
        rules.append("RESTRICT")
    else:
        rules.append(rule)
require(observed.count("SET NULL")==5 and rules.count("SET NULL")==0,"Tenant FK delete policy mismatch")
print(f"QA_V3_DELETE_RULES=PASS reviewed={len(rules)} legacy_set_null=5 scoped_set_null=0",flush=True)
for t in tables:
    run(f"ALTER TABLE {q(t)} ADD COLUMN EmpresaId int NULL, ADD UNIQUE INDEX {q('UX_QAC3_'+t+'_OwnerId')} (EmpresaId,Id), ADD CONSTRAINT {q('FK_QAC3_'+t+'_Empresa')} FOREIGN KEY (EmpresaId) REFERENCES Empresas (Id) ON DELETE RESTRICT","owner "+t)
for t,ix,cc in extra:
    run(f"ALTER TABLE {q(t)} ADD UNIQUE INDEX {q(ix)} ({cols(cc)})","additional principal key "+t)
for t in sorted({x[0] for x in unique}):
    run(f"ALTER TABLE {q(t)} ADD COLUMN EmpresaUnicidad int GENERATED ALWAYS AS (COALESCE(EmpresaId,0)) STORED NOT NULL","scoped uniqueness "+t)
for t,ix,c in unique:
    old=run(f"SELECT COLUMN_NAME,NON_UNIQUE FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='{t}' AND INDEX_NAME='{ix}' ORDER BY SEQ_IN_INDEX","original natural key")
    require(old==[c+"\t0"],"Natural key drift: "+t)
    run(f"ALTER TABLE {q(t)} DROP INDEX {q(ix)}, ADD UNIQUE INDEX {q(ix)} (EmpresaUnicidad,{q(c)})","replace natural key "+t)
for i,((child,cc,parent,pp),rule) in enumerate(zip(edges,rules),1):
    join=" AND ".join(f"c.{q(c)}=p.{q(p)}" for c,p in zip(cc,pp))
    notnull=" AND ".join(f"c.{q(c)} IS NOT NULL" for c in cc)
    orphan=scalar(f"SELECT COUNT(*) FROM {q(child)} c LEFT JOIN {q(parent)} p ON {join} WHERE {notnull} AND p.Id IS NULL","owner compatibility")
    require(orphan=="0","Existing owned reference unresolved: "+child+" -> "+parent)
    run(f"ALTER TABLE {q(child)} ADD INDEX IX_QAC3_{i:02d} ({cols(cc)}), ADD CONSTRAINT FK_QAC3_{i:02d} FOREIGN KEY ({cols(cc)}) REFERENCES {q(parent)} ({cols(pp)}) ON DELETE {rule}","scoped FK "+child+" -> "+parent)
# Preserve every legacy gallery row. The new reference is nullable only for
# pre-existing quarantined data; new writes must present a verified link.
run("ALTER TABLE CategoriasGalerias ADD COLUMN CategoriaVerificadaId int NULL, ADD INDEX IX_QAC3_Galeria_Verificada (EmpresaId,CategoriaVerificadaId), ADD CONSTRAINT FK_QAC3_Galeria_Verificada FOREIGN KEY (EmpresaId,CategoriaVerificadaId) REFERENCES Categorias (EmpresaId,Id) ON DELETE RESTRICT","gallery composite reference")
for action,short in (("INSERT","BI"),("UPDATE","BU")):
    run(f"""DELIMITER $$
CREATE TRIGGER TR_QAC3_Galeria_{short} BEFORE {action} ON CategoriasGalerias FOR EACH ROW
BEGIN
 IF NEW.CategoriaVerificadaId IS NULL OR NEW.CategoriaVerificadaId <> NEW.CategoriaId THEN
 SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Verified tenant category required';
 END IF;
END$$
DELIMITER ;
""","gallery guard "+action)
require(scalar("SELECT COUNT(*) FROM CategoriasGalerias WHERE CategoriaVerificadaId IS NOT NULL","legacy gallery quarantine")=="0","Unexpected inferred gallery owner")
for t in tables:
    require(scalar(f"SELECT COUNT(*) FROM {q(t)} WHERE EmpresaId IS NOT NULL","legacy owner preservation")=="0","Historical owner inferred")
    require(scalar(f"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='{t}' AND COLUMN_NAME='EmpresaId' AND IS_NULLABLE='YES' AND DATA_TYPE='int'","owner metadata")=="1","Owner column mismatch")
require(original_fks <= set(fk_rows()),"Original foreign keys changed")
for i,(child,cc,parent,pp) in enumerate(edges,1):
    actual=run(f"SELECT COLUMN_NAME,REFERENCED_TABLE_NAME,REFERENCED_COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='{child}' AND CONSTRAINT_NAME='FK_QAC3_{i:02d}' ORDER BY ORDINAL_POSITION","verify composite FK")
    require(actual==[f"{c}\t{parent}\t{p}" for c,p in zip(cc,pp)],"Composite FK mismatch")
    actual_rule=scalar(f"SELECT DELETE_RULE FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND TABLE_NAME='{child}' AND CONSTRAINT_NAME='FK_QAC3_{i:02d}'","verify composite delete action")
    require(actual_rule==rules[i-1] or (rules[i-1]=="RESTRICT" and actual_rule=="NO ACTION"),"Scoped FK delete rule mismatch")
require(rowcounts()==counts,"Historical row count changed")
require(all(digest(t,c)==before[t] for t,c in original.items()),"Historical value or IAM changed")
print("QA_V3_REHEARSAL_PRESERVED=PASS",flush=True)
# Real SQL negative/positive tests on the disposable copy only.
prefix="qa3"+secrets.token_hex(6)
ids=run(f"""START TRANSACTION;
INSERT INTO Empresas (Nombre,Activa,FechaCreacion,FechaActualizacion) VALUES ('{prefix}A',1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @ea=LAST_INSERT_ID();
INSERT INTO Empresas (Nombre,Activa,FechaCreacion,FechaActualizacion) VALUES ('{prefix}B',1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @eb=LAST_INSERT_ID();
INSERT INTO Categorias (EmpresaId,Nombre,Activa,Eliminada,FechaCreacion,FechaActualizacion) VALUES (@ea,'{prefix}',1,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @ca=LAST_INSERT_ID();
INSERT INTO Categorias (EmpresaId,Nombre,Activa,Eliminada,FechaCreacion,FechaActualizacion) VALUES (@eb,'{prefix}',1,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SET @cb=LAST_INSERT_ID();
COMMIT;
SELECT @ea,@eb,@ca,@cb;""","synthetic A/B")[0].split("\t")
require(len(ids)==4 and all(x.isdigit() and int(x)>0 for x in ids),"Fixture IDs invalid")
ea,eb,ca,cb=map(int,ids)

# The original optional category FK must remain nullable; the new tenant FK
# must reject cross-company references without ever changing the product owner.
pid=int(scalar(f"""INSERT INTO Productos (EmpresaId,Nombre,Marca,Modelo,CategoriaId,TipoInventario,Cantidad,Costo,Precio,UmbralStockBajo,Activo,EsDestacado,Eliminado,FechaCreacion,FechaActualizacion)
VALUES ({ea},'{prefix}-product','','',{ca},1,1,5,10,1,1,0,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));
SELECT LAST_INSERT_ID();""","product tenant fixture"))
require(pid>0,"Missing synthetic product ID")
run(f"UPDATE Productos SET CategoriaId={cb} WHERE Id={pid} AND EmpresaId={ea}","cross-company product category",expected={1452})
require(scalar(f"SELECT COUNT(*) FROM Productos WHERE Id={pid} AND EmpresaId={ea} AND CategoriaId={ca}","product owner after rejection")=="1","Rejected update changed product owner")
run(f"UPDATE Categorias SET Activa=0,Eliminada=1 WHERE Id={ca} AND EmpresaId={ea}","logical category deletion")
require(scalar(f"SELECT COUNT(*) FROM Productos WHERE Id={pid} AND EmpresaId={ea} AND CategoriaId={ca}","owner after soft deletion")=="1","Logical deletion changed product owner")
run(f"UPDATE Categorias SET Activa=1,Eliminada=0 WHERE Id={ca} AND EmpresaId={ea}","restore synthetic category")
run(f"UPDATE Productos SET CategoriaId=NULL WHERE Id={pid} AND EmpresaId={ea}","explicit optional detachment")
require(scalar(f"SELECT COUNT(*) FROM Productos WHERE Id={pid} AND EmpresaId={ea} AND CategoriaId IS NULL","owner after detachment")=="1","Optional detachment removed owner")
run(f"DELETE FROM Productos WHERE Id={pid} AND EmpresaId={ea} AND Nombre='{prefix}-product'","remove synthetic product")
print("QA_V3_OPTIONAL_CATEGORY_OWNER_PRESERVED=PASS",flush=True)
def insert_gallery(e,c,v):
    return f"INSERT INTO CategoriasGalerias (EmpresaId,CategoriaId,CategoriaVerificadaId,Version,ImagenesJson) VALUES ({e},{c},{v},'qa3fixture','[]');"
run(insert_gallery(ea,ca,ca),"same-company gallery")
run(insert_gallery(eb,ca,ca),"cross-company gallery",expected={1452})
run(insert_gallery(eb,cb,"NULL"),"unverified new gallery",expected={1644})
run(insert_gallery(eb,cb,ca),"mismatched verified gallery",expected={1644})
for e in (ea,eb):
    run(f"INSERT INTO Descuentos (EmpresaId,Nombre,CodigoPromocional,CodigoPromocionalNormalizado,Tipo,Valor,RequiereAprobacion,Acumulable,Prioridad,UsosRealizados,Activo,Eliminado,FechaCreacion) VALUES ({e},'{prefix}','{prefix}','{prefix.upper()}',1,5,0,0,100,0,1,0,UTC_TIMESTAMP(6));","scoped promotion code")
    run(f"INSERT INTO Impuestos (EmpresaId,Nombre,Codigo,Tipo,Tasa,IncluidoEnPrecio,SeCalculaAntesDescuento,Acumulativo,Prioridad,RequiereRetencion,Activo,Eliminado,FechaCreacion) VALUES ({e},'{prefix}','{prefix}',1,15,0,0,1,100,0,1,0,UTC_TIMESTAMP(6));","scoped tax code")
da=int(scalar(f"SELECT Id FROM Descuentos WHERE EmpresaId={ea} AND Nombre='{prefix}'","discount A"))
ia=int(scalar(f"SELECT Id FROM Impuestos WHERE EmpresaId={ea} AND Nombre='{prefix}'","tax A"))
run(f"INSERT INTO DescuentoCategorias (EmpresaId,DescuentoId,CategoriaId) VALUES ({ea},{da},{ca});","same-company discount")
run(f"INSERT INTO DescuentoCategorias (EmpresaId,DescuentoId,CategoriaId) VALUES ({ea},{da},{cb});","cross-company discount category",expected={1452})
run(f"INSERT INTO ImpuestoCategorias (EmpresaId,ImpuestoId,CategoriaId) VALUES ({ea},{ia},{ca});","same-company tax")
run(f"INSERT INTO ImpuestoCategorias (EmpresaId,ImpuestoId,CategoriaId) VALUES ({ea},{ia},{cb});","cross-company tax category",expected={1452})
run(f"""START TRANSACTION;
DELETE FROM DescuentoCategorias WHERE EmpresaId IN ({ea},{eb});
DELETE FROM ImpuestoCategorias WHERE EmpresaId IN ({ea},{eb});
DELETE FROM Descuentos WHERE EmpresaId IN ({ea},{eb}) AND Nombre='{prefix}';
DELETE FROM Impuestos WHERE EmpresaId IN ({ea},{eb}) AND Nombre='{prefix}';
DELETE FROM CategoriasGalerias WHERE EmpresaId IN ({ea},{eb});
DELETE FROM Categorias WHERE EmpresaId IN ({ea},{eb}) AND Nombre='{prefix}';
DELETE FROM Empresas WHERE Id IN ({ea},{eb}) AND Nombre IN ('{prefix}A','{prefix}B');
COMMIT;""","local fixture cleanup")
require(rowcounts()==counts and all(digest(t,c)==before[t] for t,c in original.items()),"Local cleanup or preservation failed")
print(f"QA_V3_PHYSICAL_REHEARSAL=PASS owner_tables={len(tables)} gallery=1 scoped_edges={len(edges)+1} natural_keys={len(unique)} A_B_GALLERY_DISCOUNT_TAX=PASS",flush=True)
print("QA_ORIGINAL_DB_WRITES=0",flush=True)
print("QA_V3_APPLICATION_CERTIFICATION=NOT_ESTABLISHED",flush=True)
