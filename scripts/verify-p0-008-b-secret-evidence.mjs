import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { resolve, dirname, join } from 'node:path';

// Only reads the already versioned, metadata-only DEV evidence. No provider SDK or secret values.
const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const baselinePath = join(root, 'docs/baselines/P0-008-A-DEV-METADATA-20261029.json');
const validator = join(root, 'scripts/verify-p0-008-a-secret-metadata.mjs');
const baseline = JSON.parse(readFileSync(baselinePath, 'utf8'));

const bannedFields = /^(?:value|raw_value|plaintext|password|passwd|private_key|privateKey|credential|credentialValue|access_token|accessToken|refresh_token|refreshToken|client_secret|clientSecret|api_key|apiKey|secret_value|secretValue|connection_string|connectionString|authorization)$/i;
const bannedValuePatterns = [
  /-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----/i,
  /\bgh[pousr]_[A-Za-z0-9]{16,}\b/,
  /\bsk-[A-Za-z0-9_-]{16,}\b/,
  /\b(?:mysql|mongodb|postgres(?:ql)?):\/\/[^\s:@/]+:[^\s@/]+@/i,
  /\b(?:password|passwd|client_secret|access_token|api_key)\s*[=:]\s*\S{6,}/i,
  /\bBearer\s+[A-Za-z0-9._-]{16,}\b/i,
];
function assertMetadataSafe(node) {
  if (Array.isArray(node)) return node.forEach(assertMetadataSafe);
  if (node && typeof node === 'object') {
    for (const [key, value] of Object.entries(node)) {
      assert.ok(!bannedFields.test(key), 'Forbidden field in metadata-only artifact');
      assertMetadataSafe(value);
    }
  } else if (typeof node === 'string') {
    for (const pattern of bannedValuePatterns) {
      assert.ok(!pattern.test(node), 'Credential-looking value in metadata-only artifact');
    }
  }
}

const upstream = spawnSync(process.execPath, [validator, '--self-test'], {
  cwd: root, encoding: 'utf8', timeout: 30000,
  env: { PATH: process.env.PATH ?? '' },
});
assert.equal(upstream.status, 0, 'P0-008.A metadata contract failed');
assert.match(upstream.stdout, /12\/12 rejected/);
assertMetadataSafe(baseline);

const mutations = [
  x => { x.providers.vercel.variables[0].value = 'SYNTHETIC_ONLY'; },
  x => { x.providers.render.password = 'SYNTHETIC_ONLY'; },
  x => { x.providers.aiven.connectionString = 'SYNTHETIC_ONLY'; },
  x => { x.providers.github.accessToken = 'SYNTHETIC_ONLY'; },
  x => { x.providers.vercel.variables[0].key = ['ghp_', 'a'.repeat(40)].join(''); },
  x => { x.providers.render.workspace = ['-----BEGIN ', 'PRIVATE KEY-----\nSYNTHETIC'].join(''); },
  x => { x.providers.render.serviceName = 'mysql://fakeuser:fakepass@fakehost/dev'; },
  x => { x.providers.aiven.runtimeDatabase = 'sk-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'; },
  x => { x.providers.aiven.runtimeEnvironment = 'Bearer aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'; },
  x => { x.providers.vercel.projectName = 'password=SYNTHETIC_ONLY'; },
];
for (const mutation of mutations) {
  const fixture = structuredClone(baseline);
  mutation(fixture);
  assert.throws(() => assertMetadataSafe(fixture), Error, 'Synthetic secret leak was not rejected');
}
console.log('P0-008.B PASS: inherited 12/12 metadata negatives and 10/10 synthetic credential-leak negatives; no provider access');
