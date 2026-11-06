## Contrato técnico de custodia por etapa — PR #3854

El selector de scripts/automation-review-routing.mjs reparte revisiones de forma determinista entre TASK_ID distintos, sin registrar bloqueos en Sheets. scripts/automation-stage-ownership.mjs conserva comprobaciones de autoría y transiciones, sin bloqueo externo. GitHub y las validaciones institucionales siguen protegiendo el código y los cierres.

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
CURRENT_REGISTERED_TASKS=6
AUTO_CERTIFICATION_MODE=REQUIRED_SCOPE_LOCK
FORMAL_APPROVAL_ROUTINE_COMMERCIAL_REQUIRED=FALSE
FORMAL_APPROVAL_GOVERNANCE_REQUIRED=TRUE
GITHUB_CHAT_EXECUTOR=solqaryn-platform
GITHUB_CHAT_REVIEWER=JAVIER_GITHUB_LOGIN
GITHUB_CHAT_ROLE_SEPARATION=STRICT
GITHUB_CHAT_ROLE_POLICY=docs/GITHUB_ACCOUNT_ROLES.md

## ORDEN VIGENTE — 15/15/15 CON ASIGNACION AL DISPARARSE LA TASK (2026-11-05)

**Precedencia:** orden expresa del propietario: distribucion directa por microtarea, SIN BLOQUEO EN SHEETS, sin compare-and-swap, sin escritor de reservas ni control de vencimiento. El contrato 15/15/15, la autoria independiente, los permisos y PROJECT_SCOPE_LOCK=STRICT permanecen.

- MAX_CONCURRENT_SCOPES=15; MAX_OWNED_SCOPES_PER_TASK=15; MAX_ACTIVE_WRITES_PER_TASK=15. Son techos de scopes distintos, NO instruccion de reclamar quince trabajos por Task.
- La propia Task selecciona trabajo cuando su ocurrencia nativa se inicia; no se apartan scopes ni se escriben propietarios por anticipado. Solo usa el plan comercial vigente, sus dependencias, PLAN_MAESTRO/COLA, BITACORA y GitHub live; la historia no impide un nuevo trabajo libre.
- **Reparto sin reservador:** para cada scope, el selector scripts/automation-review-routing.mjs determina exactamente una Task revisora distinta del autor usando los seis IDs autorizados y preferencia fija solo para las cinco entregas actualmente VALIDANDO. El resultado es routing consultivo (no propiedad, no lease, no bloqueo en Sheets). Si un actor no es el seleccionado, debe continuar con otro scope correspondiente a su ruta. El routing debe ser identico para todas las Tasks; no elegir a dos revisoras para un scope.
- Primero atender VALIDANDO ajeno realmente ejecutable; despues EN_PROGRESO propio recuperable; para desarrollo P1 seleccionar PENDIENTE con COLA!O=SI (DESARROLLO_HABILITADO, que acepta dependencia VALIDANDO o LISTO). COLA!G conserva el requisito ESTRICTO de dependencias LISTO para fusionar y certificar la sucesora. Al pasar una predecesora a VALIDANDO la formula O libera automaticamente el siguiente trabajo de desarrollo, sin esperar GitHub CI. Ninguna Task debe hacer polling, reservar scopes en Sheets ni ignorar conflictos POSITIVOS de editores; AUTHOR_TASK_ID y revision independiente se preservan.
**DEPENDENCY_PIPELINE_RELEASE_V7 (decisión operativa del propietario, 2026-11-05):** en COLA la columna O `DESARROLLO_HABILITADO` es una formula automatica de inicio para P1: `PENDIENTE`, responsable vacio, todas las dependencias en `VALIDANDO` o `LISTO`, `SI` (maximo dos dependencias conforme al contrato actual). La columna G `DEPENDENCIAS_OK` mantiene la semantica original `LISTO` ESTRICTO para integracion, merge y certificacion final, NO se reescriben sus formulas ni se cambia la huella A:N. Ninguna CI pendiente de una predecesora detiene el comienzo de otra microtarea: desarrollo e inspeccion causal pueden avanzar en rama unica, pero si requieren codigo aun no mergeado no inventar resultado ni integrar anticipadamente. Las seis Tasks leen O al iniciar PENDIENTE libre, priorizan VALIDANDO revisable, EN_PROGRESO propio y fallback PENDIENTE, respetando WIP 15/15/15, unicidad GitHub, permisos y no CAS. Al entregar PR a VALIDANDO se libera al autor; un sucesor SOLO pasa a LISTO con dependencias G=SI, gates propios y PR protegido/CI exact-SHA completos. No crear automatizaciones ni emplear `run_now` como sustituto de la programacion nativa.

