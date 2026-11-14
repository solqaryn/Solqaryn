import { existsSync, readFileSync, readdirSync, statSync } from 'node:fs';
import { join, relative } from 'node:path';

const root = process.cwd();
const workflowsDir = join(root, '.github', 'workflows');
const errors = [];

function walk(dir) {
  if (!existsSync(dir)) return [];
  return readdirSync(dir).flatMap(name => {
    const abs = join(dir, name);
    return statSync(abs).isDirectory() ? walk(abs) : [abs];
  });
}

for (const abs of walk(workflowsDir)) {
  if (!/\.ya?ml$/i.test(abs)) continue;

  const rel = relative(root, abs).replaceAll('\\', '/');
  const source = readFileSync(abs, 'utf8');
  const lower = source.toLowerCase();

  const isScheduled = /(^|\n)\s*schedule\s*:/m.test(source)
    || /(^|\n)\s*-?\s*cron\s*:/m.test(source);
  const targetsRender = lower.includes('.onrender.com');
  const declaresKeepalive = /keep[-_ ]?alive|uptime[-_ ]?robot|prevent[-_ ]?sleep|stay[-_ ]?awake/.test(lower);

  if (isScheduled && targetsRender) {
    errors.push(`${rel}: scheduled workflow must not ping *.onrender.com to keep Free services awake`);
  }

  if (declaresKeepalive && targetsRender) {
    errors.push(`${rel}: artificial Render keep-alive is prohibited`);
  }
}

const renderYaml = join(root, 'render.yaml');
if (existsSync(renderYaml)) {
  const source = readFileSync(renderYaml, 'utf8');
  if (/keep[-_ ]?alive|prevent[-_ ]?sleep|stay[-_ ]?awake/i.test(source)) {
    errors.push('render.yaml: artificial keep-alive configuration is prohibited');
  }
}

if (errors.length) {
  console.error('SOLQARYN RENDER FREE POLICY GATE FAILED');
  for (const error of errors) console.error(`- ${error}`);
  process.exit(1);
}

console.log('SOLQARYN RENDER FREE POLICY GATE OK: no scheduled external keep-alive pings');
