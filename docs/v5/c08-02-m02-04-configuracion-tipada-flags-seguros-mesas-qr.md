# SOLQARYN V17 — C08-02.M02.04: configuración tipada y flags seguros de mesas, QR y pedidos

**ID:** C08-02.M02.04, PLAN_MAESTRO fila 1199. **Autor:** SOLQARYN PRIMARY DEV 03 JAVIER, Task ID 6adc2e4bd8a48191ac7d8641ed95a1a3. **Rama única:** \`solqaryn/Solqaryn@dev\`, PROJECT_SCOPE_LOCK=STRICT. **Nivel de riesgo:** R0 exclusivamente documental. No se crean nuevas claves reales de configuración, endpoints, roles, QR ejecutables, pagos, colas, bases de datos, workers ni feature flags efectivos.

**Fuentes:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, documento Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA; PLAN_MAESTRO/COLA/DAG_TECNICO institucionales; \`docs/v5/c08-02-m01-06-bloqueo-rollback-liberacion-lote.md\` (blob \`8a7c4f48888ef0581015cfe2ed9889fa9247daa6\`, verificado al preparar). Su DAG técnico C08-02.M01.01–06+A5 fue VALIDADO/SI, COLA G/O=SI/SI al reclamar. La disponibilidad documental de esta unidad **no** sustituye ninguna validación runtime ni convierte M02.01 u otra sucesora en LISTO.

## 1. Frontera de autoridad, incompatibilidades y ámbito

- Los valores aquí son **nombres conceptuales**, no archivos \`appsettings\`, variables de entorno, API ni propiedades existentes de código.
- El estado de denegación predomina siempre: valores ausentes, corruptos, versión obsoleta, consentimiento revocado, QR inválido, token caducado, tenant sin resolver o backend indisponible bloquean las funciones de negocio.
- Empresa y sucursal se resuelven con autenticación y relaciones **servidor**. Parámetros de host, QR, URL, cabeceras, body o preferencias de Angular jamás cambian \`EmpresaId\`/permisos.
- La proyección pública de un menú no otorga capacidades de escribir pedidos. Un QR solo no equivale a JWT; cualquier futuro autoservicio con mutación requiere capacidad distinta, acotada, validada y revocable por servidor.
- Autoridad de precios, impuestos, inventario, cobros y cierre continúa en ERP transaccional existente; flags conceptuales no autorizan descuentos, cambios de stock ni facturas.
- Toda configuración futura tendrá fuente canónica, versión, auditoría, fecha UTC y rollback. Caché por tenant/sucursal/versión y revocación prevalecen sobre datos antiguos. No crear dispatcher ni eventos ejecutables.

## 2. Seis ámbitos tipados (propuestas sin implementación)

| Ámbito | Permiso máximo de restricción | Lo que jamás puede autorizar |
| --- | --- | --- |
| GlobalSeguridad | Desactivación total, cota máxima de cuota, política de firma/verificación y error seguro | Acceso de tenant o privilegios de operador |
| Empresa | Desactivar funciones por compañía, limitar cuota/TTL a valores inferiores | Activar un módulo prohibido globalmente ni cruzar A/B |
| Sucursal | Desactivar QR/mesa o reducir cuotas por sede | Resolver mesa ajena por \`MesaId\` de cliente |
| Canal | Restringir uso de menú público, autoservicio, terminal o cocina | Transformar QR público en JWT ni enviar eventos |
| TipoDispositivo | Restringir UX/lectura móvil o terminal validado en servidor | Leer PII ni escribir pedidos sin autorización |
| UsuarioSesion | Preferencia de presentación no autoritativa y revocación instantánea | Cambiar RBAC, stock, saldos, firma, transacción o TTL superior |

**Precedencia:** denegación global > tenant > sede > canal/dispositivo > sesión. Combinar mediante intersección, jamás unión que amplíe permisos. Una preferencia del cliente sólo reduce o modifica la presentación; cada operación revalida backend, tenant, sede, actor y estado.

## 3. Dieciséis opciones contractuales inertes y sus valores iniciales

| ID | Opción propuesta | Tipo futuro | Default denegador | Obligación y rechazo |
| --- | --- | --- | --- | --- |
| O01 | PublicMenuEnabled | Boolean | false | Solo menú publicado filtrado, sin datos internos |
| O02 | QrResolutionEnabled | Boolean | false | Firma/expiración/revocación comprobadas server-side |
| O03 | TableOrdersEnabled | Boolean | false | Pedido sólo con JWT/rol o capacidad autorizada separada |
| O04 | SelfServiceMutationsEnabled | Boolean | false | QR solo jamás permite mutaciones |
| O05 | KitchenTransitionsEnabled | Boolean | false | Cocina valida rol, sede, estado y expectedVersion |
| O06 | CashCloseEnabled | Boolean | false | Caja transaccional, sin cobro inferido de UI |
| O07 | QrTokenTtlSeconds | NonNegativeInteger | 0 (deshabilitado) | Debe ser mayor que 0 sólo al habilitar QR de forma autorizada |
| O08 | QrResolutionsPerMinute | NonNegativeInteger | 0 (deshabilitado) | Contador servidor por contexto, no cuota impuesta por cliente |
| O09 | PublishedMenuCacheTtlSeconds | NonNegativeInteger | 0 (deshabilitado) | Invalida por versión y revocación, nunca usa caché B |
| O10 | MaxLinesPerOrder | NonNegativeInteger | 0 (deshabilitado) | Validación server-side antes de aceptar líneas |
| O11 | MaxQuantityPerLine | NonNegativeInteger | 0 (deshabilitado) | Evita desbordamiento y stock negativo |
| O12 | IdempotencyWindowSeconds | NonNegativeInteger | 0 (deshabilitado) | Clave+fingerprint+tenant, ventana finita explícita |
| O13 | AllowedStationTypes | BoundedStringSet | conjunto vacío | Sólo estaciones allowlisted en la empresa/sede |
| O14 | CustomerConsentRequired | Boolean | true | Nunca ignora opt-out o revocación vigente |
| O15 | ConfigVersion | NullableVersion | null (no válida) | Exigir versión canónica aprobada antes de habilitar |
| O16 | ErrorRedactionMode | StrictOnlyEnum | Strict | Nunca incluir PII, secretos, token o stack en error |

Todos los límites no negativos **admiten cero como modo deshabilitado**, sin contradicción entre tipo y default. Ningún cero es "sin límite"; \`null\`, cadena vacía, NaN o configuración desconocida niegan la función. Un número positivo por sí solo tampoco habilita una función booleana falsa ni autoriza tenant.

## 4. Doce reglas fail-closed de validación y precedencia

| ID | Regla | Caso negativo futuro |
| --- | --- | --- |
| FG01 | Resolver configuración canónica por tenant server-side | EmpresaId manipulada desde UI habilita QR B |
| FG02 | Intersección deny-first de seis ámbitos | Sucursal activa flag global falso |
| FG03 | Versionado CAS y auditoría de cambios autorizados | Edición con versión antigua sobrescribe nuevo deny |
| FG04 | Revocación y desactivación prevalecen sobre caché | QR revocado continúa sirviendo menú privado |
| FG05 | Firma/TTL/replay de QR verificables en backend | QR falso/vencido/B adquiere permiso |
| FG06 | Menú público solo de catálogo publicado | Producto oculto o precio de B aparece |
| FG07 | Sesión y JWT/RBAC para mutaciones internas | Usuario sin rol cierra orden |
| FG08 | Capacidad de autoservicio separada del QR | Visitante con QR solo modifica líneas |
| FG09 | Idempotencia tenant+operación+payload | Retry con payload diferente duplica orden |
| FG10 | Estado/versión/transacción en pedido y cierre | Operador obsoleto cobra o cierra dos veces |
| FG11 | PII/secretos saneados en logs, errores y métricas | QR firmado/token se vuelca en log |
| FG12 | Falla de ERP/servicio deniega sin éxito aparente | Timeout muestra "pagado"/"cerrado" |

**Política de compatibilidad:** un cliente legado que no conoce estos flags mantiene el comportamiento existente sin habilitación implícita; no inyectar nuevos defaults en configuración real ni alterar rutas/DTO en esta microtarea. Una integración futura deberá verificar migración de configuración, clientes y rollback por SHA.

## 5. Diez pares de criterios positivos/negativos FUTUROS_NO_EJECUTADOS

| Caso | Positivo que deberá comprobar un test real | Negativo obligatorio en prueba futura |
| --- | --- | --- |
| T01 | Empresa A habilitada explícitamente recibe sólo su menú público | Empresa B/host ajeno no lee menú A |
| T02 | QR vigente verifica firma, tenant y mesa | QR falsificado/revocado o expirado rechaza |
| T03 | Operador con rol y sede correcta abre pedido | QR solo, usuario revocado o sede B no escribe |
| T04 | Configuración versionada aplica restricciones menores | Override local no derrota deny global |
| T05 | Replay idéntico conserva un único pedido | Payload distinto con misma clave genera conflicto |
| T06 | Cocina A transiciona estado autorizado una sola vez | Cocina B o rol quitado no cambia pedido |
| T07 | Caja A cierra con conciliación efectiva | Error intermedio y retry no duplican factura |
| T08 | Caché versionada invalida al revocar QR o tenant | Caché de B, sesión vieja o cierre terminal no habilitan |
| T09 | Lectura pública y UX teclado/móvil muestran estado auténtico | Offline/401/403/409 no muestra cierre/compra ficticios |
| T10 | Logs permiten correlación mínima sin secretos | PII, JWT y QR secreto no aparecen en errores |

**Ejecución en esta entrega:** T01–T10 = NO_EJECUTADOS; ningún test HTTP, MySQL 8.4, Angular/Playwright, JWT, proveedor ni cobro se corrió por escribir esta especificación. Cuando la implementación R2 sea autorizada, deberá usar suites causales reales, incluyendo pruebas tenant A/B, roles y revocación, concurrencia transaccional en MySQL aislado, Chromium cuando modifique UI y API negativa/positiva.

## 6. Evidencia, límites de cierre y rollback

- **A0:** un documento de esta unidad y sección autocontenida de CHANGELOG; sin archivos de runtime ni secretos. Revisar referencias y diff verdadero antes de publicar.
- **A1:** comprobar documentalmente seis ámbitos, O01–O16, FG01–FG12 y T01–T10 positivos/negativos marcados FUTUROS_NO_EJECUTADOS, no PASS de runtime.
- **A2:** aislamiento tenant A/B, QR falso/revocado, JWT/RBAC, consentimiento, idempotencia, precios y cierre seguro como requisitos estáticos, no políticas instaladas.
- **A3:** consistencia con M01.06; build/API/MySQL/EF/Playwright nuevas NO_APLICA por diff R0, sin atribuir pruebas futuras como ejecutadas.
- **A4:** commit dev individual fast-forward sobre HEAD esperado, Scope Lock y V5 procedencia completed/success del SHA, PLAN/COLA/DAG/BITACORA con readbacks, sin forzar G/O.
- **A5:** revisor de **otra** Task Javier distinta de DEV03. Puede rechazar contradicciones o ausencia de pruebas. A0–A4 y LISTO_DEV no equivalen a A5_PASS.
- **Rollback:** \`git revert\` normal del commit documental en dev; registrar nueva evidencia, preservar historial institucional. No modificar QA, main, PROD, R2, migraciones, permisos, DNS, datos reales, cuentas o proveedores por este contrato.

**Resultado de C08-02.M02.04:** borrador tipado y restrictivo para implementación futura de mesas/QR, no 16 flags vivos ni un sistema de pedidos autorizado.
