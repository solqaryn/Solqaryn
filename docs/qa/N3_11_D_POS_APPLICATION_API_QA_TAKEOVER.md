# N3.11.D — POS — Application/API QA Takeover

## Authority

- N3.11.A preflight: LISTO_REAL. Existing `Venta` is the commercial authority; barcode lookup, Factura/FacturaPago, ReservaInventario and DevolucionCliente are reusable adjacent capabilities.
- N3.11.B domain/contracts: LISTO_REAL. No second POS aggregate or POS ledger is authorized.
- N3.11.C persistence: LISTO_REAL / N/A product delta. No POS schema, migration or snapshot is authorized while POS-specific contracts remain pending.
- Grounded API preflight #1084: existing sales surface already exposes product/barcode lookup and draft/create/calculate/update/confirm/void behavior; Factura, Reserva and Devolucion controllers provide adjacent reusable contracts with existing authentication/RBAC.

## N3.11.D decision

For the currently authorized contract, no new POS-specific Application/API product delta is grounded. N3.11.D reuses the existing Venta/Facturacion/Reserva/Devolucion application and API authorities.

The following remain DECISION_PENDING and MUST NOT be materialized by N3.11.D without a later explicit contract: cashier/session or terminal identity, atomic split/multi-tender, cash-change rules, suspended-ticket semantics, receipt/reprint policy, uniform POS idempotency, and POS-specific RBAC permissions.

## DoD result

- Repository/service/API reuse boundary: grounded and documented.
- New repository/service/DTO/controller required by the current authorized POS contract: N/A.
- New endpoint required by the current authorized POS contract: N/A.
- New POS permission/RBAC module: N/A.
- Existing auth/RBAC must remain fail-closed and unchanged.
- No product file, migration, schema, workflow, secret or deployment change is required for this child.
- P0 attributable introduced by this QA takeover: 0.
- P1 attributable introduced by this QA takeover: 0.

## Promotion guard

This document certifies only N3.11.D. It does not authorize N3.11.E+ promotion out of order and does not convert any DECISION_PENDING POS capability into an implemented contract. `WORK_CAN_PIPELINE__PROMOTION_CANNOT` remains in force.
