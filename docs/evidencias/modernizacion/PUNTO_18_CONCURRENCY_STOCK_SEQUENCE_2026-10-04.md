# Punto 18 — Concurrencia de inventario, numeración y escrituras

Fecha: 2026-10-04 (hora local)
Repositorio: `solqaryn/Solqaryn`
Rama: `dev`

## Hallazgo y corrección

La lane Oracle EF10 ejercitó diez reservas simultáneas contra la secuencia documental y reprodujo el agotamiento de `MaxIntentosConcurrencia=8`. El compare-and-swap mantuvo la exclusión mutua, pero la ráfaga excedía el presupuesto de reintentos y una reserva terminaba rechazada.

Se elevó el máximo a 32 intentos y se incorporó una espera aleatoria acotada de 1–100 ms después de que una reserva perdedora revierte su transacción y limpia el tracker. Se conserva el compare-and-swap sobre el valor anterior, el contador monotónico y la auditoría dentro de la misma transacción: un conflicto no consume número ni deja auditoría parcial.

## Certificación de los dos providers

- Gate Fase 6 exact-head run `37307576959`, SHA `9abf04bc5564d91631c4fb5d6e6b7d4e1bb83c6a`: 5/5 jobs success, `FASE_6_MYSQL_EF_PROVIDER=PASS`, `P0=0`, `P1=0`, `PHASE7_EXECUTED=false`.
- Oracle EF10/net10: `ORACLE10_CONCURRENCY_CONTRACT=PASS stockForUpdateWaitMs=352 sequenceReservations=10 uniqueMonotonic=true audits=10`. El lock de `InventarioConcurrencyService` bloqueó una segunda conexión hasta liberarse; diez reservas concurrentes produjeron 101–110, contador final 110 y diez auditorías. La suite incluyó las pruebas de concurrencia de documentos/stock y `SecuenciaDocumentoConcurrencyIntegrationTests`.
- Pomelo/MySQL 8.4.11: la prueba `ReservasConcurrentes_ProducenNumeracionUnicaMonotonicaYAuditoriaAtomica` y el conjunto completo de integración terminaron 28/28 pass.
- [Fase 6 exact-head](https://github.com/solqaryn/Solqaryn/actions/runs/37307576959); aceptación DEV también success en el mismo HEAD: [run](https://github.com/solqaryn/Solqaryn/actions/runs/37307576988).

## Deuda separada

El ensayo informativo EF8/Pomelo retargeteado a net10 registró 8 fallos unitarios (2380/2388 pasan) y 3 fallos de integración (25/28 pasan); incluye `InventarioDocumentConcurrencyTests.CompraConMovimientoPosterior_NoPuedeAnularse`. La nueva prueba concurrente de secuencias sí pasó en los dos providers certificados; las fallas del lane exploratorio permanecen abiertas para diagnóstico en los puntos 20/21. Fase 7 no se ejecutó.

**Punto 18: CERRADO para los providers certificados.** Se verificaron bloqueo de stock, reservas únicas/monotónicas y auditoría atómica bajo carga concurrente; la deuda observada al retargetear EF8/Pomelo a net10 queda separada y pendiente.

MAPA_ARQUITECTURA: SIN_CAMBIO.
