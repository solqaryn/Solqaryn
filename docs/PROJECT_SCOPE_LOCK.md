## PRECEDENCIA NO_IDLE_PENDING_RELEASE_V9 — 2026-11-06 — Plan Comercial Inicial, dev

Correccion expresa del propietario sobre V8.2: VALIDANDO tiene prioridad SOLO cuando exista una accion de integracion o cierre realmente ejecutable. No hay que vaciar VALIDANDO antes de desarrollar PENDIENTE.

1. En cada run nativo comprobar PR VALIDANDO, created_at, revision independiente exact HEAD, dependencias COLA!G=SI, mergeability CLEAN y required checks SUCCESS exact SHA. Si existe uno fusionable AHORA, fusionar el mas antiguo entre los aptos, protegido y de uno en uno; revalidar dev y repetir mientras haya otro fusionable.
2. Si NO hay otro PR apto para MERGE/CERTIFICACION AHORA (CI pendiente, conflictos sin traspaso, revision pendiente o dependencia G=NO), comprobar de inmediato PENDIENTE con COLA!O=SI y sin propietario/editor ni rama/PR duplicados; seleccionar cualquiera disponible del PLAN COMERCIAL VIGENTE, con preferencia developerFor no exclusiva. No exigir que VALIDANDO quede vacio ni un cierre LISTO previo para comenzar desarrollo. PENDIENTE no tiene FIFO estricto; no frenar trabajo nuevo por PR bloqueados.
3. COLA!O DESARROLLO_HABILITADO se calcula para IDs del plan comercial vigente P[0-9]+, I8 y G0 (nunca planes ajenos): PENDIENTE, responsable vacio y todas las dependencias en VALIDANDO o LISTO (maximo dos). Es gate de DESARROLLO, no gate de MERGE. COLA!G DEPENDENCIAS_OK sigue exigiendo LISTO para fusion y cierre y conserva sus formulas intactas. La formula O y el selector deben coincidir; no basta con instruir prompts si la hoja devuelve NO a todo.
4. Al tomar PENDIENTE desarrollar una sola microtarea libre en una rama y PR originales, con prueba causal y entregar VALIDANDO sin esperar CI opcional. Si la ejecucion alcanza un cierre LISTO y aun puede continuar, puede desarrollar otra PENDIENTE, respetando WIP 15/15/15 como techos, no cuotas. Si realmente no existe trabajo ejecutable, reportar causa concreta y no inventar actividad.
5. Mantener PR review independiente por TASK_ID ajeno, seguridad, Scope Lock, tres checks required PREMERGE exact-HEAD, integracion protegida, rollback, bitacora y readback. CI opcional no bloquea; no eliminar REQUIRED, no CAS/leases, no cambiar Tasks/horarios/cuentas ni QA/main/PROD. No ejecutar esta regla fuera de la autoridad del Plan Comercial vigente.

## PRECEDENCIA VALIDANDO_FIRST_FIFO_READY_V8_2 — 2026-11-06, Plan Comercial Inicial, dev

Objetivo: 6 microtareas LISTO por hora cuando existan PR aptos. Indicador de producción, nunca autorización para falsear pruebas ni eludir GitHub. Esta regla tiene prioridad sobre V8, V7 y ordenamientos por CI para selección, integración y cierre. No crear Tasks, CAS, reservas ni cambios de horarios.

