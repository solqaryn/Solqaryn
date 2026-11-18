import test from "node:test";
import assert from "node:assert/strict";
import {endpoint,classify,readChecks} from "./v19-c14-04-m05-03-provider.mjs";
const sha="a".repeat(40);
const check=(name,overrides={})=>({name,head_sha:sha,status:"completed",conclusion:"success",...overrides});
const response=()=>({total_count:2,check_runs:[
  check("verify-solqaryn-project-scope"),
  check("V5 - Sin encabezados vacios ni atribucion cruzada")
]});
const safe=x=>{assert.equal(x.allowsRelease,false);assert.equal(x.allowsProduction,false);assert.equal(x.certifiesTenant,false);};
test("exact-SHA successful checks remain read-only without release authority",()=>{
  const r=classify(response(),sha);assert.equal(r.code,"CHECKS_READ_ONLY_VERIFIED");safe(r);
});
test("queued in-progress cancelled failed and skipped required checks fail closed",()=>{
  for(const [status,conclusion] of [["queued",null],["in_progress",null],
      ["completed","cancelled"],["completed","failure"],["completed","skipped"]]){
    const p=response();p.check_runs[0]=check("verify-solqaryn-project-scope",{status,conclusion});
    const r=classify(p,sha);assert.equal(r.code,"REQUIRED_CHECK_NOT_SUCCESS");safe(r);
  }
});
test("foreign SHA and invalid SHA cannot be reused",()=>{
  const p=response();p.check_runs[0].head_sha="b".repeat(40);
  assert.equal(classify(p,sha).code,"REQUIRED_CHECK_NOT_SUCCESS");
  assert.equal(classify(p,"untrusted/sha").code,"INVALID_SHA");
  assert.throws(()=>endpoint("../path"),/SHA_INVALID/);
});
test("missing, duplicate or paginated required evidence fails",()=>{
  const p=response();p.check_runs.push({...p.check_runs[0]});p.total_count=3;
  assert.equal(classify(p,sha).code,"MISSING_OR_DUPLICATE_REQUIRED");
  const missing=response();missing.check_runs.shift();missing.total_count=1;
  assert.equal(classify(missing,sha).code,"MISSING_OR_DUPLICATE_REQUIRED");
  const paged=response();paged.total_count=101;
  assert.equal(classify(paged,sha).code,"INCOMPLETE_PAGE");
});
test("invalid API response and other failed or pending check fail closed",()=>{
  assert.equal(classify({total_count:1},sha).code,"INVALID_PROVIDER_RESPONSE");
  for(const status of ["failure","in_progress"]){
    const p=response();p.check_runs.push(check("extra-gate",{
      status:status==="failure"?"completed":status,
      conclusion:status==="failure"?"failure":null
    }));p.total_count=3;
    assert.equal(classify(p,sha).code,"OTHER_CHECKS_UNRESOLVED");
  }
});
test("authenticated real-provider call contract has fixed URL and GET, no writes",async()=>{
  const r=await readChecks(sha,"synthetic-token",async(url,options)=>{
    assert.equal(url,endpoint(sha));assert.equal(options.method,"GET");
    assert.equal(options.headers.Authorization,"Bearer synthetic-token");
    return {ok:true,json:async()=>response()};
  });
  assert.equal(r.code,"CHECKS_READ_ONLY_VERIFIED");safe(r);
  await assert.rejects(()=>readChecks(sha,"synthetic-token",async()=>({ok:false,status:403})),
    /GITHUB_CHECKS_HTTP_403/);
  await assert.rejects(()=>readChecks(sha,""),/GITHUB_CHECKS_TOKEN_MISSING/);
});
