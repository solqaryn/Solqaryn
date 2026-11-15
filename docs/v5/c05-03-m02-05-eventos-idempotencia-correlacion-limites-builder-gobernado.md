# SOLQARYN V16 — C05-03.M02.05: eventos contractuales, idempotencia, correlación y límites del builder

**ID:** C05-03.M02.05, PLAN_MAESTRO fila 804; **owner** PRIMARY DEV 05 JAVIER (`TASK_ID=6adc2e55f47c81919f6374e561687076`), `solqaryn/Solqaryn@dev`; PROJECT_SCOPE_LOCK=STRICT; **riesgo V15 R0 exclusivamente documental**; 2026-11-15. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN/COLA/DAG institucional.

**Resultado exigido:** definir intenciones de eventos del builder gobernado, claves de idempotencia, correlación, duplicados, rechazo seguro y límites, **sin implementar eventos ejecutables, bus, dispatcher, webhook, worker, trigger, outbox, tabla, API ni side effects**. Esta documentación no habilita ejecución automática entre Tasks.

## 1. Dependencias y límites de autoridad

`C05-03.M01.06` mantiene BL01–BL16, RB01–RB10, LG01–LG12, decisiones D01–D08 PENDIENTES. `M02.01` modeló entidades/invariantes; `M02.02` contratos API/DTO/errores hipotéticos; `M02.03` propuso persistencia e índices sin ejecución; `M02.04` propuso 12 flags todos OFF y configuración sin cambiar valores reales. DAG F765=VALIDADO/G765=SI, COLA G804/O804=SI permitieron este trabajo R0, nunca M03 ni publicación pública.

**No se cambia:** backend ASP.NET Core 10, JWT/RBAC, tenancy, Angular 22.2.1, MySQL, EF, bases reales, secrets, CDN, correo, APIs, trabajadores, runners, CI, horarios, QA/main/PROD. Un evento futuro no reemplaza autorización por tenant, rol, propiedad del recurso, revisión o host verificado. Ninguna clave se deriva directamente de un header arbitrario de cliente para elegir tenant.

## 2. Doce intenciones de eventos EV01–EV12 — FUTUROS_NO_IMPLEMENTADOS

| ID | Evento propuesto | Trigger futuro condicionado, SIN dispatcher |
| --- | --- | --- |
| EV01 | DraftCreated | persistencia de borrador autenticado y tenant propio |
| EV02 | SectionAdded | sección allowlist tipada agregada con CAS |
| EV03 | SectionUpdated | property schema validado, sin HTML/JS/CSS libre |
| EV04 | SectionRemoved | operación autorizada, historial sin hard-delete |
| EV05 | RevisionCommitted | revisión monotónica, transacción y ETag aprobados |
| EV06 | PreviewRequested | registro mínimo read-only, **sin ningún side effect** |
| EV07 | ApprovalRequested | autor y revisor permitidos, revisión vinculada |
| EV08 | ApprovalGranted | RBAC independiente, aprobación vigente y revocable |
| EV09 | PublicationRequested | host propio acreditado, aprobación y revisión actuales |
| EV10 | PublicationSucceeded | proyección mínima, sin drafts/PII privados |
| EV11 | PublicationRevoked | revocación invalida caches/lecturas futuras |
| EV12 | RollbackRecorded | reversa aditiva preservando historial y auditoría |

**Estado de los 12:** exclusivamente nombres, disparadores y límites escritos aquí; **0 publicados y 0 ejecutados**. `PreviewRequested` no puede crear un evento a proveedor ni disparar worker de forma indirecta. Los intentos denegados son auditoría mínima controlada, no promesa de publicación.

## 3. Doce claves y defensas IDEM01–IDEM12

| ID | Control futuro | Comportamiento ante repetición/conflicto |
| --- | --- | --- |
| IDEM01 | CorrelationId asignado server-side | no confiar en ID externo para autorización |
| IDEM02 | Clave única tenant+actor+operación+recurso | actor B nunca reutiliza clave A |
| IDEM03 | Revision/version en hash de operación mutable | mismo key con payload distinto => 409/denegar |
| IDEM04 | Scope y expiry limitados de clave | expirado => revalidar auth y estado, no replay privilegiado |
| IDEM05 | CAS antes de persistencia | stale write => conflicto sin lost update |
| IDEM06 | Dedupe atómica transaction-scoped | duplicado exacto => resultado consistente, no dos writes |
| IDEM07 | Host y projection tenant-scoped | no mezclar cache A/B ni usar Host header como dueño |
| IDEM08 | Approval vinculada a revision/rol | revocación invalida publicación posterior |
| IDEM09 | Outbox futuro solo bajo autorización R2 | no habilitar cola/bus por existir este contrato |
| IDEM10 | Reintentos futuros acotados y con hipótesis | no retry infinito ni tormenta de workers |
| IDEM11 | Auditoría redactada y acotada | sin JWT, PII, secretos, payload completo ni IDs B |
| IDEM12 | Rollback idempotente/append-only | duplicado no elimina ni altera historia ajena |

