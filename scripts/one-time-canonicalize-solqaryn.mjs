import {
  existsSync,
  lstatSync,
  mkdirSync,
  readFileSync,
  readdirSync,
  renameSync,
  unlinkSync,
  writeFileSync
} from 'node:fs';
import { basename, dirname, extname, join, relative, sep } from 'node:path';

const root = process.cwd();
const transientScript = 'scripts/one-time-canonicalize-solqaryn.mjs';
const transientWorkflow = '.github/workflows/one-time-canonical-solqaryn-identity.yml';

const excludedDirectoryNames = new Set([
  '.git', 'node_modules', 'dist', 'bin', 'obj', '.angular', 'coverage',
  'playwright-report', 'test-results'
]);

const activeRoots = [
  'backend',
  'frontend',
  'scripts',
  '.github/workflows',
  '.github/scripts',
  '.githooks',
  '.agents'
];

const rootTextFiles = [
  'Dockerfile',
  'render.yaml',
  '.dockerignore',
  '.gitignore',
  'README.md',
  'PROJECT_INDEX.md',
  'ARCHITECTURE.md'
];

const textExtensions = new Set([
  '.cs', '.csproj', '.sln', '.ts', '.tsx', '.js', '.mjs', '.cjs',
  '.json', '.html', '.scss', '.css', '.ps1', '.sh', '.yml', '.yaml',
  '.xml', '.props', '.targets', '.md', '.txt'
]);

function posixPath(value) {
  return value.split(sep).join('/');
}

function shouldSkipDirectory(abs) {
  return excludedDirectoryNames.has(basename(abs));
}

function walk(abs, includeDirs = false) {
  if (!existsSync(abs)) return [];
  const out = [];
  const info = lstatSync(abs);
  if (!info.isDirectory()) return [abs];
  if (includeDirs) out.push(abs);
  for (const name of readdirSync(abs)) {
    const child = join(abs, name);
    const childInfo = lstatSync(child);
    if (childInfo.isDirectory()) {
      if (shouldSkipDirectory(child)) continue;
      out.push(...walk(child, includeDirs));
    } else {
      out.push(child);
    }
  }
  return out;
}

function isTextFile(abs) {
  const name = basename(abs);
  if (name === 'Dockerfile' || name.startsWith('.env')) return true;
  return textExtensions.has(extname(name).toLowerCase());
}

function looksBinary(buffer) {
  return buffer.includes(0);
}

function caseStyledReplacement(match, replacement) {
  if (match === match.toUpperCase()) return replacement.toUpperCase();
  if (match[0] && match[0] === match[0].toUpperCase()) {
    return replacement[0].toUpperCase() + replacement.slice(1);
  }
  return replacement.toLowerCase();
}

function canonicalizeText(input) {
  let value = input;
  value = value.replace(/vari[-_\s]?store[-_\s]?hn/gi, function (match) {
    return caseStyledReplacement(match, 'storefront');
  });
  value = value.replace(/inventory[-_\s]?app/gi, function (match) {
    return caseStyledReplacement(match, 'solqaryn');
  });
  value = value.replace(/vari[-_\s]?app/gi, function (match) {
    return caseStyledReplacement(match, 'solqaryn');
  });
  value = value.replace(/javiermejia3112@gmail\.com/gi, 'solqaryn.platform@outlook.com');
  value = value.replace(/jmejia31/gi, 'solqaryn');
  value = value.replace(/solqaryn-api-desarrollo/gi, 'solqaryn-api-dev');
  value = value.replace(/\bDesarrollo\b/g, 'dev');
  return value;
}

function canonicalizePath(rel) {
  let value = posixPath(rel);
  value = value.replace(/varistorehn/gi, function (match) {
    return caseStyledReplacement(match, 'storefront');
  });
  value = value.replace(/inventoryapp/gi, function (match) {
    return caseStyledReplacement(match, 'solqaryn');
  });
  value = value.replace(/variapp/gi, function (match) {
    return caseStyledReplacement(match, 'solqaryn');
  });
  value = value.replace(/jmejia31/gi, 'solqaryn');
  value = value.replace(/Desarrollo/g, 'dev');
  value = value.replace(/desarrollo/g, 'dev');
  return value;
}

function mutateTextFile(abs, transform = canonicalizeText) {
  if (!existsSync(abs) || !isTextFile(abs)) return false;
  const raw = readFileSync(abs);
  if (looksBinary(raw)) return false;
  const before = raw.toString('utf8');
  const after = transform(before);
  if (after === before) return false;
  writeFileSync(abs, after, 'utf8');
  return true;
}

