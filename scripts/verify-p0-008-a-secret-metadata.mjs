import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

const path = new URL("../docs/baselines/P0-008-A-DEV-METADATA-20261026.json", import.meta.url);
const baseline = JSON.parse(readFileSync(path, "utf8"));
const expected = {
  SOLQARYN_PUBLIC_STORE_SIGNING_KEY: ["production"],
  SOLQARYN_PUBLIC_STORE_PROOF_ENABLED: ["production"],
  SEO_INDEXING_ENABLED: ["production", "preview", "development"],
  PUBLIC_ORIGIN: ["production", "preview", "development"],
  API_UPSTREAM: ["production", "preview", "development"],
  SOLQARYN_ENV: ["production", "preview", "development"],
};
const safeKeys = new Set(["schema","project","scope","mode","observedAtUtc","devHead","source","providers","vercel","projectName","projectId","decrypt","variables","key","targets","type","render","workspace","workspaceId","devService","serviceId","branch","envRegistryReadable","github","repository","secretMetadataReadable","conclusion","deploymentParityAccredited","actualSecretValuesInspected","devSecretsComplete"]);

function validate(record) {
  assert.equal(record.schema, "solqaryn-secret-registry-metadata-v1");
  assert.equal(record.project, "SOLQARYN");
  assert.equal(record.scope, "P0-008.A");
  assert.equal(record.mode, "METADATA_ONLY");
  assert.match(record.devHead, /^[0-9a-f]{40}$/);
  assert.equal(record.providers.vercel.projectName, "solqaryn-dev");
  assert.equal(record.providers.vercel.decrypt, "false");
  assert.equal(record.providers.render.devService, "solqaryn-api-dev");
  assert.equal(record.providers.render.branch, "dev");
  assert.equal(record.providers.render.envRegistryReadable, false);
  assert.equal(record.providers.github.secretMetadataReadable, false);
  assert.equal(record.conclusion, "PARTIAL_NO_GO");
  assert.equal(record.deploymentParityAccredited, false);
  assert.equal(record.actualSecretValuesInspected, false);
  assert.equal(record.devSecretsComplete, false);
  const keys = Object.keys(expected);
  const items = record.providers.vercel.variables;
  assert.equal(items.length, keys.length);
  assert.deepEqual(items.map((v) => v.key).sort(), keys.slice().sort());
  for (const item of items) {
    assert.deepEqual(item.targets.slice().sort(), expected[item.key].slice().sort());
    assert.ok(["sensitive", "encrypted"].includes(item.type));
    assert.equal(Object.hasOwn(item, "value"), false);
  }
  function metadataOnly(node) {
    if (Array.isArray(node)) return node.forEach(metadataOnly);
    if (node && typeof node === "object") {
      for (const [k,v] of Object.entries(node)) {
        assert.ok(safeKeys.has(k), "Unexpected metadata key: " + k);
        metadataOnly(v);
      }
    }
  }
  metadataOnly(record);
}
validate(baseline);
console.log("P0-008.A: metadata baseline validated (NO_GO, no secret values).");

if (process.argv.includes("--self-test")) {
  const mutations = [
    (x) => { x.mode = "SECRET_VALUES"; },
    (x) => { x.providers.vercel.decrypt = "true"; },
    (x) => { x.providers.vercel.variables[0].value = "SYNTHETIC"; },
    (x) => { x.providers.vercel.variables.push({...x.providers.vercel.variables[0]}); },
    (x) => { x.providers.vercel.variables[0].targets = ["development"]; },
    (x) => { x.providers.vercel.variables[2].targets = ["production"]; },
    (x) => { x.providers.render.envRegistryReadable = true; },
    (x) => { x.providers.github.secretMetadataReadable = true; },
    (x) => { x.conclusion = "GO"; },
    (x) => { x.deploymentParityAccredited = true; },
    (x) => { x.actualSecretValuesInspected = true; },
    (x) => { x.devSecretsComplete = true; },
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
