# SOLQARYN V5 FINAL — C11-03.M02.02: contratos internos, DTO, errores y compatibilidad de outbox

**Autor:** PRIMARY DEV 04 JAVIER, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. **Proyecto:** solqaryn/Solqaryn, exclusivamente dev, PROJECT_SCOPE_LOCK=STRICT. **Fuente:** STANDARD V5 FINAL, PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA, arquitectura canónica y código exacto de Git. **Tipo:** contrato documental de M02; NO endpoint nuevo ni dispatcher en ejecución.

## Evidencia real antes de definir contratos futuros

- Domain/MensajeOutbox.cs (blob 891572626c19cd6cf8f522fca5ec876333286ffb): EventoId GUID, EmpresaId, TipoEvento, PayloadJson, ClaveIdempotencia, TipoAgregado, IdAgregado, CorrelationId, Intentos, Estado y tiempos UTC; límites actuales de 160/65535/200/120/160/160 caracteres según campo. Los estados terminales son Entregado/DeadLetter.
- Application/MensajeOutboxService.cs (blob a00d353c474d7d7f785a58f443c66c5f12a542f7): RegistrarAsync(empresaId, RegistrarMensajeOutboxRequest, claveIdempotencia, cancellationToken). Verifica EmpresaId >0, Idempotency-Key presente y membresía server-side; devuelve intención existente si equivalente; conflicto para igual clave con contenido distinto; AddAsync no confirma la transacción.
- Application/Interfaces/IMensajeOutboxRepository.cs (blob a7dc258df9b1123dfe54ea85ba6d4dcc2a1eb478): AddAsync, lookup por EventoId/clave, ExisteClave, ClaimDisponiblesAsync por tenant con CAS requerido, RecuperarProcesandoStaleAsync, MarcarEntregadoAsync/RegistrarFalloAsync condicionados al intento. Implementaciones legacy pueden lanzar NotSupported para métodos default.
- Documento C11-03.M01.06 (blob fb068b1687e4c1d33fe2989ca9678ae3fb42e297): B01-B14, RB01-RB10 y pares P/N FUTUROS_NO_EJECUTADOS. C11-03.M02.01 describe entidades y ownership (documento en preparación concurrente; su estado no se presume LISTO/A5).
- DAG para este ID F=VALIDADO/G=SI y COLA G/O=SI, PLAN sin owner antes de reclamar; sólo habilita diseñar M02.02. Ninguna operación contractual se interpreta como API HTTP existente por aparecer en la tabla.

## Contratos de operación — propuestas, no rutas HTTP creadas

| ID | Operación y DTO futuro / contrato existente | Requisitos y salida esperada |
| --- | --- | --- |
| OP01 | Registrar intención, método existente RegistrarAsync | EmpresaId verificada por backend, clave idempotente no vacía, evento permitido; devolver MensajeOutbox o error seguro |
| OP02 | Recuperar intención por EventoId, interfaz existente | empresaId + GUID, recurso de otro tenant siempre 403/404 indistinguible |
| OP03 | Consultar intención por clave, interfaz existente | clave normalizada y tenant vinculados, sin retornar payload privado fuera de autorización |
| OP04 | ClaimDisponiblesAsync existente | tenant >0, fecha UTC, lote acotado, operación atómica con solo un ganador; prohibido simular CAS con lectura+escritura |
| OP05 | Recuperar stale existente | lease vencida y ausencia de worker vigente, comparación condicional del estado e intento |
| OP06 | MarcarEntregadoAsync existente | empresa, mensaje, intento y ACK durable coincidentes; falla en claim supersedido |
| OP07 | RegistrarFalloAsync existente | errorSeguro redactado, ahoraUtc y próximo intento UTC, límite finito, DeadLetter bajo política |
| OP08 | Estado de salud/diagnóstico de relay FUTURO | read-only por tenant con permisos, sin activar consumo ni revelar payload |
| OP09 | Revisión segura de DeadLetter FUTURA | no reintentar automáticamente ni revivir por acceso de lectura; aprobación y prueba aparte |
| OP10 | API pública de consulta/gestión FUTURA | no se define ruta ni permiso implícito; requiere diseño HTTP versionado y autorización explícita M03/M04 |

**DTO conceptual RegisterIntentV1:** TipoEvento obligatorio y allowlisted, PayloadJson opaco validado por esquema/tamaño antes de persistir, ClaveIdempotencia por encabezado/body contractualmente único, TipoAgregado/IdAgregado opcionales tenant-bound, CorrelationId opcional solo trazabilidad. EmpresaId, actor, rol, suscripción y autorización no se aceptan como hechos del caller. La entidad existente sigue siendo la fuente de límites; no se agregan DTO ni columnas nuevas aquí.

