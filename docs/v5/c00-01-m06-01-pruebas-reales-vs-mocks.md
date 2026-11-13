# SOLQARYN V5 FINAL — C00-01.M06.01: correspondencia entre pruebas y alcance real

**Tipo:** 1, auditoría documental de pruebas existentes; no modifica código, infraestructura, datos, migraciones ni secretos.  
**Responsable operativo:** PRIMARY DEV 06 — JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`, GitHub `jmejia31`. La atribución identifica la ejecución, no representa firma criptográfica.  
**Fuente:** [Plan V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit).  
**Baseline inspeccionado:** `14ea2e238cffa33bb93af38412983918de306cc5`. Se debe revalidar SHA para toda certificación posterior.  
**Resultado exigido:** revisar si las pruebas realmente comprueban la superficie afectada y no sustituir comportamientos live por mocks.

## Clasificación y límites de las pruebas

| Dimensión | Requiere ejecución técnica NUEVA para este documento | Decisión causal |
| --- | --- | --- |
| dotnet | NO | La auditoría inspecciona código, jobs y evidencia .NET ya ejecutados; no cambia C# |
| node / Angular | NO | No cambia TypeScript/CSS; examina Playwright y compilaciones previas |
| MySQL | NO | No introduce migración ni consulta nueva; constata MySQL efímero existente |
| Docker | NO | No modifica imágenes, contenedores ni despliegue |
| E2E | NO | Se examinan tests y logs reales, sin alegar ejecución nueva |
| Seguridad | SÍ, documental | Delimitar autenticación, tenancy, evidencias negativas y omisiones |
| Scope Lock | SÍ | El commit documental debe obtener `verify-solqaryn-project-scope=SUCCESS` en su SHA exacto |

**Prerrequisito conservador:** `C00-01.M05.01` a `M05.06` constaban LISTO en PLAN al claim; `COLA!G=SI` y `COLA!O=SI`, sin titular anterior de M06.01. No hay fila explícita M06.01 en DAG_TECNICO, por lo que no se interpreta como habilitación automática de otra fase.

## Matriz de evidencia: positivo, negativo, límites y sustituciones no válidas

| Caso | Evidencia verificable | Qué prueba | Qué NO prueba |
| --- | --- | --- | --- |
| Login/anónimo/credenciales inválidas | `frontend/e2e/c00-01-m04-04-login-flujo.spec.ts` blob `c70107aa3edd4afc497e53344b6ce7e23201098d`; [M10 job 132017609415](https://github.com/solqaryn/Solqaryn/actions/runs/43463854111/job/132017609415) | `page.goto`, POST `/auth/login` realmente continuado hacia API, respuesta HTTP >=400, errores accesibles y protección doble submit; ruta privada redirige al login | No equivale a prueba transversal de todos los roles o dos empresas mutuamente aisladas |
| Playwright M10 UI/WCAG | `frontend/e2e/m10-ui-ux-accesibilidad.spec.ts` blob `ce3d50c53b5de8447fa9eba09208160657415fa3`; mismo job M10 | Navegación teclado, foco, contraste, 320/390 px, prefers-reduced-motion, controles visibles y condiciones de UI reales en Chromium | El caso dashboard error/vacío usa `page.route('**/dashboard/resumen*')` con `route.fulfill`: valida presentación y recuperación UI, **no** error real backend ni seguridad de su payload |
| Kardex paginado y error | `frontend/e2e/n1-5-kardex.spec.ts` blob `feaf7d091e6afa940408f4de3caf70b41c5d805c`; mismo job M10 | Solicitudes reales paginadas al API con HTTP 200 y rango inválido HTTP 400, filtros y anchura móvil | No representa carga masiva, volumen productivo o concurrencia multinodo |
| M10 backend + DB | Workflow `.github/workflows/m10-ui-ux-accesibilidad.yml` blob `e81e4e429d322b0127f3027c4d63b165ee47ffbf`; [run 43463854111](https://github.com/solqaryn/Solqaryn/actions/runs/43463854111) | Servicio `mysql:8.4`, bootstrap EF10 Oracle, API health/ready, membresía tenant E2E creada, Angular e integración Chromium; log del job: **12 passed (1.4m)** | No prueba migraciones sobre Aiven/PROD, restauración productiva, ni permisos de todos los roles de tenant A/B |
| Seguridad xUnit | [Job seguridad 132017600479](https://github.com/solqaryn/Solqaryn/actions/runs/43463853912/job/132017600479) | Logs del SHA exacto: **1.109 passed, 0 failed, 0 skipped**, filtro `Tenant|Authorization|Security|Isolation` con `Category!=Integration` | El filtro excluye categoría Integration; no reemplaza una prueba MySQL de aislamiento entre empresas |
| Build y gobernanza | [Backend 132017600542](https://github.com/solqaryn/Solqaryn/actions/runs/43463853912/job/132017600542); [Frontend 132017600131](https://github.com/solqaryn/Solqaryn/actions/runs/43463853912/job/132017600131); [Scope Lock 132017600887](https://github.com/solqaryn/Solqaryn/actions/runs/43463854267/job/132017600887) | Jobs `completed/success`, `head_sha=14ea2e23...`; certifican las comprobaciones realmente incluidas | El auxiliar de retry `SKIPPED` no es un PASS de prueba; estos jobs no validan por sí solos criterios QA/PROD |

## Análisis causal de mocks y falsos positivos

1. **PASS real de integración limitada:** el job M10 del SHA base levanta MySQL 8.4 efímero y API real; el login usa `route.continue()`, y Kardex observa respuestas HTTP. No son meros mocks.
2. **PASS dirigido de UI solamente:** `route.fulfill` para estados error/vacío del dashboard demuestra reacción Angular ante respuestas sintéticas, pero **no** demuestra fallo/recuperación del servidor ni ausencia de fugas con datos reales. No elevar este caso a garantía HTTP ni tenancy.
3. **PASS unitario no es aceptación live:** la prueba de callback tardío del shell Angular (M05.01) usa Subjects RxJS controlados. Su regresión es útil para orden de emisiones, sin verificar interacción remota ni tiempos productivos.
4. **Seguridad diferenciada:** 1.109 pruebas xUnit de seguridad aprobadas; no inferir cobertura de DB/A-B a partir de suites `Category!=Integration`. M10 cubre sesión y membresía E2E, no todo RBAC interempresa.
5. **Rendimiento acotado:** los umbrales F11 provenientes del runner y de datos de prueba no demuestran SLO/SLA productivos de Vercel/Render/Aiven.
6. **Persistencia/rollback:** M06.04 tiene ensayo de backup/restore en MySQL aislado y guard negativo shell. No se presenta como fault injection MySQL o restauración de PROD.
7. **Gobernanza:** BITACORA tiene **A:Z (26 campos)**; `EVENT_ID` y `IDEMPOTENCY_KEY` únicos. Si no se obtiene RUN_ID nativo verificable, se conserva vacío.
8. **No cierre de fase:** esta auditoría M06.01 no libera M06.06, fases siguientes ni QA/main/PROD, ni sustituye los A5 independientes de SHA ajenos.

## Decisión y criterios de aceptación

**Resultado de M06.01:** matriz de correspondencia auditada, con límites explícitos entre CI real y mocks. Los jobs citados están completados en SUCCESS para el SHA BASE, pero la publicación documental necesita su propio Scope Lock SUCCESS del **SHA nuevo**. Una regresión de seguridad o de Scope Lock en ese SHA bloquea cierre, aunque otros jobs sean ajenos.

**A0:** verificar el diff exclusivamente Markdown/CHANGELOG, secretos y cambios no intencionales. **A1:** verificar correspondencia positiva/negativa entre cada fila y test/log real. **A2:** conservar fronteras JWT/RBAC/tenancy, falsas inferencias y prohibición de promociones. **A3:** comprobar contratos de fuente / workflows / artefactos y que no hay runtime modificado; pruebas tecnológicas nuevas NO_APLICA. **A4:** verificar commit directo FF exacto, Scope Lock de SHA, PLAN/COLA/BITACORA readback, rollback por `git revert` normal. **A5:** PENDIENTE de Task distinta; no autoaprobar.

**Siguiente acción de producto:** pruebas negativas cross-tenant con backend/MySQL real sólo bajo la microtarea ejecutable correspondiente; no se autoriza cambiar código para satisfacer este documento.

**Rollback:** revertir exclusivamente el commit documental mediante `git revert <SHA>` normal, sin force-push ni alterar archivos de otros autores.