function activeTextFiles() {
  const files = [];
  for (const rel of activeRoots) {
    const abs = join(root, rel);
    for (const file of walk(abs)) {
      if (isTextFile(file)) files.push(file);
    }
  }
  for (const rel of rootTextFiles) {
    const abs = join(root, rel);
    if (existsSync(abs) && isTextFile(abs)) files.push(abs);
  }
  return [...new Set(files)];
}

function renameActivePaths() {
  const entries = [];
  for (const rel of activeRoots) {
    entries.push(...walk(join(root, rel), true));
  }

  entries
    .filter(function (abs) { return abs !== root; })
    .sort(function (a, b) {
      return posixPath(relative(root, b)).split('/').length - posixPath(relative(root, a)).split('/').length;
    })
    .forEach(function (abs) {
      if (!existsSync(abs)) return;
      const oldRel = posixPath(relative(root, abs));
      const newRel = canonicalizePath(oldRel);
      if (newRel === oldRel) return;
      const target = join(root, ...newRel.split('/'));
      if (existsSync(target)) {
        throw new Error('Canonical rename collision: ' + oldRel + ' -> ' + newRel);
      }
      mkdirSync(dirname(target), { recursive: true });
      renameSync(abs, target);
    });
}

function normalizePublicStorefrontRoutes() {
  const frontend = join(root, 'frontend');
  for (const file of walk(frontend)) {
    if (!isTextFile(file)) continue;
    mutateTextFile(file, function (input) {
      let value = input;
      value = value.replace(/\/storefront(?=\/|['"\s<])/g, '/tienda');
      value = value.replace(/path:\s*'storefront(?=\/|')/g, function (match) {
        return match.replace('storefront', 'tienda');
      });
      value = value.replace(/STOREFRONT_BASE_PATH\s*=\s*'storefront'/g, "STOREFRONT_BASE_PATH = 'tienda'");
      value = value.replace(/Abrir en Storefront/g, 'Abrir tienda');
      return value;
    });
  }
}

function rewriteScopeGate() {
  const rel = 'scripts/verify-project-scope.mjs';
  const abs = join(root, rel);
  let content = readFileSync(abs, 'utf8');
  const start = content.indexOf('\nconst retiredIdentityToken');
  const end = content.indexOf('\nif (errors.length)');
  if (start < 0 || end < 0 || end <= start) {
    throw new Error('Could not locate replaceable identity block in verify-project-scope.mjs');
  }

  const block = [
    '',
    'const canonicalSolution = join(root, "backend", "Solqaryn.sln");',
    'if (!existsSync(canonicalSolution)) {',
    '  errors.push("backend/Solqaryn.sln is required as the canonical solution");',
    '}',
    '',
    'const angularPath = join(root, "frontend", "angular.json");',
    'if (existsSync(angularPath)) {',
    '  const angular = JSON.parse(readFileSync(angularPath, "utf8"));',
    '  const project = angular.projects?.["solqaryn-frontend"];',
    '  if (!project) errors.push("Angular project must be named solqaryn-frontend");',
    '  const outputPath = project?.architect?.build?.options?.outputPath;',
    '  if (outputPath !== "dist/solqaryn-frontend") {',
    '    errors.push("Angular outputPath must be dist/solqaryn-frontend");',
    '  }',
    '}',
    '',
    'const packagePath = join(root, "frontend", "package.json");',
    'if (existsSync(packagePath)) {',
    '  const packageJson = JSON.parse(readFileSync(packagePath, "utf8"));',
    '  if (packageJson.name !== "solqaryn-frontend") {',
    '    errors.push("frontend package name must be solqaryn-frontend");',
    '  }',
    '}',
    '',
    'const storefrontPath = join(root, "frontend", "src", "app", "features", "storefront");',
    'if (!existsSync(storefrontPath)) {',
    '  errors.push("public storefront feature must use the tenant-neutral storefront module");',
    '}',
    '',
    'for (const abs of walk(join(root, "backend"))) {',
    '  const rel = relative(root, abs).split(sep).join("/");',
    '  if (rel.includes("/bin/") || rel.includes("/obj/")) continue;',
    '',
    '  if (rel.endsWith(".csproj")) {',
    '    const fileName = rel.split("/").at(-1) || "";',
    '    if (!fileName.startsWith("Solqaryn.")) {',
    '      errors.push("backend project must use Solqaryn.* identity: " + rel);',
    '    }',
    '  }',
    '',
    '  if (rel.endsWith(".cs")) {',
    '    const source = readFileSync(abs, "utf8");',
    '    for (const match of source.matchAll(/^\\s*namespace\\s+([A-Za-z0-9_.]+)/gm)) {',
    '      if (!match[1].startsWith("Solqaryn.")) {',
    '        errors.push("backend namespace must use Solqaryn.* identity: " + rel + " -> " + match[1]);',
    '      }',
    '    }',
    '  }',
    '}',
    ''
  ].join('\n');

  content = content.slice(0, start) + block + content.slice(end);
  writeFileSync(abs, content, 'utf8');
}

function updateCanonicalDocs() {
  function identityOnly(input) {
    return input.replace(/InventoryApp/g, 'Solqaryn').replace(/inventoryapp/g, 'solqaryn');
  }

  for (const rel of ['README.md', 'PROJECT_INDEX.md', 'ARCHITECTURE.md']) {
    const abs = join(root, rel);
    if (existsSync(abs)) mutateTextFile(abs, identityOnly);
  }

  const architecture = join(root, 'ARCHITECTURE.md');
  if (existsSync(architecture)) {
    let text = readFileSync(architecture, 'utf8');
    const marker = '### Identidad técnica canónica SOLQARYN';
    if (!text.includes(marker)) {
      text += [
        '',
        '',
        marker,
        '',
        '- Assemblies, namespaces, proyectos, solución, artefactos de build y claves técnicas propias usan únicamente la identidad Solqaryn / SOLQARYN.',
        '- El storefront público es un módulo tenant-neutral bajo frontend/src/app/features/storefront; las marcas comerciales y nombres de empresas se resuelven desde datos/configuración, nunca desde nombres de código.',
        '- La ruta pública técnica canónica del storefront es /tienda; dominios y nombres comerciales pertenecen a configuración, no al source code.',
        ''
      ].join('\n');
      writeFileSync(architecture, text, 'utf8');
    }
  }

  const context = join(root, 'PROJECT_CONTEXT.md');
  if (existsSync(context)) {
    let text = readFileSync(context, 'utf8');
    const marker = '- Identidad técnica de código:';
    if (!text.includes(marker)) {
      const anchor = '- Frontend: Angular 20 standalone, Signals y Angular Material.';
      text = text.replace(anchor, anchor + '\n' + marker + ' namespaces/assemblies/proyectos usan Solqaryn.*; el storefront fuente es tenant-neutral y vive bajo features/storefront.');
      writeFileSync(context, text, 'utf8');
    }
  }

  const changeLog = join(root, 'ARCHITECTURE_CHANGELOG.md');
  if (existsSync(changeLog)) {
    let text = readFileSync(changeLog, 'utf8');
    const entry = '## 2026-09-30 — Identidad técnica canónica y storefront tenant-neutral';
    if (!text.includes(entry)) {
      text += [
        '',
        '',
        entry,
        '',
        '- Se normalizaron nombres de solución, proyectos, namespaces, build outputs, claves técnicas, tests y scripts a SOLQARYN.',
        '- El frontend público quedó desacoplado de nombres comerciales y pasó a un módulo genérico de storefront con ruta técnica /tienda.',
        '- No hubo migración ni eliminación de datos; los valores históricos persistidos permanecen bajo control de datos/migraciones explícitas.',
        ''
      ].join('\n');
      writeFileSync(changeLog, text, 'utf8');
    }
  }

  const aiLog = join(root, 'CHANGELOG_AI.md');
  if (existsSync(aiLog)) {
    let text = readFileSync(aiLog, 'utf8');
    const entry = '## 2026-09-30 — Canonicalización técnica SOLQARYN';
    if (!text.includes(entry)) {
      text += [
        '',
        '',
        entry,
        '',
        '- Refactor nominal transversal en DEV: proyectos, namespaces, artefactos, storefront, pruebas y scripts quedan bajo identidad técnica SOLQARYN y nombres tenant-neutral.',
        '- Sin cambios de datos productivos, sin migraciones destructivas y sin cambios en main/PROD.',
        ''
      ].join('\n');
      writeFileSync(aiLog, text, 'utf8');
    }
  }
}

function removeTransientFiles() {
  for (const rel of [transientScript, transientWorkflow]) {
    const abs = join(root, rel);
    if (existsSync(abs)) unlinkSync(abs);
  }
}

for (const file of activeTextFiles()) {
  const rel = posixPath(relative(root, file));
  if (rel === 'scripts/verify-project-scope.mjs') continue;
  if (rel === transientScript || rel === transientWorkflow) continue;
  mutateTextFile(file);
}

renameActivePaths();
normalizePublicStorefrontRoutes();
rewriteScopeGate();
updateCanonicalDocs();
removeTransientFiles();

console.log('SOLQARYN canonical identity refactor applied in working tree.');
