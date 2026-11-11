## V13 — perfil operativo 2026-11-11 (aplicación condicionada al merge protegido)

Este changeset sustituye, DESPUÉS de merge protegido y readback institucional, el alcance histórico de 52 PR por el Plan Comercial Inicial, mantiene la capacidad V13 1000x4 y declara **un revisor independiente con tres auditorías** para PR comercial ordinario. FIFO por PR.created_at, detrás de dev no es conflicto por sí solo, reconciliación causal no forzada, PENDIENTE O=SI desarrollable, G=SI imprescindible para integración, CI exact SHA y postmerge 3/3. PR gobernanza/seguridad/CI mantiene revisión formal genuinamente independiente. Fuente única detallada: `docs/AUTOMATION_V13.md`. Seis Tasks Javier pausadas mientras se prepara esta enmienda; dos Alex no modificadas. El estado nativo debe releerse al reactivarlas. Ninguna parte de esta nota desactiva GitHub Actions o autoriza bypass de FIFO y rulesets.

## Archivo histórico 2026-11-09 — 6 Javier + 2 Alex, V13 vigente sin alterar cuatro techos
Task Javier nueva :10 ID 6af16bb9492c8191b8e8c7d606f11507 y nueva :50 ID 6af16bbad26881918c283abe06e1f32a, distintas de las Tasks Alex :10/:50. La nueva autorización protegida entra en vigor sólo tras review independiente, required CI exact-HEAD, merge a dev y readback del Sheet. Las dos nuevas ya figuran HABILITADAS físicamente (is_enabled=true; TAREAS_PROGRAMADAS M8/M9) tras readback; la escritura comercial sigue NO_GO hasta merge protegido y readback de autoridad en Sheets. Su misión posterior es idéntica a las otras cuatro Javier: recuperar exclusivamente los 52 PR VALIDANDO originales por FIFO, sin nuevos scopes, falsa aprobación ni falsos LISTO. No modificar las Tasks Alex, sus permisos, ni QA/main/PROD.

## V13 — contrato consolidado 1000 x 4 y FIFO multicierre (2026-11-07)

CAPACITY_POLICY_VERSION=V13_1000_FIFO_BATCH

Unica regla operativa de capacidad, FIFO, parada ante autor y desarrollo sin CAS: `docs/AUTOMATION_V13.md`. Sustituye V8/V9/V10/V11/V12 en todo punto contradictorio, manteniendo seguridad, independencia, CI exact-SHA y alcance dev. Esta propuesta NO es vigente antes del merge protegido y readback.

## Contrato técnico de custodia por etapa — PR #3854

El selector consultivo scripts/automation-review-routing.mjs propone revisiones entre TASK_ID distintos; reviewerFor es preferencia y no veto ni reserva, sin bloqueos en Sheets. scripts/automation-stage-ownership.mjs conserva comprobaciones de autoría y transiciones, sin bloqueo externo. GitHub y las validaciones institucionales siguen protegiendo el código y los cierres.

# SOLQARYN — Reglas actuales de trabajo

PROJECT_SCOPE_LOCK=STRICT
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
PROJECT_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
EXECUTION_PLAN_DRIVE_FILE_ID=1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA
CURRENT_COMMERCIAL_ONLY=TRUE
AUTOMATION_RUNTIME=ACTIVE_DEV
TASK_DESIGN=OWNER_CONFIGURABLE_TASK_SET
TASK_COUNT_LIMIT=PROVIDER_EFFECTIVE_CAPACITY
CURRENT_REGISTERED_TASKS=8
AUTO_CERTIFICATION_MODE=REQUIRED_SCOPE_LOCK
FORMAL_APPROVAL_ROUTINE_COMMERCIAL_REQUIRED=FALSE
FORMAL_APPROVAL_GOVERNANCE_REQUIRED=TRUE
GITHUB_CHAT_EXECUTOR=solqaryn-platform
GITHUB_CHAT_REVIEWER=JAVIER_GITHUB_LOGIN
GITHUB_CHAT_ROLE_SEPARATION=STRICT
GITHUB_CHAT_ROLE_POLICY=docs/GITHUB_ACCOUNT_ROLES.md

## ORDEN VIGENTE — V13 1000/1000/1000/1000 CON ASIGNACION AL DISPARARSE LA TASK (2026-11-07)

