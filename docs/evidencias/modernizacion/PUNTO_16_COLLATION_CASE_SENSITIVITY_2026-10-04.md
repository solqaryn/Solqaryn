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

- Gate Fase 6 exact-head run `37304445221`, SHA `acb7ee14e7e42bf6d89a78f162df18279f2b2cd5`: 5/5 jobs success, `P0=0`, `P1=0`, `FASE_6_MYSQL_EF_PROVIDER=PASS`, `PHASE7_EXECUTED=false`.
- Pomelo/MySQL 8.4.11: `POMELO_COLLATION_CASE_CONTRACT=PASS column=utf8mb4_bin table=utf8mb4_0900_ai_ci distinctCaseVariants=2`; 28/28 integraciones MySQL pass.
- Oracle EF10/net10: `ORACLE10_COLLATION_CASE_CONTRACT=PASS column=utf8mb4_bin table=utf8mb4_0900_ai_ci distinctCaseVariants=2`.
- [Fase 6 exact-head](https://github.com/solqaryn/Solqaryn/actions/runs/37304445221).

El lane informativo EF8/Pomelo retargeteado a net10 en el mismo HEAD reporta 8 fallos unitarios (2380/2388 pasan) y 3 fallos de integración (25/28 pasan). Esto requiere diagnóstico en los puntos 20/21 y no debe presentarse como deuda resuelta ni estable. Fase 7 no se ejecutó.

**Punto 16: CERRADO para el contrato comparativo de providers.** Se probó que el override binario por columna prevalece frente al default insensible de la tabla y que variantes sólo por capitalización coexisten/buscan correctamente. Esto no implica que SOLQARYN haya adoptado una collation binaria productiva: la inspección de esquema no encontró ese requisito y no se modificaron datos ni migraciones.

MAPA_ARQUITECTURA: SIN_CAMBIO.
