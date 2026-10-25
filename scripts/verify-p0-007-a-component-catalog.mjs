#!/usr/bin/env node
// P0-007.A: read-only file catalog drift check, NOT UX/E2E or RBAC certification.
import { readFileSync, readdirSync } from 'node:fs';
import { dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const docPath = join(root, 'docs/P0-007-A-INVENTARIO-COMPONENTES-DEV.md');
const doc = readFileSync(docPath, 'utf8');
const canonicalize = (paths) => [...new Set(paths)].sort();
const expectedRaw = [...doc.matchAll(/^- `(frontend\/src\/app\/(?:features|shared)\/[^`]+\.component\.ts)`$/gm)]
  .map(match => match[1]);
const expected = canonicalize(expectedRaw);

function walk(dirnamePath) {
  const paths = [];
  for (const entry of readdirSync(dirnamePath, { withFileTypes: true })) {
    const absolute = join(dirnamePath, entry.name);
    if (entry.isDirectory()) paths.push(...walk(absolute));
    else if (entry.isFile() && entry.name.endsWith('.component.ts')) {
      paths.push(relative(root, absolute).replaceAll('\\', '/'));
    }
  }
  return paths;
}

function comparePaths(actualPaths, documentedPaths) {
  const actualSet = new Set(actualPaths);
  const documentedSet = new Set(documentedPaths);
  return {
    missing: documentedPaths.filter(path => !actualSet.has(path)),
    unlisted: actualPaths.filter(path => !documentedSet.has(path)),
  };
}
function requirePass(ok, description) { if (!ok) throw new Error('P0-007.A FAIL: ' + description); }

requirePass(expected.length === 137, '137 documented source paths');
requirePass(expectedRaw.length === expected.length, 'manifest entries must be unique');
requirePass(expected.every(path => /^frontend\/src\/app\/(features|shared)\//.test(path)),
  'features/shared-only scope');

const actual = canonicalize([
  ...walk(join(root, 'frontend/src/app/features')),
  ...walk(join(root, 'frontend/src/app/shared')),
]);
const { missing, unlisted } = comparePaths(actual, expected);
requirePass(missing.length === 0 && unlisted.length === 0,
  'catalog drift: missing=' + missing.join(', ') + '; unlisted=' + unlisted.join(', '));

if (process.argv.includes('--self-test')) {
  const removed = comparePaths(actual.slice(1), expected);
  requirePass(removed.missing.length === 1, 'a deleted file must fail the comparison');
  const inserted = comparePaths([...actual, 'frontend/src/app/features/__fixture__/falso.component.ts'], expected);
  requirePass(inserted.unlisted.length === 1, 'an undocumented file must fail the comparison');
  const other = ['frontend/src/app/features/falso.component.spec.ts',
    'frontend/src/app/features/falso.component.html', 'frontend/src/app/app.component.ts'];
  requirePass(other.every(path => !path.endsWith('.component.ts') ||
    !/^frontend\/src\/app\/(features|shared)\//.test(path)),
  'specs/templates/root components must not enter manifest');
}

const shared = actual.filter(path => path.startsWith('frontend/src/app/shared/')).length;
requirePass(shared === 8 && actual.length - shared === 129, '129 feature / 8 shared baseline');
console.log(`P0-007.A STATIC catalog PASS: ${actual.length} .component.ts paths (${actual.length - shared} feature, ${shared} shared).`);
if (process.argv.includes('--self-test')) console.log('P0-007.A NEGATIVE PASS: deleted/undocumented files and scope exclusions detected.');
console.log('LIMIT: no UX, accessibility, E2E, RBAC, tenant or deployment certification.');
