# N8.17.G — TEST_CI traceability validation

Estado de trabajo: `REVIEW_FIRST / TEST_CI`.

Universo congelado: `49` contract roots de `N8_17_A_BATCH_MANIFEST.json`.

## Criterio de trazabilidad

N8.17.G no infla el ciclo de vida. Su responsabilidad es demostrar que el universo congelado puede recorrerse de forma determinística como:

`MATRIX_ID -> contrato de dominio -> contrato de datos -> backend authority -> UI/implementation root -> security disposition -> test/gate -> receipt/evidencia`.

La publicación de `SPEC_COMPLETE`/`CERTIFIED` pertenece a N8.17.H. Un gate de compilación o un contrato compartido no se presenta como prueba ficticia de comportamiento feature-by-feature.

## Validador ejecutable

`scripts/validate_n8_17_g_traceability.py` exige, en CI:

- manifest exacto de `49` MATRIX_ID únicos;
- catálogo exacto 49/49, sin root faltante/extra/duplicado;
- `IMPLEMENTATION_REF` no vacío y físicamente resoluble para 49/49 roots;
- contratos N8.17.B exactos 49/49 con objetivo, actores, precondiciones, invariantes, happy/alternate flows, side effects e idempotencia;
- contratos N8.17.C exactos 49/49 y `unknowns=[]`;
- mapping N8.17.D exacto 49/49, `unresolved=[]` y assertions verdaderas;
- contrato UI N8.17.E con `expected_total=49`, `unresolved=[]` y assertions verdaderas, heredado por los roots físicos del catálogo;
- auditoría N8.17.F con una disposición explícita para cada MATRIX_ID y REVIEW_FIRST `P0=0/P1=0/P2=0`;
- receipts durables `LISTO_REAL` de N8.17.B/C/D/E/F;
- cero promoción de lifecycle dentro de G; H conserva el control de publicación.

El workflow `.github/workflows/matrix-governance.yml` ejecuta este validador junto con los validadores canónicos de gobierno, datos, backend y frontend.

## Código / tests / evidencia

### Código

Los 49 roots tienen `IMPLEMENTATION_REF` canónico en `CATALOGO_MATRICES.md`. La existencia física de cada referencia se comprueba en CI. APP_SHELL usa su composición/routing root; los otros 48 roots usan el feature root catalogado.

### Tests y gates

- El gate `Desarrollo - Gobierno de matrices / MATRIX_ID, catálogo y plantilla` ejecuta el validador exhaustivo G sobre el HEAD candidato.
- El mismo gate reejecuta los validadores N8.17.C/D/E, evitando que G certifique sobre contratos estructuralmente rotos.
- La evidencia funcional base no se reinventa: `N8_15_G_TEST_CI_VALIDATION.md` conserva el último baseline integral exitoso (frontend lint/build, backend build/tests, Docker y MySQL/migrations) y la evidencia causal de deltas posteriores.
- N8.17.B-F son reconciliación/especificación; sus receipts y gates no se convierten en una afirmación de que cada feature tiene un E2E dedicado. Esa diferencia es deliberada y evita inflar `CERTIFIED`.

### Evidencia durable

La cadena B-F está respaldada por receipts `LISTO_REAL`. G agrega su propio REVIEW_FIRST y receipt sólo después de que el gate de trazabilidad exact-head termine en success.

## Detección de omisiones y TBD

El validador falla por:

- MATRIX_ID faltante, extra o duplicado;
- implementation root inexistente;
- contrato B/C incompleto o missing;
- `unknowns`/`unresolved` no vacíos en las capas que los gobiernan;
- assertions D/E no satisfechas;
- ausencia de fila de seguridad F;
- ausencia de receipt B-F;
- intento de perder la declaración explícita de lifecycle baseline antes de H.

No se trata la palabra documental `UNKNOWN` como error cuando describe una regla fail-closed; se bloquean únicamente estados estructurados no resueltos. No existe `TBD` silencioso aceptado por este gate.

## REVIEW_FIRST candidato

- P0: `0`.
- P1: `0`.
- P2: `0`.
- Missing matrices: `0`.
- Duplicate MATRIX_ID: `0`.
- Unresolved structured gaps: `0`.
- Lifecycle inflation: `0`.
- Runtime/schema/deploy/Production delta: `0`.

## Resultado candidato

`N8.17.G` puede certificarse `LISTO_REAL` únicamente cuando el workflow de gobierno que incluye `validate_n8_17_g_traceability.py` cierre `success` sobre el HEAD candidato y el receipt/readback se persista.
