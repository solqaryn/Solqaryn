# ADR N3.10.B — Autoridad de dominio para crédito comercial del cliente

## Estado

ACEPTADA para N3.10.B bajo VAEP v3.25.1.

## Contexto

`Cliente` contiene hoy identidad/contacto/tipo/activo, pero no mantiene límite, plazo, bloqueo, alertas ni excepciones de crédito. `Factura`/`FacturaPago` siguen siendo la autoridad del saldo pendiente y `CuentasPorCobrar` es una proyección read-only. El alcance N3.10 exige soportar límite, días, bloqueo automático, alertas y autorización excepcional sin crear un segundo ledger ni inventar scoring, mora o políticas financieras adicionales.

## Decisión

1. Introducir `CreditoCliente` como aggregate de configuración/estado de crédito ligado a un `ClienteId`. N3.10.C deberá hacer explícita y verificable la cardinalidad persistente de una política vigente por cliente.
2. La política guarda `Moneda`, `LimiteCredito`, `DiasCredito` y un `UmbralAlertaPorcentaje` opcional/configurable. No existe valor hardcodeado de moneda, límite, días o alerta.
3. El aggregate **no calcula** por sí mismo crédito utilizado/disponible y **no deduce** cuándo bloquear a partir de una fórmula de saldo. La fuente exacta de consumo/liberación y el disparador de bloqueo deben quedar grounded por Application N3.10.D usando las autoridades vigentes (`Factura`, pagos y contratos aprobados).
4. El dominio sí permite registrar fail-closed el resultado de una evaluación automática (`AplicarBloqueoAutomatico`/`LiberarBloqueoAutomatico`) con motivo y UTC, preservando trazabilidad sin inventar el algoritmo que dispara la evaluación.
5. La autorización excepcional exige monto positivo, vigencia UTC futura y actor responsable. El permiso RBAC exacto y el caso de uso que la concede pertenecen a N3.10.D/F.
6. `CreditoCliente` no muta `Factura.SaldoPendiente`, no crea un ledger CxC, no toca stock/Kardex/caja y no implementa interés, mora, scoring o cobranza automática.

## Consecuencias

- N3.10.C puede persistir el aggregate y sus constraints sin duplicar saldo financiero.
- N3.10.D deberá definir de forma transaccional qué saldo/compromiso consume crédito y cuándo llama a bloqueo/alerta, respaldado por pruebas.
- N3.10.F deberá definir permisos, auditoría y observabilidad de la autorización excepcional y cambios de política.
- Cualquier regla futura de multi-moneda, aging, scoring o cobro requiere una decisión separada; no se deriva de este ADR.
