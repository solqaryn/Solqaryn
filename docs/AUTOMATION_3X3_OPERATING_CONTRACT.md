# SOLQARYN — Contrato operativo 3+3 de microfase 3

VERSION=2.0
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BASE_BRANCH=dev
TIMEZONE=America/Tegucigalpa
DESIGN_TASK_LIMIT=6
DESIGN_JAVIER_TASKS=3
DESIGN_ALEX_TASKS=3
MAX_OPEN_WORK_BRANCHES=2
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
3. Tomar `PENDIENTE` con **todas** sus dependencias en `LISTO`, prioridad P0 antes
   que P1 y orden del Plan Maestro dentro de una misma prioridad.
4. Si no hay elegibles, dejar como máximo un evento deduplicado
   `SIN_TRABAJO_ELEGIBLE` y finalizar esa ejecución.
5. Tras un cierre `LISTO`, recalcular dependencias y tomar otra elegible dentro
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

Se mantiene `VALIDANDO` durante reparaciones del revisor. No regresar
artificialmente a `EN_PROGRESO` por un defecto corregible.
Si una ejecución termina por limite del proveedor, conservar estado veridico,
commits, PR y responsable: nunca simular continuidad infinita o cierre.

## Ramas, revision e idempotencia

Ramas permanentes: `dev`, `qa` y `main`. Para cada microtarea elegible
crear/reutilizar **una** rama determinista `work/solqaryn-<id-normalizado>`
y PR exclusivo hacia `dev`. No utilizar una rama almacén compartida.
Máximo dos ramas de trabajo abiertas al inicio del piloto; cuentan todas
las ramas `work/solqaryn-*` no fusionadas y ramas operativas de esta microfase
cuando correspondan, sin borrar trabajo existente.

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
