# SOLQARYN V5 — C08-02.M02.01 — Modelo conceptual de mesas, QR y pedidos

**Autor:** PRIMARY DEV 06 JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-15. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, fila PLAN_MAESTRO 1196. **Riesgo:** R0, solo documentación; sin ejecución ni migraciones. **Entorno:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

## 1. Contrato y límites

M02.01 define entidades, ownership, relaciones e invariantes para **mesas, QR, menú publicado, pedidos internos y autoservicio**. No crea tablas, DTO, API, esquemas EF, credenciales, QR reales, workers, colas, cobros ni componentes Angular. La autoridad de precios, impuestos, inventario, facturación y caja permanece en ERP existente.

Predecesores: C08-02.M01.01–06 en LISTO con A5 independiente; terminal M01.06 define B01–B16, E01–E12 **futuras NO_EJECUTADAS** y RB01–RB08, y las dependencias de C07-02/C08-01/C01/C02/C06. Al claim V17 la fila 1196 estaba PENDIENTE, owner vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI. No autoriza sucesores automáticamente. Decisiones D01–D08 del análisis M01 siguen abiertas y no representan autorización de implementación.

## 2. Entidades conceptuales y ownership

| ID | Entidad | Propiedad y vínculo mínimo | Control conceptual |
| --- | --- | --- | --- |
| E01 | Empresa | Tenant raíz | TenantId efectivo verificado por servidor |
| E02 | Sucursal | Empresa 1:N Sucursal | TenantId+SucursalId; membresía de sede |
| E03 | ZonaServicio | Sucursal 1:N Zona | Zona activa, sin acceso entre sedes |
| E04 | Mesa | Sucursal 1:N Mesa | ID opaco, versión y estado; propiedad inequívoca |
| E05 | AliasQR | Mesa 1:N aliases versionados | Referencia pública no autorizante |
| E06 | CredencialMesa | AliasQR 1:N credenciales rotables | TTL, firma/entropía y revocación, secreto fuera de logs |
| E07 | SesionServicio | Mesa 1:N sesiones acotadas | Tenant/sede/mesa, vencimiento; sin rol de caja |
| E08 | MenuPublicado | Sucursal 1:N versiones | Solo proyección explícitamente publicada |
| E09 | ItemMenu | MenuPublicado 1:N ítems | SKU/variante ERP vinculada a tenant, precio de servidor |
| E10 | OpcionMenu | ItemMenu 1:N opciones | Validación de límites, modificadores y disponibilidad |
| E11 | Pedido | Empresa+Sucursal+Mesa+Sesión | Estado versionado, autoría, moneda y trazabilidad |
| E12 | LineaPedido | Pedido 1:N líneas | Cantidad positiva y snapshot de precio/IVA de servidor |
| E13 | ComandaProduccion | Pedido 1:N destinos | Cocina autorizada de sede; no implica cobrar |
| E14 | TransicionPedido | Pedido 1:N eventos | Estado anterior/nuevo, versión y actor saneado |
| E15 | ReservaInventario | LíneaPedido 0:N vínculos ERP | No crea segunda fuente de stock |
| E16 | CierrePedido | Pedido 0:1 cierre lógico | Factura/venta/caja siguen autoridad ERP |
| E17 | RegistroIdempotencia | Operación y fingerprint | Key por tenant, sede, actor/sesión y tipo |
| E18 | AuditoriaServicio | Operación/resultado | Correlación sin JWT/QR secreto/PAN/PII |
| E19 | ConciliacionERP | Pedido/Cierre 0:N evidencias | Confirma estado definitivo sin efectos duplicados |
| E20 | RevisionContrato | Empresa+sede+versión | Flags restrictivos, compatibilidad y rollback |

Todas son **propuestas conceptuales**: no se afirma que ya existan en MySQL. M02.02 definirá API/DTO, M02.03 índices/fixtures, M02.04 configuración y M02.05 eventos bajo gates individuales.

## 3. Relaciones y límites de confianza

- `Empresa -> Sucursal -> Mesa` establece ownership obligatorio; IDs de cliente nunca alteran empresa del contexto autenticado. Reasignar mesa requiere invalidar QR/sesión anterior.
- `Mesa -> AliasQR -> CredencialMesa` es localización no autorización. QR copiado, vencido, revocado o de B no concede derechos de rol ni datos privados.
- `Mesa -> SesionServicio -> Pedido -> LineaPedido` conserva TenantId/SucursalId y versión. No hay lectura/escritura cruzada aunque identificadores numéricos coincidan.
- `MenuPublicado -> ItemMenu -> OpcionMenu` expone solo catálogo habilitado. Precio, impuestos, descuentos y existencias son calculados por autoridad ERP/servidor, nunca por payload del cliente.
- `Pedido -> ComandaProduccion / TransicionPedido / CierrePedido / ConciliacionERP` preserva estados terminales y efectos transaccionales o compensables. Los registros de auditoría e idempotencia son transversales pero minimizados.

## 4. Invariantes exigibles para implementaciones posteriores

