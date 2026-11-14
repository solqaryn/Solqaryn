# SOLQARYN — prueba aislada de fencing lógico en Google Sheets (21/10/2026)

## Alcance y autoridad

- Fuente comercial protegida: `1gcVyCoyhLU0jFMwRtf0s5_x8FSnfBs38ojml1QF7Xwk` **NO utilizada** para los ensayos de colisión.
- Fixture nativo independiente (no comercial): [Google Sheets — TEMP VAEP FENCE](https://docs.google.com/spreadsheets/d/1FnChLmpmhCNh8vGKcHmwODhfuiY-PIamcimmM74X3GE/edit), `Sheet1`, `sheetId=1231311045`.
- Repositorio autorizado: `solqaryn/Solqaryn`, base `dev` `82201655abc66e5058968c5c6889e714bb456a63`; parche PR #3709.
- Ninguna ChatGPT Task fue ejecutada ni reprogramada en estos ensayos. No se modificaron leases, OWNER, estados o políticas comerciales. Runtime comercial permanece `PAUSADO`.

## Evidencia real del proveedor (Google Sheets API v4)

La petición tiene **dos subsolicitudes en un mismo `spreadsheets.batchUpdate`**: (1) `addNamedRange` con `namedRangeId` y nombre deterministas en `D1`; (2) `updateCells` con un token distinto por contendiente. El marcador persiste, nunca se borra durante el experimento.

| Prueba | Resultado confirmado por API | Readback |
|---|---|---|
| A y B compiten por mismo marcador | A: éxito; B: `HTTP 400 INVALID_ARGUMENT`, nombre ya existe | A1=`RUN_A`, B1=`RUN_A_TOKEN`; B no sobrescribe |
| Recuperación controlada (marcador siguiente) | Éxito, exactamente dos respuestas | A1=`RECOVERED_FENCED` |
| Escritura tardía que reutiliza marcador de recuperación | `HTTP 400 INVALID_ARGUMENT` | A1=`RECOVERED_FENCED`, B1=`RECOVERED_FENCED_TOKEN` |
| Diez contendientes sobre un mismo scope | **1 éxito, 9 errores 400** por marcador duplicado | A1=`WINNER_0`, B1=`UNIQUE_TOKEN_0`; no hubo mezcla |
| Diez scopes separados en diez filas | **10 éxitos, 0 errores** | A2:A11=`INDEPENDENT_0` ... `INDEPENDENT_9`; tokens exactos B2:B11 |

En todos los rechazos de prueba el proveedor respondió `Invalid requests[0].addNamedRange: Cannot add named range ... a named range with that name already exists`. Se inspeccionó la celda posterior al rechazo y se confirmó que no se ejecutó el `updateCells` competidor.

## Seguridad, reentrada y límites

1. El guard de cada mutación está derivado de `TASK_ID + LEASE_STATUS anterior` y se guarda como un `namedRangeId` permanente; no usa `DeveloperMetadata` ni CAS dual.
2. Todas las escrituras de estado/ownership y la auditoría de una transición deben viajar **en ese mismo batch**. Escrituras separadas o componentes heredados que salten el guard invalidan la garantía.
3. Fencing de reentrada: un runner que conserve la instantánea anterior no puede repetir la inserción de ese mismo marcador. Marcadores ya usados **no se eliminan**.
4. El ensayo demuestra comportamiento observado de la API en un fixture pequeño y controlado; **no acredita por sí mismo** la identidad de una Scheduled Task ni sustituye revisión independiente, prueba de carga de muchas transiciones o una garantía contractual del proveedor sobre todas las carreras posibles.
5. La prueba exige que nadie escriba `_RUNTIME_SOURCE` eludiendo el protocolo; los permisos de editores externos y corridas terminales siguen siendo controles externos al código puro.
6. Si OpenAI deniega un `batchUpdate` operativo, se respeta `TOOL_SAFETY_BLOCK`; este fixture no autoriza reintentar una acción bloqueada con otro endpoint o identidad.
7. El manifiesto sigue `mode=PROPOSAL_ONLY`. No hay activación de `RUNTIME_MODE`, `WRITE_POLICY` ni nuevas microtareas asignadas.

## Revisión independiente pendiente

Revisor solicitado en [PR #3709](https://github.com/solqaryn/Solqaryn/pull/3709): `jmejia31`, distinto de `solqaryn-platform`. Revisar condiciones de carrera, atomicidad del lote, regeneración/reutilización de marcadores, acumulación de named ranges, operaciones comerciales antiguas fuera del guard, autenticidad de terminalidad de Task y fail-closed de todas las transiciones antes del merge. No confundir el fixture con prueba 10/10 de Task-origin writes.
