# Modernización — Fase 6 MySQL / EF Provider Gate — Estado vigente

## Estado actual después de Fase 7 — 2026-10-06

Fase 6 permanece certificada después de la migración productiva de DEV a .NET 10/EF Core 10.

HEAD post-merge verificado:

`7013a55cd7491c3c79d6808df5db9ba0b8580f40`

Run Fase 6 post-Fase7:

`37540172761` — SUCCESS.

Dictamen:

- `FASE_6_MYSQL_EF_PROVIDER=PASS`
- `CURRENT_PROVIDER_CERTIFIED=ORACLE_MYSQL_EFCORE_10_0_9`
- `TARGET_PROVIDER_SELECTED=ORACLE_MYSQL_EFCORE_10_0_9`
- `POMELO_HISTORY_PRESERVED=107`
- `POMELO_HISTORY_REPLAYED_BY_ORACLE=false`
- `PRODUCT_PROVIDER_AUTHORITY=ORACLE_ONLY`
- `P0=0`
- `P1=0`

## Stack vigente en DEV

- `TargetFramework=net10.0`
- .NET SDK `10.0.401`
- .NET Runtime / ASP.NET Core Runtime `10.0.12`
- C# 14
- EF Core / Design / CLI `10.0.12`
- Oracle `MySql.EntityFrameworkCore 10.0.9`
- JwtBearer `10.0.12`

Pomelo/MySqlConnector no forman parte del runtime productivo.

## Historial Pomelo

Las 107 migraciones Pomelo permanecen preservadas como historia auditable. Oracle no las reproduce. La cadena activa usa el assembly Oracle `Solqaryn.Infrastructure.Migrations` y el baseline `20261006111818_OracleBaseline`.

## Pomelo 10

La disponibilidad futura de Pomelo 10 no participa en la ruta productiva seleccionada. Cualquier cambio de provider posterior requiere una certificación separada.

## Cierre histórico de Fase 6

Fase 6 fue cerrada originalmente antes de ejecutar Fase 7, con `TARGETFRAMEWORK_CHANGE=ALLOWED_AFTER_PHASE6_CLOSE`. Ese estado histórico autorizó el inicio de Fase 7 pero ya no describe el runtime vigente.

La autoridad actual es el gate post-Fase7 `37540172761`, que demuestra que los contratos de Fase 6 permanecen verdes con Oracle EF10 como provider productivo.

MAPA_ARQUITECTURA: ACTUALIZADO.
