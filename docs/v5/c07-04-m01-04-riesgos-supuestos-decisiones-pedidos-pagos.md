# SOLQARYN V5 — C07-04.M01.04 — Riesgos, supuestos y decisiones pendientes de pedidos, pagos y documentos

**14 noviembre 2026 · M01 preparación documental.** Contrato sujeto al STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL. Los riesgos son **hipótesis técnicas que requieren comprobación**, no vulnerabilidades constatadas. Las decisiones permanecen expresamente PENDIENTES; no crean permisos, esquemas, servicios ni integraciones.

## Fuentes verificables y alcance

- `C07-04.M01.01` — `docs/v5/c07-04-m01-01-alcance-pedidos-pagos-documentos.md`, blob `2efabd1f6b2444e605ca49cf1cce34ec4bce23d8`: NR01–NR12 de no-regresión; P01–P06/N01–N14 futuros.
- `C07-04.M01.02` — `docs/v5/c07-04-m01-02-actores-interfaces-pedidos-pagos-documentos.md`, blob `9c5c0aba4694ffbbb6a4bfacfa683330de814f1d`: 12 actores, 12 interfaces, ocho fronteras; no certifica HTTP/DB.
- `C07-04.M01.03` — `docs/v5/c07-04-m01-03-inventario-contratos-codigo-pedidos-pagos.md`, blob `654815da8a0f9b4d60c45a033eed0430bf89d317`: 12 blobs Git Domain/API y brechas G01–G10; los oráculos aún no han sido ejecutados.
- Evidencia directa limitada: `PedidoVenta.cs` (`12c265e9b3dbd6d27883d52e3b2a0b05783eb0cf`) contiene clave/fingerprint de idempotencia y `EmpresaId` nullable; `FacturaPago.cs` (`c35f5e678a2a267f8dd56fedefd24450ae36ee7b`) distingue monto aplicado, recibido y cambio; `PagoOnline.cs` (`5063b0dc667583f7403fbeec879b519b6ca216d2`) representa tenant obligatorio y referencia de proveedor.
- `FacturacionFiscalController.cs` (`6dd92895e2299feda3737a1a6eb28a3d960bcf7e`) muestra validación de scope/RTN/sucursal en el código observado, sin certificar emisión externa.

## Matriz de riesgos R01–R12 (hipótesis no verificadas)

| ID | Riesgo potencial | Control y prueba futura obligatoria |
| --- | --- | --- |
| R01 | `EmpresaId=NULL` histórico leído como global | Filtrar fail-closed, contar legados y negar lectura A/B sin mapeo. |
| R02 | Factura/pago consultado por ID de otro tenant | Validación de membresía y ownership por objeto en API, Application y repositorio. |
| R03 | Reintento de `Idempotency-Key` crea dos pedidos/cargos | Índices/deduplicación por tenant y carga concurrente con mismo/diferente fingerprint. |
| R04 | Cotización no aceptada se convierte o se convierte dos veces | Máquina de estados con rechazo y rollback idempotente. |
| R05 | Venta/pedido descuenta stock dos veces ante carreras | UnitOfWork/constraints y prueba MySQL 8.4 efímero con reservas concurrentes. |
| R06 | Pago aplicado más cambio excede total o genera saldo negativo | Invariantes de monto, conversión monetaria, snapshot y tests de concurrencia. |
| R07 | Pago o anulación sin permiso específico | Revalidar JWT, pertenencia activa y permisos `Aplicar`/`Anular` en backend. |
| R08 | Callback o referencia externa falsa se acredita | Verificar firma/autenticidad del proveedor, tenant, importe y evento único. |
| R09 | Emisión fiscal de empresa con RTN/sucursal/identidad no validada | Autorización tenant y validación en sandbox fiscal antes de respuesta; rollback. |
| R10 | Cambios de impuestos/precios sobreescriben factura histórica | Snapshots inmutables y test de no-regresión de documentos emitidos. |
| R11 | PDF/exportación/enlace público expone datos ajenos o PII | Filtros por tenant y objeto, TTL, scopes mínimos, revocación y pruebas A/B. |
| R12 | Rollback/anulación parcial deja saldo, stock y auditoría desalineados | Integración transaccional compensatoria y evidencias de error forzado. |

No declarar R01–R12 como fallos reales hasta tener ruta, evidencia reproducible y prueba causal.

## Supuestos falsables S01–S08

