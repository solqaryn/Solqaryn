#!/usr/bin/env bash
set -euo pipefail

readonly DEFAULT_MASTER_FILE="docs/VAEP_AUTHORITY.md"
readonly ALLOWED_KEYS=(
  EXECUTION_MODEL DIRECT_EXECUTION_DEFAULT PRIMARY_DIRECT_BUILD_CLOSE
  SUPERVISOR_VERIFY_RECOVER_SECONDARY_BUILD TASK_SCOPE_LEASE_REQUIRED
  TASK_SCOPE_LEASE_SINGLE_WRITER TASK_SCOPE_LEASE_TTL_MINUTES
  STALE_LEASE_TAKEOVER_AFTER_MINUTES LEASE_FRESH_REQUIRES_LIVE_MATERIAL_PROGRESS
  ENDED_INVOCATION_RELEASES_LEASE_IMMEDIATELY DIRECT_NEXT_SAFE_PREARM_REQUIRED
  MAX_VOLUNTARY_IDLE GLOBAL_DISPATCH_ADMISSION GLOBAL_FROZEN_PROHIBITED
  CAUSAL_HOLD_SCOPE UNRELATED_WORKFLOW_BLOCKING_PROHIBITED
  REVIEW_BEFORE_EXPENSIVE_CI CLOSURE_CANDIDATE_HEAD_FREEZE
  PREARM_BEFORE_CAUSAL_CI VAEP_CHECKPOINTS VAEP_SUPERVISOR_CHECKPOINTS
  VAEP_ALL_ACTIVE_SLOTS PRIMARY_RESPONSABLE_OPERATIVO SUPERVISOR_RESPONSABLE_OPERATIVO
  MANUAL_RUN_SLOT_GUARD MANUAL_RUN_NEAREST_DUE_SLOT_ONLY
  MANUAL_RUN_PAST_WINDOW_MINUTES MANUAL_RUN_FUTURE_SLOT_PREEMPT_PROHIBITED
  MANUAL_RUN_OFF_SLOT_PROHIBITED DEFECT_RECOVERY_FIRST FIRST_DETECTOR_OWNS_RECOVERY
  NO_REJECT_QUEUE RECOVERY_MUST_RESOLVE_SAME_RUN
  RECOVERY_UNBLOCK_DEPENDENTS_SAME_RUN CURRENT_MASTER_ONLY
  PREVIOUS_PLAN_DEPENDENCY PREVIOUS_PHASE_GATE PREVIOUS_QUEUE_ANCHOR
)

fail(){ printf 'VAEP_POLICY_PARSER_ERROR: %s\n' "$1" >&2; exit 1; }

