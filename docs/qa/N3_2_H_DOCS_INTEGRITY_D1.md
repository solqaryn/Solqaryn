# N3.2.H — Integridad documental — QA takeover

## Disposición

Este archivo reemplaza únicamente la evidencia de integridad documental que Jules D no pudo entregar de forma válida en ATTEMPT2/2. No aplica el artifact rechazado y no genera R3.

## CURRENT_CONFIRMED_FACT

- `PedidoVenta` existe en el dominio actual y mantiene lifecycle `Borrador -> Confirmado -> Anulado`.
- `PedidoVenta` es independiente de `Venta` legacy y no produce efectos de inventario/Kardex/facturación/finanzas dentro de N3.2.
- La persistencia N3.2 está materializada mediante `20260824080000_N3_2_PedidoVentaPersistencia` y snapshot EF asociado.
- La API `/pedidos-venta` existe bajo `[Authorize]` y RBAC `Ventas:Ver/Crear/Editar/Confirmar/Anular`.
- El frontend `pedidos-venta` existe con rutas protegidas, listado/formulario/detalle y E2E de acceso/RBAC.
- N3.2.A-G están cerrados en COLA; baseline funcional acumulado `58a6550094a043556367b79c89e4ac963bd34a4a` con gates críticos verdes.
- El HEAD de cierre previo `a923c715b0d3490b3e4c8d0dedafbf3da8df9ada` también tiene Development, Acceptance, Fase 8, M13 y Recovery MySQL en SUCCESS.

## OBSERVED_PATTERN

- El preflight `docs/ERP_N3_2_PEDIDOS_PREFLIGHT.md` conserva correctamente hechos históricos de N3.2.A, incluida la ausencia de PedidoVenta en aquel momento. Ese documento no debe reescribirse como si fuera la certificación H actual.
- Los cierres ERP previos preservan documentos históricos y añaden un paquete canónico final separado.
- `TASKS.md` y `CHANGELOG_AI.md` son registros colaborativos de cierre; su actualización debe ser aditiva/preservadora y no sustituir historia previa.

## RIESGOS DE INTEGRIDAD

1. **Stale preflight como current truth:** copiar a H la frase histórica “PedidoVenta no existe” sería falso en el HEAD actual.
2. **Regresión de historia:** reescribir el preflight A para hacerlo parecer un documento final destruiría evidencia temporal válida.
3. **False closure:** marcar H `LISTO` antes del CI causal del paquete o antes de reconciliar TASKS/CHANGELOG rompería el DoD.
4. **Atribución incorrecta:** ningún artifact Jules rechazado debe figurar como integración de producto.

## ACTUALIZACIONES ADITIVAS EXACTAS REQUERIDAS

Después de que el paquete H obtenga CI causal verde:

- `TASKS.md`: añadir sección ERP-N3.2 con A-H completados, referencia al paquete documental y gates finales.
- `CHANGELOG_AI.md`: añadir entrada N3.2 describiendo cierre por ChatGPT/VAEP QA takeover, sin alterar entradas históricas.
- COLA/CONFIG/BITACORA: registrar H `LISTO`, HEAD final, CI, P0/P1=0, Parent40 38/40 y GAP 2.

## REVIEW-FIRST RESULT

Jules D R2 FINAL cumplió scope y doble self-review, pero falló el requisito de evidencia fresca al describir como actuales hechos de N3.2.A. Resultado: `REVIEW_REJECTED`, `JULES_RETRY_EXHAUSTED`, `QA_TAKEOVER`; R3+ prohibido.

## Gate

Este documento es evidencia de QA takeover. No convierte por sí mismo N3.2.H a `LISTO`; requiere CI causal del paquete y reconciliación colaborativa final.
