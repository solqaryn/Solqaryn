# SOLQARYN V17 — C08-02.M02.02: contratos futuros de API, DTO, errores y compatibilidad de mesas y QR

**Microtarea:** C08-02.M02.02, PLAN_MAESTRO fila 1197. **Autoría:** PRIMARY DEV 03 — JAVIER, Task ID 6adc2e4bd8a48191ac7d8641ed95a1a3. **Alcance:** solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. **Riesgo V15:** R0, contrato exclusivamente documental. **Estado del runtime:** NINGUNO de los endpoints, DTO, políticas de QR, esquemas, roles o claves propuestos aquí se crea ni se activa.

**Fuente de ejecución:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA; PLAN_MAESTRO, COLA y DAG_TECNICO institucionales. **Antecesores técnicos:** C08-02.M01.01–06 + A5 independientes, verificados por fórmulas de DAG_TECNICO F=VALIDADO/G=SI al reclamar. El documento terminal de M01.06, \`docs/v5/c08-02-m01-06-bloqueo-rollback-liberacion-lote.md\`, tiene blob \`8a7c4f48888ef0581015cfe2ed9889fa9247daa6\` en la base consultada. Este alcance no supone que M02.01 esté LISTO: M02.02 es documental independiente autorizada por el DAG vivo, sin habilitar M03–M06 por sí sola.

## 1. Contrato de confianza y fronteras

- **Contexto comercial:** resolver mesa, sucursal, empresa, menú, precios y permisos únicamente mediante relaciones verificadas en servidor; jamás usar \`EmpresaId\`, \`MesaId\`, \`SucursalId\`, slug, host, cookie o cabecera de navegador como autoridad autónoma.
- **QR público:** una referencia opaca firmada con control de expiración y revocación sólo podría permitir obtener proyección pública mínima de mesa/menú. No equivale a sesión autenticada, no permite mutaciones ni expone PII, stock interno o identificadores de otra empresa. La política exacta de firma/TTL es decisión futura, nunca un secreto añadido en este cambio.
- **Operador:** enviar, cambiar o cerrar pedidos exigiría sesión/JWT vigente, membresía de empresa, rol y sede autorizados por backend. Revocaciones prevalecen sobre guard UI, token caché y permisos antiguos.
- **Cliente de autoservicio futuro:** una mutación requeriría una capacidad de corta duración vinculada server-side a mesa y pedido más control antifraude, revocación, idempotencia y rate limiting; QR por sí solo **no** es permiso de escritura. Si esta prueba no existe, denegar la operación.
- **Estados y dinero:** estados terminales no se reabren; precio, impuesto, descuento, inventario, factura y cobro quedan bajo las autoridades transaccionales existentes. Un error de cocina, red o PSP no puede mostrarse como pedido cobrado/cerrado.
- **Compatibilidad:** rutas, métodos y nombres siguientes son **propuestas de contrato**, no rutas HTTP actuales ni cambios de API. No se reemplazan controllers ni contratos ERP existentes. La futura publicación requerirá versionado, compatibilidad aditiva, pruebas de clientes y rollback independiente.

## 2. Ocho operaciones de contrato propuestas (NO IMPLEMENTADAS)

| Operación propuesta | Actor y condición | Resultado mínimo | Denegación segura |
| --- | --- | --- | --- |
| OP01 ResolverMesaQR (lectura) | Visitante con QR opaco vigente, host/tenant y mesa verificados en servidor | Proyección pública de sede y mesa sin PII | QR falso/revocado/de B: no divulgar si existe la mesa |
| OP02 ConsultarMenuPublicado (lectura) | Referencia pública acotada a sede, versión publicada y rate limit | Productos visibles y precios canónicos de servidor | SKU oculto, tenant cruzado o versión obsoleta: respuesta mínima |
| OP03 CrearPedidoMesa (escritura futura) | Mesero autorizado o capacidad de autoservicio separada, verificada server-side | Identificador opaco, versión, estado inicial y clave idempotente | QR solo, rol revocado, sede ajena o replay divergente: rechazar |
| OP04 ConsultarPedido (lectura) | Personal con JWT/rol/sede o token de pedido acotado, nunca mesa ajena | Estado permitido sin notas privadas de otra mesa | Identificador B, token ajeno/caducado: no revelar existencia |
| OP05 AgregarLineaPedido (mutación) | Pedido abierto, versión actual, producto publicado y permiso vigente | Línea y total recalculados en servidor | Pedido cerrado, precio cliente, producto oculto, versión vieja |
| OP06 EnviarPedidoCocina (transición) | Mesero autorizado, pedido válido y transacción idempotente | Estado ENVIADO de una sola vez y acuse auditable | Doble envío, versión obsoleta, pedido cancelado |
| OP07 MarcarPreparacion (transición) | Cocina autorizada en empresa/sede correctas y orden vigente | Estado PREPARANDO/LISTO permitido, versión incrementada | Cocina B, rol revocado, transición hacia atrás/terminal |
| OP08 CerrarPedido (transición) | Caja o responsable autorizado, conciliación ERP y versión actual | Cierre único y correlación sin PII ni secretos | Replay, pago sin comprobar, stock parcial o conflicto: rollback/no cierre |

Las operaciones no introducen endpoint, DTO ejecutable, nueva tabla ni derecho de cobro. Una cancelación, modificación de orden o pago futuro exige contrato y autorización separados; no se ocultan dentro de OP08.

## 3. Ocho DTO conceptuales versionados (NO CREADOS EN CÓDIGO)

| DTO propuesto | Campos contractuales mínimos | Campos rechazados o restringidos |
| --- | --- | --- |
| DT01 MesaQrPublicaV1 | \`referenciaOpaca\`, \`vencimientoUtc\`, \`estadoPublicable\` | JWT, QR secreto completo, EmpresaId autoritativo |
| DT02 MenuPublicadoV1 | \`versionCatalogo\`, \`productosPublicos\`, \`precioMoneda\` | costo interno, PII, inventario reservado |
| DT03 CrearPedidoMesaV1 | \`capacidadAutorizada\`, \`lineasSolicitadas\`, \`idempotencyKey\` | precio/costo/impuesto calculado por cliente |
| DT04 EstadoPedidoPermitidoV1 | \`pedidoRef\`, \`estado\`, \`version\`, \`actualizadoUtc\` | correo, notas privadas, tokens, datos B |
| DT05 ModificarLineaV1 | \`pedidoRef\`, \`productoRef\`, \`cantidad\`, \`expectedVersion\`, \`idempotencyKey\` | sede/tenant como permiso, importes del cliente |
| DT06 TransicionCocinaV1 | \`pedidoRef\`, \`accion\`, \`expectedVersion\`, \`idempotencyKey\` | actor/rol enviados como autorización |
| DT07 CierrePedidoV1 | \`pedidoRef\`, \`expectedVersion\`, \`idempotencyKey\`, \`confirmacionAutorizada\` | PAN/secretos PSP, \`pagado=true\` sin prueba |
| DT08 ErrorSeguroV1 | \`codigoPublico\`, \`correlationIdOpaco\`, \`reintentable\` | excepción, stack, identificadores cruzados |

Los nombres, cardinalidades y codificación son aspiracionales; su migración a contratos reales requerirá análisis de compatibilidad sobre el API actual, definición de nullable/required y validadores que produzcan fallos verdaderos.

## 4. Doce códigos de error y manejo seguro propuestos

| Código | Semántica | Política observable futura |
| --- | --- | --- |
| ER01 INVALID_QR | QR malformado, falsificado o revocado | Denegar sin filtrar validez de mesa ajena |
| ER02 EXPIRED_CAPABILITY | Capacidad o QR expirados | No reactivar por hora de navegador |
| ER03 TENANT_SCOPE_DENIED | Empresa/sede/mesa fuera del contexto autorizado | 403/404 seguro según contrato, sin PII |
| ER04 UNAUTHENTICATED | Sesión no acreditada donde corresponde | 401 sin escribir datos |
| ER05 PERMISSION_REVOKED | Rol, miembro o permiso revocado | 403 incluso si la UI muestra caché |
| ER06 PRODUCT_NOT_PUBLISHED | Producto o versión fuera de menú vigente | No mostrar SKU, costo o inventario oculto |
| ER07 VERSION_CONFLICT | Pedido cambiado desde expectedVersion | 409, pedir refresco sin mutación parcial |
| ER08 IDEMPOTENCY_CONFLICT | Misma clave, payload/fingerprint distinto | 409, no duplicar efectos |
| ER09 ORDER_TERMINAL | Pedido cerrado/cancelado | No reabrir por transición accidental |
| ER10 STOCK_OR_PRICE_CONFLICT | Stock o precio canónico inválido | Denegar sin cálculo económico del cliente |
| ER11 DEPENDENCY_UNAVAILABLE | ERP, cocina o autorización no disponible | Retries acotados, estado real no ficticio |
| ER12 CLOSE_NOT_VERIFIED | No se verificó cierre/cobro conciliado | No facturar, no marcar pagado y auditar mínimo |

No se fijan nuevos códigos HTTP reales. La selección definitiva debe seguir prácticas existentes del repositorio y no romper clientes actuales.

## 5. Dieciséis validaciones contractuales (FUTURAS)

| ID | Verificación obligatoria | Escenario negativo a rechazar |
| --- | --- | --- |
| VA01 | Host público vinculado inequívocamente a empresa y sede verificadas | Host de B resuelve mesa de A |
| VA02 | QR íntegro, expiración UTC y revocación | Firma falsa o código vencido |
| VA03 | Mesa activa dentro de sucursal canónica | Mesa eliminada o moved a otra sede |
| VA04 | Menú/proyección publica sólo datos allowlisted | SKU no publicado o PII expuesta |
| VA05 | Identidad, JWT y membresía de operador en servidor | JWT ausente o rol eliminado |
| VA06 | Permiso por operación y sucursal, no por vista UI | Cocina intenta cierre de caja |
| VA07 | Capacidad de autoservicio ligada al pedido, no al QR por sí solo | Visitante intenta escritura sólo con QR |
| VA08 | Identidad tenant A/B en cada repositorio/caché | Pedido B consultado desde A |
| VA09 | Estados y transiciones permitidos con versionado | Reabrir terminal o enviar dos veces |
| VA10 | \`expectedVersion\` comprobado atómicamente | Dos operadores hacen lost-update |
| VA11 | Idempotencia: clave + tenant + operación + hash de payload | Clave repetida con payload nuevo |
| VA12 | Producto/stock/precio/impuesto de servidor y transacción | Cliente altera cantidad/precio fuera límites |
| VA13 | Cierre financiero/stock con reversión o compensación verificable | Fallo entre factura y cierre |
| VA14 | Rate limit, tamaño, lista permitida y errores sanos | Fuerza bruta QR, payload excesivo o HTML |
| VA15 | Auditoría saneada correlacionada y sin secretos | Token, PII o QR completo en log |
| VA16 | Respuesta UX de error/timeout basada en estado real | Offline/409 presenta "pagado" o "cerrado" |

## 6. Matriz reproducible de aceptación futura — NO_EJECUTADA

| Prueba futura | Positivo requerido | Negativo adversarial requerido | Entorno al implementarse |
| --- | --- | --- | --- |
| TP01 | QR A firmado y vigente muestra menú A | QR falso/expirado/B no revela mesa B | HTTP + autenticidad/capacidad |
| TP02 | Mesero A crea pedido autorizado | Usuario sin rol, cocina B o QR solo rechaza | JWT/RBAC + MySQL 8.4 efímero |
| TP03 | Replay idéntico no duplica pedido | Misma clave con líneas distintas devuelve conflicto | Concurrencia e índice de idempotencia |
| TP04 | Cambio de línea aplica versión actual | Stale/terminal rechaza y deja stock intacto | xUnit + EF/MySQL aislado |
| TP05 | Cocina A cambia estado propio | Cocina B, rol revocado o paso inválido rechaza | API + roles A/B |
| TP06 | Caja cierra una vez con conciliación real | Falla intermedia/replay no duplica factura | Integración ERP MySQL descartable |
| TP07 | Móvil/teclado muestra error y foco correcto | 401/403/409/offline no finge cierre | Angular + Chromium real |
| TP08 | Auditoría registra correlación mínima | Logs/errores no exponen PII, QR o secretos | Logs de pruebas efímeras |

**Estado:** TP01–TP08, todos NO_EJECUTADOS en esta microtarea documental. MySQL, API, Angular, Playwright, JWT runtime y pagos: **A3 NO_APLICA para este diff Markdown**, no PASS de integración.

## 7. No regresión, dependencias, publicación y reversión

1. No alterar ERP, clientes existentes, host bindings, facturación, inventario, JWT/RBAC ni proveedores por este documento. Domain, Application, Infrastructure y API conservan sus fronteras actuales; UI nunca sustituye validación de backend.
2. No crear módulos M03/M04/M05/M06, contratos live ni eventos/dispatchers: cada sucesora debe pasar por COLA G/O=SI, DAG F=VALIDADO/G=SI, predecesoras LISTO+A5 cuando exija la fórmula, owner exclusivo y V17 named-range lock.
3. **A0:** revisión de alcance de un documento nuevo más sección propia contigua de CHANGELOG, sin secretos ni migraciones. **A1:** comprobación de ocho operaciones, ocho DTO, doce errores, dieciséis validaciones y ocho pares futuro positivo/negativo, sin atribuir pruebas runtime. **A2:** auditoría estática de tenant A/B, roles, QR, replay, revocación, privacidad y estados terminales. **A3:** coherencia documental; integración real NO_APLICA al diff. **A4:** publicación individual fast-forward de dev, Scope Lock y V5 procedencia completed/success en SHA causal, PLAN/BITACORA/readback. **A5:** revisión independiente por Task Javier distinta de la autora; puede rechazar si hay ambigüedad o riesgo.
4. No afirmar certificación API/HTTP, JWT end-to-end, compatibilidad de clientes, MySQL, UX, pagos o producción. Las métricas de rendimiento y suites nuevas quedan \`NO_MEDIDO\` hasta prueba real.
5. Rollback permitido: \`git revert\` normal del commit documental en dev con validación posterior; no force-push, borrar BITACORA ni modificar las filas/fórmulas de dependencias.

**Resultado:** contrato documental R0 de C08-02.M02.02, no ocho endpoints ni un flujo de mesas/QR implementado.
