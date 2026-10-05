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

- Pomelo/MySQL 8.4.11, gate run `37249770325`, SHA `b5ae33500`: `POMELO_TRANSACTION_CONTRACT=PASS isolation=READ-COMMITTED commit=true rollback=true errorRollback=true`; integración MySQL 27/27.
- Oracle EF10/net10, provider lane `37249770331`, mismo SHA: `ORACLE10_TRANSACTION_CONTRACT=PASS isolation=READ-COMMITTED commit=true rollback=true errorRollback=true`.
- Gate Fase 6 `37249770325`: todos los jobs success, `P0=0`, `P1=0`, `FASE_6_MYSQL_EF_PROVIDER=PASS`, `PHASE7_EXECUTED=false`. Provider-final run `37249770301` también success.

El lane informativo EF8/Pomelo retargeteado a net10 conserva 8 fallos unitarios y 3 de integración en esta ejecución; queda pendiente para puntos 20/21. Fase 7 no se ejecutó.

MAPA_ARQUITECTURA: SIN_CAMBIO.
