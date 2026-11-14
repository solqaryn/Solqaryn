// G0-004.C: classify Playwright JSON result evidence without hiding skips or retries.
import { readFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';

const FAILURE_STATUSES = new Set(['failed', 'timedOut', 'interrupted']);
const VALID_STATUSES = new Set(['passed', 'failed', 'timedOut', 'interrupted', 'skipped']);

export function classifyE2eTest(test) {
  if (!test || !Array.isArray(test.results) || test.results.length === 0) return 'UNKNOWN';
  const statuses = test.results.map((run) => run?.status);
  if (statuses.some((status) => !VALID_STATUSES.has(status))) return 'UNKNOWN';
  const last = statuses.at(-1);
  if (last === 'skipped') return 'SKIPPED';
  if (test.status === 'flaky' || (last === 'passed' && statuses.slice(0, -1).some((status) => FAILURE_STATUSES.has(status)))) {
    return 'FLAKY';
  }
  if (last === 'passed') {
    return test.expectedStatus === 'failed' || test.status === 'unexpected' ? 'UNEXPECTED_PASS' : 'PASS';
  }
  if (FAILURE_STATUSES.has(last)) {
    return test.expectedStatus === 'failed' && test.status !== 'unexpected' ? 'EXPECTED_FAILURE' : 'FAILURE';
  }
  return 'UNKNOWN';
}

export function summarizeE2eJson(report) {
  if (!report || !Array.isArray(report.suites)) throw new Error('INVALID_PLAYWRIGHT_REPORT');
  // Playwright can report global/setup/worker errors independently of test cases.
  // Never claim PASS when top-level infrastructure evidence contains errors.
  if (report.errors !== undefined && !Array.isArray(report.errors))
    throw new Error('INVALID_PLAYWRIGHT_REPORT_ERRORS');
  const globalErrors = report.errors?.length ?? 0;
  const counts = { PASS: 0, FLAKY: 0, SKIPPED: 0, FAILURE: 0, EXPECTED_FAILURE: 0, UNEXPECTED_PASS: 0, UNKNOWN: 0 };
  const cases = [];
  function visit(suite) {
    for (const spec of suite?.specs ?? []) {
      for (const test of spec?.tests ?? []) {
        const classification = classifyE2eTest(test);
        counts[classification]++;
        cases.push({ title: spec.title ?? '', projectName: test.projectName ?? '', classification });
      }
    }
    for (const nested of suite?.suites ?? []) visit(nested);
  }
  for (const suite of report.suites) visit(suite);
  if (!cases.length) throw new Error('NO_E2E_CASES');
  const verdict = globalErrors || counts.FAILURE || counts.UNEXPECTED_PASS || counts.UNKNOWN ? 'NO_GO' : counts.FLAKY || counts.SKIPPED ? 'REVIEW_REQUIRED' : 'PASS';
  return { verdict, counts, cases };
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    const file = process.argv[2];
    if (!file) throw new Error('USAGE: node e2e-result-classifier.mjs <playwright-json-report>');
    const result = summarizeE2eJson(JSON.parse(readFileSync(file, 'utf8')));
    process.stdout.write(JSON.stringify(result, null, 2) + '\n');
    if (result.verdict !== 'PASS') process.exitCode = 1;
  } catch (error) {
    process.stderr.write(error.message + '\n');
    process.exitCode = 1;
  }
}
