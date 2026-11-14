# Certificación N3.5 — Venta/factura

## Dictamen

ERP-N3.5 queda funcionalmente completado con N3.5.A–G certificados y N3.5.H dedicado exclusivamente al cierre documental y de evidencia. Esta certificación no reabre ni modifica código funcional.

## Alcance certificado

El análisis dirigido confirma que `Venta`, `Factura` y `PedidoVenta` ya se encuentran desacoplados en el alcance requerido por N3.5. `Venta` conserva la autoridad operativa/financiera del flujo de venta que confirma stock, Kardex y movimiento financiero; `Factura` permanece ligada a `Venta` mediante `VentaId`; `PedidoVenta` conserva identidad y lifecycle propios sin convertirse implícitamente en una venta ni ejecutar por sí mismo efectos de facturación o finanzas.

N3.5 no introdujo una nueva conversión automática `PedidoVenta → Venta`, una nueva FK cross-document, una segunda autoridad de stock ni un contrato financiero inventado. La ausencia de delta fue tratada explícitamente como N/A con evidencia en las microtareas B–G, en lugar de crear código, migraciones o pruebas artificiales.

## Evidencia funcional

- Baseline funcional certificado previo: `a167434880eab07c3b08ca651ae9309da964c23b`.
- Gate M13 causal del baseline: `#32809392404` SUCCESS.
- N3.5.A preflight: LISTO; Issue `#516` cerrado.
- N3.5.B dominio/contratos: LISTO N/A grounded; Issue `#517` cerrado.
- N3.5.C persistencia/migración/datos: LISTO N/A grounded; Issue `#518` cerrado; delta EF/migración requerido=0.
- N3.5.D Application/API: LISTO N/A grounded; Issue `#519` cerrado.
- N3.5.E frontend/UX: LISTO N/A grounded; Issue `#520` cerrado.
- N3.5.F RBAC/auditoría/seguridad/observabilidad: LISTO N/A grounded; Issue `#521` cerrado.
- N3.5.G QA/regresión/CI: LISTO N/A grounded; Issue `#522` cerrado; suites nuevas aplicables=0 bajo `CI_CAUSALITY_BUDGET`.
- P0/P1 bloqueantes conocidos atribuibles a N3.5: 0.

## Autoridad y límites

- `PedidoVenta` continúa como documento comercial previo e independiente.
- `Venta` conserva la autoridad de la operación de venta y sus efectos de inventario/finanzas existentes.
- `Factura` conserva la relación financiera/documental con `Venta` mediante `VentaId`.
- N3.5 no autoriza duplicar movimientos de stock, Kardex, facturas ni movimientos financieros al relacionar documentos.
- Cualquier futura conversión automática entre PedidoVenta y Venta deberá definir autoridad, idempotencia y reversión explícitas en una microtarea que lo requiera; no se infieren en este cierre.

## Rollback y recuperación

Como N3.5.B–G no introdujeron delta de producto, esquema ni interfaz, no existe rollback funcional específico que ejecutar. Ante una regresión atribuida a este alcance:

1. detener la promoción del parent afectado en `Desarrollo`;
2. preservar Venta, Factura, PedidoVenta, stock, Kardex y movimientos financieros existentes;
3. identificar qué componente introdujo el acoplamiento o doble efecto;
4. corregir forward-only sin borrar históricos;
5. ejecutar las pruebas y gates causales que correspondan al delta real antes de reabrir promoción.

Producción queda fuera de alcance.

## DoD de cierre

- N3.5.A–G: LISTO en COLA.
- Certificación H publicada en `Desarrollo`.
- `TASKS.md` debe reconciliar el cierre formal preservando historial.
- `CHANGELOG_AI.md` debe registrar la entrada de cierre preservando historial.
- P0=0 y P1=0 conocidos atribuibles al parent.
- Siguiente parent permitido por dependencias tras H: `N3.6.A — Devoluciones — Auditoría y preflight`.

**Dictamen final:** `N3.5.H = LISTO` únicamente después de que el paquete documental de H quede completamente publicado y verificado como aditivo/documental. Hasta entonces esta certificación constituye evidencia material de cierre, no autorización para false LISTO.