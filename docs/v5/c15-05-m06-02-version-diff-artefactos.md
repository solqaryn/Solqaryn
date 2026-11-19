# C15-05.M06.02 — manifiesto de versión, diff, artefactos y reproducción (V22)

**ID único:** `C15-05.M06.02`. **Requisito PLAN_MAESTRO!D2337:** «Registrar versión, diff, artefactos y resultado reproducible». **Autor de esta unidad:** DEV06 (`6af16bbad26881918c283abe06e1f32a`). **Entorno:** `solqaryn/Solqaryn@dev` exclusivamente; `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo:** R0 documental: ningún controlador, servicio, migración, dependencia, workflow o interfaz runtime es alterado por este changeset.

## A0 — versión y procedencia

- BASE_HEAD observado para el CLAIM exclusivo V17 de la fila 2337: `db9ced4197dcd34b7e9fec5f1ed37b2c911b9497`; candado `SQV17_C15_05_M06_02`, ID `2000002337`, intervalo H:I, PLAN H=EN_PROGRESO, I=DEV06, CAS PENDIENTE→EN_PROGRESO exactamente una ocurrencia. El HEAD puede cambiar por otros ejecutores; el commit de este manifiesto se publica exclusivamente contra el HEAD leído y con fast-forward/CAS.
- Esta unidad es el **inventario reproducible de evidencias existentes**, no un nuevo test runtime. La meta acumulativa de PLAN!E «Completa M06» no queda acreditada por este documento individual.
- Fuente de comparación y clasificación independiente: [auditoría R0 M06.01](../v5/c15-05-m06-01-auditoria-mocks-vs-reales.md), commit `06fa2a8390ac34ceaa52d2ea2584b060362e6437`, blob `c7017b7a75aa3d612908ba02a6adf54974c136c0`; su estado institucional pasó a LISTO mediante cierre cruzado DEV06 y todavía requiere A5 distinta del autor DEV03 y del rescatista DEV06.

## A1 — inventario causal de artefactos y resultados del lote M05

| Microunidad | Commit de evidencia | GitHub Actions dirigido | Resultado de alcance realmente probado |
| --- | --- | --- | --- |
| `C15-05.M05.01` | `61b9716e87001b3836b3bc499f8b216e1f0c3b6c` | [run 44208692359](https://github.com/solqaryn/Solqaryn/actions/runs/44208692359) | xUnit 10/10 de reglas, CAS e idempotencia **en memoria**, no candado distribuido |
| `C15-05.M05.02` | `0755055364237839d566f0fb4b455aeedf033ce9` | [run 44506693047](https://github.com/solqaryn/Solqaryn/actions/runs/44506693047) | xUnit 30/30, TestServer ASP.NET Core y JWT firmado para contratos de errores; proveedor de permisos de fixture, sin DB empresarial |
| `C15-05.M05.03` | `f4827013eb41321dc90da46749bc62e3e9c0f498` | [run 44211270980](https://github.com/solqaryn/Solqaryn/actions/runs/44211270980) | Node 7/7 con GET real al proveedor GitHub, sin autorización a escrituras de terceros |
| `C15-05.M05.04` | `ae1d0de5b5a94820d4ac8d7813d246f874ebb091` | [run 44209664751](https://github.com/solqaryn/Solqaryn/actions/runs/44209664751) | Chromium 3/3 en UI con sesión sintética; no prueba JWT/RBAC real |
| `C15-05.M05.05` | `43fd19b1a47e45e5c115e4f5706acba438b28fe9` | [run 44210318565](https://github.com/solqaryn/Solqaryn/actions/runs/44210318565) | xUnit 12/12 negativos de abuso sobre servicios en memoria |
| `C15-05.M05.06` | `88da235e7edb381a41fd91862643fc744d937ce5` | [run 44605758500](https://github.com/solqaryn/Solqaryn/actions/runs/44605758500) | Chromium 4/4 responsive/teclado/403 simulado, token sintético; no Web Vitals de campo |
| `C15-05.M06.01` | `06fa2a8390ac34ceaa52d2ea2584b060362e6437` | [run 44610700996](https://github.com/solqaryn/Solqaryn/actions/runs/44610700996) | auditoría de seis fuentes reales frente a mocks, sin runtime; LISTO atómico con A5 independiente pendiente |

Se citan resultados atribuidos a pruebas **de la unidad indicada**, conforme a PLAN/BITACORA y los jobs enlazados. No se declara éxito de workflows globales ajenos, ni se usa `SKIPPED` como `PASS`. Ninguna prueba con JWT o MySQL se extrapola a otras unidades.

## A2 — hashes de trazabilidad

- M05.01: prueba xUnit blob `a9da136f128094a8597a776e203f9da2a873198e`, workflow `b6a4baaf9e6de99f507cc044a15d35210cb5f2a2`, documento `ec12c0ab771ee71504f606703ef3084266ae7c82`.
- M05.04: prueba Chromium blob `5606437e4ed7ad7ff7fb440cde5eb7296a9339c5`, workflow `55df3bbe857bcef8f2f22c35d827be9a88b85c61`, documento `c849ea935ba469b467e40794e4c0675114263602`.
- M05.05: prueba xUnit blob `4abf3ee430a0728029a8c5c051c59540ea4c8a65`.
- M05.06: prueba Playwright blob `a14f300402c70cbbb5a7ff6ba17a15b4bff1ee85`, workflow `aea1d627afc7e9cc6cc4423ea43fba03993cf4f1`, documento `59b3072bc5bc9b9871146f2be5be2c95f7dbfc72`.
- M06.01: documento blob `c7017b7a75aa3d612908ba02a6adf54974c136c0`.

Estos hashes describen objetos fuente específicos, **no** prueban que no haya cambios posteriores; una reutilización posterior requiere comparar los blobs concretos contra el nuevo HEAD.

## A3 — reproducción segura por SHA

1. Abrir el commit fuente `https://github.com/solqaryn/Solqaryn/commit/<SOURCE_SHA>`, revisar diff y archivos. El SHA identifica la entrega, no sustituye sus pruebas.
2. Consultar `GET /repos/solqaryn/Solqaryn/commits/<SOURCE_SHA>/check-runs?per_page=100`; para cada check requerido confirmar `head_sha` exacto, `status=completed` y `conclusion=success`. Un `skipped`, `cancelled`, `queued`, `failure` o `in_progress` requerido **no** acredita PASS.
3. Abrir el job de GitHub Actions vinculado; comparar filtros/TRX/conteos, logs reales y alcance del test con PLAN!D. En pruebas con proveedor/HTTP/DB, verificar en logs si el proveedor y la BD fueron auténticos o sustituidos por fixture.
4. Releer PLAN H/I/K/L, BITACORA A:Z, COLA G/O y DAG F/G por ID exacto. No editar fórmulas ni presentar el objetivo acumulativo M06 como completado.
5. Para este changeset R0 comparar el diff (solamente este manifiesto y nueva sección autocontenida de CHANGELOG), el historial previo de CHANGELOG completo, Scope Lock y check V5 del SHA publicado. La publicación y el cierre de esta unidad deben conservar autoría y BITACORA idempotente.

