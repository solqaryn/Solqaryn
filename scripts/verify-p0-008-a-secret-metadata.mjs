import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

const path = new URL("../docs/baselines/P0-008-A-DEV-METADATA-20261029.json", import.meta.url);
const baseline = JSON.parse(readFileSync(path, "utf8"));
const expectedVercel = {
  SOLQARYN_PUBLIC_STORE_SIGNING_KEY: ["production"],
  SOLQARYN_PUBLIC_STORE_PROOF_ENABLED: ["production"],
  SEO_INDEXING_ENABLED: ["production", "preview", "development"],
  PUBLIC_ORIGIN: ["production", "preview", "development"],
  API_UPSTREAM: ["production", "preview", "development"],
  SOLQARYN_ENV: ["production", "preview", "development"],
};
const safeKeys = new Set([
  "schema", "project", "scope", "mode", "observedAtUtc", "devHead", "source",
  "providers", "vercel", "projectName", "projectId", "decrypt", "variables",
  "key", "targets", "type", "render", "workspace", "workspaceId", "environment",
  "serviceName", "serviceId", "branch", "liveCommit", "linkedEnvironmentGroups",
  "observedVariableCount", "managedKeyCount", "expectedManagedKeyCount", "runtimeContract",
  "runtimeContractLogTimesUtc", "databaseReadiness", "aiven", "status", "engine", "version",
  "host", "port", "sslMode", "databases", "serviceUsers", "runtimeBindingVerified",
  "runtimeEnvironment", "runtimeDatabase", "runtimeUser", "runtimeCredentialValueInspected",
  "github", "repository", "environmentSecrets", "environment", "repositorySecrets",
  "organizationSecretsAvailable", "secretValuesInspected", "conclusion",
  "deploymentParityAccredited", "actualSecretValuesInspected", "devSecretsComplete", "remainingGates",
]);

function validate(record) {
  assert.equal(record.schema, "solqaryn-secret-registry-metadata-v2");
  assert.equal(record.project, "SOLQARYN");
  assert.equal(record.scope, "P0-008.A");
  assert.equal(record.mode, "METADATA_ONLY");
  assert.match(record.devHead, /^[0-9a-f]{40}$/);
  assert.equal(record.providers.vercel.projectName, "solqaryn-dev");
  assert.equal(record.providers.vercel.decrypt, "false");
  const variables = record.providers.vercel.variables;
  assert.equal(variables.length, Object.keys(expectedVercel).length);
  assert.deepEqual(variables.map((item) => item.key).sort(), Object.keys(expectedVercel).sort());
  for (const item of variables) {
    assert.deepEqual(item.targets.slice().sort(), expectedVercel[item.key].slice().sort());
    assert.ok(["sensitive", "encrypted"].includes(item.type));
    assert.equal(Object.hasOwn(item, "value"), false);
  }
  const render = record.providers.render;
  assert.equal(render.serviceName, "solqaryn-api-dev");
  assert.equal(render.branch, "dev");
  assert.equal(render.liveCommit, record.devHead);
  assert.equal(render.observedVariableCount, 52);
  assert.equal(render.managedKeyCount, render.expectedManagedKeyCount);
  assert.equal(render.runtimeContract, "PASS");
  assert.equal(render.databaseReadiness, "ready/connected");
  const aiven = record.providers.aiven;
  assert.equal(aiven.runtimeBindingVerified, true);
  assert.equal(aiven.runtimeEnvironment, "Development");
  assert.equal(aiven.runtimeDatabase, "solqaryn_dev");
  assert.equal(aiven.runtimeUser, "solqaryn_dev_user");
  assert.equal(aiven.runtimeCredentialValueInspected, false);
  assert.equal(record.providers.github.secretValuesInspected, false);
  assert.equal(record.conclusion, "PARTIAL_NO_GO");
  assert.equal(record.deploymentParityAccredited, false);
  assert.equal(record.actualSecretValuesInspected, false);
  assert.equal(record.devSecretsComplete, false);
  function metadataOnly(node) {
    if (Array.isArray(node)) return node.forEach(metadataOnly);
    if (node && typeof node === "object") {
      for (const [key, value] of Object.entries(node)) {
        assert.ok(safeKeys.has(key), "Unexpected metadata key: " + key);
        metadataOnly(value);
      }
    }
  }
  metadataOnly(record);
}

validate(baseline);
console.log("P0-008.A: latest metadata baseline validated (partial, no secret values).");

if (process.argv.includes("--self-test")) {
  const mutations = [
    (x) => { x.mode = "SECRET_VALUES"; },
    (x) => { x.providers.vercel.decrypt = "true"; },
    (x) => { x.providers.vercel.variables[0].value = "SYNTHETIC"; },
    (x) => { x.providers.vercel.variables.push({ ...x.providers.vercel.variables[0] }); },
    (x) => { x.providers.vercel.variables[0].targets = ["development"]; },
    (x) => { x.providers.render.runtimeContract = "FAIL"; },
    (x) => { x.providers.render.databaseReadiness = "disconnected"; },
    (x) => { x.providers.aiven.runtimeDatabase = "solqaryn_prod"; },
    (x) => { x.providers.aiven.runtimeCredentialValueInspected = true; },
    (x) => { x.providers.github.secretValuesInspected = true; },
    (x) => { x.conclusion = "GO"; },
    (x) => { x.actualSecretValuesInspected = true; },
  ];
  let rejected = 0;
  for (const mutate of mutations) {
    const copy = structuredClone(baseline);
    mutate(copy);
    try { validate(copy); } catch { rejected++; }
  }
  assert.equal(rejected, mutations.length, "Every negative mutation must fail");
  console.log("P0-008.A negative mutations: " + rejected + "/" + mutations.length + " rejected.");
}
