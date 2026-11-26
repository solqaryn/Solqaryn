import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';

// El log causal del build es la fuente del presupuesto de Angular. El desglose
// por entrada es diagnóstico adicional, nunca un sustituto del tamaño medido.
const logPath = process.argv[2];
if (!logPath || !fs.existsSync(logPath)) {
  console.error('[initial-stats] Falta el log del build exacto.');
  process.exit(1);
}
const log = fs.readFileSync(logPath, 'utf8').replace(/\u001b\[[0-9;]*m/g, '');
const totals = [...log.matchAll(/\|\s*Initial total\s*\|\s*(\d+(?:\.\d+)?)\s*(bytes|kB|MB)\s*\|/gi)];
if (totals.length !== 1) {
  console.error('[initial-stats] No se pudo acreditar un único tamaño inicial.');
  process.exit(1);
}
const [, amount, unit] = totals[0];
const bytes = Number(amount) * ({ bytes: 1, kb: 1000, mb: 1000000 }[unit.toLowerCase()]);
const warnings = log.split('\n').filter(line => /exceeded maximum budget/i.test(line));
console.log('[initial-stats] Tamaño inicial Angular: ' + amount + ' ' + unit + '; aviso: 650 kB.');
if (!Number.isFinite(bytes) || bytes > 650000 || warnings.length) {
  for (const warning of warnings) console.error('[initial-stats] ' + warning.trim());
  console.error('[initial-stats] Presupuesto de advertencia no cumplido; no se certifica rendimiento.');
  process.exitCode = 1;
} else {
  console.log('[initial-stats] Presupuestos de advertencia: PASS');
}

// Reutilizar el analizador existente: no mantener dos algoritmos de contribuciones.
const root = path.resolve('dist/solqaryn-frontend');
const browserRoot = fs.existsSync(path.join(root, 'browser')) ? path.join(root, 'browser') : root;
const candidates = [
  path.join(root, 'stats.json'), path.join(root, 'browser-stats.json'),
  path.join(browserRoot, 'stats.json'), path.join(browserRoot, 'browser-stats.json'),
  path.resolve('stats.json'), path.resolve('browser-stats.json')
];
if (!fs.existsSync(path.join(browserRoot, 'index.html')) || !candidates.some(file => fs.existsSync(file))) {
  console.warn('[initial-stats] Desglose por entrada no disponible: faltan metadatos o index; el presupuesto se comprueba por el log causal.');
} else {
  const result = spawnSync(process.execPath, [path.resolve('scripts/analyze-angular-stats.mjs')], { stdio: 'inherit' });
  if (result.status !== 0) {
    console.error('[initial-stats] No se pudo verificar el desglose de contribuciones.');
    process.exitCode = 1;
  }
}
