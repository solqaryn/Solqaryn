#!/usr/bin/env bash
# C10-04.M05.06: disposable GitHub-hosted MySQL 8.4 test only; NEVER Aiven/production.
set -Eeuo pipefail
CID="$1"
DB="sqv19_m0506_disposable"
PASSWORD="sqv19-only-disposable-test"
TMP_DIR="$(mktemp -d)"
mysql_local() {
  docker exec -i -e MYSQL_PWD="$PASSWORD" "$CID" mysql --protocol=tcp -h 127.0.0.1 -P 3306 -uroot --batch --skip-column-names "$@"
}
cleanup() {
  mysql_local -e "DROP DATABASE IF EXISTS $DB" >/dev/null 2>&1 || true
  rm -rf -- "$TMP_DIR"
}
trap cleanup EXIT
assert_equal() {
  if [[ "$1" != "$2" ]]; then
    printf 'C1004_M0506_ASSERT_FAILED=%s\n' "$3" >&2
    exit 1
  fi
}
checksum() {
  mysql_local -D "$DB" -e "SELECT SHA2(GROUP_CONCAT(CONCAT(tenant_id, ':', work_id, ':', idem_key, ':', version, ':', status) ORDER BY tenant_id, work_id SEPARATOR '|'), 256) FROM review_jobs"
}
mysql_local -e "SELECT VERSION()" | grep -E '^8[.]4[.]' >/dev/null
mysql_local -e "CREATE DATABASE $DB"
mysql_local -D "$DB" -e "CREATE TABLE review_jobs (tenant_id INT NOT NULL, work_id VARCHAR(64) NOT NULL, idem_key VARCHAR(64) NOT NULL, version BIGINT NOT NULL, status VARCHAR(24) NOT NULL, PRIMARY KEY (tenant_id, work_id), UNIQUE KEY uq_idem (tenant_id, idem_key)) ENGINE=InnoDB"
mysql_local -D "$DB" -e "INSERT INTO review_jobs VALUES (101,'job-a','key-a',7,'pending'),(202,'job-b','key-b',7,'pending')"
assert_equal "$(mysql_local -D "$DB" -e "SELECT COUNT(*) FROM review_jobs")" "2" "fixture_count"
assert_equal "$(mysql_local -D "$DB" -e "SELECT COUNT(*) FROM review_jobs WHERE tenant_id=101 AND work_id='job-b'")" "0" "tenant_a_must_not_read_b"
assert_equal "$(mysql_local -D "$DB" -e "SELECT COUNT(*) FROM review_jobs WHERE tenant_id=202 AND work_id='job-a'")" "0" "tenant_b_must_not_read_a"
mysql_local -D "$DB" -e "UPDATE review_jobs SET version=8,status='claimed' WHERE tenant_id=101 AND work_id='job-a' AND version=7 AND status='pending'"
assert_equal "$(mysql_local -D "$DB" -e "SELECT CONCAT(status, ':', version) FROM review_jobs WHERE tenant_id=101 AND work_id='job-a'")" "claimed:8" "persisted_claim_after_session_end"
mysql_local -D "$DB" -e "UPDATE review_jobs SET version=9,status='pending' WHERE tenant_id=101 AND work_id='job-a' AND version=8 AND status='claimed'"
assert_equal "$(mysql_local -D "$DB" -e "SELECT CONCAT(status, ':', version) FROM review_jobs WHERE tenant_id=101 AND work_id='job-a'")" "pending:9" "bounded_recovery_precondition"
# Real independent MySQL clients race on one synthetic row: NOT the deployed worker.
(mysql_local -D "$DB" -e "UPDATE review_jobs SET version=8,status='claimed' WHERE tenant_id=202 AND work_id='job-b' AND version=7 AND status='pending'; SELECT ROW_COUNT()" > "$TMP_DIR/race_1") &
RACE_1=$!
(mysql_local -D "$DB" -e "UPDATE review_jobs SET version=8,status='claimed' WHERE tenant_id=202 AND work_id='job-b' AND version=7 AND status='pending'; SELECT ROW_COUNT()" > "$TMP_DIR/race_2") &
RACE_2=$!
wait "$RACE_1"
wait "$RACE_2"
assert_equal "$(( $(cat "$TMP_DIR/race_1") + $(cat "$TMP_DIR/race_2") ))" "1" "exactly_one_concurrent_claim"
assert_equal "$(mysql_local -D "$DB" -e "SELECT CONCAT(status, ':', version) FROM review_jobs WHERE tenant_id=202 AND work_id='job-b'")" "claimed:8" "raced_claim_durable"
mysql_local -D "$DB" -e "UPDATE review_jobs SET version=9,status='pending' WHERE tenant_id=202 AND work_id='job-b' AND version=8 AND status='claimed'"
assert_equal "$(mysql_local -D "$DB" -e "SELECT COUNT(*) FROM review_jobs WHERE tenant_id=101 AND status='claimed'")" "0" "racer_did_not_claim_other_tenant"

