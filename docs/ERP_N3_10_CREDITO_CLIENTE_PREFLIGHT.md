# ERP-N3.10 Crédito del cliente — Preflight canónico

**Control:** VAEP v3.25.1 Closure Governor / QA takeover.

## Dictamen

N3.10.A es un preflight de alcance y autoridad. El repositorio ya contiene capacidades relacionadas con crédito, pero **no existe todavía un contrato autoritativo de crédito comercial del cliente** que permita implementar límites, días de crédito, bloqueo automático, alertas o autorizaciones excepcionales sin decisiones adicionales. N3.10.B+ no debe inventar ese contrato.

## CURRENT_CONFIRMED_FACT

- `NotaCreditoCliente` existe como documento mínimo ligado a `Factura` y `Venta`.
- Su dominio exige factura persistida y elegible, `VentaId` válido, moneda de tres caracteres, monto positivo que no exceda el total de la factura y motivo obligatorio.
- El propio contrato de `NotaCreditoCliente` declara fuera de su alcance actual el lifecycle fiscal, numeración, aplicación contable/saldo, idempotencia, RBAC/HTTP adicional, devolución física, stock, Kardex y caja.
- La API existente `NotasCreditoClienteController` expone únicamente `GET /notas-credito-cliente/{id}` y `POST /notas-credito-cliente`, protegidos por `[Authorize]` y permisos `Ventas/Ver` y `Ventas/Crear`.
- `CuentasPorCobrarController` es una proyección **read-only** sobre `Factura`/`FacturaPago`; filtra facturas con `SaldoPendiente > 0` y excluye anuladas/canceladas. No existe un ledger CxC mutable independiente.
- No se observan campos autoritativos de límite de crédito de cliente, días de crédito, scoring, aging/mora, bloqueo comercial automático ni autorización excepcional como contrato cerrado de N3.10.

## AUTHORITY_BOUNDARIES

1. `Factura` y sus pagos continúan siendo la autoridad vigente del saldo pendiente mientras N3.10 no defina otra cosa explícitamente.
2. `NotaCreditoCliente` no debe convertirse implícitamente en un ledger de crédito comercial ni mutar `Factura.SaldoPendiente` sin contrato posterior aprobado.
3. No crear un segundo modelo mutable de CuentasPorCobrar: la superficie actual es una consulta derivada.
4. Las decisiones de límite/días/bloqueo/alertas/autorización excepcional pertenecen a N3.10.B+ y deben definir primero autoridad, lifecycle, invariantes y relación exacta con Cliente/Factura/Venta.
5. Cualquier nueva superficie HTTP/RBAC debe justificarse por el contrato de N3.10.B+; no se deduce automáticamente de `Ventas` o `Facturacion`.

## DECISION_PENDING PARA N3.10.B+

- Dónde vive la autoridad del crédito comercial del cliente y su cardinalidad.
- Unidad/moneda del límite y tratamiento multi-moneda.
- Días/plazo de crédito y fuente de la fecha de vencimiento.
- Regla exacta de disponible/utilizado y qué documentos consumen/liberan crédito.
- Semántica de bloqueo automático y condiciones de desbloqueo.
- Alertas: evento, destinatarios, severidad y deduplicación.
- Autorización excepcional: actor, permiso, trazabilidad, vigencia y límites.
- Idempotencia/concurrencia de operaciones que consuman crédito.
- Integración con `Factura`, `FacturaPago`, `Venta`, `NotaCreditoCliente` y la proyección CxC sin doble autoridad.
- Requerimientos de persistencia, migración/backfill, auditoría, observabilidad, API y frontend.

## OUT OF SCOPE / NO INVENTAR EN N3.10.A

- Crear el aggregate definitivo de crédito comercial.
- Añadir schema/migraciones, endpoints, permisos, UI o workflows de crédito.
- Introducir scoring, aging/mora o cobranza automática.
- Cambiar saldo de Factura o inventario/caja.

## Evidencia revisada

- `backend/src/Domain/Entities/NotaCreditoCliente.cs`.
- `backend/src/API/Controllers/NotasCreditoClienteController.cs`.
- `backend/src/API/Controllers/CuentasPorCobrarController.cs`.
- Jules B `N3.10.A.DATA_PREFLIGHT.B1`: PASS evidence-only, no integrado.
- Jules C `N3.10.A.API_QA_PREFLIGHT.C1`: PASS evidence-only, no integrado.
- Jules A ATTEMPT1 rechazado por evidencia stale; no integrado.
- Jules D ATTEMPT2/2 agotado con changeset vacío/base divergence; QA takeover, R3 prohibido.

## Cierre de N3.10.A

Este preflight no modifica producto ni contratos runtime. Con los hechos y decisiones pendientes anteriores, N3.10.A puede cerrarse como **LISTO** y habilitar N3.10.B, siempre manteniendo fail-closed: N3.10.B debe materializar primero el contrato de dominio de crédito comercial antes de persistencia/API/UI.
