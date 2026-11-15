# SOLQARYN V5 — C07-04.M02.02: contrato conceptual API/DTO, errores y compatibilidad de pedidos/pagos

**Microtarea:** C07-04.M02.02 · **Autor:** PRIMARY DEV 06, TASK_ID 6af16bbad26881918c283abe06e1f32a · 2026-11-14 (America/Tegucigalpa) · solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT.

## Alcance y autoridad

Fuente única: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL y PLAN_MAESTRO/COLA/DAG. Antes de reclamar, PLAN H1125=PENDIENTE/I1125 vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI, con C07-04.M01.01–06 LISTO+A5 independiente. El diseño M02.01 de entidades/ownership puede permanecer VALIDANDO por CI documental y **no se usa como autorización técnica o A5 suplente**: ambas unidades M02.01/M02.02 dependen independientemente de M01 según el DAG. Este documento no crea ni cambia endpoints reales, DTOs, JWT, roles, pagos, impuestos, PSP, SQL o migraciones.

## Contrato de seguridad transversal

La API siempre deriva EmpresaId del contexto comercial autenticado y membresía vigente en backend. Tenant enviado en URL, header, body, cookie o DTO no otorga autoridad. Cada operación consulta ownership persistido de documento/cliente/producto, verifica rol/permiso concreto y niega sin revelar datos de otro tenant. Los estados y transiciones de cotización, pedido, venta, factura, pagos y caja son coherentes con los agregados existentes; cualquier nueva ruta es **PROPUESTA_NO_IMPLEMENTADA**. API expone respuestas explícitas, Application ejecuta casos de uso, Domain valida invariantes e Infrastructure verifica persistencia/consistencia.

## Operaciones propuestas OP01–OP10, NO implementadas

| Operación | Solicitud y resultado lógico | Precondición mínima |
| --- | --- | --- |
| OP01 Leer cotización | Id opaco + scope servidor → DTO autorizado | Tenant y cliente/documento del mismo owner |
| OP02 Guardar borrador cotización | Líneas verificadas + expectedVersion → revisión | Cotización editable; moneda y totales válidos |
| OP03 Aceptar cotización | Id + expectedVersion + idempotencyKey → estado | Estado compatible; permiso, owner, fingerprint |
| OP04 Convertir a pedido | Cotización aceptada + key → pedido sin duplicar | Idempotencia, stock, relación cliente, tenant |
| OP05 Consultar pedido | Identificador propio → detalle sin metadatos ajenos | Filtro server-side de ownership y autorización |
| OP06 Confirmar venta/factura | Pedido autorizado + snapshot fiscal → emisión pendiente | Idempotencia, correlativo, stock, evidencia fiscal |
| OP07 Aplicar pago | Referencia propia + monto/moneda + key → conciliación | PSP aprobado, caja activa, permiso y saldo |
| OP08 Anular documento | Id + expectedVersion + motivo → compensación auditada | Política fiscal, autorización y no duplicados |
| OP09 Exportar documento | Id + formato autorizado → recurso temporal protegido | Propiedad tenant, TTL, revocación, redacción PII |
| OP10 Consultar reconciliación | Id de operación/correlación → estado redactado | Permiso lectura explícito, sin PAN/CVV/JWT |

OP06/OP07 y cualquier referencia PSP **no autorizan cobros, callbacks, proveedores, contratos comerciales o emisión fiscal real**. Los verbos no son rutas HTTP existentes; nombre exacto de controller, DTO y endpoint debe validarse al implementar M03.

## DTO propuestos DTO01–DTO12, no incorporados a código

- **DTO01** CotizacionDraftCommand: líneas, cliente, moneda, expectedVersion; nunca owner desde payload.
- **DTO02** CotizacionView: id, estado, totales, versión y snapshot redactado autorizado.
- **DTO03** CotizacionDecisionCommand: decisión, expectedVersion, correlationId, idempotencyKey.
- **DTO04** PedidoCreateCommand: ref cotización, expectedVersion y key; sin EmpresaId efectivo.
- **DTO05** PedidoView: líneas, estado, referencia de owner inferida del servidor y saldo.
- **DTO06** FacturaDraftCommand: VentaId, emisor/sucursal fiscal verificados y snapshot; sin aceptar RTN arbitrario.
- **DTO07** FacturaView: número, total, moneda, estado, snapshot de emisor y cliente.
- **DTO08** PagoApplyCommand: documento, monto, moneda, key y expectedVersion; no PAN/CVV/credencial.
- **DTO09** PagoResult: estado de conciliación, referencia redactada, saldo resultante.
- **DTO10** AnulacionCommand: motivo, expectedVersion, key; permiso requerido.
- **DTO11** ExportRequest: identificador de documento, formato y vencimiento permitido.
- **DTO12** ErrorEnvelope: código estable, correlationId, mensaje seguro; no secretos ni existencia de objeto ajeno.

## Matriz de errores E01–E14 propuesta