- Al iniciar una edicion, contrastar GH branch/PR y el responsable actual. Mantener una sola rama/PR por scope. GitHub protege el commit y los cambios concurrentes con sus controles nativos. Nunca recrear una rama/PR existente ni forzar pushes, reescribir historia o editar otro scope activo. Ante conflicto real, parar solo ese scope y seleccionar otro.
- La revision tecnica solo puede hacerla otra TASK_ID diferente del AUTHOR_TASK_ID. No convertir un login compartido en GitHub APPROVED formal; no aprobarse a si misma, no editar entregas de Codex ocupadas. Los cierres exigen CI exact-SHA requerida, merge protegido aplicable, validacion independiente y conciliacion PLAN_MAESTRO/COLA/BITACORA.
- Cuando la CI siga en curso, registrar CI_PENDING_ASYNC una vez, continuar otra tarea de la misma ruta sin esperar el workflow. Al liberarse trabajo no hay reserva que cancelar: conservar autor y PR, documentar estado y finalizar el turno.
- Nunca crear Tasks, cambiar sus RRULE, habilitar QA/main/PROD ni desactivar checks exigidos. Si falta informacion critica de permisos o hay conflicto positivo, NO_GO solo en el scope afectado; no convertir la falta de una reserva historica en bloqueo global.

## COMPLEMENTO OPERATIVO VIGENTE — CI ASÍNCRONA Y META 6 LISTO/H (2026-11-05)

**Objetivo de rendimiento:** `THROUGHPUT_TARGET_LISTO_PER_HOUR=6` microtareas distintas realmente certificadas como LISTO en ventana móvil de `THROUGHPUT_WINDOW_MINUTES=60`. Es una **meta operativa mínima**, no una garantía, permiso para falsificar evidencia o sexto cierre ficticio. Medir `LISTO_60M`, `DEFICIT_60M=max(0,6-LISTO_60M)` y razones específicas; solo contar IDs únicos con revisión independiente, merge protegido cuando corresponda, CI obligatoria y postmerge aplicable en verde y PLAN_MAESTRO/COLA/BITACORA readback completo. Si el ledger no está verificado, reportar `METRIC_UNVERIFIED`, nunca cero supuesto ni seis imaginarios.

**CI independiente del turno:** `CI_WAIT_MODE=ASYNC_NO_TASK_IDLE`. Cuando un PR esté VALIDANDO y los checks del SHA exacto figuren QUEUED/IN_PROGRESS, consultar GitHub una vez por scope y por ejecución física; registrar evento deduplicado `CI_PENDING_ASYNC` con scope, SHA, PR, nombres de checks, autor/revisor, hora y próxima comprobación en una ejecución posterior. No hacer sleep, polling prolongado, loops de espera, no ocupar el turno de una Task ni agotar capacidad artificialmente esperando GitHub. El pipeline de Actions puede continuar sus controles por su cuenta. Al detectar fallo real, `CI_FAILED_FIX_SCOPE` y corregir solo ese scope; no certificar por excepción.

**Continuidad de revisión:** una revisión terminada no mantiene bloqueos sobre la microtarea. Si la CI está pendiente, registrar CI_PENDING_ASYNC y continuar por el selector; conservar la autoría, rama y PR. No crear ni renovar reservas de revisión en Sheets. Si otro editor realmente trabaja, no intervenir.

**Trabajo productivo sin interferencia:** tras CI pendiente, continuar VALIDANDO ajena asignable por ruta → EN_PROGRESO recuperable con traspaso explícito → PENDIENTE elegible. Reutilizar GitHub branch/PR sin crear bloqueos; un conflicto positivo detiene solo ese scope. Los comentarios antiguos de Codex no impiden tomar trabajo nuevo cuando ya no hay ejecución activa documentada.

**Cierre estricto:** los required checks no se desactivan ni se usan estados SKIPPED/PENDING como PASS. Al volver verde la CI exact-SHA, un revisor elegible verifica el merge protegido y postmerge aplicable; solo después concilia LISTO institucional. Las revisiones y autorizaciones humanas exigibles siguen intactas. La priorización y la meta no modifican el contrato 15/15/15, ni crean/reanudan las seis Tasks físicas pausadas.