# A synthetic worker PROCESS is really SIGKILL-ed after committing its SQL claim.
# A fresh process recovers the persisted claim; stale replay must fail.
(
  mysql_local -D "$DB" -e "UPDATE review_jobs SET version=10,status='claimed' WHERE tenant_id=101 AND work_id='job-a' AND version=9 AND status='pending'; SELECT ROW_COUNT()" > "$TMP_DIR/killed_worker_claim"
  touch "$TMP_DIR/worker_committed"
  exec sleep 90
) &
WORKER_PID=$!
for attempt in $(seq 1 60); do
  if [[ -f "$TMP_DIR/worker_committed" ]]; then break; fi
  if ! kill -0 "$WORKER_PID" 2>/dev/null; then
    echo 'Synthetic worker ended before crash injection' >&2
    exit 1
  fi
  sleep 0.2
done
test -f "$TMP_DIR/worker_committed"
assert_equal "$(cat "$TMP_DIR/killed_worker_claim")" "1" "worker_claim_committed_before_sigkill"
kill -9 "$WORKER_PID"
set +e
wait "$WORKER_PID" 2>/dev/null
KILL_EXIT=$?
set -e
assert_equal "$KILL_EXIT" "137" "worker_process_actually_sigkilled"
assert_equal "$(mysql_local -D "$DB" -e "SELECT CONCAT(status, ':', version) FROM review_jobs WHERE tenant_id=101 AND work_id='job-a'")" "claimed:10" "persisted_claim_survives_sigkill"
(
  mysql_local -D "$DB" -e "UPDATE review_jobs SET version=11,status='pending' WHERE tenant_id=101 AND work_id='job-a' AND version=10 AND status='claimed'; SELECT ROW_COUNT()" > "$TMP_DIR/restarted_worker_recovery"
) &
RECOVERY_PID=$!
wait "$RECOVERY_PID"
assert_equal "$(cat "$TMP_DIR/restarted_worker_recovery")" "1" "fresh_worker_recovers_once"
assert_equal "$(mysql_local -D "$DB" -e "UPDATE review_jobs SET version=11,status='pending' WHERE tenant_id=101 AND work_id='job-a' AND version=10 AND status='claimed'; SELECT ROW_COUNT()")" "0" "stale_worker_replay_rejected"
assert_equal "$(mysql_local -D "$DB" -e "SELECT CONCAT(status, ':', version) FROM review_jobs WHERE tenant_id=101 AND work_id='job-a'")" "pending:11" "recovered_version_after_sigkill"
echo 'C1004_M0506_CONCURRENT_CAS_AND_SYNTHETIC_SIGKILL_PASS=TRUE'

BEFORE="$(checksum)"
docker exec -e MYSQL_PWD="$PASSWORD" "$CID" mysqldump -uroot --single-transaction --no-tablespaces --skip-comments --skip-dump-date --databases "$DB" >"$TMP_DIR/backup.sql"
test -s "$TMP_DIR/backup.sql"
DUMP_SHA="$(sha256sum "$TMP_DIR/backup.sql" | cut -d ' ' -f 1)"
cp "$TMP_DIR/backup.sql" "$TMP_DIR/tampered.sql"
printf '\n-- deliberately altered disposable fixture\n' >>"$TMP_DIR/tampered.sql"
if [[ "$(sha256sum "$TMP_DIR/tampered.sql" | cut -d ' ' -f 1)" == "$DUMP_SHA" ]]; then
  echo 'Corrupt dump was not detected' >&2
  exit 1
fi
# Tampered dump is NEVER passed to MySQL; checksum mismatch denies restore.
mysql_local -D "$DB" -e "UPDATE review_jobs SET status='damaged' WHERE tenant_id=101; DELETE FROM review_jobs WHERE tenant_id=202"
if [[ "$(checksum)" == "$BEFORE" ]]; then
  echo 'Mutation was not observable before restore' >&2
  exit 1
fi
mysql_local -e "DROP DATABASE $DB"
mysql_local <"$TMP_DIR/backup.sql"
assert_equal "$(checksum)" "$BEFORE" "restored_row_digest"
assert_equal "$(mysql_local -D "$DB" -e "SELECT COUNT(*) FROM review_jobs")" "2" "restored_both_tenants"
assert_equal "$(mysql_local -D "$DB" -e "SELECT COUNT(*) FROM review_jobs WHERE tenant_id=101")" "1" "tenant_a_count"
assert_equal "$(mysql_local -D "$DB" -e "SELECT COUNT(*) FROM review_jobs WHERE tenant_id=202")" "1" "tenant_b_count"
mysql_local -D "$DB" -e "START TRANSACTION; UPDATE review_jobs SET status='rolled-back' WHERE tenant_id=101; ROLLBACK"
assert_equal "$(checksum)" "$BEFORE" "rollback_preserved_restored_snapshot"
echo 'C1004_M0506_MYSQL84_DISPOSABLE_RESTORE_REPLAY_PASS=TRUE'
echo 'C1004_M0506_NO_PROD_NO_EF_NO_JWT_NO_ENCRYPTION_NO_REAL_WORKER=TRUE'