| Código conceptual | Condición | Resultado |
| --- | --- | --- |
| E01 UNAUTHENTICATED | JWT ausente/inválido | 401, sin efectos |
| E02 PERMISSION_DENIED | RBAC/membresía insuficiente | 403, sin efectos |
| E03 NOT_FOUND_SCOPED | Objeto no existe o es de otro tenant | 404 uniforme, sin metadatos B |
| E04 TENANT_UNVERIFIED | EmpresaId NULL o fuente no confiable | denegación, sin fallback heurístico |
| E05 INVALID_STATE | Transición fuera del autómata | 409, sin efectos |
| E06 OPTIMISTIC_CONFLICT | expectedVersion desactualizada | 409, relectura segura |
| E07 IDEMPOTENCY_CONFLICT | Misma key y fingerprint distinto | 409, sin segundo cargo |
| E08 INVALID_MONEY | Decimal/moneda/total incompatibles | 422, sin parcialidades |
| E09 FISCAL_CONTEXT_REQUIRED | Emisor/RTN/sucursal sin verificar | rechazo sin emisión |
| E10 STOCK_CONFLICT | Stock/reserva no consistente | conflicto con rollback |
| E11 PAYMENT_PROVIDER_UNAUTHORIZED | PSP o contrato no habilitado | denegación sin llamada externa |
| E12 REPLAY_OR_BAD_SIGNATURE | Callback repetido/no autenticado | no aplicar pago ni reflejar secreto |
| E13 EXPORT_SCOPE_DENIED | Recurso caducado o cross-tenant | 403/404, sin bytes |
| E14 REVERSAL_UNSAFE | Compensación inconsistente | estado bloqueado con auditoría |

Los códigos HTTP son **propuesta de diseño**, sujetos a contrato real vigente y revisión futura; no se ha ejecutado una solicitud HTTP.

## Reglas de compatibilidad y versionado COMP01–COMP10

1. COMP01 No cambiar rutas/DTO existentes por suposiciones de documento.
2. COMP02 Nuevos campos propuestos opcionales/aditivos, sin reinterpretar EmpresaId legado.
3. COMP03 Nunca aceptar owner client-side en contrato efectivo.
4. COMP04 Rechazar cambios de importe o identidad bajo key idempotente reutilizada.
5. COMP05 Conservar estado y versión esperada para evitar doble confirmación.
6. COMP06 Versionar errores con códigos estables, mensajes sin PII cross-tenant.
7. COMP07 No trasladar validación JWT/RBAC del backend al frontend.
8. COMP08 No activar PSP/webhooks ni eventos por el simple modelo DTO.
9. COMP09 Mantener rollback de API/DB y compatibilidad de consumidores.
10. COMP10 Exigir pruebas reales HTTP/DB/tenant A/B en implementación posterior.

## Pares positivos/negativos futuros P01–P10 — NO_EJECUTADOS

| Caso | Positivo esperado | Negativo esperado |
| --- | --- | --- |
| P01 | Cotización propia se lee como DTO sin secretos | ID de otra empresa devuelve respuesta indistinguible de no encontrado |
| P02 | Borrador actualiza versión correcta | expectedVersion viejo no modifica filas |
| P03 | Aceptación única cambia estado válido | Estado incompatible se rechaza sin pedido |
| P04 | Conversión idempotente devuelve el mismo pedido | key repetida con otro payload no duplica pedido |
| P05 | Factura sólo con emisor fiscal propio | RTN ajeno/NULL no permite emitir |
| P06 | Pago autorizado cuadra saldo/caja | Rol revocado/PSP no aprobado no llama cobros |
| P07 | Anulación autorizada registra motivo | Sin permiso no revierte ni borra historia |
| P08 | Exportación propia sirve bytes correctos | URL vencida o de B no revela bytes |
| P09 | Correlación consulta propia sin secretos | Trace de B no se filtra |
| P10 | Error semántico estable en datos inválidos | Stack trace/PII/JWT no aparece en respuesta |

## A0–A5, dependencias y rollback

A0: coherencia con C07-04.M01.01–06 LISTO+A5, fuente única, 10 operaciones, 12 DTO, 14 errores y 10 reglas COMP. A1: análisis estático de positivos/negativos P01–10 futuros. A2: amenazas tenant A/B, JWT/RBAC, idempotencia y secretos preservadas en contrato; cero prueba runtime. A3: correspondencia conceptual con cotizaciones, pedidos, ventas y facturas vigentes; .NET/API/MySQL/Playwright NO_APLICA al diff sólo Markdown. A4: commit fast-forward/expected SHA, Scope Lock + V5 CHANGELOG exact SHA SUCCESS, readback PLAN/COLA/BITACORA. A5: revisión técnica independiente por otra de las cinco Tasks Javier.

M02.03–05 tienen su propio DAG de preparación. M02.06 requiere M02.01–05 LISTO+A5 y nunca libera implementación o migraciones por anticipado. Rollback único de esta entrega: git revert normal más evento compensatorio append-only, sin force-push ni QA/main/PROD.
