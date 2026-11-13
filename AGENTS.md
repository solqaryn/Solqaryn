# AGENTS — SOLQARYN (instrucciones vigentes)

# SOLQARYN V5 — autoridad operativa vigente (13 noviembre 2026)

**ÚNICA CONFIGURACIÓN OPERATIVA: 6 Tasks de Javier.** Esta orden posterior expresa del propietario revoca toda disposición anterior que permitiera ejecutar, registrar como elegibles, reactivar, recuperar o usar para revisión A5 las Tasks 07/08 de Alex. Toda cita previa de 8 Tasks, roles secundarios activos o reactivación de Alex carece de autoridad operativa. Los registros de auditoría existentes no se borran ni se interpretan como permisos vigentes. Los dos TASK_ID de Alex están excluidos del allowlist SOLQARYN. Según el propietario sus Tasks físicas ya están pausadas; esta sesión no tiene acceso administrativo a ellas, por lo que no certifica su estado físico.

PLATFORM=SOLQARYN
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
PROJECT_SCOPE_LOCK=STRICT
AUTOMATION_RUNTIME=ACTIVE_DEV
TASK_DISPATCH_STATE=ACTIVE_V5_JAVIER_6_ONLY
SCHEDULED_CHATGPT_TASKS_AUTHORIZED=6
PHYSICAL_JAVIER_TASKS_ENABLED=6
AUTHORIZED_COMMERCIAL_TASK_IDS=6adbb9fff1548191a1bd5b30d9e9984a,6af16bb9492c8191b8e8c7d606f11507,6adc2e4bd8a48191ac7d8641ed95a1a3,6aeacb7820ac8191b7d768a6f33dfa09,6adc2e55f47c81919f6374e561687076,6af16bbad26881918c283abe06e1f32a
REGISTERED_TASK_IDS=6adbb9fff1548191a1bd5b30d9e9984a,6af16bb9492c8191b8e8c7d606f11507,6adc2e4bd8a48191ac7d8641ed95a1a3,6aeacb7820ac8191b7d768a6f33dfa09,6adc2e55f47c81919f6374e561687076,6af16bbad26881918c283abe06e1f32a
PHYSICAL_TASKS_REGISTERED=6
ALEX_TASKS_PROJECT_AUTHORIZED=0
ALEX_PEER_WATCH_SCOPE=NONE
JAVIER_PEER_WATCH_SCOPE=SIX_EXISTING_TASK_IDS
JAVIER_PEER_AUTORESUME=AUTHORIZED_SINGLE_ATTEMPT_WITH_READBACK
WORK_BRANCH_CREATION_ALLOWED=FALSE
QUALITY_PROTOCOL_VERSION=V13_A0_A5
TIMEZONE=America/Tegucigalpa
EXECUTION_PLAN_DRIVE_FILE_ID=1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA
SHEET_ID=1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU

| Ejecutor autorizado | TASK_ID | RRULE |
| --- | --- | --- |
| Javier DEV 01 | `6adbb9fff1548191a1bd5b30d9e9984a` | FREQ=HOURLY;BYMINUTE=0;BYSECOND=0 |
| Javier DEV 02 | `6af16bb9492c8191b8e8c7d606f11507` | FREQ=HOURLY;BYMINUTE=10;BYSECOND=0 |
| Javier DEV 03 | `6adc2e4bd8a48191ac7d8641ed95a1a3` | FREQ=HOURLY;BYMINUTE=20;BYSECOND=0 |
| Javier DEV 04 | `6aeacb7820ac8191b7d768a6f33dfa09` | FREQ=HOURLY;BYMINUTE=30;BYSECOND=0 |
| Javier DEV 05 | `6adc2e55f47c81919f6374e561687076` | FREQ=HOURLY;BYMINUTE=40;BYSECOND=0 |
| Javier DEV 06 | `6af16bbad26881918c283abe06e1f32a` | FREQ=HOURLY;BYMINUTE=50;BYSECOND=0 |

## Despacho, continuidad y rescate

