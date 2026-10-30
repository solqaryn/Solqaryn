# SOLQARYN — Contrato operativo 3+3 de microfase 3

VERSION=2.0
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BASE_BRANCH=dev
TIMEZONE=America/Tegucigalpa
DESIGN_TASK_LIMIT=6
DESIGN_JAVIER_TASKS=3
DESIGN_ALEX_TASKS=3
MAX_OPEN_WORK_BRANCHES=6
MAX_CONCURRENT_SCOPES=6
MAX_OWNED_SCOPES_PER_TASK=2
MAX_ACTIVE_WRITES_PER_TASK=1
REVIEW_INDEPENDENCE_REQUIRED=TRUE
VAEP_DISPATCH_AUTHORIZED=FALSE
JULES_WORKERS_AUTHORIZED=0

## Autoridad y etapa de implantacion

Este contrato v2.0 se aplica al piloto 3+3 **solo cuando** este archivo y los validadores
esten integrados en el HEAD vigente de `dev`, el Scope Lock sea STRICT y el proveedor
demuestre permisos efectivos. `DESIGN_TASK_LIMIT=6` es un techo de arquitectura, no
una prueba de seis Tasks creadas, habilitadas o materialmente ejecutadas. Los conteos
físicos y autorizados actuales se leen en `docs/AUTOMATION_STATUS.md` y Google Sheets.

Se conservaron las dos Tasks físicas originales con sus TASK_ID estables y se
crearon las otras cuatro durante M3.2, desde cada cuenta. El readback de las seis
configuraciones está en `TAREAS_PROGRAMADAS!A2:Z7` y BITACORA. La instalación no incrementa por sí misma el contador de autorizaciones comerciales.
La autoridad efectiva depende de `SCHEDULED_CHATGPT_TASKS_AUTHORIZED` y de
`AUTHORIZED_COMMERCIAL_TASK_IDS`, enumerados en `docs/AUTOMATION_STATUS.md` y
`docs/AUTOMATION_AUTHORITY.md`: cada ID nuevo requiere evidencia, autorización
individual, PR protegido y readback. La habilitación física no permite escritura
comercial a los slots que no figuran en la lista, aunque el flag global de Sheets
esté activo. Está prohibido usar sesiones o credenciales
de otra cuenta o invocar la Task privada de Alex desde Javier.

## Programacion nominal, no asignacion

| Slot | Cuenta | Minuto por hora | Implantacion |
| --- | --- | --- | --- |
| PRIMARY_DEV_01 | JAVIER_CHATGPT | :00 | ID original conservado; instalada |
| SECONDARY_REVIEW_01 | ALEX_CHATGPT | :10 | ID original conservado; instalada |
| PRIMARY_DEV_02 | JAVIER_CHATGPT | :20 | Instalada desde Javier |
| SECONDARY_REVIEW_02 | ALEX_CHATGPT | :30 | Instalada desde Alex |
| PRIMARY_DEV_03 | JAVIER_CHATGPT | :40 | Instalada desde Javier |
| SECONDARY_REVIEW_03 | ALEX_CHATGPT | :50 | Instalada desde Alex |

Los identificadores originales `PRIMARY_01` (Javier) y `SECONDARY_02`
(Alex) se conservan como aliases operativos aun despues de la migracion M3.2 de las
etiquetas logicas `PRIMARY_DEV_01` y `SECONDARY_REVIEW_01`.
Los **TASK_ID físicos originales no cambian**. La numeración en pantalla
no transfiere permisos ni duplica una Task; `TAREAS_PROGRAMADAS` debe
confirmar unicidad de ID/propietario/horario.

Los nombres Primary/Secondary y Dev/Review **no conceden privilegios ni
limitaciones distintos**: las seis tienen igual capacidad funcional de desarrollar,
revisar, corregir, preparar PR y tramitar el cierre en DEV. Cada accion requiere
identidad de su propia cuenta, permiso real, contrato vigente y ausencia de conflicto.
No se garantiza inicio ni duracion ininterrumpida por programacion nominal de ChatGPT.

**SIMETRIA SIN EXCEPCIONES POR NOMBRE:** Javier Primary 01/02/03 Y Alex
Secondary 01/02/03 pueden seleccionar indistintamente una microtarea
`PENDIENTE` elegible para DESARROLLAR o una microtarea `VALIDANDO` de la
otra cuenta para REVISAR, CORREGIR y tramitar el cierre. Un nombre
`REVIEW` NO significa solo lectura ni prohíbe crear ramas/PR; un
nombre `DEV` NO prohíbe revisión de PR ajenos. La prioridad
`REVIEW_FIRST` establece el ORDEN de búsqueda de trabajo disponible,
no la asignación exclusiva de revisión a Alex ni de desarrollo a Javier.
La autonomía equivalente nunca elimina la independencia entre autor
y revisor, los permisos efectivos, el Scope Lock ni los checks de GitHub.

## Seleccion y continuidad

