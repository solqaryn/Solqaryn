# SOLQARYN V5 — C04-04.M01.01: alcance, no-alcance y no-regresión de cobros por enlace

**Autor:** SOLQARYN PRIMARY DEV 05 — JAVIER; Task `6adc2e55f47c81919f6374e561687076`. **Fecha:** 2026-11-14. **Rama:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** preparación documental M01, sin ejecución de cobros ni activación de proveedores.

## 1. Mandato y resultado delimitado

Fuente ejecutable: `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`); `PLAN_MAESTRO!A686:O686` — `C04-04.M01.01`: «Fijar objetivo, no-alcance y criterio de no-regresión». Resultado de bloque C04-04: preparar pagos por enlace, webhooks, conciliación y reversión sin acoplar proveedor. Evidencia exigida del bloque: contrato neutral al proveedor, replay y rollback.

**Objetivo concreto:** establecer la frontera documental que deberán cumplir M01.02–M06: un intento de cobro de una factura perteneciente a un tenant verificado, con monto/moneda autorizados server-side, enlace opaco, correlación persistente y reconciliación segura de estados. Esta entrega NO agrega endpoint, pasarela, integración, evento/dispatcher, clave o migración.

**Dependencia individual observada:** `DAG_TECNICO!A647:G647` clasifica esta unidad `DOC_M01_PREP`, `F=VALIDADO`, `G=SI`. Sus 60 prerrequisitos explícitos `C01-01..04/C02-01..04/C03-01..04.M01.01..05` figuraban LISTO en PLAN_MAESTRO antes del claim. Este gate solo autoriza preparación documental; no constituye certificación de pagos ni autorización para QA/PROD.

## 2. Línea base real revisada (blobs del HEAD de preflight)

| Archivo de código existente | Blob | Lo que demuestra | Lo que NO demuestra |
| --- | --- | --- | --- |
| `backend/src/Application/Interfaces/IPagoOnlineProvider.cs` | `50ad09788c3ad31a1a727f9ae6b154834813ee26` | Puerto provider-agnostic; solicitud EmpresaId/FacturaId/Monto/Moneda/ReferenciaFactura/ClaveIdempotencia y respuesta con URI opaca | Proveedor comercial habilitado, precio o settlement confirmado |
| `backend/src/Application/Services/PagoOnlineService.cs` | `65272808514f2995f8c0abee5316aa872a9ce7a4` | Idempotencia por empresa/proveedor, factura con saldo pendiente y validación de moneda; estado Fallido ante excepción | Transacción distribuida ni ausencia absoluta de duplicados bajo múltiples procesos |
| `backend/src/API/Controllers/PagosOnlineController.cs` | `99962b2957f672a6dd435895daffaba7333f8a6b` | `[Authorize]`, permiso Facturacion/Aplicar para iniciar y comprobación de scope tenant; Idempotency-Key de 16–128 caracteres | Un usuario con URL de pago pueda saltar permiso o tenant |
| `backend/src/API/Controllers/InboundWebhooksController.cs` | `05bc86051f32c2de75b68877b79bf70b82c07078` | Ruta de ingreso por tenant/proveedor, límite 256 KiB, secreto por origen, firma y respuestas Accepted/Duplicate/Conflict/InvalidSignature | Aplicación automática de un cobro a ledger ni autorización de dispatcher |
| `backend/src/Domain/Entities/PagoOnline.cs` | `5063b0dc667583f7403fbeec879b519b6ca216d2` | EmpresaId, FacturaId, referencia de proveedor, hash de idempotencia, monto, moneda, estado y fechas UTC | Reverso contable, auditoría multiempresa integral o conciliación real certificada |
| `backend/src/Domain/Entities/WebhookEntrante.cs` | `90294acb096f4ab228bc974fce3be7e7eadbd9d0` | Huella de payload, identidad de origen y estados de recepción sin guardar secretos | Liquidación verificada o autorización de movimiento financiero |

La existencia de rutas y servicios es inventario de código, **no una prueba A1–A3 de cobro o de conciliación** en esta microtarea.

## 3. Alcance futuro condicionado y no-alcance actual

**En futuras unidades, sujeto a diseño, autorización y CI dirigida:** enlace de pago con plazo/caducidad, monto y moneda inmutables por intento, correlación tenant-factura-intento-proveedor, webhook firmado y deduplicado, conciliación financiera contra registro fuente aprobado, historial de transición, estado ambiguo revisable, compensación/rollback explícito y acceso por RBAC. Los tiempos de expiración, comisiones, métodos de pago, moneda, impuestos y política de reintentos requieren decisiones comerciales independientes.

