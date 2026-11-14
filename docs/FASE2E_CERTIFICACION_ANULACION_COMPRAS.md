# Fase 2E — Certificación de anulación conservadora de compras

Fecha: 2026-08-07

## Objetivo

Certificar la regla del plan maestro que impide anular una compra cuando existe cualquier movimiento posterior sobre las combinaciones de producto/variante afectadas, preservando además la valoración histórica del inventario.

## Implementación verificada

### Bloqueo transaccional

`CompraService.AnularAsync`:

1. exige motivo de anulación;
2. ejecuta dentro de `IUnitOfWork.ExecuteInTransactionAsync`;
3. bloquea la cabecera de la compra con `GetByIdForUpdateAsync`;
4. exige estado `Confirmada`;
5. bloquea y valida inventario mediante `IInventarioConcurrencyService`;
6. obtiene el último movimiento original de la compra;
7. consulta si existe cualquier movimiento posterior sobre las claves originales producto/variante;
8. si existe, lanza `BusinessRuleException` y revierte la operación completa.

La consulta `MovimientoInventarioRepository.ExisteMovimientoPosteriorAsync` compara por:

```text
ProductoId + ProductoVarianteId
```

y no limita el tipo del movimiento posterior, por lo que cubre ventas, compras posteriores, ajustes, consumos y reversiones.

### Snapshots de valoración

La infraestructura ya incorpora snapshots de valoración en `CompraDetalle`:

```text
CostoProductoAnteriorSnapshot
CostoProductoNuevoSnapshot
CostoVarianteAnteriorSnapshot
CostoVarianteNuevoSnapshot
StockProductoAnteriorSnapshot
StockProductoNuevoSnapshot
StockVarianteAnteriorSnapshot
StockVarianteNuevoSnapshot
```

`AppDbContext.SaveChangesAsync` detecta las transiciones:

```text
Borrador -> Confirmada
Confirmada -> Anulada
```

Al confirmar captura los valores anteriores/nuevos. Al anular restaura el costo/stock de la variante afectada y recalcula el costo consolidado del producto usando las restantes variantes actuales no eliminadas.

Una compra histórica sin snapshots completos no se restaura a ciegas: la anulación automática es rechazada mediante `BusinessRuleException`.

## Pruebas específicas

`CompraValorizacionSnapshotTests` verifica:

- captura de snapshots antes/después al confirmar;
- restauración de variante afectada al anular;
- recálculo del producto cuando existe otra variante actual;
- restauración de costo anterior nullable;
- bloqueo de compra histórica sin snapshots completos.

Las pruebas de concurrencia/inventario del repositorio cubren adicionalmente las protecciones transaccionales utilizadas por `CompraService`.

## Evidencia automatizada de referencia

El candidato funcional que contiene esta implementación fue validado en el ciclo integral:

```text
Commit funcional: c5942990a36287ccb476c66f6f73c7d361d9eca3
Backend Release: 201/201 pruebas no-integración aprobadas
MySQL 8.4.11: aprobado
Playwright integral: 87/87 aprobado
Regresiones bloqueantes: 0
```

Los commits posteriores de certificación documental no modificaron esta lógica.

## Dictamen

```text
FASE 2E: COMPLETADA
BLOQUEO POR MOVIMIENTOS POSTERIORES: APROBADO
ANULACIÓN TRANSACCIONAL: APROBADA
SNAPSHOTS DE VALORACIÓN: APROBADOS
RESTAURACIÓN DE COSTO/EXISTENCIAS: APROBADA
HISTÓRICOS SIN SNAPSHOT: BLOQUEO FAIL-CLOSED APROBADO
REGRESIONES BLOQUEANTES CONOCIDAS: 0
```

## Gobernanza

- Rama: `Desarrollo`.
- PR #2 permanece abierto y en borrador.
- `main` permanece congelada.
- No se autoriza merge ni auto-merge.
- Producción no fue utilizada ni modificada.