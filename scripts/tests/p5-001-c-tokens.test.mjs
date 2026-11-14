import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

// P5-001.C: delimitar un archivo productivo; no instalar otro motor de temas.
const realFile = fileURLToPath(new URL('../../frontend/src/styles.scss', import.meta.url));
const actual = readFileSync(realFile, 'utf8');
const colors = [
  '--color-primary','--color-primary-dark','--color-accent','--color-bg',
  '--color-surface','--color-sidebar','--color-topbar','--color-heading',
  '--color-button','--color-text','--color-text-muted','--color-border',
  '--color-danger','--color-success','--color-warning','--color-info','--color-on-primary',
];
export function inspectInstitutionalTokens(source) {
  const failures = [], verify = (ok,key) => { if (!ok) failures.push(key); };
  const root = source.match(/(?:^|\n)\s*:root\s*\{([^}]*)\}/s)?.[1] ?? '';
  verify(Boolean(root),'ROOT_MISSING');
  const declarations = [...root.matchAll(/(?:^|\n)\s*(--[a-z][\w-]*)\s*:\s*([^;]*);/g)]
    .map(x => [x[1],x[2].trim()]);
  const value = name => declarations.find(x => x[0]===name)?.[1];
  for (const name of colors) {
    const found = declarations.filter(x => x[0]===name);
    verify(found.length===1,'TOKEN_COUNT_'+name);
    if (found.length===1) {
      verify(/^#[\da-f]{6}$/i.test(found[0][1]),'HEX_'+name);
      verify(!/url\s*\(|expression\s*\(|javascript\s*:|!important/i.test(found[0][1]),'UNSAFE_'+name);
    }
  }
  verify(value('--color-card')==='var(--color-surface)','CARD_ALIAS');
  verify(value('--color-icon')==='var(--color-text)','ICON_ALIAS');
  verify(value('--focus-ring')?.includes('var(--color-primary)')===true,'FOCUS_TOKEN');
  const body = source.match(/(?:^|\n)\s*body\s*\{([^}]*)\}/s)?.[1] ?? '';
  verify(/background:\s*var\(--color-bg\)\s*;/.test(body) &&
    /color:\s*var\(--color-text\)\s*;/.test(body),'BODY_TOKENS');
  verify(source.includes('--mdc-filled-button-container-color: var(--color-button);') &&
    source.includes('--mdc-filled-button-label-text-color: var(--color-on-primary);'),'MATERIAL_TOKENS');
  verify(source.includes('&:focus-visible') && source.includes('box-shadow: var(--focus-ring);'),'KEYBOARD_FOCUS');
  return failures;
}
test('P5-001.C: tokens institucionales reales delimitados en styles.scss',()=>{
  assert.deepEqual(inspectInstitutionalTokens(actual),[]);
});
test('P5-001.C: token obligatorio ausente falla',()=>{
  const bad=actual.replace(/--color-primary:\s*#[\da-f]{6};/i,'--color-removed: #0369a1;');
  assert.notEqual(bad,actual);
  assert.ok(inspectInstitutionalTokens(bad).includes('TOKEN_COUNT_--color-primary'));
});
test('P5-001.C: rechaza CSS peligroso y valor no hexadecimal',()=>{
  const bad=actual.replace(/--color-warning:\s*#[\da-f]{6};/i,'--color-warning: url(javascript:bad);');
  assert.notEqual(bad,actual);
  assert.ok(inspectInstitutionalTokens(bad).includes('HEX_--color-warning'));
  assert.ok(inspectInstitutionalTokens(bad).includes('UNSAFE_--color-warning'));
});
test('P5-001.C: variable duplicada falla',()=>{
  const bad=actual.replace(/(--color-primary:\s*#[\da-f]{6};)/i,'$1\n  --color-primary: #123456;');
  assert.notEqual(bad,actual);
  assert.ok(inspectInstitutionalTokens(bad).includes('TOKEN_COUNT_--color-primary'));
});
test('P5-001.C: alias de card e icono dependen de fuentes semanticas',()=>{
  const bad=actual.replace('--color-card: var(--color-surface);','--color-card: #ffffff;')
    .replace('--color-icon: var(--color-text);','--color-icon: #ffffff;');
  assert.notEqual(bad,actual);
  assert.ok(inspectInstitutionalTokens(bad).includes('CARD_ALIAS'));
  assert.ok(inspectInstitutionalTokens(bad).includes('ICON_ALIAS'));
});
test('P5-001.C: body y botones Material consumen tokens',()=>{
  const bad=actual.replace('background: var(--color-bg);','background: #f3f7fb;')
    .replace('--mdc-filled-button-container-color: var(--color-button);','--mdc-filled-button-container-color: #0369a1;');
  assert.notEqual(bad,actual);
  assert.ok(inspectInstitutionalTokens(bad).includes('BODY_TOKENS'));
  assert.ok(inspectInstitutionalTokens(bad).includes('MATERIAL_TOKENS'));
});
test('P5-001.C: foco visible y primario no desaparecen',()=>{
  const focus=actual.replace('&:focus-visible','&:hover');
  const token=actual.replace(/(--focus-ring:\s*[^;]*?)var\(--color-primary\)/,'$1#ffffff');
  assert.notEqual(focus,actual); assert.notEqual(token,actual);
  assert.ok(inspectInstitutionalTokens(focus).includes('KEYBOARD_FOCUS'));
  assert.ok(inspectInstitutionalTokens(token).includes('FOCUS_TOKEN'));
});
test('P5-001.C: fuente vacia falla cerrada',()=>{
  assert.ok(inspectInstitutionalTokens('').length>=15);
});
