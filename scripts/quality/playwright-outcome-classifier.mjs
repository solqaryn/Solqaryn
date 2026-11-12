// G0-004.D/G0-004.A: Playwright JSON signal classification (no credentials or test titles).
// FAIL, FLAKY, SKIPPED, EXPECTED_FAIL and invalid reports never become green.
import { readFile } from 'node:fs/promises';
import { pathToFileURL } from 'node:url';

export function classifyPlaywrightReport(report) {
  const counts = { passed: 0, failed: 0, flaky: 0, skipped: 0, expectedFailed: 0, invalid: 0 };
  if (!report || typeof report !== 'object' || !Array.isArray(report.suites))
    return { status: 'INVALID_REPORT', counts: { ...counts, invalid: 1 }, tests: 0 };
  // Playwright reports global/worker/setup errors separately from individual tests.
  // Never mark a green suite when those errors exist, and never echo their bodies.
  if (report.errors !== undefined && !Array.isArray(report.errors))
    return { status: 'INVALID_REPORT', counts: { ...counts, invalid: 1 }, tests: 0 };
  const globalErrors = report.errors?.length ?? 0;
  function visit(suite) {
    if (!suite || typeof suite !== 'object') { counts.invalid++; return; }
    for (const spec of suite.specs || []) {
      if (!spec || !Array.isArray(spec.tests)) { counts.invalid++; continue; }
      for (const t of spec.tests) {
        if (!t || !Array.isArray(t.results)) { counts.invalid++; continue; }
        const attempts = t.results.map(x => x?.status);
        if (t.status === 'expected' && t.expectedStatus === 'passed' &&
            attempts.length && attempts.every(x => x === 'passed')) counts.passed++;
        else if (t.status === 'expected' && t.expectedStatus === 'failed') counts.expectedFailed++;
        else if (t.status === 'unexpected') counts.failed++;
        else if (t.status === 'flaky' && attempts.includes('passed') &&
                 attempts.some(x => ['failed','timedOut','interrupted'].includes(x))) counts.flaky++;
        else if (t.status === 'skipped') counts.skipped++;
        else counts.invalid++;
      }
    }
    for (const nested of suite.suites || []) visit(nested);
  }
  for (const suite of report.suites) visit(suite);
  const tests = Object.values(counts).reduce((a,b)=>a+b, 0);
  const status = counts.invalid || (tests === 0 && !globalErrors) ? 'INVALID_REPORT' :
    (counts.failed || globalErrors) ? 'FAIL' : counts.flaky ? 'FLAKY_REVIEW' :
    (counts.skipped || counts.expectedFailed) ? 'INCOMPLETE' : 'PASS';
  return { status, counts, tests, ...(globalErrors ? { globalErrors } : {}) };
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    if (!process.argv[2]) throw Error('Missing Playwright JSON report path');
    const json = JSON.parse(await readFile(process.argv[2], 'utf8'));
    const result = classifyPlaywrightReport(json);
    console.log(JSON.stringify(result));
    if (result.status !== 'PASS') process.exitCode = 2;
  } catch (e) {
    console.error('PLAYWRIGHT_REPORT_UNVERIFIED: ' + e.message);
    process.exitCode = 3;
  }
}
