#!/usr/bin/env bash
set -euo pipefail
# Only a disposable GitHub-hosted MySQL container is changed.
test "$GITHUB_REPOSITORY" = "solqaryn/Solqaryn"
test "$GITHUB_REF" = "refs/heads/dev"
password="SQV22_Synthetic_MySQL_Only_20261119"
container="sqv22-c0304-m0604-$RANDOM"
cleanup() { docker rm -f "$container" >/dev/null 2>&1 || true; }
trap cleanup EXIT
docker run -d --rm --name "$container" -e MYSQL_ROOT_PASSWORD="$password" mysql:8.4 >/dev/null
ready=0
for attempt in $(seq 1 80); do
  if docker exec -e MYSQL_PWD="$password" "$container" mysqladmin -uroot ping --silent >/dev/null 2>&1; then ready=1; break; fi
  sleep 2
done
test "$ready" = 1
sql() { docker exec -e MYSQL_PWD="$password" "$container" mysql -uroot --batch --skip-column-names -e "$1" 2>/dev/null; }
version="$(sql 'SELECT VERSION()')"
[[ "$version" == 8.4.* ]] || { echo "FAIL: unexpected MySQL version: $version" >&2; exit 21; }
sql "CREATE DATABASE sqv22_continuity_rehearsal;
CREATE TABLE sqv22_continuity_rehearsal.records (id INT PRIMARY KEY, tenant_id INT NOT NULL, payload VARCHAR(60) NOT NULL);
INSERT INTO sqv22_continuity_rehearsal.records VALUES (1,10,'LEGITIMATE_A'),(2,20,'LEGITIMATE_B');"
snapshot() { sql "SELECT CONCAT(id,'|',tenant_id,'|',payload) FROM sqv22_continuity_rehearsal.records ORDER BY id"; }
before="$(snapshot)"
test "$(printf '%s\n' "$before" | wc -l | tr -d ' ')" = 2
hash_before="$(printf '%s' "$before" | sha256sum | cut -d ' ' -f1)"
# A genuine transaction rollback after mutation of both synthetic tenants.
changed="$(sql "START TRANSACTION; UPDATE sqv22_continuity_rehearsal.records SET payload='TEMPORARY' WHERE id=1 AND tenant_id=10; SELECT ROW_COUNT(); ROLLBACK;")"
test "$changed" = 1
test "$(snapshot)" = "$before"
# Wrong-tenant predicate must not mutate the target row, even before rollback.
wrong_tenant="$(sql "START TRANSACTION; UPDATE sqv22_continuity_rehearsal.records SET payload='UNAUTHORIZED' WHERE id=1 AND tenant_id=20; SELECT ROW_COUNT(); ROLLBACK;")"
test "$wrong_tenant" = 0
test "$(snapshot)" = "$before"
# Partial failure: rollback to savepoint, then commit the preserved state.
sql "START TRANSACTION; SAVEPOINT safety; UPDATE sqv22_continuity_rehearsal.records SET payload='PARTIAL' WHERE id=2; ROLLBACK TO SAVEPOINT safety; COMMIT;"
test "$(snapshot)" = "$before"
# The preserved legitimate rows must also survive restarting the ephemeral MySQL container.
docker restart "$container" >/dev/null
ready=0
for attempt in $(seq 1 80); do
  if docker exec -e MYSQL_PWD="$password" "$container" mysqladmin -uroot ping --silent >/dev/null 2>&1; then ready=1; break; fi
  sleep 2
done
test "$ready" = 1
after="$(snapshot)"
test "$after" = "$before"
hash_after="$(printf '%s' "$after" | sha256sum | cut -d ' ' -f1)"
test "$hash_before" = "$hash_after"
echo "C0304_M0604_ROLLBACK_REHEARSAL=PASS"
echo "MYSQL_EPHEMERAL_VERSION=$version;SYNTHETIC_ROWS=2;TX_ROLLBACK=PASS;FOREIGN_TENANT_WHERE=0;SAVEPOINT=PASS;RESTART_PRESERVATION=PASS;SHA256_MATCH=PASS"
echo "JWT_HTTP=NOT_TESTED;FULL_BACKUP_RESTORE=NOT_TESTED;RPO_RTO=NOT_MEASURED;LIVE_CUSTOMER_DATA=NEVER_TOUCHED;PROD=NO_GO"
