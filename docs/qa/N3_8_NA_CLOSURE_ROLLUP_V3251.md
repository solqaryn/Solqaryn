# N3.8 — Nota de débito de cliente — Cierre N/A derivado del preflight

## Autoridad

N3.8.A certificó que el alcance del punto es condicional —añadir NotaDebitoCliente cuando legislación/operación lo requiera— y que no existe actualmente requisito legal/operativo autoritativo suficiente para fijar el contrato. N3.8.B documentó por ello dominio/contratos como N/A en vez de inventar semántica.

Este rollup aplica exclusivamente a los padres derivados C–G. No elimina N3.8 del roadmap: si aparece un requisito autoritativo, el bloque deberá reabrirse desde dominio con contratos explícitos.

## N3.8.C — Persistencia, migración y datos

**Disposición: N/A / LISTO_REAL.**

Sin aggregate/contrato N3.8.B aplicable no existe esquema autoritativo que persistir. Crear tabla, FK, cardinalidad, índices, precisión o migración sería especulativo. No se modifican AppDbContext, configuraciones EF, snapshot ni MySQL. Rollback/data migration: N/A porque no existe delta de datos.

## N3.8.D — Aplicación, servicios y API

**Disposición: N/A / LISTO_REAL.**

Sin dominio/persistencia aplicables no existe caso de uso autoritativo. No se crean repository/service/DTO/controller/endpoints, ProblemDetails específicos ni idempotencia. Cualquier API futura depende del contrato reabierto y certificado de B/C.

## N3.8.E — Frontend y UX

**Disposición: N/A / LISTO_REAL.**

Sin API autoritativa no existe flujo UI que implementar. No se crean rutas, formularios, tablas, permisos UI ni E2E ficticios. La ausencia de UI es deliberada y consistente con el requisito condicional no activado.

## N3.8.F — RBAC, auditoría, seguridad y observabilidad

**Disposición: N/A / LISTO_REAL.**

No existe nueva superficie de dominio/API/UI que requiera permiso, auditoría o threat model específico. Se conservan los controles globales existentes; no se fabrican permisos o eventos de auditoría para una operación inexistente.

## N3.8.G — QA, regresión y CI

**Disposición: N/A / LISTO_REAL.**

El delta de producto N3.8 es cero. Por CI_CAUSALITY_BUDGET no se fabrican suites ni se relanza CI pesada para validar código que no existe. La evidencia a verificar es documental: A/B y este rollup prueban que no hubo cambios funcionales ni datos que puedan introducir regresión atribuible a N3.8.

## P0/P1 y seguridad

P0/P1 atribuibles a N3.8.C-G bajo el requisito actual: **0 conocidos**. No se tocó Producción, main, ramas, merge, secretos ni deploy.

## Siguiente paso

Con A–G cerrados mediante preflight/N/A evidence, N3.8.H queda como único parent del bloque: debe registrar la decisión condicional/N/A en documentación canónica y cerrar el bloque sin afirmar que NotaDebitoCliente fue implementada.