**Contrato unico V13:** capacidad 1000 en los cuatro controles; FIFO global con multiples cierres por corrida, verificaciones individuales y barrera ante autoria propia, seguido de una PENDIENTE realmente elegible. Sin cambio de los gates de CI, review independiente, seguridad y dependencias. SIN BLOQUEO EN SHEETS, sin CAS ni leases; `docs/AUTOMATION_V13.md` es la referencia no redundante.

## Complemento de CI asíncrona y meta 6 LISTO/h subordinado a V13 (2026-11-05)

**Objetivo de rendimiento:** `THROUGHPUT_TARGET_LISTO_PER_HOUR=6` microtareas distintas realmente certificadas como LISTO en ventana móvil de `THROUGHPUT_WINDOW_MINUTES=60`. Es una **meta operativa mínima**, no una garantía, permiso para falsificar evidencia o sexto cierre ficticio. Medir `LISTO_60M`, `DEFICIT_60M=max(0,6-LISTO_60M)` y razones específicas; solo contar IDs únicos con revisión independiente, merge protegido cuando corresponda, CI obligatoria y postmerge aplicable en verde y PLAN_MAESTRO/COLA/BITACORA readback completo. Si el ledger no está verificado, reportar `METRIC_UNVERIFIED`, nunca cero supuesto ni seis imaginarios.

**CI independiente del turno:** `CI_WAIT_MODE=ASYNC_NO_TASK_IDLE`. Cuando un PR esté VALIDANDO y los checks del SHA exacto figuren QUEUED/IN_PROGRESS, consultar GitHub una vez por scope y por ejecución física; registrar evento deduplicado `CI_PENDING_ASYNC` con scope, SHA, PR, nombres de checks, autor/revisor, hora y próxima comprobación en una ejecución posterior. No hacer sleep, polling prolongado, loops de espera, no ocupar el turno de una Task ni agotar capacidad artificialmente esperando GitHub. El pipeline de Actions puede continuar sus controles por su cuenta. Al detectar fallo real, `CI_FAILED_FIX_SCOPE` y corregir solo ese scope; no certificar por excepción.

**Continuidad de revisión:** una revisión terminada no mantiene bloqueos sobre la microtarea. Si la CI está pendiente, registrar CI_PENDING_ASYNC y continuar por el selector; conservar la autoría, rama y PR. No crear ni renovar reservas de revisión en Sheets. Si otro editor realmente trabaja, no intervenir.

**Trabajo productivo sin interferencia:** tras CI pendiente, continuar VALIDANDO ajena asignable por ruta → EN_PROGRESO recuperable con traspaso explícito → PENDIENTE elegible. Reutilizar GitHub branch/PR sin crear bloqueos; un conflicto positivo detiene solo ese scope. Los comentarios antiguos de Codex no impiden tomar trabajo nuevo cuando ya no hay ejecución activa documentada.

**Cierre estricto:** los required checks no se desactivan ni se usan estados SKIPPED/PENDING como PASS. Al volver verde la CI exact-SHA, un revisor elegible verifica el merge protegido y postmerge aplicable; solo después concilia LISTO institucional. Las revisiones y autorizaciones humanas exigibles siguen intactas. La priorización y la meta no modifican el contrato 1000/1000/1000/1000, ni crean/reanudan las seis Tasks físicas pausadas.

**Implementación comprobable:** scripts/automation-review-routing.mjs y scripts/automation-ci-throughput.mjs se validan sin escribir en negocio. Ningún selector concede una aprobación, permiso, revisión formal o LISTO.


## Traspasos futuros — idle-first, proxima ejecucion, sin preasignacion (2026-11-05)

El selector de próxima Task libre es consultivo y no asigna propiedad antes de la ejecución. La ruta determinista por scope y autoría evita múltiples revisoras para una misma entrega; al iniciarse la Task se revisa estado actual de GitHub y Sheets, sin bloqueos de fila. Recuperaciones EN_PROGRESO requieren traspaso autorizado, ausencia de editor concurrente comprobado y preservación de rama/PR.



## Protocolo vigente de revisión técnica por TASK_ID — orden 2026-11-04