Orden uniforme para las seis Tasks:
1. Reanudar trabajo propio incompleto con rama/PR y evidencia, si no hay otro editor.
2. Revisar `VALIDANDO` que provenga de otra identidad verificable y no tenga revisor activo; confirmar PR GitHub y permisos efectivos antes de asumir la revisión.
3. Tomar `PENDIENTE` con **todas** sus dependencias en `LISTO` solo si WIP global es menor que 6 y se dispone de cupo propio. Primera tarea sin otro scope asignado; segunda únicamente si la primera está `VALIDANDO` sin edición activa, el actor verificó `REVIEW_FIRST` y no hay revisión ajena abordable. Nunca mantener dos trabajos en escritura activa desde la misma Task. Prioridad P0 antes
   que P1 y orden del Plan Maestro dentro de una misma prioridad.
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
- `VALIDANDO -> VALIDANDO` (correccion): el revisor de **otra cuenta**
  diagnostica y corrige en la misma rama/PR, sin devolver la implementacion al
  primer actor. Sus cambios quedan identificados por SHA, autor y pruebas nuevas.
- Si el revisor añadió commits, necesita revisión **independiente** de sus cambios
  antes de cierre. El autor original puede analizar el diff correctivo, pero no
  emitir una aprobacion formal de su propio PR; si la proteccion exige aprobacion
  formal adicional se requiere otro revisor elegible. Ninguna identidad puede
  aprobar sus propias modificaciones fingiendo independencia.
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

### Aceptación excepcional y limitada — P1-001.A / PR #3793 (2026-10-30)

El propietario aceptó expresamente una excepción única para la falta de revisión
independiente previa al merge del PR #3793, correspondiente exclusivamente a
P1-001.A. El PR fue creado por `jmejia31` con el commit
`e5032751b22cb1eab56762f5c79638c777a9ed27` y se integró en `dev` el
`2026-10-30T16:12:24Z` como merge SHA
`064dfd4979811164ab34c1e0319b3b7b609db44e`, por `solqaryn-platform`. GitHub
registra `reviews=[]`: no hubo aprobación formal previa ni posterior en ese PR.
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
el `2026-10-30T16:31:36Z`. Es una revisión formal posterior al merge, pero el
perfil consultado identifica una cuenta institucional sin una persona humana
nominal; no basta para satisfacer la condición de representante humano
identificable que exige esta aceptación. Alex (`morales35alex`) fue solicitado
como revisor en #3793 y en el borrador #3795. El control seguirá pendiente
hasta que Alex o un representante humano identificable de SOLQARYN Platform
revise el diff exacto de #3793 y el flujo real #41087748364, y consigne el
resultado en una review formal. No habilitar el merge de #3795 antes de esa
revisión.

Se mantiene `VALIDANDO` durante reparaciones del revisor. No regresar
artificialmente a `EN_PROGRESO` por un defecto corregible.
Si una ejecución termina por limite del proveedor, conservar estado veridico,
commits, PR y responsable: nunca simular continuidad infinita o cierre.

## Ramas, revision e idempotencia

Ramas permanentes: `dev`, `qa` y `main`. Para cada microtarea elegible
crear/reutilizar **una** rama determinista `work/solqaryn-<id-normalizado>`
y PR exclusivo hacia `dev`. No utilizar una rama almacén compartida.
Máximo **seis scopes comerciales distintos activos** (`MAX_CONCURRENT_SCOPES=6`, equivalente a `MAX_OPEN_WORK_BRANCHES=6` aplicado solo a ramas de microtareas comerciales), independientemente de que hoy solo cinco Tasks estén autorizadas. Cada Task de la allowlist 5/6 puede mantener **a lo sumo dos scopes asignados**, nunca más de uno con escritura activa. La segunda toma solo se permite con la primera en `VALIDANDO` y PR real, sin otro editor/revisor correctivo sobre el primer scope, luego de haber comprobado la ausencia de una revisión ajena abordable conforme a `REVIEW_FIRST`. Una Task con scope `EN_PROGRESO` debe RECOVER antes de CLAIM; una Task con dos scopes abiertos no puede reclamar un tercero. Contar IDs de microtarea únicos del Plan Comercial Inicial con rama determinista `work/solqaryn-<id-normalizado>`, PR abierto o entrega real en progreso, sin doble contar una misma microtarea. No cuentan PR normativos (#3743/#3745), ramas de diagnóstico `test/solqaryn-m3-2-native-write-*`, auditorías, ramas ya fusionadas ni estados obsoletos del Sheet. Toda rama existente conserva exclusividad de edición, aunque no consuma WIP. La toma requiere branch create-if-absent nativa y readback de dueño, permisos y dependencias `LISTO`. El sexto cupo **NO** autoriza a Alex :30, ni una segunda Task bajo la misma cuenta, ni revisión propia o saltos de CI. Jamás borrar trabajo para liberar cupo.

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
5. Review correctiva: una Task de la otra cuenta corrige en mismo PR.
6. Correcciones del revisor: comprobación independiente de esos commits,
   sin autoaprobaciones ni supuestas aprobaciones nativas.
7. CI exact-HEAD; merge protegido real; `VALIDANDO -> LISTO` con readback.
8. Sin trabajo: evento deduplicado, sin nuevas ramas ni microtareas ficticias.
9. Interrupcion: reanudar desde commit/PR sin perder ni duplicar.
10. Perdida de permisos o de Google Sheets: bloquear solo operación afectada;
    reconciliar desde pruebas reales.

La certificacion requiere artefactos efectivos, no porcentajes o campos de
RUN_ID imposibles. El contrato 3+3 no habilita por sí solo cuatro Tasks inexistentes.
