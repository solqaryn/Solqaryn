# N3.9.C — Cuentas por cobrar — Persistencia, migración y datos VAEP v3.25.1

## Disposición

**N/A CERTIFIED FOR NEW CxC PERSISTENCE.** N3.9.B fijó `CuentaPorCobrar` como read-model/proyección de la autoridad existente `Factura` + `FacturaPago`, no como un aggregate/ledger mutable separado. Por tanto N3.9.C no debe crear una tabla, migración, snapshot o backfill `CuentaPorCobrar` que duplique saldo.

## Evidencia persistente actual

- `FacturaConfiguration` persiste `Factura` en `Facturas`, conserva número único y las propiedades del aggregate; `Factura` sigue siendo la autoridad de Total/TotalPagado/SaldoPendiente/vencimiento/moneda/condición.
- `FacturaPagoConfiguration` persiste pagos en `FacturaPagos`, usa precisión `18,2` para montos, índice `(FacturaId, FechaPago)` y FK `FacturaId -> Factura` con `DeleteBehavior.Restrict`.
- La colección `Factura.Pagos` ya vincula la deuda con su evidencia de pagos sin requerir un ledger duplicado.

## Regla de no duplicación

No se autoriza:

- tabla `CuentasPorCobrar` que replique `Factura.SaldoPendiente`;
- backfill de saldos a un segundo source of truth;
- eventos/ledger de cargos/créditos no definidos por N3.9.B;
- persistencia de aging/mora, anticipos, allocation multi-factura o aplicación de NotaCreditoCliente sin contrato previo.

## Optimización futura permitida

Índices, vistas/query-models o materialización técnica solo podrán proponerse si existe evidencia de necesidad y si son derivables/reconstruibles desde Factura/FacturaPago sin convertirse en autoridad mutable paralela. Cualquier materialización persistente futura requiere reconciliación, data-safety y rollback explícitos.

## Rollback

No hay schema/migración nueva en N3.9.C; por tanto no existe rollback DB nuevo que ejecutar. Reabrir persistencia CxC requiere primero una nueva decisión de dominio que modifique N3.9.B.

## DoD

- persistencia autoridad Factura/FacturaPago verificada;
- nueva tabla/migración CxC clasificada N/A bajo el contrato vigente;
- duplicación de saldo prohibida explícitamente;
- riesgos futuros mantenidos fail-closed;
- P0/P1 introducido: **0 conocidos**.

**N3.9.C puede cerrarse LISTO_REAL / N_A_CERTIFIED.** El siguiente parent N3.9.D puede implementar únicamente una superficie de aplicación/consulta coherente con el read-model autorizado, sin CRUD de aggregate inexistente.
