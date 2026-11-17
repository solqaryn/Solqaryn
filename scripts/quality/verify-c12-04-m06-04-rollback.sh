#!/usr/bin/env bash
set -euo pipefail
# C12-04.M06.04: ONLY an isolated, historical Git rollback rehearsal.
# No remote writes, database access, migrations or production operations.
readonly approved="f8c27a0ef47f82bac61b9ec1ebdcb70080b70f04"
readonly approved_parent="c99f9df2f374792588d2a53b10da07fca4a8d285"
if [[ "$#" -ne 1 || "${1:-}" != "$approved" ]]; then
  echo "REJECTED_UNAPPROVED_SHA" >&2
  exit 64
fi
root="$(git rev-parse --show-toplevel)"
readonly initial_head="$(git -C "$root" rev-parse HEAD)"
[[ -z "$(git -C "$root" status --porcelain)" ]] || { echo "PRIMARY_WORKTREE_DIRTY" >&2; exit 1; }
[[ "$(git -C "$root" rev-parse "$approved^")" == "$approved_parent" ]] || { echo "SOURCE_PARENT_MISMATCH" >&2; exit 1; }
git -C "$root" merge-base --is-ancestor "$approved" "$initial_head" || { echo "SOURCE_NOT_ANCESTOR" >&2; exit 1; }

expected="$(printf '%s\n' \
  '.github/workflows/v19-c12-04-m05-06-regresion-accesibilidad.yml' \
  'docs/evidencias/C12-04.M05.06-V19.md' \
  'frontend/e2e/c12-04-m05-06-regresion-accesibilidad.spec.ts' | LC_ALL=C sort)"
changed="$(git -C "$root" diff --name-only "$approved_parent" "$approved" | LC_ALL=C sort)"
[[ "$changed" == "$expected" ]] || { echo "UNEXPECTED_SOURCE_PATHS" >&2; exit 1; }

tmp="$(mktemp -d)"
work="$tmp/isolated"
cleanup() {
  if [[ -e "$work/.git" ]]; then git -C "$root" worktree remove --force "$work" >/dev/null 2>&1 || true; fi
  rm -rf "$tmp"
}
trap cleanup EXIT
# Synthetic data sentinel: verifies that this isolated Git-only test does not change unrelated files.
printf 'empresa,documento,importe\nA,orden-1,100\nB,orden-2,200\n' > "$tmp/synthetic-data.csv"
readonly data_before="$(sha256sum "$tmp/synthetic-data.csv" | cut -d' ' -f1)"
readonly rows_before="$(wc -l < "$tmp/synthetic-data.csv" | tr -d ' ')"

git -C "$root" worktree add --detach "$work" "$approved" >/dev/null
[[ "$(git -C "$work" rev-parse HEAD)" == "$approved" ]] || exit 1
git -C "$work" -c user.name='Solqaryn CI' -c user.email='ci@example.invalid' revert --no-commit "$approved"
readonly actual_tree="$(git -C "$work" write-tree)"
readonly parent_tree="$(git -C "$root" rev-parse "$approved_parent^{tree}")"
[[ "$actual_tree" == "$parent_tree" ]] || { echo "REVERT_TREE_MISMATCH" >&2; exit 1; }
reverted_paths="$(git -C "$work" diff --cached --name-only | LC_ALL=C sort)"
[[ "$reverted_paths" == "$expected" ]] || { echo "REVERT_CHANGED_UNEXPECTED_PATHS" >&2; exit 1; }
[[ "$(sha256sum "$tmp/synthetic-data.csv" | cut -d' ' -f1)" == "$data_before" ]] || exit 1
[[ "$(wc -l < "$tmp/synthetic-data.csv" | tr -d ' ')" == "$rows_before" ]] || exit 1
[[ "$(git -C "$root" rev-parse HEAD)" == "$initial_head" ]] || { echo "PRIMARY_HEAD_CHANGED" >&2; exit 1; }
[[ -z "$(git -C "$root" status --porcelain)" ]] || { echo "PRIMARY_TREE_CHANGED" >&2; exit 1; }
printf 'ISOLATED_REVERT_PASS=1 SOURCE_SHA=%s PARENT_SHA=%s TREE=%s FILES=3\n' "$approved" "$approved_parent" "$actual_tree"
printf 'SYNTHETIC_SENTINEL_SHA256=%s SYNTHETIC_ROWS=%s\n' "$data_before" "$rows_before"
echo 'LIVE_MYSQL_RESTORE=NOT_TESTED;MIGRATIONS=NOT_TESTED;RPO_RTO=NOT_MEASURED;PRODUCTION=NOT_TOUCHED'
