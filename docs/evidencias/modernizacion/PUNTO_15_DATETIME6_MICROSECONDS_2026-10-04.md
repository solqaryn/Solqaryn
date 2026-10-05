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

- Pomelo/MySQL 8.4.11, run `37246049107`, SHA `32536e867c3ef04021007054762b7d5eeabbe060`: `DATETIME6_PROVIDER_CONTRACT=PASS model=Compras.Fecha physical=datetime(6) efRoundTrip=true sqlMicroseconds=123456 rolledBack=true`.
- Oracle EF10/net10, run `37246049128`, mismo SHA: el mismo contrato `DATETIME6_PROVIDER_CONTRACT=PASS`.
- Integración MySQL del provider vigente: 27/27 pass.
- Gate exact-head Fase 6: run `37246049107`, 5/5 jobs success; dictamen `FASE_6_MYSQL_EF_PROVIDER=PASS`, `P0=0`, `P1=0`, `PHASE7_EXECUTED=false`.

## Deuda independiente conservada

El lane informativo que retargetea efímeramente EF8/Pomelo a net10 sigue reportando 8 fallos unitarios (2380/2388 pasan) y 3 de integración (24/27 pasan). Esta deuda pertenece a los puntos 20/21, no al contrato de fecha ni a la integración del stack vigente. Fase 7 no se ejecutó.

MAPA_ARQUITECTURA: SIN_CAMBIO.
