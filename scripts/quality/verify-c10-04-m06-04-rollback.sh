#!/usr/bin/env bash
# V21 C10-04.M06.04 — Only disposable GitHub-hosted MySQL 8.4. Never Aiven or production.
set -Eeuo pipefail
if [[ "$#" -ne 1 || -z "$1" ]]; then
  echo "Expected GitHub Actions ephemeral MySQL service container ID" >&2
  exit 2
fi
CID="$1"
DB="sqv21_c1004_m0604_disposable"
PASSWORD="sqv21-only-disposable-rollback"
CREATED=0
mysql_local() {
  docker exec -i -e MYSQL_PWD="$PASSWORD" "$CID" mysql --protocol=tcp -h 127.0.0.1 -P 3306 -uroot --batch --skip-column-names "$@"
}
cleanup() {
  if [[ "$CREATED" == "1" ]]; then
    mysql_local -e "DROP DATABASE IF EXISTS $DB" >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT
assert_equal() {
  if [[ "$1" != "$2" ]]; then
    printf 'C1004_M0604_ASSERT_FAIL=%s got=%s expected=%s\n' "$3" "$1" "$2" >&2
    exit 1
  fi
}
digest() {
  mysql_local -D "$DB" -e "SELECT SHA2(GROUP_CONCAT(CONCAT(tenant_id,':',record_id,':',state,':',enabled,':',version) ORDER BY tenant_id,record_id SEPARATOR '|'),256) FROM rollback_records"
}
mysql_local -e "SELECT VERSION()" | grep -E '^8[.]4[.]' >/dev/null
mysql_local -e "CREATE DATABASE $DB"
CREATED=1
mysql_local -D "$DB" -e "CREATE TABLE rollback_records (tenant_id INT NOT NULL, record_id VARCHAR(32) NOT NULL, state VARCHAR(32) NOT NULL, enabled TINYINT NOT NULL, version INT NOT NULL, PRIMARY KEY (tenant_id,record_id)) ENGINE=InnoDB"
mysql_local -D "$DB" -e "INSERT INTO rollback_records VALUES (101,'doc-a','active',1,7),(101,'doc-b','active',1,7),(202,'doc-c','active',1,7),(202,'doc-d','active',1,7)"
assert_equal "$(mysql_local -D "$DB" -e "SELECT COUNT(*) FROM rollback_records")" "4" "fixture_four_records"
assert_equal "$(mysql_local -D "$DB" -e "SELECT COUNT(*) FROM rollback_records WHERE tenant_id=101")" "2" "tenant_a_before"
assert_equal "$(mysql_local -D "$DB" -e "SELECT COUNT(*) FROM rollback_records WHERE tenant_id=202")" "2" "tenant_b_before"
BEFORE="$(digest)"
if [[ -z "$BEFORE" ]]; then echo 'Empty baseline digest' >&2; exit 1; fi

# Actual server-side transaction mutates AND deletes disposable rows, then rolls back.
TENTATIVE="$(mysql_local -D "$DB" -e "START TRANSACTION; UPDATE rollback_records SET state='uncommitted', enabled=0, version=version+1 WHERE tenant_id=101 AND record_id='doc-a'; DELETE FROM rollback_records WHERE tenant_id=202 AND record_id='doc-c'; SELECT CONCAT(COUNT(*),':',SUM(enabled)) FROM rollback_records; ROLLBACK;")"
assert_equal "$TENTATIVE" "3:2" "negative_change_observed_before_rollback"
assert_equal "$(digest)" "$BEFORE" "rollback_restored_exact_digest"
assert_equal "$(mysql_local -D "$DB" -e "SELECT COUNT(*) FROM rollback_records")" "4" "rollback_did_not_delete_legitimate_rows"

# Negative cross-tenant mutation: ID from tenant B cannot be modified under tenant A.
CROSS="$(mysql_local -D "$DB" -e "UPDATE rollback_records SET state='leaked' WHERE tenant_id=101 AND record_id='doc-c'; SELECT ROW_COUNT()")"
assert_equal "$CROSS" "0" "tenant_b_row_cannot_be_modified_by_a"
assert_equal "$(digest)" "$BEFORE" "cross_tenant_attempt_zero_effect"

# Commit a reversible feature disable. A stale second writer must be rejected.
DISABLED="$(mysql_local -D "$DB" -e "UPDATE rollback_records SET enabled=0, version=8 WHERE tenant_id=101 AND record_id='doc-b' AND version=7 AND enabled=1; SELECT ROW_COUNT()")"
assert_equal "$DISABLED" "1" "exactly_one_authorized_disable"
assert_equal "$(mysql_local -D "$DB" -e "SELECT CONCAT(enabled,':',version) FROM rollback_records WHERE tenant_id=101 AND record_id='doc-b'")" "0:8" "disable_durable"
assert_equal "$(mysql_local -D "$DB" -e "SELECT COUNT(*) FROM rollback_records")" "4" "disable_no_row_loss"
assert_equal "$(mysql_local -D "$DB" -e "SELECT COUNT(*) FROM rollback_records WHERE tenant_id=202 AND enabled=1 AND version=7")" "2" "other_tenant_unchanged"
STALE="$(mysql_local -D "$DB" -e "UPDATE rollback_records SET state='bad-replay' WHERE tenant_id=101 AND record_id='doc-b' AND version=7; SELECT ROW_COUNT()")"
assert_equal "$STALE" "0" "stale_replay_rejected"

# Restore feature state with CAS, then require byte-equivalent SQL digest.
RESTORED="$(mysql_local -D "$DB" -e "UPDATE rollback_records SET enabled=1, version=7 WHERE tenant_id=101 AND record_id='doc-b' AND version=8 AND enabled=0; SELECT ROW_COUNT()")"
assert_equal "$RESTORED" "1" "feature_reenabled_once"
assert_equal "$(digest)" "$BEFORE" "reversible_disable_restores_baseline"
assert_equal "$(mysql_local -D "$DB" -e "SELECT COUNT(*) FROM rollback_records WHERE tenant_id=202")" "2" "no_other_tenant_deletion"
echo 'C1004_M0604_MYSQL84_REAL_TRANSACTION_ROLLBACK_4_OF_4_PRESERVED=TRUE'
echo 'C1004_M0604_TENANT_AB_CAS_STALE_NEGATIVES_PASS=TRUE'
echo 'C1004_M0604_DISABLE_REENABLE_REVERSIBLE_NO_DATA_LOSS=TRUE'
echo 'C1004_M0604_DISPOSABLE_ONLY_NO_EF_NO_JWT_NO_REAL_WORKER_NO_PROD=TRUE'