1. REVIEW_FIRST estricto. En cada ejecución nativa: conciliar VALIDANDO ya fusionados, revisar VALIDANDO de autor distinto con evidencia y permisos reales, y después fusionar los comercialmente aptos. Revisión independiente una vez por diff/SHA, seguridad y pruebas causales; renovar si cambia el código. reviewerFor es preferencia y no reserva exclusiva cuando el HEAD lo habilite. No autoaprobar. Solo tomar PENDIENTE tras cerrar un LISTO o si no hay VALIDANDO ejecutable; como máximo una nueva PENDIENTE por LISTO de ese run.
2. FIFO por PR created_at, nunca por finalización de CI, letras A/B/C/D ni selector de Task. Entre PR elegibles fusionar primero el más antiguo que tenga COLA!G=SI, diff revisado por TASK_ID ajeno en HEAD actual, checks REQUIRED exact-head SUCCESS, mergeable=true y ningún editor activo. Respetar dependencias topológicas. Si el antiguo está pendiente de CI, en conflicto o con G=NO, registrar bloqueo una vez, revisar siguiente ejecutable y poder fusionar otro realmente elegible; no inmovilizar toda la cola. Solo un merge protegido a la vez, refrescar dev HEAD, PR, CI y COLA antes de cada merge; GitHub rechaza carreras.
3. Conflictos: autor/editor propio incorpora dev en misma rama/PR, sin force-push, conservando idempotentemente todas las entradas de CHANGELOG_AI.md y tests/funcionalidad concurrentes; nueva revisión independiente si cambia SHA. No intervenir código de autor ajeno sin traspaso. Reducir colisiones no garantiza literalmente cero conflictos.
4. Entrada y merge: COLA!O=SI permite empezar desarrollo con predecesoras VALIDANDO/LISTO; COLA!G=SI exige predecesoras LISTO para fusionar. Mantener Scope Lock, Seguridad multiempresa, Build backend y pruebas dirigidas aplicables como ordene branch protection sobre PR HEAD exacto. No esperar suites opcionales por mera existencia. FAILED/SKIPPED/CANCELLED/PENDING de REQUIRED no son SUCCESS.
5. Cierre rápido proporcional: para microtareas P1 que solo cambian tests/docs, sin código productivo, migraciones, permisos, secretos, workflows o contratos críticos, una vez comprobados merge protegido a dev, revisión independiente, COLA!G=SI y todos los required PREMERGE SUCCESS exact-HEAD, registrar LISTO y readback en PLAN_MAESTRO, COLA y BITACORA EN ESE RUN, con MERGE_SHA y etiqueta POSTMERGE_CI_ASYNC. Esto no declara CI posmerge SUCCESS: vigilarla después; un fallo requerido posmerge exige incidente único, impedir cierre de sucesoras dependientes y reabrir el scope a VALIDANDO hasta reparar/revertir por cauce protegido. Para cambios de runtime, seguridad, datos, migraciones o gobierno conservar CI requerida postmerge exact-SHA antes de LISTO. HASTA que este contrato se fusione en dev, exigir el cierre postmerge anterior y sin espera pasiva.
6. Si falta CI requerida, registrar CI_PENDING_ASYNC una vez, trabajar otro VALIDANDO sin sleep/polling. Sin CI fingida ni merges prematuros. Límite 15/15/15 es techo, no cuota. Sin QA/main/PROD ni alteraciones de protecciones.

## PRECEDENCIA OPERATIVA FAST_CLOSE_V8 — revisión libre y cierre ágil (2026-11-05)
Orden expresa del propietario: el cuello de botella es VALIDANDO -> LISTO, no la mera admisión a desarrollo. Para microtareas P1 de dev, esta regla sustituye cualquier interpretación anterior que reserve exclusivamente el review a reviewerFor o que espere todos los jobs opcionales antes del merge/cierre. No elimina seguridad, revisión independiente, CI de branch protection, dependencias LISTO para merge ni auditoría institucional.