**Exclusiones estrictas en M01.01:** no iniciar un pago, no abrir cuentas de proveedor, no registrar PAN/CVV, tokens/secretos ni API keys, no enviar webhooks, no implementar dispatcher, no emitir reembolsos, no modificar balances ni registros de clientes, no crear tablas/índices, no cambiar JWT/RBAC/tenant, no migrar datos, no activar facturación ni tocar `qa`, `main`, producción, DNS o servicios de pago. Una URL de proveedor no concede autorización al tenant ni acceso privilegiado.

## 4. Invariantes de no-regresión

| ID | Condición exigible en etapas ejecutables | NO_GO si... |
| --- | --- | --- |
| NR01 | Tenant autenticado y membresía activa verificados en backend en cada operación privilegiada | Actor A consulta/inicia/conciliación de pago de B |
| NR02 | Factura y saldo de la misma empresa se verifican antes del intento | Factura ajena o saldo 0 genera enlace |
| NR03 | Monto y moneda autorizados no se alteran por callback ni por parámetro cliente | Se liquida moneda/monto distinto |
| NR04 | `Idempotency-Key` con mismo payload causa un único efecto; payload cambiado falla cerrado | Doble cobro, replay conflictivo o reintentos indeterminados |
| NR05 | Un evento externo se valida por firma/origen/tenant y huella antes de tratarse | Webhook anónimo, inválido o tampered se aplica |
| NR06 | Confirmación del proveedor NO sustituye asiento reconciliado con fuente aprobada | Estado `Pagado` induce efecto económico sin reconciliación |
| NR07 | Reintentos concurrentes entre procesos/instancias exigen unicidad durable y manejo de errores | Un lock local se asume CAS distribuido |
| NR08 | Fallos, timeouts, callbacks duplicados y desordenados no conceden acceso ni alteran contabilidad | Callback tardío resucita intento cancelado/fallido |
| NR09 | Toda transición futura conserva actor/causa/timestamp UTC/correlation-id sin revelar secretos o datos de tarjeta | Falta auditoría o se registran credenciales |
| NR10 | La reversión no borra facturas, historial ni cobros legítimos; debe ser auditable | Se propone DROP, compensación opaca o borrado irreversible |

## 5. Oráculos de aceptación futura (NO ejecutados aquí)

**P01–P05:** tenant A con factura y saldo autorizados obtiene referencia opaca; reintento idéntico retorna el mismo intento; webhook firmado del origen esperado se registra una sola vez; conciliación aprobada vincula exactamente un evento al intento; reversión autorizada conserva historial y no concede permisos.

**N01–N12:** JWT ausente, permiso insuficiente, tenant B, membresía revocada, factura ajena, importe superior al saldo, moneda distinta, proveedor sin configuración, mismo idempotency key con payload distinto, firma inválida/secret ausente, evento duplicado con hash conflictivo y callback tardío/desordenado. Cada caso requiere A1/A2/A3 reales en etapas de implementación y debe fallar cerrado.

Estas son **especificaciones de prueba**, no resultados de `dotnet test`, HTTP, MySQL ni pasarela. El inventario no acredita conexión de pago funcional ni readiness comercial.

## 6. Impacto, auditorías y reversibilidad

Impacto presente: únicamente un nuevo `docs/v5/*.md` y una sección autocontenida de `CHANGELOG_AI.md`. Consumidores futuros: contratos y suscripciones C04-02, metering C04-03, pagos online, webhook ingress, ledger/reconciliación, frontend de checkout y controles multiempresa. Sin modificación de Domain/Application/Infrastructure/API, sin DTO/API nuevos, sin migraciones, secretos ni tareas ejecutables; no cambia la arquitectura vigente.

- **A0 documental:** mandato V5, DAG y blobs vigentes inspeccionados; separada implementación futura.
- **A1 documental:** objetivos/positivos P01–P05 claros, no ejecución real.
- **A2 documental:** NR01–NR10 y N01–N12 definen negativas de tenant/JWT/RBAC, firma, idempotencia y finanzas.
- **A3 documental:** referencias verificables a puertos/servicios/entidades reales y sus limitaciones; no se afirma integración ejecutada.
- **A4 pospublicación:** verificar parent/head/diff, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` sobre SHA exacto; readback PLAN, COLA y BITACORA; compilación .NET, MySQL, Playwright no aplican a diff exclusivamente Markdown.
- **A5:** queda `PENDIENTE` tras A0–A4; la certificación independiente corresponde a otra de las cinco Tasks Javier, nunca a la autora.

**Rollback documental:** `git revert` ordinario del commit (sin force-push), manteniendo eventos históricos en BITACORA. Esta entrega no autoriza M01.06/M02 sin sus gates, ni QA/main/PROD.
