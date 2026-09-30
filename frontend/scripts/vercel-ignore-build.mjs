import { execFileSync } from 'node:child_process';

const branch = process.env.VERCEL_GIT_COMMIT_REF;
const current = process.env.VERCEL_GIT_COMMIT_SHA || 'HEAD';
const configuredPrevious = process.env.VERCEL_GIT_PREVIOUS_SHA;

// This optimization is deliberately dev-only. Production/main and any
// unknown branch always build. The file exists on dev only until an
// explicitly authorized publication changes that fact.
if (branch !== 'dev') {
  process.exit(1);
}

const resolveCommit = ref =>
  execFileSync('git', ['rev-parse', '--verify', ref], { encoding: 'utf8' }).trim();

let previous;
try {
  // VERCEL_GIT_PREVIOUS_SHA is the last successful deployment SHA, not
  // necessarily HEAD^. Prefer it when trustworthy so a chain of control-plane
  // commits can be ignored together. If Vercel omits it, HEAD^ is a safe
  // fallback for a normal Git push. Any resolution failure remains fail-open.
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
    'CHANGELOG_AI.md',
    'PLAN_EJECUCION_AUTONOMA.md',
    'implementation_plan.md'
  ].includes(file);

if (changedFiles.length > 0 && changedFiles.every(explicitlyNonRuntime)) {
  // Vercel contract: exit 0 ignores the build; exit 1 continues it.
  console.log(`VAEP_CONTROL_PLANE_ONLY_SKIP files=${changedFiles.length} branch=${branch}`);
  process.exit(0);
}

// Frontend, backend, infrastructure, vercel config, broad docs and every
// unclassified change fail open to a normal preview build.
process.exit(1);
