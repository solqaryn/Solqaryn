import fs from 'node:fs';
import path from 'node:path';

const root = path.resolve('dist/solqaryn-frontend');
const statsPath = [path.join(root, 'stats.json'), path.join(root, 'browser', 'stats.json')].find(file => fs.existsSync(file));
const indexPath = [path.join(root, 'browser', 'index.html'), path.join(root, 'index.html')].find(file => fs.existsSync(file));
if (!statsPath || !indexPath) {
  console.warn('[initial-stats] No se encontraron metadatos e index del build. Diagnóstico no verificado.');
  process.exit(0);
}

const { outputs } = JSON.parse(fs.readFileSync(statsPath, 'utf8'));
if (!outputs || typeof outputs !== 'object') {
  console.warn('[initial-stats] Formato de metadatos no reconocido.');
  process.exit(0);
}
const normalized = value => value.replaceAll('\\', '/');
const byName = new Map();
for (const key of Object.keys(outputs)) {
  const name = path.posix.basename(normalized(key));
  if (/\.(?:js|css)$/.test(name)) {
    const previous = byName.get(name);
    if (previous && previous !== key) {
      console.warn('[initial-stats] Nombre de salida ambiguo: ' + name);
      process.exit(0);
    }
    byName.set(name, key);
  }
}
const html = fs.readFileSync(indexPath, 'utf8');
const entries = [...html.matchAll(/(?:src|href)=["']([^"']+\.(?:js|css))(?:\?[^"']*)?["']/g)]
  .map(match => match[1]).filter(value => !/^(?:[a-z]+:|\/\/)/i.test(value))
  .map(value => byName.get(path.posix.basename(value))).filter(Boolean);
const visited = new Set();
const totals = new Map();
function walk(key) {
  if (!key || visited.has(key) || !outputs[key]) return;
  visited.add(key);
  const output = outputs[key];
  for (const [input, info] of Object.entries(output.inputs || {})) {
    totals.set(input, (totals.get(input) || 0) + (info.bytesInOutput || 0));
  }
  for (const entry of output.imports || []) {
    if (entry.external || entry.kind === 'dynamic-import') continue;
    walk(outputs[entry.path] ? entry.path : byName.get(path.posix.basename(normalized(entry.path))));
  }
}
entries.forEach(walk);
if (!visited.size) {
  console.warn('[initial-stats] No se identificaron entradas iniciales; no se estima una reducción.');
  process.exit(0);
}
console.log('[initial-stats] Archivos iniciales identificados: ' + visited.size + '; bytes: ' + [...visited].reduce((sum, key) => sum + (outputs[key].bytes || 0), 0));
for (const [input, bytes] of [...totals.entries()].sort((a, b) => b[1] - a[1]).slice(0, 30)) {
  console.log('[initial-stats] ' + bytes + ' bytes ' + input);
}
