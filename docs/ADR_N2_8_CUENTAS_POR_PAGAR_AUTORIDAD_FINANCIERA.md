# ADR N2.8 — Autoridad financiera de Cuentas por Pagar

## Decisión

`FacturaProveedor` permanece como autoridad del documento comercial recibido del proveedor. `CuentaPorPagar` es la autoridad del saldo financiero exigible derivado de esa factura y `AplicacionCuentaPorPagar` registra los movimientos que reducen o revierten ese saldo.

## Consecuencias

1. Registrar una cuenta por pagar exige una `FacturaProveedor` en estado `Registrada` y es idempotente por factura.
2. La creación de CxP no recibe mercancía ni modifica stock/Kardex.
3. Los movimientos de pago/aplicación son transaccionales e idempotentes por cuenta + referencia.
4. La reversión no borra historia: registra una reversión coherente y mantiene trazabilidad.
5. Los endpoints quedan bajo autenticación y RBAC del módulo Finanzas.
6. Contabilidad general, conciliación bancaria y tesorería no se inventan dentro de N2.8; se integran en módulos posteriores mediante contratos explícitos.

## Alternativas descartadas

- Usar `FacturaProveedor` como saldo mutable: mezcla documento comercial con subledger financiero.
- Crear CxP desde recepción física: confunde recepción/stock con obligación financiera.
- Hard-delete de aplicaciones/reversiones: destruye trazabilidad.
- Rollback de esquema con datos: riesgo de pérdida; se bloquea fail-closed.

## Estado

Aceptada como decisión canónica de ERP-N2.8.
