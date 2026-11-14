// Contract tests: CI retains its exact Node/npm pins while Vercel may update patches.
import test from 'node:test';
import assert from 'node:assert/strict';
import { assessToolchain } from './toolchain-policy.mjs';

const manifest = Object.freeze({
  engines: { node: '24.x', npm: '11.19.0' },
  packageManager: 'npm@11.19.0',
});

function check(actualNode, actualNpm, isVercel = false, packageJson = manifest) {
  return assessToolchain({ actualNode, actualNpm, isVercel, packageJson });
}

test('CI local accepts only the certified exact toolchain', () => {
  assert.deepEqual(check('24.21.0', '11.19.0').failures, []);
  assert.equal(check('24.22.1', '11.19.0').failures.length, 1);
  assert.equal(check('24.21.0', '11.19.1').failures.length, 1);
});

test('Vercel accepts its observed patch drift without changing CI pins', () => {
  const accepted = check('24.22.1', '11.19.1', true);
  assert.equal(accepted.mode, 'VERCEL_BOUNDED_24X_11_19X');
  assert.deepEqual(accepted.failures, []);
  assert.deepEqual(check('24.21.0', '11.19.0', true).failures, []);
});

test('Vercel rejects unsupported Node major and pre-baseline minor versions', () => {
  for (const version of ['23.99.9', '24.20.99', '25.0.0', '24.x', '']) {
    assert.ok(check(version, '11.19.1', true).failures.length > 0, version);
  }
});

test('Vercel rejects unreviewed npm minor/major updates and invalid versions', () => {
  for (const version of ['11.18.99', '11.20.0', '12.0.0', '11.19.x', '']) {
    assert.ok(check('24.22.1', version, true).failures.length > 0, version);
  }
});

test('Both modes always enforce the exact repository engine and manager declarations', () => {
  for (const isVercel of [false, true]) {
    const node = isVercel ? '24.22.1' : '24.21.0';
    const npm = isVercel ? '11.19.1' : '11.19.0';
    for (const changed of [
      { engines: { ...manifest.engines, node: '25.x' }, packageManager: manifest.packageManager },
      { engines: { ...manifest.engines, npm: '^11' }, packageManager: manifest.packageManager },
      { engines: manifest.engines, packageManager: 'npm@11.19.1' },
    ]) {
      assert.equal(check(node, npm, isVercel, changed).failures.length, 1);
    }
  }
});
