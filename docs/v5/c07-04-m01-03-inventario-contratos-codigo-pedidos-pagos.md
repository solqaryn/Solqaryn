# SOLQARYN V5 — C07-04.M01.03 — Inventario verificable de contratos, código y evidencia de pedidos/pagos

**14 de noviembre de 2026. Alcance:** preparación documental M01.03 de `C07-04 | Pedidos, pagos y documentos`, según el STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL. **No es implementación runtime**: no crea endpoints, migraciones, conexiones de pago o emisiones fiscales. Este inventario distingue declaraciones observadas de garantías aún no probadas.

## Relaciones con las unidades preliminares

- `C07-04.M01.01`: `docs/v5/c07-04-m01-01-alcance-pedidos-pagos-documentos.md`, blob `2efabd1f6b2444e605ca49cf1cce34ec4bce23d8`; incluye NR01–NR12, P01–P06 y N01–N14, todos los oráculos futuros NO_EJECUTADOS.
- `C07-04.M01.02`: `docs/v5/c07-04-m01-02-actores-interfaces-pedidos-pagos-documentos.md`, blob `9c5c0aba4694ffbbb6a4bfacfa683330de814f1d`; inventaría A01–A12, I01–I12 y B01–B08, sin certificar la implementación del sistema.
- Ambas unidades terminaron A0–A4 documentales en `dev`; **A5 permanece independiente y no se infiere aprobación de integración por su publicación**.

## Matriz de doce fuentes vivas de Git (blobs SHA verificados)

| # | Fuente | Blob Git SHA | Señal técnica comprobada por inspección |
| --- | --- | --- | --- |
| 01 | `backend/src/Domain/Entities/Cotizacion.cs` | `28d682b83d5b7d895aceb0ce57b56aa3ea3843e8` | `EmpresaId` nullable legacy, snapshot de cliente y transiciones cotización. |
| 02 | `backend/src/Domain/Entities/PedidoVenta.cs` | `12c265e9b3dbd6d27883d52e3b2a0b05783eb0cf` | `EmpresaId` nullable, `IdempotencyKey` y fingerprint, creación desde cotización aceptada. |
| 03 | `backend/src/Domain/Entities/Venta.cs` | `64ab3cd92e81031e74137f420cd748c564e28f01` | Venta y estado de pago; tenant nullable, transición comercial vinculada a ERP actual. |
| 04 | `backend/src/Domain/Entities/Factura.cs` | `8c59b9a939cc9439efa5fd09777d9b0dfb19de66` | Snapshots de emisor/cliente, impuestos, estado, saldo y pagos, tenant nullable. |
| 05 | `backend/src/Domain/Entities/FacturaPago.cs` | `c35f5e678a2a267f8dd56fedefd24450ae36ee7b` | Monto aplicado, recibido, cambio, método de pago, anulaciones y tenant nullable. |
| 06 | `backend/src/Domain/Entities/PagoOnline.cs` | `5063b0dc667583f7403fbeec879b519b6ca216d2` | Tenant obligatorio, factura, estado, hash de clave de idempotencia y referencia del proveedor. |
| 07 | `backend/src/API/Controllers/CotizacionesController.cs` | `b6cc9aa76dc6f0ee178a7e37b6c4471dac7a749e` | `Authorize`, permisos `Ventas`, enviar/aceptar/rechazar/convertir. |
| 08 | `backend/src/API/Controllers/PedidosVentaController.cs` | `293839e33ced2fda5328eee8b42b2e7ae7a1fa50` | `Authorize`, permisos `Ventas`, encabezado `Idempotency-Key` obligatorio al crear. |
| 09 | `backend/src/API/Controllers/VentasController.cs` | `5a1041030359ed1503c8740d2e04e60ade984797` | `Authorize`, permisos de venta, crear/confirmar y consulta/vista previa. |
| 10 | `backend/src/API/Controllers/FacturasController.cs` | `8a70f59763749896c3662a365ec9e8ffe47dc72a` | `Authorize`, aplicar/anular pago, rutas y auditoría de endpoints observados. |
| 11 | `backend/src/API/Controllers/PagosOnlineController.cs` | `99962b2957f672a6dd435895daffaba7333f8a6b` | `IPagoOnlineProvider` opcional, scope tenant en controlador, clave de idempotencia de longitud acotada. |
| 12 | `backend/src/API/Controllers/FacturacionFiscalController.cs` | `6dd92895e2299feda3737a1a6eb28a3d960bcf7e` | Permiso fiscal, validación de RTN, sucursal y scope de empresa; no equivale a emisión fiscal certificada. |

**Método y límite:** los doce blobs fueron confirmados en un mismo HEAD `dev` mediante lectura Git autenticada. Inspección selectiva de entidades/controladores **no prueba** aislamiento en los servicios, repositorios o transacciones, ni conformidad fiscal del proveedor. No se revisaron secrets, datos productivos ni operadores de pago.

## Brechas G01–G10 que requieren evidencia causal, no afirmaciones de vulnerabilidad