| ID | Supuesto de trabajo (NO CERTIFICADO) | Evidencia que lo confirma o refuta |
| --- | --- | --- |
| S01 | La separación PedidoVenta/Venta legacy evita efectos secundarios automáticos | Trace de servicio, EF y caso de confirmación positivo/negativo. |
| S02 | El `EmpresaId=NULL` histórico permanece en cuarentena | Consultas de todos los endpoints con fixture NULL y denegación. |
| S03 | El fingerprint de idempotencia se persiste con unicidad efectiva | Migraciones/índices y prueba de replay concurrente MySQL. |
| S04 | Los totales y cambio son consistentes bajo anulaciones y pagos parciales | Fixtures monetarios, saldo y rollback atómico. |
| S05 | La autorización de controlador se prolonga al repositorio/objeto | Test HTTP directo con ID de otro tenant y rol revocado. |
| S06 | El proveedor de pagos está aislado hasta configurarse explícitamente | DI/provider vacío, negativa de cobro y control de secretos. |
| S07 | La numeración y snapshots fiscales son inmutables y correctos | Ejemplos fiscales de dev/sandbox aprobados y pruebas de historia. |
| S08 | La exportación/PDF no hereda privilegios extra de rutas públicas | A/B y TTL en enlaces, caché, contenido y logs. |

Refutar un supuesto exige nueva evidencia y corrección con control de cambios, jamás alteración retroactiva de BITACORA.

## Decisiones D01–D08 sin autorización

| ID | Decisión PENDIENTE | Gate probatorio antes de resolver |
| --- | --- | --- |
| D01 | Semántica final de vínculo cotización/pedido/venta/factura | Modelo causal con estados, documentos y efectos colaterales autorizados. |
| D02 | Política de migración para EmpresaId nullable y datos históricos | Inventario reversible, ownership determinista y negativos A/B. |
| D03 | Scope/duración y retención de claves de idempotencia | Tenant, fingerprint, índice, TTL y tratamiento de conflictos. |
| D04 | Origen contable de monto pagado, vuelto, saldo y reconciliación | Reglas monetarias firmadas, casos unitarios/integración y auditoría. |
| D05 | Proveedores de pagos permitidos y política de callback | Contrato, seguridad/sandbox, coste, permisos y secretos autorizados. |
| D06 | Modelo fiscal por jurisdicción, RTN, sucursal y emisión | Normativa/negocio aprobados y pruebas sin datos productivos. |
| D07 | Compartición y exportación pública de factura y otros documentos | Scopes por cliente/tenant, expiración, PII y revocación. |
| D08 | Criterio de cierre terminal M01.06 y paso a M02/M03 | Dependencias LISTO + A5 independientes, verificaciones específicas y rollback. |

**Las ocho decisiones continúan PENDIENTES.** No activar pagos, facturación externa, DNS, producción, QA, webhooks ni permisos por inferencia.

## Oráculos positivos y negativos de aceptación FUTUROS NO_EJECUTADOS

- **P01–P06:** cotización propia aceptada convierte una sola vez; pedido crea replay consistente; factura conserva snapshots; pago parcial calcula saldo/cambio; anulación autorizada conserva historia; exportación propia permite acceso sin PII extra.
- **N01–N14:** factura B consultada desde A; ID legacy NULL publicado; conversión antes de aceptación; replay distinto fingerprint; doble reserva; confirmación sin permiso; pago negativo; cambio como ingreso; anulación sin rol; callback sin firma; duplicado providerEventId; sucursal/RTN ajenos; PDF ajeno; rollback parcial rompe saldo/stock.
- Todos los casos son especificaciones **NO_EJECUTADAS**, jamás PASS de tests xUnit, API, EF/MySQL, Playwright o integraciones. Requieren CI exact-SHA y evidencias reales en sus microtareas futuras.

## Impacto y validación de esta entrega

- **Impacto:** solamente este documento Markdown y sección autocontenida de `CHANGELOG_AI.md`; sin Domain, API, Application, Infrastructure, Angular, MySQL, EF, JWT/RBAC, pagos/SMTP/fiscal, workers, eventos, secretos, QA/main/PROD.
- **DAG:** `C07-04.M01.04` F=VALIDADO/G=SI y `COLA!G/O=SI` al claim. Es preparación M01, no gate terminal `M01.06` ni autorización de migración.
- **A0:** coherencia con tres documentos anteriores, Git blobs y alcance; **A1:** consistencia R01–R12/S01–S08/D01–D08, casos FUTUROS; **A2:** autorización y aislamiento tenant como riesgos, no pruebas ejecutadas; **A3:** runtime `NO_APLICA` porque diff Markdown; **A4:** fast-forward `dev`, Scope Lock y CHG provenance `completed/success` exact SHA más readback; **A5:** requiere revisor Javier de TASK_ID distinto.
- **Reversión:** `git revert` normal del commit documental, nuevo evento compensatorio en BITACORA; no fuerza de rama ni edición histórica.

**Resultado:** matriz de riesgos/hipótesis/decisiones disponible para planificar pruebas, sin atribuir autorizaciones, certificaciones o implementaciones inexistentes.
