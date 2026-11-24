# C06-04.M03.05 — Portal de cliente: frontera segura de despacho (incremento R2)

Autor: SOLQARYN PRIMARY DEV 01 JAVIER. Fuente V5 FINAL; dev exclusivo; PROJECT_SCOPE_LOCK=STRICT.

## A0 y alcance

Se incorpora una frontera Application `C0604PortalWorkDispatchBoundary` con puerto `IC0604PortalWorkQueue` y pruebas xUnit. Consume el gate existente `C0604PortalClienteAuthorizationGate`, cuya propiedad `GrantsWrite` es siempre falsa. No se registra un puerto en DI, no se crea API, worker alojado, cron, outbox MySQL, proveedor externo, email, pedido, reserva, pago o despliegue. El código no expone PII ni credenciales. La cola real queda deliberadamente **sin activación**, porque no existe una prueba de autorización runtime firmada y deduplicación persistente para este scope. Esta entrega es un checkpoint auténtico de código seguro, **no M03.05 terminado**.

## A1–A3

Se agregan xUnit con denegaciones sobre operaciones no permitidas, claves idempotentes inválidas/hostiles, UTC ausente, versión inválida, roles autoatribuidos, solicitud de escritura o entrega externa, error de evidencia y cancelación; se verifica que no llega ningún enqueue al puerto. El camino nominal también deniega en ausencia de permisos vigentes, incluso si la información estructural es aparentemente favorable. Las pruebas de backend y seguridad deben ejecutarse en GitHub Actions del SHA causal. Estas pruebas son in-process, NO_APLICA para MySQL/HTTP/Chromium de este diff, pero el cierre del scope R2 completo **sí exige** integración real: JWT CUSTOMER, tenant A/B, RBAC, ownership, revocación, CAS durable, idempotencia atómica, retry/dead-letter y worker con proveedor auditado. No simular un PASS.

## A4, A5 y reversión

Verificar Scope Lock, V5, .NET Release/xUnit y seguridad en el SHA exacto con logs, además de readback de PLAN/COLA/BITACORA. Mantener VALIDANDO mientras la integración productiva autorizada y los criterios P1 permanezcan sin acreditar. A5 por otra Task Javier distinta. Revertir con commit Git normal conservando trabajos concurrentes e historial, nunca force-push ni borrar registros.

## V22 — Corrección adversarial de cancelación posterior al await (2026-11-20)

Una fuente confiable puede completar una lectura después de que el llamante cancele, sin lanzar excepción. El límite Application comprueba ahora el CancellationToken inmediatamente después del preflight asíncrono y antes de decidir si una operación puede despacharse; un test xUnit reproduce la carrera (cancelación al leer evidencia), exige OperationCanceledException y verifica cero llamadas a la cola. El test falla con la versión anterior al cambio. No se activa cola, adaptador ni escritura y no se afirma integración JWT/HTTP/MySQL real por este diff aislado. El cierre global R2 sigue pendiente de autenticación CUSTOMER real, control de tenant A/B, idempotencia durable y worker probado. Scope Lock, V5, backend Release/xUnit y seguridad requieren GitHub Actions SUCCESS en el SHA publicado; A5 por otra Task Javier; no QA/main/PROD.

## V22 — incremento MySQL 8.4 durable y aislado (2026-11-20)

La clase Infrastructure `C0604IsolatedMySqlDurableQueue` implementa SQL InnoDB real pero queda **sin DI, sin HTTP, sin scheduler, sin migración productiva y sin proveedor**. La tabla `v22_c0604_portal_work` se crea y elimina únicamente en el servicio MySQL 8.4 efímero de GitHub Actions. Se verifica deduplicación atómica `UNIQUE(tenant_id,idempotency_key)`, colisiones de payload, aislamiento por empresa A/B, lease único con `FOR UPDATE SKIP LOCKED`, rechazo de ACK de otro tenant/worker, lease vencido, reintentos y DEAD al agotar intentos. El test de integración genérico sin variable no acredita MySQL; solo el workflow dirigido exige MySQL 8.4 y TRX real con cero fallos.

La propiedad `trustedTenantId` **no autentica al cliente**: antes de conectar el adaptador de runtime siguen siendo obligatorias sesión firmada CUSTOMER/JWT, verificación de pertenencia/RBAC y propiedad de recurso, migración controlada, proveedor/worker con entrega verificable, auditoría, pruebas HTTP/tenant A/B/revocación, rollback, CI exact SHA y A5 independiente. Este cambio no acredita la fase M03.05 completa ni autoriza el despacho a clientes.

## V22 - Reparacion real P2: lease final expirado sin ACK (2026-11-20)

La cola aislada tenia un defecto de recuperacion: TryLeaseAsync filtraba attempts < maxAttempts, de modo que un worker que moria tras recibir el ultimo intento dejaba para siempre la fila LEASED cuando vencia el tiempo, en vez de registrar DEAD. El nuevo UPDATE InnoDB, dentro de la misma transaccion y condicionado por tenant_id, marca DEAD exclusivamente los ultimos leases caducados; no reenvia nada al proveedor ni rebaja la proteccion de tenant.