| ID | Aspecto no certificado | Evidencia para resolver en fase autorizada |
| --- | --- | --- |
| G01 | EmpresaId NULL legado en Cotizacion/Pedido/Venta/Factura/Pago | Conteo/análisis reversible + consultas negativas tenant A/B con filas NULL rechazadas. |
| G02 | Alcance de cliente y documento en servicios/EF | Tests de recuperación por ID ajeno y filtros de tenant/objeto para cada endpoint. |
| G03 | Idempotencia durable de pedido y confirmación | Test concurrente, índice/constraint y replay misma clave/diferente payload con MySQL efímero. |
| G04 | Transiciones de cotización a pedido y venta | Aceptación previa obligatoria, semántica de conversión única y rollback de estado. |
| G05 | Consistencia stock/pedido/venta/factura | Integración transaccional que impida stock duplicado y factura sin venta válida. |
| G06 | Importes, cambio y saldo de FacturaPago | Pruebas de moneda, redondeo, monto negativo, pagos simultáneos y reversión autorizada. |
| G07 | Respuestas y autorización en pagos online | Credenciales fuera de código, firma/callback, replay provider-event y ausencia de dobles cargos. |
| G08 | Integridad fiscal/legal de comprobantes | Pruebas con identidad RTN, sucursal, snapshots, numeración y proveedor sandbox habilitado. |
| G09 | Permisos y PII en PDF/exportación | A/B por tenant y usuario, enlaces vencidos, datos en logs/cache y revocación. |
| G10 | Auditoría, rollback y aislamiento en fallos | Error en mitad de transacción, compensación y eventos inmutables sin pérdida de evidencia. |

G01–G10 son **brechas de evidencia**, no hallazgos confirmados ni autorización para modificar esquema, pagos o emitir comprobantes.

## Puntos de integración y órdenes de comprobación

1. **Frontend Angular 22.2.1** envía acciones comerciales; validación de UI nunca sustituye autorización backend.
2. **API ASP.NET Core 10** valida JWT, módulo/acción, scopes por tenant y DTO antes de invocar Application.
3. **Application/Domain** preservan máquina de estados, idempotencia, snapshots y propiedad del documento.
4. **Infrastructure EF Core 10 / MySQL** soportaría transacciones, índices, persistencia y migraciones sólo en unidades posteriores expresamente autorizadas y CI efímera.
5. **Integraciones** de pago, PDF, fiscal, SMTP o enlaces públicos requieren pruebas/certificación por proveedor y permisos; nada queda activado por esta M01.03.
6. **Auditoría y rollback** verifican eventos, estado inicial/final y compensaciones por tenant sin secretos ni reescritura de historial.

## Oráculos futuros, NO_EJECUTADOS

**P01–P06:** convertir cotización propia aceptada; crear un pedido idempotente; registrar pago válido y balance; anular con motivo y permiso; recuperar PDF propio; emitir fiscal sólo con tenant/sucursal/identidad válidos en entorno de prueba autorizado.

**N01–N12:** tenant B busca documento A; NULL legacy aparece a otro tenant; cotización no aceptada convierte; replay conflictivo crea dos pedidos; doble confirmación resta inventario dos veces; pago negativo se acredita; cambio aumenta pagado; usuario sin `Aplicar` paga; usuario sin `Anular` revierte; callback falso aplica cargo; factura ajena se exporta; proveedor fiscal recibe sucursal/RTN inválidos.

**Ningún P/N se ejecutó aquí.** Se exigen pruebas reales de xUnit, HTTP, MySQL efímero, autorización A/B y Playwright proporcional en microtareas de código pertinentes, con Scope Lock exact SHA y A5 independiente.

## A0–A5, impacto, trazabilidad y retorno seguro

- **Impacto**: dos archivos Markdown solamente (este documento y sección compacta autocontenida en `CHANGELOG_AI.md`). Sin API/DTO, Domain, base de datos, Angular, JWT/RBAC, secrets, nuevos servicios, events, workers, DNS, hosting, QA/main/PROD.
- **A0**: 12 archivos/blobs y autoridad V5 presentes; **A1**: coherencia del inventario y diez brechas + oráculos documentales; **A2**: riesgos de tenant, auth, privacidad y fiscal explicados, no certificados en runtime; **A3**: pruebas runtime `NO_APLICA` para diff exclusivamente Markdown; **A4**: commit fast-forward y checks Scope Lock + V5 provenance `completed/success` sobre SHA exacto, readback PLAN/COLA/BITACORA.
- **Dependencias**: DAG técnico `C07-04.M01.03` F=VALIDADO/G=SI y COLA G/O=SI por ID al reclamar; M01.06 conserva gate terminal individual; A5 separado y sin autopromoción.
- **Rollback documental**: `git revert` normal con nuevo evento compensatorio, sin sobrescribir las 26 columnas históricas de BITACORA.
- **A5**: revisión independiente de otra de las cinco Tasks JAVIER autorizadas; no confiere permiso para producción o migraciones.

**Estado:** inventario y brechas documentados; runtime, fiscalidad y transacciones no certificados por esta entrega.
