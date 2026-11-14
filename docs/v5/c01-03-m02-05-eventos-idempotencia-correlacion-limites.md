# SOLQARYN V5 FINAL — C01-03.M02.05: eventos, idempotencia, correlación y límites de configuración dinámica

**Clase:** contrato DOCUMENTAL M02.05, sin ejecución de eventos. **Autor:** PRIMARY DEV 06 — JAVIER, Task `6af16bbad26881918c283abe06e1f32a`. **Rama y ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO/COLA/DAG_TECNICO de `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.

## 1. Alcance real y no-alcance

Se define **sólo el protocolo futuro** de eventos, idempotencia y trazas para cambios de configuración dinámica tipada en el bloque C01-03. Esta especificación **no** instala un event dispatcher, webhook, broker, worker, outbox, endpoint, migración EF/MySQL, nueva Task, scheduler ni ejecución entre Tasks. M02.06 evalúa el cierre técnico de lote y las M03 implementan código por separado, sujeto a sus propios DAG, seguridad y CI. La descripción de un evento no acredita publicación, entrega, persistencia, aislamiento runtime o éxito de tests.

Los contratos complementarios ya presentes son M02.01 (entidades y owner), M02.02 (API/DTO/errores), M02.03 (persistencia y fixtures) y M02.04 (claves tipadas y ámbitos). No se reemplazan los recursos existentes `ConfigEmpresa` (operación por tenant) ni `EmpresaConfiguracion` (legado de presentación global); un registro legacy sin owner no puede emitir un evento privado atribuido a un tenant sin reconciliación demostrada.

## 2. Fuente de autoridad, invariantes y límites de confianza

1. La identidad efectiva se resuelve **en servidor** por actor autenticado, membresía activa, empresa y sucursal/recurso verificados, permiso RBAC para la clave/operación, catálogo allowlisted, versión esperada y entitlement cuando corresponda. Todos son restricciones acumulativas.
2. Ningún `tenant_id`, `EmpresaId`, host, slug, cookie, header, DTO, plan o feature flag del cliente otorga permiso ni es una fuente de owner. Los contextos A y B no comparten claves de deduplicación, resultados, trazas o cargas de eventos.
3. La modificación futura aplica CAS a la versión de valor dentro de la misma transacción que el registro idempotente. No se declara éxito antes de commit. Si no existe transacción/outbox real, **no se emite un evento asíncrono**.
4. Los rechazos de autorización, conflicto, validación y versión obsoleta no originan mensajes con valores privados; se registran sólo como telemetría segura si existe una autorización independiente de observabilidad. Nunca se retransmite un secreto o JWT.
5. Una revisión fallida, excepción o timeout no puede causar actualización parcial ni doble efecto; una clave de idempotencia sola no es autorización ni garantiza consistencia transaccional.

## 3. Sobre conceptual de evento — contrato, NO payload desplegado

| Campo propuesto | Requisito verificable | Rechazo seguro |
| --- | --- | --- |
| `eventId` | Identificador opaco único, generado por servidor tras commit; versión de esquema separada | Duplicación y reutilización de ID |
| `eventType` | Enum cerrado, p. ej. `ConfigChangeCommitted.v1`; sólo evento de cambio confirmado | Tipo arbitrario, deserialización polimórfica o ejecución |
| `occurredAtUtc` | Timestamp UTC del commit, no hora de navegador | Zona u hora de cliente como autoridad |
| `correlationId` | Identificador opaco acotado, validado/regenerado en borde; vincula petición, auditoría y resultado | CRLF, longitud excesiva, token, usuario o PII |
| `scopeKind` / `scopeOwnerRef` | Ámbito tipado y referencia **verificada por servidor**, no identificador sensible público | Owner solicitado por cliente o valor global atribuido a tenant |
| `keyCode` | Clave canónica de catálogo allowlisted; sin valores sensibles | SQL/HTML/expresión/ruta libre |
| `revisionBefore` / `revisionAfter` | Revisión monotónica: la posterior es mayor sólo tras commit | Cambio con versión obsoleta o revisión inventada |
| `actorRef` | Referencia seudonimizada de actor autorizado, nunca credencial | JWT, correo, secreto o perfil íntegro |
| `idempotencyDigest` | Huella servidor ligada a actor+tenant+operación+clave+request canónico | Exponer clave original o usar huella compartida entre empresas |
| `schemaVersion` | Versión explícita para compatibilidad y rechazo de versiones desconocidas | Consumir versiones futuras en modo permisivo |

**No incluir** valor privado antes/después, payload SMTP, material criptográfico, archivos, datos bancarios, PII, cabeceras de autenticación ni stack traces. Si un consumidor legítimo necesita un valor, debe leerlo por una API autorizada y versionada, nunca recibirlo por un evento común. Eventos hipotéticos no conceden permiso a consumidores y no modifican estados institucionales.

## 4. Idempotencia y concurrencia de una futura operación

Se propone un protocolo `(actorServidor, tenantServidor, operacionCanonica, keyCode, idempotencyKey)` con huella del cuerpo **canónico y versionado**, que incluya `expectedVersion` y tipo. El servidor valida pertenencia y permiso **en cada intento**, incluso al reproducir una respuesta anterior. La clave del cliente se trata como identificador opaco con cota de longitud y caracteres; no es secreto ni reemplaza JWT/RBAC. Su almacenamiento sólo se autorizaría con hash/HMAC y política documentada de TTL, limpieza y protección de replay, aprobadas en la implementación.

| Caso | Resultado contractual futuro |
| --- | --- |
| Primer cambio válido y versión vigente | Una operación atómica, una revisión nueva, una entrada idempotente y a lo sumo un evento confirmado |
| Misma clave + misma huella + mismo actor/tenant autorizado | Reutilizar resultado autorizado, sin segunda modificación ni evento |
| Misma clave + distinta huella | Conflicto estable (HTTP 409 conceptual), sin mutación |
| Mismo texto de clave desde tenant B | **No** reutilizar resultado del tenant A; comprobar aislamiento y derechos de B |
| `expectedVersion` obsoleta | Conflicto sin escribir; mostrar revisión sólo si está autorizada |
| Membresía revocada/usuario bloqueado después del primer resultado | Denegar nuevo intento; no reproducir datos privados de respuesta cacheada |
| Timeout o caída antes de commit | No afirmar aplicado; reintento consulta estado transaccional y conserva consistencia |
| Caída después de commit, antes de entrega hipotética | Outbox durable futuro con entrega al menos una vez + consumidor idempotente; no prometer exactly-once |

**Límites propuestos, no activados:** longitud máxima de clave y correlación, tamaño de envelope, número de reintentos, TTL idempotente, retención auditada, paralelismo por scope y cuotas tenant deben fijarse contra métricas y threat model antes de M03/M05. Ante límite desconocido o agotado: rechazar de forma segura o diferir, nunca abrir escritura ilimitada.

## 5. Correlación, auditoría y privacidad

- `correlationId` de servidor se asocia a un `operationId` interno y a `revisionAfter` únicamente en sistemas autorizados. Las trazas públicas no revelan la empresa de terceros, token, cuerpo completo o datos del actor.
- Un evento de cambio de configuración **no equivale** a `BITACORA` institucional: esta última registra estados de microtareas A:Z con EVENT_ID/IDEMPOTENCY_KEY y es append-only; la cola deriva de PLAN/COLA/DAG, no de mensajes futuros.
- Las consultas cross-tenant deben responder 403/404 no enumerativo según contrato aprobado; errores de validación se reducen a códigos seguros y correlación no sensible. Los logs son estructurados, con redacción y retención acotada; ningún log sustituye a una prueba de tenant A/B real.

## 6. Oráculos reproducibles — inspección documental, no ejecución

| ID | Clase | Entrada simbólica | Resultado exigido |
| --- | --- | --- | --- |
| P01 | Positiva | Empresa A, actor A autorizado, clave allowlisted, versión vigente | Un commit hipotético y evento sólo post-commit |
| P02 | Positiva | Repetir mismo request idempotente desde A, sesión vigente | Mismo resultado sin duplicar revisión |
| P03 | Positiva | CorrelationId válido, auditoría autorizada | Referencia correlacionada sin datos privados |
| P04 | Positiva | Consumidor futuro reintenta tras crash post-commit | Deduplicación por eventId/tenant/version sin segundo efecto |
| N01 | Negativa | Tenant B intenta leer resultado idempotente de A | Denegado; sin resultado ni caché de A |
| N02 | Negativa | Misma clave con distinta huella | 409 conceptual, cero mutaciones |
| N03 | Negativa | Rol revocado pero clave idempotente previamente aceptada | Denegado tras nueva verificación |
| N04 | Negativa | Versión esperada vencida | 409 sin evento |
| N05 | Negativa | Payload con SQL/JS/CSS libre o clave fuera de allowlist | Rechazo antes de persistir |
| N06 | Negativa | Referencia SMTP/JWT/PII en payload de evento | Prohibido; redacción/fallo cerrado |
| N07 | Negativa | CorrelationId con CRLF o tamaño excesivo | Rechazar/regenerar sin inyectar logs |
| N08 | Negativa | Timeout antes de commit o outbox ausente | No reportar éxito ni emitir evento |
| N09 | Negativa | Tenant o actor del cliente suplanta identidad de servidor | No elevar permisos; resolver contexto confiable |
| N10 | Negativa | Consumidor de versión desconocida o repetición de eventId | No ejecutar efecto; fallo seguro |

Estos son **criterios de aceptación futuros**, no un reporte de 14 pruebas ejecutadas. Su validación runtime requerirá tests unitarios, API positivos/negativos, integración MySQL transaccional y pruebas de aislamiento A/B bajo los scopes de implementación autorizados.

## 7. Dependencias, release y reversión

**Preflight de esta especificación:** `C01-03.M01.01–06` en LISTO con A5 independiente bajo allowlist Javier; `DAG_TECNICO` ID exacto con `F=VALIDADO/G=SI`; `PLAN_MAESTRO!H228` y `I228` reclamados exclusivamente mediante transición condicional; `COLA!G/O=SI` antes del claim. M02.01–04 proporcionan contratos, **no** autorizan escritura runtime.

**A0–A4 documental:** integridad de diff, ausencia de secretos o sintaxis ejecutable, consistencia con M02.01–04 y oráculos simbólicos P/N, Scope Lock y CHANGELOG exact-SHA, publicación fast-forward, BITACORA A:Z y PLAN/COLA readback. Tests backend/MySQL/UI no pertinentes al cambio Markdown son `NO_APLICA` motivado; no se cuentan como PASS. El cierre dev sólo permite `A5_PENDIENTE` y revisión independiente por otra Task Javier antes de promoción. **Rollback:** `git revert` normal del commit documental con nueva evidencia, sin borrar BITACORA; cualquier implementación futura tendrá rollback propio de código/datos. Sin QA/main/PROD ni nuevos ejecutores.
