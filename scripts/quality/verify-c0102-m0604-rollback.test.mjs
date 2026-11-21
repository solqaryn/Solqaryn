import { strict as assert } from 'node:assert';
import { test } from 'node:test';
import { EXPECTED, onlyExpectedPaths, SOURCE } from './verify-c0102-m0604-rollback.mjs';

test('positive: exact three code/doc/changelog paths permitted for source rollback', () => {
  assert.match(SOURCE, /^[a-f0-9]{40}$/);
  assert.equal(onlyExpectedPaths([...EXPECTED]), true);
});
test('negative: never revert database migrations or tenant data files', () => {
  assert.equal(onlyExpectedPaths([...EXPECTED, 'backend/src/Infrastructure/Migrations/DropCustomer.cs']), false);
  assert.equal(onlyExpectedPaths(['backend/src/Infrastructure/Persistence/AppDbContext.cs', ...EXPECTED.slice(1)]), false);
});
test('boundary: empty, duplicate, missing or extra files all rejected', () => {
  assert.equal(onlyExpectedPaths([]), false);
  assert.equal(onlyExpectedPaths([EXPECTED[0], EXPECTED[0], EXPECTED[1]]), false);
  assert.equal(onlyExpectedPaths(EXPECTED.slice(0, 2)), false);
  assert.equal(onlyExpectedPaths([...EXPECTED, 'README.md']), false);
});
test('security: reject untrusted path names and invalid shapes', () => {
  assert.equal(onlyExpectedPaths(['../secrets', ...EXPECTED.slice(1)]), false);
  assert.equal(onlyExpectedPaths('CHANGELOG_AI.md'), false);
});
