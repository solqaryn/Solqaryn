# N3.7.D — Security / Audit / Error Grounding

Status: REVIEW_FIRST evidence for CURRENT_PARENT N3.7.D. This document does not promote the parent and does not authorize deferred business semantics.

## Authoritative scope

N3.7.D may implement only the Application/repository/service/API/DI/RBAC/error surface supported by the certified N3.7.B domain and N3.7.C persistence. Lifecycle fiscal, fiscal numbering, accounting/balance application, idempotency/cardinality, physical return, stock, Kardex and cash effects remain fail-closed unless a current contract explicitly authorizes them.

## Grounded domain boundary

`NotaCreditoCliente.CrearDesdeFactura` requires a persisted invoice with a valid sale, rejects Borrador/Anulada/Cancelada invoices, inherits and normalizes the invoice currency, requires a positive credit amount not greater than the invoice total, and requires a non-empty reason. The entity currently exposes no fiscal lifecycle, numbering, accounting application, cancellation or inventario mutation contract.

Therefore the N3.7.D application/API layer MUST NOT manufacture Confirmar/Registrar/Anular, fiscal-number assignment, balance posting, idempotency, cumulative-cardinality, return, stock/Kardex or cash behavior merely by analogy with `NotaCreditoProveedorService` or `DevolucionClienteService`.

## Security and error requirements

1. Mutating operations require an authenticated current user; unresolved identity must fail closed rather than creating an unaudited mutation.
2. RBAC must use an existing explicit permission supported by the current permission model. If no specific NotaCreditoCliente permission exists, do not silently widen access and do not invent a new permission in this parent without a grounded contract.
3. `FacturaId <= 0`, `MontoCredito <= 0`, blank `Motivo`, ineligible invoice state, invalid invoice sale linkage, invalid currency and credit greater than invoice total must map to deterministic 4xx business-validation behavior, never 500 by avoidable argument/domain leakage.
4. Missing source invoice must map to the repository/application not-found contract; it must not be treated as an empty successful result during creation.
5. Any successful mutation must write strict audit evidence after persistence inside the established transaction boundary. Audit failure must not yield a falsely successful API response.
6. API responses must not expose stack traces, database provider details, connection strings, SQL, secrets or internal exception text.
7. Read paths may return DTOs only; do not serialize EF navigation graphs such as `Factura` directly.
8. Do not claim idempotency, concurrency/cardinality safety or cumulative-credit enforcement until those contracts are explicitly authorized and tested. Absence of those guarantees is a closure blocker only if N3.7.D DoD/contract makes them mandatory; otherwise they remain DECISION_PENDING, not silently implemented.

## Concrete implementation acceptance matrix

| Surface | Required for N3.7.D | Fail-closed acceptance |
| --- | --- | --- |
| Application DTO/request/response | Yes | Contains only FacturaId, VentaId/read-only, Moneda/read-only, MontoCredito, Motivo, Observaciones and persisted audit identifiers/timestamps already available from the base entity. |
| Repository | Yes | Get by id + create/persist primitives needed by the authorized service; no invented fiscal/idempotency semantics. |
| Service | Yes | Loads invoice, invokes `NotaCreditoCliente.CrearDesdeFactura`, persists transactionally, maps known domain validation to business errors, records strict audit. |
| API | Yes | Authenticated/RBAC-protected create/read routes using existing error middleware/contracts. No invented lifecycle endpoints. |
| DI | Yes | Repository/service registration resolves at startup. |
| Directed tests | Yes | Auth/RBAC denial, invoice not found, invalid invoice state, amount <=0, amount > invoice total, blank reason, successful creation, audit failure/fail-closed, safe error response. |
| Deferred fiscal/accounting/inventario semantics | No | Must remain absent unless separately grounded. |

## Transport reconciliation

The Jules D recovery manifest for this scope was structurally valid and its exact-head GitHub workflow existed, but the workflow concluded `startup_failure` before any job/session was created. This is a pre-session bootstrap/transport failure: `ACTIVE_REAL=NO` and ATTEMPT1 is not consumed. A GitHub rerun was attempted and rejected by the platform with HTTP 403, so no third blind manifest is justified from the same transport state.

## Close gate

N3.7.D remains EN_PROGRESO until real implementation is present, REVIEW_FIRST is complete, directed tests pass, causal CI is terminal, and P0=0/P1=0. This evidence cannot by itself mark LISTO.