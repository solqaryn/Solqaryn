#!/usr/bin/env bash
set -euo pipefail
# C12-04.M06.04 - GitHub-hosted synthetic-only MySQL 8.4 restore, never Aiven.
[[ "$#" -eq 1 && -n "$1" ]] || { echo "MYSQL_SERVICE_ID_REQUIRED" >&2; exit 64; }
cid="$1"
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT
sql(){ docker exec -e MYSQL_PWD=root "$cid" mysql -uroot -Nse "$1"; }
version="$(sql 'SELECT VERSION();')"
[[ "$version" =~ ^8\.4\. ]] || { echo "MYSQL_VERSION_NOT_8_4:$version" >&2; exit 1; }
sql "CREATE DATABASE c1204_source; CREATE DATABASE c1204_restored; CREATE DATABASE c1204_corrupt;
CREATE TABLE c1204_source.evidencia (id INT PRIMARY KEY, empresa_id INT NOT NULL, code VARCHAR(40) NOT NULL, amount DECIMAL(12,2) NOT NULL, UNIQUE KEY uq_empresa_code(empresa_id,code)) ENGINE=InnoDB;
INSERT INTO c1204_source.evidencia VALUES (1,11,'A01',100.00),(2,22,'B01',200.00),(3,11,'A02',50.00);"
fingerprint(){ sql "SELECT CONCAT(COUNT(*),'|',COALESCE(SUM(amount),0),'|',SHA2(GROUP_CONCAT(CONCAT(empresa_id,':',code,':',amount) ORDER BY id SEPARATOR ';'),256)) FROM $1.evidencia;"; }
before="$(fingerprint c1204_source)"
[[ "$before" == 3\|350.00\|* ]] || { echo "BAD_FIXTURE" >&2; exit 1; }
tenants="$(sql "SELECT CONCAT(SUM(empresa_id=11),'|',SUM(empresa_id=22)) FROM c1204_source.evidencia;")"
[[ "$tenants" == "2|1" ]] || exit 1
docker exec -e MYSQL_PWD=root "$cid" mysqldump -uroot --single-transaction --skip-lock-tables --no-tablespaces --skip-comments c1204_source > "$tmp/backup.sql"
[[ -s "$tmp/backup.sql" ]] || { echo "EMPTY_BACKUP" >&2; exit 1; }
backup_hash="$(sha256sum "$tmp/backup.sql" | awk '{print $1}')"
restore_start_ns="$(date +%s%N)"
docker exec -i -e MYSQL_PWD=root "$cid" mysql -uroot c1204_restored < "$tmp/backup.sql"
restore_ms="$(( ($(date +%s%N) - restore_start_ns) / 1000000 ))"
[[ "$(sha256sum "$tmp/backup.sql" | awk '{print $1}')" == "$backup_hash" ]] || { echo "BACKUP_CHANGED_DURING_RESTORE" >&2; exit 1; }
[[ "$(fingerprint c1204_restored)" == "$before" ]] || { echo "RESTORE_CHECKSUM_FAIL" >&2; exit 1; }
[[ "$(sql "SELECT CONCAT(SUM(empresa_id=11),'|',SUM(empresa_id=22)) FROM c1204_restored.evidencia;")" == "$tenants" ]] || exit 1
tamper="$(sql "START TRANSACTION;
UPDATE c1204_restored.evidencia SET amount=amount+999 WHERE empresa_id=22;
SELECT CONCAT(COUNT(*),'|',SUM(amount),'|',SHA2(GROUP_CONCAT(CONCAT(empresa_id,':',code,':',amount) ORDER BY id SEPARATOR ';'),256)) FROM c1204_restored.evidencia;
ROLLBACK;
SELECT CONCAT(COUNT(*),'|',SUM(amount),'|',SHA2(GROUP_CONCAT(CONCAT(empresa_id,':',code,':',amount) ORDER BY id SEPARATOR ';'),256)) FROM c1204_restored.evidencia;")"
[[ "$(printf '%s\n' "$tamper" | head -n 1)" != "$before" ]] || exit 1
[[ "$(printf '%s\n' "$tamper" | tail -n 1)" == "$before" ]] || { echo "TX_ROLLBACK_FAIL" >&2; exit 1; }
cp "$tmp/backup.sql" "$tmp/invalid.sql"
printf '\nTHIS IS INVALID MYSQL SYNTAX;\n' >> "$tmp/invalid.sql"
if docker exec -i -e MYSQL_PWD=root "$cid" mysql -uroot c1204_corrupt < "$tmp/invalid.sql" > "$tmp/invalid.log" 2>&1; then
 echo "INVALID_BACKUP_ACCEPTED" >&2; exit 1
fi
[[ "$(fingerprint c1204_source)" == "$before" && "$(fingerprint c1204_restored)" == "$before" ]] || exit 1
# Malformed SQL imports are not atomic; discard their disposable target.
sql "DROP DATABASE c1204_corrupt;"
remaining="$(sql "SELECT COUNT(*) FROM information_schema.SCHEMATA WHERE SCHEMA_NAME='c1204_corrupt';")"
[[ "$remaining" == "0" ]] || { echo "INVALID_RESTORE_TARGET_NOT_CLEANED" >&2; exit 1; }
if sql "INSERT INTO c1204_restored.evidencia VALUES (4,11,'A01',999.00);" >/dev/null 2>&1; then
 echo "TENANT_DUPLICATE_ACCEPTED" >&2; exit 1
fi
[[ "$(fingerprint c1204_restored)" == "$before" ]] || { echo "NEGATIVE_CASE_CHANGED_RESTORED_DATA" >&2; exit 1; }
echo "MYSQL84_SYNTHETIC_RESTORE_PASS=TRUE ROWS=3 TENANT_A=2 TENANT_B=1 CHECKSUM_EQUAL=TRUE ROLLBACK=PASS INVALID_SQL=REJECTED CORRUPT_TARGET_CLEANUP=PASS DUPLICATE_TENANT_CODE=REJECTED RESTORE_SYNTHETIC_MS=$restore_ms"
echo "LIVE_AIVEN=NOT_USED ORACLE_EF_MIGRATIONS=NOT_TESTED PROD_RPO_RTO=NOT_MEASURED PROD=NOT_TOUCHED"
