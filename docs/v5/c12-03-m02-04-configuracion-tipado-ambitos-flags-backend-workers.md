# SOLQARYN V5 — C12-03.M02.04: opciones tipadas, ámbitos y flags seguros para backend/workers

**Autor:** PRIMARY DEV 02 JAVIER, TASK_ID 6af16bb9492c8191b8e8c7d606f11507. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, PLAN_MAESTRO/COLA/DAG_TECNICO. **Rama:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK STRICT. **Estado del artefacto:** contrato documental; ninguna variable/configuración real, proceso worker, relay, scheduler, endpoint, secreto, migración o proveedor se modificó.

## 1. Baseline comprobable y fronteras

La API real es ASP.NET Core 10 con JWT Bearer, CORS por lista y rate limiting en backend/src/API/Program.cs (blob 321fc0a3c29d5cbc53f5e30373f82939e907d6f0). La separación futura API/worker no traslada automáticamente la identidad HttpContext ni el permiso del usuario a procesos background. En backend/src/Application/Services/OutboxRetryProcessor.cs (blob 542c0a655a3ee0698e3d80f1854d43903b6470fa) YA existe OutboxRetryProcessorOptions, cuyo método Validar() exige BatchSize entre 1 y 200, MaxAttempts finito, BaseDelay mayor que cero, MaxDelay mayor o igual a BaseDelay, StaleClaimAfter mayor que cero y JitterRatio entre 0 y 1; también existe ProcesarLoteAsync, pero no scheduler propio. Los contratos backend/src/Application/Interfaces/IMensajeOutboxRepository.cs (blob a7dc258df9b1123dfe54ea85ba6d4dcc2a1eb478) exigen claim/ACK condicional; AddAsync NO realiza commit automáticamente. El documento C12-03.M01.06 (blob 3771d1eb465cdb165a3402dbfc673c325b23f78b) contiene B01–B16 y R01–R10 obligatorios.

No se presume que API y worker hayan sido desplegados por separado ni que existan ya otros servicios. Hostinger y Cloudflare son objetivo sujeto a autorización futura. Los contratos propuestos aquí NO son claves existentes, ni alteran appsettings, DI, Render, Aiven, Tasks, secretos o producción.

## 2. Configuración conceptual por área F01–F16

| ID | Opción conceptual / tipo | Owner y ámbito permitido | Condición segura |
| --- | --- | --- | --- |
| F01 | WorkerExecutionEnabled: bool | Plataforma/entorno, false | No habilitar por documento |
| F02 | ExternalEffectDispatchEnabled: bool | Plataforma/entorno, false | Requiere autorización y CI de proveedor |
| F03 | WorkerIdentityMode: enum | Plataforma, servicio autenticado | Nunca identidad de payload |
| F04 | TenantResolutionMode: enum | Plataforma, binding verificado | Sin HttpContext y sin binding, denegar |
| F05 | OutboxBatchSize: int | Plataforma, validación actual | Mantener 1..200 |
| F06 | OutboxMaxAttempts: int | Plataforma, finito | Respetar OutboxRetryPolicy |
| F07 | RetryBaseDelay: TimeSpan | Plataforma, positivo | Sin polling infinito |
| F08 | RetryMaxDelay: TimeSpan | Plataforma, acotado | >= BaseDelay, <= política |
| F09 | StaleClaimAfter: TimeSpan | Plataforma, positivo | Lease UTC con claim exclusivo |
| F10 | JitterRatio: double | Plataforma, 0..1 | No reintento ilimitado |
| F11 | ReadinessRequiresDb: bool | Entorno, true | DB ausente => NOT_READY |
| F12 | ReadinessRequiresSecret: bool | Entorno, true | Secreto inválido => NOT_READY |
| F13 | AllowedEffectDestinations: catálogo | Plataforma, vacío | Sin URL libre del cliente |
| F14 | TelemetryRedactionMode: enum | Plataforma, sanitized-only | No JWT, DSN, PII ni payload |
| F15 | ManualMaintenanceStop: bool | Plataforma, true hasta autorización | Parada no equivale a permiso de reinicio |
| F16 | IndependentApprovalRequired: bool | Plataforma, true | Task revisora distinta a autora |

F05–F10 reflejan límites del OutboxRetryProcessorOptions ya existente; son nombres del contrato para futura separación, NO una segunda implementación. F01/F02/F13 siguen desactivadas o no implementadas. Ningún tenant puede elevar límites globales ni aprobar la ejecución del relay.

## 3. Precedencia y autoridad