1. Primero conciliar VALIDANDO cuyo PR ya esté MERGED, con TECH_REVIEW_PASS de TASK_ID diferente del autor sobre el diff aprobado y checks postmerge REQUERIDOS exact-SHA SUCCESS; cualquier Task comercial autorizada puede hacer esa conciliación administrativa, incluso la autora, sin reauditar ni reaprobar. Marcar LISTO y readback PLAN_MAESTRO/COLA/BITACORA en el mismo run.
2. Luego revisar cualquier VALIDANDO de otra Task sin conflicto positivo de editor o revisor, con native run/permisos/autoría vigentes; reviewerFor es una PREFERENCIA de reparto, nunca veto ni reserva temporal. La verificación técnica sustantiva se hace una vez: diff, pruebas dirigidas, seguridad multitenant/RBAC/JWT, SHA y TECH_REVIEW_PASS/REQUEST_CHANGES auténtico. No exigir aprobación formal humana en PR comercial ordinario con required_reviews=0; gobernanza y reglas especiales conservan revisión formal independiente.
3. Para fusionar: PR sin conflictos, COLA!G=SI, TECH_REVIEW_PASS independiente y ÚNICAMENTE los checks que exija el ruleset actual de dev en el HEAD exacto con resultado SUCCESS (al aprobar esta enmienda: verify-solqaryn-project-scope, SOLQARYN - Seguridad multiempresa, SOLQARYN - Build y pruebas backend). Merge protegido en el MISMO run. Los workflows no requeridos, por ejemplo las integraciones MySQL de 20+ minutos, no son gates por mera existencia, aunque sus fallos reales de seguridad deben atenderse proporcionalmente.
4. Después del merge, revisar únicamente los checks POSTMERGE requeridos que correspondan al merge SHA; cuando estén verdes, conciliar LISTO sin esperar CI opcional ni nueva aprobación. Si required sigue pendiente, registrar una sola vez CI_PENDING_ASYNC sin sleep/polling y tomar otro scope; cualquier siguiente Task puede conciliar cuando se complete. SKIPPED/CANCELLED/FAILED no equivalen a PASS requerido.
5. Si PR está dirty, no fusionar: el autor/editor autorizado debe actualizar ESA rama preservando commits y tests, sin force-push ni segundo PR, y renovar verificaciones del nuevo SHA. Ninguna revisora que cambie código puede avalar su propia contribución.
6. Después de cierres/revisiones, EN_PROGRESO propio; después, PENDIENTE libre con COLA!O=SI aunque G=NO (G=SI solo para merge/LISTO). Sin CAS, leases, bloqueos o preasignación en Sheets. Se preservan límites 15/15/15 y alcance DEV sin QA/main/PROD.
Esta enmienda no autoriza pasar scopes concretos a LISTO antes de sus controles reales ni alterar rulesets/workflows. No ejecutar el cambio de selector hasta que se integre protegidamente al HEAD dev.

# PROJECT_SCOPE_LOCK — SOLQARYN

PROJECT_SCOPE_LOCK=STRICT
PLATFORM=SOLQARYN
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
LOCAL_SKILL_COUNT=1
LOCAL_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT
CONTEXT_MODE=CURRENT_STATE_ONLY
EXECUTION_PLAN_DRIVE_FILE_ID=1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA
COMMERCIAL_AUTHORITY_MODE=CURRENT_PLAN_ONLY
LEGACY_MASTER_EXECUTABLE=FALSE
TASK_COUNT_LIMIT=PROVIDER_EFFECTIVE_CAPACITY
AUTO_CERTIFICATION_MODE=REQUIRED_SCOPE_LOCK

## ORDEN VIGENTE — 15/15/15 CON ASIGNACION AL DISPARARSE LA TASK (2026-11-05)

**Precedencia:** orden expresa del propietario: distribucion directa por microtarea, SIN BLOQUEO EN SHEETS, sin compare-and-swap, sin escritor de reservas ni control de vencimiento. El contrato 15/15/15, la autoria independiente, los permisos y PROJECT_SCOPE_LOCK=STRICT permanecen.

