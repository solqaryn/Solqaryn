import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { basename, join, relative, resolve } from 'node:path';

const cwd = process.cwd();
const distRoot = resolve(cwd, 'dist/solqaryn-frontend');
const browserRoot = existsSync(join(distRoot, 'browser')) ? join(distRoot, 'browser') : distRoot;
const indexPath = join(browserRoot, 'index.html');
const statsPath = [join(distRoot, 'stats.json'), join(browserRoot, 'stats.json')]
  .find(candidate => existsSync(candidate));

if (!existsSync(indexPath)) {
  console.error('[angular-stats] No se encontro index.html del build productivo.');
  process.exit(1);
}
if (!statsPath) {
  console.error('[angular-stats] No se encontro stats.json. Ejecuta ng build --stats-json.');
  process.exit(1);
}

const stats = JSON.parse(readFileSync(statsPath, 'utf8'));
const outputs = stats.outputs ?? {};
const index = readFileSync(indexPath, 'utf8');
const documentRefs = [...index.matchAll(/(?:src|href)=["']([^"']+\.(?:js|css))["']/g)]
  .map(match => match[1].replace(/^\.\//, '').replace(/^\//, ''));

const normalized = value => String(value).replaceAll('\\', '/');
const outputByFile = new Map();
for (const [outputPath, meta] of Object.entries(outputs)) {
  const name = normalized(outputPath);
  outputByFile.set(basename(name), { outputPath: name, meta });
  outputByFile.set(name, { outputPath: name, meta });
}

function findOutput(ref) {
  const clean = normalized(ref);
  return outputByFile.get(clean)
    ?? outputByFile.get(basename(clean))
    ?? [...outputByFile.values()].find(item => item.outputPath.endsWith('/' + clean));
}

function packageName(sourcePath) {
  const source = normalized(sourcePath);
  const marker = '/node_modules/';
  const direct = source.startsWith('node_modules/') ? source.slice('node_modules/'.length) : null;
  const nestedIndex = source.lastIndexOf(marker);
  const rest = direct ?? (nestedIndex >= 0 ? source.slice(nestedIndex + marker.length) : null);
  if (!rest) return source.startsWith('src/') ? '(app source)' : '(other)';
  const parts = rest.split('/');
  return parts[0]?.startsWith('@') ? parts.slice(0, 2).join('/') : parts[0];
}

function collectContributors(refs) {
  const modules = new Map();
  const packages = new Map();
  const unresolved = [];

  for (const ref of refs) {
    const output = findOutput(ref);
    if (!output) {
      unresolved.push(ref);
      continue;
    }
    const inputs = output.meta?.inputs ?? {};
    for (const [sourcePath, contribution] of Object.entries(inputs)) {
      const bytes = Number(contribution?.bytesInOutput ?? 0);
      if (!Number.isFinite(bytes) || bytes <= 0) continue;
      modules.set(sourcePath, (modules.get(sourcePath) ?? 0) + bytes);
      const pkg = packageName(sourcePath);
      packages.set(pkg, (packages.get(pkg) ?? 0) + bytes);
    }
  }

  const sort = map => [...map.entries()]
    .map(([name, bytes]) => ({ name: normalized(name), bytes }))
    .sort((a, b) => b.bytes - a.bytes);

  return {
    unresolved,
    modules: sort(modules).slice(0, 40),
    packages: sort(packages).slice(0, 30)
  };
}

const allOutputs = Object.entries(outputs)
  .map(([outputPath, meta]) => ({
    outputPath: normalized(outputPath),
    file: basename(normalized(outputPath)),
    bytes: Number(meta?.bytes ?? 0),
    entryPoint: meta?.entryPoint ? normalized(meta.entryPoint) : null
  }))
  .filter(item => /\.(?:js|css)$/.test(item.file));

const outputKeyByFile = new Map();
for (const outputPath of Object.keys(outputs)) {
  outputKeyByFile.set(basename(normalized(outputPath)), outputPath);
}

const initialSet = new Set(documentRefs.map(ref => basename(normalized(ref))));
const queue = [...initialSet];
while (queue.length) {
  const currentFile = queue.shift();
  const outputKey = outputKeyByFile.get(currentFile);
  const output = outputKey ? outputs[outputKey] : null;
  for (const imported of output?.imports ?? []) {
    if (imported.external || imported.kind === 'dynamic-import') continue;
    const importedFile = basename(normalized(imported.path));
    if (!/\.(?:js|css)$/.test(importedFile) || initialSet.has(importedFile)) continue;
    initialSet.add(importedFile);
    queue.push(importedFile);
  }
}

const initialRefs = [...initialSet];
const initialOutputs = allOutputs
  .filter(item => initialSet.has(item.file))
  .sort((a, b) => b.bytes - a.bytes);
const lazyOutputs = allOutputs
  .filter(item => !initialSet.has(item.file))
  .sort((a, b) => b.bytes - a.bytes);

const contributors = collectContributors(initialRefs);
const stylesRefs = initialRefs.filter(ref => ref.endsWith('.css'));
const scriptsRefs = initialRefs.filter(ref => ref.endsWith('.js'));

const report = {
  generatedAtUtc: new Date().toISOString(),
  statsFile: relative(cwd, statsPath).replaceAll('\\', '/'),
  initialRefs,
  initialOutputs,
  initialRawBytesFromStats: initialOutputs.reduce((total, item) => total + item.bytes, 0),
  topInitialPackages: contributors.packages,
  topInitialModules: contributors.modules,
  topInitialScriptPackages: collectContributors(scriptsRefs).packages,
  topInitialStylePackages: collectContributors(stylesRefs).packages,
  largestLazyOutputs: lazyOutputs.slice(0, 30),
  unresolvedInitialOutputs: contributors.unresolved
};

const outputPath = join(distRoot, 'angular-stats-summary.json');
writeFileSync(outputPath, JSON.stringify(report, null, 2) + '\n');

console.info('SOLQARYN Angular stats summary');
console.info('Initial outputs:');
console.table(report.initialOutputs);
console.info('Top initial packages:');
console.table(report.topInitialPackages.slice(0, 20));
console.info('Top initial modules:');
console.table(report.topInitialModules.slice(0, 20));
console.info('Largest lazy outputs:');
console.table(report.largestLazyOutputs.slice(0, 15));
console.info(`Reporte guardado en ${relative(cwd, outputPath)}`);
