# N5.5.B — Reportes de compras · contrato de dominio y semántica

Autoridad operativa: `docs/VAEP_AUTHORITY.md`.

## Decisión

N5.5.B no crea una segunda autoridad de compras. Define un contrato read-only sobre entidades ERP ya persistidas y conserva sus snapshots históricos. No requiere modificar invariantes de `OrdenCompra`, `RecepcionCompra`, `FacturaProveedor`, `DevolucionProveedor` ni `EvaluacionProveedor`.

## Autoridades por medida

| Medida / dimensión | Autoridad persistida | Semántica canónica |
| --- | --- | --- |
| Proveedor | `OrdenCompra.ProveedorId` + `ProveedorNombreSnapshot` | Identidad documental del proveedor de la orden. Agrupar por ID; nombre es snapshot de presentación. |
| Precio ordenado | `OrdenCompraDetalle.PrecioUnitario` | Precio unitario comprometido en la línea de orden; nunca recalcular desde catálogo actual. |
| Precio facturado | `FacturaProveedorDetalle.PrecioUnitarioSnapshot` ligado por `OrdenCompraDetalleId` | Precio unitario facturado de la línea explícitamente vinculada a la orden. |
| Variación absoluta de precio | línea de factura ↔ línea de orden por `OrdenCompraDetalleId` | `PrecioUnitarioFacturado - PrecioUnitarioOrdenado`. Positivo = facturado por encima de lo ordenado; negativo = por debajo. Sólo se calcula cuando existe vínculo persistido y moneda compatible. |
| Variación porcentual | — | **NO EXPUESTA** en N5.5: no se autoriza un denominador adicional ni comportamiento para precio ordenado cero. |
| Recepción | `RecepcionCompra` + `RecepcionCompraDetalle` | Fechas, cantidades recibidas/aceptadas/dañadas/faltantes/sobrantes y costo snapshot. |
| Devolución a proveedor | `DevolucionProveedor` + detalles | Sólo documentos de devolución reales y su linaje persistido. Cancelación/anulación de orden o recepción no se cuenta como devolución. Para totales efectivos se excluye `Estado=Anulada`; borrador puede mostrarse sólo si el filtro lo pide explícitamente. |
| Cumplimiento temporal factual | `EvaluacionProveedor.FechaEsperadaUtc` y `FechaRecepcionUtc` | Exponer ambas fechas y `DesviacionEntregaDias = FechaRecepcionUtc.Date - FechaEsperadaUtc.Date`. Valor <=0 significa recepción en/a tiempo; >0 significa días posteriores. No se convierte en score/SLA/ranking. |
| Cumplimiento de cantidad factual | `EvaluacionProveedor` | Exponer `CantidadOrdenada`, `CantidadAceptada`, `CantidadDanada`, `CantidadSobrante`; no inventar ponderación ni porcentaje compuesto. |

## Moneda y comparabilidad

Una comparación de precio sólo es válida dentro de la misma moneda documental. Si la factura y la orden no tienen moneda compatible o el vínculo de línea no es explícito, `VariacionPrecioAbsoluta` debe ser `null`/no disponible; está prohibido aplicar una tasa de cambio implícita.

## Filtros mínimos autorizados

El contrato de consulta puede filtrar por:

- rango de fecha UTC (`DesdeUtc`, `HastaUtc`), con `DesdeUtc <= HastaUtc`;
- `ProveedorId` positivo;
- `ProductoId` positivo y `ProductoVarianteId` positivo cuando exista;
- estado documental cuando la medida corresponda;
- paginación acotada (`Page >= 1`, `1 <= PageSize <= 100`).

Los filtros son restrictivos y nunca amplían el data-scope/RBAC del usuario.

## Agrupaciones autorizadas

- `Proveedor`.
- `Producto` / variante cuando el dato de línea exista.
- `OrdenCompra` para trazabilidad.
- Período temporal únicamente con buckets explícitos de fecha; no inventar calendarios fiscales.

## Ausencia de dato

- Orden sin factura: precio facturado/variación = `null`, no cero.
- Orden sin recepción/evaluación: cumplimiento factual = `null`, no “incumplido”.
- Sin devolución: total/cantidad de devoluciones puede ser cero únicamente después de consultar la autoridad de devoluciones para el filtro; ausencia de relación no se infiere desde cancelaciones.
- Snapshots vacíos no se sustituyen con maestros actuales para reescribir historia.

## Seguridad e invariantes

- Endpoints futuros: `[Authorize]` + permiso relacional `Compras/Ver`.
- Read-only: el reporte no muta orden, recepción, factura, devolución, evaluación, inventario, Kardex ni CxP.
- No exponer idempotency keys/fingerprints, motivos sensibles no requeridos ni payloads de auditoría completos.
- Auditoría/correlation de lectura se añade en N5.5.F conforme al patrón canónico, sin datos sensibles.

## Contrato para N5.5.C/D

N5.5.C debe comprobar si las proyecciones pueden resolverse desde persistencia actual. Si sí, debe cerrar `N/A` sin tabla, snapshot ni migración redundante. N5.5.D implementará DTO/query/service/API exactamente sobre las medidas autorizadas arriba, sin introducir porcentaje de variación, ranking ni score de cumplimiento.

## Pruebas obligatorias posteriores

- relación explícita factura↔línea de orden para variación absoluta;
- signo de variación positiva/negativa y `null` sin comparación válida;
- recepción parcial y cantidades factualizadas;
- devolución confirmada frente a anulada;
- desviación temporal antes/en/después de fecha esperada;
- filtros inválidos fail-closed;
- `Compras/Ver` y ausencia de mutaciones.

## Resultado

`N5.5.B` fija un contrato pequeño, source-backed y verificable. No requiere cambio de dominio persistente: las entidades actuales ya contienen las autoridades necesarias; las métricas no respaldadas permanecen explícitamente fuera de alcance.
