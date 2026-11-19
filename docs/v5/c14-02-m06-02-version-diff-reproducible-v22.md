# C14-02.M06.02 — manifiesto reproducible de versiones, diff y resultados (V22)

**SOLQARYN, repo `solqaryn/Solqaryn`, rama `dev`, `PROJECT_SCOPE_LOCK=STRICT`.** MICROTAREA_ID=C14-02.M06.02; AUTHOR_TASK_ID=6adbb9fff1548191a1bd5b30d9e9984a. Riesgo R0 de este cambio documental; las pruebas y áreas de negocio subyacentes pueden ser R2. PLAN D2085 exige **registrar versión, diff, artefactos y resultado reproducible**. PLAN E2085 es objetivo acumulativo global de seguridad y no se certifica con esta matriz ni con un BUILD verde.

## A0 — fuentes congeladas por SHA, su resultado y procedencia

| Prueba fuente | Commit de código y cómo reproducir la evidencia | Resultado documentado y límite causal |
| --- | --- | --- |
| C14-02.M05.01 | [`6f44a0df7265e401f127dfc83632def0a06afcf6`](https://github.com/solqaryn/Solqaryn/commit/6f44a0df7265e401f127dfc83632def0a06afcf6); [job Vitest](https://github.com/solqaryn/Solqaryn/actions/runs/44473864419/job/135389570199) | 5/5 reglas Angular TestBed, sesión denegada con fixtures; **no** JWT/SQL servidor |
| C14-02.M05.02 | [`2cc9ef8f09718457defec1dda8e240691a7ce2bd`](https://github.com/solqaryn/Solqaryn/commit/2cc9ef8f09718457defec1dda8e240691a7ce2bd); [job autenticado](https://github.com/solqaryn/Solqaryn/actions/runs/44550252419/job/135651731863) | 15/15 test HTTP/DTO ASP.NET/JWT firmado A/B; 3/3 MySQL8.4 efímero, negativos 401/403; no proveedor productivo y A5_FAIL anterior preservado |
| C14-02.M05.03 | [`4cbaab60070f0dc9d9d62814911decd5e647f6cb`](https://github.com/solqaryn/Solqaryn/commit/4cbaab60070f0dc9d9d62814911decd5e647f6cb); [job MySQL](https://github.com/solqaryn/Solqaryn/actions/runs/44541038263/job/135619118041) | 3/3 xUnit Category=Integration Oracle EF/MySQL 8.4.13 real efímero y revocación A/B persistente; no Aiven vivo |
| C14-02.M05.04 | [`7d09ee9905d51be05dcb5d0d55c04f89ff6f8b87`](https://github.com/solqaryn/Solqaryn/commit/7d09ee9905d51be05dcb5d0d55c04f89ff6f8b87); [CI de aceptación](https://github.com/solqaryn/Solqaryn/actions/runs/44482456151/job/135417711633) | xUnit Release casos positivos y adaptadores de evidencia; NO_APLICA integración firmada a diff test-only y no equivale a BOLA global |
| C14-02.M05.05 | [`1c0785f07dce6b4f88c7e0095ef49793ce3866c0`](https://github.com/solqaryn/Solqaryn/commit/1c0785f07dce6b4f88c7e0095ef49793ce3866c0); [job negativo](https://github.com/solqaryn/Solqaryn/actions/runs/44485193274/job/135426779647) | 20 xUnit PASS negativos de reglas de seguridad; no cobertura total de propietario archivo/job/host/caché |
| C14-02.M05.06 | [`73cfb858acb7a6fdf0a90a51c4cfd53662ef8330`](https://github.com/solqaryn/Solqaryn/commit/73cfb858acb7a6fdf0a90a51c4cfd53662ef8330); [job Chromium 5/5](https://github.com/solqaryn/Solqaryn/actions/runs/44634130762/job/135948164434) | Real browser Angular, 4 viewports, Tab/Enter, 403/503 y mutación CSS; sesión interceptada token sintético **NOT-JWT**, DOMContentLoaded local no CWV de campo |

Los seis ID están `LISTO` con evidencias institucionales A5 independientes verificadas en `PLAN_MAESTRO!K/L`; la propia auditoría [M06.01](c14-02-m06-01-auditoria-real-vs-mocks-v22.md) distingue cuáles tests son dobles. Esta tabla es **readback de resultados históricos**, no nuevas ejecuciones. La presencia de `SKIPPED` auxiliar no se eleva a PASS; FAIL histórico y P0/P1 sin resolver en otras superficies permanecen abiertos.

## A1 — protocolo reproducible positivo y adversarial

1. `git fetch origin dev`; colocar HEAD de checkout descartable en el **SHA fuente** de cada fila. Ejecutar `git show --stat --oneline <SHA>` y `git diff <SHA>^ <SHA> -- <rutas_del_scope>` (sin escribir producción); leer blobs y workflows del **mismo SHA**, no extrapolar del HEAD posterior. No crear ramas ni PR institucionales como efecto de esta auditoría.
2. Abrir la ruta `GET /repos/solqaryn/Solqaryn/commits/<SHA>/check-runs?per_page=100`. Exigir `head_sha=<SHA>`, `status=completed`, `conclusion=success` para cada prueba pertinente, workflow y Scope Lock; inspeccionar el log del job, TRX/contador realmente ejecutado y negativos/0 FAIL. Un job `SKIPPED/FAIL/CANCELLED/QUEUED/IN_PROGRESS` **no acredita PASS**. Para validación equivalente en descendiente comprobar hashes iguales de archivos, dependencias, adapters y CI.
3. Distinguir `HttpTestingController`/`page.route` y `localStorage` artificial de servidor HTTP+JWT firmado. Para MySQL real confirmar `mysql:8.4` efímero y `Category=Integration` ejecutado; para Chromium exigir Playwright real y 0 tests skipped. Negativos relevantes: token falsificado 401, tenant B cruzado 403, revocado, 503 sin secretos, CSS overflow inyectado debe **fallar** antes de revertir y recuperar después.
4. Control adversarial: si se usa el resultado de un test de UI sintético para afirmar autorización JWT real o protección de archivos/jobs no probada, rechazar la certificación. Si aparece un fallo P0/P1 de un subsistema, no cambiarlo a NO_APLICA por ser este documento R0. `COLA!G/O` y `DAG_TECNICO!F/G` permanecen fórmulas de solo lectura.

## A2–A5, límites y rollback

- **A2/A3 para este diff R0:** `NO_APLICA` a repetir HTTP/JWT/MySQL/browser porque sólo se añaden este manifiesto y sección aditiva en CHANGELOG. No significa que el gate global de host, roles, BOLA/IDOR, cache, backups y sesiones esté cumplido. Se conserva R3 NO_GO de restore/cutover, QA/main/PROD y secretos.
- **A4:** publicar el documento sobre `dev` por fast-forward Git CAS sobre HEAD real y comprobar `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` en SHA **nuevo exacto**. Realizar CAS V17 `EN_PROGRESO→VALIDANDO→LISTO` con readback PLAN H/I/J/K/L/M, COLA, DAG y BITACORA idempotente, liberar únicamente candado propio. No afirmar LISTO antes de esos checks.
- **A5:** revisor genuino de otra de las seis Tasks Javier distinto del autor DEV01; puede rechazar si falta un job, HASH o se confunde real/mock. Sin autopase ni promoción de entornos.
- **Reversión:** `git revert` normal del commit documental sobre HEAD actual (no reset ni force-push), estado institucional por CAS auditable y BITACORA append-only. No pagos ni operaciones productivas.

Esta salida deja una **receta reproducible** y límites explícitos por artefacto; la siguiente unidad C14-02.M06.03 documenta riesgos de fase, no se auto-certifica C14-02.M06.06.
