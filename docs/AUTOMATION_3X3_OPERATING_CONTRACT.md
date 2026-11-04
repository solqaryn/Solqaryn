# SOLQARYN — Contrato operativo de automatizaciones de microfase 3

VERSION=2.0
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BASE_BRANCH=dev
TIMEZONE=America/Tegucigalpa
TASK_COUNT_LIMIT=PROVIDER_EFFECTIVE_CAPACITY
TASK_ASSIGNMENT_MODEL=OWNER_CONFIGURABLE
CURRENT_REGISTERED_TASKS=6
CURRENT_JAVIER_TASKS=4
CURRENT_ALEX_TASKS=2
MAX_OPEN_WORK_BRANCHES=6
MAX_CONCURRENT_SCOPES=6
MAX_OWNED_SCOPES_PER_TASK=1
MAX_ACTIVE_WRITES_PER_TASK=1
REVIEW_INDEPENDENCE_REQUIRED=TRUE
AUTO_CERTIFICATION_MODE=REQUIRED_SCOPE_LOCK
FORMAL_APPROVAL_ROUTINE_COMMERCIAL_REQUIRED=FALSE
FORMAL_APPROVAL_GOVERNANCE_REQUIRED=TRUE
GITHUB_CHAT_EXECUTOR=solqaryn-platform
GITHUB_CHAT_REVIEWER=JAVIER_GITHUB_LOGIN
GITHUB_CHAT_ROLE_SEPARATION=STRICT
GITHUB_CHAT_ROLE_POLICY=docs/GITHUB_ACCOUNT_ROLES.md
VAEP_DISPATCH_AUTHORIZED=FALSE
JULES_WORKERS_AUTHORIZED=0


## Criterio obligatorio para FUTUROS traspasos — próxima Task libre en ejecutarse (orden 2026-11-04)

Antes de reasignar una microtarea comercial por exceso de propiedad o transferencia autorizada, **NO escoger una Task libre por conveniencia ni por orden de nombre**. Obtener la hora de decisión actual con zona `America/Tegucigalpa` y un censo vivo de las Tasks individuales autorizadas: `AUTHORIZED_COMMERCIAL_TASK_IDS`, `REGISTERED_TASK_IDS`, identidad, cuenta, conexión, permisos efectivos, `automations.peek` (incluido `is_enabled` y ejecución actual), `TAREAS_PROGRAMADAS`, `CONFIG`, y ownership completo `PLAN_MAESTRO`/`COLA`/`BITACORA`. Filtrar candidatas cuyo `TASK_ID` no esté autorizado, habilitado o dentro del mismo pool/cuenta expresamente permitidos; con otra microtarea propia `EN_PROGRESO`/`VALIDANDO`; con edición, lease o ejecución activa; sin permiso/schedule/identity/Scope Lock verificados. **No habilitar Tasks pausadas de Alex ni solicitar su revisión ordinaria**.

Para cada candidata libre, obtener la **próxima fecha-hora futura** de ejecución programada (no comparar solamente `:00/:20/:30/:40`): usar `next_run_time` nativo si está expuesto; cuando el proveedor no lo exponga, calcular la siguiente ocurrencia desde el `RRULE` horaria verificable y la zona horaria, dejando explícito que es predicción de programación, no garantía de ejecución. Ordenar las candidatas por instante futuro creciente; desempatar deterministamente por `TASK_ID`. Seleccionar solo la primera realmente elegible, después de comparar si el traspaso reduce la espera respecto de la continuación legítima del propietario actual. En ausencia de datos fiables, **NO_GO** del traspaso, no suponer disponibilidad. Revalidar disponibilidad inmediatamente antes de registrar el cambio.

La elección de receptora **NO efectúa el traspaso**: seguir exigiendo autorización expresa, release real del lease previo, ausencia de edición concurrente, misma rama/PR/HEAD, permisos reales, readback de GitHub y `PLAN_MAESTRO`/`BITACORA` y preservación de autoría; no autoaprobar, eludir bloqueos del proveedor ni declarar `LISTO` sin CI/merge protegido. Las revisiones técnicas de un `TASK_ID` sobre otro NO constituyen propiedad de una microtarea. `MAX_CONCURRENT_SCOPES=6`, `MAX_OWNED_SCOPES_PER_TASK=1`, `MAX_ACTIVE_WRITES_PER_TASK=1` y `REVIEW_FIRST → RECOVER → CLAIM` permanecen vigentes.

**Instrucción más reciente del propietario, 2026-11-04:** la candidata libre más próxima para `P1-010.A` es Javier `:30`, no Javier `:00` (a las 11:29, ejecución `:30` frente a siguiente `:00` a las 12:00). El nuevo owner **NO queda efectivo hasta liberar la ejecución/lease de :30, registrar y releer `PLAN_MAESTRO!I44` y BITACORA y conservar PR #3842/HEAD/autoría**. Si :30 está en ejecución al intentar el traspaso, NO_GO parcial sin sobrescribir el trabajo y revalidar tras la liberación. No repetir escrituras GitHub previamente rechazadas. Esta actualización prevalece sobre la excepción anterior que conservaba a Javier :00. Las transferencias futuras obedecen el mismo criterio de siguiente horario libre y verificado.

## Orden vigente del propietario — WIP comercial 6/1/1 (2026-11-04)

**Norma actual, con precedencia sobre cualquier descripción histórica 6/2/1:** `MAX_CONCURRENT_SCOPES=6` (máximo global), `MAX_OWNED_SCOPES_PER_TASK=1` (una microtarea propia por TASK_ID físico) y `MAX_ACTIVE_WRITES_PER_TASK=1` (un solo scope en escritura). Un scope sigue siendo **propio** durante `EN_PROGRESO` y **durante todo `VALIDANDO`** aunque espere CI, revisión técnica, un proveedor o la resolución de un conflicto. Solo se libera para reclamar otra microtarea tras `LISTO` confirmado con merge protegido, CI aplicable y readback de `PLAN_MAESTRO`/`BITACORA`/`COLA` derivada; `CANCELADO` o transferencia explícita, autorizada y verificada también exige liberar realmente la propiedad antes de otro CLAIM. No existe excepción de «segunda microtarea en VALIDANDO».

