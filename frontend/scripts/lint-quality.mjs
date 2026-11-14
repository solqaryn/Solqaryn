import { readdir, readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const sourceDirectory = path.resolve(scriptDirectory, '../src');
const supportedExtensions = new Set(['.ts', '.html', '.scss']);

const rules = [
  {
    name: 'marcadores de conflicto',
    expression: /^(<<<<<<<|=======|>>>>>>>)(?:\s|$)/m
  },
  {
    name: 'sentencias debugger',
    expression: /\bdebugger\s*;/
  },
  {
    name: 'salidas de consola de depuración',
    expression: /\bconsole\.(log|debug|trace)\s*\(/
  },
  {
    name: 'URLs javascript inseguras',
    expression: /(?:href|src)\s*=\s*["']javascript:/i
  },
  {
    name: 'tabindex positivo que rompe el orden natural de teclado',
    expression: /tabindex\s*=\s*["'][1-9]\d*["']/i
  },
  {
    name: 'foco visible eliminado',
    expression: /outline\s*:\s*(?:none|0(?:px)?)(?:\s*!important)?\s*;/i,
    extensions: new Set(['.scss'])
  }
];

async function collectFiles(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const files = [];

  for (const entry of entries) {
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      files.push(...await collectFiles(fullPath));
      continue;
    }

    if (supportedExtensions.has(path.extname(entry.name))) files.push(fullPath);
  }

  return files;
}

const failures = [];
for (const file of await collectFiles(sourceDirectory)) {
  const content = await readFile(file, 'utf8');
  const extension = path.extname(file);
  for (const rule of rules) {
    if (rule.extensions && !rule.extensions.has(extension)) continue;
    const match = rule.expression.exec(content);
    if (!match) continue;

    const line = content.slice(0, match.index).split(/\r?\n/).length;
    failures.push(`${path.relative(path.resolve(scriptDirectory, '..'), file)}:${line} — ${rule.name}`);
  }
}

if (failures.length > 0) {
  console.error('La validación estática encontró problemas:');
  failures.forEach((failure) => console.error(`- ${failure}`));
  process.exit(1);
}

console.info('Validación estática aprobada: calidad, seguridad básica y guardas de accesibilidad sin regresiones.');
