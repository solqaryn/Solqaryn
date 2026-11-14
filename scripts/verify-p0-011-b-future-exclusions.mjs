#!/usr/bin/env node
// P0-011.B: independent, read-only negative boundary check for the initial commercial scope.
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const file = fileURLToPath(new URL('../docs/P0-011-A-FRONTERA-VERSION-INICIAL-DEV.md', import.meta.url));
const expected = new Map([
  ...Array.from({ length: 10 }, (_, i) => [`C${String(i + 1).padStart(2, '0')}`, 'INICIAL']),
  ['C11', 'CONDICIONAL'], ['C12', 'CONDICIONAL'], ['C13', 'CONDICIONAL'],
  ...Array.from({ length: 7 }, (_, i) => [`C${String(i + 14).padStart(2, '0')}`, 'FUTURO']),
]);
const identityTerms = new Map([
  ['C07', 'manual'],
  ['C08', 'JWT'],
  ['C14', 'Dedicated/shared'],
  ['C16', 'Experience Studio'],
  ['C17', 'self-service'],
  ['C18', 'agentes IA'],
  ['C19', 'Industry packs'],
  ['C20', 'upgrades'],
]);
const isRow = (line) => /^\|\s*C\d{2}\s*\|/.test(line);

export function validateBoundary(text) {
  const errors = [];
  const seen = new Set();
  for (const line of text.split(/\r?\n/).filter(isRow)) {
    const cells = line.split('|').slice(1, -1).map((x) => x.trim());
    if (cells.length !== 5) {
      errors.push('Fila con estructura invalida');
      continue;
    }
    const [id, name, category, reason, reference] = cells;
    if (!expected.has(id)) errors.push(`ID ajeno al alcance: ${id}`);
    if (seen.has(id)) errors.push(`ID duplicado: ${id}`);
    seen.add(id);
    if (category !== expected.get(id)) errors.push(`Clase prohibida: ${id} -> ${category}`);
    if (name.length < 12) errors.push(`Nombre insuficiente: ${id}`);
    const term = identityTerms.get(id);
    if (term && !name.includes(term)) errors.push(`Identidad del requisito cambiada: ${id}`);
    if (reason.length < 55) errors.push(`Justificacion insuficiente: ${id}`);
    if (!reference.startsWith('§')) errors.push(`Referencia ausente: ${id}`);
  }
  for (const id of expected.keys()) if (!seen.has(id)) errors.push(`Fila faltante: ${id}`);
  if (seen.size !== 20) errors.push(`Cantidad no valida: ${seen.size}/20`);
  return errors;
}

function rowOf(text, id) {
  return text.split(/\r?\n/).find((line) => line.startsWith(`| ${id} |`));
}

function rewrite(text, id, cell, value) {
  const original = rowOf(text, id);
  if (!original) throw Error(`Fixture sin fila: ${id}`);
  const parts = original.split('|');
  parts[cell] = ` ${value} `;
  return text.replace(original, parts.join('|'));
}

function negativeFixtures(text) {
  return [
    ['Experience Studio incluido indebidamente', rewrite(text, 'C16', 3, 'INICIAL')],
    ['IA/SDK incluido indebidamente', rewrite(text, 'C18', 3, 'INICIAL')],
    ['Enterprise industry packs incluido', rewrite(text, 'C19', 3, 'INICIAL')],
    ['Billing automatico habilitado', rewrite(text, 'C17', 3, 'INICIAL')],
    ['Placement dedicado habilitado', rewrite(text, 'C14', 3, 'INICIAL')],
    ['Upgrades pagados habilitados', rewrite(text, 'C20', 3, 'INICIAL')],
    ['DNS/TLS condicionado activado', rewrite(text, 'C12', 3, 'INICIAL')],
    ['Seguridad server-side diferida', rewrite(text, 'C08', 3, 'FUTURO')],
    ['Contratacion manual diferida', rewrite(text, 'C07', 3, 'FUTURO')],
    ['Fila futura duplicada', text + '\n' + rowOf(text, 'C16')],
    ['Fila IA eliminada', text.split(/\r?\n/).filter((line) => line !== rowOf(text, 'C18')).join('\n')],
    ['Experience Studio ocultado en titulo', rewrite(text, 'C16', 2, 'Editor de interfaz ya habilitado')],
  ];
}

const input = readFileSync(file, 'utf8');
const errors = validateBoundary(input);
if (errors.length) {
  console.error('FAIL P0-011.B:', errors.join('; '));
  process.exitCode = 1;
} else if (process.argv.includes('--self-test')) {
  let fails = 0;
  for (const [name, mutated] of negativeFixtures(input)) {
    if (validateBoundary(mutated).length === 0) {
      console.error(`FAIL fixture negativo: ${name}`);
      fails += 1;
    }
  }
  if (fails) process.exitCode = 1;
  else console.log(`PASS P0-011.B 20/20 filas; 12/12 negativos; FUTURO/CONDICIONAL excluidos; read-only; node ${process.version}`);
} else {
  console.log('PASS P0-011.B frontera FUTURO/CONDICIONAL; 20/20 filas; read-only');
}