La prueba adversarial real MySQL 8.4 reserva tenant 15, simula caida sin ACK en el intento final, verifica que tenant 16 no modifica esa fila, rechaza ACK tardio y exige DEAD/attempts=1 con cero redespachos. Antes de la correccion, la expectativa de estado DEAD fallaba; despues debe pasar en GitHub-hosted con C0604_MYSQL84_TEST y el TRX dirigido. Sigue sin existir integracion CUSTOMER JWT/RBAC, API HTTP y worker externo: VALIDANDO; NO LISTO/A5 hasta completar A0-A4 globales y revision distinta. Rollback: revert normal de este commit sin tocar datos reales ni historial.

## V22 — P2: ACK antiguo del mismo worker no puede cerrar otro lease (2026-11-20)

La reentrega podia asignarse al MISMO `workerId` tras vencimiento del lease original; `SettleAsync` solo verificaba tenant, workId, workerId y vencimiento actual, lo que permitia que un ACK rezagado del primer intento marcara DONE el segundo intento. Se exige ahora `expectedAttempt` obtenido de `LeasedWork.Attempt` y CAS SQL InnoDB `attempts=@attempt` en el mismo UPDATE de resultado. No se acepta intento 0, fuera de 1..10 ni mayor que maxAttempts; el worker solo puede confirmar la entrega que realmente adquirio. No se añade HTTP, DI, proveedor, scheduler ni dato productivo.

Prueba adversarial dedicada MySQL 8.4 efimero: tenant 17 reserva, recibe lease #1, caduca, `worker_shared` readquiere lease #2, ACK obsoleto de #1 retorna false y ACK #2 retorna true, estado durable DONE/attempts=2; las regresiones de tenant 11/12/14/15 y dead-letter siguen activas. El codigo anterior permitiria el ACK obsoleto. Riesgo R2; exigir GitHub Actions exact-SHA C0604_MYSQL84_TEST, 0 FAIL/0 SKIPPED, Backend Release, seguridad, Scope Lock y V5. Mantener VALIDANDO: siguen faltando JWT CUSTOMER/RBAC, HTTP, worker/proveedor real y auditoria para A0-A4 global; A5 independiente, sin QA/main/PROD. Rollback por revert normal con HEAD revalidado y bitacora preservada.

## V22 — R2: clave de idempotencia con alias de colacion (20 noviembre 2026)

El indice InnoDB `utf8mb4_0900_ai_ci` considera iguales `request_0001` y `REQUEST_0001`, aunque no sean la misma solicitud exacta. Antes `ReserveAsync` comparaba recurso, operacion y version, pero omitia la clave de idempotencia original: podia afirmar `Duplicate` para una clave distinta. Ahora la lectura transaccional `FOR UPDATE` incorpora `idempotency_key` y exige comparacion ordinal; un alias retorna `Conflict`, sin atribuirse el trabajo de otra solicitud. El test fija la colacion adversarial en MySQL efimero y falla sobre el codigo previo. No hay nueva tabla de produccion, DI, JWT, HTTP, worker ni proveedor real. Seguir VALIDANDO con A0-A4 globales incompletas y A5 independiente pendiente. Verificar en GitHub Actions job dirigido MySQL 8.4, Backend Release, Scope Lock, V5 y seguridad exact-SHA; NO certificar RUN pendiente. Rollback: revert normal.

## V5 — R2: identidad exacta del titular de lease (24 noviembre 2026)

La tabla InnoDB de la cola aislada usa una colación insensible a mayúsculas. El CAS de `SettleAsync` ahora compara el titular persistido y el parámetro como cadenas binarias, además de tenant, ID, intento y vencimiento; `WORKER_A` no puede confirmar el lease de `worker_A`. La regresión en el fixture MySQL 8.4 mantiene los negativos de tenant y de intento y verifica que el titular original todavía puede completar el flujo. No se activa DI, HTTP, JWT CUSTOMER, proveedor ni entrega externa. CI dirigida exact-SHA y A5 independiente requeridas; conservar VALIDANDO mientras falte la integración del worker/proveedor. Reversión normal por Git.

## V5 — R2: recuperación durable al reducir el límite de reintentos (24 noviembre 2026)

Si un trabajo PENDING ya consumió tantos intentos como el nuevo límite, el selector anterior lo excluía indefinidamente sin llevarlo a DEAD. La limpieza transaccional InnoDB ahora incluye PENDING agotados del tenant autorizado además de leases vencidos; los leases vigentes y los otros tenants quedan intactos. La regresión del fixture MySQL 8.4 existente crea trabajos de dos tenants con NACK y reduce el límite solo para uno, verifica DEAD, ausencia de redespacho al volver a elevar el límite, PENDING ajeno y conservación/ACK del lease vigente. A0: cambio acotado de Infrastructure, prueba y documentación, sin esquema, API, DI, JWT/RBAC, proveedor ni datos productivos. No se afirma PASS de la nueva CI hasta ejecución real dirigida exact-SHA. El alcance global sigue VALIDANDO; falta integración CUSTOMER JWT/RBAC/ownership y worker/proveedor autorizado. A5 independiente; rollback normal de Git.
