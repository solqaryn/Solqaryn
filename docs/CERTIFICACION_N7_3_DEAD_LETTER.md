# Certificación ERP-N7.3 — Dead-letter del Outbox

## Alcance certificado

ERP-N7.3 certifica el estado terminal DeadLetter del Outbox y la evidencia necesaria para operarlo de forma tenant-bound, idempotente y auditable. La autoridad operativa única es `docs/VAEP_AUTHORITY.md` y la rama certificada es `Desarrollo`.

El alcance no introduce una segunda cola, una tabla DeadLetter separada, un endpoint de replay manual ni una superficie frontend adicional. Tampoco toca `main`, Producción, deploy, secretos ni PR #2.

## Contrato funcional

- `MensajeOutbox` exige identidad tenant y clave de idempotencia durable.
- El estado DeadLetter es terminal y no puede volver a reclamarse como pendiente/procesando mediante el contrato normal.
- El agotamiento de intentos mueve el mensaje a DeadLetter usando confirmación tenant-bound y el intento esperado; un claim supersedido no debe forzar una transición obsoleta.
- El error persistido/auditado se mantiene en una allow-list segura; no se registran payload, credenciales, `Authorization`, secreto ni datos sensibles del proveedor.
- La persistencia reutiliza el esquema Outbox existente; N7.3.C certificó que no era necesaria una migración destructiva ni una tabla paralela.
- La reconstrucción de auditoría conserva TenantId/OutboxId/CorrelationId/Resultado y error seguro, sin ampliar el scope a replay operativo.

## Evidencia canónica A–G

### N7.3.A — PRE

- Receipt: `vaep/evidence/receipts/N7.3.A_LISTO_REAL_20260913T153201Z.json`, commit `e01492dc3e2db823e83930f8857d9cd8d0061f06`.
- REVIEW_FIRST: `vaep/evidence/reviews/N7.3.A_REVIEW_FIRST_20260913T153133Z_SUP24.json`, P0=0/P1=0.
- Preflight confirmó que el estado DeadLetter, persistencia y evento de auditoría ya existían; no se requirió endpoint manual de replay ni tabla DeadLetter nueva.

### N7.3.B — DOMAIN

- Receipt: `vaep/evidence/receipts/N7.3.B_LISTO_REAL_20260913T155200Z.json`, commit `1df336afff5e9213038b6ddfa540956c3aef1535`.
- REVIEW_FIRST: `vaep/evidence/reviews/N7.3.B_REVIEW_FIRST_20260913T154800Z_SUP48.json`, P0=0/P1=0.
- Disposición: `SATISFIED_NA_IMPLEMENTATION`; las invariantes existentes ya cubren identidad tenant, idempotencia, ventana de retry, agotamiento de intentos y terminalidad DeadLetter.

### N7.3.C — PERSISTENCE

- Receipt: `vaep/evidence/receipts/N7.3.C_LISTO_REAL_20260913T170100Z.json`, commit `c1bed91798d2f759d6c78d7858455b4933e64238`.
- Candidate funcional: `734a85911009faf352fe7d89a87f6575f0a112e5`.
- REVIEW_FIRST: `vaep/evidence/reviews/N7.3.C_REVIEW_FIRST_20260913T165037Z_SUP48.json`, P0=0/P1=0.
- Certificado dirigido: `backend/tests/Solqaryn.Tests/N73COutboxDeadLetterPersistenceTests.cs`.
- Gate causal: run `34768580714`, job `103753938935`; Release build 0 warnings/0 errors, backend 2205/2205 y MySQL 8.4 PASS.
- Se reutilizó `20260913050800_N71COutboxPersistence`; no se creó migración duplicada o destructiva.

### N7.3.D — BACKEND_API

- Receipt: `vaep/evidence/receipts/N7.3.D_LISTO_REAL_20260913T173523Z.json`, commit `2359480b211d359035a521555d15be92b86ffc7b`.
- Candidate funcional: `ca7870f7e565fe408861db04850bcbdd085c8f02`.
- REVIEW_FIRST: `vaep/evidence/reviews/N7.3.D_REVIEW_FIRST_20260913T172708Z_SUP12.json`, P0=0/P1=0.
- Certificado dirigido: `backend/tests/Solqaryn.Tests/N73DOutboxDeadLetterApplicationTests.cs`.
- Backend gate `34771524112` / `103761874096` PASS; gate estático recuperado same-run `34771650570` / `103762215012` SUCCESS.
- No se agregó endpoint nuevo: el comportamiento requerido queda cubierto por `OutboxRetryProcessor` y el CAS tenant-bound existente.

### N7.3.E — FRONTEND_UX

