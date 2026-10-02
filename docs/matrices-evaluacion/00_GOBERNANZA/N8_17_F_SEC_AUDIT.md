# N8.17.F — SEC_AUDIT contracts

Estado de trabajo: `REVIEW_FIRST / SEC_AUDIT`.

Base: `N8_17_A_BATCH_MANIFEST.json` + contratos N8.17.B/C/D/E + `N8_15_F_SEC_AUDIT_INVENTARIO.md`.

## Contrato global obligatorio

- **View/Edit/Action:** cada operación protegida requiere autorización server-side independiente; `authGuard`/`permisoGuard` son sólo UX/navigation y nunca conceden autoridad.
- **Tenant:** empresa/tenant se resuelve desde contexto autenticado del backend; un tenant/id enviado por cliente no constituye autoridad.
- **Sensitive data:** DTOs/logs/auditoría excluyen credenciales, `Authorization`, tokens y valores secretos. PII/finanzas se exponen sólo al capability requerido.
- **Auditoría:** cambios de seguridad, lifecycle y mutaciones materiales generan metadata de actor/tenant/acción/resultado sin secretos.
- **Tamper/cross-tenant:** todo ID/referencia se re-resuelve server-side y se verifica ownership; un ID válido de otro tenant debe fallar cerrado.
- **Fail-closed:** authz/tenant/source/lifecycle ambiguo o inválido => denial sin mutación parcial.

## Matriz 49/49

