# Fase 6 — MySQL / EF Provider Gate — Estado vigente post-Fase 7

Fecha de reconciliación: 2026-10-06  
Repositorio: `solqaryn/Solqaryn`  
Rama vigente: `dev`

## Dictamen vigente

`FASE_6_MYSQL_EF_PROVIDER=PASS`

La Fase 6 permanece cerrada después de integrar Fase 7. Su función vigente es proteger los contratos de provider, migraciones, MySQL y tenancy sobre el runtime actual.

Evidencia exact-head post-Fase 7:

- SHA: `7013a55cd7491c3c79d6808df5db9ba0b8580f40`;
- run Fase 6: `37540172761`;
- `Fase 6 - contratos MySQL EF preservados post-Fase7`: PASS;
- `Dictamen Fase 6`: PASS.

## Runtime vigente

El stack actual en `dev` es:

- `TargetFramework=net10.0`;
- C# 14;
- SDK `10.0.401`;
- runtime/ASP.NET `10.0.12`;
- EF Core `10.0.12`;
- provider Oracle `MySql.EntityFrameworkCore 10.0.9`;
- JwtBearer `10.0.12`;
- `dotnet-ef 10.0.12`;
- autoridad productiva de provider: Oracle solamente.

Pomelo 8/MySqlConnector ya no son el runtime productivo.

## Historia Pomelo

Se conserva el contrato histórico:

- `POMELO_HISTORY_PRESERVED=107`;
- `POMELO_HISTORY_REPLAYED_BY_ORACLE=false`;
- las 107 migraciones antiguas permanecen auditables e inmutables;
- el runtime activo usa el assembly Oracle;
- `OracleBaseline` es la frontera de adopción;
- las migraciones futuras son Oracle EF10.

## Contratos preservados

El gate post-Fase 7 mantiene:

- duplicate key MySQL 1062;
- retry transitorio 1205/1213;
- LINQ compatible;
- JSON;
- precisión decimal;
- `datetime(6)`;
- collations/case-sensitivity;
- transacciones y rollback;
- concurrencia;
- aislamiento tenant fail-closed;
- integración MySQL;
- coherencia EF runtime/design/CLI;
- fresh bootstrap;
- modelo sin pending changes;
- provider único productivo.

Dictamen del run:

- `CURRENT_PROVIDER_CERTIFIED=ORACLE_MYSQL_EFCORE_10_0_9`;
- `TARGET_PROVIDER_SELECTED=ORACLE_MYSQL_EFCORE_10_0_9`;
- `PRODUCT_PROVIDER_AUTHORITY=ORACLE_ONLY`;
- `P0=0`;
- `P1=0`.

## Relación con Fase 7

`PHASE7_EXECUTED=true`.

La afirmación histórica `PHASE7_EXECUTED=false` sólo describe los cierres anteriores de Fase 6 y ya no representa el estado vigente.

La evidencia canónica de Fase 7 es:

`docs/evidencias/modernizacion/FASE_7_DOTNET10_EF10_2026-10-06.md`.

## Enforcement

El ruleset vigente de `dev` mantiene `Dictamen Fase 6` como required status check. Fase 7 dispone de su gate permanente separado; la incorporación de `Dictamen Fase 7` al ruleset requiere mutación administrativa y readback.

MAPA_ARQUITECTURA: ACTUALIZADO.
