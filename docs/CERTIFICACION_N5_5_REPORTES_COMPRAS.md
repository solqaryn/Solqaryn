# N5.5 — Certificación de Reportes de Compras

Autoridad: `docs/VAEP_AUTHORITY.md`  
Rama: `Desarrollo`  
Functional head certificado: `8b06b3d6181bad54a867eddb849c08b6f41eab63`

## Alcance certificado

N5.5 cubre el flujo de reportes de compras y su cierre por las microtareas A–H del Plan Maestro. La certificación conserva las semánticas existentes de compras y no inventa reglas de negocio nuevas.

La fase de seguridad/auditoría N5.5.F quedó cerrada con autorización y permiso `Compras/Ver` preservados, validación fail-closed de filtros antes del audit de éxito, correlación acotada y metadata de auditoría sin payload sensible de compra. La evidencia terminal tardía que no conservó el scope autoritativo fue tratada como evidencia únicamente y no se integró automáticamente.

N5.5.G se cerró por fastpath de QA/CI porque el mismo functional head ya contaba con evidencia causal aplicable en verde: VAEP Jules Diagnostic, catalog throughput guard, engine lightweight checks y regresión N4.4; M10 quedó aceptado mediante `CONTROL_PLANE_HEAD_EQUIVALENCE` sobre un descendiente exclusivamente de control-plane.

## Evidencia canónica

- `vaep/evidence/fragments/N5.5.A_LISTO_REAL_CANONICAL_20260910T0806Z.json`
- `vaep/evidence/fragments/N5.5.B_LISTO_REAL_20260910T0807Z.json`
- `vaep/evidence/fragments/N5.5.C_LISTO_REAL_20260910T0810Z.json`
- `vaep/evidence/fragments/N5.5.D_LISTO_REAL_20260910T0845Z.json`
- `vaep/evidence/fragments/N5.5.E_LISTO_REAL_20260910T0956Z.json`
- `vaep/evidence/fragments/N5.5.F_LISTO_REAL_20260910T1028Z.json`
- `vaep/evidence/fragments/N5.5.G_LISTO_REAL_20260910T1029Z.json`

## Gates y guardrails

La certificación no toca `main`, Producción, secretos ni realiza merge de PR #2. No usa R3 ni filler. Los gates no aplicables por ausencia de cambio de schema, persistencia, nuevo flujo frontend o nuevo camino crítico de carga permanecen explícitamente N/A, no se falsifican como ejecutados.

## Rollback

Ante una regresión causal de N5.5, revertir únicamente el delta funcional de N5.5 sobre `Desarrollo`, conservar los receipts como evidencia histórica y reabrir la microtarea causal afectada. No revertir commits ajenos ni control-plane no relacionado, y no tocar Producción desde este procedimiento.

## Cierre

N5.5.H certifica documentación/evidencia de A–G y habilita el sucesor dependency-valid del Plan Maestro. El cierre sólo es válido con `P0=0`, `P1=0`, receipts A–G presentes y control-plane/Sheet reconciliados con el parent promovido.