Las garantías de atomicidad requerirán implementación y prueba real MySQL 8.4 efímero; un `get` seguido por `write` en Sheets o BD **no es CAS atómico**. Tampoco se introduce outbox ni se registra `PASS` de replay.

## 4. Ocho límites y errores seguros LIM01–LIM08

| ID | Política futura |
| --- | --- |
| LIM01 | Bytes, cantidad de secciones y profundidad siempre acotados por política aprobada |
| LIM02 | Carga excesiva/recursiva devuelve error seguro sin efectos |
| LIM03 | Ventana de idempotencia con TTL y número de reintentos finitos |
| LIM04 | Publicación exige permisos/lease/approval/revisión vigente de tenant correcto |
| LIM05 | URL destino debe pertenecer allowlist; negar javascript, file, localhost y redes internas |
| LIM06 | Un fallo externo no degrada JWT/RBAC ni se publica contenido por fallback |
| LIM07 | Logs minimizados con retención limitada y correlación, no secrets ni PII |
| LIM08 | Errores 403/404/409/422/429 no revelan datos, SQL o existencia ajena |

Valores concretos de límites, plazos, retries y capacidad quedan sujetos a D01–D08 y medición posterior. No configurar umbrales reales en este documento ni activar workflows.

## 5. Doce pares de escenarios T01–T12 — SOLO FUTUROS, 0 ejecutados

| ID | Positivo futuro | Negativo/falla que debe detectarse |
| --- | --- | --- |
| T01 | DraftCreated en tenant A con permiso | B intenta crear sobre página A: deny |
| T02 | Duplicate exacto de EV01 devuelve mismo resultado | mismo key/payload cambiado da conflicto |
| T03 | Sección allowlist actualiza revisión | script HTML/JS o CSS libre rechazados |
| T04 | CAS con versión correcta registra una revisión | CAS stale no sobrescribe |
| T05 | Preview autorizado es read-only | preview no envía mail ni crea worker/evento |
| T06 | Reviewer vigente aprueba revisión propia | rol revocado no puede aprobar |
| T07 | Host verificado proyecta contenido mínimo | host falsificado no revela draft/tenant B |
| T08 | Publicación aprobada registra una sola vez | replay no publica dos versiones |
| T09 | Revocación bloquea futuros accesos | token/lease expirado no reautoriza |
| T10 | Rollback permitido crea historia aditiva | duplicado o acceso B no borra A |
| T11 | Payload dentro de límites acepta solicitud | sobrecuota o reintento masivo da 422/429 |
| T12 | Auditoría minimizada permite correlación | log no expone JWT/PII/SQL/secretos |

**12 pares documentados, 0/12 pruebas funcionales/HTTP/DB/browser ejecutadas.** La fase de implementación R2 requerirá asserts genuinos de replay, conflicto, ownership A/B, revocación, límites y errores, xUnit, MySQL 8.4 `Category=Integration` si se toca persistencia y Playwright si UI cambia. Un documento de eventos NO implementa un dispatcher.

## 6. Evidencia A0–A5 y reversión del R0

- **A0:** alcance M02.05, autoridades, EV01–12, IDEM01–12, LIM01–08, T01–12, precedentes y diff acotado a documento + sección completa CHANGELOG.
- **A1:** coherencia de operación/duplicado/conflicto, límites y escenarios futuros; ninguno fue ejecutado.
- **A2:** tenant A/B, JWT/RBAC, propiedad, revocación, SSRF/XSS, CAS, replay y PII solo documentados; NO_APLICA pruebas runtime al diff Markdown.
- **A3:** trazabilidad M01.06/M02.01–04; .NET/Angular/MySQL/EF/HTTP/Playwright NO_APLICA por no cambiar ejecutables.
- **A4:** publicación fast-forward/CAS `dev`, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` **completed/success por SHA**, PLAN/COLA/DAG/BITACORA A:Z readback.
- **A5:** Task Javier distinta del autor debe revisar de forma independiente y puede rechazar. `LISTO_DEV` no implica A5_PASS ni desbloquea tareas runtime sin DAG.

**Rollback:** `git revert` normal tras revalidar HEAD; conservar BITACORA, nunca force-push. D01–D08 y LG01–LG12 siguen pendientes; no QA/main/PROD ni datos productivos.
