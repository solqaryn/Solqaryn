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
