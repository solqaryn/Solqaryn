## Contrato técnico de custodia por etapa — PR #3854

El validador `scripts/automation-stage-ownership.mjs` implementa la liberación causal y la reserva exclusiva de revisión dentro del contrato vigente 15/15/15. Separar siempre `AUTHOR_TASK_ID` histórico, `STAGE_OWNER_TASK_ID` y `REVIEW_RESERVATION_TASK_ID`. El resultado `REVIEW_RESERVATION_CANDIDATE` es solo una propuesta: reservar revisión requiere lease/version exclusivo con readback GitHub, PLAN_MAESTRO y BITACORA antes de editar. Una entrega `VALIDANDO` sin recibo de liberación NO deja libre al autor; una entrega liberada no consume su cupo individual pero sí WIP global. No usar `RECOVER_VALIDANDO` sobre la propia autoría. El módulo solo valida pruebas de transiciones, no crea una transacción multi-proveedor. `docs/AUTOMATION_AUTHORITY.md` continúa siendo autoridad y `docs/AUTOMATION_STATUS.md` es censo.

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

**Precedencia:** esta política sustituye íntegramente los contratos de cupos anteriores. La única autoridad vigente es 15/15/15, con exclusividad por scope, revisión independiente, CAS y PROJECT_SCOPE_LOCK=STRICT.

- `MAX_CONCURRENT_SCOPES=15`: maximo global de quince microtareas comerciales distintas en EN_PROGRESO o VALIDANDO (no PR de gobierno, PENDIENTE, historia ni tareas ya LISTO).
- `MAX_OWNED_SCOPES_PER_TASK=15` y `MAX_ACTIVE_WRITES_PER_TASK=15`: techo de hasta quince scopes DISTINTOS por Task fisica autorizada. Un scope tiene un unico editor y una unica reserva de revisor, con CAS/lease/version/readback. No dos Tasks simultaneas en el mismo scope, branch ni PR; el techo no garantiza quince runs concurrentes del proveedor.
- **Asignacion unicamente durante una ejecucion nativa real de la Task**: cada Task se despierta, valida su propia identidad, hora, permisos, RUN, estado y carga, estado vivo de las seis Tasks autorizadas, cola, dependencias, autorias, reservas, leases y GitHub. Solo entonces reclama para SI MISMA un scope libre y elegible, con CAS/readback; no pre-asignar scopes para una Task que todavia no se ha disparado. No reservar dos veces el mismo scope mientras otro permanece libre.
- **Prioridad invariable**: (1) VALIDANDO sin reviewer/lease, ajena a la propia autoria, para revisar/corregir/cerrar; (2) EN_PROGRESO realmente abandonada, con ausencia comprobada de run/editor/lease y handoff autorizado en rama original; (3) PENDIENTE elegible, con TODAS sus dependencias LISTO y cupo global disponible. Nunca tomar trabajo activo ni inferir abandono por tiempo transcurrido.
- **Distribucion inteligente**: preferir Tasks habilitadas genuinamente LIBRES (cero scopes/reservas propias). Para traspasos futuros, desempatar por la proxima ejecucion REAL verificada mas cercana dentro del pool y cuenta autorizados. Una Task con trabajo solo toma un scope adicional si no hay otra Task genuinamente libre disponible y los scopes son independientes; no bloquear a una Task que ya se disparo solo por la proximidad teorica de otra sin prueba de elegibilidad. La seleccion NO equivale a autorizacion, reserva ni escritura.
- El AUTOR_TASK_ID permanece historico; el autor que libero una entrega VALIDANDO no puede revisarla/certificarla, aunque tenga capacidad. Se requiere otra Task_ID para TECH_REVIEW, sin simular GitHub APPROVED formal ni eludir rulesets. VALIDANDO -> LISTO requiere evidencia causal, CI exact-SHA, merge protegido cuando aplique y readback PLAN_MAESTRO/COLA (formulas intactas)/BITACORA.
- Si la CI esta pendiente, registrar estado y continuar otro scope elegible sin esperar pasivamente. Cuando **ningun scope** sea elegible en ese orden, solamente auditar, registrar quien trabaja en los scopes activos y las razones precisas de no elegibilidad, sin interferir ni inventar LISTO.
- El tope 15 no crea ni activa Tasks nuevas: conservamos seis TASK_ID autorizados (cuatro Javier, dos Alex). No tocar QA/main/PROD, secretos, credenciales, seguridad multitenant, fuerza de ramas ni permisos.



## COMPLEMENTO OPERATIVO VIGENTE — CI ASÍNCRONA Y META 6 LISTO/H (2026-11-05)

**Objetivo de rendimiento:** `THROUGHPUT_TARGET_LISTO_PER_HOUR=6` microtareas distintas realmente certificadas como LISTO en ventana móvil de `THROUGHPUT_WINDOW_MINUTES=60`. Es una **meta operativa mínima**, no una garantía, permiso para falsificar evidencia o sexto cierre ficticio. Medir `LISTO_60M`, `DEFICIT_60M=max(0,6-LISTO_60M)` y razones específicas; solo contar IDs únicos con revisión independiente, merge protegido cuando corresponda, CI obligatoria y postmerge aplicable en verde y PLAN_MAESTRO/COLA/BITACORA readback completo. Si el ledger no está verificado, reportar `METRIC_UNVERIFIED`, nunca cero supuesto ni seis imaginarios.

