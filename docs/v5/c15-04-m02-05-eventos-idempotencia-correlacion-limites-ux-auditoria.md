# SOLQARYN V5 — C15-04.M02.05: eventos, idempotencia, correlación y límites de auditoría UX

**Autor:** PRIMARY DEV 03 — JAVIER_CHATGPT, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Fecha:** 2026-11-14. **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Clase:** contrato documental M02.05. **No implementar eventos, webhooks, workers, dispatchers, Tasks adicionales ni triggers en este scope.**

## 1. Autoridad, preflight y fuentes

Fuente ejecutable: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, con PLAN_MAESTRO/COLA/DAG_TECNICO del Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. En el claim `PLAN_MAESTRO!H2280=PENDIENTE`/I vacío, `COLA!G/O2280=SI`, `DAG_TECNICO!A2241=C15-04.M02.05` y F=VALIDADO/G=SI; CAS limitado a H2280 con `occurrencesChanged=1`, owner propio en I2280, BITACORA A:Z y readback. Los M01.01–06 están LISTO con revisiones independientes; los trabajos M02 de otros autores son contratos contemporáneos separados, no autorizaciones de runtime.

Fuentes del repositorio consultadas:
- `backend/src/Domain/Entities/MensajeOutbox.cs` (blob `891572626c19cd6cf8f522fca5ec876333286ffb`) **ya define** intención durable de efecto externo, `EmpresaId` requerido, `ClaveIdempotencia`, `CorrelationId`, UTC, estados Pendiente/Procesando/Fallido/Entregado/DeadLetter y validación de longitudes. El propio comentario del tipo indica que claim/dispatcher/persistencia quedan para scopes posteriores: **no se afirma que exista un dispatcher activo**.
- `backend/src/Infrastructure/Persistence/AppDbContext.cs` (blob `c4b32f6637f1b4426655bb0cd12af8dd9d229866`) declara `DbSet<MensajeOutbox>` junto a autorizaciones de lectura de empresa y guards de escritura; presencia de entidad no prueba entrega externa o colas operativas.
- `frontend/src/app/app.routes.ts` (blob `2f560c14693455b79ac4546e9333fb74586ef234`) muestra storefront público y dashboard protegido por guards; una interacción UI no debe saltar autorización backend.
- `docs/v5/c15-04-m02-01-modelo-ownership-invariantes-ux-accesibilidad-rendimiento.md` (blob `604c55ec350a879f3766b5c655f6f844e91aba0a`) fija UX01–12, INV01–16 y AT01–10 conceptuales.
- `docs/v5/c15-04-m02-02-contratos-api-dto-errores-validaciones-compatibilidad-ux.md` (blob `eb79be34dc20a6c32a702c5fe1a7c2bdcceab5b9`) fija OP01–OP08/ERR01–ERR10 como propuestas **sin endpoints nuevos**.
- `docs/v5/c15-04-m01-06-bloqueos-rollback-liberacion-ux-accesibilidad-rendimiento.md` (blob `5fff95e8a351a85bb1fee19a8cc95ffb9a8aae96`) conserva doce gates para métricas, WCAG, tenant A/B y CI exact-SHA.

Estos archivos no autorizan conectar MensajeOutbox ni eventos de navegador al mecanismo de Tasks programadas. La telemetría de TAREAS_PROGRAMADAS es autorreporte del último run, no heartbeats de tiempo real ni un bus de mensajes.

## 2. Catálogo de señales *futuras*, sin transporte implementado

| Clave | Señal conceptual | Condición de emisión *futura* | Consumidor *propuesto* y límite |
| --- | --- | --- | --- |
| EV01 | `AuditScopePrepared` | Scope y SHA reales validados por A0 | Registro de evidencia local; no dispara otras Tasks |
| EV02 | `SurfaceObservationCaptured` | Ruta implementada y fixture/screenshot depurado | Almacén futuro autorizado; no incluir cookies |
| EV03 | `AccessibilityCheckExecuted` | Test browser realmente completado con log/criterio | Revisor de accesibilidad; `NO_EJECUTADO` no dispara PASS |
| EV04 | `PerformanceSampleRecorded` | Muestra con red, unidad, dispositivo y baseline | Informe con cobertura explícita, no promedios inventados |
| EV05 | `UxFindingConfirmed` | Reproducción y clasificación de severidad | Remediación autorizada, sin emitir P0/P1 por intuición |
| EV06 | `UxRemediationChecked` | Nuevo SHA con prueba causal y diff evaluado | Auditor A4, no aprobación automática de QA/PROD |
| EV07 | `AuditReviewCompleted` | A5 con TASK_ID distinto del autor | Gobernanza de dev tras readback, no despliegue |
| EV08 | `EvidenceExpired` | Retención vencida y revocación verificada | Depuración de evidencia privada por dueño autorizado |

