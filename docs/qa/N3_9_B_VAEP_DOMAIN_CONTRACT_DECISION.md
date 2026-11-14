# N3.9.B — Cuentas por cobrar — Decisión canónica de dominio VAEP v3.25.1

## Disposición

**DOMAIN CONTRACT CERTIFIED / QA_TAKEOVER.** En el estado actual del repositorio, `CuentaPorCobrar` se define como **concepto de lectura/proyección sobre la autoridad existente `Factura` + `FacturaPago`**, no como un segundo aggregate mutable ni como una segunda fuente de verdad.

Esta decisión es deliberadamente mínima y fail-closed. No introduce todavía schema, migración, API, frontend ni nuevas políticas contables.

## Autoridad vigente

- `Factura` conserva la autoridad de `Total`, `TotalPagado`, `SaldoPendiente`, `FechaVencimiento`, `Moneda` y `CondicionPago`.
- `FacturaPago` conserva la evidencia auditable de pagos aplicados por factura.
- `NotaCreditoCliente` conserva su propio documento/importe, pero su aplicación contable sobre `Factura.SaldoPendiente` continúa **DECISION_PENDING**; N3.9.B no inventa ese efecto.
- No existe una entidad, tabla o aggregate `CuentaPorCobrar` dedicado en el repositorio actual.

## Contrato mínimo seguro

1. **Fuente de verdad:** Factura/FacturaPago permanecen como autoridad financiera del saldo por cobrar.
2. **CuentaPorCobrar:** es un read-model/query concept derivado de esa autoridad; no tiene comandos propios ni estado mutable independiente en N3.9.B.
3. **No duplicación:** N3.9.C no debe crear una tabla/ledger `CuentaPorCobrar` que replique `SaldoPendiente` salvo una decisión futura explícita que reemplace la autoridad actual y defina reconciliación/migración.
4. **Créditos:** `NotaCreditoCliente` no reduce saldo automáticamente hasta que exista un contrato de aplicación contable autorizado.
5. **Pagos:** no se autoriza distribución multi-factura, anticipos/saldos a favor ni nuevos eventos de ledger.
6. **Mora/aging:** aging buckets, late fees/mora y políticas de cobranza permanecen fuera del contrato hasta existir requerimiento explícito.
7. **Idempotencia/concurrencia:** cualquier nuevo comando futuro de aplicación de crédito/pago deberá definirla antes de persistencia; el read-model actual no crea comandos nuevos.

## Consecuencia para N3.9.C

Persistencia/migración queda **N/A para un nuevo aggregate CxC** bajo este contrato. N3.9.C debe verificar que Factura/FacturaPago ya materializan los datos requeridos por la proyección y solo podrá proponer índices/read-model técnico adicional si existe evidencia de necesidad sin duplicar la fuente de verdad.

## Consecuencia para N3.9.D/F

Application/API/RBAC no pueden inventar CRUD de un aggregate inexistente. Si el producto requiere una superficie de consulta CxC, deberá ser una consulta/proyección read-only sobre la autoridad vigente y reutilizar seguridad/auditoría coherente con el módulo que ya gobierna Factura, salvo decisión explícita posterior.

## REVIEW_DRAIN

- Jules A ATTEMPT1: sesión real/COMPLETED; **REJECTED** por base divergence y scope leak `patch.diff`; ATTEMPT1 consumido; no integrado.
- Jules B ATTEMPT1: sesión real/COMPLETED; **REJECTED** por base divergence; ATTEMPT1 consumido; evidencia útil retenida; no integrado.
- Jules D ATTEMPT1: sesión real/COMPLETED; **REJECTED / EMPTY_CHANGESET** por base mismatch; ATTEMPT1 consumido; no integrado.
- Jules C: primer dispatch sin jobs/session >5m; recovery SAME-ATTEMPT1 emitido correctamente. Ese lane no es requisito para integrar esta decisión canónica y cualquier terminal posterior se reconciliará como evidence-only/superseded si no aporta un blocker nuevo.

## DoD

- autoridad de dominio explícitamente decidida sin segunda fuente de verdad;
- decisiones no autorizadas permanecen fail-closed;
- downstream B→C→D/F tiene frontera concreta;
- ningún patch Jules inseguro integrado;
- P0/P1 funcional introducido por esta decisión: **0 conocidos**.

Con esta decisión N3.9.B puede cerrarse como dominio/contratos; no implica implementación de C/D/E/F.
