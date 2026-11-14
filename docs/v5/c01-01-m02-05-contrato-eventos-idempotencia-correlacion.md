# SOLQARYN V5 FINAL — C01-01.M02.05: eventos, idempotencia, correlación y límites de identidad universal

**Autor:** PRIMARY DEV 04 — JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Clase:** diseño contractual documental; `PROJECT_SCOPE_LOCK=STRICT`; repositorio `solqaryn/Solqaryn@dev`. **No implementa eventos**, webhooks, Tasks que se llaman entre sí, workers, nueva API, trabajos programados ni outbox ejecutable. No modifica BD ni código real.

**Resultado exigido:** definir eventos de identidad, claves de idempotencia, correlación, límites de concurrencia y propiedad multiempresa, sin transformar la trazabilidad en permiso de acceso. La guía fuente única es STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` y el Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.

## 1. Dependencias verificadas y prohibiciones

Para M02.05, `C01-01.M01.01–06` y `C01-01.M02.01–04` figuran LISTO; los tres cierres de apoyo `C00-01.M06.06`, `C00-03.M01.06` y `C00-03.M06.06` también. `DAG_TECNICO!F142=VALIDADO/G142=SI`; `PLAN_MAESTRO!H156=PENDIENTE/I156` vacía y `COLA!G156/O156=SI` antes del claim CAS exclusivo. M02.04 cerró como diseño en SHA `84323ffb5b2f110b6d02624db76af63309d1b2cb`, con A5_PENDIENTE en otra Task autorizada. Un LISTO de diseño no concede autoridad para implantar entornos, webhooks, IAM, JWT, RBAC, MySQL ni un runtime de eventos.

**No implementar eventos todavía:** este documento describe únicamente el futuro contrato. No crear dispatcher, suscripción, endpoint que reenvíe tareas, eventos de red, scheduler, job, tabla ni integración con un proveedor. Ninguna llamada manual entre Tasks se interpreta como servidor infinito ni genera un RUN_ID nativo.

## 2. Fuentes de código y límites probatorios

| Fuente en dev y blob exacto | Contrato observado | Límite |
| --- | --- | --- |
| `backend/src/API/Middleware/CorrelationIdMiddleware.cs` — `3b76dca41bb4bc0b44f89c2a4664c79907f9f242` | `X-Correlation-ID` máximo 64 caracteres seguros ASCII, log scope y TraceIdentifier; entrada inválida se sustituye por GUID | ID correlativo no es autenticación, autorización, idempotencia durable ni `EmpresaId` |
| `backend/src/API/Middleware/RequestObservabilityMiddleware.cs` — `4fdb711c558750dfaf8a9c415f2f4058cbb4f336` | Métrica método/estado/duración, sampling y baseline opcional | Log o traza no confirma entrega de evento ni permiso; no loguear payload, token ni PII |
| `backend/src/Domain/Entities/MensajeOutbox.cs` — `891572626c19cd6cf8f522fca5ec876333286ffb` | Mensaje con `EmpresaId>0`, `EventoId`, `ClaveIdempotencia`, `CorrelationId`, estado e intentos; límites tipados de longitud | Clase de dominio no prueba persistencia, claim multiworker ni dispatcher habilitado |
| `backend/src/Application/Services/MensajeOutboxService.cs` — `a00d353c474d7d7f785a58f443c66c5f12a542f7` | Verifica `IUsuarioScopeService.ObtenerActualAsync(empresaId)`, idempotency key, compara misma solicitud y rechaza conflicto | La atomicidad entre lookup y Add la debe demostrar constraint/transacción futura; no inferir idempotencia global |
| `backend/src/Application/Services/OutboxRetryPolicy.cs` — `a9bcacda24a7ba031a271b527d8ca2043a8510b` | Reintentos deterministas, máximo predeterminado 5 y permitido 100, jitter acotado, dead letter | Política pura no ejecuta ni acredita envío, deduplicación durable o retry en vivo |

Estos cinco blobs existen. Se inspeccionaron sus contratos fuente; no se ejecutaron pruebas HTTP, C#, MySQL, servicios reales ni mensajes de proveedor por esta microtarea. Las pruebas de entrega y compensación serán obligaciones de futuras unidades ejecutables.

## 3. Esquema de evento propuesto, NO publicado

| Campo conceptual | Validación y dueño | Prohibición |
| --- | --- | --- |
| `EventoId` | GUID único de evento técnico, generado server-side en contexto verificado | No utilizar ID proporcionado por cliente como prueba de autorización |
| `EmpresaId` | Entero positivo y resuelto desde membresía activa y scope, owner del evento | Un evento A no puede consumir o modificar datos B por header, caché o payload |
| `TipoEvento` | Nombre normalizado y registrado en catálogo aprobado, máximo del contrato outbox (160) | Tipo desconocido no crea acciones ni abre módulos o grants |
| `TipoAgregado`, `IdAgregado` | Recurso real y owner efectivo, límites 120/160 si aplica | No correlacionar un agregado de B en evento A |
| `ClaveIdempotencia` | Cadena normalizada y acotada a 200, unicidad lógica por (`EmpresaId`, clave) | Reutilizar clave con payload diferente debe ser conflicto, nunca ejecutar doble |
| `CorrelationId` | Propagar ID de middleware HTTP válido o valor generado, separado de clave idempotente | No usar correlación como key de deduplicación ni como secreto |
| `CreadoEnUtc`, `DisponibleDesdeUtc` | UTC verificable, reloj y persistencia efectivas de futuros workers | No fabricarlos desde la respuesta del navegador o fecha local |
| `Estado`, `Intentos` | Pendiente → Procesando → Entregado o DeadLetter con límites y actualización condicional | No marcar Entregado antes de ack real o tras fallo de red; no reintento infinito |
| `VersionContrato` | Campo propuesto de compatibilidad futura, sujeto a aprobación y migración | No afirmar que existe hoy en MensajeOutbox ni introducirlo en este cambio |

**Claves distintas:** `EventoId` identifica el mensaje; `ClaveIdempotencia` evita duplicación de intención por tenant; `CorrelationId` une trazas sin otorgar acceso. Un solo intento de publicación o timeout no demuestra si el receptor confirmó; futuros consumidores deben deduplicar igualmente mediante una clave duradera y transacción verificable.

## 4. Catálogo mínimo de eventos de identidad propuesto (NO emitido)

| Evento de diseño | Causa válida futura | Precondición de seguridad y consumidores |
| --- | --- | --- |
| `Identidad.MembresiaActivada` | Membresía Usuario–Empresa activa, cambio transaccional confirmado | Owner empresa y rol de esa empresa, permisos para alta; consumidores solo del tenant |
| `Identidad.MembresiaRevocada` | Revocación confirmada y versionada | Invalidar permisos/caché de la empresa afectada; no reactivar por evento duplicado |
| `Identidad.RolEmpresaActualizado` | Cambio de rol con comparador de versión válido | Verificar que rol pertenece a EmpresaId; no elevar rol por payload |
| `Identidad.SucursalAsignada` | Sucursal asignada a EmpresaId validado | Reasignación requiere flujo independiente, auditoría y control cruzado |
| `Identidad.ContextoInvalidado` | Revocación/bloqueo/sesión caducada verificados | Forzar nueva evaluación de membresía/RBAC, no otorgar token nuevo |
| `Identidad.ConfiguracionActualizada` | Cambios de configuración tenant autorizados con CAS | Cache particionada por empresa y version, nunca compartir settings privados |

**Estos nombres son propuestas** y no enum/eventos en el código actual. No hay handlers ni publicaciones autorizadas por esta unidad. El documento no propone eventos de IAM para duplicar trabajo de capítulos posteriores.

## 5. Concurrencia, entregas e idempotencia futura

1. **Futuro writer autorizado:** autenticar JWT, resolver `UsuarioEmpresa` y RBAC, validar `EmpresaId` de agregado y persistir cambio de negocio + intención outbox en una misma transacción real, **solo tras una microtarea y pruebas MySQL pertinentes**. Si falla transacción, no dejar intención huérfana.
2. **Future claim worker:** tomar un solo evento elegible con CAS/lease atómico, owner de tenant, `EventoId` y versión, registrando tiempo y recuperando lease expirado con prueba. Dos trabajadores nunca pueden despachar simultáneamente el mismo mensaje. Este documento **no crea workers**.
3. **Entrega al menos una vez, no exactamente una vez garantizada:** después de confirmación externa marcar entregado; timeout con ack desconocido exige deduplicación por receptor. Error permanente/violación tenant pasa a DeadLetter o STOP sin repetición insegura; reintentos limitados y jitter acotado por la política existente.
4. **Monitoreo:** retener evento, idempotency fingerprint y SHA causal, no payload sensible. Telemetría de `TAREAS_PROGRAMADAS!AA:AN` es autoinforme de corrida, **no** runtime nativo de workers ni sustituto de BITACORA institucional.
5. **Rollback futuro:** revertir la emisión sin borrar evento; compensación auditada por owner. Nunca `force-push`, `reset`, borrar BITACORA histórica, borrar registros reales ni usar JOBs de producción no autorizados.

## 6. Casos de aceptación documentales con positivos y negativos

| Caso | Positivo propuesto | Negativo prohibido |
| --- | --- | --- |
| EVT01 | Actor U1 con membresía activa A y permiso produce intención solo A | U1 sin B emite event B por header/payload: DENY |
| EVT02 | Dos reintentos misma clave U/A y payload igual devuelven intención existente | Misma clave U/A con payload diferente: CONFLICT |
| EVT03 | Cliente envía correlation válido ≤64 caracteres, backend lo usa en trazas | Header con control/PII o >64: reemplazar por ID seguro |
| EVT04 | Worker futuro recibe claim exclusivo antes de efecto | Dos workers adquieren mismo evento simultáneo: FAIL |
| EVT05 | Confirmación del receptor genera estado Entregado una sola vez | Timeout sin ack se reporta como Entregado: FAIL |
| EVT06 | Retries exponenciales + jitter/limite y dead letter final | Sin límite, flood de proveedor, reintento de permiso denegado: FAIL |
| EVT07 | Revocación de membresía invalida datos de tenant A afectados | Cache B reutiliza evento, rol, permiso o correlación de A: DENY |
| EVT08 | Cambio de configuración autorizado conserva version CAS y owner | Futura respuesta atrasada sobrescribe configuración nueva: CONFLICT |
| EVT09 | A5 por otra Task Javier revisa contrato documental sin autoaprobación | Task autora aprueba A5 o crea otra Task por un evento: DENY |
| EVT10 | Evento conserva ID idempotente y evidencia en BITACORA 26 columnas | Duplicar EVENT_ID, truncar 21 columnas o inventar RUN_ID: FAIL |
| EVT11 | Worker futuro falla cerrado si backend/DB/proveedor no prueba atomicidad | Mock/solo docs se presentan como test live MySQL/HTTP: NO_GO |
| EVT12 | M02.06 verifica precedencia y DAG antes de permitir fase nueva | M02.05 LISTO libera IAM/JWT/DB/QA por sí misma: DENY |

Estos doce casos son criterios conceptuales. No se presentarán como doce pruebas unitarias .NET, MySQL, E2E o runtime ejecutadas.

## 7. Auditorías aplicables, compatibilidad y rollback

**Archivos afectados:** exclusivamente este Markdown y sección agrupada de `CHANGELOG_AI.md`. `requiere_dotnet=NO`; `requiere_node_angular=NO`; `requiere_mysql=NO`; `requiere_docker=NO`; `requiere_e2e=NO`; `requiere_security=SI_DOCUMENTAL`; `requiere_scope_lock=SI`.

- **A0:** validar cinco source hashes, contrato sin secretos, diff y encabezados de CHANGELOG sin intercalado.
- **A1:** verificar documentos EVT01–EVT12 con oráculos positivos/negativos.
- **A2:** JWT, RBAC, tenant A/B, revocación, permisos y PII, sin crear código ejecutable ni proveedor.
- **A3:** 13 predecesores, `DAG_TECNICO!F142=VALIDADO/G142=SI`, PLAN/COLA, compatibilidad con MensajeOutbox sin pretender entrega real.
- **A4:** publicar FF directo a dev, readback árbol/diff SHA, Gate Scope Lock y V5 CHANGELOG `completed/success` exact-SHA y BITACORA/PLAN/COLA.
- **A5:** revisión independiente por otra de las cinco Tasks Javier, registrar A5_PENDIENTE antes de LISTO_DEV; no QA/main/PROD hasta autorización adicional.
- **Rollback:** revertir commit documental con `git revert` si se invalida contrato, append correctivo idempotente; no crear PR/ramas ni modificar reglas de trabajo.

**No autoriza:** M02.06 (gate especial), M03 código, nuevo API, worker, dispatcher, proveedor, QA, main, producción, secretos ni cambios sobre Tasks.
