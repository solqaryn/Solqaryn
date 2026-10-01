import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { execFileSync } from 'node:child_process';

const root = process.cwd();
const temp = fs.mkdtempSync(path.join(os.tmpdir(), 'solqaryn-vaep-block3-'));
const sha = '0123456789abcdef0123456789abcdef01234567';
const invoke = (script, args, env = {}) => {
  try { return JSON.parse(execFileSync(process.execPath, [path.join(root, 'scripts', 'vaep', script), ...args], { cwd: root, encoding: 'utf8', env: { ...process.env, ...env, VAEP_BITACORA_WEBHOOK_URL: undefined, VAEP_BITACORA_WEBHOOK_TOKEN: undefined } })); }
  catch (error) { return JSON.parse(error.stdout); }
};
const payload = path.join(temp, 'payload.json');
fs.writeFileSync(payload, JSON.stringify({ projectId: 'SOLQARYN', repository: 'solqaryn/Solqaryn', branch: 'dev', taskId: 'N4.7.A.PREFLIGHT', parentId: 'N4.7.A', commitSha: sha, workflow: 'self-test', runId: '1', conclusion: 'success', timestamp: '2026-09-04T00:00:00Z', artifactRefs: [], p0: 0, p1: 0 }));
const sync = invoke('sync-bitacora.mjs', ['--payload', payload]);
const readyInput = path.join(temp, 'ready.json');
fs.writeFileSync(readyInput, JSON.stringify({ parentId: 'N4.7.A', currentHead: sha, gates: ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H'].map((id) => ({ id, head: sha, status: 'SUCCESS' })), p0: 0, p1: 0, documentationPresent: true, ownershipConflict: false, dependenciesSatisfied: true }));
const heldInput = path.join(temp, 'held.json');
fs.writeFileSync(heldInput, JSON.stringify({ ...JSON.parse(fs.readFileSync(readyInput, 'utf8')), gates: [{ id: 'G', head: sha, status: 'PENDING' }] }));
const ready = invoke('reconcile-status.mjs', ['--input', readyInput]);
const held = invoke('reconcile-status.mjs', ['--input', heldInput]);
const events = path.join(temp, 'events.json');
fs.writeFileSync(events, JSON.stringify([{ dispatches: 3, sessions: 2, attempts: 2, bundleSuccesses: 1, docOnlyCommits: 1 }]));
const metrics = invoke('metrics.mjs', ['--events', events]);
const checks = [
  ['sync missing config is explicit skip', sync.outcome === 'SKIPPED'],
  ['reconciler keeps controller authority', ready.outcome === 'ELIGIBLE_FOR_CONTROLLER_REVIEW' && ready.autoPromote === false],
  ['reconciler holds incomplete gates', held.outcome === 'PROMOTION_HELD'],
  ['metrics aggregate observable counters', metrics.totals.dispatches === 3 && metrics.totals.bundleSuccesses === 1]
];
fs.rmSync(temp, { recursive: true, force: true });
for (const [name, ok] of checks) console.log(`${ok ? 'PASS' : 'FAIL'} ${name}`);
if (checks.some(([, ok]) => !ok)) process.exitCode = 1;
