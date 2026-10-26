#!/usr/bin/env node
// P0-011.A: deterministic, read-only contract test for the commercial scope matrix.
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const source = fileURLToPath(new URL('../docs/P0-011-A-FRONTERA-VERSION-INICIAL-DEV.md', import.meta.url));
const expected = new Map([
  ...Array.from({ length: 10 }, (_, i) => [`C${String(i + 1).padStart(2, '0')}`, 'INICIAL']),
  ['C11', 'CONDICIONAL'], ['C12', 'CONDICIONAL'], ['C13', 'CONDICIONAL'],
  ...Array.from({ length: 7 }, (_, i) => [`C${String(i + 14).padStart(2, '0')}`, 'FUTURO']),
]);
const tableLine = /^\|\s*C\d{2}\s*\|/;

export function validate(text) {
  const errors = [];
  const actual = new Map();
  const lines = text.split(/\r?\n/).filter((line) => tableLine.test(line));
  for (const line of lines) {
    const columns = line.trim().split('|').slice(1, -1).map((part) => part.trim());
    if (columns.length !== 5) {
      errors.push('Fila con columnas invalidas');
      continue;
    }
    const [id, requirement, classification, justification, sourceRef] = columns;
    if (!expected.has(id)) errors.push(`ID no autorizado: ${id}`);
    if (actual.has(id)) errors.push(`ID duplicado: ${id}`);
    actual.set(id, classification);
    if (classification !== expected.get(id)) errors.push(`Clase incorrecta: ${id}=${classification}`);
    if (requirement.length < 12) errors.push(`Requisito incompleto: ${id}`);
    if (justification.length < 55) errors.push(`Justificacion insuficiente: ${id}`);
    if (!sourceRef.startsWith('§')) errors.push(`Fuente ausente: ${id}`);
  }
  for (const id of expected.keys()) if (!actual.has(id)) errors.push(`ID faltante: ${id}`);
  if (actual.size !== expected.size) errors.push(`Conteo: ${actual.size}/${expected.size}`);
  return errors;
}

function lineOf(text, id) {
  return text.split(/\r?\n/).find((line) => line.startsWith(`| ${id} |`));
}

function changedCategory(text, id, category) {
  const original = lineOf(text, id);
  if (!original) throw Error(`Fixture faltante: ${id}`);
  const cells = original.split('|');
  cells[3] = ` ${category} `;
  return text.replace(original, cells.join('|'));
}

function blankReason(text, id) {
  const original = lineOf(text, id);
  if (!original) throw Error(`Fixture faltante: ${id}`);
  const cells = original.split('|');
  cells[4] = '  ';
  return text.replace(original, cells.join('|'));
}

const input = readFileSync(source, 'utf8');
const errors = validate(input);
if (errors.length) {
  console.error('FAIL P0-011.A:', errors.join('; '));
  process.exit(1);
}
if (process.argv.includes('--self-test')) {
  const mutations = [
    ['FUTURO no entra al release', changedCategory(input, 'C16', 'INICIAL')],
    ['dominio sin DNS/TLS no es INICIAL', changedCategory(input, 'C12', 'INICIAL')],
    ['id duplicado', input + '\n' + lineOf(input, 'C03')],
    ['id faltante', input.replace(lineOf(input, 'C20') + '\n', '')],
    ['justificacion vacia', blankReason(input, 'C04')],
  ];
  for (const [name, mutated] of mutations) {
    if (validate(mutated).length === 0) {
      console.error(`FAIL fixture negativo: ${name}`);
      process.exit(1);
    }
  }
  console.log(`PASS P0-011.A 20/20 requisitos; 5/5 negativos; read-only; node ${process.version}`);
} else {
  console.log('PASS P0-011.A 20/20 requisitos; read-only');
}