`REVIEW_FIRST → RECOVER → CLAIM` se preserva: una Task puede revisar técnicamente trabajo de un **TASK_ID diferente** sin adquirir propiedad y registrar `TECH_REVIEW_PASS`/`REQUEST_CHANGES` contra el SHA exacto, aun si ambos usan la misma identidad GitHub. Esto **no es** un `APPROVED` formal de otro login. Para reclamar o aceptar por handoff un scope adicional, comprobar antes el inventario completo de tareas propias no cerradas, propietario y TASK_ID, exclusividad, permisos y SHA/PR reales; si el conteo es >=1, **NO_GO** para nuevo CLAIM/traspaso de titularidad. Puede continuar su único scope propio y revisar otros sin hacerse propietario. La Task autora no certifica técnicamente ni marca `LISTO` su propia entrega. Los cambios de gobierno, seguridad y validadores conservan controles independientes de GitHub. Reparto corregido con evidencia `PLAN_MAESTRO!I38/I41/I44` y `BITACORA!546`: Javier :20 posee P1-007.A, Javier :40 posee P1-008.B y Javier :00 posee P1-010.A; Javier :30 no tiene scope propio. Este inventario debe comprobarse de nuevo antes de cualquier acción, no es una asignación nueva de este documento.

## Protocolo vigente de revisión técnica por TASK_ID — orden 2026-11-04

La unidad de autoría funcional de una microtarea comercial es el `TASK_ID` físico autorizado y su evidencia de `PLAN_MAESTRO`/`BITACORA`, no la coincidencia del login GitHub. Una Task habilitada **distinta de la autora** puede revisar técnica y sustantivamente el PR de una Task que use la misma cuenta GitHub, comprobar diff, aislamiento multitenant/RBAC, checks y pruebas exact-SHA, registrar `TECH_REVIEW_PASS` o `REQUEST_CHANGES` con `TASK_ID_AUTOR != TASK_ID_REVISOR`, SHA y readback. Si todo cumple y los permisos y reglas protegidas lo permiten, la Task revisora puede efectuar el merge y el cierre `LISTO` después de CI postmerge exact-SHA y readback de `PLAN_MAESTRO`, `COLA` (fórmulas intactas) y `BITACORA`. **La Task autora no puede emitir aprobación técnica cruzada ni marcar como LISTO su propia microtarea.** Un traspaso de edición requiere lease libre, consentimiento/propiedad válida y ausencia de edición simultánea; revisión solo lectura no otorga permiso de escritura.

La validación cruzada por `TASK_ID` es **técnica**, no un GitHub `APPROVED` formal de otra identidad. GitHub sigue identificando a las cuatro Tasks Javier como `jmejia31` y a las dos Alex como un solo login de su cuenta. No falsificar cuentas, reviews ni firmar aprobaciones propias. PR comerciales ordinarios aprovechan `AUTO_CERT=PASS` confiable y los checks reales sin pedir rutinariamente revisión humana; los PR protegidos de gobierno/validadores o un ruleset que exija aprobación formal conservan sus controles por identidad GitHub realmente independiente. No solicitar de forma predeterminada a `morales35alex` que apruebe trabajo de Javier, ni a `jmejia31` que apruebe trabajo de Alex. La regla es simétrica y no activa ni reprograma Tasks de Alex, que permanecen inactivas hasta nueva orden. Se preservan `PROJECT_SCOPE_LOCK=STRICT`, WIP 6/1/1, rama base `dev`, veto a autoaprobación formal, prohibición de force-push y límites de PROD.

## Censo vigente — Javier :30 (2026-11-04)

Por orden expresa del propietario se creó `SOLQARYN — PRIMARY DEV 04 — JAVIER`, ID `6aeacb7820ac8191b7d768a6f33dfa09`, cada hora al minuto :30 en America/Tegucigalpa. Identidad y horario verificados en ChatGPT; quedó PAUSADA hasta formalizar autorización individual en PR protegido, revisión independiente, CI exact-SHA y Sheets. Después del merge serán seis identidades autorizadas: cuatro Javier (:00/:20/:30/:40) y dos Alex (:10/:50). La antigua Alex :30 permanece excluida, sin reactivación ni herencia de permisos. No se amplían límites de scopes WIP 6/2/1 ni autorizaciones a QA/main/PROD. Permisos individuales y prueba nativa de lectura/escritura siguen sujetos a evidencia, y las pruebas desde un chat manual no sustituyen las de una Task.

## Registro anterior M3.2 (2026-11-03, histórico)

Cinco identidades autorizadas y registradas, no un reparto obligatorio: Javier :00/:20/:40 y Alex :10/:50. El slot antiguo :30 está excluido; su ausencia física no puede certificarse desde la cuenta ajena. Los fragmentos que describen el inventario de seis son históricas y no autorizan altas ni revisiones propias. Las cinco identidades están sujetas a cuentas, ACL y rol GitHub efectivos; ninguna automatización se activa desde la documentación.

## M3.3 — Interpretación vigente y evidencia reutilizable (2026-11-03)

Este control verifica un ciclo comercial real de extremo a extremo, **no** una cantidad de Tasks encendidas. La topología autorizada sigue siendo cinco identidades (Javier :00/:20/:40; Alex :10/:50), WIP comercial 6/2/1 y Scope Lock STRICT. El trabajo nace exclusivamente del Plan Comercial Inicial. No crear Tasks, reactivar automatizaciones, ampliar permisos, modificar COLA ni repetir pruebas diagnósticas consumidas.