- Receipt: `vaep/evidence/receipts/N7.3.E_LISTO_REAL_20260913T173710Z.json`, commit `a9b640f5e9b7071d46474d78055bab474d42c154`.
- REVIEW_FIRST: `vaep/evidence/reviews/N7.3.E_REVIEW_FIRST_20260913T173700Z_SUP12.json`, P0=0/P1=0.
- Disposición: `N_A_NO_FRONTEND_SURFACE_REQUIRED`; no se amplió scope a una UI de replay manual. Lint y production build aplicables quedaron PASS por equivalencia causal documentada.

### N7.3.F — SEC_AUDIT

- Receipt: `vaep/evidence/receipts/N7.3.F_LISTO_REAL_20260913T181100Z.json`, commit `65390cba011c62ed00999eaff91e71719bec3274`.
- Candidate funcional: `590e4e3d6cca3c984c1620370b34bfed0995965c`.
- REVIEW_FIRST: `vaep/evidence/reviews/N7.3.F_REVIEW_FIRST_20260913T181040Z_SUP48_CLOSURE.json`, P0=0/P1=0.
- Gate seguridad `34773501994` / `103767278498` SUCCESS; gate estático backend `34773501987` / `103767277402` completó el step causal de restore/build/test en SUCCESS.
- La allow-list de auditoría conserva metadata de reconstrucción y excluye payload, idempotency key, datos del proveedor y secretos.

### N7.3.G — TEST_CI

- Receipt: `vaep/evidence/receipts/N7.3.G_LISTO_REAL_20260913T191310Z.json`, commit `1f5a62910b3a84b99ee90b45d204ee2fcf240f75`.
- Candidate funcional final: `2c19221fdbb0a3cdfdcf8afdc839999bf8e80265`.
- REVIEW_FIRST: `vaep/evidence/reviews/N7.3.G_REVIEW_FIRST_20260913T190103Z_SUP48_RECOVERY.json`, P0=0/P1=0; review head `67be3a7aff8f6ce77bb6de27f1a96187452565ad` es evidence-only.
- Recovery same-run: el candidate inicial `98c14b1eca2a07d33337e0ed7117f1de99cc7bf8` falló en run `34774715570` / job `103770603867` por namespaces/API obsoletos del test; `2c19221f...` corrigió únicamente `N73GDeadLetterQaRegressionTests.cs`.
- Gates causales terminales sobre `2c19221f...`: `34776343835/103775053849=SUCCESS`, `34776340372/103775044416=SUCCESS` y `34776340372/103775044622=SUCCESS`.
- La regresión certifica agotamiento→DeadLetter, aislamiento tenant, identidad idempotente estable y bloqueo de reprocesamiento terminal.

## Operación y recuperación

1. Procesar siempre dentro de un `empresaId` explícito y con la identidad durable del mensaje.
2. No volver a reclamar mensajes DeadLetter mediante el flujo normal.
3. Ante fallo externo, conservar la transición condicionada por intento esperado y evitar confirmar trabajo supersedido.
4. Diagnosticar con metadata auditada segura; nunca copiar payload, credenciales o material sensible al log/receipt.
5. La recuperación de DeadLetter no equivale a replay automático. Cualquier capacidad futura de replay requiere alcance, autorización y contrato explícitos antes de implementarse.
6. Si una corrección cambia producto o persistencia, reejecutar los gates causales del área afectada y no reutilizar este documento como PASS automático.

## Rollback

Rollback de N7.3 significa aplicar una corrección forward o revertir en `Desarrollo` el changeset funcional/documental causal y revalidar los gates correspondientes. No implica borrar mensajes Outbox, alterar mensajes DeadLetter en Producción, resetear intentos productivos ni manipular proveedores externos desde VAEP.

## Aplicabilidad documental

- ADR nuevo: N/A; N7.3 desarrolla la terminalidad DeadLetter del Outbox ya adoptado y no crea una arquitectura paralela.
- ERD nuevo: N/A; no existe delta de schema propio de N7.3.C.
- OpenAPI adicional: N/A; N7.3.D no agrega endpoint de replay ni otro endpoint nuevo.
- Runbook: la sección Operación y recuperación de este documento es la guía mínima certificada para el alcance actual.

## Estado de certificación

N7.3.A–G tienen receipts `LISTO_REAL`; el candidate funcional más reciente de N7.3 es `2c19221fdbb0a3cdfdcf8afdc839999bf8e80265` y N7.3.G quedó cerrado por receipt en `1f5a62910b3a84b99ee90b45d204ee2fcf240f75`.

La publicación de este documento materializa el artefacto canónico de N7.3.H, pero **no declara por sí sola N7.3.H=LISTO_REAL**. Antes del cierre H deben reconciliarse `TASKS.md` y `CHANGELOG_AI.md` de forma estrictamente aditiva/history-preserving cuando corresponda al cambio de estado, ejecutar REVIEW_FIRST fresco con P0=0/P1=0, verificar equivalencia/gates causales aplicables y persistir/releer el receipt H. `N7.4.A` permanece promotion-held hasta ese cierre real.
