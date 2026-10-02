# N8.15.C — DB_MIG inventario

Estado: `LISTO_REAL`

Baseline de entrada: `80e63559500fbed7864b834027f024e890346740`.

## Persistence root

- DbContext autoritativo localizado: `backend/src/Infrastructure/Persistence/AppDbContext.cs` (`AppDbContext : DbContext`).
- Provider/runtime: EF Core 8.0.2 + `Pomelo.EntityFrameworkCore.MySql` 8.0.2 + `MySqlConnector` 2.3.7, definidos en `Solqaryn.Infrastructure.csproj`.
- Persistencia principal: MySQL.
- Configuraciones relacionales: `backend/src/Infrastructure/Persistence/Configurations/**`.
- Repositorios: `backend/src/Infrastructure/Repositories/**`.

## Entidades/tablas

`AppDbContext` expone DbSets de catálogo/productos, usuarios/RBAC, compras, inventario, ventas/facturación, finanzas, cotizaciones, bancos, contabilidad, descuentos, impuestos, enlaces/documentos, configuración, cargas masivas, KPI y outbox. El inventario físico de `Domain/Entities` contiene además bounded folders como `Bancos/`, `Cajas/`, `Catalogos/` y entidades contables; por ello la lista de persistencia se deriva del DbContext/configuraciones y no sólo del layout de entidades.

## Integridad, FKs, constraints e índices

La definición de integridad se distribuye entre:

1. `AppDbContext` / `OnModelCreating` y filtros/reglas transversales;
2. `Persistence/Configurations/*Configuration.cs`, con configuraciones por aggregate/entidad;
3. snapshots y archivos de migración que materializan FKs, constraints e índices;
4. invariantes adicionales de `SaveChangesAsync`, que actualmente revalidan aislamiento comercial de ventas y snapshots/valorización de compras antes de persistir.

Se inventariaron configuraciones explícitas para áreas como ajustes de inventario, almacenes, CxP, asientos, asignación de costos, bancos, cajas, capas de costo y otras entidades. Ninguna constraint/índice se considera removible por ausencia aparente en una sola capa.

## Historial de migraciones

Existen **dos roots físicos de migraciones** que deben conservarse durante el baseline:

- `backend/src/Infrastructure/Migrations/**`: historial inicial/legacy (`InitialCreate`, fases de usuarios/categorías, compras, ventas/facturación/finanzas, proveedores, clientes y sucesivas migraciones).
- `backend/src/Infrastructure/Persistence/Migrations/**`: historial posterior que incluye M7 y series N1/N2 y posteriores.

Esto se clasifica como `KEEP` durante N8.15. La coexistencia de dos roots es un candidato de `CONSOLIDATE` arquitectónico/documental para N8.16/N8.18, pero **no** autoriza mover, regenerar o borrar migraciones, designers o snapshots.

## Seeds / fixtures

El baseline no presupone que un seed exista por nombre. Seeds/fixtures deben contabilizarse desde `AppDbContext`, configuraciones, scripts y tests en el catálogo final; cualquier candidato no confirmado permanece `UNKNOWN`.

## Clasificación

| Asset | Clasificación N8.15.C | Motivo |
| --- | --- | --- |
| `AppDbContext` | KEEP | composition/persistence authority actual |
| `Persistence/Configurations/**` | KEEP | FKs/constraints/indexes/mapping |
| `Infrastructure/Migrations/**` | KEEP | historia EF activa/legacy |
| `Infrastructure/Persistence/Migrations/**` | KEEP | historia EF posterior activa |
| migraciones duplicadas sólo por apariencia | UNKNOWN | requiere referencia/snapshot/build antes de remover |
| scripts/fixtures no referenciados todavía | UNKNOWN | G debe confirmar referencias |

`REMOVE_SAFE = 0` en esta etapa.

## REVIEW_FIRST

P0=0, P1=0. No se ejecutó DDL, no se aplicaron migraciones, no se tocaron datos ni Producción. El riesgo de tratar las dos carpetas de migración como duplicado fue neutralizado clasificándolas como historial preservado hasta validación causal posterior.

## Resultado

`N8.15.C = LISTO_REAL`.

Siguiente dependency-valid: `N8.15.D — BACKEND_API`.
