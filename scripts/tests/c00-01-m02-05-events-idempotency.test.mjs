import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
const doc = readFileSync(new URL('../../docs/v5/c00-01-m02-05-events-idempotency.md', import.meta.url), 'utf8');
const files=['CorrelationIdMiddleware.cs','MovimientoInventarioCorrelationId.cs','OrdenCompra.cs','MensajeOutbox.cs','MensajeOutboxService.cs'];
const phrases=['V5 FINAL','C00-01.M02.05','PROJECT_SCOPE_LOCK=STRICT','PLAN_MAESTRO!H','COLA!E/G/O','BITACORA!A:U','X-Correlation-ID','EmpresaId','IdempotencyFingerprint','ForbiddenAccessException','A5_PENDIENTE','otro TASK_ID','git revert','NO_RUNTIME_CERTIFICATION','NO_APLICA'];
function valid(src) {
  const low=src.normalize('NFC').toLowerCase();
  const rows=src.split('\n').filter(x=>x.startsWith('| ')&&x.includes('backend/src/'));
  return rows.length===5 && files.every(f=>src.includes(f)) && rows.every(x=>/`[a-f0-9]{40}`/.test(x))
    && phrases.every(p=>low.includes(p.toLowerCase()))
    && /mismatch lanza `ForbiddenAccessException`/i.test(src)
    && /`origenId <= 0` se rechaza/.test(src)
    && /empresa A sobre empresa B debe fallar cerrado/.test(src)
    && /`SKIPPED`, `PENDING`, `FAILURE` o un check de otro SHA NO es PASS/.test(src)
    && /`A5_PASS` independiente precede QA\/main\/PROD/.test(src)
    && /nunca `git reset --hard`, force-push/.test(src)
    && /no valida identidad, firma ni autorizacion/.test(src);
}
test('A1 positivo: contrato de cinco fuentes reales y limites',()=>assert.equal(valid(doc),true));
test('A0 negativo: faltar blob origen es falta de trazabilidad',()=>assert.equal(valid(doc.replace('3b76dca41bb4bc0b44f89c2a4664c79907f9f242','badblob')),false));
test('A1 negativo: correlacion no puede autenticar',()=>assert.equal(valid(doc.replace('no valida identidad, firma ni autorizacion','valida identidad, firma y autorizacion')),false));
test('A2 negativo: empresa A no puede leer B',()=>assert.equal(valid(doc.replace('empresa A sobre empresa B debe fallar cerrado','empresa A sobre empresa B puede pasar')),false));
test('A2 negativo: no permitir origenId no positivo',()=>assert.equal(valid(doc.replace('`origenId <= 0` se rechaza','`origenId <= 0` se permite')),false));
test('A2 negativo: conserva rechazo de tenant outbox',()=>assert.equal(valid(doc.replace('Mismatch lanza `ForbiddenAccessException`','mismatch retorna Ok')),false));
test('A3 negativo: no omitir A5 independiente',()=>assert.equal(valid(doc.replaceAll('A5_PENDIENTE','A5_AUTOR')),false));
test('A3 negativo: SKIPPED no es PASS',()=>assert.equal(valid(doc.replace('`SKIPPED`, `PENDING`, `FAILURE` o un check de otro SHA NO es PASS','`SKIPPED` es PASS')),false));
test('A3 negativo: no sustituir rollback por reset destructivo',()=>assert.equal(valid(doc.replace('nunca `git reset --hard`, force-push','hacer `git reset --hard`, force-push')),false));