**Evidencia funcional real preexistente, no autorización vigente de escritura:**

- P0-006.A: Task Alex :50 (6adcc98a80a48191b71a5156624c21df) tomó el scope y abrió PR [#3751](https://github.com/solqaryn/Solqaryn/pull/3751) en SHA 30274e27f036b3a619b9e1ff543cce202a49bdaf. Javier jmejia31 aprobó revisión #5644765982; tres checks requeridos SUCCESS; merge protegido bafcde53ce2562f70d5b4115184df51e5c48483d. BITACORA!92:93 registra VALIDANDO -> LISTO, PLAN_MAESTRO!H12, COLA!E12 y sucesora COLA!G13=SI.
- P0-007.A: Task Alex :10 (6adbbc9ad9e88191bea8eb69104afb25) creó el PR [#3753](https://github.com/solqaryn/Solqaryn/pull/3753), corrigió el mismo PR y publicó HEAD final 7d8846107d57be815450660daac3d108b69dc54a. Javier :40/:20 documentó revisión cruzada y jmejia31 aprobó formalmente #5645065439 en ese HEAD; tres checks requeridos SUCCESS, prueba dirigida y CI postmerge 5/5 SUCCESS. Merge 72327d132ee69955c5bb13a03423b229a08bd432; BITACORA!96,97,99,103 acredita PLAN_MAESTRO!H14=LISTO, COLA!E14=LISTO y COLA!G15/N15=SI.
- P0-007.B: [PR #3757](https://github.com/solqaryn/Solqaryn/pull/3757) y BITACORA!108,110,113,114 registran recuperación de conflicto, corrección del PR existente sin force-push, CI del SHA actualizado y cierre. Esto demuestra continuidad en ese caso, no que las Tasks hoy estén encendidas.
- Las pruebas #3751, #3753 y #3757 ocurrieron ANTES de la separación institucional de roles del 31-10-2026. Son evidencia verificable de funcionamiento entonces, no autorización actual para que Javier o Alex publiquen nuevos PR desde este asistente.
- El self-test de scripts/validate-dual-task-configuration.mjs incluye casos negativos de colisión de scope, permisos ausentes, rama sin readback, WIP excedido, revisión propia, corrección no revisada, fallo de Sheets y dependencias. Un test de modelo NO sustituye una ejecución física concurrente de dos Tasks.

**Criterio de certificación actualizado:** EVIDENCIA_CICLO_REAL_PREEXISTENTE=PASS. Los negativos se verifican mediante invariantes probados, self-tests fail-closed y evidencias GitHub/Sheets preexistentes; no se afirma que existieron simultaneidad real o cortes físicos si no ocurrieron. El PASS integral exige revisión independiente de cualquier cambio de gobierno, CI, merge protegido y readback. AUTOMATION_RUNTIME=ACTIVE_DEV es autoridad lógica, no habilitación física; las tres Tasks Javier constan pausadas en lectura nativa y las dos Alex figuran pausadas según último readback institucional. La Task Alex :30 sigue excluida.

## Enmienda operativa propuesta — certificación automática CI en DEV (2026-10-30)

**Vigencia condicionada:** esta norma solo sustituye la revisión formal para futuros PR comerciales ordinarios cuando el changeset esté fusionado por PR protegido en `dev`, el job requerido `verify-solqaryn-project-scope` ejecute la certificación contra el SHA exacto y exista readback de CI. Durante la implantación/bootstrap de este PR normativo sigue exigida revisión formal realmente independiente. No se simula un `APPROVED` humano: `AUTO_CERT=PASS` significa exclusivamente certificación técnica de GitHub Actions.

Para PR comerciales de ramas deterministas `work/solqaryn-<microtarea>`, el gate automático comprueba identidad y repositorio originales, alcance/diff, base DEV, SHA exacto, ausencia de secretos añadidos en código productivo, tres checks requeridos (incluyendo el propio Scope Lock), pruebas específicas del scope, resultado de todos los checks aplicables y rechazo fail-closed de fallos, faltas de evidencia o cambios concurrentes de HEAD. El código del verificador se ejecuta desde el commit BASE confiable, no desde el PR que evalúa. El job de Scope Lock ya es obligatorio en el ruleset de dev; no se requieren botones ni aprobación humana para el PR comercial que supera toda la certificación y sea fusionado mediante permisos reales y el flujo protegido. Un PASS de una Task o de Sheets no sustituye este gate.

Los PR que cambian gobierno, Skill, reglas de seguridad, workflows requeridos o el propio verificador **siguen necesitando revisión formal independiente** del SHA final. El bootstrap no puede autocertificarse. Ningún cambio expande el allowlist comercial 5/6 ni habilita Alex :30; no altera QA/main/PROD, credenciales, Aiven, grants ni despliegues. Una Task no puede autoaprobarse ni usar otra identidad. El cierre `LISTO` exige merge protegido, CI postmerge aplicable y readback de `PLAN_MAESTRO`, `COLA` (fórmula intacta) y `BITACORA`. Revertir mediante PR protegido con nuevo CI, sin eliminar evidencia histórica.

## Autoridad y etapa de implantacion

Este contrato v2.0 se aplica al inventario configurable de Tasks solo cuando este archivo y los validadores
esten integrados en el HEAD vigente de `dev`, el Scope Lock sea STRICT y el proveedor demuestre permisos efectivos. No existe techo interno fijo ni distribución obligatoria por cuenta; la capacidad efectiva depende del proveedor y de la autoridad institucional protegida. Los conteos físicos y autorizados se leen en `docs/AUTOMATION_STATUS.md` y Google Sheets. El WIP comercial máximo de seis scopes es independiente del inventario físico de Tasks.

Los registros originales de M3.2 incluyeron seis Tasks, pero el inventario institucional
operativo vigente incluye únicamente cinco identidades autorizadas, conservando los
dos TASK_ID originales. La cuenta de Alex se verifica desde su propietario; no inferir
eliminación física de una Task fuera del censo a partir de una lectura inaccesible.
Las evidencias previas están en BITACORA como auditoría, no como autoridad ejecutable. La instalación no incrementa por sí misma el contador de autorizaciones comerciales.
La autoridad efectiva depende de `SCHEDULED_CHATGPT_TASKS_AUTHORIZED` y de
`AUTHORIZED_COMMERCIAL_TASK_IDS`, enumerados en `docs/AUTOMATION_STATUS.md` y
`docs/AUTOMATION_AUTHORITY.md`: cada ID nuevo requiere evidencia, autorización
individual, PR protegido y readback. La habilitación física no permite escritura
comercial a los IDs que no figuran en la lista, aunque el flag global de Sheets esté activo. Está prohibido usar sesiones o credenciales de otra cuenta o invocar una Task privada ajena. La propietaria puede proponer cambios de cantidad o distribución mediante enmienda protegida. Antes de activar una Task se confirma capacidad del proveedor, se leen de vuelta ID, cuenta, propietario, horario y estado habilitado, y se tramita su autorización individual por separado. Esta regla no ordena altas/bajas ni cambia el inventario o allowlist actuales.

## Programacion nominal, no asignacion

La tabla siguiente enumera las seis Tasks del censo comercial que autoriza esta enmienda después de su merge protegido; no fija una cantidad ni un reparto obligatorio. Cualquier Task externa requiere verificación y autorización propia.

| Slot | Cuenta | Minuto por hora | Implantacion |
| --- | --- | --- | --- |
| PRIMARY_DEV_01 | JAVIER_CHATGPT | :00 | ID original conservado; instalada |
| SECONDARY_REVIEW_01 | ALEX_CHATGPT | :10 | ID original conservado; instalada |
| PRIMARY_DEV_02 | JAVIER_CHATGPT | :20 | Instalada desde Javier |
| PRIMARY_DEV_04 | JAVIER_CHATGPT | :30 | Nueva; ID 6aeacb7820ac8191b7d768a6f33dfa09; contrato 2.0, autorización protegida |
| PRIMARY_DEV_03 | JAVIER_CHATGPT | :40 | Instalada desde Javier |
| SECONDARY_REVIEW_03 | ALEX_CHATGPT | :50 | Instalada desde Alex |

Los identificadores originales `PRIMARY_01` (Javier) y `SECONDARY_02`
(Alex) se conservan como aliases operativos aun despues de la migracion M3.2 de las
etiquetas logicas `PRIMARY_DEV_01` y `SECONDARY_REVIEW_01`.
Los **TASK_ID físicos originales no cambian**. La numeración en pantalla
no transfiere permisos ni duplica una Task; `TAREAS_PROGRAMADAS` debe
confirmar unicidad de ID/propietario/horario.

## 5. Responsables y permisos — seis Tasks autorizadas (actualizado 2026-11-04)

**Igualdad de protocolo no es igualdad automática de credenciales ni activación.** El censo actual contiene seis `TASK_ID` autorizados: Javier `:00/:20/:30/:40` y Alex `:10/:50`; la antigua Alex `:30` queda excluida. Las etiquetas visibles `PRIMARY_DEV` y `SECONDARY_REVIEW` son aliases históricos, no categorías de privilegio ni autorizaciones adicionales. El censo no impone un máximo inmutable ni una distribución 3+3. Cada operación se decide por `TASK_ID` vigente, cuenta autenticada, permiso efectivo, estado `is_enabled`, lease/owner del scope, revisión realmente independiente y Scope Lock `STRICT`.

| Operación | Actor admisible según la autoridad **actual** | Control indispensable |
| --- | --- | --- |
| Buscar y seleccionar trabajo | Cualquiera de los seis IDs **si está habilitado** y posee permisos reales; también puede examinar candidatos en solo lectura | `REVIEW_FIRST` primero, `RECOVER` después y `CLAIM` al final; no apropiarse de scopes ajenos por aparente inactividad |
| Tomar `PENDIENTE` | Task individualmente autorizada y habilitada que pueda efectuar un claim exclusivo | Dependencias todas `LISTO`, rama determinista create-if-absent, readback, WIP global 6, máximo 2 scopes por Task y 1 escritura activa |
| Recuperar `EN_PROGRESO` | Task propietaria con recuperación válida o Task distinta con traspaso expresamente autorizado | Lease liberado, ausencia de editor, rama y último commit reales, permisos y cambio de owner verificados; conservar PR existente |
| Desarrollar, corregir código, publicar commit o PR | **Cualquiera de los seis TASK_ID comerciales** si está habilitado y tiene identidad GitHub propia, `contents:write` y permisos efectivos; **solo para intervenciones manuales de este asistente** aplica la autoría institucional `solqaryn-platform` | No intercambiar sesiones ni cuentas; una Task que contribuyó al PR no puede emitir revisión independiente del mismo PR |
| Revisar `VALIDANDO`, diagnosticar y corregir | Cualquiera de los seis TASK_ID con permisos efectivos; revisión **independiente formal** solo por login GitHub ajeno a todos los contribuyentes y con derecho real de revisión; `jmejia31` es revisor predeterminado **solo para PR manuales institucionales** | Se permite autoevaluación técnica, comentarios y corrección del defecto en la rama original; al contribuir, ese actor pierde independencia formal en ese PR, que requerirá otro revisor independiente cuando sea exigible |
| Corregir el defecto detectado | Autora inicial o Task revisora con permisos autenticados y traspaso/exclusividad de escritura verificados; en intervenciones manuales de este asistente corrige `solqaryn-platform` | Reutilizar el mismo PR/rama y publicar SHA nuevo; si corrigió el revisor, dejar de considerarlo independiente y tramitar revisión formal de otro login solo cuando resulte obligatoria |
| Registrar `EN_PROGRESO` y `VALIDANDO` | Actor autorizado que acredita claim y entrega auténticos | Registro auditable y PR/diff/pruebas reales; no adelantar estados por calendario |
| Emitir aceptación técnica | CI/Scope Lock confiable del SHA exacto para PR comerciales ordinarios; cambios de gobierno/seguridad/validadores requieren además `APPROVED` de revisor formalmente independiente | Autoevaluación técnica propia permitida, autocertificación independiente prohibida; CI no simula un `APPROVED` humano ni autoriza bypass |
| Merge a `dev` | Identidad GitHub que realmente tenga permiso de merge en rama protegida | Scope Lock, checks y aprobaciones exigidas, estado exacto y ausencia de conflicto |
| Registrar `LISTO` | Actor autorizado que comprueba el cierre efectivo | Merge SHA y HEAD de `dev`, `PLAN_MAESTRO` y `BITACORA` con readback; `COLA` recalculada por fórmulas intactas y sucesoras solo con todas las dependencias `LISTO` |

**Jerarquía de decisión (prioridad absoluta de cierre):** inspeccionar TODOS los `VALIDANDO` (ajenos con independencia real y propios con continuidad técnica/cierre comprobable) → `EN_PROGRESO` propio recuperable → `EN_PROGRESO` ajeno con handoff autenticado y seguro → `PENDIENTE` elegible → evento `SIN_TRABAJO_ELEGIBLE` deduplicado. Ningún `VALIDANDO` ajeno del mismo login confiere independencia; el `VALIDANDO` propio nunca autoriza autoaprobarse. Cada Task debe agotar acciones de revisión, corrección, sincronización de base, CI exact-SHA, merge permitido y cierre postmerge de sus `VALIDANDO` antes de reclamar una nueva microtarea; si un `VALIDANDO` depende de un tercero o proveedor y no tiene intervención técnica autorizada disponible, registrar el bloqueo causal deduplicado y continuar con el siguiente `VALIDANDO` o el siguiente nivel de la jerarquía. Los horarios separados por 10 minutos no sustituyen el control de concurrencia. No crear dos ramas/editores para el mismo scope. Sin transferencia de sesiones ni autoaprobaciones.

**Estado real, sin inferencias:** `AUTOMATION_RUNTIME=ACTIVE_DEV` expresa capacidad contractual, **no** que las Tasks estén encendidas: último readback nativo Javier `:00/:20/:40 = PAUSADA`; último readback institucional Alex `:10/:50 = PAUSADA`. La eliminación física de Alex `:30` no se acredita aquí; únicamente consta excluida del censo autorizado. La certificación integral M3.3 `PASS` (PR #3833/#3834; `BITACORA!468`) acredita pruebas previas y validadores, no actividad presente.

**Autoridades:** `docs/GITHUB_ACCOUNT_ROLES.md` rige nuevas operaciones de este asistente; `docs/AUTOMATION_STATUS.md` y `docs/AUTOMATION_AUTHORITY.md` determinan IDs y permisos individuales; `scripts/validate-dual-task-configuration.mjs` valida el selector. Esta matriz interpreta reglas existentes; por sí sola no concede permisos, crea/activa Tasks, cambia grants o autoriza QA/main/PROD.


**REFERENCIA HISTÓRICA — NO EJECUTABLE:** durante el piloto inicial, los nombres Primary/Secondary no asignaban por sí mismos desarrollo o revisión. Esa simetría dejó de definir las conexiones GitHub utilizadas por este asistente. Para cambios nuevos rige docs/GITHUB_ACCOUNT_ROLES.md: solqaryn-platform es la autora institucional de ramas, commits y PR; Javier (jmejia31) es revisor independiente cuando corresponda. Nadie puede escribir código desde la identidad revisora para aprobar sus propios commits, intercambiar cuentas o heredar permisos de la etiqueta visible. Se conserva la evidencia de trabajo anterior sin reinstaurar sus permisos.


### Reconciliación preventiva del backlog 2026-11-04

Se detectó que el selector daba prioridad solo a `VALIDANDO` **ajeno** y, por ello, cuatro Tasks de Javier que comparten el login GitHub `jmejia31` no seleccionaban sus propios PR para continuidad técnica/postmerge antes de iniciar nuevos CLAIM. La regla corregida mantiene la revisión verdaderamente independiente ajena primero y añade la recuperación de `VALIDANDO` **del mismo TASK_ID físico**, sin confundir autocorrección con aprobación formal. Ejemplos del diagnóstico: P1-009.A/PR #3841 fusionado pero pendiente de checks postmerge y readback; P1-010.A/PR #3842 con CI del SHA aprobado pero base `dev` más reciente y conflicto; P1-007.A/PR #3839 con checks verdes anteriores, conflicto y revisión de `morales35alex` pendiente. No declarar LISTO ni alterar PR ajenos sin evidencia viva. El WIP 6/2/1, Scope Lock STRICT y separación de cuentas no cambian.

## Seleccion y continuidad

Orden de selección **vigente** para las seis Tasks autorizadas (ningún nombre de Task transfiere credenciales ni sustituye `docs/GITHUB_ACCOUNT_ROLES.md`):
1. **REVIEW_FIRST, obligatoria ANTES de RECOVER/CLAIM:** inventariar todas las filas `VALIDANDO` del Plan y contrastarlas con los PR y el HEAD GitHub vivos, sin filtrar solo por autor ajeno. Priorizar revisión ajena con login GitHub realmente independiente, PR verificable y lease libre; después recuperar los `VALIDANDO` asignados al TASK_ID propio para autocorrección técnica, actualización no destructiva de rama/base, espera causal de CI, merge protegido autorizado o cierre postmerge en Sheets. Si el PR ya fue fusionado, verificar su merge SHA, CI postmerge aplicable terminal y readback antes de `LISTO`. Para PR ajeno del mismo login GitHub, solo diagnóstico técnico sin falsa revisión independiente ni edición sin handoff explícito y lease libre. Un PR de gobierno/seguridad/validador requiere `APPROVED` auténtico independiente cuando corresponda, sin bypass. Si toda acción sobre un `VALIDANDO` está realmente bloqueada por terceros o proveedor, registrar fingerprint/NO_GO deduplicado, no repetir diagnósticos vacíos y seguir al siguiente `VALIDANDO` o a RECOVER, sin dejar de revalidar su estado en la próxima ejecución. No crear un nuevo CLAIM mientras haya un `VALIDANDO` propio realmente cerrable o corregible.
2. **RECOVER:** después, reanudar `EN_PROGRESO` propio con rama real, lectura posterior y sin otro editor. Un `EN_PROGRESO` de otra Task solo admite traspaso expresamente autorizado, lease anterior liberado, último commit/branch verificados, permiso y cesión de propietario comprobados, sin editor activo. Nunca asumir libertad solo por inactividad aparente.
3. **CLAIM:** después, tomar `PENDIENTE` elegible con todas las dependencias `LISTO`, rama atómica exclusiva y permisos propios; WIP máximo 6/2/1, segundo scope solo si el anterior está `VALIDANDO` sin edición y REVIEW_FIRST agotado. Prioridad P0 antes que P1 y luego orden del Plan Maestro.
4. Objetivo de medición: 6 microtareas comerciales realmente LISTO por hora entre Tasks autorizadas, nunca garantía de resultado. Tras cada LISTO comprobado, recargar PLAN_MAESTRO/COLA y continuar si hay espacio. Un impedimento externo (p. ej. Vercel CANCELED en P0-001.B) detiene solo ese scope; no inmoviliza candidatos independientes. No contar diagnóstico, PR documental NO_GO, auditoría ni ejecución horaria como LISTO.
5. Si no hay elegibles, dejar como máximo un evento deduplicado
   `SIN_TRABAJO_ELEGIBLE` y finalizar esa ejecución.
6. Tras un cierre `LISTO`, recalcular dependencias y tomar otra elegible dentro
   de la capacidad efectiva restante. No limitar artificialmente a un cierre por run.

`VALIDANDO` no libera dependencias. Una sucesora puede prepararse en solo lectura
sin iniciar código hasta que todas sus dependencias estén `LISTO`.
`BLOQUEADO` y `CANCELADO` no satisfacen dependencias.
No se fabrican candidatos para mantener ocupadas las Tasks.

## Máquina de estados y responsabilidades

- `PENDIENTE -> EN_PROGRESO`: una Task con toma exclusiva efectiva y avance real.
- `EN_PROGRESO -> VALIDANDO`: quien implementa presenta PR + diff + pruebas reales.
- `VALIDANDO -> VALIDANDO` (corrección vigente): cualquiera de las seis Tasks puede autoevaluar su entrega o analizar y corregir defectos de una entrega ajena si posee `contents:write`, claim exclusivo y permiso efectivo; conserva el MISMO PR, publica SHA nuevo y deja evidencia. Si el revisor aportó commits ya no podrá certificar independientemente ese PR. Para intervenciones manuales de este asistente, `solqaryn-platform` corrige sus PR y `jmejia31` revisa el SHA final cuando proceda.
- Si una identidad revisora aportó commits reales al PR, no puede emitir **certificación formal independiente** de esa misma entrega. Para PR comerciales ordinarios puede operar el gate automático confiable Scope Lock/CI exact-SHA si la política lo permite; para gobierno, seguridad o controles críticos se exige revisión por un tercero independiente de TODOS los contribuyentes. Nunca simular aprobación ni cambiar cuentas para fingir independencia.
- `VALIDANDO -> LISTO`: revisión independiente requerida completa, CI exact-HEAD
  completo, merge autorizado y **confirmado en dev**, SHA de merge verificable,
  `PLAN_MAESTRO` actualizado, `COLA` recalculada por formula y evento en
  `BITACORA`. GitHub no escribe Sheets automaticamente. Una falla de Sheet se
  reconcilia con evidencia; no afirmar LISTO hasta readback.
- `BLOQUEADO`: solo impedimento causal, con recuperación al estado real.
- `CANCELADO`: solo autorización explícita, sin liberar dependientes.

### Aceptación excepcional y limitada — P0-001.B / PR #3778 (2026-10-28)

El propietario aceptó expresamente, para resolver únicamente el incumplimiento
de secuencia de `P0-001.B`, la revisión independiente que Alex
(`morales35alex`) registró después del merge del PR #3778. GitHub registra el
merge `fed99ac8f2faf146df9c1bdbd6866f420d784cf8` a las `2026-10-28T15:25:26Z`
y la revisión `APPROVED` #5685527254, anclada al HEAD del PR
`57ab5885674e04ae24f1ea339e713ca037140600`, a las `2026-10-28T15:47:37Z`.
La revisión fue posterior al merge; **esta aceptación no la convierte en una
aprobación previa ni declara que se cumplió retroactivamente la secuencia**.

La aceptación se limita a la omisión de revisión previa de ese PR y de esa
microtarea. No modifica `REVIEW_INDEPENDENCE_REQUIRED=TRUE`, no establece un
precedente ni dispensa revisiones previas para otros PR. La aprobación
independiente posterior de Alex y la decisión expresa del propietario quedan
como controles compensatorios documentados para este único caso. La transición
de `P0-001.B` a `LISTO` todavía requiere incorporar esta decisión mediante PR
protegido, CI requerido exact-SHA, conservar evidencia de Render/Vercel para el
SHA de código certificado y realizar readback de `PLAN_MAESTRO`, `COLA` y
`BITACORA`. Esta sección por sí sola no cambia el estado de la hoja.

### Aceptación excepcional y limitada — P0-001.B / PR #3797 (2026-10-30)

El propietario autorizó expresamente una excepción adicional, única y limitada
a la secuencia de revisión del PR #3797 de `P0-001.B`. El PR fue creado por
`jmejia31` con HEAD `cd8c7dfd24b6a3ac30d46995b78ae9b451082b6c` y se fusionó en
`dev` el `2026-10-30T18:12:35Z`, con merge SHA
`6282d59319fded41a4b65641720890853c29b65a`. No se registró una revisión formal
antes del merge. SOLQARYN Platform registró después la revisión independiente
`APPROVED` #5717654037, anclada al HEAD exacto del PR, a las
`2026-10-30T19:26:25Z`. Esa revisión fue posterior al merge y **no se declara
como aprobación previa ni como cumplimiento retroactivo de la secuencia**.

La excepción compensa exclusivamente la falta de revisión previa de
P0-001.B/#3797. No amplía ni reinterpreta la excepción P0-001.B/#3778, no
modifica `REVIEW_INDEPENDENCE_REQUIRED=TRUE`, ni establece precedente para
otros PR o scopes. Permanecen obligatorios CI exact-SHA, merge protegido y
readback institucional. Para #3797 se verificaron CI requerido 3/3 y los
deployments Render LIVE y Vercel READY sobre el merge SHA
`6282d59319fded41a4b65641720890853c29b65a`; la excepción documental y los
readbacks de `PLAN_MAESTRO`, `COLA` y `BITACORA` deben integrarse y verificarse
antes de cambiar el estado de la tarea. Esta sección por sí sola no cambia el
estado institucional.

### Aceptación excepcional y limitada — P1-001.A / PR #3793 (2026-10-30)

El propietario aceptó expresamente una excepción única para la falta de revisión
independiente previa al merge del PR #3793, correspondiente exclusivamente a
P1-001.A. El PR fue creado por `jmejia31` con el commit
`e5032751b22cb1eab56762f5c79638c777a9ed27` y se integró en `dev` el
`2026-10-30T16:12:24Z` como merge SHA
`064dfd4979811164ab34c1e0319b3b7b609db44e`, por `solqaryn-platform`. GitHub
Al momento del merge, GitHub registraba `reviews=[]`: no hubo aprobación formal
previa al merge.
La descripción del PR indicaba que se esperaba revisión previa; el resultado
real no cumplió esa secuencia. Esta aceptación no la presenta como cumplida
retroactivamente.

Como control compensatorio obligatorio, un revisor independiente debe examinar
el diff exacto de #3793 y la evidencia de su ejecución real posterior al merge
antes de cerrar P1-001.A. Son elegibles Alex (`morales35alex`) o SOLQARYN
Platform mediante un representante humano identificable, siempre que el login
de revisión sea distinto del autor y de los contribuyentes revisados. La
revisión y esta aceptación se registrarán en un PR protegido separado con CI
exact-SHA; ese PR debe integrarse en `dev` antes de cualquier cierre
institucional. El flujo real #41087748364 terminó `SUCCESS` sobre el merge SHA:
MySQL se consultó solo con SELECT; `/roles` aceptó 403 RBAC o el 503 exacto
`TENANT_DATA_ISOLATION_REQUIRED`; los controles A/B/C de identidad, contexto
propio y aislamiento cruzado pasaron. La evidencia no sustituye la revisión.

La excepción se limita a P1-001.A/#3793, no modifica
`REVIEW_INDEPENDENCE_REQUIRED=TRUE` ni establece precedente. P1-001.A solo
podrá pasar a `LISTO` después de la revisión independiente compensatoria, CI
requerido exact-SHA del PR que documenta la excepción, merge protegido y
readback de `PLAN_MAESTRO`, `COLA` y `BITACORA`. Esta sección no cambia por sí
sola el estado institucional.

#### Seguimiento del control compensatorio (2026-10-30)

El PR documental #3794, commit `8f6778c012d8d7ed6cf90133aa99b45d09b92f26`,
se fusionó por `solqaryn-platform` como
`804d9c636540e1aaa4a92e0bfecdd6f2260d65e7` sin una revisión formal (`reviews=[]`).
Por tanto, #3794 integra la decisión del propietario y el registro factual,
pero **no satisface** el control compensatorio independiente descrito arriba.
Después, `solqaryn-platform` registró la review `APPROVED` #5715412300 en
#3793, anclada al commit exacto `e5032751b22cb1eab56762f5c79638c777a9ed27`,
el `2026-10-30T16:31:36Z`. En el recheck, Alex (`morales35alex`) también
registró `APPROVED` #5715473373 en #3793 a las `2026-10-30T16:36:31Z`,
anclada al mismo commit; su login es distinto del autor `jmejia31`. Esta
aprobación humana es posterior al merge, no se presenta como previa y satisface
la revisión compensatoria autorizada por el propietario.

El flujo real #41087748364 terminó `SUCCESS` sobre el merge SHA: MySQL se
consultó solo con `SELECT`; la API solo recibió `GET`; las pruebas A/B/C de
identidad, contexto propio y aislamiento cruzado pasaron. El PR documental #3795
debe actualizar este registro con la review de Alex y el resultado real. Sigue
`OPEN/DRAFT` y sin reviews; el control institucional permanecerá
`EN_PROGRESO` hasta que #3795 pase CI requerido exact-SHA, reciba revisión
independiente y se integre mediante merge protegido. No habilitar el merge de
#3795 antes de esos controles.

Se mantiene `VALIDANDO` durante reparaciones del revisor. No regresar
artificialmente a `EN_PROGRESO` por un defecto corregible.
Si una ejecución termina por limite del proveedor, conservar estado veridico,
commits, PR y responsable: nunca simular continuidad infinita o cierre.

## Ramas, revision e idempotencia

Ramas permanentes: `dev`, `qa` y `main`. Para cada microtarea elegible
crear/reutilizar **una** rama determinista `work/solqaryn-<id-normalizado>`
y PR exclusivo hacia `dev`. No utilizar una rama almacén compartida.
Máximo **seis scopes comerciales distintos activos** globales (MAX_CONCURRENT_SCOPES=6 y MAX_OPEN_WORK_BRANCHES=6); **una sola microtarea propia por Task** (MAX_OWNED_SCOPES_PER_TASK=1), tanto EN_PROGRESO como VALIDANDO, incluyendo espera de CI o revisión. Prohibido reclamar una segunda, aunque la primera tenga PR real o esté sin editor. REVIEW_FIRST sobre trabajos ajenos no añade propiedad; RECOVER del único scope propio sí se permite con evidencias; handoff solo tras propietario cedente y lease liberados y si el TASK_ID receptor tiene cero scopes propios no cerrados. Rama determinista y PR único por microtarea, exclusión nativa real y readback: no usar Sheets como mutex; no borrar ramas o PR para inventar cupo. Las evidencias previas con dos scopes fueron corregidas institucionalmente en PLAN_MAESTRO!I38/I41/I44 y BITACORA!546; verificar de nuevo antes de operar. El sexto cupo global no habilita una segunda asignación para una Task ni Alex :30.

La toma de una microtarea requiere una primitiva de exclusividad **real** del
proveedor: crear una referencia GitHub con nombre determinista solo si no existe
y comprobar después el SHA y su dueño. Lectura de Sheets seguida de escritura
NO es exclusiva; no es válido el supuesto `read-before-write` como candado.
Para revisar/editar la rama ya existente, un único editor activo a la vez; realizar
traspaso explícito y readback. Prohibido force-push y recrear PR duplicados.
Si no se puede acreditar que el editor anterior terminó, no hacer takeover
a ciegas. Las ramas se eliminan mediante `delete_branch_on_merge` tras merge;
no eliminar ramas con PR abierto o trabajo sin revisar.

Respetar el ruleset de `dev`, los tres checks obligatorios
(`verify-solqaryn-project-scope`, `SOLQARYN - Seguridad multiempresa`,
`SOLQARYN - Build y pruebas backend`), la revisión independiente del
proyecto y la política de aprobación del proveedor. No autoaprobar,
no hacer merge sin autoridad, no modificar `qa`, `main`, PROD ni secretos.
No revivir VAEP, Vibe, Jules, leases, epochs o un scheduler residente.

## Registro y observabilidad

Fuente del trabajo: Google Doc oficial
`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`.
Seguimiento único: Google Sheet
`1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.

`PLAN_MAESTRO!H:M`: estado, responsable, observación, siguiente acción,
evidencia y fecha. `COLA`: **solo formulas**, no escribirla directamente.
`BITACORA!B`: fecha y hora `America/Tegucigalpa` (-06:00).
`BITACORA!Z`: fuente UTC ISO exacta del mismo evento. `TAREAS_PROGRAMADAS`
contiene IDs y última actividad observada; `last_run_time` no prueba entrega.
`RUN_ID` nativo no disponible NO bloquea trabajo DEV: evidencia suficiente
para entrega es rama/PR, SHA, checks, autor, revisión y readback auténticos.
No crear IDs nativos ficticios.

## Pruebas de aceptación M3.3

1. Doble toma del mismo scope: una sola rama/owner efectivo.
2. Microtarea ocupada: segundo actor no sobrescribe ni interrumpe.
3. Dependencia `VALIDANDO`: sucesora no elegible.
4. Dependencia `LISTO`: sucesora elegible si todas satisfechas.
5. Revisión y corrección: cualquier Task autorizada puede diagnosticar y corregir defectos ajenos en el mismo PR bajo exclusividad y permisos propios; quien contribuye pierde condición de revisor formal independiente para ese PR. CI exact-SHA certifica cambios comerciales ordinarios cuando corresponda; gobierno y seguridad requieren revisor independiente real.
6. Si existiesen commits de quien revisa, un tercero independiente de todos los contribuyentes comprueba los cambios; nunca se simulan aprobaciones ni identidades.
7. CI exact-HEAD; merge protegido real; `VALIDANDO -> LISTO` con readback.
8. Sin trabajo: evento deduplicado, sin nuevas ramas ni microtareas ficticias.
9. Interrupcion: reanudar desde commit/PR sin perder ni duplicar.
10. Perdida de permisos o de Google Sheets: bloquear solo operación afectada;
    reconciliar desde pruebas reales.

La certificacion requiere artefactos efectivos, no porcentajes o campos de RUN_ID imposibles. El contrato no impone una cantidad ni reparto fijo; la existencia de una Task no la autoriza comercialmente.

**Acreditación negativa no destructiva:** un caso de colisión o pérdida de permisos de Sheets se evalúa mediante regresiones deterministas fail-closed del validador y los readbacks nativos ya existentes de claims, commits, PR, revisión, CI y Sheets. No es necesario provocar físicamente dos Tasks en ejecución ni cortar acceso a Sheets para comprobar una protección, pero los tests no se presentarán como incidentes nativos. Un indicador `PASS` integral requiere revisión protegida de esta norma, CI exact-SHA de selector/negativas y conciliación institucional. Los diez minutos entre arranques no garantizan ausencia de solapamiento.

**Regla consolidada:** REVIEW_FIRST -> RECOVER propio -> RECOVER ajeno solo con traspaso autorizado y lease liberado -> CLAIM elegible. Cada Task **puede autoevaluar técnicamente** sus cambios, ejecutar pruebas y corregir errores; ninguna Task puede **autocertificarse como revisora independiente** de un PR que contribuyó. Dos Task IDs del mismo login tampoco equivalen a independencia formal. CI/Scope Lock confiable del SHA exacto certifica PR comerciales ordinarios sin aprobación humana rutinaria; gobierno y seguridad conservan revisión ajena obligatoria.
