#!/usr/bin/env bash
set -euo pipefail

readonly MASTER_FILE="docs/VAEP_AUTHORITY.md"
readonly PARSER=".github/scripts/vaep-policy-parser.sh"
readonly ADMISSION="vaep/control/dispatch-admission.json"
readonly BRANCH="dev"

fail(){ echo "VAEP_CHECKPOINT_ERROR=$1" >&2; exit 2; }
usage(){ echo 'usage: vaep-checkpoint.sh --checkpoint :00|:12|:24|:36|:48 | --self-test'; }
normalize_checkpoint(){ case "${1:-}" in :00|:12|:24|:36|:48) printf '%s\n' "$1";; '0 * * * *') echo ':00';; '12 * * * *') echo ':12';; '24 * * * *') echo ':24';; '36 * * * *') echo ':36';; '48 * * * *') echo ':48';; *) return 1;; esac; }

admission_is_open(){ jq -e 'type=="object" and .newDispatchAdmission=="OPEN" and .allowExistingActiveSessions==true' >/dev/null 2>&1; }

validate_contract(){
  [[ -f "$MASTER_FILE" && -f "$PARSER" && -f "$ADMISSION" ]] || fail missing_runtime_file
  bash "$PARSER" --validate "$MASTER_FILE" >/dev/null
  [[ "$(bash "$PARSER" --get EXECUTION_MODEL "$MASTER_FILE")" == TASKS_ONLY ]] || fail execution_model
  [[ "$(bash "$PARSER" --get GLOBAL_DISPATCH_ADMISSION "$MASTER_FILE")" == OPEN_ONLY ]] || fail admission_policy
  admission_is_open < "$ADMISSION" || fail admission_not_open
}

emit_role(){ case "$1" in :00) echo 'VAEP_CHECKPOINT_ROLE=PRIMARY_GUARD';; :12) echo 'VAEP_CHECKPOINT_ROLE=RECOVERY_GUARD';; :24) echo 'VAEP_CHECKPOINT_ROLE=REVIEW_GUARD';; :36) echo 'VAEP_CHECKPOINT_ROLE=WATCHDOG_GUARD';; :48) echo 'VAEP_CHECKPOINT_ROLE=DEBT_GUARD';; esac; }

run_self_test(){
  validate_contract
  [[ "$(normalize_checkpoint ':00')" == ':00' ]]
  [[ "$(normalize_checkpoint '48 * * * *')" == ':48' ]]
  if normalize_checkpoint ':15' >/dev/null 2>&1; then fail historical_checkpoint_accepted; fi
  ! grep -Eqi 'jules|J[1-6]|worker lane|autorefill' "$0"
  echo 'VAEP_CHECKPOINT_SELF_TEST=PASS model=TASKS_ONLY'
}

main(){
  if [[ "${1:-}" == --self-test ]]; then run_self_test; exit 0; fi
  local checkpoint_raw='' checkpoint=''
  while (($#)); do case "$1" in --checkpoint) checkpoint_raw="${2:-}"; shift 2;; --help|-h) usage; exit 0;; *) usage >&2; fail "unknown_argument=$1";; esac; done
  validate_contract
  checkpoint="$(normalize_checkpoint "$checkpoint_raw")" || fail "invalid_checkpoint=$checkpoint_raw"
  emit_role "$checkpoint"
  echo "VAEP_CHECKPOINT_EXECUTION=PASS checkpoint=$checkpoint model=TASKS_ONLY"
  echo "VAEP_CHECKPOINT_HEAD=$(git rev-parse HEAD)"
  echo 'VAEP_CHECKPOINT_AUTHORITY=docs/VAEP_AUTHORITY.md'
  echo 'VAEP_CHECKPOINT_SCOPE=GUARD_ONLY__NO_PRODUCT_WRITE__NO_EXTERNAL_WORKERS'
}
main "$@"