**NO_APLICA (motivado):** nuevas pruebas HTTP, JWT firmado, MySQL 8.4 o Chromium para la modificación **R0 documental**, porque no se alteran APIs, datos, interfaz, middleware, tests ni workflow. Las lagunas R2 de la fase permanecen abiertas; este manifiesto no autoriza servicios productivos, QA/main/PROD, pagos, secretos, reemplazo de proveedor ni difusión de credenciales.

## A4 / A5 y rollback

- Gate de esta entrega: commit `dev` fast-forward, diff de dos archivos con historia preservada, Scope Lock y V5 **completed/success** en SHA publicado, checks de CI disparados y pertinentes revisados, CAS + readback PLAN/COLA/BITACORA y liberación del candado propio. Si CI todavía corre, conservar VALIDANDO con SHA/URL y esperar revisión posterior; no afirmar LISTO.
- A5 técnica **independiente** por otra Task del allowlist Javier, distinta del autor DEV06, sin autovalidación; registrar la revisión real tras cierre atómico.
- Rollback autorizado del changeset documental mediante `git revert` normal tras verificar HEAD vigente, sin `reset`, `force-push`, reescritura histórica ni afectar otros commits.
- Limitaciones institucionales conservadas: M06.01 A5 pendiente; no se certifica implementación distribuida, autorización runtime global, persistencia empresarial, seguridad/tenancy general, restauración real, fase M06 ni entrega productiva.
