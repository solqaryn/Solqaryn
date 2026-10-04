# Modernización — Fase 6 MySQL / EF Provider Gate — Estado vigente 2026-10-04

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
