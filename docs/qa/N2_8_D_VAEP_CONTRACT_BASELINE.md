# ERP-N2.8.D — VAEP application/API contract baseline

## Authority and scope

This checkpoint opens N2.8.D only after N2.8.C persistence was causally certified on `16cafdaa7f1df7b204644c96812f46945c8520bd`. It is an implementation baseline, not a replacement for product code or tests.

## Current confirmed facts

- `CuentaPorPagar` is the authoritative aggregate for a supplier obligation.
- Payment condition is fail-closed: `Contado=1`, `Credito=2`.
- Lifecycle states are `Pendiente`, `Parcial`, `Pagada`, `Anulada`.
- Application types are `Pago`, `Anticipo`, `Retencion`, `NotaCredito`.
- `Aplicar(...)` requires a positive amount, UTC date and a normalized idempotency key; reusing a key with a different payload fails closed.
- Active applications cannot exceed the outstanding balance.
- Reversal is addressed by idempotency key and requires reason + UTC timestamp.
- An annulled account cannot receive or reverse applications; annulment requires no active applications.
- Persistence authority is `CuentasPorPagar` + `AplicacionesCuentaPorPagar`, with unique factura ownership and unique `(CuentaPorPagarId, IdempotencyKey)`.

## N2.8.D implementation target from Plan Maestro

Implement repositories/services/use-cases/DTOs/API/error mapping/pagination/filters/idempotency for supplier accounts payable. The parent must support generation of the obligation from a supplier invoice and the domain operations already modeled for partial payments, advances, withholdings and balance.

## Fail-closed rules for implementation

1. Do not invent new payment/application types or FX/tolerance rules.
2. Do not trust client-supplied supplier identity when it can be derived from persisted supplier invoice authority.
3. Preserve domain idempotency semantics end-to-end; API retries must not duplicate applications.
4. Do not bypass domain guards by updating state or balances directly in repositories/controllers.
5. Transactions must preserve aggregate + application persistence coherently; no partial write may be reported as success.
6. API authorization and permission names must be derived from current repository conventions, not invented in evidence documents.
7. ProblemDetails/error mapping must distinguish validation/conflict/not-found/authorization according to current middleware conventions.

## Critical-path implementation order

1. Inspect existing repository/service/controller patterns and current permission conventions.
2. Implement application contracts and repository access for query + mutation.
3. Implement API/controller and DI wiring.
4. Add directed tests for create/generate obligation, idempotent apply, conflicting idempotency key, over-application, reversal, annulment, pagination/filtering and authorization.
5. Run Development + Acceptance + Fase8 + M13 on one causal HEAD.

## Promotion gate

N2.8.D remains `EN_PROGRESO` until product implementation exists, directed tests are green, P0/P1=0 and Development + Acceptance + Fase8 + M13 are terminal SUCCESS on the same HEAD. Support artifacts never count as parent completion.