La unidad de autoría funcional de una microtarea comercial es el `TASK_ID` físico autorizado y su evidencia de `PLAN_MAESTRO`/`BITACORA`, no la coincidencia del login GitHub. Una Task habilitada **distinta de la autora** puede revisar técnica y sustantivamente el PR de una Task que use la misma cuenta GitHub, comprobar diff, aislamiento multitenant/RBAC, checks y pruebas exact-SHA, registrar `TECH_REVIEW_PASS` o `REQUEST_CHANGES` con `TASK_ID_AUTOR != TASK_ID_REVISOR`, SHA y readback. Si todo cumple y los permisos y reglas protegidas lo permiten, la Task revisora puede efectuar el merge y el cierre `LISTO` después de CI postmerge exact-SHA y readback de `PLAN_MAESTRO`, `COLA` (fórmulas intactas) y `BITACORA`. **La Task autora no puede emitir aprobación técnica cruzada ni marcar como LISTO su propia microtarea.** Un traspaso de edición requiere autorización positiva y ausencia de edición simultánea comprobada; revisión de solo lectura no concede permiso de escritura.


## Censo autorizado actual — ocho Tasks (seis Javier y dos Alex; adenda de 2026-11-09)

El censo protegido propuesto contiene seis Tasks Javier (:00,:10,:20,:30,:40,:50) y dos Tasks Alex (:10,:50), con TASK_ID únicos. Los nuevos Javier 6af16bb9492c8191b8e8c7d606f11507 y 6af16bbad26881918c283abe06e1f32a se autorizan sólo tras CI y revisión independiente de este PR, merge protegido a dev y readback institucional. Mientras tanto no pueden escribir ni ejecutar trabajos comerciales. Los cuatro límites V13 de 1000 permanecen intactos. Los estados físicos reales son independientes de este censo y deben leerse en la cuenta nativa de cada propietario; las Tasks pausadas por el propietario permanecen pausadas hasta el readback de todas las configuraciones. No reintroducir reglas de cupo, censo o roles retirados.


## Archivo histórico — Autorizacion adicional M3.2 — Javier :40 y Alex :10/:50

Se amplía a 5/6 exclusivamente después de merge protegido y conciliación de Sheets; detalles y pruebas en `docs/AUTOMATION_STATUS.md` y `docs/AUTOMATION_AUTHORITY.md`. GitHub commits de prueba individuales: Javier :40 `46e6a9822d89932697c93e77c96df239d73bed4f`, Alex :10 `37fb117a38959c99c5795130a1a40365693cc0b4` y Alex :50 `b0f5a77757847d623a17c0f1d9c5fde57fd909fd`. BITACORA filas 85, 76 y 81 corroboran. Pruebas consumidas, no repetir. Se excluye Alex :30 hasta evidencia propia contents:write y nueva autorización protegida. Se conservan independencia GitHub de revisores, CI 3/3, Scope Lock STRICT, WIP comercial máximo 6 según M3.4, excluidos QA/main/PROD.

## Archivo histórico — Autorizacion individual Javier :20 (condicionada al merge)

