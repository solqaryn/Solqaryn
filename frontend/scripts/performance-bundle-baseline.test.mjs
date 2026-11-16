import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtempSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { test } from 'node:test';

const analyzer = fileURLToPath(new URL('./performance-bundle-baseline.mjs', import.meta.url));
const MiB = 1024 * 1024;

function fixture(mainBytes, { index = true, lazy = false } = {}) {
  const root = mkdtempSync(join(tmpdir(), 'solqaryn-v18-m0501-'));
  const browser = join(root, 'dist', 'solqaryn-frontend', 'browser');
  mkdirSync(browser, { recursive: true });
  if (index) {
    writeFileSync(join(browser, 'index.html'),
      '<!doctype html><script src="main.js"></script><link href="styles.css" rel="stylesheet">');
  }
  writeFileSync(join(browser, 'main.js'), 'x'.repeat(mainBytes));
  writeFileSync(join(browser, 'styles.css'), '');
  if (lazy) writeFileSync(join(browser, 'lazy.js'), 'y'.repeat(173));
  return root;
}

function run(root) {
  return spawnSync(process.execPath, [analyzer], {
    cwd: root, encoding: 'utf8', maxBuffer: 4 * MiB, timeout: 20000
  });
}

function report(root) {
  return JSON.parse(readFileSync(join(root, 'dist', 'solqaryn-frontend',
    'performance-bundle-baseline.json'), 'utf8'));
}

test('C05-04.M05.01: el baseline clasifica la entrada real y excluye el bundle lazy', () => {
  const root = fixture(300, { lazy: true });
  try {
    const result = run(root);
    assert.equal(result.status, 0, result.stderr);
    const data = report(root);
    assert.equal(data.initial.rawBytes, 300);
    assert.equal(data.initial.fileCount, 2);
    assert.equal(data.allBundles.rawBytes, 473);
    assert.ok(data.initial.gzipBytes > 0);
    assert.ok(data.initial.brotliBytes > 0);
    assert.ok(data.initial.gzipBytes < data.initial.rawBytes);
    assert.equal(data.initial.files.some(file => file.file === 'lazy.js'), false);
    assert.equal(data.allBundles.largest.some(file => file.file === 'lazy.js'), true);
  } finally { rmSync(root, { recursive: true, force: true }); }
});

for (const bytes of [MiB - 1, MiB, 2 * MiB, 2 * MiB + 1]) {
  test('C05-04.M05.01: frontera de bytes iniciales ' + bytes, () => {
    const root = fixture(bytes);
    try {
      const result = run(root);
      assert.equal(result.status, 0, result.stderr);
      const data = report(root);
      assert.equal(data.initial.rawBytes, bytes);
      assert.equal(data.existingInitialBudget.warningBytes, MiB);
      assert.equal(data.existingInitialBudget.errorBytes, 2 * MiB);
      assert.equal(typeof data.initial.rawBytes, 'number');
    } finally { rmSync(root, { recursive: true, force: true }); }
  });
}

test('C05-04.M05.01: no inventa medición cuando falta index.html', () => {
  const root = fixture(100, { index: false });
  try {
    const result = run(root);
    assert.notEqual(result.status, 0);
    assert.match(result.stderr, /No se encontró el build productivo/);
  } finally { rmSync(root, { recursive: true, force: true }); }
});

test('C05-04.M05.01: entrada sin assets referenciados se informa como cero, no se infiere tamaño', () => {
  const root = fixture(100);
  try {
    writeFileSync(join(root, 'dist', 'solqaryn-frontend', 'browser', 'index.html'),
      '<!doctype html><title>Sin referencia</title>');
    const result = run(root);
    assert.equal(result.status, 0, result.stderr);
    const data = report(root);
    assert.equal(data.initial.fileCount, 0);
    assert.equal(data.initial.rawBytes, 0);
    assert.ok(data.allBundles.rawBytes >= 100);
  } finally { rmSync(root, { recursive: true, force: true }); }
});
