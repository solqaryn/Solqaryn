# ERP-N0 — Punto 4: Entidad y persistencia relacional `MetodoPago`

**Estado:** ✅ IMPLEMENTADO  
**Rama:** `Desarrollo`  
**Fecha:** 2026-08-11  
**Baseline de inicio:** `4b4ba2871a9ce2a6f9728f1b0efbb5da82ce13a4`

## 1. Objetivo

Implementar la base persistente y relacional de `MetodoPago` sin retirar todavía el enum legacy, de forma que las fases siguientes puedan realizar seed, backfill y migración de consumidores sin una ruptura big-bang.

## 2. Entidad persistente

Se creó:

- `backend/src/Domain/Entities/Catalogos/MetodoPago.cs`

La entidad incluye:

- `Id`
- `Codigo`
- `Nombre`
- `Tipo`
- `Activo`
- `RequiereReferencia`
- `RequiereBanco`
- `PermiteCambio`
- `Orden`
- `Metadata`
- auditoría de creación/actualización heredada de `AuditableEntity`
- soft-delete: `Eliminado`, `FechaEliminacion`, `EliminadoPorUsuarioId`
- `CodigoNormalizado` calculado para imponer unicidad funcional estable

La entidad se ubica temporalmente en `Solqaryn.Domain.Entities.Catalogos` para evitar una colisión nominal con `Solqaryn.Domain.Enums.MetodoPago` mientras dura la transición.

## 3. Configuración EF Core

Se creó:

- `backend/src/Infrastructure/Persistence/Configurations/MetodoPagoConfiguration.cs`

Configuración principal:

- tabla: `MetodosPago`;
- PK: `Id`;
- `Codigo`: requerido, máximo 50;
- `CodigoNormalizado`: columna calculada `LOWER(TRIM(Codigo))`;
- `Nombre`: requerido, máximo 120;
- `Tipo`: requerido, máximo 50;
- `Metadata`: tipo MySQL `json`;
- defaults explícitos para flags de comportamiento, `Activo`, `Orden` y `Eliminado`;
- query filter para soft-delete.

`AppDbContext` ya aplica configuraciones mediante `ApplyConfigurationsFromAssembly`, por lo que no fue necesario introducir registro manual específico para esta entidad.

## 4. Índices y restricciones

Se definieron:

- `UX_MetodosPago_Codigo_Normalizado`: índice UNIQUE sobre `CodigoNormalizado`;
- `IX_MetodosPago_Nombre`;
- `IX_MetodosPago_Estado_Orden` sobre `Activo`, `Eliminado`, `Orden`;
- `IX_Ventas_MetodoPagoId`;
- `IX_FacturaPagos_MetodoPagoId`;
- `IX_MovimientosFinancieros_MetodoPagoId`.

La unicidad por código normalizado impide reutilizar códigos por diferencias de mayúsculas/minúsculas o espacios periféricos.

## 5. Relaciones implementadas

Se agregaron FKs transicionales nullable:

- `Venta.MetodoPagoId -> MetodosPago.Id`;
- `FacturaPago.MetodoPagoId -> MetodosPago.Id`;
- `MovimientoFinanciero.MetodoPagoId -> MetodosPago.Id`.

Cada agregado incorpora navegación `MetodoPagoCatalogo`.

Las tres relaciones usan `DeleteBehavior.Restrict` / `ReferentialAction.Restrict` para impedir que una eliminación física accidental del catálogo destruya o invalide documentos y movimientos históricos relacionados.

## 6. Compatibilidad legacy intencional

En este punto **no se eliminó** `Solqaryn.Domain.Enums.MetodoPago` ni se modificaron destructivamente las columnas legacy actuales.

Se mantienen temporalmente:

- `Venta.MetodoPago` como representación legacy string;
- `FacturaPago.MetodoPago` como representación legacy numérica;
- `MovimientoFinanciero.MetodoPago` como representación legacy string nullable.

`MetodoPagoId` permanece nullable hasta completar el seed/backfill y la migración de servicios/contratos. Esto permite una transición expand-and-contract segura.

## 7. Migración EF

EF Core generó:

- `20260812022343_N0_5_MetodoPagoRelacionalBase.cs`;
- `20260812022343_N0_5_MetodoPagoRelacionalBase.Designer.cs`;
- actualización de `AppDbContextModelSnapshot.cs`.

El nombre lógico de la migración es `N0_5_MetodoPagoRelacionalBase`; la numeración responde a la secuencia técnica de migraciones ERP-N0 ya existente en el repositorio.

La migración es aditiva:

1. agrega `MetodoPagoId` nullable a `Ventas`, `FacturaPagos` y `MovimientosFinancieros`;
2. crea `MetodosPago`;
3. crea índices;
4. crea FKs `RESTRICT`;
5. su `Down` elimina únicamente los objetos incorporados por esta migración.

No ejecuta `DROP` ni alteración destructiva sobre las columnas legacy `MetodoPago`.

## 8. Validaciones realizadas

- scaffolding real mediante `dotnet ef migrations add` con EF Core 8;
- restore previo del backend completado correctamente;
- generación de migration + Designer + snapshot completada correctamente;
- guard automático que verificó presencia de `MetodosPago` y `MetodoPagoId` y bloqueó patrones destructivos legacy antes de publicar;
- revisión del `Up/Down` generado;
- revisión del snapshot para confirmar entidad, índices y las tres navegaciones/FKs;
- el workflow temporal de scaffolding fue retirado del HEAD final.

La certificación definitiva del HEAD se realiza con los workflows normales de `Desarrollo`, incluyendo la verificación de cambios pendientes del modelo EF.

## 9. Fuera de alcance de este punto

Este cierre estructural no realiza todavía:

- seed de `Efectivo`, `Transferencia`, `Tarjeta`, `Otro`;
- backfill de valores históricos;
- `MetodoPagoId` NOT NULL donde corresponda;
- migración de `Enum.TryParse<MetodoPago>` en servicios;
- cambios DTO/API/frontend/PDF;
- eliminación del enum legacy.

Esas tareas corresponden a los puntos posteriores de migración funcional y retirada legacy.

## 10. Resultado

**Punto 4 — Entidad y persistencia relacional `MetodoPago`: ✅ REALIZADO.**

La autoridad relacional ya tiene modelo, configuración EF, índices, restricciones, migración y vínculos estructurales preparados. El enum permanece de forma temporal y controlada hasta completar seed/backfill y migración de consumidores.