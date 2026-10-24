#!/usr/bin/env node
// P0-001.A: validador puramente local de snapshot DEV, sin red, secretos ni escritura.
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import assert from 'node:assert/strict';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const snapshot = JSON.parse(readFileSync(path.join(root, 'docs/baselines/P0-001-A-DEV-20261024.json'), 'utf8'));
const sha = /^[0-9a-f]{40}$/;
export function deploymentStatus(head, deployment) {
  if (!sha.test(head) || !sha.test(deployment?.sha ?? '')) return 'NO_GO';
  if (deployment.state !== 'READY') return 'NO_GO';
  return deployment.sha === head ? 'MATCH' : 'NO_GO';
}
export function validateBaseline(s) {
  assert.equal(s.schema, 'solqaryn.p0-001-a.dev-baseline.v1');
  assert.equal(s.microtask, 'P0-001.A');
  assert.equal(s.repository, 'solqaryn/Solqaryn');
  assert.equal(s.base_branch, 'dev');
  assert.equal(s.scope_lock, 'STRICT');
  assert.match(s.base_head, sha);
  assert.ok(s.observed_at_utc.endsWith('Z'), 'Conservar UTC original');
  assert.equal(s.branches.find(b => b.name === 'dev')?.sha, s.base_head);
  assert.equal(s.branches.find(b => b.name === 'dev')?.protected, true);
  // Snapshot consistency is not provider attestation; live GitHub checks remain authoritative.
  const requiredNames = [
    'verify-solqaryn-project-scope',
    'SOLQARYN - Seguridad multiempresa',
    'SOLQARYN - Build y pruebas backend'
  ];
  assert.deepEqual(s.required_checks.map(c => c.name).sort(), requiredNames.sort());
  assert.ok(s.required_checks.every(c => c.status === 'success'
    && c.head_sha === s.base_head
    && /^https:\/\/github\.com\/solqaryn\/Solqaryn\/actions\/runs\/\d+\/job\/\d+$/.test(c.url)),
    'CI requerido debe referir al commit y repositorio exactos');
  assert.equal(s.providers.render.live_status, 'NOT_VERIFIED', 'Render sin workspace no acredita READY');
  assert.equal(s.providers.render.live_sha, null);
  const v = s.providers.vercel;
  assert.equal(v.dev_project, 'solqaryn-dev');
  assert.equal(v.latest_observed.state, 'CANCELED', 'Nunca presentar el intento cancelado como READY');
  assert.equal(v.latest_observed.sha, s.base_head, 'Último intento debe referir al HEAD observado');
  assert.equal(v.first_ready_in_observed_page.state, 'READY');
  assert.notEqual(v.first_ready_in_observed_page.sha, s.base_head, 'La evidencia observada no acredita READY exact-HEAD');
  assert.equal(deploymentStatus(s.base_head, v.latest_observed), 'NO_GO');
  assert.equal(deploymentStatus(s.base_head, v.first_ready_in_observed_page), 'NO_GO');
  return true;
}
validateBaseline(snapshot);
if (process.argv.includes('--self-test')) {
  const copy = () => structuredClone(snapshot);
  let count = 0;
  for (const mutate of [
    s => { s.base_head = 'invalid'; },
    s => { s.repository = 'foreign/repo'; },
    s => { s.required_checks[0].status = 'failure'; },
    s => { s.required_checks[0].head_sha = '0000000000000000000000000000000000000000'; },
    s => { s.required_checks[0].name = s.required_checks[1].name; },
    s => { s.required_checks[0].url = 'https://github.com.evil.invalid/checks/123'; },
    s => { s.providers.render.live_status = 'READY'; },
    s => { s.providers.vercel.latest_observed.state = 'READY'; },
    s => { s.providers.vercel.first_ready_in_observed_page.sha = s.base_head; }
  ]) {
    const t = copy(); mutate(t);
    assert.throws(() => validateBaseline(t));
    count++;
  }
  assert.equal(deploymentStatus(snapshot.base_head, {state:'READY', sha:snapshot.base_head}), 'MATCH');
  assert.equal(deploymentStatus(snapshot.base_head, {state:'CANCELED', sha:snapshot.base_head}), 'NO_GO');
  console.log('PASS: snapshot consistente; ' + count + ' mutaciones negativas + 2 checks de estado');
} else console.log('PASS: P0-001.A baseline observado, sin afirmar deployment READY exact-HEAD');