## Errores seguros y validaciones de contrato

| Código conceptual | Causa y política futura |
| --- | --- |
| E01 UNAUTHENTICATED | Falta identidad backend verificada; 401 según ruta |
| E02 MEMBERSHIP_REVOKED | Membresía revocada o inactiva; 403 |
| E03 TENANT_SCOPE_MISMATCH | Empresa de request no coincide con contexto server-side; 403/404 sin enumeración |
| E04 PERMISSION_DENIED | RBAC no acredita acción; 403 |
| E05 EVENT_TYPE_UNSUPPORTED | Tipo/versión no están en catálogo permitido; 400/422 contractual |
| E06 PAYLOAD_INVALID | Esquema inválido, deserialización segura o tamaño excedido; 400/413 según ruta |
| E07 IDEMPOTENCY_REQUIRED | Clave vacía/no normalizada; 400 |
| E08 IDEMPOTENCY_CONFLICT | Misma clave tenant con distinto contenido; 409 |
| E09 NOT_FOUND_OR_HIDDEN | Recurso ausente o B; 404 no enumerable |
| E10 CLAIM_CONFLICT | Lease actual pertenece a otro intento; 409 sin doble efecto |
| E11 NOT_YET_DUE | Backoff aún vigente; no ejecutar retry |
| E12 RETRY_LIMIT_EXCEEDED | DeadLetter/cuarentena, sin loop infinito |
| E13 PROVIDER_UNAVAILABLE | Fallo remoto, respuesta genérica sin host/token/cuerpo |
| E14 TRANSACTION_UNCONFIRMED | AddAsync sin commit confirmado; nunca publicar |
| E15 INVALID_TIME | Fecha no UTC o ventana inconsistente |
| E16 UNVERIFIED_GATE | Check cancelado/skipped/in_progress o A5 del autor; NO_PASS |

Los códigos E01-E16 son **propuestos** y no se declaran implementados como endpoints, HTTP status actuales o excepciones existentes. Las respuestas externas futuras deben conservar compatibilidad con ApiResponse/clientes Angular tras contrato exacto. CorrelationId no autentica, idempotency key nunca sustituye la autorización, y los errores nunca incluyen JWT, OTP, DSN, payload o PII.

## Versionado, compatibilidad y seguridad

No sustituir contratos Application existentes sin inventario de callers y regresión. Si se introduce VersionV1: coexistencia con emisores actuales, rechazo de tipo desconocido, migración aditiva y rollback probado. Las validaciones deben distinguir duplicado idéntico (no nuevo efecto) y conflicto (no escribir); comprobar repetición con dos consumidores, stale ACK, timeout y fallo parcial. Outbox y negocio necesitan commit atómico real si comparten causalidad; una llamada AddAsync aislada no lo prueba. Un relay no está autorizado por este documento, tampoco una nueva Task, webhook, scheduler o proveedor pagado.

## Oráculos futuros y cierre

**P01–P08 FUTUROS_NO_EJECUTADOS:** registrar tenant A válido, duplicado idéntico, lookup A, claim CAS exclusivo, ACK del intento actual, retry acotado, DeadLetter sin autoejecución y error redactado. **N01–N12 FUTUROS_NO_EJECUTADOS:** actor de B, rol revocado, tipo no permitido, payload excesivo, clave faltante, misma clave con payload distinto, doble claim, ACK stale, ahoraUtc no UTC, proveedor con secreto en excepción, rollback parcial y check cancelled/A5 propio. Ninguno es test ejecutado ni PASS.

A0: Scope Lock, Skill, PLAN/COLA/DAG y exclusión runtime. A1: OP01–OP10, E01–E16, P08/N12 documentados y coherentes con código. A2: fail-closed JWT/RBAC, A/B, lease/idempotencia, privacidad y errores. A3: referencias Domain/Application/repository y contrato M01; builds/API/MySQL son NO_APLICA al diff Markdown, no PASS de runtime. A4: único Markdown + CHANGELOG autocontenido, fast-forward/CAS, blobs, Scope Lock y procedencia V5 exact-SHA, BITACORA/PLAN/COLA readback. A5: revisión independiente de otra Task Javier. Rollback con git revert normal; sin QA/main/PROD, migración, evento, DNS, secretos o datos productivos.
