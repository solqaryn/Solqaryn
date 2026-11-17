#!/usr/bin/env bash
set -euo pipefail
# Safe GitHub-hosted isolated rehearsal: no remote ref, DB or production writes.
readonly source_sha='315dc209deb75d31eb2f56545d1a9d8fa7bdaf83'
readonly parent_sha='e79fee28a02c0928d6789afa6d8dc39293adcd01'
if [[ "$#" -ne 1 || "$1" != "$source_sha" ]]; then
  echo 'NO_GO: only the exact approved source SHA may be rehearsed' >&2
  exit 64
fi
root="$(git rev-parse --show-toplevel)"
before="$(git -C "$root" rev-parse HEAD)"
test -z "$(git -C "$root" status --porcelain)"
git -C "$root" cat-file -e "$source_sha^{commit}"
test "$(git -C "$root" rev-parse "$source_sha^")" = "$parent_sha"
expected="$(printf '%s\n' \
  '.github/workflows/v19-c05-01-m05-06-regression.yml' \
  'CHANGELOG_AI.md' \
  'docs/evidencias/C05-01.M05.06-V19.md' \
  'frontend/e2e/c05-01-m05-06-regression-a11y.spec.ts' | LC_ALL=C sort)"
actual="$(git -C "$root" diff --name-only "$parent_sha" "$source_sha" | LC_ALL=C sort)"
if [[ "$actual" != "$expected" ]]; then
  echo 'NO_GO: unexpected file count or path' >&2
  exit 65
fi
if printf '%s\n' "$actual" | grep -Eq '^(backend/src/|backend/migrations/|database/|infra/|frontend/src/)'; then
  echo 'NO_GO: source touched runtime/data' >&2
  exit 66
fi
tmp="$(mktemp -d)"
worktree="$tmp/detached"
cleanup() {
  if [[ -e "$worktree/.git" ]]; then
    git -C "$root" worktree remove --force "$worktree" >/dev/null 2>&1 || true
  fi
  rm -rf -- "$tmp"
}
trap cleanup EXIT
git -C "$root" worktree add --detach "$worktree" "$source_sha" >/dev/null
test -z "$(git -C "$worktree" status --porcelain)"
git -C "$worktree" revert --no-commit "$source_sha"
reverted_tree="$(git -C "$worktree" write-tree)"
parent_tree="$(git -C "$root" rev-parse "$parent_sha^{tree}")"
test "$reverted_tree" = "$parent_tree"
test "$(git -C "$worktree" diff --cached --name-only | LC_ALL=C sort)" = "$expected"
test "$(git -C "$worktree" rev-parse HEAD)" = "$source_sha"
test "$(git -C "$root" rev-parse HEAD)" = "$before"
test -z "$(git -C "$root" status --porcelain)"
echo "ROLLBACK_ISOLATED_PASS=1;SOURCE_SHA=$source_sha;PARENT_TREE=$parent_tree;FILES=4"
echo 'DATA_DELETION=NONE;MYSQL_RESTORE=NOT_TESTED;JWT_RBAC=NOT_TESTED;PROD=NOT_TOUCHED'