**Implementación comprobable:** scripts/automation-review-routing.mjs y scripts/automation-ci-throughput.mjs se validan sin escribir en negocio. Ningún selector concede una aprobación, permiso, revisión formal o LISTO.


## Traspasos futuros — idle-first, proxima ejecucion, sin preasignacion (2026-11-05)

El selector de próxima Task libre es consultivo y no asigna propiedad antes de la ejecución. La ruta determinista por scope y autoría evita múltiples revisoras para una misma entrega; al iniciarse la Task se revisa estado actual de GitHub y Sheets, sin bloqueos de fila. Recuperaciones EN_PROGRESO requieren traspaso autorizado, ausencia de editor concurrente comprobado y preservación de rama/PR.



## Protocolo vigente de revisión técnica por TASK_ID — orden 2026-11-04

La unidad de autoría funcional de una microtarea comercial es el `TASK_ID` físico autorizado y su evidencia de `PLAN_MAESTRO`/`BITACORA`, no la coincidencia del login GitHub. Una Task habilitada **distinta de la autora** puede revisar técnica y sustantivamente el PR de una Task que use la misma cuenta GitHub, comprobar diff, aislamiento multitenant/RBAC, checks y pruebas exact-SHA, registrar `TECH_REVIEW_PASS` o `REQUEST_CHANGES` con `TASK_ID_AUTOR != TASK_ID_REVISOR`, SHA y readback. Si todo cumple y los permisos y reglas protegidas lo permiten, la Task revisora puede efectuar el merge y el cierre `LISTO` después de CI postmerge exact-SHA y readback de `PLAN_MAESTRO`, `COLA` (fórmulas intactas) y `BITACORA`. **La Task autora no puede emitir aprobación técnica cruzada ni marcar como LISTO su propia microtarea.** Un traspaso de edición requiere autorización positiva y ausencia de edición simultánea comprobada; revisión de solo lectura no concede permiso de escritura.


## Prioridad de revisión y cierre — corrección operativa 2026-11-04


## Censo autorizado actual — seis Tasks físicas (2026-11-05)

El allowlist vigente incluye únicamente los cuatro TASK_ID de Javier (:00, :20, :30, :40) y los dos de Alex (:10, :50), según `REGISTERED_TASK_IDS` y `AUTHORIZED_COMMERCIAL_TASK_IDS` en AUTOMATION_AUTHORITY y AUTOMATION_STATUS. El máximo 15/15/15 NO registra quince Tasks ni reactiva ninguna. Los estados físicos reales son independientes de este censo y deben leerse en la cuenta nativa de cada propietario; las Tasks pausadas por el propietario permanecen pausadas hasta el readback de todas las configuraciones. No reintroducir reglas de cupo, censo o roles retirados.


## Autorizacion adicional M3.2 — Javier :40 y Alex :10/:50

Se amplía a 5/6 exclusivamente después de merge protegido y conciliación de Sheets; detalles y pruebas en `docs/AUTOMATION_STATUS.md` y `docs/AUTOMATION_AUTHORITY.md`. GitHub commits de prueba individuales: Javier :40 `46e6a9822d89932697c93e77c96df239d73bed4f`, Alex :10 `37fb117a38959c99c5795130a1a40365693cc0b4` y Alex :50 `b0f5a77757847d623a17c0f1d9c5fde57fd909fd`. BITACORA filas 85, 76 y 81 corroboran. Pruebas consumidas, no repetir. Se excluye Alex :30 hasta evidencia propia contents:write y nueva autorización protegida. Se conservan independencia GitHub de revisores, CI 3/3, Scope Lock STRICT, WIP comercial máximo 6 según M3.4, excluidos QA/main/PROD.

## Autorizacion individual Javier :20 (condicionada al merge)

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
4. `docs/AUTOMATION_AUTHORITY.md` y `docs/AUTOMATION_AUTHORITY.md` regulan un inventario configurable por la propietaria, con permisos comprobados individualmente. El censo de seis Tasks y la distribución actual 3+3 son fotografía, no techo ni autorización. Se preservan el allowlist comercial vigente de cinco Tasks y el WIP máximo seis scopes. El piloto v1.2 permanece como antecedente. No autoriza QA/main/PROD ni restaura VAEP. Gobierno paritario: `PROPIETARIA_INSTITUCIONAL` SOLQARYN Platform; `EJECUTOR_PRINCIPAL_01` Javier (`93171442`) y `EJECUTOR_PRINCIPAL_02` Alex (`morales35alex`) son pares humanos, sin jerarquía ni transferencia de permisos.
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