| MATRIX_ID | View/Edit/Action | Sensitive data | Tamper / cross-tenant / audit focus |
|---|---|---|---|
| `VAEP-MX::IDENTITY_ACCESS::LOGIN` | public auth entry | credentials/session metadata | generic-denial + token/credential non-disclosure; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::IDENTITY_ACCESS::PERFIL` | self or authorized admin | user PII | protected identity/security fields cannot be changed through profile; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::IDENTITY_ACCESS::PERMISOS` | security admin | authorization policy | self-escalation/unknown target denied; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::IDENTITY_ACCESS::ROLES` | security admin | authorization policy | role composition cannot exceed caller authority; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::IDENTITY_ACCESS::USUARIOS` | user admin | user PII/account lifecycle | credential/security material excluded from ordinary profile writes; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::PRODUCT_CATALOG::CATALOGOS_PRODUCTO` | catalog view/edit/action | tenant catalog data | foreign IDs/references; cross-tenant catalog ownership; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::PRODUCT_CATALOG::CATEGORIAS` | catalog view/edit/action | tenant catalog data | foreign IDs/references; cross-tenant catalog ownership; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::PRODUCT_CATALOG::PRODUCTOS` | catalog view/edit/action | tenant catalog data | foreign IDs/references; cross-tenant catalog ownership; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::CUSTOMERS_COMMERCIAL::CLIENTES` | commercial auth | customer PII/credit | customer/credit visibility and mutation scoped by tenant; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::CUSTOMERS_COMMERCIAL::COSTOS_ENVIO` | commercial/billing by operation | customer PII + commercial/financial data | totals/status/IDs; cross-tenant customer/document ownership; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::CUSTOMERS_COMMERCIAL::COTIZACIONES` | commercial/billing by operation | customer PII + commercial/financial data | totals/status/IDs; cross-tenant customer/document ownership; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::CUSTOMERS_COMMERCIAL::DESCUENTOS` | commercial/billing by operation | customer PII + commercial/financial data | totals/status/IDs; cross-tenant customer/document ownership; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::CUSTOMERS_COMMERCIAL::FACTURAS` | billing auth | customer PII/invoice/payment | server totals/tax/status; public links never grant mutation authority; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::CUSTOMERS_COMMERCIAL::PEDIDOS_VENTA` | commercial/billing by operation | customer PII + commercial/financial data | totals/status/IDs; cross-tenant customer/document ownership; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::CUSTOMERS_COMMERCIAL::TIPO_CLIENTES` | commercial/billing by operation | customer PII + commercial/financial data | totals/status/IDs; cross-tenant customer/document ownership; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::CUSTOMERS_COMMERCIAL::STOREFRONT` | public catalog + protected checkout | public catalog; checkout customer/order/payment metadata | public UI never bypasses backend order/payment/tenant controls; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::CUSTOMERS_COMMERCIAL::VENTAS` | commercial/billing by operation | customer PII + commercial/financial data | totals/status/IDs; cross-tenant customer/document ownership; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::PURCHASES_SUPPLIERS::COMPRAS` | purchasing by operation | supplier/contact + purchase data | qty/price/status/IDs; cross-tenant supplier/document ownership; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::PURCHASES_SUPPLIERS::ORDENES_COMPRA` | purchasing by operation | supplier/contact + purchase data | qty/price/status/IDs; cross-tenant supplier/document ownership; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::PURCHASES_SUPPLIERS::PROVEEDORES` | purchasing by operation | supplier/contact + purchase data | qty/price/status/IDs; cross-tenant supplier/document ownership; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::PURCHASES_SUPPLIERS::RECEPCIONES_COMPRA` | purchasing by operation | supplier/contact + purchase data | qty/price/status/IDs; cross-tenant supplier/document ownership; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::PURCHASES_SUPPLIERS::SOLICITUDES_COMPRA` | purchasing by operation | supplier/contact + purchase data | qty/price/status/IDs; cross-tenant supplier/document ownership; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::INVENTARIO_LOGISTICA::ALMACENES` | inventario/logistics by operation | stock/location/lot/serial | stock/location/lot/serial tamper; cross-tenant warehouse ownership; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::INVENTARIO_LOGISTICA::INVENTARIO` | inventario auth | stock/location/lot/serial | authoritative stock cannot be set from client balance; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::INVENTARIO_LOGISTICA::PREPARACIONES_PEDIDO_VENTA` | inventario/logistics by operation | stock/location/lot/serial | stock/location/lot/serial tamper; cross-tenant warehouse ownership; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::INVENTARIO_LOGISTICA::SUCURSALES` | inventario/logistics by operation | stock/location/lot/serial | stock/location/lot/serial tamper; cross-tenant warehouse ownership; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::INVENTARIO_LOGISTICA::UBICACIONES_ALMACEN` | inventario/logistics by operation | stock/location/lot/serial | stock/location/lot/serial tamper; cross-tenant warehouse ownership; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::CASH_BANKS::CAJA` | restricted treasury | cash/session/movement | session state + amount validated server-side; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::CASH_BANKS::CUENTAS_BANCARIAS` | restricted treasury | bank-account metadata | mask when full value not required; server ownership enforced; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::CASH_BANKS::METODOS_PAGO` | restricted treasury by operation | cash/bank/payment data | amount/session/account tamper; cross-tenant treasury ownership; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::FINANCE_ACCOUNTING::ASIENTOS_CONTABLES` | restricted accounting | ledger entries | balanced journal/period/authority validated server-side; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::FINANCE_ACCOUNTING::CENTROS_COSTO` | restricted finance/accounting | ledger/receivable/tax/financial data | period/amount/account/source tamper; cross-tenant financial scope; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::FINANCE_ACCOUNTING::CUENTAS_POR_COBRAR` | restricted finance | customer receivable data | derived balance cannot be client-overridden; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::FINANCE_ACCOUNTING::ESTADOS_FINANCIEROS` | restricted finance/report | financial statements | read model cannot broaden source authorization; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::FINANCE_ACCOUNTING::FINANZAS` | restricted finance | financial operations/bank/payment | orchestration inherits underlying aggregate authorization; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::FINANCE_ACCOUNTING::IMPUESTOS` | restricted finance/accounting | ledger/receivable/tax/financial data | period/amount/account/source tamper; cross-tenant financial scope; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::FINANCE_ACCOUNTING::PERIODOS_CONTABLES` | restricted finance/accounting | ledger/receivable/tax/financial data | period/amount/account/source tamper; cross-tenant financial scope; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::FINANCE_ACCOUNTING::PLAN_CUENTAS` | restricted finance/accounting | ledger/receivable/tax/financial data | period/amount/account/source tamper; cross-tenant financial scope; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::BI_REPORTING::CENTRO_REPORTES` | report capability + source authorization | derived commercial/inventario/financial/audit data | filter/source tamper; cross-tenant report scope; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::BI_REPORTING::DASHBOARD` | report capability + source authorization | derived commercial/inventario/financial/audit data | filter/source tamper; cross-tenant report scope; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::BI_REPORTING::RENTABILIDAD` | report capability + source authorization | derived commercial/inventario/financial/audit data | filter/source tamper; cross-tenant report scope; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::BI_REPORTING::REPORTES_ADMINISTRATIVOS` | report capability + source authorization | derived commercial/inventario/financial/audit data | filter/source tamper; cross-tenant report scope; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::BI_REPORTING::REPORTES_COMPRAS` | report capability + source authorization | derived commercial/inventario/financial/audit data | filter/source tamper; cross-tenant report scope; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::BI_REPORTING::REPORTES_INVENTARIO` | report capability + source authorization | derived commercial/inventario/financial/audit data | filter/source tamper; cross-tenant report scope; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::BI_REPORTING::REPORTES_VENTAS` | report capability + source authorization | derived commercial/inventario/financial/audit data | filter/source tamper; cross-tenant report scope; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL` | authenticated navigation | identity/navigation context | menu/route visibility is UX only; backend grants authority; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::GOV_CONFIG_INTEGRATIONS::AUDITORIA` | governance restricted | audit actor/event metadata | append-only source; no client edit; tokens/secrets excluded; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::GOV_CONFIG_INTEGRATIONS::CARGAS_MASIVAS` | admin restricted | uploaded business data may include PII | server parse/validate; reject tenant spoofing/partial unauthorized writes; auditar mutaciones/lifecycle relevantes; fail-closed |
| `VAEP-MX::GOV_CONFIG_INTEGRATIONS::CONFIGURACION` | admin restricted | configuration metadata; secret-key names only | secret values never returned/logged/committed; reject forged global scope; auditar mutaciones/lifecycle relevantes; fail-closed |

## Pruebas dirigidas / reconciliación

- frozen manifest count: `49`.
- matrix IDs únicos: `49`.
- faltantes respecto al manifest: `0`.
- duplicados: `0`.
- perfiles sin contrato de seguridad: `0`.
- `UNKNOWN` de seguridad que bloquee SPEC_COMPLETE: `0`.
- búsqueda documental de secretos en este artefacto: `0` valores secretos; sólo nombres/clases de datos.
- frontera de seguridad preservada: backend JWT/RBAC/tenant; frontend guard sólo UX.

## REVIEW_FIRST

- P0: `0`.
- P1: `0`.
- P2: `0`.
- No se detectó bypass demostrable nuevo en el alcance documental/contractual N8.17.F.
- No se modificó runtime, schema, deploy, secretos ni Producción.

## Resultado candidato

`N8.17.F` puede certificarse `LISTO_REAL` cuando el gate causal de este HEAD confirme la integridad estructural del artefacto y el readback/receipt sea persistido.