**CI independiente del turno:** `CI_WAIT_MODE=ASYNC_NO_TASK_IDLE`. Cuando un PR esté VALIDANDO y los checks del SHA exacto figuren QUEUED/IN_PROGRESS, consultar GitHub una vez por scope y por ejecución física; registrar evento deduplicado `CI_PENDING_ASYNC` con scope, SHA, PR, nombres de checks, autor/revisor, hora y próxima comprobación en una ejecución posterior. No hacer sleep, polling prolongado, loops de espera, no ocupar el turno de una Task ni agotar capacidad artificialmente esperando GitHub. El pipeline de Actions puede continuar sus controles por su cuenta. Al detectar fallo real, `CI_FAILED_FIX_SCOPE` y corregir solo ese scope; no certificar por excepción.

**Liberación de reserva de revisión:** si la revisión técnica independiente ya terminó pero la CI sigue pendiente, liberar el lease de REVIEWER solo cuando esté permitido y quede acreditado mediante CAS/version/readback de GitHub y PLAN_MAESTRO/BITACORA. Preservar el AUTHOR_TASK_ID, la rama, PR y SHA; la microtarea continúa `VALIDANDO` y cuenta en el WIP global 15. Si el reviewer aún no concluyó, continuar su revisión sin suplantarla. Sin recibo válido, la reserva sigue exclusiva y NO se crea una segunda.

**Trabajo productivo sin interferencia:** tras registrar CI pendiente, elegir de nuevo trabajo `VALIDANDO` ajeno libre → `EN_PROGRESO` realmente abandonada con handoff → `PENDIENTE` elegible y dependencias LISTO, prefiriendo Tasks nativas libres y evitando una segunda responsabilidad arbitraria. Solo la Task que se disparó puede aceptar un scope libre mediante CAS/readback. Si no existe ningún scope elegible, auditoría no invasiva con todos los ocupantes y gates pendientes; terminar el run sin espera. Nunca crear nuevos scopes para simular productividad ni tocar trabajos activos de Codex u otro agente.

**Cierre estricto:** los required checks no se desactivan ni se usan estados SKIPPED/PENDING como PASS. Al volver verde la CI exact-SHA, un revisor elegible verifica el merge protegido y postmerge aplicable; solo después concilia LISTO institucional. Las revisiones y autorizaciones humanas exigibles siguen intactas. La priorización y la meta no modifican el contrato 15/15/15, ni crean/reanudan las seis Tasks físicas pausadas.

**Implementación comprobable:** `scripts/automation-ci-throughput.mjs --self-test` evalúa decisiones sin escritura y contabiliza los cierres únicos con comprobación completa. Es apoyo de auditoría, nunca sustituye la verificación real de GitHub ni concede leases, aprobaciones o estados.


## Traspasos futuros — idle-first, proxima ejecucion, sin preasignacion (2026-11-05)

El selector de proxima Task libre es consultivo: revisar la capacidad, permisos, estado nativo, lease y RRULE reales de todas las Tasks de la misma cuenta autorizada. Preferir una candidata sin scopes; entre candidatas de igual carga, elegir la ejecucion futura comprobada mas proxima. Nunca asignar ANTES de que la Task elegida se active: solo ella puede aceptar/reclamar mediante version/CAS/readback. El traspaso de una EN_PROGRESO requiere evidencia de abandono, autorizacion, lease anterior liberado, SHA/rama original y ausencia de editor o run activo. No confundir Task libre con scope libre.



## Protocolo vigente de revisión técnica por TASK_ID — orden 2026-11-04

La unidad de autoría funcional de una microtarea comercial es el `TASK_ID` físico autorizado y su evidencia de `PLAN_MAESTRO`/`BITACORA`, no la coincidencia del login GitHub. Una Task habilitada **distinta de la autora** puede revisar técnica y sustantivamente el PR de una Task que use la misma cuenta GitHub, comprobar diff, aislamiento multitenant/RBAC, checks y pruebas exact-SHA, registrar `TECH_REVIEW_PASS` o `REQUEST_CHANGES` con `TASK_ID_AUTOR != TASK_ID_REVISOR`, SHA y readback. Si todo cumple y los permisos y reglas protegidas lo permiten, la Task revisora puede efectuar el merge y el cierre `LISTO` después de CI postmerge exact-SHA y readback de `PLAN_MAESTRO`, `COLA` (fórmulas intactas) y `BITACORA`. **La Task autora no puede emitir aprobación técnica cruzada ni marcar como LISTO su propia microtarea.** Un traspaso de edición requiere lease libre, consentimiento/propiedad válida y ausencia de edición simultánea; revisión solo lectura no otorga permiso de escritura.


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
