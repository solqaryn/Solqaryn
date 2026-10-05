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

- Fase 6 exact-head: run `37302818734`, SHA `a13fa5190c7c636432751334ca38fa608fdc6536`; 5/5 jobs success. Dictamen `FASE_6_MYSQL_EF_PROVIDER=PASS`, `P0=0`, `P1=0`, `PHASE7_EXECUTED=false`.
- Pomelo/MySQL 8.4.11: `DATETIME6_PROVIDER_CONTRACT=PASS model=Compras.Fecha physical=datetime(6) efRoundTrip=true sqlMicroseconds=123456 rolledBack=true`; inventario físico `POMELO_DATETIME6=373`; integración 28/28 pass.
- Oracle EF10/net10: mismo `DATETIME6_PROVIDER_CONTRACT=PASS`; 2336/2336 unitarias y 22/22 integraciones pass.
- [Fase 6 exact-head](https://github.com/solqaryn/Solqaryn/actions/runs/37302818734).

## Deuda independiente conservada

El lane informativo que retargetea efímeramente EF8/Pomelo a net10 en ese mismo HEAD reporta 8 fallos unitarios (2380/2388 pasan) y 3 de integración (25/28 pasan). Esta deuda pertenece a los puntos 20/21, no al contrato de fecha ni a la integración del stack vigente. Fase 7 no se ejecutó.

**Punto 15: CERRADO.** El mapeo, esquema físico, round-trip exacto de microsegundos y rollback pasan con ambos providers certificados.

MAPA_ARQUITECTURA: SIN_CAMBIO.
