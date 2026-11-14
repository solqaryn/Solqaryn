# Runbook N4.4 — Cuentas por cobrar

## Propósito

Guía operativa para validar y diagnosticar el módulo Cuentas por Cobrar sin crear una segunda autoridad financiera. La fuente canónica es `Factura + FacturaPago`, ligada a `Venta`.

## Contrato operativo

- Lectura CxC: `GET /cuentas-por-cobrar`.
- Lectura protegida por autenticación y permiso `Facturacion/Ver`.
- El saldo y estado provienen de `Factura`; las aplicaciones provienen de `FacturaPago`.
- La UI de CxC es una proyección operativa y no un ledger independiente.
- Las mutaciones de cobro/pago se ejecutan por los contratos financieros existentes, no por endpoints CxC paralelos.

## Verificación funcional

1. Confirmar que una factura con `SaldoPendiente > 0` aparece en CxC.
2. Confirmar que facturas anuladas/canceladas no se presenten como deuda activa cuando el contrato vigente las excluye.
3. Verificar total, total pagado y saldo pendiente contra la factura fuente.
4. Verificar navegación desde CxC hacia factura/pagos.
5. Verificar estados UI `loading`, `empty` y `error`.
6. Verificar que una respuesta 401/403 no exponga datos ni bypass de autorización.
7. Ejecutar regresión backend del controller/API/RBAC y regresión frontend canónica.
8. Ejecutar lint y build producción del frontend.

## Auditoría y observabilidad

Para mutaciones financieras soportadas por `FacturaService`:
- preservar evidencia before/after de estado, total pagado y saldo;
- no registrar datos sensibles innecesarios;
- mantener correlation/observabilidad ya establecida;
- un fallo de auditoría crítica no debe convertirse silenciosamente en una operación certificada.

## Diagnóstico de fallos

### CxC no muestra una factura esperada
- revisar `SaldoPendiente`, estado de factura y filtros del controller;
- verificar que la factura fuente exista y conserve relación con la venta;
- no crear registros CxC manuales para compensar un problema de proyección.

### Saldo incorrecto
- revisar aplicaciones `FacturaPago` y recálculo de `FacturaService`;
- verificar sincronización de `Venta.EstadoPago`;
- tratar el problema en la autoridad financiera, no mediante un ledger alterno.

### 401/403
- validar autenticación y grants `Facturacion` vigentes;
- no ampliar permisos para hacer pasar una prueba;
- comprobar que la UI y API fallen cerradas.

### Regresión frontend
- ejecutar la spec canónica CxC;
- ejecutar lint y build producción;
- corregir imports/rutas contra la estructura real del frontend, sin mocks que oculten el contrato de `environment.apiUrl`.

## Rollback

N4.4.B/C fueron certificados sin nuevo dominio ni schema, por lo que no existe migración CxC que revertir. Para cambios de UI/auditoría:
- revertir únicamente el changeset causal en `Desarrollo` si una regresión lo exige;
- preservar `Factura + FacturaPago` como autoridad;
- no tocar Producción, secretos, ramas adicionales ni `main`.

## Evidencia de referencia

- Frontend CxC certificado: `61c8445ff948912a1a3e7a2792106849064e51c7`.
- Seguridad/auditoría certificada: `2487badc4759db4ca87d60f823c6fffd9899f0d2`.
- Regresión funcional G: `a85396b8e5d6ed579f2815cb7a193f45ed3d54e0`.
- Run exacto N4.4 regression: `33772443239` SUCCESS.
- Certificación canónica: `docs/CERTIFICACION_N4_4_CUENTAS_POR_COBRAR.md`.

N4.4.H solo puede declararse `LISTO_REAL` con documentación/certificación preservada, checks exact-head aplicables terminales y P0=0/P1=0.
