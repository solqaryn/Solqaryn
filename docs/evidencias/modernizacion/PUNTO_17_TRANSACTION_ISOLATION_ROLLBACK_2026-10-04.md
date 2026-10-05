# Punto 17 — Transacciones, aislamiento y rollback ante error

Fecha: 2026-10-04 (hora local)
Repositorio: `solqaryn/Solqaryn`
Rama: `dev`

## Cobertura

Los dos carriles de provider prueban en bases MySQL efímeras:

- Nivel de aislamiento `READ-COMMITTED`.
- `COMMIT`: la fila insertada permanece visible después de finalizar la transacción.
- `ROLLBACK` explícito: la fila de la transacción no queda persistida.
- Error dentro de una transacción después de una escritura: se provoca duplicate-key `1062` y se demuestra que el trabajo parcial previo también se revierte.
- En Oracle EF10, el error se ejecuta a través de `UnitOfWork`; se verifica que la fila pre-error no persiste y que la fila duplicada original sigue única. En Pomelo se verifica el rollback a nivel de transacción MySQL.

No se cambió la implementación de negocio, el default global de aislamiento ni ningún dato persistente. La primera sonda mostró que `@@transaction_isolation` dentro del provider Oracle conserva el default de sesión cuando el aislamiento sólo se solicita para una transacción concreta. La verificación corregida establece `READ-COMMITTED` en la sesión abierta y compara la configuración efectiva observada; no interpreta erróneamente el default como el nivel transaccional solicitado.

## Evidencia exact-head

- Gate Fase 6 exact-head run `37306094850`, SHA `e2688576076860f0fb3d254a31061d6c9b142a3c`: 5/5 jobs success; `P0=0`, `P1=0`, `FASE_6_MYSQL_EF_PROVIDER=PASS`, `PHASE7_EXECUTED=false`.
- Pomelo/MySQL 8.4.11: `POMELO_TRANSACTION_CONTRACT=PASS isolation=READ-COMMITTED commit=true rollback=true errorRollback=true`; integración MySQL 28/28 pass.
- Oracle EF10/net10: `ORACLE10_TRANSACTION_CONTRACT=PASS isolation=READ-COMMITTED commit=true rollback=true errorRollback=true`.
- [Fase 6 exact-head](https://github.com/solqaryn/Solqaryn/actions/runs/37306094850); aceptación DEV Playwright también success en el mismo HEAD: [run](https://github.com/solqaryn/Solqaryn/actions/runs/37306094896).

El lane informativo EF8/Pomelo retargeteado a net10 en ese HEAD reporta 8 fallos unitarios (2380/2388 pasan) y 3 de integración (25/28 pasan); queda pendiente para puntos 20/21. Fase 7 no se ejecutó.

**Punto 17: CERRADO.** Aislamiento solicitado, commit, rollback explícito y rollback tras error/1062 se verifican en ambos carriles sin modificar defaults productivos.

MAPA_ARQUITECTURA: SIN_CAMBIO.
