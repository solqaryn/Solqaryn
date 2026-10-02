import { readFileSync, existsSync, readdirSync, statSync } from 'node:fs';
import { join, relative, sep } from 'node:path';
import { execFileSync } from 'node:child_process';

const root = process.cwd();
const expectedRepo = 'solqaryn/Solqaryn';
const expectedProjectId = 'SOLQARYN';
const requiredMarker = 'PROJECT_SCOPE_LOCK=STRICT';
const onlyLocalSkill = '.agents/skills/solqaryn-project-governance/SKILL.md';
const registryPath = 'docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md';
const allowlistPath = 'docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md';

const mandatory = [
  'AGENTS.md',
  'PROJECT_CONTEXT.md',
  'PLAN_EJECUCION_AUTONOMA.md',
  'CONTRIBUTING.md',
  'docs/VAEP_AUTHORITY.md',
  'docs/COLABORATIVO.md',
  'docs/COLABORACION_IA.md',
  '.githooks/pre-commit',
  '.githooks/post-commit',
  'scripts/iniciar-sesion-ia.ps1',
  'scripts/configurar-jules-vaep.ps1',
  'scripts/configurar-colaboracion.ps1',
  'scripts/vaep/sync-bitacora.mjs',
  'docs/ROLLBACK_RUNBOOK.md',
  'docs/runbooks/GO_LIVE_MIGRATION_RUNBOOK.md',
  'docs/runbooks/HYPERCARE_RUNBOOK.md',
  'docs/runbooks/GO_LIVE_SMOKE_RUNBOOK.md',
  '.github/CODEOWNERS',
  'docs/PROJECT_SCOPE_LOCK.md',
  allowlistPath,
  registryPath,
  onlyLocalSkill,
];

const expectedExternalSources = [
  ['agentskills/agentskills', '69ef37e9424c0a7ea9dd2293b559e43ec8176379'],
  ['pbakaus/impeccable', '2149fcce39a90bb409df5f16515f316a76dc6199'],
  ['emilkowalski/skills', 'd23d7f88a2e21c9e4b1418c7abe420f5c1052ba7'],
  ['Leonxlnx/taste-skill', 'ccbc15639c97057cbfcf32ecebc38ef716e4bb37'],
  ['blader/humanizer', '9862685f575c65a8247f90369951df1b3416e3d6'],
  ['blader/napkin', '27fa60a895de4383b26a539136bc983155cb979c'],
  ['alexgreensh/token-optimizer', '37a9546b9fecba2c4e9a02ef4e90855d449bf08f'],
  ['JuliusBrussee/caveman', '15581d14007fd01fb3f132016741962f34936ca2'],
];

const errors = [];

function read(rel) {
  return readFileSync(join(root, rel), 'utf8');
}

function walk(dir) {
  if (!existsSync(dir)) return [];
  const out = [];
  for (const name of readdirSync(dir)) {
    const path = join(dir, name);
    if (statSync(path).isDirectory()) out.push(...walk(path));
    else out.push(path);
  }
  return out;
}

for (const rel of mandatory) {
  if (!existsSync(join(root, rel))) {
    errors.push(`missing required SOLQARYN file: ${rel}`);
  }
}

const lockRequired = mandatory.filter(rel =>
  ![registryPath].includes(rel) && existsSync(join(root, rel))
);
for (const rel of lockRequired) {
  const content = read(rel);
  if (!content.includes(requiredMarker)) {
    errors.push(`${rel} missing ${requiredMarker}`);
  }
}

const skillFiles = walk(join(root, '.agents', 'skills'))
  .filter(path => path.endsWith('SKILL.md'))
  .map(path => relative(root, path).split(sep).join('/'))
  .sort();

if (skillFiles.length !== 1) {
  errors.push(`SOLQARYN must contain exactly one local SKILL.md; found ${skillFiles.length}`);
}
if (skillFiles[0] !== onlyLocalSkill) {
  errors.push(`the only local skill must be ${onlyLocalSkill}`);
}

