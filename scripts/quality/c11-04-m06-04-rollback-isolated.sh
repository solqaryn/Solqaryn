#!/usr/bin/env bash
set -euo pipefail
# SAFETY: worktree-only reversal; never alter dev refs, production, or data.
source_sha=e8a2663ccec7964a005ca7b73bdebebc0158ddb0
parent_sha=91a0485a696feb72d825ccb111afa2703e535fbf
original_head="$(git rev-parse HEAD)"
[[ "$(git rev-parse "$source_sha^")" == "$parent_sha" ]] || { echo PARENT_MISMATCH; exit 1; }
[[ -z "$(git status --porcelain)" ]] || { echo DIRTY_PRIMARY; exit 1; }
count="$(git diff --name-only "$parent_sha" "$source_sha" | wc -l | tr -d ' ')"
[[ "$count" == 4 ]] || { echo UNEXPECTED_SCOPE_COUNT; exit 1; }
while IFS= read -r path; do
  case "$path" in
    .github/workflows/v18-c11-04-m05-06-regression.yml|CHANGELOG_AI.md|docs/v5/c11-04-m05-06-regression-performance-a11y.md|frontend/e2e/c11-04-m05-06-regression.spec.ts) ;;
    *) echo "UNEXPECTED_PATH=$path"; exit 1 ;;
  esac
done < <(git diff --name-only "$parent_sha" "$source_sha")
tmp="$(mktemp -d)"
w="$tmp/detached"
cleanup() {
  if [[ -f "$w/.git" ]]; then git worktree remove --force "$w" || true; fi
  rm -rf "$tmp"
}
trap cleanup EXIT
git worktree add --detach "$w" "$source_sha"
[[ -z "$(git -C "$w" status --porcelain)" ]] || exit 1
git -C "$w" -c user.name="GitHub CI" -c user.email="ci@example.invalid" revert --no-commit "$source_sha"
want="$(git rev-parse "$parent_sha^{tree}")"
got="$(git -C "$w" write-tree)"
[[ "$want" == "$got" ]] || { echo REVERSE_TREE_MISMATCH; exit 1; }
[[ "$original_head" == "$(git rev-parse HEAD)" ]] || { echo HEAD_CHANGED; exit 1; }
[[ -z "$(git status --porcelain)" ]] || { echo PRIMARY_DIRTY; exit 1; }
echo "ROLLBACK_ISOLATED_PASS=1;SCOPE_FILES=4;SOURCE_SHA=$source_sha;PARENT_SHA=$parent_sha"
echo "MYSQL_RESTORE=NOT_TESTED;PROD=NOT_TOUCHED;JWT=NOT_TESTED"
