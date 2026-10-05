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

- Corrida exact-head vigente: Fase 6 run `37300244614`, HEAD `de4713127608412e265bf949fbf475dcb8e14d25`; el dictamen completo terminó `success`, `FASE_6_MYSQL_EF_PROVIDER=PASS`, `P0=0`, `P1=0`, `PHASE7_EXECUTED=false`.
- En el job Pomelo/MySqlConnector: `JSON_PROVIDER_CONTRACT=PASS mappings=3 efRoundTrips=3 sqlExtractions=3 rolledBack=true`; el inventario informó `POMELO_JSON_COLUMNS=3`.
- En la lane Oracle EF10/net10 del mismo HEAD: `JSON_PROVIDER_CONTRACT=PASS mappings=3 efRoundTrips=3 sqlExtractions=3 rolledBack=true`; suite provider 2336/2336 unitarias y 22/22 integraciones.

## Deuda no atribuible a este punto

En la lane informativa EF8/Pomelo retargeteada temporalmente a net10 de la misma corrida exact-head, permanecieron 8 fallos de 2,388 unitarias (todos en `ReporteComprasServiceTests`, con `System.ReadOnlySpan<int>`/`FuncCallInstruction` al evaluar parámetros LINQ) y 3 fallos de 28 integraciones (`InventarioDocumentConcurrencyTests`, `ConsumoInsumoIntegrationTests` y `MovimientoInventarioOrigenTipadoIntegrationTests`). Esa lane es no seleccionada para el provider certificado y no invalida este contrato JSON, pero la deuda no está en cero: queda abierta para clasificación y resolución en los puntos 20/21 antes del cierre global previo a Fase 7. No se afirma que todas las suites estén verdes por el PASS del gate agregador.

No se alteró el modelo productivo, la historia de migraciones ni se ejecutó Fase 7.

**Punto 13: CERRADO.** Las tres columnas JSON reales pasan mapping, persistencia y extracción SQL en MySqlConnector y Oracle; la deuda net10 EF8/Pomelo queda explícitamente abierta a los puntos 20/21.

MAPA_ARQUITECTURA: SIN_CAMBIO.
