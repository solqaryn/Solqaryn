import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

const targetUrl = new URL('../../frontend/src/styles/_m10-accessibility.scss', import.meta.url);
const noteUrl = new URL('../../docs/P5-018-C-DELIMITACION-ACCESIBILIDAD-DEV.md', import.meta.url);
const source = readFileSync(fileURLToPath(targetUrl), 'utf8');
const note = readFileSync(fileURLToPath(noteUrl), 'utf8');

function detectScenario(css) {
  const reduced = css.split('@media (prefers-reduced-motion: reduce)')[1]
    ?.split('@media (forced-colors: active)')[0] ?? '';
  const forced = css.split('@media (forced-colors: active)')[1] ?? '';
  const issues = [];
  if (!/#main-content:focus\s*\{/.test(css)) issues.push('FOCUS_TARGET_MISSING');
  if (!reduced.includes('.skip-link') || !reduced.includes('.app-surface-card--interactive') ||
      !reduced.includes('transition: none !important') || !reduced.includes('transform: none !important')) {
    issues.push('REDUCED_MOTION_NOT_COVERED');
  }
  if (!forced.includes('#main-content:focus') || !forced.includes('outline-color: Canvas') ||
      !forced.includes('.app-selected') || !forced.includes('Highlight')) {
    issues.push('FORCED_COLORS_NOT_COVERED');
  }
  return issues;
}

test('P5-018.C: a single versioned SCSS scenario is identified, not an AA certification', () => {
  assert.match(note, /TARGET_PATH[^\n]*`frontend\/src\/styles\/_m10-accessibility\.scss`/);
  assert.match(note, /BASELINE_BLOB_SHA[^\n]*`6140d41b8a7e1e98f4fa1e43e0ab72f04b6ae29f`/);
  assert.match(note, /no la implementaci[oó]n|no la implementación/i);
  assert.deepEqual(detectScenario(source), []);
});

test('P5-018.C: missing reduced motion branch is rejected', () => {
  assert.ok(detectScenario(source.replace('@media (prefers-reduced-motion: reduce)', '@media (motion: permitted)'))
    .includes('REDUCED_MOTION_NOT_COVERED'));
});

test('P5-018.C: missing forced-colors branch is rejected', () => {
  assert.ok(detectScenario(source.replace('@media (forced-colors: active)', '@media (contrast: unknown)'))
    .includes('FORCED_COLORS_NOT_COVERED'));
});

test('P5-018.C: missing keyboard focus target is rejected', () => {
  assert.ok(detectScenario(source.replaceAll('#main-content:focus', '#nonexistent:focus'))
    .includes('FOCUS_TARGET_MISSING'));
});
