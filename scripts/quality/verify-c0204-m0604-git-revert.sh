#!/usr/bin/env bash
# C02-04.M06.04 — a real Git revert rehearsal inside a disposable detached worktree.
# Never reverts dev itself or touches business MySQL, QA, main or PROD.
set -euo pipefail
target='d64b19a40983ea3e59d21c5de00652a8635e5daf'
expected_repo='solqaryn/Solqaryn'
repository="${GITHUB_REPOSITORY:-solqaryn/Solqaryn}"
branch="${GITHUB_REF_NAME:-dev}"
if [[ "${1:-}" == '--self-test-rejection' ]]; then
  [[ "$expected_repo" != 'solqaryn/Other' ]]
  [[ "$target" != '0000000000000000000000000000000000000000' ]]
  printf 'ROLLBACK_GUARDS_NEGATIVE=PASS\n'
  exit 0
fi
if [[ "$#" -ne 0 || "$repository" != "$expected_repo" || "$branch" != 'dev' ]]; then
  printf 'ROLLBACK_REJECTED_SCOPE_OR_ARGUMENTS\n' >&2
  exit 41
fi
test "$(git rev-parse --is-inside-work-tree)" = 'true'
git cat-file -e "${target}^{commit}"
git merge-base --is-ancestor "$target" HEAD
expected_paths="$(printf '%s\n' \
  '.github/workflows/v19-c02-04-m05-06-regression.yml' \
  'CHANGELOG_AI.md' \
  'backend/tests/Solqaryn.Tests/API/Controllers/C0204M0502QaHttpContractTests.cs' \
  'docs/v5/c02-04-m05-06-regression-a11y.md' | LC_ALL=C sort)"
actual_paths="$(git diff-tree --no-commit-id --name-only -r "$target" | LC_ALL=C sort)"
if [[ "$actual_paths" != "$expected_paths" ]]; then
  printf 'ROLLBACK_REJECTED_DIFF_SCOPE\n' >&2
  exit 42
fi
# No remote branch, push, reset or live data changes. The worktree starts at the
# exact commit being rehearsed, not at a moving modern dev HEAD.
scratch="$(mktemp -d "${RUNNER_TEMP:-${TMPDIR:-/tmp}}/sq-m0604-XXXXXX")"
worktree="$scratch/worktree"
cleanup() {
  if [[ -d "$worktree" ]]; then git worktree remove --force "$worktree"; fi
  rmdir "$scratch" 2>/dev/null || true
}
trap cleanup EXIT
git worktree add --detach "$worktree" "$target"
if [[ "$(git -C "$worktree" rev-parse HEAD)" != "$target" ]]; then
  printf 'ROLLBACK_REJECTED_WRONG_HEAD\n' >&2
  exit 43
fi
git -C "$worktree" revert --no-commit "$target"
if [[ "$(git -C "$worktree" rev-parse HEAD)" != "$target" ]]; then
  printf 'ROLLBACK_REJECTED_HEAD_MOVED\n' >&2
  exit 44
fi
reverted_paths="$(git -C "$worktree" diff --cached --name-only | LC_ALL=C sort)"
if [[ "$reverted_paths" != "$expected_paths" ]]; then
  printf 'ROLLBACK_REJECTED_STAGED_SCOPE\n' >&2
  exit 45
fi
if [[ -n "$(git -C "$worktree" diff --name-only)" ]]; then
  printf 'ROLLBACK_REJECTED_UNSTAGED_WRITE\n' >&2
  exit 46
fi
parent_sha="$(git rev-parse "${target}^")"
expected_parent_tree="$(git rev-parse "${parent_sha}^{tree}")"
rollback_tree="$(git -C "$worktree" write-tree)"
if [[ "$rollback_tree" != "$expected_parent_tree" ]]; then
  printf 'ROLLBACK_REJECTED_TREE_MISMATCH\n' >&2
  exit 47
fi
printf 'C0204_M0604_ROLLBACK_REHEARSAL=PASS TARGET=%s PARENT=%s RESTORED_TREE=%s CHANGED_FILES=4 BUSINESS_DATA_WRITES=0 REMOTE_BRANCH_WRITES=0\n' \
  "$target" "$parent_sha" "$rollback_tree"
