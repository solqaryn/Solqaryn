import { readFileSync, existsSync } from 'node:fs';
import { execFileSync } from 'node:child_process';

const expectedRepo = 'solqaryn/Solqaryn';
const expectedBranch = 'dev';

function git(...args) {
  return execFileSync('git', args, { encoding: 'utf8' }).trim();
}

const branch = git('branch', '--show-current');
if (branch !== expectedBranch) throw new Error(`SOLQARYN scope gate: branch ${branch} != ${expectedBranch}`);

const origin = git('remote', 'get-url', 'origin');
const allowed = new Set([
  'https://github.com/solqaryn/Solqaryn',
  'https://github.com/solqaryn/Solqaryn.git',
  'git@github.com:solqaryn/Solqaryn.git',
  'ssh://git@github.com/solqaryn/Solqaryn.git',
]);
if (!allowed.has(origin)) throw new Error(`SOLQARYN scope gate: unexpected origin ${origin}`);

for (const path of ['AGENTS.md', 'docs/PROJECT_SCOPE_LOCK.md', 'docs/VAEP_AUTHORITY.md']) {
  if (!existsSync(path)) throw new Error(`SOLQARYN scope gate: missing ${path}`);
  const content = readFileSync(path, 'utf8');
  if (path !== 'docs/VAEP_AUTHORITY.md' && !content.includes('PROJECT_SCOPE_LOCK=STRICT')) {
    throw new Error(`SOLQARYN scope gate: ${path} missing PROJECT_SCOPE_LOCK=STRICT`);
  }
}

console.log(`SOLQARYN scope OK: ${expectedRepo} / ${expectedBranch}`);
