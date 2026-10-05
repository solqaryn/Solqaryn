# Punto 18 — Concurrencia de inventario, numeración y escrituras

Fecha: 2026-10-04 (hora local)
Repositorio: `solqaryn/Solqaryn`
Rama: `dev`

## Hallazgo y corrección

La lane Oracle EF10 ejercitó diez reservas simultáneas contra la secuencia documental y reprodujo el agotamiento de `MaxIntentosConcurrencia=8`. El compare-and-swap mantuvo la exclusión mutua, pero la ráfaga excedía el presupuesto de reintentos y una reserva terminaba rechazada.

Se elevó el máximo a 32 intentos y se incorporó una espera aleatoria acotada de 1–100 ms después de que una reserva perdedora revierte su transacción y limpia el tracker. Se conserva el compare-and-swap sobre el valor anterior, el contador monotónico y la auditoría dentro de la misma transacción: un conflicto no consume número ni deja auditoría parcial.

## Certificación de los dos providers

- Oracle EF10/net10, lane aislada `37251987015`, SHA `4f40b8ea6`: `ORACLE10_CONCURRENCY_CONTRACT=PASS stockForUpdateWaitMs=352 sequenceReservations=10 uniqueMonotonic=true audits=10`. El lock de `InventarioConcurrencyService` bloqueó una segunda conexión hasta la liberación; diez solicitudes paralelas produjeron exactamente 101–110, contador final 110 y diez auditorías.
- Pomelo/MySQL 8.4.11, gate `37251986975`, mismo SHA: integración `28/28` pass. Se agregó `SecuenciaDocumentoConcurrencyIntegrationTests` para diez reservas paralelas, unicidad, monotonía, contador final y auditoría.
- Gate Fase 6 exact-head `37251986975`: todos los jobs success, `P0=0`, `P1=0`, `FASE_6_MYSQL_EF_PROVIDER=PASS`, `PHASE7_EXECUTED=false`. Provider-final run `37251986973` success.

## Deuda separada

El ensayo informativo EF8/Pomelo retargeteado a net10 registró 8 fallos unitarios (2380/2388 pasan) y 3 fallos de integración (25/28 pasan). La nueva prueba de secuencias sí pasó en ese ensayo; las fallas restantes continúan abiertas para los puntos 20/21. Fase 7 no se ejecutó.

MAPA_ARQUITECTURA: SIN_CAMBIO.