Las claves EV01–EV08 son nombres de diseño, **no** eventos existentes, handlers, triggers, API pública ni configuraciones de GitHub/Drive. M02.05 no produce ni consume ninguna de estas señales. Una futura implementación requiere microtarea habilitada, autorización específica, pruebas dirigidas y revisión independiente.

## 3. Envelope, claves e idempotencia propuestas

Una especificación futura mínima para una *señal de auditoría no sensible* sería `schemaVersion`, `eventType`, `eventId`, `scopeId`, `planItemId`, `sourceSha`, `surfaceKey`, `tenantScope` (derivado del backend), `occurredAtUtc`, `correlationId`, `idempotencyKey`, `evidenceRef` y `privacyClass`. No incluir JWT, bearer, refresh token, cookie, email, datos de cliente, capturas crudas ni URLs firmadas persistentes. Para observación pública sin datos privados, el scope público se codifica explícitamente sin suplantar un `EmpresaId` arbitrario.

| Control | Regla de diseño para M03 futuro | Negativo a comprobar |
| --- | --- | --- |
| ID01 | `eventId` global opaco; firma/procedencia del productor verificado | Payload del navegador falsifica A5 o Scope Lock |
| ID02 | `idempotencyKey` estable por tenant + scope + evento lógico + SHA | Reintento produce dos aprobaciones o dos notificaciones |
| ID03 | Dedupe durable/atómico solo en almacén autorizado | Leer y escribir sin CAS se declara garantía |
| ID04 | `correlationId` por flujo, no nombre de usuario ni dato sensible | Correlación permite enumerar otro tenant |
| ID05 | Orden parcial por scope/versión y monotonicidad verificada | Evento antiguo revierte estado LISTO |
| ID06 | Estados `NO_EJECUTADO`, `FAIL`, `PASS` explícitos | `queued` o `cancelled` cuenta PASS |
| ID07 | Consumidor aplica JWT/RBAC y tenant de servidor | `tenantScope` del cliente concede autorización |
| ID08 | Evento y BITACORA tienen identificadores y fuentes diferentes | RUN_ID local se atribuye al proveedor nativo |

En dev actual la auditoría institucional mantiene PLAN_MAESTRO H/I, COLA G/O, DAG F/G y BITACORA A:Z como autoridades. Ningún evento conceptual sustituye esos readbacks ni habilita escritura concurrente. Las seis Tasks existentes son ejecutores programados, **no** consumidores de un ciclo server-side infinito.

## 4. Límites, reintentos, cuota y contención futura

| Control | Valor/decisión pendiente antes de implementar | Señal de fallo que bloquea |
| --- | --- | --- |
| LM01 | Presupuesto de volumen por tenant/scope (sin cifra ficticia) | Cola ilimitada o saturación de DB/CI |
| LM02 | Tamaño máximo y clasificación PII por envelope (por aprobar) | Payload con datos privados o token |
| LM03 | Límite de intentos y backoff con jitter (por probar) | Reintento infinito sin hipótesis nueva |
| LM04 | Dead-letter con redacción de errores y resolución autorizada | Error SQL/secretos visibles a visitantes |
| LM05 | Retención y borrado/anonimización por clase (por definir) | Captura privada disponible sin necesidad |
| LM06 | Exclusión/lease transaccional y recuperación validada (por diseñar) | Dos workers procesan el mismo evento |
| LM07 | Rate limit e idempotencia por tenant + evento (por calibrar) | Tenant A agota capacidad de B |
| LM08 | Límite de fan-out y apagado seguro del dispatcher (no implementado) | Webhook genera nuevas Tasks no autorizadas |
| LM09 | Observabilidad sin PII: correlación, edad, error, intento | Dashboard presenta datos de tenant ajeno |
| LM10 | Prueba de recuperación y rollback en servicio efímero | Se declara durable sin ensayo de crash |

