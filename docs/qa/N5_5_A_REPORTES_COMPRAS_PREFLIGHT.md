# N5.5.A — Reportes de compras: auditoría y preflight

Authority: `docs/VAEP_AUTHORITY.md`.

## Alcance inspeccionado

El roadmap de N5.5 pide analizar proveedor, precio, variación, recepción, devoluciones y cumplimiento. La inspección dirigida del HEAD vivo de `Desarrollo` confirma que Solqaryn ya tiene primitivas operativas suficientes para diseñar el reporte, pero no un subsistema dedicado `ReporteCompras`; el contrato debe fijarse antes de persistencia/API.

Fuentes inspeccionadas:

- `OrdenCompra` / `OrdenCompraDetalle` y sus controller/service/repository: proveedor, estado, fecha esperada, moneda, cantidades, `PrecioUnitario`, descuentos, impuestos y snapshots históricos de producto.
- `RecepcionCompra` / `RecepcionCompraDetalle` y sus controller/service/repository: vínculo a la orden, estado/fecha de recepción, cantidades recibidas/aceptadas/dañadas/faltantes/sobrantes y `CostoUnitarioSnapshot`.
- `FacturaProveedor` / `FacturaProveedorDetalle`: vínculo a proveedor y orden, estado/fechas, cantidades facturadas y `PrecioUnitarioSnapshot`, descuentos/impuestos y snapshots históricos.
- `DevolucionProveedor` / `DevolucionProveedorDetalle` y `DevolucionesProveedorController`: vínculo explícito a proveedor, orden, recepción y factura, lifecycle confirmado/anulado, cantidades devueltas, costo/impuesto snapshot y crédito derivable.
- Contratos/documentación existentes N2.2–N2.7, incluido three-way match, idempotencia, RBAC, auditoría y rollback; se usan como evidencia de semántica existente, no como autoridad operativa.

## Estado real y límites

1. **Proveedor**: source-backed por `OrdenCompra.ProveedorId`, `FacturaProveedor.ProveedorId`, `DevolucionProveedor.ProveedorId` y snapshots de nombre/documento. Es dimensión segura para reporting.
2. **Precio**: source-backed por `OrdenCompraDetalle.PrecioUnitario`, `RecepcionCompraDetalle.CostoUnitarioSnapshot` y `FacturaProveedorDetalle.PrecioUnitarioSnapshot`, además de descuentos/impuestos/totales. El reporte debe preservar snapshots; no recalcular historia con maestros actuales.
3. **Recepción**: source-backed por `RecepcionCompra`/detalles vinculados a la orden. Puede medir cantidades, estado y tiempo con datos persistidos existentes.
4. **Variación**: existen magnitudes comparables source-backed —precio ordenado, costo de recepción y precio facturado—, pero no existe todavía un contrato canónico único de “variación”. N5.5.B debe fijar qué pares compara, granularidad, denominador y tratamiento de descuentos/impuestos; no se inventa una fórmula aquí.
5. **Devoluciones**: están modeladas canónicamente por `DevolucionProveedor` y `DevolucionProveedorDetalle`; no son un gap. El reporte puede incorporar devoluciones reales usando sus vínculos a proveedor/orden/recepción/factura y sus snapshots/cantidades/crédito. Debe distinguir lifecycle confirmado de anulado y nunca sustituir devoluciones por cancelaciones/anulaciones de orden o recepción.
6. **Cumplimiento**: existen `FechaEsperadaUtc`, estados de orden/recepción, `FechaRecepcionUtc`, cantidades ordenadas/recibidas/faltantes y el three-way match como evidencia relacionada. No existe un KPI canónico único; N5.5.B debe fijar fórmula, granularidad y reglas temporales antes de persistencia/API.
7. **Seguridad**: los endpoints operativos de compras/devoluciones usan autorización y permisos relacionales del módulo de compras/proveedores según sus contratos existentes. Un reporte futuro debe conservar esos límites y no crear permisos nuevos sin evidencia/contrato autorizado.
8. **Producción/datos**: N5.5.A no requiere migración, backfill ni escritura de datos; no se toca Producción.

## Dependencias y estrategia de ejecución

- `N5.1.H` y `GATE-N4` ya están certificados; N5.4 A-H están cerrados antes de esta promoción.
- N5.5.B debe ser el siguiente parent: dominio/contratos primero. Debe definir DTO/query semantics de reporting sobre las autoridades existentes de orden, recepción, factura y devolución sin modificar esquemas si no es necesario.
- N5.5.C sólo podrá decidir persistencia/migración después de B. Si el reporte puede derivarse de datos históricos existentes, C debe documentar `N/A` con evidencia y evitar una migración artificial.
- N5.5.D implementará servicio/API únicamente tras B/C aceptados.

## Riesgos y gates

- No confundir cancelación/anulación de orden o recepción con `DevolucionProveedor`.
- No calcular variación o cumplimiento con una fórmula no autorizada.
- No perder snapshots históricos de precio/producto/proveedor.
- No mezclar moneda sin una política de conversión source-backed; no inventar FX.
- No crear escrituras o tablas de reporting si una consulta read-only satisface el contrato.
- Evitar N+1 y materialización de grafos operativos completos; preferir proyección server-side y paginación/filtros explícitos.
- Mantener RBAC y auditoría/correlation conforme a los patrones existentes cuando aplique.

## Criterios de aceptación para N5.5.B

N5.5.B debe producir un contrato source-backed y verificable para proveedor, precio, recepción y devoluciones; fijar explícitamente la semántica aún ambigua de variación y cumplimiento; preservar snapshots/moneda/lifecycle y no inventar semántica. Debe dividirse si un único changeset deja de ser coherente y verificable.

## Rollback

Este preflight es documentación/evidencia únicamente. Rollback: revertir el commit del documento en `Desarrollo`; no hay migración, cambio de datos, `main`, Producción, secrets ni merge de PR #2.

## REVIEW_FIRST disposition

`PASS_PRE_AFTER_CAUSAL_CORRECTION`: la inspección dirigida queda corregida para reconocer `DevolucionProveedor` como fuente canónica existente. Es suficiente para habilitar N5.5.B; no constituye PASS de B ni autoriza fórmulas no resueltas.
