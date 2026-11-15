# SOLQARYN V5 — C11-03.M02.04: configuración tipada, ámbitos y flags seguros del outbox

**Microtarea:** C11-03.M02.04. **Autor:** PRIMARY DEV 02, TASK_ID 6af16bb9492c8191b8e8c7d606f11507, cuenta JAVIER_CHATGPT. **Repositorio:** solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL; PLAN_MAESTRO/COLA/DAG_TECNICO; controles A0–A5.

## 1. Alcance y estado real

La entrega especifica opciones, límites, procedencia y validaciones **conceptuales**, no modifica configuración de ambientes, appsettings, contenedores, workers, servicios hospedados, endpoints, JWT/RBAC, migraciones o proveedores. No activa nuevos eventos entre Tasks ni un dispatcher permanente. El hecho de documentar una opción NO equivale a que exista implementada, enlazada a DI, probada con MySQL o habilitada por operador.

Fuentes de código y contrato contrastadas en dev: Domain/Entities/MensajeOutbox.cs (identidad EmpresaId, EventoId, ClaveIdempotencia y fechas de estado); Application/Services/MensajeOutboxService.cs (validación de pertenencia y equivalencia idempotente); Application/Interfaces/IMensajeOutboxRepository.cs (claim y confirmación condicional); Application/Services/OutboxRetryProcessor.cs (rechazo de lotes mezclados, retry acotado); docs/v5/c11-03-m02-01-entidades-ownership-invariantes-outbox.md (14 elementos e invariantes I01–I16); docs/v5/c11-03-m01-06-gates-rollback-outbox-eventos.md (B01–B14 y RB01–RB10). El contrato M02.03 documenta proyecciones de persistencia, pero NO crea índices ni migraciones.

La arquitectura real sigue en ASP.NET Core 10, Domain <- Application <- Infrastructure, API como borde; EF Core 10.0.12 y MySql.EntityFrameworkCore 10.0.9 bajo control del repositorio. La base y credenciales de ejecución no se leen ni se modifican para esta microtarea.

## 2. Superficie propuesta de configuración F01–F14

Las claves son un **esquema de contrato propuesto** para evaluación futura. No son nombres de variables de entorno existentes ni una orden de incorporarlas a producción. Todo valor ausente, ambiguo o fuera de rango debe provocar denegación de la acción privilegiada, no adoptar defaults permisivos.

| ID | Opción conceptual y tipo | Ámbito y valor seguro inicial | Restricción de activación |
| --- | --- | --- | --- |
| F01 | EnableOutboxRelay: boolean | Plataforma/entorno, false | Requiere revisión independiente, CI de integración y permiso explícito |
| F02 | EnableExternalDispatch: boolean | Entorno, false | Nunca se habilita desde tenant o navegador |
| F03 | AllowedEventTypes: conjunto de identificadores | Plataforma, conjunto vacío hasta catálogo revisado | Ningún nombre de evento selecciona código arbitrario |
| F04 | AllowedDestinations: catálogo tipado | Plataforma, vacío | No aceptar URL o proveedor libre de un payload |
| F05 | MaxBatchSize: entero positivo acotado | Plataforma, mínimo operativo por definir | No ejecuta lote si no existe límite acreditado |
| F06 | MaxAttempts: entero positivo acotado | Plataforma, techo finito | Al agotar intentos, DeadLetter con auditoría |
| F07 | RetryBaseDelaySeconds: entero positivo | Plataforma/entorno | Backoff finito, sin espera activa |
| F08 | RetryMaxDelaySeconds: entero positivo | Plataforma/entorno | Mayor o igual a base y sujeto a cuota |
| F09 | ClaimLeaseSeconds: entero positivo | Plataforma/entorno | Reclamo atómico y expiración UTC; sin doble escritor |
| F10 | StaleRecoveryEnabled: boolean | Plataforma/entorno, false | Requiere prueba de inexistencia de claim vivo |
| F11 | TenantDispatchPolicy: enum | Plataforma, deny-by-default | Tenant A no autoriza mensajes de B; no override de UI |
| F12 | PayloadLoggingMode: enum | Plataforma, redact-only | Rechazar modo raw; secretos y PII fuera de logs |
| F13 | ProviderTimeoutSeconds: entero acotado | Plataforma/entorno | Timeout no equivale a acuse durable |
| F14 | RequireIndependentApproval: boolean | Plataforma, true e inmutable por tenant | A5 de TASK_ID diferente al autor antes del gate requerido |

Ninguna opción concede acceso comercial, altera identidades de Tasks o habilita cambios fuera de dev. Los valores definitivos, tipos concretos de C#, rangos y persistencia se fijarán únicamente en una microtarea técnica autorizada.

## 3. Ámbitos, precedencia y autoridad

1. **Código y contrato de seguridad**: límites no rebajables de tenant, JWT, RBAC, idempotencia, exclusividad CAS, privacidad, tasa y A5.
2. **Configuración de plataforma/entorno validada**: solo claves conocidas, origen autenticado, revisión y versionado; los secretos permanecen fuera de Git.
3. **Política de tenant autorizada por backend**: puede reducir cuotas y desactivar funciones permitidas; no puede activar relay o proveedor, sustituir empresa del token ni ampliar grants.
4. **Solicitud del cliente**: jamás decide EmpresaId, destino, permisos, feature flags privilegiados ni TTL de lease. CorrelationId y agregado son trazas, no autoridades.
5. **Overrides de emergencia**: solo para detener y aislar; no para saltar CI, A5 o aprobar entregas. Revocación registrada con UTC y trazabilidad.

