import { appendFileSync, readFileSync } from 'node:fs';
import { execFileSync } from 'node:child_process';

// CI-impact routing is deliberately conservative. Unknown files and database
// test changes ALWAYS keep the MySQL Oracle integration suite enabled.
const SHA = /^[a-f0-9]{40}$/i;
const SCOPED_UNIT = /^backend\/tests\/Solqaryn\.Tests\/P\d{4}[A-Z][A-Za-z0-9_]*Tests\.cs$/;
const DB_CODE = /\b(?:MySqlConnection|MySqlConnector|AppDbContext|DbContext|Testcontainers|SqlConnection|DatabaseFixture)\b|(?:\.\s*UseMySql\s*\(|\.\s*Migrate\s*\(|\bDatabase\s*\.)/i;
const INTEGRATION_TRAIT = /\[\s*Trait\s*\(\s*"Category"\s*,\s*"Integration"\s*\)/i;

function withoutComments(source) {
  return source.replace(/\/\*[\s\S]*?\*\//g, '').replace(/^\s*\/\/.*$/gm, '');
}

export function needsMySqlIntegration(paths, loadSource = path => readFileSync(path, 'utf8')) {
  if (!Array.isArray(paths) || paths.length === 0) return true;
  return paths.some(path => {
    if (typeof path !== 'string' || !path || path.includes('..') || path.includes('\\') || path.includes('\0')) return true;
    if (path === 'CHANGELOG_AI.md' || path.startsWith('docs/') || path.startsWith('frontend/')) return false;
    if (!SCOPED_UNIT.test(path)) return true;
    try {
      const raw = loadSource(path);
      if (typeof raw !== 'string' || raw.length === 0) return true;
      const code = withoutComments(raw);
      return INTEGRATION_TRAIT.test(code) || DB_CODE.test(code);
    } catch {
      // Deletions, missing files and read errors cannot be classified safely.
      return true;
    }
  });
}

function selfTest() {
  const unit = 'backend/tests/Solqaryn.Tests/P1003BSupportRouteCertificationTests.cs';
  const clean = 'using Xunit;\n// MySQL integration is tested elsewhere\npublic sealed class P1003BSupportRouteCertificationTests { [Fact] public void Pass() {} }\n';
  const cases = [
    ['directed offline unit test only', [unit], clean, false],
    ['frontend only', ['frontend/src/app/app.component.ts'], clean, false],
    ['non-executable evidence only', ['CHANGELOG_AI.md', 'docs/CI_IMPACT_ROUTING.md'], clean, false],
    ['backend production source', ['backend/src/API/Program.cs'], clean, true],
    ['migration infrastructure', ['backend/src/Infrastructure.Migrations/NewMigration.cs'], clean, true],
    ['other backend test', ['backend/tests/Solqaryn.Tests/SupportSessionMySqlIntegrationTests.cs'], clean, true],
    ['unknown new file', ['scripts/security/ci-impact-routing.mjs'], clean, true],
    ['workflow change', ['.github/workflows/dev-ci.yml'], clean, true],
    ['explicit Integration category', [unit], '[Trait("Category", "Integration")] public class P1003BSupportRouteCertificationTests {}', true],
    ['direct database context', [unit], 'using Xunit; var context = new AppDbContext();', true],
    ['changed backend and unit test', [unit, 'backend/src/Infrastructure/Repository.cs'], clean, true],
    ['no files or missing diff', [], clean, true],
    ['missing scoped file', [unit], null, true],
  ];
  for (const [label, paths, source, expected] of cases) {
    const actual = needsMySqlIntegration(paths, () => source);
    if (actual !== expected) throw Error('CI_IMPACT_ROUTING_SELF_TEST_FAILED:' + label);
  }
  console.log('CI_IMPACT_ROUTING_SELF_TEST=PASS (' + cases.length + ' cases)');
}

function classifyLive() {
  const base = process.env.CI_BASE_SHA || '';
  const head = process.env.CI_HEAD_SHA || '';
  if (!SHA.test(head)) throw Error('CI_IMPACT_INVALID_HEAD');
  let paths = [];
  let mustRun = true;
  if (SHA.test(base) && !/^0+$/.test(base)) {
    const buffer = execFileSync('git', ['diff', '--name-only', '-z', base, head], { encoding: 'utf8', maxBuffer: 4 * 1024 * 1024 });
    paths = buffer.split('\0').filter(Boolean);
    mustRun = needsMySqlIntegration(paths);
  }
  if (!process.env.GITHUB_OUTPUT) throw Error('CI_IMPACT_GITHUB_OUTPUT_MISSING');
  appendFileSync(process.env.GITHUB_OUTPUT, 'run_mysql_integration=' + (mustRun ? 'true' : 'false') + '\n');
  console.log('CI_IMPACT_MYSQL_INTEGRATION=' + (mustRun ? 'REQUIRED' : 'SKIPPED_SAFE_SCOPED_TEST_OR_FRONTEND'));
  console.log('CI_IMPACT_CHANGED_FILES=' + paths.length);
}

if (process.argv.includes('--self-test')) selfTest();
else if (process.argv.includes('--github-output')) classifyLive();
else throw Error('Provide --self-test or --github-output');
