# Certificación N4.4 — Cuentas por cobrar

## Alcance certificado

ERP-N4.4 consolida Cuentas por Cobrar sobre la autoridad financiera existente `Factura + FacturaPago`, ligada a `Venta`. No se crea un ledger CxC paralelo ni una segunda fuente de verdad.

La implementación certificada cubre:
- proyección/API CxC basada en facturas con saldo pendiente;
- frontend standalone de Cuentas por Cobrar con estados loading/empty/error;
- visualización de factura, venta, cliente, vencimiento, estado, total, pagado y saldo;
- navegación hacia factura/pagos;
- autorización `Facturacion/Ver` para lectura;
- mutaciones financieras preservadas en `FacturaService` con auditoría before/after real;
- observabilidad/correlation existente preservada;
- regresión backend/API/RBAC y frontend/lint/build producción.

## Autoridad y no duplicación

La fuente única permanece:
- `Factura` para total, saldo, vencimiento y estado financiero;
- `FacturaPago` para aplicaciones de pago;
- `Venta.EstadoPago` sincronizado por el servicio existente.

No se introduce:
- entidad `CuentaPorCobrar` mutable independiente;
- esquema/migración/backfill CxC nuevo;
- endpoints de escritura CxC paralelos;
- grants RBAC CxC nuevos cuando los contratos `Facturacion` existentes cubren el caso;
- reglas nuevas de mora, aging, scoring o contabilización no autorizadas por el plan.

## Evidencia por microtarea

- `N4.4.A` — `LISTO_REAL / PREFLIGHT_CERTIFIED`: autoridad `Factura + FacturaPago` confirmada, sin delta funcional necesario.
- `N4.4.B` — `LISTO_REAL / DOMAIN_NA_CERTIFIED`: no se requiere nuevo dominio/contrato.
- `N4.4.C` — `LISTO_REAL / DB_MIG_NA_CERTIFIED`: no existe delta de persistencia ni migración.
- `N4.4.D` — `LISTO_REAL / COVERED_EXISTING_CONTRACT`: Application/API cubiertos por `FacturasController`, `FacturaService` y `CuentasPorCobrarController`.
- `N4.4.E` — `LISTO_REAL / FRONTEND_CXC`: frontend exact-head `61c8445ff948912a1a3e7a2792106849064e51c7`, con unit/lint/build producción en SUCCESS.
- `N4.4.F` — `LISTO_REAL / SEC_AUDIT_CERTIFIED`: auditoría before/after corregida por QA takeover y RBAC/observabilidad preservados; exact-head `2487badc4759db4ca87d60f823c6fffd9899f0d2`.
- `N4.4.G` — `LISTO_REAL / TEST_CI_CERTIFIED`: exact-head funcional `a85396b8e5d6ed579f2815cb7a193f45ed3d54e0`; GitHub Actions run `33772443239` SUCCESS; backend CxC/API/RBAC, frontend canónico, lint y build producción PASS.

## P0/P1 y guardrails

En la certificación de cierre funcional de G:
- P0 abiertos atribuibles: 0.
- P1 abiertos atribuibles: 0.
- PR #2 permanece `OPEN + DRAFT`.
- `main` permanece congelada.
- Producción no fue tocada.
- no se ejecutó merge, auto-merge, force-push, creación de rama, modificación de secretos ni deploy.

## Gate documental N4.4.H

Este documento materializa la certificación canónica faltante de N4.4.H. El cierre `LISTO_REAL` de H exige además:
1. preservar historia documental existente;
2. registrar el rollup correspondiente en los colaborativos canónicos cuando proceda;
3. obtener checks exact-head aplicables terminales sobre el HEAD documental resultante;
4. revalidar P0=0/P1=0;
5. mantener PR #2 OPEN+DRAFT y `main`/Producción intactos.

Solo después de ese gate el selector puede promover el siguiente parent dependency-valid (`N4.5.A`) sin promoción prematura.
