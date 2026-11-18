// C14-04.M05.03: real GitHub Checks read-only evidence. Never grants a release.
const REQUIRED = ["verify-solqaryn-project-scope", "V5 - Sin encabezados vacios ni atribucion cruzada"];
const SHA = /^[0-9a-f]{40}$/;
export function endpoint(sha) {
  if (!SHA.test(sha)) throw new Error("GITHUB_CHECKS_SHA_INVALID");
  return "https://api.github.com/repos/solqaryn/Solqaryn/commits/" + sha + "/check-runs?per_page=100";
}
export function classify(payload, sha) {
  const deny = (code, count=0) => ({code, count, allowsRelease:false, allowsProduction:false, certifiesTenant:false});
  if (!SHA.test(sha)) return deny("INVALID_SHA");
  if (!payload || !Array.isArray(payload.check_runs) || !Number.isInteger(payload.total_count) ||
      payload.total_count < payload.check_runs.length || payload.total_count < 1)
    return deny("INVALID_PROVIDER_RESPONSE");
  const checks = payload.check_runs;
  if (payload.total_count > checks.length) return deny("INCOMPLETE_PAGE", checks.length);
  for (const name of REQUIRED) {
    const matches = checks.filter(c => c && c.name === name);
    if (matches.length !== 1) return deny("MISSING_OR_DUPLICATE_REQUIRED", checks.length);
    const c = matches[0];
    if (c.head_sha !== sha || c.status !== "completed" || c.conclusion !== "success")
      return deny("REQUIRED_CHECK_NOT_SUCCESS", checks.length);
  }
  if (checks.some(c => !c || c.head_sha !== sha || c.status !== "completed" ||
      !["success", "skipped"].includes(c.conclusion)))
    return deny("OTHER_CHECKS_UNRESOLVED", checks.length);
  return deny("CHECKS_READ_ONLY_VERIFIED", checks.length);
}
export async function readChecks(sha, token, fetcher=fetch) {
  const url=endpoint(sha);
  if (!token) throw new Error("GITHUB_CHECKS_TOKEN_MISSING");
  let response;
  try {
    response=await fetcher(url,{method:"GET",headers:{
      Accept:"application/vnd.github+json",Authorization:"Bearer "+token,
      "X-GitHub-Api-Version":"2022-11-28"
    },signal:AbortSignal.timeout(15000)});
  } catch { throw new Error("GITHUB_CHECKS_PROVIDER_UNAVAILABLE"); }
  if (!response.ok) throw new Error("GITHUB_CHECKS_HTTP_"+response.status);
  let payload;
  try { payload=await response.json(); } catch { throw new Error("GITHUB_CHECKS_JSON_INVALID"); }
  return classify(payload,sha);
}
if (process.argv.includes("--live")) {
  (async()=>{
    if (process.env.GITHUB_REPOSITORY!=="solqaryn/Solqaryn" ||
        process.env.GITHUB_REF!=="refs/heads/dev")
      throw new Error("GITHUB_CHECKS_DEV_SCOPE_REQUIRED");
    const sha=process.env.GITHUB_SHA||"";
    const result=await readChecks(sha,process.env.GITHUB_TOKEN);
    if (result.code==="INVALID_PROVIDER_RESPONSE") throw new Error("GITHUB_CHECKS_SCHEMA_INVALID");
    if (result.allowsRelease || result.allowsProduction || result.certifiesTenant)
      throw new Error("GITHUB_CHECKS_AUTHORITY_VIOLATION");
    console.log("C1404_M0503_PROVIDER_READ_SUCCESS;SHA="+sha+
      ";OBSERVED="+result.count+";EVIDENCE="+result.code+
      ";RELEASE_AUTH=false;PROD_AUTH=false");
  })().catch(e=>{console.error(e.message?.startsWith("GITHUB_CHECKS_")?e.message:
    "GITHUB_CHECKS_UNKNOWN_ERROR");process.exitCode=1;});
}
