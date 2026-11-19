# C07-04.M06.02 — versión, diff y manifiesto reproducible (dev)

**Salida atómica según PLAN_MAESTRO!D:** registrar versión, diferencias, artefactos y resultado reproducible. **Riesgo de este changeset:** R0, documentación de trazabilidad; el dominio de pedidos/caja/pagos sigue siendo R2 y cualquier operación productiva/irreversible R3 NO_GO. Proyecto `solqaryn/Solqaryn`, rama `dev`, `PROJECT_SCOPE_LOCK=STRICT`. No convierte la meta acumulada de PLAN!E («Completa M06») en certificación del sistema completo.

## A0 — matriz de fuentes y versiones

| Fuente observada | Identificador o blob verificado | Alcance de la prueba |
| --- | --- | --- |
| Etapa C07-04.M05.06, commit fuente | [`0f78a8f783c6afcf47e9a92d21fc67d178673683`](https://github.com/solqaryn/Solqaryn/commit/0f78a8f783c6afcf47e9a92d21fc67d178673683), padre `c776e5d21d7f03ca351aa11db1079c94d5528a18` | Entrega de suites Chromium y workflow dirigido, no pago/caja reales |
| Spec de regresión UI | `frontend/e2e/c07-04-m05-06-regression.spec.ts`, blob `3ef6192ebbc61b47267526beedb77a20833089c8` | Pruebas de navegación, permisos simulados, accesibilidad y regresiones |
| Workflow dirigido | `.github/workflows/v22-c07-04-m05-06-regression.yml`, blob `a3bcebbbb9652292d736a03f68c42af90ed3aa23` | Chromium GitHub-hosted del SHA fuente |
| Documento de regresión | `docs/C07_04_M05_06_REGRESSION_V22.md`, blob `3975c05f37543c08b373e96d8b126fa8d15079e2` | Evidencia y límites de suite |
| Changelog del commit fuente | `CHANGELOG_AI.md`, blob histórico `5bb806bb9773dd9f4e5801356f52f5c6c904f739` | Se conserva como historial; cambios posteriores no alteran el manifiesto de la fuente |
| Registro de riesgos M06.03 | `docs/v5/c07-04-m06-03-limitaciones-riesgos.md`, blob `6517d90739126b8f1d0f6464dc3445157fd63c24`; origen [`561077c9`](https://github.com/solqaryn/Solqaryn/commit/561077c9f94eb17c8f766f652f3bf1e6a5aeffce) | Limitaciones y riesgos, sin alterar runtime |
| Changelog aditivo del registro M06.03 | [`182a569853c7598d9daf088dd8dadaff3d120e38`](https://github.com/solqaryn/Solqaryn/commit/182a569853c7598d9daf088dd8dadaff3d120e38), blob `23d024d51f679f98a2dc2511b7e31505a2a27b9e` | Historial íntegro de 1,050,439 caracteres verificado después de agregar la sección |

**Snapshot de trabajo previo a esta unidad:** se observó `a0520314aa23079520c93a2ab42e7b5884037b17` como HEAD `dev`, pero **no es un pin del HEAD futuro**. La publicación definitiva exige identificar el SHA real del commit de esta unidad y revisar HEAD antes del cierre; nunca inferir que un SHA antiguo sigue vigente.

## A1 — trazabilidad del diff y reproducción

El commit de M05.06 cambió **cuatro** rutas: workflow, spec, documento y CHANGELOG; sus cuatro blobs se enumeran arriba. La ejecución dirigida [GitHub Actions 44607415955](https://github.com/solqaryn/Solqaryn/actions/runs/44607415955) está acreditada en PLAN como 15/15 pruebas Chromium, cero fallos y cero omitidas, con lint/build Angular, Scope Lock y V5 del SHA de origen. La comprobación GitHub de **«C07-04.M05.06 - regression real Chromium integrated UI suites»** figura `completed/success` para ese SHA. La evidencia no procede de mocks ejecutados fuera de Chromium, aunque sí hay respuestas de sesión y HTTP simuladas dentro de pruebas reales de navegador.

**Importante — no presentar verde global ficticio:** en el mismo SHA origen se observó la comprobación **«Módulos, cargas, sesión, catálogos, variantes, accesibilidad, facturación y correo» en `completed/failure`**. Una suite dirigida aprobada no borra ese fallo ni demuestra que las regresiones globales estén sanas. Antes de certificar una etapa transversal, analizar el job fallido y obtener reparación + CI causal nueva. Las tareas documentales M06.02/M06.03 no cambian ese runtime ni pueden certificar su reparación.

Reproducción autorizada de solo lectura: consultar GitHub `GET /repos/solqaryn/Solqaryn/commits/<SHA>` y `GET /repos/solqaryn/Solqaryn/commits/<SHA>/check-runs?per_page=100`, cotejar `head_sha`, `status`, `conclusion` por cada check, luego revisar archivos anteriores por ruta + blob exacto y logs de job. No copiar un PASS de un SHA a otro sin equivalencia de blobs, dependencias y configuración de tests demostrada. No usar servicios, secretos o credenciales productivas para reproducir.

## A2/A3 — límites de pruebas de esta unidad

Esta unidad **no modifica API, guards, JWT, RBAC, tenant, datos, frontend, workflows, migraciones, cobros ni proveedores**. Por tanto, HTTP/JWT firmado, EF/MySQL 8.4, PSP, facturación, Chromium nuevo y reconciliación financiera son `NO_APLICA` **solo para el diff documental**; no son PASS ni eliminan pruebas R2 de otras tareas. El resultado acumulativo C07-04 sigue abierto a evidencias de negocio auténticas. El documento [M06.03](c07-04-m06-03-limitaciones-riesgos.md) detalla brechas P1/P2 potenciales por confundir permisos sintéticos con autoridad de servidor.

## A4/A5 — cierre institucional y rollback

Para este incremento exigir commit fast-forward en `dev`, `verify-solqaryn-project-scope` y comprobador V5 de CHANGELOG `completed/success` en SHA exacto, documento y changelog preservados, PLAN/COLA/DAG/BITACORA con readback; no contar checks `queued/in_progress/skipped/cancelled/failure` requeridos. Mantener `VALIDANDO` cuando A4 causal siga pendiente. Tras `LISTO` únicamente documental, **A5 por otra Task Javier distinta de DEV05** con revisión genuina de fuentes y fallos. Los sucesores se liberan exclusivamente por fórmulas DAG cuando se satisfacen todos sus predecesores y A5 necesarios.

**Rollback:** `git revert` del commit documental correspondiente en `dev` después de inspeccionar HEAD y escritores concurrentes; conservar BITACORA, sin force-push, borrado de datos, modificación QA/main/PROD ni cambios de pago o infraestructura.