- MAX_CONCURRENT_SCOPES=15; MAX_OWNED_SCOPES_PER_TASK=15; MAX_ACTIVE_WRITES_PER_TASK=15. Son techos de scopes distintos, NO instruccion de reclamar quince trabajos por Task.
- La propia Task selecciona trabajo cuando su ocurrencia nativa se inicia; no se apartan scopes ni se escriben propietarios por anticipado. Solo usa el plan comercial vigente, sus dependencias, PLAN_MAESTRO/COLA, BITACORA y GitHub live; la historia no impide un nuevo trabajo libre.
- **Reparto sin reservador:** para cada scope, el selector scripts/automation-review-routing.mjs determina exactamente una Task revisora distinta del autor usando los seis IDs autorizados y preferencia fija solo para las cinco entregas actualmente VALIDANDO. El resultado es routing consultivo (no propiedad, no lease, no bloqueo en Sheets). Si un actor no es el seleccionado, debe continuar con otro scope correspondiente a su ruta. El routing debe ser identico para todas las Tasks; no elegir a dos revisoras para un scope.
- Primero revisar VALIDANDO ajeno con PR verificable y sin actor/editor activo comprobado; despues continuar EN_PROGRESO propio o transferencia verdaderamente autorizada; finalmente PENDIENTE elegible con todas las dependencias LISTO. NO exigir demostrar inexistencia absoluta de actividad externa de Codex; solo respetar conflictos POSITIVOS vigentes. La delegacion historica o un comentario viejo no constituyen ocupacion.
- Al iniciar una edicion, contrastar GH branch/PR y el responsable actual. Mantener una sola rama/PR por scope. GitHub protege el commit y los cambios concurrentes con sus controles nativos. Nunca recrear una rama/PR existente ni forzar pushes, reescribir historia o editar otro scope activo. Ante conflicto real, parar solo ese scope y seleccionar otro.
- La revision tecnica solo puede hacerla otra TASK_ID diferente del AUTHOR_TASK_ID. No convertir un login compartido en GitHub APPROVED formal; no aprobarse a si misma, no editar entregas de Codex ocupadas. Los cierres exigen CI exact-SHA requerida, merge protegido aplicable, validacion independiente y conciliacion PLAN_MAESTRO/COLA/BITACORA.
- Cuando la CI siga en curso, registrar CI_PENDING_ASYNC una vez, continuar otra tarea de la misma ruta sin esperar el workflow. Al liberarse trabajo no hay reserva que cancelar: conservar autor y PR, documentar estado y finalizar el turno.
- Nunca crear Tasks, cambiar sus RRULE, habilitar QA/main/PROD ni desactivar checks exigidos. Si falta informacion critica de permisos o hay conflicto positivo, NO_GO solo en el scope afectado; no convertir la falta de una reserva historica en bloqueo global.

## Enmienda operativa propuesta — certificación automática CI en DEV (2026-10-30)

**Vigencia condicionada:** esta norma solo sustituye la revisión formal para futuros PR comerciales ordinarios cuando el changeset esté fusionado por PR protegido en `dev`, el job requerido `verify-solqaryn-project-scope` ejecute la certificación contra el SHA exacto y exista readback de CI. Durante la implantación/bootstrap de este PR normativo sigue exigida revisión formal realmente independiente. No se simula un `APPROVED` humano: `AUTO_CERT=PASS` significa exclusivamente certificación técnica de GitHub Actions.

Para PR comerciales de ramas deterministas `work/solqaryn-<microtarea>`, el gate automático comprueba identidad y repositorio originales, alcance/diff, base DEV, SHA exacto, ausencia de secretos añadidos en código productivo, tres checks requeridos (incluyendo el propio Scope Lock), pruebas específicas del scope, resultado de todos los checks aplicables y rechazo fail-closed de fallos, faltas de evidencia o cambios concurrentes de HEAD. El código del verificador se ejecuta desde el commit BASE confiable, no desde el PR que evalúa. El job de Scope Lock ya es obligatorio en el ruleset de dev; no se requieren botones ni aprobación humana para el PR comercial que supera toda la certificación y sea fusionado mediante permisos reales y el flujo protegido. Un PASS de una Task o de Sheets no sustituye este gate.

