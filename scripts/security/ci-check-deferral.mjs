// SOLQARYN: single trusted classification for Scope Lock and its CI-completion retry.
// Read-only pure policy: no approvals, status fabrication, merge or GitHub writes.
export const TRUSTED_GITHUB_ACTIONS_APP_ID = 15368;
export const BLOCKED_RUNTIME_PATHS = /(^|\/)(?:\.env(?:\..*)?|[^/]+\.(?:pem|p12|key)|appsettings\.Production\.json)$|^(?:qa|prod|main)\//i;
export const GUARDED = new Set([
  'AGENTS.md', 'docs/PROJECT_SCOPE_LOCK.md', 'docs/AUTOMATION_STATUS.md', 'docs/GITHUB_ACCOUNT_ROLES.md',
  'docs/AUTOMATION_AUTHORITY.md',
  '.github/workflows/project-scope-lock.yml', '.github/workflows/solqaryn-core-ci.yml',
  'scripts/automation-stage-ownership.mjs',
  'scripts/automation-review-routing.mjs', 'scripts/automation-ci-throughput.mjs',
  'scripts/validate-dual-task-configuration.mjs',
            'scripts/validate-automation-authority.mjs', 'scripts/verify-project-scope.mjs',
  'scripts/verify-automated-pr-certification.mjs', 'scripts/verify-solqaryn-skills.mjs',
]);
export const isGuarded = name => GUARDED.has(name) || name.startsWith('.agents/skills/') || name.startsWith('.github/') || name.startsWith('scripts/security/');

// Only known, non-mandatory broad suites may finish after the fast lane.
// Never defer required security/backend, the directed scope or an unknown check.
export const CI_IMPACT_CHECK = 'Impacto CI - clasificacion MySQL fail-closed';
export const MYSQL_INTEGRATION_CHECK = 'Migraciones EF Oracle, variantes y cargas masivas en MySQL 8.4';
export const BROAD_OPTIONAL = new Set([
  'Módulos, cargas, sesión, catálogos, variantes, accesibilidad, facturación y correo',
  'ERP - Contratos, pruebas y build',
  'Migraciones EF Oracle, variantes y cargas masivas en MySQL 8.4',
  'Authorization, files, secrets and tenant gate',
  'MySQL 8.4 isolated backup and restore proof',
  'Backend Release y pruebas',
  'Frontend producción',
  'SOLQARYN - Frontend tipado y build',
  'Docker y aislamiento de entornos',
  'Preflight, backfill y autoridad en MySQL 8.4',
]);
const ASYNC_REVIEWED_NON_RUNTIME=new Set([
 'AGENTS.md','docs/PROJECT_SCOPE_LOCK.md','docs/AUTOMATION_STATUS.md',
 'docs/AUTOMATION_AUTHORITY.md','scripts/automation-review-routing.mjs',
 'scripts/automation-stage-ownership.mjs','scripts/automation-ci-throughput.mjs',
 'scripts/validate-dual-task-configuration.mjs','scripts/security/ci-impact-routing.mjs',
]);
const CI_IMPACT_SOURCE='scripts/security/ci-impact-routing.mjs';
const DOCS_WITH_RUNTIME_CONTRACTS = new Set([
  'docs/PRODUCTION_READINESS_CONTRACT.md',
  'docs/PERFORMANCE_BENCHMARK_CONTRACT.md',
  'docs/DETALLES_PENDIENTES.md',
]);
// Test-only P1 changes are already exercised by required backend/security + directed scope jobs.
// Exclude integration-suite paths, runtime code and protected contracts from the fast lane.
const OFFLINE_COMMERCIAL_UNIT = /^backend\/tests\/Solqaryn\.Tests\/(?:[A-Za-z0-9_.-]+\/)*P\d{4}[A-Z][A-Za-z0-9_]*Tests\.cs$/;
const FRONTEND_TEST_ONLY = /^frontend\/src\/[A-Za-z0-9_./-]+\.spec\.ts$/;
const INTEGRATION_TEST_PATH = /(?:\/Integration(?:Tests)?\/|\/Database(?:Tests)?\/|IntegrationTests\.cs$)/i;

function canDeferUnrelatedSuites(files,byName){
 if(!Array.isArray(files)||!files.length||files.length>200)return false;
 if(files.some(f=>!f||!['added','modified'].includes(f.status)||
   typeof f.filename!=='string'||f.filename.includes('..')||
   f.filename.includes('\\')||BLOCKED_RUNTIME_PATHS.test(f.filename)||
   (isGuarded(f.filename)&&!ASYNC_REVIEWED_NON_RUNTIME.has(f.filename))))return false;
 let units=0,governance=0,classifier=false;
 for(const f of files){
  const path=f.filename;
  if(path==='CHANGELOG_AI.md')continue;
  if(ASYNC_REVIEWED_NON_RUNTIME.has(path)){
   governance++;if(path===CI_IMPACT_SOURCE)classifier=true;continue;
  }
  if(path.startsWith('docs/')&&path.endsWith('.md')&&
     !DOCS_WITH_RUNTIME_CONTRACTS.has(path)&&
     !path.startsWith('docs/evidencias/modernizacion/'))continue;
  if(!INTEGRATION_TEST_PATH.test(path)&&
     (OFFLINE_COMMERCIAL_UNIT.test(path)||FRONTEND_TEST_ONLY.test(path))){units++;continue;}
  return false;
 }
 if(units&&governance)return false;
 if(!units&&!classifier)return true;
 const impact=byName.get(CI_IMPACT_CHECK),mysql=byName.get(MYSQL_INTEGRATION_CHECK);
 return impact?.head_sha===mysql?.head_sha&&impact?.status==='completed'&&
   impact.conclusion==='success'&&impact.app?.id===TRUSTED_GITHUB_ACTIONS_APP_ID&&
   mysql?.app?.id===TRUSTED_GITHUB_ACTIONS_APP_ID&&
   (mysql.status==='completed'&&['success','skipped'].includes(mysql.conclusion));
}

export function optionalPendingDeferred(files, byName) {
  if (!canDeferUnrelatedSuites(files, byName)) return [];
  return [...byName.values()].filter(check => BROAD_OPTIONAL.has(check.name) &&
    ['queued', 'in_progress'].includes(check.status) &&
    check.app?.id === TRUSTED_GITHUB_ACTIONS_APP_ID).map(check => check.name);
}

