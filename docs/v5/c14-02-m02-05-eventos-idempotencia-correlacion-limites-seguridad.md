# SOLQARYN V5 — C14-02.M02.05: eventos, idempotencia, correlación y límites de aceptación de seguridad

**Microtarea:** `C14-02.M02.05` · **Autor:** PRIMARY DEV 04 — JAVIER_CHATGPT, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09` · **Fecha:** 2026-11-14 (America/Tegucigalpa) · **Rama:** `solqaryn/Solqaryn@dev` · **PROJECT_SCOPE_LOCK=STRICT**.

**Alcance:** contrato **DOCUMENTAL** de eventos *conceptuales* de futura aceptación de seguridad. Identificadores, esquemas, cuotas y pruebas son `PROPUESTOS_NO_IMPLEMENTADOS` / `FUTUROS_NO_EJECUTADOS`. **No** crea dispatcher, webhook, cron, bus, worker, colas reales, API, DTO, tabla, migración, feature flag, secreto ni prueba de penetración. Nada habilita QA/main/PROD o altera las seis Tasks Javier. No se afirma P0/P1=0.

## 1. Autoridad, dependencias y fronteras

Autoridad ejecutable: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [Sheet V5](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), junto con Skill canónica, `AGENTS.md`, Scope Lock y HEAD exacto de `dev`. En el claim de `C14-02.M02.05` se releyó PLAN fila 2064 H=PENDIENTE/I vacía, COLA G/O=SI, DAG ID exacto F=VALIDADO/G=SI. El gate declarado para M02.05 es **C14-02.M01.01–06+A5**, no un PASS de M02.04; M02.06 tiene sus propios prerequisitos y no se libera por VALI­DANDO.

| Fuente | Evidencia Git existente | Límite |
| --- | --- | --- |
| `docs/v5/c14-02-m01-06-gates-bloqueo-rollback-liberacion-seguridad.md` | `6cb629af7993a6f4eb7cb8518f538f7ed196326f` | B01–B12, bloqueo y rollback |
| `docs/v5/c14-02-m02-01-modelo-ownership-invariantes-aceptacion-seguridad.md` | `1bf7ca3f8c549212a24e10ed1afefb2fdedea972` | SE01–14, SI01–16, SA01–12 |
| `docs/v5/c14-02-m02-02-api-dto-errores-validaciones-compatibilidad-seguridad.md` | `9de9d398cc2d238ce2ba66f27890c54b3317d782` | API01–12, errores, no endpoints |
| `docs/v5/c14-02-m02-03-persistencia-indices-fixtures-seguridad.md` | `f8eeec67ea3c7165c967d9a9d6c28a6970d8909a` | PD01–12, IX01–08, sin esquema físico |

Los ámbitos de autoridad son distintos: actor JWT/RBAC/tenant en backend, microtarea con owner exclusivo en PLAN, revisión A5 por otro TASK_ID, resultado CI desde GitHub SHA y acceso a evidencia según permiso. Una columna de Sheets o texto de CHANGELOG **no constituye** un evento ejecutable ni una prueba. La única BITACORA institucional es append-only A:Z con `EVENT_ID` e `IDEMPOTENCY_KEY`, sin reescribir historia.

## 2. Catálogo EV01–EV12 — eventos lógicos futuros, no emitidos

| ID | Señal lógica hipotética | Condición causal mínima | No habilita |
| --- | --- | --- | --- |
| EV01 | `AcceptanceScopeRequested` | Scope permitido, actor, versión y dev explícitos | Despliegue/tenants globales |
| EV02 | `SubjectAuthorizationEvaluated` | JWT firma/exp/issuer/aud y membresía backend | Grants por flag/UI |
| EV03 | `TenantIsolationCaseEvaluated` | Tenant A/B sintético y evidencia saneada | Leer datos reales de B |
| EV04 | `HostBindingCaseEvaluated` | Binding exacto y origen confiable | Aceptar spoof o substring |
| EV05 | `ProtectedMediaCaseEvaluated` | Owner, TTL y firma vigente | Entregar bytes de otro tenant |
| EV06 | `AsyncEffectCaseEvaluated` | Job autorizado, versión, efecto correlacionado | Arrancar worker/dispatcher |
| EV07 | `SecurityFindingRecorded` | Hallazgo reproducible, severidad, scope y SHA | Declarar P0/P1 resuelto |
| EV08 | `RetestEvidenceAttached` | Resultado reproducible exact-SHA y permisos | Supuesto PASS por documento |
| EV09 | `RequiredCheckObserved` | GitHub check-runs SHA, status y conclusion | Cancelled/skipped como PASS |
| EV10 | `ScopeTransitionAudited` | PLAN H/I, clave idempotente, readback | CAS atómico inexistente |
| EV11 | `IndependentA5Reviewed` | Reviewer TASK_ID distinto de autor y diff original | Autoaprobar o producir release |
| EV12 | `AcceptanceReleaseDecisionProposed` | Todos los gates verdaderos y permiso externo | QA/main/PROD automático |

No se define ruta HTTP, mensaje Pub/Sub ni productor real. Cada nombre designa una intención verificable en una **implementación futura** que requerirá una microtarea M03 auténtica, diseño de esquema, idempotencia transaccional, seguridad y CI causal. Ninguna señal activa eventos entre Tasks.

## 3. Contrato propuesto de correlación e idempotencia ID01–ID10

| ID | Contrato de implementación futura | Negativa que debe rechazarse |
| --- | --- | --- |
| ID01 | `eventId` único y `idempotencyKey` estable por efecto y propietario | Mismo evento duplica efecto |
| ID02 | `tenantId` resuelto server-side y unido al scope | Clave de A reutilizada en B mezcla estado |
| ID03 | `scopeId` allowlisted y `expectedVersion` verificado en datastore atómico | Dos writers ganan mismo CAS |
| ID04 | `sourceCommitSha` exacto y check-run `head_sha` equivalente | SHA anterior acredita nuevo código |
| ID05 | `actorTaskId` dentro de seis autorizadas y rol contrastado | Cuenta/Task retirada autoriza revisión |
| ID06 | `correlationId` opaco, longitud y cardinalidad limitadas | Inyectar PII/JWT o identificador de otro tenant |
| ID07 | Eventos duplicados iguales devuelven mismo resultado, distintos payloads dan conflicto | Replay altera historia |
| ID08 | `occurredAtUtc` y `observedAtUtc` distinguidos, nunca falsificar run_id | Autorreporte se confunde con runtime nativo |
| ID09 | Evidencia `NOT_EVALUATED` o CI incompleta no satisface gate | Cancelled/skipped se declara success |
| ID10 | Reviewer de A5 no coincide con author, y validación ligada a blob/SHA original | A5 propio o sin diff queda PASS |

**Regla de seguridad:** no afirmar exclusión distribuida de dos escritores por una simple lectura seguida de escritura de Sheets. La futura implementación de CAS debe ofrecer una primitiva condicional atómica real y prueba de un solo ganador; sin ella se rechazan relevos con carreras posibles. El modelo institucional conserva historial original y registra eventos compensatorios, nunca muta BITACORA histórica.

## 4. Límites LM01–LM12 y efectos permitidos

| ID | Límite / política futura |
| --- | --- |
| LM01 | Deshabilitado por defecto; ningún productor ni consumidor instalado |
| LM02 | Sólo ámbito dev de microtarea aprobada; otras ramas requieren orden expresa |
| LM03 | Cuota de emisión/reintento por actor/scope y tenant para impedir abuso |
| LM04 | Backoff finito, dead-letter y retención aprobada antes de habilitación |
| LM05 | Un único owner activo por scope; no dos mutadores |
| LM06 | Idempotencia ligada a versión y payload canonicalizado, no a prompt |
| LM07 | Sanitización fuerte: sin JWT, refresh tokens, PII, secretos o SQL sensible |
| LM08 | Evidencia por checksum y enlace de acceso controlado, no URL pública de artifact |
| LM09 | Correlación y tiempo UTC verificables; nativo vs telemetría separados |
| LM10 | Error, timeout o 5xx equivalen a estado desconocido y relectura, nunca PASS |
| LM11 | Eventual consistencia de dashboards no reemplaza PLAN/COLA/DAG/readback |
| LM12 | Revisión independiente, Scope Lock y rollback son gates, no configuración opcional |

Toda integración futura debe probar denegación cross-tenant A/B, aislamiento RBAC, idempotencia y política de retención usando datos sintéticos; no datos vivos. Si no hay identidad, cuota, versio­nado, secret manager, rollback o infraestructura autorizada, el feature permanece apagado.

## 5. Matriz de escenarios CA01–CA12 — FUTUROS_NO_EJECUTADOS

| Caso | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| CA01 | Evento único genera una sola señal lógica | Replay idéntico duplica effect |
| CA02 | Mismo ID/payload retorna resultado previo | Mismo ID con payload distinto sobrescribe registro |
| CA03 | Tenant A sólo obtiene evidencia de A | ID o clave de B revela datos a A |
| CA04 | Actor con grant JWT válido evalúa caso | Token inválido o permiso revocado publica evento |
| CA05 | Dos versiones en CAS tienen único ganador | Dos escritores con versión antigua publican |
| CA06 | Scope allowlisted dev limita evaluación | Scope comodín amplía ambientes |
| CA07 | CI exact SHA completed/success acredita ejecución | Check queued/cancelled/skipped cuenta como éxito |
| CA08 | Hallazgo probado conserva severidad y retest | P0/P1 abierto se marca resuelto sin prueba |
| CA09 | Correlación opaca une run/evidencia autorizados | Secret/JWT/email ajeno entra en logs |
| CA10 | A5 independiente revisa author/commit/blobs | Autor se revisa a sí mismo |
| CA11 | Error de productor deja estado seguro y reintentable | Timeout dispara release o efecto duplicado |
| CA12 | Prueba futura de rollback en entorno descartable preserva historial | Documento activa webhook/QA/PROD |

Son 12 pares positivos/negativos para futura implementación, **no resultados ejecutados ni métricas de cobertura**. No existe evidencia nueva de cero P0/P1, retest efectivo, restore, pentest, logs de runtime o seguridad productiva por este documento.

## 6. Cierre A0–A5 y reversión

- **A0:** sólo Markdown nuevo y `CHANGELOG_AI.md`, secciones autocontenidas, lenguaje de diseño no desplegado, ausencia de secretos y rutas ajenas.
- **A1:** consistencia EV01–EV12, ID01–ID10, LM01–LM12 y CA01–CA12; pruebas dinámicas `FUTUROS_NO_EJECUTADOS`, no atribuir `dotnet test`.
- **A2:** inspección de JWT/RBAC, tenant A/B, host/media, PII, SHA y A5 para fail-closed conceptual; no test HTTP, MySQL o pentest.
- **A3:** trazabilidad M01.06 + M02.01–03, STANDARD y DAG por ID; .NET/Angular/MySQL/Docker/E2E `NO_APLICA_DOCUMENTAL` si diff sólo de dos Markdown, nunca PASS sintético.
- **A4:** commit fast-forward con control de HEAD, SHA y blobs exactos, CI Scope Lock + procedencia V5 `completed/success`, PLAN/COLA/DAG/BITACORA readback.
- **A5:** otra de las cinco Tasks Javier distinta del autor, pendiente hasta evaluación independiente efectiva. No se publica en QA/main/PROD.

**Rollback documental:** `git revert` normal del changeset y BITACORA aditiva, sin fuerza, sin modificar bases ni historias de ejecución. M02.06 exige además su propio gate de M02.01–05 y A5, no se libera por esta publicación.