is_boolean_key(){
  case "$1" in
    DIRECT_EXECUTION_DEFAULT|PRIMARY_DIRECT_BUILD_CLOSE|SUPERVISOR_VERIFY_RECOVER_SECONDARY_BUILD|TASK_SCOPE_LEASE_REQUIRED|TASK_SCOPE_LEASE_SINGLE_WRITER|LEASE_FRESH_REQUIRES_LIVE_MATERIAL_PROGRESS|ENDED_INVOCATION_RELEASES_LEASE_IMMEDIATELY|DIRECT_NEXT_SAFE_PREARM_REQUIRED|CLOSURE_CHAIN_SAME_RUN|GLOBAL_FROZEN_PROHIBITED|CAUSAL_GATE_MATRIX_REQUIRED|UNRELATED_WORKFLOW_BLOCKING_PROHIBITED|REVIEW_BEFORE_EXPENSIVE_CI|CLOSURE_CANDIDATE_HEAD_FREEZE|LOGICAL_CLOSER_CONTINUITY|SPLIT_BEFORE_WRITE_OVER_20M|CYCLE_TIME_TELEMETRY_REQUIRED|PREARM_BEFORE_CAUSAL_CI|MANUAL_RUN_SLOT_GUARD|MANUAL_RUN_NEAREST_DUE_SLOT_ONLY|MANUAL_RUN_FUTURE_SLOT_PREEMPT_PROHIBITED|MANUAL_RUN_OFF_SLOT_PROHIBITED|DEFECT_RECOVERY_FIRST|FIRST_DETECTOR_OWNS_RECOVERY|NO_REJECT_QUEUE|RECOVERY_MUST_RESOLVE_SAME_RUN|RECOVERY_UNBLOCK_DEPENDENTS_SAME_RUN|PARENT_CLOSE_FIRST|OWNER_INTERVENTION_GATE_ACTIVE|OWNER_INTERVENTION_AUTOMATIONS_PAUSED|OWNER_PAUSE_OVERRIDES_CANONICAL_LIVENESS|OWNER_INTERVENTION_BYPASS_PROHIBITED|POST_INTERVENTION_REVALIDATION_REQUIRED|POST_INTERVENTION_REVALIDATE_EXISTING_LISTO|POST_INTERVENTION_REOPEN_ON_GAP|POST_INTERVENTION_HISTORICAL_STATUS_PRESERVED|POST_INTERVENTION_BULK_REOPEN_PROHIBITED|OWNER_BULK_REOPEN_701_881_EXECUTED|PRODUCTION_RELEASE_EXCEPTION_EXECUTED|PR2_REOPEN_REQUIRED|FUTURE_MAIN_CHANGES_REQUIRE_FRESH_OWNER_AUTH|FUTURE_PRODUCTION_CHANGES_REQUIRE_FRESH_OWNER_AUTH|CURRENT_MASTER_ONLY) return 0;;
    *) return 1;;
  esac
}

is_string_key(){
  case "$1" in
    EXECUTION_MODEL|VAEP_CHECKPOINTS|VAEP_SUPERVISOR_CHECKPOINTS|VAEP_ALL_ACTIVE_SLOTS|PRIMARY_RESPONSABLE_OPERATIVO|SUPERVISOR_RESPONSABLE_OPERATIVO|MANUAL_RUN_SELECTION_TIEBREAK|GLOBAL_DISPATCH_ADMISSION|CAUSAL_HOLD_SCOPE|PREVIOUS_PLAN_DEPENDENCY|PREVIOUS_PHASE_GATE|PREVIOUS_QUEUE_ANCHOR|OWNER_INTERVENTION_ENTRY_AFTER|OWNER_INTERVENTION_ALLOWED_PARENTS|OWNER_INTERVENTION_EXIT|OWNER_INTERVENTION_REPLAY_START|OWNER_INTERVENTION_RESUME_FROM|POST_INTERVENTION_REVALIDATION_BASIS|POST_INTERVENTION_REVALIDATION_ADMISSION|POST_INTERVENTION_REVALIDATION_STATE|PRODUCTION_RELEASE_PR_STATE|PRODUCTION_RELEASE_MAIN_BASELINE) return 0;;
    *) return 1;;
  esac
}

