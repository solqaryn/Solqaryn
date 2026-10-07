# Modernización — Fase 6 MySQL / EF Provider Gate — Estado vigente 2026-10-04

## Actualización autoritativa post-Fase 7 — 2026-10-06

El estado vigente de `dev` ya no es net8/EF8/Pomelo. Fase 7 fue integrada mediante PR #3552 y certificada sobre el HEAD exacto:

`7013a55cd7491c3c79d6808df5db9ba0b8580f40`

Evidencia vigente:

- Fase 6 post-Fase7: run `37540172761` — SUCCESS.
- Fase 7: run `37540172636` — SUCCESS.
- `FASE_6_MYSQL_EF_PROVIDER=PASS`.
- `CURRENT_PROVIDER_CERTIFIED=ORACLE_MYSQL_EFCORE_10_0_9`.
- `CURRENT_PROVIDER_STATUS=STABLE_ON_NET10_EF10`.
- `TARGET_PROVIDER_SELECTED=ORACLE_MYSQL_EFCORE_10_0_9`.
- `POMELO_HISTORY_PRESERVED=107`.
- `POMELO_HISTORY_REPLAYED_BY_ORACLE=false`.
- `PRODUCT_PROVIDER_AUTHORITY=ORACLE_ONLY`.
- `P0=0`.
- `P1=0`.
- `PHASE7_EXECUTED=true`.

Stack vigente post-Fase7:

- `TargetFramework=net10.0`;
- SDK `10.0.401`;
- runtime/ASP.NET `10.0.12`;
- C# 14;
- EF Core/Design/CLI `10.0.12`;
- `MySql.EntityFrameworkCore 10.0.9`;
- `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.12`;
- provider productivo único Oracle;
- Pomelo queda únicamente como historia/probe de compatibilidad y no como provider runtime productivo.

Las secciones históricas posteriores se conservan para trazabilidad. Cualquier texto histórico que diga que Fase 7 no fue ejecutada o que el runtime vigente sigue net8/EF8/Pomelo queda superado por esta actualización autoritativa.

## Actualización formal de cierre — 2026-10-05

El estado anterior de bloqueo queda reemplazado por el dictamen exact-head del gate `37268035079`, sobre `dev` HEAD `877af434ee15c8fd3a08f974bf58a62c91c03179`: **`FASE_6_MYSQL_EF_PROVIDER=PASS`, `P0=0`, `P1=0`, `TARGETFRAMEWORK_CHANGE=ALLOWED_AFTER_PHASE6_CLOSE`, `PHASE7_EXECUTED=false`**. Los cinco jobs del gate terminaron `success`. La aceptación integral DEV (`37268035136`) y el tooling frontend Fase 5 (`37268035072`) también pasaron sobre el mismo HEAD. Fase 6 queda formalmente cerrada; este resultado no inicia Fase 7 ni cambia el runtime productivo.

Evidencia del cierre formal: `docs/evidencias/modernizacion/PUNTO_30_FASE6_STOP_A_PASS_2026-10-05.md`.

## Alcance

Esta evidencia describe el estado vigente de la modernización del provider MySQL en `dev`. No modifica QA, `main`, PROD, Aiven real, datos productivos ni secretos. Fase 7 continúa sin ejecutarse mientras esta fase sólo certifica la ruta técnica.

## Stack vigente

El runtime productivo permanece en:

- `TargetFramework=net8.0`;
- EF Core 8;
- `Pomelo.EntityFrameworkCore.MySql 8.0.2`;
- `MySqlConnector 2.3.7`.

Este stack continúa certificado como baseline actual y el gate permanente vuelve a ejecutar migraciones, modelo, SQL, integración MySQL, contratos 1062/1205/1213 y aislamiento tenant.

## Ruta objetivo certificada para .NET 10

La ruta objetivo ya certificada es:

- `net10.0`;
- `Microsoft.EntityFrameworkCore 10.0.12`;
- `MySql.EntityFrameworkCore 10.0.9` (Oracle Connector/NET).

La lane se ejecuta de forma efímera sobre el backend real y exige build, conexión MySQL, LINQ, modelo, JSON, `decimal(18,2)`, `decimal(18,4)`, `datetime(6)`, transacciones/rollback, duplicate key 1062, retry transitorio y auditoría tenant.

La historia Pomelo no se reescribe. La conversión certificada usa baseline físico MySQL y adopción Oracle sin DDL sobre esquemas históricos.

## Estado Pomelo 10

El gate prueba explícitamente `Pomelo.EntityFrameworkCore.MySql 10.0.0` desde NuGet estable. Al 2026-10-04 el paquete estable no es resoluble y el upstream público mantiene 9.0.0 como último release estable.

Esta ausencia es **informativa y no bloqueante** porque SOLQARYN ya no selecciona Pomelo 10 como ruta objetivo.

Reglas permanentes:

- `POMELO10_REQUIRED_FOR_TARGET_ROUTE=false`;
- `POMELO10_ROUTE_SELECTED=false`;
- si Pomelo 10 sigue ausente: `UNAVAILABLE_NON_BLOCKING`;
- si Pomelo 10 aparece en el futuro: `AVAILABLE_NOT_SELECTED_PENDING_SEPARATE_CERTIFICATION`;
- una nightly, preview, RC o release futura de Pomelo nunca sustituye automáticamente la ruta Oracle certificada;
- cualquier cambio de provider futuro requiere su propia certificación exact-head y autorización técnica.

## Dictamen vigente

El certifier permanente `.github/workflows/modernization-phase6-mysql-ef-provider.yml` sólo autoriza la ruta de .NET 10 cuando la lane Oracle EF10 pasa. La disponibilidad de Pomelo 10 no participa en la condición de éxito.

Resultados esperados de cierre:

- `FASE_6_MYSQL_EF_PROVIDER=PASS`;
- `TARGET_PROVIDER_SELECTED=ORACLE_MYSQL_EFCORE_10_0_9`;
- `TARGET_EF_SELECTED=10.0.12`;
- `TARGET_NET_SELECTED=net10.0`;
- `POINT_2_POMELO10_DEPENDENCY=PASS`;
- `POMELO10_BLOCKS_MODERNIZATION=false`;
- `POMELO10_REQUIRED_FOR_TARGET_ROUTE=false`;
- `PHASE7_EXECUTED=false`.

## Cierre

La inexistencia actual de Pomelo 10 estable deja de ser deuda bloqueante. Pomelo 10 permanece únicamente como alternativa futura potencial y no seleccionada.

MAPA_ARQUITECTURA: SIN_CAMBIO.
