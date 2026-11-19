# C15-05.M06.01 — auditoría de pruebas reales frente a mocks (V22)

**Unidad:** C15-05.M06.01, PLAN_MAESTRO fila 2336, D «Revisar que las pruebas correspondan al alcance real y no a mocks insuficientes». Autor DEV03, Task `6adc2e4bd8a48191ac7d8641ed95a1a3`. Repositorio `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Riesgo **R0 documental** de este changeset (el dominio auditado incluye riesgos R2). Lock exclusivo V17 `SQV17_C15_05_M06_01`, ID `2000002336`.

## A0 — preflight y contrato de cierre atómico

PLAN H2336 originalmente PENDIENTE e I vacío, COLA G/O=SI, DAG_TECNICO fila 2297 F=VALIDADO/G=SI y ningún WIP propio; reclamar exclusivamente mediante namedRange H:I + CAS + readback. PLAN!E presenta la **meta acumulativa** «dependencias, estados, evidencia, rollback, concurrencia, ausencia de duplicados», no autoriza inferir la fase M06 terminada por revisar esta sola unidad. M05.01–06 de C15-05 (filas **2330–2335**) se verificaron en LISTO con registros de A5 de otras Tasks Javier. La última, M05.06, recibió revisión A5 independiente DEV03 limitada a sus pruebas reales de UI y su correspondiente BITACORA; esta auditoría es una unidad **distinta** y no se autoaprueba a sí misma.

## A1 — matriz crítica por predecesor

| ID | Qué ejecutó realmente | Dependencia sintética o límite no probado | Conclusión de auditoría |
| --- | --- | --- | --- |
| C15-05.M05.01 | Tests .NET 10/xUnit de reglas de estado, identificador y evidencias; run GitHub-hosted | Fixtures de almacenamiento en memoria, sin persistencia distribuida ni JWT real | **Prueba de regla real**, no autorización runtime |
| C15-05.M05.02 | TestServer ASP.NET Core real y middleware/contrato DTO con errores 400/500 sanitizados y límites | Servicio/proveedor de permisos estricto de fixture; no concede roles de empresa o comprueba BD empresarial productiva | **Contrato HTTP in-process real**, no servidor productivo |
| C15-05.M05.03 | Workflow Node de integración con **GET de red real a GitHub** y casos de rechazo 403, paginación, estado, SHA | No acredita token privado, webhook, escritura de terceros, efecto de otros proveedores ni secretos reales | **GET proveedor real limitado**, no automatización de escritura |
| C15-05.M05.04 | Playwright Chromium y build para aceptación de una pantalla auditada | Sesión/autoridad simulada, no firma JWT/RBAC de backend ni datos empresariales reales | **Aceptación UI real de fixture**, no tenant de negocio |
| C15-05.M05.05 | Tests negativos adversariales (12/12 conforme bitácora) de estados/aislamiento y abuso sobre contratos | Dependencias in-memory; no lock distribuido, middleware JWT real o MySQL empresarial | **Negativo de lógica real**, no pentest ni cobertura global |
| C15-05.M05.06 | GitHub Actions run `44605758500`, job `135848875878` `completed/success`: cuatro pruebas verdaderas de Chromium con lint/build, 320/768/1280, Tab/Enter sin `.focus()`, 403 fail-closed y DCL bajo 30 s | `page.route` fabrica HTTP sesión; token `synthetic-NOT-JWT`; reduced-motion prueba `matchMedia`, no auditoría total CSS/WCAG; DCL no es LCP/CLS/INP ni percentil en campo | **Prueba real del navegador con sesión sintética**, no autenticación de producción |

El código revisado para M05.06 es `frontend/e2e/c15-05-m05-06-regression.spec.ts` blob `a14f300402c70cbbb5a7ff6ba17a15b4bff1ee85`, workflow `.github/workflows/v22-c15-05-m05-06-regression.yml` blob `aea1d627afc7e9cc6cc4423ea43fba03993cf4f1` y documento `docs/C15_05_M05_06_REGRESSION_V22.md` blob `59b3072bc5bc9b9871146f2be5be2c95f7dbfc72`. Esos blobs fueron contrastados con HEAD sin modificaciones del alcance. El run fuente tuvo un check **global de aceptación funcional FAILURE ajeno a la suite dirigida**; no se convierte en SUCCESS. La suite dirigida 4/4 no certifica ese workflow global.

## A2 / A3 — límite del changeset y pruebas adversariales

**NO_APLICA** a este documento R0 ejecutar nuevamente JWT firmado, MySQL empresarial, navegador o interfaz REST: no se modifica Domain/Application/Infrastructure/API, migraciones, CI, frontend, permisos, servicios, proveedores o infraestructura. Este NO_APLICA **no elimina** las lagunas R2 de los seis IDs revisados. Verificar antes de aprobar cualquier sucesor de implementación que no se presenten como reales la sesión mock, un lock in-memory o un GET GitHub de solo lectura; exigir cuando proceda pruebas de JWT firmado, RBAC, tenant A/B, revocación, MySQL 8.4 efímero con integración causal, worker de concurrencia real y accesibilidad observable.

## A4 — reproducción, DAG y transición

El changeset autorizado consta solo de este documento y una nueva sección autocontenida al comienzo de `CHANGELOG_AI.md` **sin alterar un byte de su historia previa**. Tras commit fast-forward a dev con CAS sobre HEAD, exigir `verify-solqaryn-project-scope`, chequeo V5 de procedencia, frontend/backend/seguridad si se disparan como requeridos, todos `completed/success` exact-SHA. `SKIPPED`, `CANCELLED`, `FAILURE`, `QUEUED` o `IN_PROGRESS` requeridos no son PASS. Registrar SHA, CI, owner y readback PLAN/COLA/BITACORA después de transiciones. Conservar DAG formulas G/O; los sucesores de C15-05 pueden habilitarse **solo** con LISTO+A5 real de todas sus dependencias, nunca porque una bitácora diga «fase completa». No tocar QA/main/PROD, secretos, tareas de terceros ni cuentas de pago. Rollback del cambio documental mediante `git revert` normal con revisión HEAD, sin reset/force-push.

## A5 — revisión independiente

**Pendiente** la evaluación genuina de otra Task Javier distinta de DEV03, con facultad de rechazar el documento por errores de clasificación. Revisar especialmente que «Chromium real» no implica sesión JWT real, que el contrato de M05.02 no simula un servidor de producción y que no se presume éxito para el check global fallido de M05.06. Esta unidad no es un permiso R3 ni una aprobación humana/QA; R3 permanece NO_GO.