parse_policy_block(){
  local target_file="${1:-$DEFAULT_MASTER_FILE}"
  [[ -f "$target_file" ]] || fail "file does not exist: $target_file"
  local begin_count=0 end_count=0 in_block=0 raw_line line key val candidate allowed required_key canonical_stream=""
  declare -g -A POLICY_MAP=()
  while IFS= read -r raw_line || [[ -n "$raw_line" ]]; do
    line="${raw_line%$'\r'}"; line="$(printf '%s' "$line" | sed -e 's/^[[:space:]]*//' -e 's/[[:space:]]*$//')"
    if [[ "$line" == BEGIN_AUTOMATION_POLICY ]]; then begin_count=$((begin_count+1)); in_block=1; continue; fi
    if [[ "$line" == END_AUTOMATION_POLICY ]]; then end_count=$((end_count+1)); in_block=0; continue; fi
    [[ "$in_block" -eq 1 && -n "$line" ]] || continue
    [[ "$line" =~ ^([A-Za-z0-9_]+)=(.*)$ ]] || fail "invalid line syntax in policy block: '$line'"
    key="${BASH_REMATCH[1]}"; val="${BASH_REMATCH[2]}"; allowed=0
    for candidate in "${ALLOWED_KEYS[@]}"; do [[ "$key" == "$candidate" ]] && { allowed=1; break; }; done
    [[ "$allowed" -eq 1 ]] || fail "unknown key in policy block: '$key'"
    [[ -z "${POLICY_MAP[$key]+_}" ]] || fail "duplicate key in policy block: '$key'"
    case "$key" in
      TASK_SCOPE_LEASE_TTL_MINUTES|STALE_LEASE_TAKEOVER_AFTER_MINUTES|PARENT_CLOSE_SLA_ROLLING_60M|PARENT_CLOSE_SLA_ROLLING_24H|PARENT_MAX_DWELL_MINUTES|PARENT_STALL_NO_PROGRESS_MINUTES|CLOSURE_REVIEW_MAX_LATENCY_MINUTES|CLOSURE_DEBT_TRIGGER_LT|MANUAL_RUN_PAST_WINDOW_MINUTES|OWNER_INTERVENTION_QUEUE_ANCHOR_ROW|OWNER_INTERVENTION_FIRST_ROW|POST_INTERVENTION_REVALIDATION_START_ROW|POST_INTERVENTION_REVALIDATION_END_ROW|PRODUCTION_RELEASE_PR)
        [[ "$val" =~ ^[1-9][0-9]*$ ]] || fail "invalid positive integer for $key: '$val'";;
      MAX_VOLUNTARY_IDLE) [[ "$val" =~ ^(0|[1-9][0-9]*)$ ]] || fail "invalid non-negative integer for $key: '$val'";;
      VAEP_CHECKPOINTS|VAEP_SUPERVISOR_CHECKPOINTS|VAEP_ALL_ACTIVE_SLOTS) [[ "$val" =~ ^:[0-5][0-9](,:[0-5][0-9])*$ ]] || fail "invalid checkpoint list for $key: '$val'";;
      EXECUTION_MODEL) [[ "$val" == TASKS_ONLY ]] || fail "invalid value for $key: '$val'";;
      PRIMARY_RESPONSABLE_OPERATIVO) [[ "$val" == JAVIER_MEJIA ]] || fail "invalid value for $key: '$val'";;
      SUPERVISOR_RESPONSABLE_OPERATIVO) [[ "$val" == ALEX_MORALES ]] || fail "invalid value for $key: '$val'";;
      MANUAL_RUN_SELECTION_TIEBREAK) [[ "$val" == LATEST_DUE_SLOT ]] || fail "invalid value for $key: '$val'";;
      GLOBAL_DISPATCH_ADMISSION) [[ "$val" == OPEN_ONLY ]] || fail "invalid value for $key: '$val'";;
      CAUSAL_HOLD_SCOPE) [[ "$val" == TASK_OR_EXECUTION_ONLY ]] || fail "invalid value for $key: '$val'";;
      PREVIOUS_PLAN_DEPENDENCY|PREVIOUS_PHASE_GATE|PREVIOUS_QUEUE_ANCHOR) [[ "$val" == PROHIBITED ]] || fail "invalid value for $key: '$val'";;
      OWNER_INTERVENTION_ENTRY_AFTER|OWNER_INTERVENTION_EXIT|OWNER_INTERVENTION_REPLAY_START|OWNER_INTERVENTION_RESUME_FROM) [[ "$val" =~ ^N8\.[0-9]+\.[A-H]$ ]] || fail "invalid parent/task reference for $key: '$val'";;
      OWNER_INTERVENTION_ALLOWED_PARENTS|POST_INTERVENTION_REVALIDATION_BASIS) [[ "$val" =~ ^N8\.[0-9]+(,N8\.[0-9]+)*$ ]] || fail "invalid parent list for $key: '$val'";;
      POST_INTERVENTION_REVALIDATION_ADMISSION) [[ "$val" == SEQUENTIAL_REVIEW_AFTER_OWNER_BULK_RESET ]] || fail "invalid value for $key: '$val'";;
      POST_INTERVENTION_REVALIDATION_STATE) [[ "$val" == PENDIENTE ]] || fail "invalid value for $key: '$val'";;
      PRODUCTION_RELEASE_PR_STATE) [[ "$val" == CLOSED_MERGED ]] || fail "invalid value for $key: '$val'";;
      PRODUCTION_RELEASE_MAIN_BASELINE) [[ "$val" =~ ^[0-9a-f]{40}$ ]] || fail "invalid commit sha for $key: '$val'";;
      *) if is_boolean_key "$key"; then [[ "$val" == TRUE || "$val" == FALSE ]] || fail "invalid boolean for $key: '$val'"; fi;;
    esac
    POLICY_MAP["$key"]="$val"
  done < "$target_file"
  [[ "$begin_count" -eq 1 && "$end_count" -eq 1 ]] || fail "expected exactly one policy block"
  for required_key in "${ALLOWED_KEYS[@]}"; do
    [[ -n "${POLICY_MAP[$required_key]+_}" ]] || fail "missing required key: '$required_key'"
    canonical_stream+="${required_key}=${POLICY_MAP[$required_key]}"$'\n'
  done
  AUTOMATION_POLICY_HASH="$(printf '%s' "$canonical_stream" | sha256sum | awk '{print $1}')"
  MASTER_COMMIT_SHA="$(git rev-parse HEAD 2>/dev/null || printf UNKNOWN)"
}

