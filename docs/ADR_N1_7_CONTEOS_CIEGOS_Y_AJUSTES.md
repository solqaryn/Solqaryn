# ADR N1.7 — Conteos ciegos, snapshots y ajustes posteriores

## Estado

Aceptado para ERP-N1.7.

## Contexto

Los conteos físicos necesitan comparar una observación humana con el stock físico autoritativo sin convertir el documento de conteo en una segunda fuente de stock. Además, el modo ciego debe impedir que el capturista conozca o reconstruya el stock esperado antes del cierre.

El sistema ya dispone de `ExistenciaVariante` como autoridad física y de `AjusteInventario` como mecanismo transaccional, auditable y reversible para modificar stock.

## Decisión 1 — ExistenciaVariante sigue siendo la única autoridad

`ConteoInventario` nunca será stock vivo. Sus snapshots son evidencia histórica del universo contado.

La identidad física del detalle se mantiene como:

`ProductoVarianteId + AlmacenId + UbicacionAlmacenId`

La cantidad contra la que se cuenta es `ExistenciaVariante.StockFisico` capturada al materializar el universo.

## Decisión 2 — Las diferencias no escriben stock directamente

Cerrar o aprobar un conteo no modifica `StockFisico`. Cuando existen diferencias aprobadas, se genera un `AjusteInventario` borrador.

La modificación efectiva del stock ocurre únicamente mediante el lifecycle formal del ajuste, con sus locks, snapshots, auditoría, Kardex y reversión.

Esto evita duplicar reglas de concurrencia y mantiene una sola frontera de escritura física.

## Decisión 3 — Privacidad ciega por contrato

El modo ciego se implementa en la capa de aplicación/API y no depende de ocultar columnas visualmente.

Mientras el conteo ciego esté activo antes del cierre:

- `StockEsperado` se oculta;
- `Diferencia` se oculta;
- `CantidadConDiferencia` se neutraliza;
- `DiferenciaNeta` se neutraliza;
- la cantidad realmente capturada puede mostrarse.

La política aplica a `GetById` y listados paginados. Un conteo ciego cancelado antes del cierre mantiene la privacidad porque nunca alcanzó la etapa de conciliación.

Después de un cierre válido, snapshot y diferencias pueden revelarse a usuarios autorizados para reconciliación.

## Decisión 4 — No permitir inferencia matemática

Ocultar únicamente `StockEsperado` no es suficiente. Si se devuelven simultáneamente `CantidadContada` y `Diferencia`, el cliente puede reconstruir:

`StockEsperado = CantidadContada - Diferencia`

Por eso la diferencia y sus agregados derivados permanecen ocultos durante el periodo ciego.

## Decisión 5 — Captura por lote atómica

La captura por lote pre-valida todas las líneas antes de mutar el agregado. Si una línea es inválida, ajena al conteo o viola el contrato, ninguna captura del lote debe quedar aplicada parcialmente.

Cantidad `0` es válida porque representa una observación física legítima. Cantidades negativas y detalles inválidos son fail-closed.

## Decisión 6 — Generación de ajuste idempotente

La generación posterior del ajuste debe ser única e idempotente. El servicio debe rechazar:

- conteos no aprobados;
- conteos sin diferencias;
- vínculos parciales de detalles hacia distintos ajustes;
- reintentos que pretendan crear un segundo ajuste para las mismas diferencias.

Cuando ya existe un vínculo consistente, la operación debe reutilizar el resultado canónico en lugar de duplicarlo.

## Consecuencias positivas

- una sola autoridad de stock;
- menor superficie de concurrencia;
- privacidad real de conteos ciegos;
- auditoría y rollback coherentes con ajustes existentes;
- menor duplicación de lógica de Kardex;
- pruebas determinísticas sobre inferencias y atomicidad.

## Costes y trade-offs

- el conteo aprobado no produce un cambio físico inmediato hasta confirmar el ajuste;
- la UI debe comunicar claramente la diferencia entre aprobar conteo y confirmar ajuste;
- la proyección DTO necesita lógica de privacidad por estado/tipo;
- la generación de ajustes requiere validaciones de idempotencia adicionales.

## Alternativas descartadas

### Escribir directamente ExistenciaVariante al aprobar conteo

Descartado porque duplicaría concurrencia, snapshots, auditoría y reversión que ya resuelve `AjusteInventario`.

### Ocultar sólo el stock esperado en frontend

Descartado porque el API seguiría filtrando información y el stock podría inferirse matemáticamente.

### Bloquear el almacén durante todo el conteo

Descartado por impacto operativo. Los locks deben ser cortos y concentrarse al materializar el ajuste.

## Validación

La decisión está respaldada por regresiones de N1.7 para privacidad ciega en borrador/en proceso/cancelado previo al cierre, inferencia matemática, revelación post-cierre, atomicidad de lote, generación idempotente de ajuste y lifecycle fail-closed.

Gates causales del HEAD de QA `7bba45d13a3fe0579285ed273062f66b2796893f`: Desarrollo, aceptación integral, Fase 8 y M13 en `SUCCESS`.
