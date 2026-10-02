import { execFileSync } from 'node:child_process';

const branch = process.env.VERCEL_GIT_COMMIT_REF;
const current = process.env.VERCEL_GIT_COMMIT_SHA || 'HEAD';
const configuredPrevious = process.env.VERCEL_GIT_PREVIOUS_SHA;
const vercelProjectId = process.env.VERCEL_PROJECT_ID;

const PROJECT_BRANCH_BINDINGS = Object.freeze({
  'prj_1Anhx5mWyXEBX89lWC24Py6JXe7A': 'dev',
  'prj_n5STx5F6VboqXd1oLUMR8AvZZtml': 'qa',
  'prj_si3ORH7lBhM4aSAYfYvXsbJT2lHA': 'main'
});

const expectedBranch = PROJECT_BRANCH_BINDINGS[vercelProjectId];

// Cada proyecto Vercel corporativo sólo construye su rama canónica.
// Exit 0 es el contrato de Vercel para ignorar el build.
if (expectedBranch && branch !== expectedBranch) {
  console.log(`CROSS_ENV_PROJECT_SKIP branch=${branch} expected=${expectedBranch} project=${vercelProjectId}`);
  process.exit(0);
}

// Un project ID desconocido falla abierto a build para no ocultar una
// configuración nueva accidental; environment-binding.js fallará cerrado
// en runtime hasta que el proyecto sea explícitamente autorizado.
if (!expectedBranch) {
  process.exit(1);
}

// La optimización de cambios de control-plane permanece deliberadamente
// limitada a DEV. QA y PROD siempre construyen su rama canónica para que
// cada promoción tenga evidencia de deployment exact-head.
if (branch !== 'dev') {
  process.exit(1);
}

const resolveCommit = ref =>
  execFileSync('git', ['rev-parse', '--verify', ref], { encoding: 'utf8' }).trim();

let previous;
try {
  if (configuredPrevious && /^[0-9a-f]{40}$/i.test(configuredPrevious)) {
    previous = resolveCommit(configuredPrevious);
  } else {
    previous = resolveCommit(`${current}^`);
  }
} catch {
  process.exit(1);
}

let changedFiles;
try {
  changedFiles = execFileSync(
    'git',
    ['diff', '--name-only', '--no-renames', previous, current],
    { encoding: 'utf8' }
  )
    .split(/\r?\n/)
    .map(file => file.trim().replaceAll('\\', '/'))
    .filter(Boolean);
} catch {
  process.exit(1);
}

const explicitlyNonRuntime = file =>
  file.startsWith('vaep/') ||
  file.startsWith('scripts/vaep/') ||
  file.startsWith('.github/scripts/vaep-') ||
  file.startsWith('.github/workflows/vaep-') ||
  file.startsWith('docs/VAEP_') ||
  [
    'AGENTS.md',
    'PROJECT_CONTEXT.md',
    'PROJECT_INDEX.md',
    'ARCHITECTURE.md',
    'ARCHITECTURE_CHANGELOG.md',
    'TASKS.md',
    'CHANGELOG_AI.md'
  ].includes(file);

if (changedFiles.length > 0 && changedFiles.every(explicitlyNonRuntime)) {
  console.log(`VAEP_CONTROL_PLANE_ONLY_SKIP files=${changedFiles.length} branch=${branch}`);
  process.exit(0);
}

process.exit(1);