run_self_test(){ parse_policy_block "$DEFAULT_MASTER_FILE"; [[ ${#AUTOMATION_POLICY_HASH} -eq 64 ]] || fail hash; printf 'VAEP_POLICY_SELF_TEST=PASS\n'; }

main(){
  local cmd="${1:---validate}"; shift || true
  case "$cmd" in
    --self-test) run_self_test;;
    --validate) local file="${1:-$DEFAULT_MASTER_FILE}"; parse_policy_block "$file"; printf 'VAEP_POLICY_VALIDATION=PASS file=%s hash=%s\n' "$file" "$AUTOMATION_POLICY_HASH";;
    --get) local key="${1:?key required}" file="${2:-$DEFAULT_MASTER_FILE}"; parse_policy_block "$file"; [[ -n "${POLICY_MAP[$key]+_}" ]] || fail "unknown key requested: $key"; printf '%s\n' "${POLICY_MAP[$key]}";;
    --hash) local file="${1:-$DEFAULT_MASTER_FILE}"; parse_policy_block "$file"; printf '%s\n' "$AUTOMATION_POLICY_HASH";;
    --env) local file="${1:-$DEFAULT_MASTER_FILE}" key; parse_policy_block "$file"; for key in "${ALLOWED_KEYS[@]}"; do printf '%s=%s\n' "$key" "${POLICY_MAP[$key]}"; done;;
    --json) local file="${1:-$DEFAULT_MASTER_FILE}" key val first=1; parse_policy_block "$file"; printf '{\n'; for key in "${ALLOWED_KEYS[@]}"; do [[ "$first" -eq 1 ]] || printf ',\n'; first=0; val="${POLICY_MAP[$key]}"; if is_boolean_key "$key"; then [[ "$val" == TRUE ]] && val=true || val=false; printf '  "%s": %s' "$key" "$val"; elif is_string_key "$key"; then printf '  "%s": "%s"' "$key" "$val"; else printf '  "%s": %d' "$key" "$val"; fi; done; printf '\n}\n';;
    *) fail "unknown command: $cmd";;
  esac
}
main "$@"