1. Las invariantes no rebajables son identidad de servicio, tenant A/B, JWT/RBAC, CAS/lease, idempotencia, seguridad de secretos, A5 independiente y Scope Lock.
2. Plataforma/entorno: configuración tipada allowlisted y versionada solo por operador autorizado, con auditoría. No se admiten valores libres que elijan código o proveedor.
3. Empresa: puede restringir cuota/capacidad previamente autorizada; jamás alterar identidad, destino, grants o activar workers globales.
4. Cliente/browser: EmpresaId, correlationId, slug, cabeceras, claims no verificados y body no son autoridad. El trabajador sin HttpContext debe denegar cuando falta resolución confiable de empresa/actor.
5. Contención: un kill-switch detiene side effects, no autoriza migraciones ni simula PASS. No recuperar un claim stale si otro escritor activo no fue descartado.
6. MySQL/EF: una futura migración requiere dueño único, backup, ensayo efímero y rollback; ni API ni worker ejecutan DDL en paralelo por consecuencia de este contrato.

## 4. Reglas V01–V18

| ID | Invariante para futura implementación |
| --- | --- |
| V01 | Config desconocida o tipo incorrecto se rechaza. |
| V02 | Flag ausente o string truthy no habilita worker. |
| V03 | Dispatch apagado impide side effect aunque existan mensajes. |
| V04 | Identidad de servicio revocada o ausente se deniega. |
| V05 | Tenant no acreditado o EmpresaId inválido se rechaza. |
| V06 | Un lote A con mensaje B se rechaza entero antes de despachar. |
| V07 | BatchSize entre 1 y 200 según validación real. |
| V08 | MaxAttempts finito dentro del techo del código. |
| V09 | BaseDelay positivo. |
| V10 | MaxDelay mayor o igual a BaseDelay y dentro de política. |
| V11 | StaleClaimAfter positivo, vencimiento UTC y CAS. |
| V12 | JitterRatio entre 0 y 1. |
| V13 | Reclamo concurrente produce un solo ganador real. |
| V14 | Entregado solo tras ACK durable del intento vigente. |
| V15 | Secreto/DB ausente implica NOT_READY sin exponer valores. |
| V16 | Logs nunca exponen token, DSN, PII, payload bruto. |
| V17 | Tenant no aumenta cuotas ni enciende flags de plataforma. |
| V18 | Cancelled/skipped/failed/in_progress o A5 del autor jamás PASS. |

## 5. Aceptación futura P/N: NO EJECUTADA

| Par | Positivo para una futura M03 | Negativo a rechazar |
| --- | --- | --- |
| P01/N01 | Worker servicio A + binding A procesa A | Sin binding o mensaje B falla antes del primer dispatch |
| P02/N02 | Opciones válidas en entorno autenticado | Tenant/browser enciende servicio o proveedor |
| P03/N03 | BatchSize tipado 1..200 | 0, 201, cadena u overflow |
| P04/N04 | Reintento finito con backoff | Loop infinito o retraso fuera de política |
| P05/N05 | Claim CAS, ACK intento vigente | Doble claim o ACK anterior |
| P06/N06 | Readiness y liveness diferenciados | DB/secret ausente reportado saludable |
| P07/N07 | Auditoría redacted por tenant | JWT, PII, DSN o payload en logs |
| P08/N08 | Política empresa reduce cuota | Empresa amplía grant o cuota plataforma |
| P09/N09 | Stop autorizado detiene efectos | Parada se interpreta como permiso de reinicio |
| P10/N10 | CI exact-SHA + A5 independiente | Auto-A5 o check cancelado declarado PASS |

Todos estos P/N son FUTUROS_NO_EJECUTADOS. Esta documentación NO ejecutó xUnit, .NET build, Docker, MySQL, Playwright, pruebas de proveedor, backup ni restauración. M03 deberá producir código real y verificar A/B, auth, idempotencia, concurrencia, fallo parcial y rollback con GitHub-hosted CI.

## 6. A0–A4, A5 y rollback

**A0:** solo este Markdown y sección de CHANGELOG_AI.md autocontenida; API, Domain, Application, Infrastructure, configuración efectiva, MySQL y seguridad sin cambios binarios. No introduce una frontera arquitectónica real.

**A1:** cotejar 16 opciones, 18 validaciones y 10 pares P/N futuros. **A2:** revisar tenant A/B, actor sin HttpContext, JWT/RBAC, cuotas, privacidad, CAS y fail-closed. **A3:** cotejar la implementación existente de OutboxRetryProcessorOptions.Validar, procesador, entidad y Program.cs; compilación no causal a dos Markdown, NO_APLICA documentado y nunca PASS inventado.

**A4:** fast-forward/CAS, diff de dos Markdown, blobs postpublicación, ausencia de encabezados vacíos y cruces de autoría en CHANGELOG, Scope Lock y V5 completados SUCCESS sobre SHA causal o equivalencia probada a HEAD descendiente. Checks cancelados o pendientes NO son PASS.

**A5:** revisión por Task Javier distinta, nunca autoaprobación; no habilita por sí sola M03, QA/main/PROD ni producción. **Rollback:** git revert normal, preservar BITACORA; futura ejecución necesitará parada segura, pruebas EF/MySQL efímero y autorizaciones adicionales.
