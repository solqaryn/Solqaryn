#!/usr/bin/env bash
# C14-04.M06.04: genuine git revert in isolated GitHub Actions worktree.
# This is NOT a DB rollback, tenant isolation, RPO/RTO, or production restore.
set -euo pipefail

target="963fa8c4e08300238f1b818fa7b33cb20ab1346c"
source_head="${GITHUB_SHA:?Must run against actual checked out SHA}"
git merge-base --is-ancestor "$target" "$source_head"

mapfile -t original_paths < <(git show --pretty=format: --name-only "$target" | sed '/^$/d' | sort)
test "${#original_paths[@]}" -eq 4
expected=(
  ".github/workflows/v20-c14-04-m05-06-regression-a11y.yml"
  "CHANGELOG_AI.md"
  "docs/v5/c14-04-m05-06-readiness-regression-a11y.md"
  "frontend/e2e/c14-04-m05-06-regression-a11y.spec.ts"
)
mapfile -t allowed < <(printf '%s\n' "${expected[@]}" | sort)
test "${original_paths[*]}" = "${allowed[*]}" || {
  printf 'Target diff mismatch, refusing unsafe revert\n' >&2
  exit 1
}

sandbox="$(mktemp -d "${RUNNER_TEMP:-/tmp}/sq-revert-v20-XXXXXX")"
rmdir "$sandbox"
cleanup() { git worktree remove --force -- "$sandbox" >/dev/null 2>&1 || true; }
trap cleanup EXIT
git worktree add --detach "$sandbox" "$source_head" >/dev/null

# Synthetic untracked sentinel is NOT real customer data; protected tracked
# application sources are checked independently with a fail-closed diff.
printf 'SYNTHETIC_NO_CUSTOMER_DATA_PRESERVE_20261118\n' > "$sandbox/.sq-synthetic-preservation"
sentinel_before="$(sha256sum "$sandbox/.sq-synthetic-preservation" | cut -d ' ' -f 1)"

# Execute actual git revert on a detached ephemeral worktree, never push.
git -C "$sandbox" revert --no-commit --no-edit "$target"
test ! -e "$sandbox/frontend/e2e/c14-04-m05-06-regression-a11y.spec.ts"
test ! -e "$sandbox/.github/workflows/v20-c14-04-m05-06-regression-a11y.yml"
test ! -e "$sandbox/docs/v5/c14-04-m05-06-readiness-regression-a11y.md"

mapfile -t actual < <(git -C "$sandbox" diff --name-only HEAD | sort)
test "${actual[*]}" = "${allowed[*]}" || {
  printf 'Revert altered unexpected tracked files: %s\n' "${actual[*]}" >&2
  exit 1
}
git -C "$sandbox" diff --check HEAD

guard_protected() {
  local changed
  changed="$(git -C "$sandbox" diff --name-only HEAD -- backend/src frontend/src backend/scripts scripts/security)"
  if [[ -n "$changed" ]]; then
    printf 'PROTECTED_SOURCE_CHANGED=%s\n' "$changed" >&2
    return 1
  fi
  return 0
}
guard_protected

# Adversarial control: modify a real tracked application source in sandbox.
# The safety guard MUST reject it, not report a vacuous PASS.
mapfile -t protected < <(git -C "$sandbox" ls-files backend/src frontend/src)
test "${#protected[@]}" -gt 0
probe="${protected[0]}"
printf '\n// V20_ADVERSARIAL_PROBE_UNSAFE\n' >> "$sandbox/$probe"
if guard_protected; then
  echo "FAIL: intentional protected source modification was not detected" >&2
  exit 1
fi
git -C "$sandbox" restore --source=HEAD --staged --worktree -- "$probe"
guard_protected

sentinel_after="$(sha256sum "$sandbox/.sq-synthetic-preservation" | cut -d ' ' -f 1)"
test "$sentinel_before" = "$sentinel_after"
echo "REAL_GIT_REVERT_EPHEMERAL=PASS"
echo "TARGET_SHA=$target"
echo "TESTED_HEAD_SHA=$source_head"
echo "REVERT_DIFF_FILES=${#actual[@]}"
echo "PROTECTED_APP_SOURCE_UNCHANGED=PASS"
echo "NEGATIVE_GUARD_DETECTED_MUTATION=PASS"
echo "SYNTHETIC_UNTRACKED_SENTINEL_PRESERVED=PASS"
echo "LIVE_CUSTOMER_DB_RESTORE=NOT_TESTED"
echo "PRODUCTION_ROLLBACK_AUTHORIZATION=NO_GO"
