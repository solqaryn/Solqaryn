# Modernización — Fase 6 MySQL / EF Provider Gate — 2026-10-04

## Alcance y regla de cierre

Esta fase se ejecuta exclusivamente sobre `dev`. No modifica `qa`, `main`, PROD, Aiven productivo, secretos ni datos reales. El objetivo es decidir, antes de cambiar el `TargetFramework`, si SOLQARYN dispone de una ruta MySQL/Entity Framework estable y demostrada. El criterio es fail-closed: **provider estable certificado o STOP**.

El certifier permanente es `.github/workflows/modernization-phase6-mysql-ef-provider.yml`; este documento forma parte de sus paths exact-head y, por tanto, cualquier cambio en esta evidencia obliga a reejecutar el gate.

## Baseline autoritativo

El backend vigente permanece sin cambios productivos en:

- `TargetFramework=net8.0`;
- `Microsoft.EntityFrameworkCore=8.0.2`;
- `Microsoft.EntityFrameworkCore.Design=8.0.2`;
- `Pomelo.EntityFrameworkCore.MySql=8.0.2`;
- `MySqlConnector=2.3.7`;
- registro runtime `UseMySql(..., MySqlServerVersion)`;
- historial y snapshot EF con extensiones específicas de Pomelo;
- `UnitOfWork` con retry explícito para MySQL 1205/1213 y traducción dirigida de 1062 mediante `MySqlConnector.MySqlException`.

No se cambió ningún `.csproj`, migración productiva, `DbContext`, repositorio o servicio para favorecer a un candidato.

## Pomelo — baseline profundo

El gate aplica el historial completo sobre MySQL 8.4 efímero, comprueba `has-pending-model-changes`, genera SQL forward y valida físicamente el esquema.

Evidencia causal previa a este documento, repetida por el certifier exact-head:

- 107 migraciones EF aplicadas;
- 137 tablas;
- 3 columnas JSON;
- 54 columnas `decimal(18,2)`;
- 64 columnas `decimal(18,4)`;
- 373 columnas `datetime(6)`;
- 563 índices;
- 249 foreign keys;
- 247 CHECK constraints;
- 137 tablas `utf8mb4`;
- transacción + rollback: PASS;
- decimal exacto `1234567890.1234`: PASS;
- microsegundos `datetime(6)`: PASS;
- JSON extraction: PASS;
- collation binaria case-sensitive: PASS;
- duplicate key 1062: PASS;
- retry/exception contract dirigido: 9/9 PASS;
- suite MySQL `Category=Integration`: 27/27 PASS;
- auditoría multi-tenant: PASS en identidad, scope vivo, autorización de recurso, persistencia, reportes, archivos, cache y background; background runtime count 0.

Conclusión del baseline: **Pomelo 8.0.2 es estable para el stack vigente net8/EF8 de SOLQARYN**.

## Oracle — comparación profunda

Se evaluó `MySql.EntityFrameworkCore 8.0.28` sin sustituir el runtime productivo. El probe reutiliza el `AppDbContext`, configuraciones e historial reales contra MySQL 8.4 descartable.

Resultado:

- conexión al esquema ya creado por Pomelo: PASS;
- modelo, LINQ, transacción/rollback, JSON, decimal, datetime e índices sobre ese esquema: PASS;
- historial completo desde base vacía: **FAIL** con `MySql.Data.MySqlClient.MySqlException: Field 'Id' doesn't have a default value`;
- equivalencia física de esquema desde cero: **NO DEMOSTRADA / false**;
- duplicate key conserva número 1062, pero el tipo real es `MySql.Data.MySqlClient.MySqlException`;
- el `UnitOfWork` vigente espera `MySqlConnector.MySqlException`, por lo que `duplicateTranslated=false`, `exceptionTypeCompatible=false` y `retryContractCompatible=false`;
- `ORACLE_DROP_IN=false`.

Oracle no es un sustituto drop-in. Adoptarlo exigiría una migración deliberada de provider, historia/migraciones, factory/runtime y contratos de excepción/retry; hacerlo dentro de un simple cambio de `TargetFramework` sería inseguro.

## Probe aislado net10

El certifier copia el repositorio a un directorio efímero, retargetea únicamente esa copia a `net10.0`, restaura y compila. El repositorio real permanece intacto.

La compilación del backend retargeteado es posible, pero el stack actual EF8/Pomelo8 falla en ejecución LINQ bajo .NET 10. La suite unitaria reproduce fallos en `Microsoft.EntityFrameworkCore.Query.Internal.ParameterExtractingExpressionVisitor` con `TypeLoadException` / `ReadOnlySpan<int>`. Este defecto es causal y bloquea cambiar sólo el TFM.

El probe de disponibilidad de paquetes demuestra además:

- `Pomelo.EntityFrameworkCore.MySql 10.0.0`: no existe como paquete estable resoluble en el gate;
- `MySql.EntityFrameworkCore 10.0.9`: disponible, pero la familia Oracle ya quedó descartada como drop-in por historia de migraciones y contratos de excepción.

No se adopta EF9/Pomelo9 como puente canónico: sería una ruta temporal próxima al fin de soporte y no resuelve la exigencia de una modernización net10 LTS sostenible.

## Dictamen

- `CURRENT_PROVIDER_CERTIFIED=POMELO` para el baseline vigente net8/EF8.
- `ORACLE_DROP_IN=false`.
- No existe en esta fase una ruta provider/EF **estable y certificada para net10**.
- `FASE_6_MYSQL_EF_PROVIDER=STOP`.
- `TARGETFRAMEWORK_CHANGE=BLOCKED`.
- Motivo: `NO_CERTIFIED_STABLE_PROVIDER_LANE_FOR_NET10`.

Por tanto, **no se autoriza cambiar el TargetFramework**. La siguiente acción sólo podrá abrirse cuando exista y se pruebe una lane EF/provider soportada para net10 que preserve migraciones, SQL, LINQ, transacciones, retry, concurrencia, tipos, excepciones MySQL y aislamiento tenant; o cuando se autorice explícitamente una migración de provider con su propio plan de conversión y rollback.

## Trazabilidad causal

- Run Pomelo profundo previo: `37212019148` — migraciones/esquema/semántica/retry/integración y audit tenant demostrados; el rojo posterior fue un falso positivo de higiene por artifacts temporales generados dentro del checkout y quedó corregido.
- Run Oracle profundo: `37212511325` / job `111466384990` — Oracle runtime parcial PASS, fresh migration FAIL y exception/retry contract incompatible.
- El run exact-head disparado por el commit que contiene este documento es la autoridad final de cierre de Fase 6.

