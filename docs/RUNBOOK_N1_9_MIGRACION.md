# Runbook de migración N1.9 — Trazabilidad por lote/serie

## Alcance

Procedimiento operativo para aplicar y verificar la persistencia N1.9 sin inventar identidades históricas ni modificar Producción desde este flujo.

Migración canónica del bloque N1.9.C:

```text
20260817100000_N1_9_TrazabilidadLotesSeries
```

## Antes de aplicar

1. Ejecutar el preflight N1.9 en modo read-only.
2. Confirmar que el historial EF está íntegro y sin migraciones parciales.
3. Verificar snapshot/modelo sin drift pendiente.
4. Confirmar respaldo/restauración compatible del ambiente autorizado.
5. No habilitar políticas de lote/serie/vencimiento automáticamente durante la migración.

## Aplicación

La migración es aditiva:

- incorpora flags opt-in en `ProductoVariante`;
- crea `LotesInventario`;
- crea `SeriesInventario`;
- agrega índices, checks y FKs restrictivas;
- no crea lotes/series ficticios;
- no mueve ni recalcula `StockFisico`, `StockReservado` o `StockTransito`.

## Verificación posterior

Ejecutar el postcheck N1.9 y comprobar:

- columnas de política presentes y con defaults coherentes;
- tablas e índices creados una sola vez;
- unicidad de identidad serial protegida en MySQL;
- FKs restrictivas válidas;
- snapshot EF reconciliado;
- `has-pending-model-changes` sin drift;
- CI/integración MySQL verdes.

## Histórico

No realizar backfill heurístico de lote, serie o vencimiento a partir de SKU, fechas, descripciones o movimientos históricos. El stock previo puede permanecer no trazado hasta una adopción/reconciliación explícita de la variante.

## Rollback

Si la migración aún no contiene identidades referenciadas y el ambiente es controlado, el mecanismo técnico `Down` puede usarse únicamente conforme al procedimiento de recuperación autorizado. Una vez existan lotes/series reales o referencias históricas, el rollback operativo es forward-fix o restauración completa compatible; nunca eliminación manual selectiva.

## Prohibiciones

- no DDL/DML manual en Producción;
- no modificar `main`;
- no force-push;
- no merge/auto-merge del PR #2;
- no inventar identidades para conseguir igualdad matemática.