// PROJECT_SCOPE_LOCK=STRICT
// EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT
// PROJECT_SCOPE_POLICY=docs/PROJECT_SCOPE_LOCK.md
// PROJECT_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
// EXTERNAL_SKILL_REGISTRY=docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md
// LOCAL_SKILL_COUNT=1
// Prohibido usar skills/documentacion/contexto fuera de SOLQARYN sin autorizacion explicita o allowlist ACTIVE.

import crypto from 'node:crypto';
import fs from 'node:fs';
import process from 'node:process';

const REQUIRED = ['projectId', 'repository', 'branch', 'taskId', 'parentId', 'commitSha', 'workflow', 'runId', 'conclusion', 'timestamp', 'artifactRefs', 'p0', 'p1'];
function parseArgs(argv) { const result = {}; for (let i = 0; i < argv.length; i += 1) { if (!argv[i].startsWith('--')) continue; const key = argv[i].slice(2); const next = argv[i + 1]; result[key] = next === undefined || next.startsWith('--') ? true : next; if (result[key] !== true) i += 1; } return result; }
function validate(payload) {
  const errors = REQUIRED.filter((field) => !(field in payload));
  if (payload.projectId !== 'SOLQARYN') errors.push('projectId must be SOLQARYN');
  if (payload.repository !== 'solqaryn/Solqaryn') errors.push('repository mismatch');
  if (payload.branch !== 'dev') errors.push('branch must be dev');
  if (!/^[0-9a-f]{40}$/.test(payload.commitSha ?? '')) errors.push('commitSha must be full SHA');
  if (!Number.isInteger(payload.p0) || !Number.isInteger(payload.p1) || payload.p0 < 0 || payload.p1 < 0) errors.push('p0/p1 must be non-negative integers');
  if (!Array.isArray(payload.artifactRefs)) errors.push('artifactRefs must be an array');
  return errors;
}
async function main() {
  const opts = parseArgs(process.argv.slice(2));
  if (!opts.payload) { console.error('usage: node scripts/vaep/sync-bitacora.mjs --payload <file>'); process.exitCode = 2; return; }
  let payload;
  try { payload = JSON.parse(fs.readFileSync(opts.payload, 'utf8')); } catch (error) { console.error(`invalid payload JSON: ${error.message}`); process.exitCode = 2; return; }
  const errors = validate(payload);
  if (errors.length) { console.error(JSON.stringify({ outcome: 'REJECTED', errors })); process.exitCode = 2; return; }
  const url = process.env.VAEP_BITACORA_WEBHOOK_URL;
  const token = process.env.VAEP_BITACORA_WEBHOOK_TOKEN;
  if (!url || !token) { console.log(JSON.stringify({ outcome: 'SKIPPED', reason: 'VAEP_BITACORA_WEBHOOK_URL or token is not configured' })); return; }
  const body = JSON.stringify(payload);
  const key = `${payload.taskId}:${payload.commitSha}:${payload.workflow}/${payload.runId}`;
  const signature = process.env.VAEP_BITACORA_HMAC_SECRET ? crypto.createHmac('sha256', process.env.VAEP_BITACORA_HMAC_SECRET).update(body).digest('hex') : undefined;
  let lastError = 'unknown error';
  for (let attempt = 1; attempt <= 3; attempt += 1) {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 10000);
    try {
      const headers = { 'content-type': 'application/json', authorization: `Bearer ${token}`, 'x-idempotency-key': key };
      if (signature) headers['x-signature-sha256'] = signature;
      const response = await fetch(url, { method: 'POST', headers, body, signal: controller.signal });
      if (response.ok) { console.log(JSON.stringify({ outcome: 'SYNCED', attempts: attempt, idempotencyKey: key })); return; }
      lastError = `HTTP ${response.status}`;
    } catch (error) { lastError = error.name === 'AbortError' ? 'timeout' : error.message; }
    clearTimeout(timeout);
    if (attempt < 3) await new Promise((resolve) => setTimeout(resolve, 250 * attempt));
  }
  console.error(JSON.stringify({ outcome: 'FAILED', error: lastError, attempts: 3 })); process.exitCode = 2;
}
main();
