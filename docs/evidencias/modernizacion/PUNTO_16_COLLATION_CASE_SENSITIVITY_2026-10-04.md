# Punto 16 — Collations y sensibilidad a mayúsculas/minúsculas

Fecha: 2026-10-04 (hora local)
Repositorio: `solqaryn/Solqaryn`
Rama: `dev`

## Hallazgo y cobertura agregada

La verificación anterior sólo insertaba `ExactCase` y comprobaba que `exactcase` no coincidiera. No probaba que el índice único aceptara dos valores que difieren únicamente por capitalización ni ejecutaba el mismo contrato en Oracle EF10.

Ambos carriles ahora:

- Consultan `INFORMATION_SCHEMA` y exigen `utf8mb4_bin` en la columna de código y `utf8mb4_0900_ai_ci` en la tabla contenedora. Esto comprueba explícitamente que el override sensible de columna prevalece sobre el default insensible de tabla.
- Insertan `CaseProbe` y `caseprobe` bajo el mismo índice único; ambos deben coexistir.
- Buscan ambos valores por igualdad exacta y exigen un único resultado para cada forma.
- Conservan intacto el default insensible de la tabla y no cambian migraciones, modelos, collations ni datos persistentes de SOLQARYN.

La inspección de las migraciones/configuración de infraestructura no encontró una collation binaria explícita por columna en el esquema productivo actual. La comparación de collations físicas entre bootstrap Pomelo y Oracle se conserva en el comparator ampliado del punto 12; este punto valida el contrato case-sensitive explícito del gate de provider en una tabla efímera.

## Evidencia exact-head

- Pomelo/MySQL 8.4, run `37247332277`, SHA `fc44057c93cd3aaf1667aad2d84ca9eb073d3130`: `POMELO_COLLATION_CASE_CONTRACT=PASS column=utf8mb4_bin table=utf8mb4_0900_ai_ci distinctCaseVariants=2`; integración MySQL 27/27.
- Oracle EF10/net10, run `37247332312`, mismo SHA: `ORACLE10_COLLATION_CASE_CONTRACT=PASS column=utf8mb4_bin table=utf8mb4_0900_ai_ci distinctCaseVariants=2`.
- Gate Fase 6 exact-head run `37247332277`: todos los jobs success, `P0=0`, `P1=0`, `FASE_6_MYSQL_EF_PROVIDER=PASS`, `PHASE7_EXECUTED=false`.

El lane informativo EF8/Pomelo retargeteado a net10 conserva 8 fallos unitarios y 3 de integración, pertenecientes a los puntos 20/21. Fase 7 no se ejecutó.

MAPA_ARQUITECTURA: SIN_CAMBIO.
