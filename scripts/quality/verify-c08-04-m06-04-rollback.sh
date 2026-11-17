#!/usr/bin/env bash
set -euo pipefail

# C08-04.M06.04: local runner-only rehearsal, never touching shared branch or database.
readonly source_sha='3a5aa69c38b716336da150650fdf881ebb5320f8'
if [[ "${1:-}" != "$source_sha" ]]; then
  echo 'ERROR: rehearsal accepts only the approved exact source commit SHA' >&2
  exit 64
fi
readonly root="$(git rev-parse --show-toplevel)"
readonly temp="$(mktemp -d)"
cleanup() {
  git -C "$root" worktree remove --force "$temp/rehearsal" >/dev/null 2>&1 || true
  rm -rf "$temp"
}
trap cleanup EXIT

git -C "$root" cat-file -e "$source_sha^{commit}"
git -C "$root" worktree add --detach "$temp/rehearsal" "$source_sha" >/dev/null
cd "$temp/rehearsal"
readonly original_head="$(git rev-parse HEAD)"
readonly parent="$(git rev-parse HEAD^)"
readonly parent_tree="$(git rev-parse "$parent^{tree}")"
[[ "$original_head" == "$source_sha" ]]
git revert --no-commit "$source_sha"
readonly reverted_tree="$(git write-tree)"
[[ "$reverted_tree" == "$parent_tree" ]] || {
  echo 'FAIL: isolated revert did not exactly restore parent tree' >&2
  exit 1
}
expected="$(printf '%s\n' \
  '.github/workflows/v18-c08-04-m05-05-negative.yml' \
  'CHANGELOG_AI.md' \
  'docs/v5/c08-04-m05-05-negative-acceptance.md' \
  'frontend/e2e/c08-04-m05-05-negative.spec.ts' | LC_ALL=C sort)"
actual="$(git diff --cached --name-only | LC_ALL=C sort)"
[[ "$actual" == "$expected" ]] || {
  echo "FAIL: unexpected rollback paths: $actual" >&2
  exit 1
}
if printf '%s\n' "$actual" | grep -Eq '^(backend/src/|frontend/src/|backend/migrations/|database/|infra/)'; then
  echo 'FAIL: a runtime/data path would be modified by the rehearsal' >&2
  exit 1
fi
[[ "$(git rev-parse HEAD)" == "$source_sha" ]] || exit 1
echo "PASS isolated git revert --no-commit: tree=$reverted_tree, source=$source_sha"
echo 'PASS only four test/workflow/doc/CHANGELOG paths changed in an ephemeral worktree'
echo 'NOT_CERTIFIED: live modules, MySQL, JWT, RBAC, tenant A/B, production rollback'
