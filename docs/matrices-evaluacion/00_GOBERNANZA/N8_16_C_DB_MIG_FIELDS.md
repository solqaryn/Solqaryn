# N8.16.C — DB_MIG / campos obligatorios

Estado: `LISTO_REAL`

Baseline: `f4b11064ae0235a33eaaff9e4f802f54433887af`.

Toda matriz material que lea/escriba datos debe declarar, con `N/A` justificado cuando no aplique: `DATA_ENTITIES`, `DB_TABLES`, `DB_FIELDS`, `RELATIONSHIPS`, `DB_CONTEXT_REF`, `MIGRATION_REFS`, `FK_CONSTRAINTS`, `UNIQUE_CHECK_CONSTRAINTS`, `INDEX_REFS`, `NULLABILITY_DEFAULTS`, `SEED_FIXTURE_REFS`, `TRANSACTION_BOUNDARY`, `INTEGRITY_RULES`, `TENANT_PARTITION_RULE`, `DATA_CLASSIFICATION` y `RETENTION_DELETION_RULE`.

Las referencias deben apuntar a rutas/símbolos reales o `N/A:<reason>`; cadenas vacías no certifican cobertura. Una migración histórica nunca se reescribe desde la matriz. El contrato diferencia esquema físico, invariantes EF/Domain y reglas Application.

Para operaciones de escritura se exige además `WRITE_IDEMPOTENCY`, `CONCURRENCY_RULE`, `FAILURE_ROLLBACK` y evidencia de tests/gates cuando exista. Para lectura se declara `READ_SCOPE` y filtros tenant/authorization aplicables.

REVIEW_FIRST: P0=0, P1=0. No se ejecutó DDL ni se modificó esquema.

`N8.16.C = LISTO_REAL`. Siguiente: `N8.16.D — BACKEND_API`.
