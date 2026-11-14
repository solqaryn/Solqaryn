# Punto 30 — Dictamen fail-closed de Fase 6

Fecha: 2026-10-06 (UTC)  
Repositorio: `solqaryn/Solqaryn`  
Rama de cierre: `dev`  
PR de implementación: [#3524](https://github.com/solqaryn/Solqaryn/pull/3524)  
Commit integrado: `3a9ac63d42ebea9887ebe4e949dc81b0e7295113`

## Hallazgo corregido

La comparación directa del provider Oracle contra el historial Pomelo no certifica drop-in:

- `ORACLE_DROP_IN=false`
- `ORACLE_FRESH_MIGRATIONS=false`
- `ORACLE_SCHEMA_EQUIVALENT=false`

Esos resultados no se ocultan ni se reinterpretan como compatibilidad directa. La ruta conserva el historial productivo Pomelo y utiliza un baseline Oracle explícito. El defecto del gate era que no exigía probar esa estrategia en el mismo HEAD antes de elegir Oracle.

## Corrección implementada y validada

El PR #3524 hizo obligatorio el workflow de baseline/adopción como dependencia del dictamen de Fase 6, tanto en la validación del PR hacia `dev` como en la ejecución de DEV. Un resultado distinto de `success` ya no puede habilitar la selección del provider objetivo. La ruta se etiqueta explícitamente `ORACLE_EF10_WITH_CERTIFIED_BASELINE_ADOPTION`; no se declara drop-in para las 107 migraciones históricas.

La revisión del PR terminó verde sobre su HEAD final `0bdca5905a0812f06616cbac0827e1bc86390fe4`: [run Fase 6 #37362932395](https://github.com/solqaryn/Solqaryn/actions/runs/37362932395), intento 2. PR #3524 se integró por squash exclusivamente en `dev` como `3a9ac63d42ebea9887ebe4e949dc81b0e7295113`.

## Certificación exact-head de DEV

En el commit integrado `3a9ac63d42ebea9887ebe4e949dc81b0e7295113`:

- Gate Fase 6 [run 37407874643](https://github.com/solqaryn/Solqaryn/actions/runs/37407874643): todos los seis jobs terminaron `success`, incluido `Dictamen Fase 6`.
- Dictamen: `FASE_6_MYSQL_EF_PROVIDER=PASS`, `TARGET_PROVIDER_ROUTE=ORACLE_EF10_WITH_CERTIFIED_BASELINE_ADOPTION`, `POINT_2_POMELO10_DEPENDENCY=PASS`, `P0=0`, `P1=0`, `PHASE7_EXECUTED=false`.
- Probe independiente de baseline/adopción [run 37407874427](https://github.com/solqaryn/Solqaryn/actions/runs/37407874427): `CANONICAL_PHYSICAL_BASELINE_RESTORE=PASS`, `ORACLE_BASELINE_SCHEMA_EQUIVALENT=true`, `ORACLE_BASELINE_TABLE_COUNT=136`, `ORACLE_BASELINE_ADOPTION_NO_DDL=PASS`.
- La ruta de rollback conservó la historia Pomelo (107 migraciones) y los datos. Los jobs de MySQL/Pomelo, Connector/NET, Oracle EF10/net10 y el probe net10 aislado terminaron `success`.

## Límites y resultado

**Punto 30: CERRADO en DEV** sobre el HEAD indicado. El runtime y los proyectos productivos no fueron retargeteados; no se ejecutó Fase 7. No se modificaron `main`, QA, PROD, Aiven ni datos persistentes.

Este cierre certifica el gate y la ruta de provider de Fase 6; no sustituye la certificación exact-head separada de los puntos restantes ni autoriza promoción de entorno.

MAPA_ARQUITECTURA: SIN_CAMBIO.
