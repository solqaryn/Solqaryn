# ADR N1.9 — Autoridad de stock y trazabilidad por lote/serie

## Estado

Aceptado para ERP-N1.9.

## Contexto

Solqaryn ya posee una autoridad física agregada: `ExistenciaVariante`, identificada por Variante + Almacén + Ubicación. N1.9 necesita lote, serie y vencimiento sin duplicar esa autoridad ni transformar metadata logística en variantes comerciales.

## Decisión

1. `ExistenciaVariante` permanece como única autoridad cuantitativa agregada.
2. `ProductoVariante` contiene una política opt-in de trazabilidad.
3. `LoteInventario` y `SerieInventario` representan identidad, no un stock libre alternativo.
4. La activación de trazabilidad sobre stock existente es fail-closed salvo adopción/reconciliación explícita.
5. Las mutaciones de política e identidad son transaccionales, idempotentes cuando el payload coincide y protegidas por unicidad persistente.
6. Las auditorías críticas se registran estrictamente dentro de la misma unidad transaccional.
7. No se inventan identidades históricas mediante backfill heurístico.

## Consecuencias

- Variantes no trazables siguen funcionando sin lote/serie.
- Lote/serie no altera la identidad comercial/SKU de la variante.
- Las carreras concurrentes se resuelven con locks y constraints, no sólo con comprobaciones previas.
- Rollback de identidad histórica es forward-only/restauración compatible cuando ya existen referencias reales.

## Rechazado

- Convertir cada lote/serie en una `ProductoVariante`.
- Mantener cantidades autoritativas independientes en `LoteInventario`.
- Confiar únicamente en validación de aplicación para seriales duplicados.
- Registrar auditoría crítica después del commit.
- Crear lotes/series ficticios para reconciliar histórico.