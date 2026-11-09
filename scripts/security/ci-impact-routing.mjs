import { appendFileSync, readFileSync } from 'node:fs';
import { execFileSync } from 'node:child_process';

// CI-impact routing is deliberately conservative. Unknown files and database
// test changes ALWAYS keep the MySQL Oracle integration suite enabled.
const SHA = /^[a-f0-9]{40}$/i;
// Allow P1/I8 offline tests and strictly declarative P2 metadata tests in ordinary unit folders.
// Integration/database/migration/fixture paths remain fail-closed regardless of filename.
const SCOPED_UNIT = /^backend\/tests\/Solqaryn\.Tests\/(?:[A-Za-z0-9_.-]+\/)*(?:P1\d{3}|I8\d{3}|P2\d{3})[A-Z][A-Za-z0-9_]*Tests\.cs$/;
// Pure P2 metadata only: exclude database, service, HTTP and dependency-injection behavior.
const P2_METADATA = /^backend\/tests\/Solqaryn\.Tests\/(?:[A-Za-z0-9_.-]+\/)*P2\d{3}[A-Z][A-Za-z0-9_]*Tests\.cs$/;
const P2_RUNTIME_RISK = /\b(?:EntityFrameworkCore|UseInMemoryDatabase|Sqlite|Dapper|WebApplicationFactory|TestServer|HttpClient|HttpMessageHandler|IServiceProvider|GetRequiredService|Repository|Transaction|DbSet|DbContext|Migration|Oracle|MySql|DependencyInjection|Moq|ControllerContext|ActionContext|DefaultHttpContext|IHttpContextAccessor)\b|\bnew\s+[A-Za-z0-9_.]*(?:Service|Repository)\s*\(/i;

// Fail-closed document-only P2 test class. This is NOT a generic P2 exemption:
// positive evidence of file reads + assertions and a strict import/side-effect gate.
function isReadOnlyP2ContractTest(code) {
  const imports = [...code.matchAll(/^\s*using\s+([^;]+);/gm)].map(match => match[1].trim());
  const permitted = new Set(['System', 'System.Collections.Generic', 'System.Linq', 'System.Text.RegularExpressions', 'Xunit']);
  if (!imports.includes('Xunit') || imports.some(name => !permitted.has(name))) return false;
  if (/\b(?:global\s+using|using\s+static|DllImport|unsafe|dynamic|Activator|Assembly|Reflection|Process|HttpClient|WebRequest|Socket|NetworkStream|Sql|DbContext|Connection|Repository|Migration|Transaction|Moq|EntityFrameworkCore|AppDbContext|Testcontainers|WebApplicationFactory)\b/i.test(code)) return false;
  if (!/\bFile\s*\.\s*ReadAllText\s*\(/.test(code) || !/\bAssert\s*\./.test(code)) return false;
  if (/\bFile\s*\.\s*(?:Write|Append|Delete|Move|Copy|Replace|Open|Create|Set|Encrypt|Decrypt)\w*\s*\(/i.test(code)) return false;
  if (/\bDirectory\s*\.\s*(?:Create|Delete|Move)\w*\s*\(/i.test(code)) return false;
  if (/\bnew\s+(?!DirectoryInfo\s*\()[A-Za-z_][\w.]*/.test(code)) return false;
  return !P2_RUNTIME_RISK.test(code) && !DB_CODE.test(code) && !INTEGRATION_TRAIT.test(code);
}

const DB_RISK_TEST_PATH = /\/(?:Integration(?:Tests)?|Database(?:Tests)?|Migration(?:s)?|Fixture(?:s)?|Infrastructure|E2E)\//i;
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
    if (!SCOPED_UNIT.test(path) || DB_RISK_TEST_PATH.test(path)) return true;
    try {
      const raw = loadSource(path);
      if (typeof raw !== 'string' || raw.length === 0) return true;
      const code = withoutComments(raw);
      // P2 may omit MySQL only with positive reflection-only or read-only contract evidence.
      // Runtime/DB/HTTP/Moq tests still run MySQL, as does any unclassifiable source.
      if (P2_METADATA.test(path)) {
        const reflectionOnly = /^\s*using\s+System\.Reflection\s*;/m.test(code) &&
          /\bGet(?:CustomAttributes?|Methods?|Properties?)\s*(?:[<(])/.test(code) &&
          !P2_RUNTIME_RISK.test(code);
        if (!reflectionOnly && !isReadOnlyP2ContractTest(code)) return true;
      }
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
  const offlineI8 = 'backend/tests/Solqaryn.Tests/I8004CCorreoSeguroTests.cs';
  const offlineI8Source = 'using Xunit;\npublic sealed class I8004CCorreoSeguroTests { [Fact] public void Pass() {} }\n';
  const offlineP2 = 'backend/tests/Solqaryn.Tests/P2005ACierreComprasPermisosTests.cs';
  const p2Source = 'using System.Reflection;\nusing Xunit;\npublic sealed class P2005ACierreComprasPermisosTests { [Fact] public void Metadata() { _ = typeof(string).GetCustomAttribute<ObsoleteAttribute>(); } }';
  const docP2 = 'backend/tests/Solqaryn.Tests/P2019DReadinessGateNoPromotionTests.cs';
  const docP2Source = 'using System.Text.RegularExpressions;\nusing Xunit;\npublic sealed class P2019DReadinessGateNoPromotionTests { [Fact] public void DocumentOnly() { var d = File.ReadAllText("docs/PRODUCTION_READINESS_CONTRACT.md"); Assert.Contains("PRODUCTION_READY = FALSE", d); } }';
  const cases = [
    ['P2 read-only readiness contract avoids unrelated MySQL', [docP2, 'CHANGELOG_AI.md'], docP2Source, false],
    ['P2 nested read-only contract avoids unrelated MySQL', ['backend/tests/Solqaryn.Tests/API/Controllers/P2019DReadinessGateNoPromotionTests.cs'], docP2Source, false],
    ['P2 doc test with EF import still runs MySQL', [docP2], docP2Source + '\nusing Microsoft.EntityFrameworkCore;', true],
    ['P2 doc test with Moq still runs MySQL', [docP2], docP2Source + '\nusing Moq;', true],
    ['P2 doc test with controller import still runs MySQL', [docP2], docP2Source + '\nusing Solqaryn.API.Controllers;', true],
    ['P2 doc test with IO write still runs MySQL', [docP2], docP2Source + '\nFile.WriteAllText("contract.md", "overwrite");', true],
    ['P2 doc test with Process still runs MySQL', [docP2], docP2Source + '\nProcess.Start("dotnet");', true],
    ['P2 doc test with arbitrary constructor still runs MySQL', [docP2], docP2Source + '\nnew CustomService()', true],
    ['P2 doc test without proof of file read runs MySQL', [docP2], 'using Xunit;\npublic class P2019DReadinessGateNoPromotionTests { [Fact] public void Pass() { Assert.True(true); } }', true],
    ['P2 doc test with Integration trait runs MySQL', [docP2], docP2Source + '\n[Trait("Category", "Integration")]', true],
    ['P2 doc test with backend runtime change runs MySQL', [docP2, 'backend/src/API/Program.cs'], docP2Source, true],
    ['P2 doc test under Integration directory runs MySQL', ['backend/tests/Solqaryn.Tests/Integration/P2019DReadinessGateNoPromotionTests.cs'], docP2Source, true],
    ['P2 doc test missing source runs MySQL', [docP2], null, true],
    ['P2 doc test and changed CI script runs MySQL', [docP2, 'scripts/security/ci-impact-routing.mjs'], docP2Source, true],
    ['P2 doc test and migration change runs MySQL', [docP2, 'backend/src/Infrastructure.Migrations/Any.cs'], docP2Source, true],
    ['P2 metadata test avoids unrelated MySQL', [offlineP2, 'CHANGELOG_AI.md'], p2Source, false],
    ['P2 nested metadata avoids unrelated MySQL', ['backend/tests/Solqaryn.Tests/API/Controllers/P2005ACierreComprasPermisosTests.cs'], p2Source, false],
    ['P2 non-declarative remains MySQL required', [offlineP2], clean, true],
    ['P2 EntityFramework remains MySQL required', [offlineP2], p2Source + '\nusing Microsoft.EntityFrameworkCore;', true],
    ['P2 DbContext remains MySQL required', [offlineP2], p2Source + '\nnew AppDbContext()', true],
    ['P2 service construction remains MySQL required', [offlineP2], p2Source + '\nnew CompraService()', true],
    ['P2 DI remains MySQL required', [offlineP2], p2Source + '\nusing Microsoft.Extensions.DependencyInjection;', true],
    ['P2 HTTP remains MySQL required', [offlineP2], p2Source + '\nnew WebApplicationFactory()', true],
    ['P2 integration trait remains MySQL required', [offlineP2], p2Source + '\n[Trait("Category", "Integration")]', true],
    ['P2 integration folder remains MySQL required', ['backend/tests/Solqaryn.Tests/Integration/P2005ACierreComprasPermisosTests.cs'], p2Source, true],
    ['P2 with runtime source remains MySQL required', [offlineP2, 'backend/src/API/Program.cs'], p2Source, true],
    ['P2 missing source remains MySQL required', [offlineP2], null, true],
    ['directed offline unit test only', [unit], clean, false],
    ['nested directed offline P1 unit test', ['backend/tests/Solqaryn.Tests/API/Controllers/P1013BOnboardingGateClosureRegressionTests.cs'], clean, false],
    ['I8-004.C isolated SMTP unit skips unrelated MySQL', [offlineI8, 'CHANGELOG_AI.md'], offlineI8Source, false],
    ['I8-005.C isolated authentication metadata skips unrelated MySQL', ['backend/tests/Solqaryn.Tests/I8005CAuthTokenAntifuerzaBrutaDelimitacionTests.cs'], offlineI8Source, false],
    ['nested I8 unit still offline', ['backend/tests/Solqaryn.Tests/API/Controllers/I8003CIsolationTests.cs'], offlineI8Source, false],
    ['I8 unit referencing DbContext requires MySQL', [offlineI8], 'var db = new AppDbContext();', true],
    ['I8 Integration trait requires MySQL', [offlineI8], '[Trait("Category", "Integration")] public sealed class I8004CCorreoSeguroTests {}', true],
    ['I8 integration directory requires MySQL', ['backend/tests/Solqaryn.Tests/Integration/I8004CCorreoSeguroTests.cs'], offlineI8Source, true],
    ['I8 plus production code requires MySQL', [offlineI8, 'backend/src/API/Program.cs'], offlineI8Source, true],
    ['missing I8 test fails closed', [offlineI8], null, true],
    ['nested unit + changelog and evidence', ['backend/tests/Solqaryn.Tests/API/Controllers/P1013BOnboardingGateClosureRegressionTests.cs', 'CHANGELOG_AI.md', 'docs/P1-013-B-CONCILIACION-GATES-ONBOARDING-DEV.md'], clean, false],
    ['nested direct DbContext is not offline', ['backend/tests/Solqaryn.Tests/API/Controllers/P1013BOnboardingGateClosureRegressionTests.cs'], 'using Xunit; var context = new AppDbContext();', true],
    ['nested Integration trait is not offline', ['backend/tests/Solqaryn.Tests/API/Controllers/P1013BOnboardingGateClosureRegressionTests.cs'], '[Trait("Category", "Integration")] public class P1013BOnboardingGateClosureRegressionTests {}', true],
    ['integration subdirectory stays required', ['backend/tests/Solqaryn.Tests/Integration/P1013BOnboardingGateClosureRegressionTests.cs'], clean, true],
    ['database subdirectory stays required', ['backend/tests/Solqaryn.Tests/Database/P1013BOnboardingGateClosureRegressionTests.cs'], clean, true],
    ['migration subdirectory stays required', ['backend/tests/Solqaryn.Tests/Migrations/P1013BOnboardingGateClosureRegressionTests.cs'], clean, true],
    ['non-P1 P9 nested test stays required', ['backend/tests/Solqaryn.Tests/API/Controllers/P9013BUnexpectedTests.cs'], clean, true],
    ['non-declarative P2 nested test stays required', ['backend/tests/Solqaryn.Tests/API/Controllers/P2013BUnexpectedTests.cs'], clean, true],
    ['fixtures subdirectory stays required', ['backend/tests/Solqaryn.Tests/Fixtures/P1013BOnboardingGateClosureRegressionTests.cs'], clean, true],
    ['infrastructure subdirectory stays required', ['backend/tests/Solqaryn.Tests/Infrastructure/P1013BOnboardingGateClosureRegressionTests.cs'], clean, true],
    ['E2E subdirectory stays required', ['backend/tests/Solqaryn.Tests/E2E/P1013BOnboardingGateClosureRegressionTests.cs'], clean, true],
    ['arbitrary nested backend test stays required', ['backend/tests/Solqaryn.Tests/API/Controllers/OnboardingSecurityTests.cs'], clean, true],
    ['nested P1 plus runtime code stays required', ['backend/tests/Solqaryn.Tests/API/Controllers/P1013BOnboardingGateClosureRegressionTests.cs', 'backend/src/API/Program.cs'], clean, true],
    ['nested missing file fails closed', ['backend/tests/Solqaryn.Tests/API/Controllers/P1013BOnboardingGateClosureRegressionTests.cs'], null, true],

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