Los PR que cambian gobierno, Skill, reglas de seguridad, workflows requeridos o el propio verificador **siguen necesitando revisión formal independiente** del SHA final. El bootstrap no puede autocertificarse. Ningún cambio expande el allowlist comercial 5/6 ni habilita Alex :30; no altera QA/main/PROD, credenciales, Aiven, grants ni despliegues. Una Task no puede autoaprobarse ni usar otra identidad. El cierre `LISTO` exige merge protegido, CI postmerge aplicable y readback de `PLAN_MAESTRO`, `COLA` (fórmula intacta) y `BITACORA`. Revertir mediante PR protegido con nuevo CI, sin eliminar evidencia histórica.

## Alcance actual

La fuente comercial aprobada es `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` (ID de Drive citado arriba). Los planes antiguos, manifestaciones, hojas, Tasks y calendarios ya retirados no tienen autoridad ejecutiva, y no se reutilizan automáticamente. El estado de automatización se documenta solamente en `docs/AUTOMATION_STATUS.md`.

## Autoridad actual de microfase 3

`docs/AUTOMATION_AUTHORITY.md` regula un inventario de Tasks configurable por decisión institucional protegida. No existe techo interno de cantidad ni distribución obligatoria por cuenta; altas, bajas o cambios requieren comprobar capacidad efectiva del proveedor y conciliar identidad, ID, propietario, horario, habilitación y permisos. El censo vigente tras aprobación protegida de la enmienda 2026-11-04 se amplía a seis Tasks autorizadas (Javier :00/:20/:30/:40 y Alex :10/:50), sin techo interno ni reparto obligatorio; la antigua Task Alex :30 permanece excluida, sin reutilizar su ID. El estado operativo y el allowlist comercial siguen en `docs/AUTOMATION_STATUS.md`; el alta de Javier :30 requiere revisión independiente, CI exact-SHA, merge protegido y readback de la Task y Sheets; el WIP máximo de seis microtareas permanece intacto. La propietaria institucional y los ejecutores Javier y Alex conservan paridad humana sin herencia de sesiones o permisos. `COMMERCIAL_AUTHORITY_MODE=CURRENT_PLAN_ONLY` y `LEGACY_MASTER_EXECUTABLE=FALSE` permanecen obligatorios. Las reglas completas residen en `docs/AUTOMATION_AUTHORITY.md`.

## Identidad, código y seguridad

Se permite una única Skill local: `solqaryn-project-governance`. Las referencias externas se utilizan solo bajo el registro y allowlist actual del proyecto, con pin y fuente aprobados. No recurrir a otras identidades, proyectos o repositorios como fallback.

El trabajo ordinario se hace en `dev` por PR protegido. Nunca usar force-push, alterar `main` o producción sin autorización específica. Conservar aislamiento empresarial, JWT, RBAC, auditoría, migraciones MySQL, respaldo, pruebas de código actuales y secretos fuera del repositorio.

Las restricciones de seguridad del producto no son hitos de un roadmap antiguo: permanecen mientras la funcionalidad protegida exista. Las validaciones históricas que exigen certificaciones de fases anteriores deben ejecutarse manualmente o sustituirse por regresiones actuales relevantes, no bloquear cada nuevo cambio sin causa.

### Archivo histórico 1.2 — dos Tasks (no vigente para la topología 3+3)

`PRIMARY_01` corresponde a Javier y `SECONDARY_02` a Alex. En el
piloto anterior, una Task escritora DEV y una Secondary READ_ONLY eran la
capacidad autorizada; esa fotografía no limita el nuevo diseño 3+3.
Ninguna Task inexistente obtiene permisos por el mero cambio del techo a 6.
`PROJECT_SCOPE_LOCK=STRICT` conserva identidad, propiedad institucional,
aislamiento de datos y los permisos efectivos por cuenta. El Maestro
anterior (`SQ-000` a `SQ-353` y Word histórico) es visión futura
NO EJECUTABLE, jamás fuente de microtareas ni autorizaciones.

## Control de cambios

Los cambios deben analizar consumidores directos, rollback, tests y documentación. No crear nuevas Tasks o automatizaciones sin autorización expresa. El histórico queda en Git; no se reescribe ni borra la genealogía de commits.
