#!/usr/bin/env node
// C06-01.M06.04: genuine isolated Git rollback of an actual SOLQARYN landing commit.
import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { createHash } from "node:crypto";
import { existsSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const target = "9b90aa0df48d155545351a35ca825bd096c4a761";
const parent = "17a18521773539138b884cf115b0b71788225822";
const route = "frontend/src/app/app.routes.ts";
const landing = "frontend/src/app/features/landing/c0601-public-saas-landing.component.ts";
const expectedPaths = [
  ".github/workflows/v22-c06-01-m04-01-public-landing.yml",
  "docs/v5/c06-01-m04-01-public-saas-landing-v22.md",
  "frontend/e2e/c06-01-m04-01-public-saas-landing.spec.ts",
  route, landing
].sort();

function git(args, cwd = root) {
  return execFileSync("git", args, {cwd, encoding:"utf8", stdio:["ignore","pipe","pipe"], timeout:120000}).trimEnd();
}
function assertExactTarget(value) {
  assert.match(value, /^[a-f0-9]{40}$/, "An immutable full SHA is required");
  assert.equal(value, target, "Ref or SHA not allowlisted for this rehearsal");
}
function names(s) { return s.split(/\r?\n/).filter(Boolean).sort(); }
function digest(x) { return createHash("sha256").update(x).digest("hex"); }

if (process.argv.includes("--self-test")) {
  for (const bad of ["", "../main", "main", "f".repeat(40), target.slice(0,12)]) {
    assert.throws(() => assertExactTarget(bad));
  }
  assert.doesNotThrow(() => assertExactTarget(target));
  assert.deepEqual(names("b\na\n"), ["a","b"]);
  console.log("PASS: five adversarial SHA/ref inputs rejected; exact SHA accepted");
  process.exit(0);
}
assert.equal(process.argv.length,2,"Unexpected arguments refused");
assertExactTarget(target);
assert.equal(git(["rev-parse",target]),target);
assert.equal(git(["rev-parse",target+"^"]),parent);
git(["merge-base","--is-ancestor",target,"HEAD"]);
const primaryHEAD=git(["rev-parse","HEAD"]);
const primaryStatus=git(["status","--porcelain","--untracked-files=no"]);
assert.equal(primaryStatus,"","Primary checkout is not clean");
assert.deepEqual(names(git(["diff-tree","--no-commit-id","--name-only","-r",target])),expectedPaths);
assert.match(git(["show",target+":"+route]),/conoce-solqaryn/);
assert.match(git(["show",target+":"+landing]),/Solqaryn/i);
const originalRoute=git(["show",parent+":"+route]);

const temporary=mkdtempSync(join(tmpdir(),"sq-c0601-revert-"));
let worktreeAdded=false;
try {
  git(["worktree","add","--detach",temporary,target]);
  worktreeAdded=true;
  assert.equal(git(["rev-parse","HEAD"],temporary),target);
  assert.throws(() => git(["symbolic-ref","-q","HEAD"],temporary),"Worktree must remain detached");

  const syntheticDataPath=join(temporary,"__legitimate_data_fixture__.json");
  writeFileSync(syntheticDataPath,'{"tenant_A":1,"tenant_B":2,"preserve":true}\n');
  const before=digest(readFileSync(syntheticDataPath));
  git(["revert","--no-commit",target],temporary);
  const inversePaths=names(git(["diff","--cached","--name-only"],temporary));
  assert.deepEqual(inversePaths,expectedPaths,"Unexpected tracked files changed by revert");
  assert.equal(git(["show",":"+route],temporary),originalRoute,"Route not restored exactly");
  assert.equal(existsSync(join(temporary,landing)),false,"Landing component not removed in isolated worktree");
  assert.equal(digest(readFileSync(syntheticDataPath)),before,"Unrelated fixture data changed");
  assert.equal(git(["rev-parse","HEAD"],temporary),target,"Rehearsal created a commit");
  assert.equal(git(["rev-parse","HEAD"]),primaryHEAD,"Primary HEAD changed");
  assert.equal(git(["status","--porcelain","--untracked-files=no"]),primaryStatus,"Primary checkout changed");
  console.log("PASS: actual git revert --no-commit; detached isolated worktree only");
  console.log("SOURCE_SHA="+target+" PARENT_SHA="+parent+" REVERTED_PATHS="+inversePaths.length);
  console.log("FIXTURE_SHA256="+before+" SHARED_BRANCH_WRITES=0 DATABASE_WRITES=0 REMOTE_WRITES=0");
} finally {
  if (worktreeAdded) {
    try { git(["worktree","remove","--force",temporary]); } catch {}
  }
  if (existsSync(temporary)) rmSync(temporary,{recursive:true,force:true});
}
