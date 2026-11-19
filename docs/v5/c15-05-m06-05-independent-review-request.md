# C15-05.M06.05 — solicitud institucional de revisión independiente

PROJECT_SCOPE_LOCK=STRICT
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
MICROTAREA_ID=C15-05.M06.05
AUTHOR_TASK_ID=6adbb9fff1548191a1bd5b30d9e9984a
RISK=R0_DOCUMENTAL
A5=SOLICITUD_PENDIENTE_NO_A5_PASS

## Acción y ausencia de atribución ficticia

PLAN_MAESTRO!D2340 exige solicitar revisión independiente de un lote; E2340 describe el resultado acumulativo de M06, no una certificación requerida a esta unidad R0. Esta solicitud es un paquete consultable en dev por otra Task Javier, **no una revisión realizada ni un mensaje enviado**, no modifica owner I, no crea Tasks ni concede QA/main/PROD. M06.04 hace un ensayo rollback separado; M06.06 exige sus propios gates y no se libera por el presente documento.

## Seis fuentes para revisión independiente

| ID | SHA causal | Evidencia y límites |
| --- | --- | --- |
| C15-05.M05.01 | [61b9716e8700](https://github.com/solqaryn/Solqaryn/commit/61b9716e87001b3836b3bc499f8b216e1f0c3b6c) | xUnit CAS in-memory; no candado Sheets distribuido |
| C15-05.M05.02 | [075505536423](https://github.com/solqaryn/Solqaryn/commit/0755055364237839d566f0fb4b455aeedf033ce9) | xUnit 30/30, TestServer JWT real; grants de permission fixture, A5_FAIL historico P2 preservado |
| C15-05.M05.03 | [f4827013eb41](https://github.com/solqaryn/Solqaryn/commit/f4827013eb41321dc90da46749bc62e3e9c0f498) | GitHub GET Checks API real, solo lectura y negativos; no GitHub write |
| C15-05.M05.04 | [ae1d0de5b5a9](https://github.com/solqaryn/Solqaryn/commit/ae1d0de5b5a94820d4ac8d7813d246f874ebb091) | Chromium 3/3 con sesion sintetica; no JWT firmado |
| C15-05.M05.05 | [43fd19b1a47e](https://github.com/solqaryn/Solqaryn/commit/43fd19b1a47e45e5c115e4f5706acba438b28fe9) | xUnit 12/12 negativo in-process; no seguridad global distribuida |
| C15-05.M05.06 | [88da235e7edb](https://github.com/solqaryn/Solqaryn/commit/88da235e7edb381a41fd91862643fc744d937ce5) | Chromium 4/4 responsive; sin dispositivos fisicos, CWV de campo ni HTTP/JWT live |

La fila C15-05.M05.02 conserva un A5_FAIL histórico por faltas de HTTP, paginación y errores; se aplicó reparación y A5_PASS posterior al SHA `0755055364237839d566f0fb4b455aeedf033ce9`, sin borrar FAIL. Fuente institucional: PLAN_MAESTRO!A2330:M2335, BITACORA!A:Z y los jobs GitHub Actions de cada changeset. La suite [Chromium de M05.06](https://github.com/solqaryn/Solqaryn/actions/runs/44605758500/job/135848875878) confirma 4 casos de UI sintética, no permisos live ni rendimiento de campo.

## Contrato verificable para el revisor elegible

1. La revisión A5 se realiza **solo por una Task Javier con TASK_ID distinto del autor y del reparador de cada cambio**. La cuenta GitHub jmejia31 común no acredita separación. Leer nativo/lease, H/I/K, BITACORA y SHA antes de revisar o escribir, adquirir V17 por ID. No autopase ni aceptación humana obligatoria.
2. Auditar diff/código, fuente, pruebas positivas y negativas, límites y errores, test/CI exact-SHA y logs. Requeridos `status=completed` y `conclusion=success` para `verify-solqaryn-project-scope`, V5 y runner .NET/Angular/Node/Chromium según tecnología; SKIPPED, CANCELLED, FAILED, QUEUED, IN_PROGRESS **NO SON PASS** si son requeridos. Descendiente exige blobs y dependencias probadamente idénticos.
3. Verificar casos de autor/tenant A/B, exclusión CAS, idempotencia, replay, paginación y permisos del backend según alcance. No equiparar mocks de sesión, pruebas unitarias in-memory o GitHub GET con grants productivos, MySQL transaccional, JWT externo, locks distribuidos o soporte R3.
4. Revisar las A5 anteriores con autor/reparador/SHAs, incluyendo A5_FAIL histórico, errores P0/P1 y decisiones de QA_DEFECT_FOUND. Emitir PASS acotado o FAIL falsable; jamás ocultar fallo ni forzar fórmulas COLA/DAG.
5. Registrar EVENT_ID e IDEMPOTENCY_KEY **únicos** en BITACORA A:Z con SHA causal, trabajo realmente probado, Task revisor distinta, fallos, job/link, timestamp UTC y readback PLAN/COLA/DAG. Conservar owner original en I; liberar solo candado propio. No invocar otro ejecutor por un archivo.
6. Esta solicitud no aprueba restauraciones reales, secretos, pagos, producción, despliegues, nuevos schedulers, QA/main/PROD ni reescrituras del historial.

## A0–A5, límites y rollback del presente diff

- A0: solo este Markdown y sección autocontenida en CHANGELOG_AI.md; seis SHA y enlaces verificados; sin modificación de Angular, .NET, EF/MySQL, API, JWT, RBAC, secretos ni CI.
- A1: matriz positiva/negativa y de frontera/error falsable de revisión; no declara que estas pruebas hayan vuelto a ejecutarse.
- A2: NO_APLICA a cambios de seguridad tenant/JWT/RBAC en runtime por ser documento R0. Mantener los riesgos abiertos.
- A3: NO_APLICA a nueva integración HTTP/MySQL/Chromium/proveedor por no cambiar runtime; no confundir el test histórico real con prueba nueva.
- A4: commit FF con CAS expected HEAD; comprobar `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success exact-SHA, readback H/I/COLA/BITACORA, luego liberar V17.
- A5: PENDIENTE de Task Javier genuinamente independiente, no A5_PASS de esta Task.

Rollback: revert normal del commit documental tras revalidar HEAD, sin force-push; conservar BITACORA histórica. Sin cambio de código/infraestructura ni R3.
