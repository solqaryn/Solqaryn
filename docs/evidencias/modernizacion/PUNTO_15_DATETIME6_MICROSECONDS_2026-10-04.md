# Punto 15 — Precisión temporal `datetime(6)` y microsegundos

Fecha: 2026-10-04 (hora local)
Repositorio: `solqaryn/Solqaryn`
Rama: `dev`

## Hallazgo y cobertura agregada

La cobertura previa no demostraba el round-trip de microsegundos sobre una entidad de SOLQARYN con ambos providers candidatos. La sonda compartida (`backend/scripts/Solqaryn.JsonProbe.csproj`) ahora valida:

- El modelo EF asigna exactamente `Compras.Fecha` a `datetime(6)`.
- `INFORMATION_SCHEMA.COLUMNS` confirma el tipo físico `datetime(6)` para `Compras.Fecha`.
- EF escribe y vuelve a leer la compra con `2026-10-04 12:34:56.123456`, comparando los ticks para detectar pérdida de precisión.
- `DATE_FORMAT` en MySQL confirma exactamente `2026-10-04 12:34:56.123456`.
- La escritura de prueba ocurre dentro de una transacción revertida; no persiste datos de prueba.

## Evidencia exact-head

- Fase 6 exact-head histórica: run `37302818734`, SHA `a13fa5190c7c636432751334ca38fa608fdc6536`; 5/5 jobs success. Dictamen `FASE_6_MYSQL_EF_PROVIDER=PASS`, `P0=0`, `P1=0`, `PHASE7_EXECUTED=false`.
- Pomelo/MySQL 8.4.11: `DATETIME6_PROVIDER_CONTRACT=PASS model=Compras.Fecha physical=datetime(6) efRoundTrip=true sqlMicroseconds=123456 rolledBack=true`; inventario físico `POMELO_DATETIME6=373`; integración 28/28 pass.
- Oracle EF10/net10: mismo `DATETIME6_PROVIDER_CONTRACT=PASS`; 2336/2336 unitarias y 22/22 integraciones pass.
- [Fase 6 exact-head](https://github.com/solqaryn/Solqaryn/actions/runs/37302818734).

## Deuda independiente conservada

## Revalidación exact-head actual (2026-10-06)

- HEAD de `dev`: `1876612747fc7ad2beb8c6e910997eb843e5796b`; [gate Fase 6 #37435737523](https://github.com/solqaryn/Solqaryn/actions/runs/37435737523) terminó `success`.
- Pomelo/MySQL 8.4.11: `DATETIME6_PROVIDER_CONTRACT=PASS model=Compras.Fecha physical=datetime(6) efRoundTrip=true sqlMicroseconds=123456 rolledBack=true`; inventario: 373 columnas físicas `datetime(6)`.
- Oracle EF10/net10 registró el mismo contrato PASS de `Compras.Fecha`.
- En la sonda, EF compara los ticks tras round-trip de `2026-10-04 12:34:56.123456`; `DATE_FORMAT` confirma esos mismos microsegundos en MySQL y la transacción se revierte.
- En el mismo HEAD, la lane informativa net10 pasó 2,388/2,388 unitarias y 28/28 integraciones; los fallos históricos citados abajo no se reproducen en esta corrida.

Los fallos históricos de la lane EF8/Pomelo retargeteada a net10 reportados por el run citado no se reprodujeron en el exact-head actual: pasaron las 2,388 unitarias y 28 integraciones completas. La clasificación causal de esos resultados históricos queda para los puntos 20/21; no se infiere causa ni cierre global sólo por esta corrida. Fase 7 no se ejecutó.

**Punto 15: CERRADO.** El mapeo, esquema físico, round-trip exacto de microsegundos y rollback pasan con ambos providers certificados.

MAPA_ARQUITECTURA: SIN_CAMBIO.
