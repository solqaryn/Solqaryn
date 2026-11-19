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

# Execute a real revert from the current SHA in a disposable worktree.
# Resolve ONLY the known CHANGELOG section conflict; all other conflicts fail closed.
if git -C "$sandbox" revert --no-commit --no-edit "$target"; then
  echo "GIT_REVERT_CLEAN=PASS"
else
  mapfile -t unmerged < <(git -C "$sandbox" diff --name-only --diff-filter=U | sort -u)
  if [[ "${#unmerged[@]}" -ne 1 || "${unmerged[0]}" != "CHANGELOG_AI.md" ]]; then
    echo "FAIL: unexpected revert conflict, refusing resolution" >&2
    exit 1
  fi
  git -C "$sandbox" show "$source_head:CHANGELOG_AI.md" > "$sandbox/CHANGELOG_AI.md"
  python3 - "$sandbox/CHANGELOG_AI.md" "$target" <<'PY'
import pathlib
import subprocess
import sys
p = pathlib.Path(sys.argv[1])
target_sha = sys.argv[2]
original = subprocess.check_output(
    ["git", "show", f"{target_sha}:CHANGELOG_AI.md"], text=True
)
stanza = original.split("\n## ", 1)[0] + "\n"
required_header = "## 2026-11-18 — V20 C14-04.M05.06 regresion Chromium readiness DEV [Javier :20]"
if not stanza.startswith(required_header + "\n") or stanza.count("\n- ") != 2:
    raise SystemExit("FAIL: source stanza shape changed")
current = p.read_text(encoding="utf-8")
if current.count(stanza) != 1:
    raise SystemExit("FAIL: target stanza missing, modified or duplicated")
p.write_text(current.replace(stanza, "", 1), encoding="utf-8")
PY
  git -C "$sandbox" add -- CHANGELOG_AI.md
  if [[ -n "$(git -C "$sandbox" diff --name-only --diff-filter=U)" ]]; then
    echo "FAIL: unresolved files remain" >&2
    exit 1
  fi
  echo "BOUNDED_CHANGELOG_CONFLICT_RESOLVED=PASS"
fi
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
