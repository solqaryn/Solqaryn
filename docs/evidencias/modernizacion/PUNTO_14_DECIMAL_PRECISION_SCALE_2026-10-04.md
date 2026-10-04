# Punto 14 — Precisión y escala decimal

Fecha: 2026-10-04
Repositorio: `solqaryn/Solqaryn`
Rama: `dev`

## Hallazgo y cobertura agregada

El gate previo sólo contaba columnas `decimal(18,2)`/`decimal(18,4)` y el test runtime insertaba un único decimal `18,4` en una tabla sintética. No probaba un round-trip `18,2` ni nombraba propiedades representativas reales.

La sonda compartida Pomelo/Oracle EF10 ahora:

- Exige en el modelo `Compra.Total` → `decimal(18,2)` y `CuentaPorPagar.MontoOriginal` → `decimal(18,4)`.
- Consulta el tipo físico real en `INFORMATION_SCHEMA.COLUMNS` para ambas columnas y exige coincidencia exacta de tipo y escala.
- Guarda y vuelve a leer con EF una `Compra` cuyo `Subtotal`/`Total` son `9999999999999999.99`.
- Envía al provider por parámetros los máximos `9999999999999999.99` (`18,2`) y `99999999999999.9999` (`18,4`), y compara su representación exacta almacenada en MySQL.
- Realiza la prueba de escritura temporal dentro de una transacción revertida y limpia la tabla temporal.

## Evidencia exact-head

- Pomelo y gate Fase 6: run `37244974128` sobre `ef3ea812482e4ba7e258ea5dc1cf5d9d56c97db1`, completed/success. `DECIMAL_PROVIDER_CONTRACT=PASS model18_2=Compras.Total model18_4=CuentasPorPagar.MontoOriginal maxPrecisionScaleRoundTrips=2 rolledBack=true`; inventario físico: `POMELO_DECIMAL_18_2=54`, `POMELO_DECIMAL_18_4=64`; tests integración MySQL 27/27 pass.
- Oracle EF10/net10 aislado: run `37244974112` success, mismo `DECIMAL_PROVIDER_CONTRACT=PASS`; provider-final del gate: run `37244974157` success.
- Dictamen del gate: 5/5 jobs success, `P0=0`, `P1=0`, `FASE_6_MYSQL_EF_PROVIDER=PASS`, `PHASE7_EXECUTED=false`.

La brecha conocida de tests EF8/Pomelo retargeteado a net10 es independiente del decimal y permanece abierta para los puntos 20/21; véase la evidencia del punto 13. No se cambió escala del modelo productivo ni se ejecutó Fase 7.

MAPA_ARQUITECTURA: SIN_CAMBIO.
