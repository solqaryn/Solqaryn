import assert from 'node:assert/strict';
import { existsSync, readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { brotliCompressSync, gzipSync } from 'node:zlib';
import { test } from 'node:test';

/** C05-04.M05.03: integra el build real Angular con el reporte de bundle,
 * sin alterar runtime, permisos, proveedor real ni contratos HTTP.
 */
const dist = resolve(process.cwd(), 'dist/solqaryn-frontend');
const root = existsSync(join(dist, 'browser')) ? join(dist, 'browser') : dist;
const report = JSON.parse(readFileSync(join(dist, 'performance-bundle-baseline.json'), 'utf8'));
const index = readFileSync(join(root, 'index.html'), 'utf8');

function references(html) {
  return [...new Set([...html.matchAll(/(?:src|href)=["']([^"']+\.(?:js|css))["']/g)]
    .map(match => match[1].replace(/^\.?\//, '')))];
}

function audit(data, html) {
  const refs = references(html);
  assert.ok(refs.length > 0, 'index productivo debe referenciar JS/CSS inicial real');
  assert.equal(data.initial.fileCount, data.initial.files.length);
  assert.deepEqual(
    [...new Set(data.initial.files.map(file => file.file))].sort(),
    [...refs].sort(),
    'el reporte debe describir todos y solo los assets iniciales del HTML'
  );
  let raw = 0;
  let gzip = 0;
  let brotli = 0;
  for (const file of data.initial.files) {
    assert.ok(!file.file.startsWith('/') && !file.file.includes('..'), 'ruta de asset segura');
    const fullPath = join(root, file.file);
    assert.ok(existsSync(fullPath), 'asset inicial debe existir: ' + file.file);
    const bytes = readFileSync(fullPath);
    assert.equal(file.rawBytes, bytes.length, 'rawBytes verificados contra disco');
    assert.equal(file.gzipBytes, gzipSync(bytes).length, 'gzip real');
    assert.equal(file.brotliBytes, brotliCompressSync(bytes).length, 'brotli real');
    raw += bytes.length;
    gzip += gzipSync(bytes).length;
    brotli += brotliCompressSync(bytes).length;
  }
  assert.equal(data.initial.rawBytes, raw);
  assert.equal(data.initial.gzipBytes, gzip);
  assert.equal(data.initial.brotliBytes, brotli);
  assert.ok(data.allBundles.rawBytes >= raw, 'no contar lazy como initial');
  assert.equal(data.existingInitialBudget.warningBytes, 1024 * 1024);
  assert.equal(data.existingInitialBudget.errorBytes, 2 * 1024 * 1024);
  return { files: refs.length, initialBytes: raw };
}

test('C05-04.M05.03: build real y reporte coinciden byte a byte en assets iniciales', () => {
  const actual = audit(report, index);
  assert.ok(actual.files > 0);
  assert.ok(actual.initialBytes > 0);
  console.info('INITIAL_BYTES_MEASURED=' + actual.initialBytes + ';FIELD_CWV=NO_MEDIDO');
});

test('C05-04.M05.03: reporte manipulado con bytes incorrectos no se certifica', () => {
  const tampered = structuredClone(report);
  tampered.initial.rawBytes += 1;
  assert.throws(() => audit(tampered, index), /Expected values to be strictly equal/);
});

test('C05-04.M05.03: asset de proveedor inexistente no se acepta como integrado', () => {
  const fakeHtml = index + '<script src="missing-v18-provider.js"></script>';
  assert.throws(() => audit(report, fakeHtml), /reporte debe describir/);
});

test('C05-04.M05.03: indice sin dependencias reales no puede reportar un PASS', () => {
  assert.throws(() => audit(report, '<!doctype html><title>vacio</title>'),
    /debe referenciar JS\/CSS inicial real/);
});