1. Conservar habilitadas las seis Tasks Javier existentes; no crear nuevas ni modificar IDs, horarios, TZ, RRULE o prompts sin causa y autorización específica. No activar, ejecutar ni actualizar Tasks privadas ajenas.
2. Orden por corrida: REVIEW_FIRST de una VALIDANDO propia; rescate de BLOQUEADA/VALIDANDO huérfana con diagnóstico causal; nueva PENDIENTE libre y elegible. Continuar mientras permita el proveedor sin cuota artificial de tres cierres.
3. Cada claim requiere identidad de Task Javier, estado PLAN_MAESTRO, unicidad del ID/owner, prueba de permisos y readback; la cola es reactiva por fórmulas, no un scheduler. Nunca dos escritores sobre una microtarea.
4. Para una microtarea cuyo responsable sea un ID retirado, **primero verificar ausencia de corrida/lease/escritura activa**, releer el owner y efectuar relevo exclusivo compare-and-swap hacia una de las seis Tasks Javier; registrar antiguo owner, nuevo TASK_ID, motivo, SHA y timestamp. Si no puede acreditarse la exclusividad, NO_GO al relevo y tomar otra PENDIENTE independiente. No borrar la evidencia original ni marcar LISTO sin auditorías.
5. Permitir rescate técnico asistido sin transferir owner mientras haya ejecución activa: otra Task Javier puede investigar CI, corregir fallo independiente o generar evidencia, sin reclamar cierre simultáneo.
6. Si una Task Javier está pausada sin orden de pausa vigente ni acción humana requerida, otra de las seis puede intentar reactivación física **una sola vez** con permisos reales y readback; nunca intentar con cuentas retiradas.
7. C00-01.M05.04, C01-04.M01.01 y otras asignaciones heredadas requieren revalidación real del estado del Sheet; no se declara concluido ningún bloqueo solo por esta modificación de gobernanza.

## Calidad, seguridad y dependencias

- A0 análisis estático; A1 funcional; A2 seguridad/JWT/RBAC/multitenancy; A3 integración causal; A4 post-publicación exact-SHA en dev. A5 requiere TASK_ID distinto del autor (entre las seis Javier) y revisión independiente. Para documentales, NO_APLICA motivado, nunca PASS inventado.
- CI pertinente según tecnología; Scope Lock STRICT siempre requerido; CANCELLED, SKIPPED o IN_PROGRESS no son PASS cuando la prueba es obligatoria. Validar check-runs contra SHA exacto y fuente actual.
- Documentación V5 FINAL contiene 2.340 microtareas. PLAN_MAESTRO!H es estado autoritativo; COLA deriva elegibilidad; BITACORA A:Z registra EVENT_ID e IDEMPOTENCY_KEY únicos y conserva historial inmutable.
- DAG_TECNICO puede adelantar preparación **documental** independiente ante predecesor VALIDANDO con evidencia, owner y ausencia de FAIL. No permite cierres terminales M01.06/M06, cambios IAM/JWT/RBAC/tenant, migraciones DB, deployments ni QA/main/PROD. Meta elástica de elegibles: MAX(24,3*N), N ejecutores realmente habilitados (actualmente 6), sin liberar dependencias falsas.
- En C00-01.M06.06 preservar gate A5_FAIL histórico de M04.03 hasta A5_PASS causal independiente del SHA pertinente; no retirar bloqueos técnicos reales por orden administrativa.
- Repositorio exclusivo solqaryn/Solqaryn, rama dev; commits fast-forward serializados sin PR o rama auxiliar; no force-push. QA/main/PROD, secretos, datos y dominios productivos requieren autorización diferenciada.
- Autoridad documental única: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, Drive ID arriba. No reactivar VAEP/Jules ni planes archivados.
- Si esta documentación contradice ejecución física, reportar discrepancia y aplicar límites reales; el estado físico exige readback. Se conserva el historial técnico en Git y BITACORA, no como instrucciones activas.

## Regla de precedencia
Esta autoridad posterior reemplaza instrucciones operativas anteriores sobre ocho Tasks. Cualquier referencia heredada no autoriza reclamos ni revisiones. Para ampliar o modificar el alcance consultar `docs/PROJECT_SCOPE_LOCK.md`, la Skill canónica y HEAD de dev. No eludir permisos del proveedor, A0–A5 ni pruebas requeridas.
