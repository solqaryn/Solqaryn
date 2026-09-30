import { existsSync, readFileSync, readdirSync, writeFileSync } from 'node:fs';
import { join, relative, resolve } from 'node:path';
import { brotliCompressSync, gzipSync } from 'node:zlib';

const distRoot = resolve(process.cwd(), 'dist/solqaryn-frontend');
const browserRoot = existsSync(join(distRoot, 'browser')) ? join(distRoot, 'browser') : distRoot;
const indexPath = join(browserRoot, 'index.html');

if (!existsSync(indexPath)) {
  console.error('No se encontró el build productivo. Ejecuta npm run build:prod antes del baseline.');
  process.exit(1);
}

function collect(directory) {
  const files = [];
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const fullPath = join(directory, entry.name);
    if (entry.isDirectory()) files.push(...collect(fullPath));
    else if (/\.(js|css)$/.test(entry.name)) files.push(fullPath);
  }
  return files;
}

function metrics(path) {
  const content = readFileSync(path);
  return {
    file: relative(browserRoot, path).replaceAll('\\', '/'),
    rawBytes: content.length,
    gzipBytes: gzipSync(content).length,
    brotliBytes: brotliCompressSync(content).length
  };
}

const all = collect(browserRoot).map(metrics);
const index = readFileSync(indexPath, 'utf8');
const initialRefs = new Set(
  [...index.matchAll(/(?:src|href)=["']([^"']+\.(?:js|css))["']/g)]
    .map(match => match[1].replace(/^\.\//, '').replace(/^\//, ''))
);
const initial = all.filter(file => initialRefs.has(file.file));

function sum(files, key) {
  return files.reduce((total, file) => total + file[key], 0);
}

const report = {
  generatedAtUtc: new Date().toISOString(),
  buildRoot: relative(process.cwd(), browserRoot).replaceAll('\\', '/'),
  existingInitialBudget: {
    warningBytes: 1024 * 1024,
    errorBytes: 2 * 1024 * 1024
  },
  initial: {
    fileCount: initial.length,
    rawBytes: sum(initial, 'rawBytes'),
    gzipBytes: sum(initial, 'gzipBytes'),
    brotliBytes: sum(initial, 'brotliBytes'),
    files: initial.sort((a, b) => b.rawBytes - a.rawBytes)
  },
  allBundles: {
    fileCount: all.length,
    rawBytes: sum(all, 'rawBytes'),
    gzipBytes: sum(all, 'gzipBytes'),
    brotliBytes: sum(all, 'brotliBytes'),
    largest: [...all].sort((a, b) => b.rawBytes - a.rawBytes).slice(0, 15)
  }
};

const outputPath = join(distRoot, 'performance-bundle-baseline.json');
writeFileSync(outputPath, JSON.stringify(report, null, 2) + '\n');

console.info('SOLQARYN bundle baseline');
console.table(report.initial.files);
console.info(JSON.stringify(report, null, 2));
console.info(`Baseline guardado en ${relative(process.cwd(), outputPath)}`);