if (existsSync(join(root, onlyLocalSkill))) {
  const content = read(onlyLocalSkill);
  const nameMatch = content.match(/^name:\s*([^\n]+)$/m);
  const name = nameMatch?.[1]?.trim().replace(/^["']|["']$/g, '') ?? '';
  if (name !== 'solqaryn-project-governance') errors.push('local skill name must be solqaryn-project-governance');
  if (!content.includes(`PROJECT_ID=${expectedProjectId}`)) errors.push('local skill missing canonical PROJECT_ID');
  if (!content.includes(`REPOSITORY=${expectedRepo}`)) errors.push('local skill missing canonical REPOSITORY');
  if (!content.includes(requiredMarker)) errors.push('local skill missing strict scope lock');

  const agentPath = join(root, '.agents/skills/solqaryn-project-governance/agents/openai.yaml');
  if (!existsSync(agentPath)) {
    errors.push('local skill missing agents/openai.yaml');
  } else {
    const agent = readFileSync(agentPath, 'utf8');
    if (!/display_name:\s*["']?SOLQARYN\b/.test(agent)) {
      errors.push('local skill display_name must start with SOLQARYN');
    }
  }
}

if (existsSync(join(root, registryPath))) {
  const registry = read(registryPath);
  if (!registry.includes('Skill Creator') || !registry.includes('ChatGPT / OpenAI, integrado en el entorno')) errors.push('registry missing official Skill Creator reference');
  if (!registry.includes('LOCAL_SKILL_COUNT=1')) errors.push('registry must declare LOCAL_SKILL_COUNT=1');
  if (!registry.includes('EXTERNAL_SKILL_SOURCES=9')) errors.push('registry must declare EXTERNAL_SKILL_SOURCES=9');
  for (const [source, pin] of expectedExternalSources) {
    if (!registry.includes(source)) errors.push(`registry missing authorized source: ${source}`);
    if (!registry.includes(pin)) errors.push(`registry missing authorized pin/resolution for: ${source}`);
  }
}

if (existsSync(join(root, allowlistPath))) {
  const allowlist = read(allowlistPath);
  if (!allowlist.includes('Skill Creator oficial de ChatGPT / OpenAI') || !allowlist.includes('Integrado en el entorno')) errors.push('allowlist missing official Skill Creator reference');
  if (!allowlist.includes('AUTHORIZED_ORIGINAL_SKILL_SOURCES=9')) {
    errors.push('allowlist must declare nine authorized original skill sources');
  }
  for (const [source, pin] of expectedExternalSources) {
    if (!allowlist.includes(source)) errors.push(`allowlist missing authorized source: ${source}`);
    if (!allowlist.includes(pin)) errors.push(`allowlist missing pin/resolution for: ${source}`);
  }
}

const identityFiles = mandatory.filter(rel =>
  existsSync(join(root, rel)) &&
  ![registryPath, allowlistPath].includes(rel)
);
for (const rel of identityFiles) {
  const content = read(rel);
  for (const match of content.matchAll(/PROJECT_ID\s*[:=]\s*[`"']?([A-Za-z0-9_-]+)/g)) {
    if (match[1].toUpperCase() !== expectedProjectId) {
      errors.push(`${rel} declares a non-canonical PROJECT_ID`);
    }
  }
  for (const match of content.matchAll(/REPOSITORY\s*[:=]\s*[`"']?([A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+)/g)) {
    if (match[1] !== expectedRepo) {
      errors.push(`${rel} declares a non-canonical REPOSITORY`);
    }
  }
  if (/skills:\/\//i.test(content)) {
    errors.push(`${rel} contains a direct external skill URI; resolve external references through the SOLQARYN registry`);
  }
}


const canonicalSolution = join(root, "backend", "Solqaryn.sln");
if (!existsSync(canonicalSolution)) {
  errors.push("backend/Solqaryn.sln is required as the canonical solution");
}

const angularPath = join(root, "frontend", "angular.json");
if (existsSync(angularPath)) {
  const angular = JSON.parse(readFileSync(angularPath, "utf8"));
  const project = angular.projects?.["solqaryn-frontend"];
  if (!project) errors.push("Angular project must be named solqaryn-frontend");
  const outputPath = project?.architect?.build?.options?.outputPath;
  if (outputPath !== "dist/solqaryn-frontend") {
    errors.push("Angular outputPath must be dist/solqaryn-frontend");
  }
}

const packagePath = join(root, "frontend", "package.json");
if (existsSync(packagePath)) {
  const packageJson = JSON.parse(readFileSync(packagePath, "utf8"));
  if (packageJson.name !== "solqaryn-frontend") {
    errors.push("frontend package name must be solqaryn-frontend");
  }
}

const storefrontPath = join(root, "frontend", "src", "app", "features", "storefront");
if (!existsSync(storefrontPath)) {
  errors.push("public storefront feature must use the tenant-neutral storefront module");
}

for (const abs of walk(join(root, "backend"))) {
  const rel = relative(root, abs).split(sep).join("/");
  if (rel.includes("/bin/") || rel.includes("/obj/")) continue;

  if (rel.endsWith(".csproj")) {
    const fileName = rel.split("/").at(-1) || "";
    if (!fileName.startsWith("Solqaryn.")) {
      errors.push("backend project must use Solqaryn.* identity: " + rel);
    }
  }

  if (rel.endsWith(".cs")) {
    const source = readFileSync(abs, "utf8");
    for (const match of source.matchAll(/^\s*namespace\s+([A-Za-z0-9_.]+)/gm)) {
      if (!match[1].startsWith("Solqaryn.")) {
        errors.push("backend namespace must use Solqaryn.* identity: " + rel + " -> " + match[1]);
      }
    }
  }
}

const legacyOperationalPatterns = [
  new RegExp(["inven", "tory"].join(""), "i"),
  new RegExp(["vari", "app"].join("[\\s_-]*"), "i"),
  new RegExp(["vari", "store", "(?:hn)?"].join("[\\s_-]*"), "i"),
  new RegExp(["jmejia", "31"].join(""), "i"),
  new RegExp(["javiermejia", "3112", "@gmail\\.com"].join(""), "i")
];

const currentStateIdentityFiles = [
  "AGENTS.md",
  "PROJECT_CONTEXT.md",
  "PROJECT_INDEX.md",
  "ARCHITECTURE.md",
  "README.md",
  "CONTRIBUTING.md",
  "docs/PROJECT_SCOPE_LOCK.md",
  "docs/VAEP_AUTHORITY.md",
  "docs/ENTORNOS_DEV_PROD.md",
  "docs/RENDER_ENVIRONMENT_CONTRACT.md",
  "docs/DETALLES_PENDIENTES.md",
];

for (const rel of currentStateIdentityFiles) {
  if (!existsSync(join(root, rel))) continue;
  const source = read(rel);
  if (legacyOperationalPatterns.some(pattern => pattern.test(source))) {
    errors.push("retired project identity remains in current-state document: " + rel);
  }
}

const repositoryWideRetiredPatterns = [
  new RegExp(["inven", "tory"].join(""), "i"),
  new RegExp(["vari", "app"].join("[\\s_-]*"), "i"),
  new RegExp(["vari", "store", "(?:hn)?"].join("[\\s_-]*"), "i"),
  new RegExp(["vari", "storage"].join("[\\s_-]*"), "i"),
  new RegExp(["varia", "storage"].join("[\\s_-]*"), "i"),
];

const trackedFiles = execFileSync("git", ["ls-files", "-z"], { cwd: root })
  .toString("utf8")
  .split("\0")
  .filter(Boolean);

for (const rel of trackedFiles) {
  if (repositoryWideRetiredPatterns.some(pattern => pattern.test(rel))) {
    errors.push("retired identity token remains in tracked path: " + rel);
  }
  const abs = join(root, rel);
  if (!existsSync(abs)) continue;
  const raw = readFileSync(abs);
  if (raw.includes(0)) continue;
  const source = raw.toString("utf8");
  if (repositoryWideRetiredPatterns.some(pattern => pattern.test(source))) {
    errors.push("retired identity token remains in tracked content: " + rel);
  }
}

const operationalRoots = ["backend", "frontend", "scripts", ".github/workflows", ".github/scripts", ".githooks", ".agents"];
for (const rootRel of operationalRoots) {
  const rootAbs = join(root, rootRel);
  if (!existsSync(rootAbs)) continue;
  for (const abs of walk(rootAbs)) {
    const rel = relative(root, abs).split(sep).join("/");
    if (legacyOperationalPatterns.some(pattern => pattern.test(rel))) {
      errors.push("retired project or tenant identity remains in path: " + rel);
    }
    const raw = readFileSync(abs);
    if (raw.includes(0)) continue;
    const source = raw.toString("utf8");
    if (legacyOperationalPatterns.some(pattern => pattern.test(source))) {
      errors.push("retired project or tenant identity remains in content: " + rel);
    }
  }
}

if (errors.length) {
  console.error('SOLQARYN PROJECT SCOPE GATE FAILED');
  for (const error of errors) console.error(`- ${error}`);
  process.exit(1);
}

console.log('SOLQARYN PROJECT SCOPE GATE OK: canonical SOLQARYN identity + one local skill + nine pinned original references');