| ID | Invariante y condición de rechazo |
| --- | --- |
| INV01 | TenantId procede del backend; un tenant de cliente no concede acceso |
| INV02 | Sucursal/Mesa deben coincidir con tenant; mesa A nunca visible desde B |
| INV03 | Cada alias QR activo resuelve una única mesa con versión y dueño verificables |
| INV04 | QR falso, revocado, vencido o de B se rechaza sin filtrar datos |
| INV05 | Rotar o revocar QR invalida credenciales anteriores y sesiones dependientes |
| INV06 | Sesión de mesa no confiere rol de empleado, caja ni permisos de pago |
| INV07 | Backend revalida membresía, rol, sede y revocación antes de toda escritura |
| INV08 | Menú público excluye SKU ocultos, PII y contenido de tenant B |
| INV09 | Precio/IVA/moneda/descuento se validan desde ERP/servidor |
| INV10 | Cantidad <=0, variante prohibida o modificador inválido no altera stock |
| INV11 | Solo transiciones de estado autorizadas y versión vigente; terminal no reabre |
| INV12 | Retry idéntico produce un efecto; replay con payload distinto no duplica |
| INV13 | Scope de key idempotente incluye tenant, operación, actor/sesión y huella |
| INV14 | Carrera entre operadores no pierde escritura; rechazo por versión/409 |
| INV15 | Pedido tiene máximo un cierre lógico; nunca doble factura/stock/cobro |
| INV16 | Falla entre pedido/inventario/factura se revierte o compensa trazablemente |
| INV17 | Mesero, cocina y caja solo acceden a su sede y funciones del rol actual |
| INV18 | Logs sanos sin JWT, secreto QR, PAN ni PII innecesaria, con correlación |
| INV19 | Offline/timeout/401/403/409 no se comunica como operación exitosa |
| INV20 | Ningún feature/migración/despliegue sin pruebas R2, A5 y permiso de entorno |

## 5. Oráculos positivos y negativos futuros — NO_EJECUTADOS

| Caso | Positivo futuro | Negativo o frontera que debe fallar cerrado |
| --- | --- | --- |
| T01 | Mesa y sucursal A válidas | Mesa A consultada desde B |
| T02 | QR A vigente resuelve mesa publicada | QR falso/revocado/expirado/B |
| T03 | Menú publicado con SKU permitido | SKU oculto, IVA/precio manipulado |
| T04 | Mesero con rol de A abre pedido A | Revocado, sin rol o sede B |
| T05 | Cantidad y opción válidas crean línea | Cantidad 0/negativa/excesiva u opción inválida |
| T06 | Misma key+payload reintenta sin nuevo efecto | Misma key con payload distinto |
| T07 | Versión vigente permite transición | Versión stale o pedido terminal |
| T08 | Cocina A ve comanda A | Cocina B o caja sin permiso |
| T09 | Cierre autorizado concilia ERP una vez | Replay o fallo parcial causa doble stock/factura |
| T10 | Desconexión informa indeterminado | UI muestra éxito sin confirmación servidor |
| T11 | CorrelationId permite auditoría saneada | Exposición de JWT, QR secreto, PAN, PII o empresa B |
| T12 | Cambio de sede usa proceso aprobado | Alias antiguo queda autorizado en nueva sede |

**Pruebas runtime ejecutadas por esta entrega: 0.** No se declara PASS de T01–T12, autenticación JWT, API, MySQL, navegadores ni proveedores. En M03/M04 exigir xUnit positivos/negativos, tenant A/B e IDOR, MySQL 8.4 **efímero** cuando se cambie persistencia, HTTP real si hay endpoint y Playwright/Chromium si cambia UI. Un mock no acredita integración.

## 6. Riesgos, impacto, A0–A5 y rollback

Capas futuras: Domain modela estados; Application exige tenant/roles y coordina; Infrastructure integra ERP/persistencia solo si se autoriza; API valida identidad; Angular nunca concede autorización. **Impacto ejecutable actual: ninguno**; cero migraciones, secretos, flujos monetarios, nuevas cuentas, rutas o despliegues. Consumidores: M02.02–05 y M02.06 sujetos a su DAG.

A0: fuente, 20 entidades, 20 invariantes, 12 pares futuros, alcance y diff. A1: comprobación documental de relaciones y coherencia, no ejecución runtime. A2: diseño contra QR falso, tenant A/B/IDOR, revocación, replay, doble cierre y privacidad; sin afirmar seguridad viva. A3: NO_APLICA a MySQL/HTTP/.NET/Angular/Playwright por diff exclusivamente Markdown. A4: exigir fast-forward/CAS, Scope Lock y procedencia V5 **SUCCESS del SHA publicado**, PLAN/BITACORA/readback y CHANGELOG sin encabezados vacíos. A5: revisión autónoma por otra Task Javier distinta de DEV06; no autocertificar.

Rollback documental: modificación aditiva o `git revert` normal de dev tras verificar HEAD/owner, preservando BITACORA y sin force-push. No se autoriza QR en vivo, pagos, migración real ni QA/main/PROD.
