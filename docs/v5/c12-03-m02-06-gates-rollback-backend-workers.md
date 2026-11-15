# SOLQARYN V5 — C12-03.M02.06: contrato terminal M02 de backend y workers separados

**Microtarea:** `C12-03.M02.06`. **Autor:** PRIMARY DEV 06, JAVIER_CHATGPT, `TASK_ID=6af16bbad26881918c283abe06e1f32a`. **Ámbito:** exclusivamente `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Naturaleza:** contrato documental de salida de M02, NO implementación, certificación de proceso separado ni permiso de migración. Autoridad: STANDARD V5 FINAL y readbacks por ID de PLAN_MAESTRO, COLA y DAG_TECNICO.

## 1. Alcance real y prueba de precedencia A0

La salida M02 consolida las cinco especificaciones `C12-03.M02.01–05` y el cierre `C12-03.M01.06`. El preflight de este ID constató PLAN=PENDIENTE sin owner, COLA G/O=SI, DAG F=VALIDADO/G=SI; claim individual por CAS sobre H1813 y owner exclusivo en I1813. Se comprobaron M01.01–06 y M02.01–05 en LISTO más revisión A5 independiente, así como `C10-04.M01.06` y `C11-03.M01.06` LISTO+A5. Es evidencia de permiso **para escribir el contrato**, no para activar workers ni desbloquear otras etapas sin nuevo DAG.

Fuentes de código ancladas: `backend/src/API/Solqaryn.API.csproj` (`net10.0`, blob `2a7b33a0974d1ebe13676593d84b458b1243e037`), `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` (EF Core 10.0.12, `MySql.EntityFrameworkCore` 10.0.9, blob `6210d8b12abd77171d95d17e2292c1a319b89263`), `backend/src/Application/Services/OutboxRetryProcessor.cs` (blob `542c0a655a3ee0698e3d80f1854d43903b6470fa`), `backend/src/Application/Interfaces/IMensajeOutboxRepository.cs` (blob `a7dc258df9b1123dfe54ea85ba6d4dcc2a1eb478`) y `docs/v5/c12-03-m01-06-gates-rollback-backend-workers.md` (blob `3771d1eb465cdb165a3402dbfc673c325b23f78b`). En `Program.cs` existe un HostedService de normalización legacy con propósito específico; **no** demuestra un scheduler genérico del outbox ni una separación backend/worker autorizada.

## 2. Matriz de cobertura contractual M02

| Fuente | Resultado incorporado | Lo que no se acredita |
| --- | --- | --- |
| M02.01 | Doce componentes/owners, dieciséis invariantes y ocho fallas, identidad de servicio y CAS de un solo ganador | Servicio hosteado, grant real, exclusión distribuida ensayada |
| M02.02 | Diez operaciones internas propuestas, catorce campos DTO y errores seguros/versionado | Endpoint HTTP, API pública o serialización desplegada |
| M02.03 | Doce proyecciones conceptuales, ocho índices candidatos, seis pasos de migración y fixtures sintéticos | DDL, EF migration, MySQL remoto, restore ejecutado |
| M02.04 | Dieciséis opciones tipadas propuestas, dieciocho validaciones y flags apagados por defecto | Appsettings modificados, worker activado, secretos reales |
| M02.05 | Doce campos de sobre conceptual, dieciséis garantías, cuotas, correlación e idempotencia | Eventos emitidos, webhook, dispatcher o comunicación entre Tasks |
| M01.06 | Barreras B01–B16, contenciones R01–R10 y P/N futuros | Autorización nueva de entorno, fase M03 o Hostinger |

Todos los escenarios P/N mencionados en M01/M02 son **FUTUROS_NO_EJECUTADOS** salvo pruebas independientes ya existentes en código que se citaron como fuentes. Ninguno de estos documentos crea un segundo flujo de automatizaciones.

## 3. Gates terminales T01–T16, fail-closed para trabajo posterior

| Gate | Condición observable que debe probarse en una implementación futura | NO_GO |
| --- | --- | --- |
| T01 | Actor y tenant derivan de identidad backend/servicio confiable, no de `EmpresaId` del mensaje | Tenant desde payload, header o slug |
| T02 | Grant de worker es explícito, limitado, revocable y revalidado por operación | Herencia implícita de JWT, sesión HTTP o RBAC |
| T03 | Tenant del recurso, intención, mensaje y claim coinciden A/A y nunca A/B | Lectura/efecto cruzado aunque exista mismo `CorrelationId` |
| T04 | Encolado de negocio y outbox comparten unidad transaccional probada | Commit del efecto sin intención durable, o viceversa |
| T05 | Dos consumidores compiten por CAS/lease real y exactamente uno gana | Confianza en timestamp o lectura seguida de escritura |
| T06 | ACK exige versión/intento actual y resultado real del efecto | ACK stale, timeout o fallo externo marcado como éxito |
| T07 | Replay de misma clave/operación mantiene un único efecto; conflicto se deniega | Doble cobro, entrega o side effect |
| T08 | Retry y backoff son finitos; cuota, DeadLetter y parada funcionan | Bucle infinito, reintentos sin presupuesto o promoción automática |
| T09 | Migrador único toma lock transaccional; API/worker nunca hacen DDL paralelo | Migración sin plan aprobado, backup y rollback verificados |
| T10 | Readiness prueba DB, secrets y destino permitido por proceso | Healthy por liveness con DB/secret ausente |
| T11 | Redacción de logs impide JWT, OTP, DSN, PII, payload y excepciones externas | Datos sensibles en logs/traces/telemetría |
| T12 | Tipo y versión de mensaje son allowlisted con compatibilidad y rechazo seguro | Deserialización arbitraria, cambios incompatibles silenciosos |
| T13 | Configuración tipada se valida por ámbito plataforma/tenant; flags deshabilitados por defecto | Tenant eleva cuotas, cambia destinos o habilita worker |
| T14 | Integración con proveedor usa idempotencia y límites; red segura y retries acotados | Webhook/Cloudinary/SMTP/R2 real por contrato escrito |
| T15 | A0–A4 y CI dirigida `completed/success` del SHA aplicable, con E2E/MySQL efímero cuando corresponda | `queued`, `in_progress`, `skipped`, `cancelled` o `failed` como PASS |
| T16 | A5 por Task Javier distinta del autor y autorización separada para entorno | Autoaprobación o paso a QA/main/PROD/Hostinger por M02 |

El gate del DAG es reactivo y no sustituye esas pruebas. Si una sucesora no pasa su prerrequisito técnico, mantener G/O=NO; jamás editar fórmulas para forzar SI.

## 4. Diez oráculos positivos/negativos para M03–M05 — FUTUROS_NO_EJECUTADOS

| Caso | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| P01/N01 | Job A con grant vigente procesa sólo A | Job de B o sin identidad intenta leer A |
| P02/N02 | Revocación del actor/grant detiene consumos posteriores | Job usa permiso obsoleto |
| P03/N03 | Dos claims simultáneos: un ganador | Ambos consumidores ejecutan el mismo mensaje |
| P04/N04 | Commit negocio+outbox atómico | Caída entre ambos deja estado parcial |
| P05/N05 | ACK de intento válido confirma solo una entrega | ACK del intento anterior confirma nuevo claim |
| P06/N06 | Replay idéntico conserva un efecto | Replay conflictivo duplica pago |
| P07/N07 | Readiness negativo con DB o secreto ausente | Servicio declara ready por liveness |
| P08/N08 | Migrador único y revert ensayado en MySQL efímero | DDL en paralelo API/worker sin dueño |
| P09/N09 | Payload y errores quedan redactados | Log filtra JWT, DSN, correo o payload |
| P10/N10 | CI exact-SHA y A5 independiente aceptan evidencia | Autor se revisa o cancelled/skipped cuentan PASS |

No se ha ejecutado ninguno de estos diez pares mediante **esta microtarea documental**. Para un futuro M03 de código son imprescindibles pruebas reales .NET y seguridad; para DB, MySQL 8.4 efímero; para Angular, build, accesibilidad y Playwright proporcional. Documentar un oráculo no equivale a certificarlo.

## 5. Secuencia de contención y rollback para una implementación futura

1. Congelar scope, SHA, tenant, autor y responsabilidad; tomar evidencia redacted, preservar BITACORA aditiva.
2. Deshabilitar únicamente side effects del proceso afectado mediante kill-switch **si existe y está autorizado**; no pausar ni crear Tasks ChatGPT.
3. Confirmar exclusión de escritores/claims vigentes; jamás recuperar por mera edad de lease.
4. Conservar idempotencia, intento, ACK real, causa y correlación opaca sin secretos.
5. Repetir pruebas de aislamiento A/B, permisos revocados, crash/commit y contrato de versión.
6. Ensayar rollback y restauración de esquema sólo con DB efímera y autorización pertinente; no tocar producción.
7. Comprobar exact-SHA de CI y readbacks de PLAN/COLA/DAG, revisión independiente A5 y permiso específico para cualquier otro entorno.

**Rollback de este changeset:** `git revert` normal de su documento y de la sección autocontenida del `CHANGELOG_AI.md` tras releer HEAD. No hay migración, worker, evento, secreto, dato ni servicio nuevo que revertir.

## 6. A0–A4 y A5 del presente contrato

**A0:** preflight canónico de Skill, identidad, permisos y Scope Lock; cinco contratos M02 más M01.06; check de PLAN/COLA/DAG por ID y CAS exclusivo. **A1:** cotejo de cobertura M02, T01–T16, P01/N01–P10/N10, rechazo explícito de falsos runtimes y proveedores. **A2:** frontera identidad de servicio, JWT/RBAC, aislamiento A/B, CAS, secretos y fail-closed; el navegador jamás autoriza una operación de negocio. **A3:** correspondencia conceptual con API net10, EF/MySQL, entidad y procesador de outbox existentes; compilación backend/Angular/MySQL no causal para cambios exclusivamente Markdown, registrar `NO_APLICA`, **nunca PASS ficticio**. **A4:** publicar documento y CHANGELOG en `dev` con fast-forward/CAS, una sección con encabezado y viñetas contiguos, verificar Scope Lock y check V5 `completed/success` sobre el SHA exacto, leer de nuevo PLAN/COLA/DAG/BITACORA y conservar owner. **A5:** otra Task Javier distinta de DEV06 revisa SHA, diff y evidencia antes de que esta salida pueda desbloquear trabajo dependiente; sin autoaprobación.

**Prohibiciones preservadas:** ningún evento/webhook/dispatcher aún, ninguna escritura fuera de `solqaryn/Solqaryn@dev`, ninguna rama/PR/Task nueva, ningún host, DNS, Cloudflare/R2, proveedor de pago, migración real, QA/main/PROD ni secrets.