El permiso comercial DEV de PRIMARY_DEV_02 TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3` se concede exclusivamente tras aprobar, fusionar y conciliar este changeset en dev/Sheet. GH contenido commit aaca89247d9aaf61eb21a308c2e43fd5fef24526 y BITACORA A77 verificadas. Permiso de GitHub ramas/PR, PLAN_MAESTRO y BITACORA solo con autoridad por identidad, WIP comercial máximo 6 conforme a M3.4, CI y revision independiente GitHub antes de LISTO. `CONFIG!B57=TRUE` no concede escritura a las otras cuatro Tasks no autorizadas. Nunca autoaprobar bajo la cuenta GitHub autenticada de Javier (ID 93171442). No QA/main/PROD ni despliegues.

## Registro histórico M3.2 — seis Tasks instaladas y autorización parcial

En aquel registro el conjunto físico constaba de seis Tasks distribuidas tres por cuenta. Es una fotografía histórica, no un techo ni una distribución obligatoria. Las pruebas individuales de permisos y la autorización comercial se mantienen ligadas a IDs concretos, no a cuotas de cuenta. El cambio vigente de esta enmienda concede `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=5` tras integración protegida y readback del Sheet: tres Tasks Javier (:00/:20/:40) y dos Tasks Alex (:10/:50), cada una con contenido GitHub y auditoría nativos verificados. Alex :30 permanece sin autorización comercial al carecer de prueba propia de contents:write (intento previamente bloqueado por seguridad); la microfase global sigue parcial y el conteo 5/6 nunca equivale a seis permisos. Ninguna Task nueva se habilita por simplemente existir. La única autoridad de la nueva lógica está en `docs/AUTOMATION_AUTHORITY.md` y el contador efectivo en `docs/AUTOMATION_STATUS.md`.

Una Task elige primero VALIDANDO de otra identidad y después PENDIENTE con TODAS las dependencias LISTO. El revisor corrige directamente en el mismo PR, conservando VALIDANDO. Sus commits requieren revisión independiente y CI del SHA final; jamás autoaprobar. Solo un merge protegido verificado en dev más readback Plan Maestro/Cola/Bitácora permite LISTO. No crear rama almacén ni duplicar ramas; máximo seis scopes comerciales distintos globales, hasta dos por Task autorizada con un único editor activo y solo segunda entrega tras VALIDANDO, conforme a M3.4. El runtime no es indefinido: registrar commits y punto de continuación ante interrupción.

Las reglas históricas del piloto 1+1 que aparecen abajo son archivo de trazabilidad y no limitan las capacidades v2.0. VAEP y Jules siguen retirados; QA, main, PROD y secretos fuera de alcance.

## Archivo histórico v1.2 — 2026-10-23 — PILOTO DEV PRIMARY AUTORIZADO POR EL PROPIETARIO (reemplaza gates historicos de microfase 3)

El propietario ordena reanudar las microtareas DEV ahora. PRIMARY_01 existente de Javier es el unico escritor automatizado autorizado, SECONDARY_02 de Alex permanece revisor READ_ONLY. Se limita a solqaryn/Solqaryn, base dev, rama especifica por microtarea del Plan Comercial Inicial y PR contra dev. Prohibido merge autonomo, QA, main, PROD, Render, Vercel, secretos o cambios productivos.

El RUN_ID, RUN_URL, TASK_ID_NATIVO_DEL_RUN, correo ChatGPT, RUN_COMPLETED_NATIVO, REVIEWED_RUN_ID y EVIDENCE_REF_AUTENTICADO no son condiciones de elegibilidad ni certificacion de trabajo DEV cuando ChatGPT no los expone. NO inventar campos. Evidencia operativa suficiente: TASK_ID configurado, ID de microtarea, HEAD base real, rama y PR/commit GitHub verdaderos, checks de CI y evidencia de readback. El 4/4 de ejecuciones de diagnostico READ_ONLY deja de ser requisito del piloto DEV.

Control de colisiones: un solo escritor Primary con WIP=1, comprobar ramas/PR activos del mismo ID antes de iniciar; continuar el PR existente en vez de duplicar. GitHub proporciona HEAD y proteccion de PR. Sin CAS ni leases en Sheets. Alex revisa un PR/SHA concreto, sin REVIEWED_RUN_ID de ChatGPT ni aprobacion ficticia. Se mantienen Project Scope Lock, permisos reales, seguridad multiempresa, CI exact-head y revision humana independiente cuando GitHub lo exija.

Esta habilitacion se vuelve efectiva unicamente al fusionar con checks obligatorios este contrato a dev y comprobar el HEAD nuevo. Los prompts programados deben validar la autoridad viva antes de escribir.
## Autoridades vigentes

1. `docs/PROJECT_SCOPE_LOCK.md` regula la identidad de SOLQARYN y el acceso a sus fuentes.
2. `docs/AUTOMATION_STATUS.md` declara si existe un motor programado autorizado.
3. El documento `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` es la fuente actual del alcance comercial. Ninguna hoja, manifiesto, bitácora o plan histórico es ejecutable por inferencia.
4. `docs/AUTOMATION_AUTHORITY.md` conserva el inventario actual de seis Task IDs (cuatro Javier y dos Alex) y la separación de permisos; `docs/AUTOMATION_V13.md` consolida los cuatro techos 1000 y el FIFO multicierre solo al integrarse por PR protegido. Los cupos seis, la topología 3+3 y la antigua autorización de cinco Tasks son antecedentes, NO reglas operativas. Sin QA/main/PROD ni reactivación de VAEP. Gobierno paritario: `PROPIETARIA_INSTITUCIONAL` SOLQARYN Platform; `EJECUTOR_PRINCIPAL_01` Javier (`93171442`) y `EJECUTOR_PRINCIPAL_02` Alex (`morales35alex`) conservan sus identidades independientes.
5. La única Skill local permitida sigue siendo `solqaryn-project-governance`; las referencias externas son opcionales y se consultan únicamente para una necesidad técnica real.

## Archivo histórico — estado de automatización al 22-10-2026 (sin vigencia operativa)

El propietario informó que eliminó las diez Tasks de ChatGPT y la hoja operativa antigua. **En aquel corte no había Tasks nuevas autorizadas, y se habían retirado las diez Tasks anteriores, workers Jules, coordinadores y cola VAEP.** No crear o reconstruir Tasks, activar cron, restaurar Sheets, reclamar leases, realizar despachos comerciales ni interpretar evidencias históricas como instrucciones vigentes. Todo motor nuevo necesita diseño y autorización explícitos; esta regla no impide el trabajo manual solicitado.

## Archivo histórico — enmienda v1.2 previa al piloto DEV

Inicialmente se diseñaron dos Tasks independientes, una de Javier y otra de Alex, sin permiso de operación. En aquel momento `AUTOMATION_RUNTIME=RETIRED` y `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0` eran los valores vigentes. Esa fotografía quedó sustituida por el piloto `ACTIVE_DEV` autorizado el 23-10-2026. No se trasladan permisos de cuenta, ni jerarquías humanas, ni autorizaciones de QA/PROD.

## Aprobaciones delegables con controles intactos (23-10-2026)

La norma única de selección flexible reside en `docs/AUTOMATION_AUTHORITY.md`, sección 7.1. Una aprobación pendiente de GitHub, Render, Vercel, Google Sheets/Drive u otra plataforma podrá dirigirse a **Javier, Alex o SOLQARYN Platform**, según disponibilidad y permisos individuales **efectivamente comprobados**, sin prioridad fija. Ninguna cuenta puede autoaprobar un cambio cuando se exige revisión independiente; la cuenta institucional necesita representante humano identificable e independiente si el proveedor exige esa independencia. Respetar las aprobaciones nativas, checks, controles del proveedor, límites de entorno y evidencia; ninguna disponibilidad habilita Tasks o PROD.

## Archivo histórico — microfase 3 READ_ONLY, superada como puerta de ejecución DEV

El 23-10-2026 se constataron dos Tasks físicas, Primary Javier (`6adbb9fff1548191a1bd5b30d9e9984a`) y Secondary Alex (`6adbbc9ad9e88191bea8eb69104afb25`). En aquel corte la certificación individual estaba en `0/4` y el runtime `RETIRED`. Esa evidencia se conserva en `docs/AUTOMATION_STATUS.md` e Issue #3730 exclusivamente como historia; ni sus horarios anteriores ni `RUN_ID`/`EVIDENCE_REF` condicionan el piloto DEV vigente. El modo y permisos actuales están únicamente en las claves de `docs/AUTOMATION_STATUS.md`, y la revisión de Alex exige evidencia GitHub del PR/SHA, no acceso a chats privados.

## Trabajo de desarrollo

- Trabajar en `solqaryn/Solqaryn`, rama `dev`, por PR y fusión protegida. Consultar el HEAD actual y no sobrescribir trabajo concurrente.
- Cambios de código tienen pruebas proporcionales, revisión causal, diffs auditables y rollback. Un plan histórico no puede imponer hitos ni gates no relacionados.
- Conservar JWT, BCrypt, RBAC, aislamiento A/B por empresa, auditoría, seguridad multiempresa, integridad de MySQL, respaldos, controles de secretos y prevención de fugas; no rebajarlos para ahorrar tiempo.
- Frontend Angular 22, backend .NET 10, MySQL EF Core, Cloudinary, SMTP y QuestPDF permanecen bajo la arquitectura vigente.
- Las pruebas de producto de CI son distintas de las automatizaciones que coordinaban Tasks. No usar un job llamado `automatizacion` como criterio único para borrarlo: verificar sus consumidores.
- No promover a QA/main/PROD, alterar datos productivos, secretos, dominios ni pagar servicios sin autorización nueva y explícita.
- `docs/VAEP_AUTHORITY.md` ya no existe en el árbol vivo `dev` (solo puede consultarse en historia Git); su autoridad antigua quedó retirada y jamás es fallback.

## Cierre

Mantener `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md`, `ARCHITECTURE.md`, `ARCHITECTURE_CHANGELOG.md` y `CHANGELOG_AI.md` cuando el cambio lo amerite. Documentar cualquier acción humana pendiente con enlace e instrucciones concretas.
