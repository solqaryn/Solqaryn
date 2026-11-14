# Punto 13 — Contrato JSON en ambos providers

Fecha: 2026-10-04
Repositorio: `solqaryn/Solqaryn`
Rama: `dev`

## Hallazgo

Antes de este punto el gate sólo exigía al menos tres propiedades EF `json` y ejercitaba inserción/extracción sobre una tabla sintética `Payload`. Eso no demostraba la correspondencia de las tres columnas reales ni persistencia por el modelo de SOLQARYN.

## Corrección y prueba

Se agregó una sonda de integración que exige exactamente las asignaciones del modelo:

- `MetodoPago.Metadata` → `MetodosPago.Metadata`, tipo `json`.
- `RegistroAuditoria.ValoresAnteriores` → `RegistrosAuditoria.ValoresAnteriores`, tipo `json`.
- `RegistroAuditoria.ValoresNuevos` → `RegistrosAuditoria.ValoresNuevos`, tipo `json`.

Contra cada provider la sonda inserta las entidades reales con EF, las lee de nuevo con EF, verifica los valores JSON y extrae cada `$.probe` con `JSON_EXTRACT` del MySQL real. Las escrituras viven en una transacción revertida; la salida PASS requiere los tres mapeos, tres round-trips y tres extracciones.

## Evidencia exact-head

- Corrida histórica: Fase 6 run `37300244614`, HEAD `de4713127608412e265bf949fbf475dcb8e14d25`; el dictamen completo terminó `success`, `FASE_6_MYSQL_EF_PROVIDER=PASS`, `P0=0`, `P1=0`, `PHASE7_EXECUTED=false`.
- En el job Pomelo/MySqlConnector: `JSON_PROVIDER_CONTRACT=PASS mappings=3 efRoundTrips=3 sqlExtractions=3 rolledBack=true`; el inventario informó `POMELO_JSON_COLUMNS=3`.
- En la lane Oracle EF10/net10 del mismo HEAD: `JSON_PROVIDER_CONTRACT=PASS mappings=3 efRoundTrips=3 sqlExtractions=3 rolledBack=true`; suite provider 2336/2336 unitarias y 22/22 integraciones.

## Revalidación exact-head actual (2026-10-06)

- HEAD de `dev`: `6a457b102a9261565c96147a436b9618b136f4c1`; [gate Fase 6 #37431527751](https://github.com/solqaryn/Solqaryn/actions/runs/37431527751) terminó `success`, con `FASE_6_MYSQL_EF_PROVIDER=PASS`, `P0=0`, `P1=0` y `PHASE7_EXECUTED=false`.
- Pomelo: `JSON_PROVIDER_CONTRACT=PASS mappings=3 efRoundTrips=3 sqlExtractions=3 rolledBack=true`; inventario `POMELO_JSON_COLUMNS=3`.
- Oracle EF10/net10: la misma salida `JSON_PROVIDER_CONTRACT=PASS mappings=3 efRoundTrips=3 sqlExtractions=3 rolledBack=true`.
- Confirmé en la sonda que son columnas de las entidades reales: `MetodoPago.Metadata`, `RegistroAuditoria.ValoresAnteriores` y `RegistroAuditoria.ValoresNuevos`; para cada una realiza round-trip EF y extracción `JSON_EXTRACT` en MySQL, dentro de una transacción revertida.
- En la lane aislada net10 pasaron 2,388 pruebas unitarias y 28 de integración; en Pomelo EF8, las 28 integraciones; en Oracle EF10, 2,336 unitarias y 22 integraciones.

## Incidencias históricas y estado actual

En el run histórico `37300244614` se registraron 8 fallos unitarios de `ReporteComprasServiceTests` y 3 fallos de integración relacionados con concurrencia/inventario en la lane informativa EF8/Pomelo retargeteada a net10. Esos fallos no deben presentarse como el estado vigente: en el exact-head `37431527751` sobre `6a457b102a9261565c96147a436b9618b136f4c1`, la misma lane ejecutó `Category!=Integration` y `Category=Integration` completas en net10 y pasó 2,388/2,388 unitarias y 28/28 integraciones. La lane Pomelo EF8 actual pasó también 28/28 integraciones; Oracle EF10 pasó 2,336/2,336 unitarias y 22/22 integraciones.

El resultado demuestra que los fallos históricos no se reproducen en ese HEAD; por sí solo no demuestra cuál cambio los corrigió ni cierra la clasificación causal. Los puntos 20/21 deben reconciliar la deuda histórica y su evidencia antes del cierre global. No se afirma que el conjunto de los 30 puntos esté cerrado por este resultado.

No se alteró el modelo productivo, la historia de migraciones ni se ejecutó Fase 7.

**Punto 13: CERRADO.** Las tres columnas JSON reales pasan mapping, persistencia y extracción SQL en MySqlConnector y Oracle; la deuda net10 EF8/Pomelo queda explícitamente abierta a los puntos 20/21.

MAPA_ARQUITECTURA: SIN_CAMBIO.
