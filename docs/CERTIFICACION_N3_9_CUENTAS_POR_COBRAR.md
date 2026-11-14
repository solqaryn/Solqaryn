# Certificación N3.9 — Cuentas por cobrar

## Alcance certificado

N3.9 se implementa como una proyección **read-only** sobre la autoridad existente `Factura + FacturaPago`. No se crea un ledger CxC mutable paralelo ni una segunda fuente de verdad financiera.

Contrato vigente:
- API: `GET /cuentas-por-cobrar`.
- Autorización: `[Authorize]` + `Facturacion/Ver`.
- Fuente de datos: `IFacturaService` / proyección existente de `FacturaDto`.
- Inclusión: facturas con `SaldoPendiente > 0`.
- Exclusión: estados `Anulada` y `Cancelada`.
- Orden: `FechaVencimiento` y luego `NumeroFactura`.
- La respuesta reutiliza `ApiResponse<List<FacturaDto>>`.

## Fuera de alcance deliberado

No se certifican como implementados:
- ledger mutable `CuentaPorCobrar` independiente;
- endpoints de escritura CxC;
- esquema/migración CxC nuevo;
- módulo RBAC CxC nuevo;
- reglas nuevas de aging/mora;
- anticipos, asignación contable o políticas de aplicación de pagos no existentes;
- semánticas financieras adicionales no respaldadas por `Factura + FacturaPago`.

## Evidencia de cierre

- `N3.9.E` — LISTO_REAL: `9b0db22c26bce42f42f97ba1e0c6124c54d86af9`.
- `N3.9.F` — LISTO_REAL / QA_TAKEOVER_CERTIFIED: `0d621920f8ebd0a7bb3f1b3af30ffbadd0f91f9c`, P0=0/P1=0.
- `N3.9.G` — LISTO_REAL / QA_REGRESSION_CERTIFIED: Issue #858, P0=0/P1=0.
- Control de cierre documental: Issue #859.
- Controller autoritativo: `backend/src/API/Controllers/CuentasPorCobrarController.cs`.

Los artifacts Jules de regresión que llegaron después del cierre de G se consideran evidencia auxiliar únicamente. No reabren G ni sustituyen la revisión/certificación de ChatGPT/VAEP.

## Gate de cierre N3.9.H

Este documento materializa la certificación canónica, pero `N3.9.H` solo puede promoverse a `LISTO` cuando además se cumplan simultáneamente:
1. `TASKS.md` reconciliado de forma aditiva/history-preserving.
2. `CHANGELOG_AI.md` reconciliado de forma aditiva/history-preserving.
3. Checks documentales/causales aplicables terminales.
4. P0=0 y P1=0 atribuibles a N3.9.
5. PR #2 permanece Draft y no existe merge hacia `main`.

Al cerrar H, el selector fail-closed debe promover inmediatamente `N3.10.A` como siguiente parent dependency-valid.
