import { strict as assert } from 'node:assert';
import { test } from 'node:test';
import { SOURCE, EXPECTED, onlyExpectedPaths } from './verify-c1401-m0604-rollback.mjs';

test('positive: source SHA immutable and three exactly allowed history/test/doc paths', () => {
  assert.match(SOURCE, /^[0-9a-f]{40}$/);
  assert.equal(onlyExpectedPaths([...EXPECTED]), true);
});
test('negative: rollback cannot include migrations or tenant data', () => {
  assert.equal(onlyExpectedPaths([...EXPECTED, 'backend/src/Infrastructure/Migrations/Migrate.cs']), false);
  assert.equal(onlyExpectedPaths(['backend/src/Infrastructure/Persistence/AppDbContext.cs', ...EXPECTED.slice(1)]), false);
});
test('boundary: duplicates, empty, incomplete and excessive lists fail closed', () => {
  assert.equal(onlyExpectedPaths([]), false);
  assert.equal(onlyExpectedPaths([EXPECTED[0], EXPECTED[0], EXPECTED[1]]), false);
  assert.equal(onlyExpectedPaths(EXPECTED.slice(0, 2)), false);
  assert.equal(onlyExpectedPaths([...EXPECTED, 'README.md']), false);
});
test('adversarial paths, null and invalid shapes cannot match allowlist', () => {
  assert.equal(onlyExpectedPaths(['../secrets', ...EXPECTED.slice(1)]), false);
  assert.equal(onlyExpectedPaths([null, ...EXPECTED.slice(1)]), false);
  assert.equal(onlyExpectedPaths('CHANGELOG_AI.md'), false);
  assert.equal(onlyExpectedPaths(null), false);
});
