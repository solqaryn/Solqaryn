# Punto 18 — Concurrencia de inventario, numeración y escrituras

Fecha de validación original: 2026-10-04 (hora local)  
Revalidación: 2026-10-06  
Repositorio: `solqaryn/Solqaryn`  
Rama de destino: `dev`

## Hallazgo y corrección

La lane Oracle EF10 ejercitó diez reservas simultáneas contra la secuencia documental y reprodujo el agotamiento de `MaxIntentosConcurrencia=8`. El compare-and-swap mantuvo la exclusión mutua, pero la ráfaga excedía el presupuesto de reintentos y una reserva terminaba rechazada.

Se elevó el máximo a 32 intentos y se incorporó una espera aleatoria acotada de 1–100 ms después de que una reserva perdedora revierte su transacción y limpia el tracker. Se conserva el compare-and-swap sobre el valor anterior, el contador monotónico y la auditoría dentro de la misma transacción: un conflicto no consume número ni deja auditoría parcial.

## Certificación de los dos providers

- Gate Fase 6 exact-head original: run `37307576959`, SHA `9abf04bc5564d91631c4fb5d6e6b7d4e1bb83c6a`, 5/5 jobs success, `FASE_6_MYSQL_EF_PROVIDER=PASS`, `P0=0`, `P1=0`, `PHASE7_EXECUTED=false`.
- Revalidación Fase 6 exact-head: run [`37442912542`](https://github.com/solqaryn/Solqaryn/actions/runs/37442912542), SHA `227e5d2950bc120fe78a5568b52064eb9ecc5b60`, conclusión `success`. Este es el HEAD de `dev` sobre el que se basó esta revalidación.
- Oracle EF10/net10: `ORACLE10_CONCURRENCY_CONTRACT=PASS stockForUpdateWaitMs=352 sequenceReservations=10 uniqueMonotonic=true audits=10`. El lock de `InventarioConcurrencyService` bloqueó una segunda conexión hasta liberarse; diez reservas concurrentes produjeron 101–110, contador final 110 y diez auditorías. La suite incluyó las pruebas de concurrencia de documentos/stock y `SecuenciaDocumentoConcurrencyIntegrationTests`.
- Pomelo/MySQL 8.4.11: `ReservasConcurrentes_ProducenNumeracionUnicaMonotonicaYAuditoriaAtomica` y el conjunto completo de integración terminaron 28/28 pass.
- En el HEAD revalidado, suite net10: 2388/2388 unitarias y 28/28 integración; Oracle EF10: 2336/2336 unitarias y 22/22 integración. El gate completo de Fase 6 quedó verde.

## Deuda separada: retarget exploratorio EF8/Pomelo

El ensayo informativo retargeteado a net10 registró históricamente 8 fallos unitarios (2380/2388 pasan) y 3 fallos de integración (25/28 pasan), incluyendo `InventarioDocumentConcurrencyTests.CompraConMovimientoPosterior_NoPuedeAnularse`. En la revalidación exact-head del 2026-10-06, la suite net10 terminó 2388/2388 y 28/28; por tanto, esas fallas no se reprodujeron en este HEAD. La causa histórica no queda establecida sólo por el resultado verde y se conserva para el diagnóstico de los puntos 20/21; no se declara resuelta aquí.

## Dictamen

**Punto 18: CERRADO para los providers certificados** (Pomelo/MySQL 8.4.11 y Oracle EF10), con evidencia original y revalidación exact-head verde. La deuda histórica del ensayo retargeteado se registra por separado para diagnóstico posterior en puntos 20/21.

Fase 7 no se ejecutó ni se autoriza con este dictamen. `MAPA_ARQUITECTURA: SIN_CAMBIO`.
