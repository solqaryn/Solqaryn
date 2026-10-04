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

- Run `37243621617` sobre `cd90b923a5e42ea22f1ad9d249dc5329409bb1b3`: job Pomelo success; `JSON_PROVIDER_CONTRACT=PASS mappings=3 efRoundTrips=3 sqlExtractions=3 rolledBack=true`; inventario físico `POMELO_JSON_COLUMNS=3`; gate general 5/5 jobs success.
- Oracle EF10 aislado: run `37243621620` success con `JSON_PROVIDER_CONTRACT=PASS mappings=3 efRoundTrips=3 sqlExtractions=3 rolledBack=true` y `ORACLE_EF10_NET10_PROVIDER_LANE=PASS`. Provider-final dentro del gate principal: run `37243621639` success.

## Deuda no atribuible a este punto

En la lane informativa EF8/Pomelo retargeteada temporalmente a net10 del run `37243621617`, el test unitario reportó 8 fallos de 2,388 y el test de integración 3 fallos de 27. Esta lane no invalida el contrato JSON certificado con Oracle EF10; sí requiere clasificación y resolución en los puntos 20/21 antes del cierre global de Fase 6. No se declara que todas las suites estén verdes por el mero PASS del gate agregador.

No se alteró el modelo productivo, la historia de migraciones ni se ejecutó Fase 7.

MAPA_ARQUITECTURA: SIN_CAMBIO.