El `MensajeOutbox` existente fija longitudes máximas y transiciones propias, pero no se copian ni se convierten automáticamente en configuración de un proceso aún inexistente. Cualquier servicio de eventos exigirá una aprobación posterior distinta: tecnología, seguridad de payload, cuota, autor, controles anti-loop, permisos, pruebas MySQL efímero y rollback.

## 5. Matriz de oráculos futuros

| Caso | Escenario positivo futuro | Rechazo negativo futuro |
| --- | --- | --- |
| P01/N01 | Un evento autorizado enlaza scope y SHA real | ID de microtarea ajena manipulado |
| P02/N02 | Dos envíos con misma clave tienen efecto único | Duplicado por retry emite dos A5 |
| P03/N03 | Dos tenants mantienen colas/evidencias aisladas | B recibe payload o métricas de A |
| P04/N04 | Estado `NO_EJECUTADO` no agrega métricas | Placeholder se presenta como PASS |
| P05/N05 | Correlación trazable sin datos sensibles | Token JWT aparece en logs o envelope |
| P06/N06 | Orden de eventos respeta versión del SHA | Evento viejo sobrescribe publicación nueva |
| P07/N07 | Fallo conserva error redactado y finitud de reintento | Dead-letter expone stack o loop ilimitado |
| P08/N08 | Cuota limita fan-out por tenant con aislamiento | A causa denegación de servicio cruzada |
| P09/N09 | Retención revoca acceso a artefactos privados | Enlace obsoleto sigue público |
| P10/N10 | A5 revisa autor distinto y evidencia causal | Autor firma su propio trabajo |
| P11/N11 | Check obligatorio exact-SHA `completed/success` | `skipped` o `in_progress` tratado PASS |
| P12/N12 | Dispatcher nunca se crea sin autorización específica | Evento documental ejecuta otra Task |

**Estado de ejecución en M02.05:** P01/N01–P12/N12 son `FUTUROS_NO_EJECUTADOS`. No se ha desplegado un productor, consumidor, cola, cron, webhook, redelivery, límite medido ni prueba de resiliencia mediante este documento. Los casos no son pruebas superadas del SaaS.

## 6. Impacto, auditorías y rollback

- **Diff intencional:** este documento y una sección íntegra de `CHANGELOG_AI.md`; no modificaciones a `MensajeOutbox`, AppDbContext, backend, frontend, MySQL, GitHub Actions, cuentas, seis Tasks, RRULE, infraestructura ni secretos.
- **Arquitectura:** Domain/Application/Infrastructure/API, EF Core 10.0.12/MySQL 8.4 y Angular 22.2.1 sin alteraciones; eventos futuros subordinados a política de tenancy/JWT/RBAC y permisos mínimos. No se activa Hostinger/Cloudflare/R2 ni QA/main/PROD.
- **A0:** EV01–08, ID01–08, LM01–10 y doce pares P/N documentados contra fuentes Git verificadas.
- **A1:** coherencia de escenarios positivos/negativos y ausencia de afirmación de delivery; pruebas funcionales runtime `NO_EJECUTADAS`.
- **A2:** aislamiento A/B, payload mínimo y fail-closed; no certificados de penetración ni ejecución HTTP.
- **A3:** referencias cruzadas a M01, M02.01–02, outbox real y estados V5; MySQL, job delivery y webhook `NO_APLICA_DOCUMENTAL`, jamás PASS.
- **A4:** dos Markdown, publicación fast-forward/CAS en dev y SUCCESS exact-SHA de Scope Lock/procedencia V5, lectura posterior PLAN/COLA/BITACORA.
- **A5:** pendiente de Task Javier distinta del autor; no promoción a otros ambientes.

Rollback de este diseño: `git revert` sobre commit propio y evento aditivo con evidencia; no sobrescribir historia de BITACORA ni cambios ajenos. Aún no existe un cambio de datos o servicio que revertir.
