# C14-04.M06.02 — Manifiesto reproducible de versiones, diffs, artefactos y resultados

**Corte institucional:** 2026-11-18, SOLQARYN `dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** R0, documentación y trazabilidad; sin efectos sobre runtime o clientes. Este manifiesto cumple la microtarea "Registrar versión, diff, artefactos y resultado reproducible" del bloque de preparación de salida C14-04.

## A0. Versiones exactas y procedencia

Los siete commits de la tabla son **identificadores del contenido bajo prueba**, no autorizaciones de despliegue. Cada diff se inspecciona exclusivamente en el commit original, sin inferir que pruebas de un SHA distinto siguen válidas por semejanza de nombres. `CHANGELOG_AI.md` es compartido y puede recibir inserciones posteriores; se preserva procedencia por sección identificada, mientras que archivos específicos deben mantener su blob SHA para invocar equivalencia en descendientes.

| Microtarea | SHA fuente y diff GitHub | Archivos de artefactos técnicos específicos del commit | CI dirigida confirmada y revisor independiente |
| --- | --- | --- | --- |
| C14-04.M05.01 | [70c4d32f](https://github.com/solqaryn/Solqaryn/commit/70c4d32f453ee2c6e21c2524a1eb5e3a03fea1e1) | `backend/tests/Solqaryn.Tests/Domain/C1404M0501ReleaseReadinessDomainBoundaryTests.cs`; `docs/v5/c14-04-m05-01-readiness-unit-regressions.md` | xUnit net10 Release 5576/5576 PASS, [job 135114563979](https://github.com/solqaryn/Solqaryn/actions/runs/44392781131/job/135114563979); A5 DEV06 |
| C14-04.M05.02 | [1e372957](https://github.com/solqaryn/Solqaryn/commit/1e3729573556ac1ae10dbad1399406ead2f9b17b) | `backend/tests/Solqaryn.Tests/Application/C1404M0502ReadinessApplicationContractRegressionTests.cs`; `docs/v5/c14-04-m05-02-application-dto-contract-regressions.md` | xUnit net10 Release 5618 PASS, job 135121403346; A5 DEV03 |
| C14-04.M05.03 | [a0675698](https://github.com/solqaryn/Solqaryn/commit/a06756982b0cb84f235a9b9f7252dab53ec03751) | `scripts/quality/v19-c14-04-m05-03-provider.mjs`; `scripts/quality/v19-c14-04-m05-03-provider.test.mjs`; `.github/workflows/v19-c14-04-m05-03-provider.yml`; `docs/v5/c14-04-m05-03-github-checks-provider-read-only.md` | Node 6/6 PASS, job 135131603764; A5 DEV02 |
| C14-04.M05.04 | [284e56d9](https://github.com/solqaryn/Solqaryn/commit/284e56d9e0f32de099e6ae7a8362e66e0a2d8aa0) | `frontend/e2e/c14-04-m05-04-readiness-acceptance.spec.ts`; `.github/workflows/v19-c14-04-m05-04-acceptance.yml`; `docs/v5/c14-04-m05-04-positive-acceptance-readiness.md` | Chromium 3/3 PASS, job 135115035681; A5 DEV02 |
| C14-04.M05.05 | [b06499f0](https://github.com/solqaryn/Solqaryn/commit/b06499f0ca101ec475dbf7fd1f662bbe69b20d5d) | `backend/tests/Solqaryn.Tests/Domain/C1404M0505ReleaseReadinessAbuseRegressionTests.cs`; `docs/v5/c14-04-m05-05-readiness-abuse-regressions.md` | xUnit net10 Release 5594 PASS, job 135117071596; A5 DEV06 |
| C14-04.M05.06 | [963fa8c4](https://github.com/solqaryn/Solqaryn/commit/963fa8c4e08300238f1b818fa7b33cb20ab1346c) | `frontend/e2e/c14-04-m05-06-regression-a11y.spec.ts`; `.github/workflows/v20-c14-04-m05-06-regression-a11y.yml`; `docs/v5/c14-04-m05-06-readiness-regression-a11y.md` | [Chromium 4/4 PASS](https://github.com/solqaryn/Solqaryn/actions/runs/44422167207/job/135217636831), npm lint/build; A5 DEV06 |
| C14-04.M06.01 (auditoría previa) | [8f28c762](https://github.com/solqaryn/Solqaryn/commit/8f28c76289c61c26154b2a7354db348c8cee0207) | `docs/v5/c14-04-m06-01-auditoria-evidencia-vs-mocks.md` y sección autocontenida `CHANGELOG_AI.md` | [Scope Lock PASS](https://github.com/solqaryn/Solqaryn/actions/runs/44424709068/job/135226428629) y [procedencia V5 PASS](https://github.com/solqaryn/Solqaryn/actions/runs/44424708919/job/135226427555); A5 M06.01 **PENDIENTE**, no se inventa |

La tabla expresa resultados CI observados en las fuentes originales y PLAN_MAESTRO/K con revisión independiente, y **NO afirma** haber reejecutado las suites completas durante la confección de este manifiesto.

## A1. Reproducción segura y resultados verificables

Secuencia válida para comprobar cualquier entrada sin conceder permisos:

1. Abrir su enlace `commit/<SHA>`, enumerar `files[]`, `blob SHA`, parent, diff y el job enlazado; probar `head_sha=<SHA>` en `/repos/solqaryn/Solqaryn/commits/<SHA>/check-runs?per_page=100`.
2. Exigir `status=completed` y `conclusion=success` del check **pertinente** y sus logs con prueba ejecutada; `SKIPPED`, `CANCELLED`, `IN_PROGRESS`, `QUEUED` y un build genérico no sustituyen el test requerido.
3. Para net10, reproducir en un checkout efímero de ese SHA con `dotnet restore backend/Solqaryn.sln` seguido por `dotnet test backend/Solqaryn.sln --configuration Release --filter "FullyQualifiedName~C1404M0501"` (ajustar al caso M05.02 o M05.05); el filtro es **instrucción propuesta de reproducción**, no prueba ejecutada aquí.
4. Para el proveedor Node, aislar el checkout del SHA M05.03 y ejecutar su workflow oficial `v19-c14-04-m05-03-provider.yml`, con resultados, permisos y logs de proveedor reales (sin token de producción en cliente).
5. Para UI, checkout de SHA M05.04/06, instalar dependencias frontend con `npm ci`, `npm run lint`, `npm run build:prod` y ejecutar el archivo Playwright correspondiente contra Angular local mediante los workflows oficiales. Chromium es real; la sesión HTTP está interceptada y **no** valida JWT firmado.
6. Comparar SHA exacto; si se propone un descendiente, cotejar blobs de archivos propios, dependencias y workflow para demostrar equivalencia antes de transferir evidencia. No reutilizar un PASS cuando cambió el comportamiento.

**Caso positivo verificable:** el job Playwright M05.06 es completed/success y su log muestra 4 pruebas reales, incluyendo Tab/Enter, viewport y reduced motion. **Casos negativos:** un mock de sesión no es autenticación real; un archivo `README` no prueba rollback; resultado CI `skipped` no es PASS; n=4 en humo no es p95/p99; pérdida de blob equivalence obliga a revalidar.

## A2/A3. Resultado delimitado y brechas R2/R3 explícitas

| Evidencia | Estado reproducible para DEV | Interpretación prohibida |
| --- | --- | --- |
| Builds net10 de pruebas M05.01/02/05 | PASS en sus SHA causales | No acredita JWT HTTP firmado, DB tenant A/B ni despliegue |
| Node proveedor read-only M05.03 | PASS 6/6 en job dedicado | No acredita resultado SUCCESS de cada required check de un release productivo |
| Chromium local M05.04/06 | PASS 3/3 y 4/4 | No acredita permisos JWT reales, WCAG global, p95/p99 ni go-live |
| REST con JWT real, RBAC y tenant A/B con aislamiento MySQL | **SIN_VERIFICAR** para este lote | Nunca simular mediante `page.route` o in-process |
| Backups/restauración, alertas reales, soporte, RPO/RTO y SLO de producción | **NO_MEDIDO/SIN_VERIFICAR** | Nunca convertir smoke local o mocks en certificación |
| Promoción a QA/main/PROD, dominios, migraciones, recursos y credenciales | **NO_GO R3** sin autorización nueva | A5 de un documento no concede autorización productiva |

Como el diff de M06.02 sólo añade este manifiesto y entrada CHANGELOG, nuevas pruebas HTTP/MySQL/browser son **NO_APLICA al diff R0**; seguirán siendo requisitos pendientes de las fases que sí pretendan operar esas integraciones.

## A4/A5. Publicación, readback y rollback

Verificar para el SHA de ESTE manifiesto `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` en `completed/success`; no atribuir PASS antes. PLAN H/I, BITACORA 26 columnas, V17 exclusión y readback son obligatorios antes de LISTO_DEV. El autor DEV06 no puede ejecutar su propia A5. M06.06 seguirá sujeto a A5 genuinas de M06.01–05 y DAG calculado, sin forzar fórmulas.

Reversión documental: `git revert` normal del commit específico después de revisar HEAD y otros escritores; nunca force-push ni borrar BITACORA. Sin operación sobre bases de datos productivas.