Prohibidos: configuración libre desde JSON no tipado, SQL/HTML/JS aportado por cliente, DSN en documentos, endpoint arbitrario, credenciales en logs, aceptar firma/cabecera de tenant como identidad, ejecutar proveedor externo por simple flag. Las decisiones de despliegue y migración permanecen separadas.

## 4. Validaciones de contrato V01–V16

| ID | Invariante a comprobar en futura implementación |
| --- | --- |
| V01 | Clave desconocida o valor de tipo incorrecto se rechaza antes de crear efectos. |
| V02 | Booleanos privilegiados no aceptan coerción de cadenas como truthy. |
| V03 | MaxBatchSize es entero finito estrictamente positivo y con techo revisado. |
| V04 | MaxAttempts es finito; nunca se permite retry ilimitado. |
| V05 | RetryMaxDelay >= RetryBaseDelay y ambos son positivos. |
| V06 | ClaimLeaseSeconds debe tener límite y usa reloj UTC para vencimiento. |
| V07 | Recuperación stale requiere CAS y ausencia de escritor activo. |
| V08 | Tenant A no modifica configuración ni efectos de B. |
| V09 | Principal sin permiso de servidor no habilita proveedor ni relay. |
| V10 | Destino/tipo de evento no allowlisted produce rechazo completo. |
| V11 | Timeout, excepción o ACK incierto no cambian resultado a Entregado. |
| V12 | PayloadLoggingMode nunca admite secretos, JWT, PII o payload sin redactar. |
| V13 | Valores de cliente no prevalecen sobre política de plataforma. |
| V14 | Versiones de configuración y de evento no admitidas se rechazan. |
| V15 | Un flag apagado impide side effects aunque haya mensajes pendientes. |
| V16 | CI obligatoria solo es PASS completed/success del SHA causal; A5 distinto de autor. |

## 5. Matriz de verificación futura P/N — NO EJECUTADA

| Caso | Positivo que deberá acreditarse | Negativo que deberá bloquearse |
| --- | --- | --- |
| P01/N01 | Lectura de opciones tipadas válidas, read-only | Clave desconocida o tipo incorrecto |
| P02/N02 | Cliente A consulta solo su estado permitido | Cliente A fuerza flags o acceso tenant B |
| P03/N03 | Evento del catálogo permitido | Tipo arbitrario o destino no registrado |
| P04/N04 | Lote válido dentro de cuota | Lote excesivo o repetido con idempotencia conflictiva |
| P05/N05 | Único claim CAS vigente | Dos procesos intentan reclamar el mismo mensaje |
| P06/N06 | Backoff limitado y DeadLetter | Retry infinito o reactivación de DeadLetter |
| P07/N07 | Confirmación del mismo intento vigente | ACK tardío o timeout tratado como éxito |
| P08/N08 | Observabilidad sin datos sensibles | Logs con JWT, DSN, payload bruto o PII |
| P09/N09 | Operador revoca relay y no se despacha | Tenant/browser intenta reencenderlo |
| P10/N10 | A5 independiente + CI exact-SHA aprobadas | A5 propia o job cancelled/skipped contado PASS |

Esta tabla describe aceptación futura. **No se ejecutaron tests xUnit, MySQL, seguridad runtime, navegador, payloads reales ni llamadas externas** por el acto de documentarla. La fase de código M03 deberá implementar alcance auténtico con suites GitHub-hosted proporcionales y rollback probado.

## 6. Impacto A0–A4, salida, A5 y rollback

**A0**: solo este Markdown y una sección nueva autocontenida de CHANGELOG_AI.md. No cambian contratos compilados, esquema de datos, endpoints, servicios, código de seguridad ni fronteras arquitectónicas. Se inspeccionaron las fuentes existentes y sus límites.

**A1**: comprobar por inspección la integridad de 14 opciones, 16 invariantes y 10 pares P/N, todos marcados como futuros; no atribuir PASS a pruebas no ejecutadas.

**A2**: revisar por contrato aislamiento tenant A/B, autorización backend JWT/RBAC, no autocertificación, idempotencia, CAS, cuotas, privacidad, logs saneados y denegación por defecto; sin conclusión de seguridad runtime.

**A3**: comprobar consistencia de nombres y estados con Domain/Application/repository actuales. La configuración conceptual no introduce API, DI, tabla EF, scheduler, webhook, worker, proveedor ni dependencia transversal; compilación no es causal al changeset solo documental.

**A4**: commit fast-forward con HEAD/árbol comprobados, diff exactamente de dos Markdown, readback de blobs y checks relevantes verify-solqaryn-project-scope y V5 de CHANGELOG en completed/success del SHA publicado. Si el check requerido sigue queued/in_progress/cancelled/failure se mantiene VALIDANDO.

**A5**: queda PENDIENTE hasta revisión real de una Task Javier distinta del autor sobre SHA, diff y pruebas. Esta documentación no abre por sí sola M03, QA, main, PROD, migraciones, mensajería permanente ni permisos.

**Rollback**: git revert normal del commit documental, manteniendo BITACORA histórica; deshabilitar cualquier futuro relay antes de una reversión de runtime autorizada. Ninguna operación de infraestructura o MySQL fue realizada.
