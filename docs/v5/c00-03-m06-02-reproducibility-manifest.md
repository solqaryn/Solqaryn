# C00-03.M06.02 — Manifiesto reproducible de evidencia de cobertura V5

**Resultado exigido:** registrar versión, SHA, diff, árbol Git, artefacto, prueba real y pasos de reproducción de las seis entregas C00-03.M05.01–06. La revisión es documental y no inventa resultados nuevos de .NET/Angular/MySQL/JWT. Preflight: seis predecesoras en LISTO, DAG F=VALIDADO/G=SI, COLA G/O=SI, PLAN sin owner ajeno, rama dev SHA base `5880fd30bddbf09c0f550e1320de1a2ad7212ae2`.

## Matriz de origen verificable (repositorio único solqaryn/Solqaryn)

| ID | SHA de publicación / árbol Git | Test exacto y blob | Resultado y límite |
| --- | --- | --- | --- |
| M05.01 | `f22e544177d1017cd894c7984aec0edd70eb14df` / `acc1c84cca87d77722ab288854b7f2672c27c1c2` | `backend/tests/Solqaryn.Tests/Application/V5CoverageM05RuleRegressionTests.cs` / `8d4f5978b5b585ced2530173a33d960b84331cfb` | Reglas puras positivas/negativas, duplicados, SHA obsoleto, evidence ausente; CI backend/seguridad/Scope Lock SUCCESS exact-SHA; no evidencia live |
| M05.02 | `8d6e12e88922140eca880f3f0d754a8d07dcbbbb` / `02ab87d8cbca3ee1c2a1544d6aa088bdd6637f16` | `backend/tests/Solqaryn.Tests/API/V5C0003M0502CoverageHttpContractTests.cs` / `c91d8c87034d7838e5f36afc1ff11850afcf48b7` | TestServer HTTP 200 y negativos HTTP 400 con DTO; backend/seguridad/auth/Scope Lock SUCCESS, no JWT/endpoint institucional live |
| M05.03 | `f320739bb1c57c8872c49c8bb13cd38d4be89e75` / `a2dcbe7106e0312523acaa3bb7ed0ece39e794ab` | `backend/tests/Solqaryn.Tests/Application/V5C0003M0503ProviderIntegrationTests.cs` / `219c5375a9e338f1ceb7c5c77115de9de1523058` | Tres xUnit adapter/proyección in-memory, éxito controlado, negativo cross-scope y revisión obsoleta; backend 3574 y security/auth rerun SUCCESS exact-SHA; originales CANCELLED no PASS; sin MySQL/CAS Sheets |
| M05.04 | `eced2da14dc3c812e137f1837e007ea6c22108c9` / `0c101bcfbbd18ff0288d3504900dc8e83b8223f3` | `frontend/e2e/c00-03-m04-05-accessibility.spec.ts` / `e16a8ea375e12723a65786930dd3c4f73f69e63a` | Chromium/UI sesión simulada, tabla, 403, foco, contraste; frontend/Chromium/Scope Lock SUCCESS; no JWT real |
| M05.05 | `1f04379da266451f54dd88669e3178481a88f7d8` / `440fe89692c2b265f81aa37a343d811f86201701` | `backend/tests/Solqaryn.Tests/API/V5C0003M0505CoverageIsolationTests.cs` / `ad4a070f7f6fbc009ebd2e70cbd5329fe50908b5` | Positivo y negativos actor/scope/tenant en fixtures, backend/seguridad/auth/Scope Lock SUCCESS; no MySQL ni JWT de producción |
| M05.06 | `fe73c45f5ec1d4f8c6dfa2a69f000725898dc36a` / `5afae78ce9fd221c18cf0607af04302fab1963a1` | `frontend/e2e/c00-03-m04-06-density.spec.ts` / `fd3756ee305e9dec02108a96628b3bf4e6145614` | Chromium real, viewports 390/1280, tiempo runner, foco, revalidación de sesión sin escrituras, frontend/Scope Lock SUCCESS; no SLA/JWT live |

**Rutas causales corregidas:** el informe documental M06.01 anterior confundió M05.04 y M05.06 con el spec genérico `frontend/e2e/v5-plataforma-browser-guards.spec.ts`. Los commits originales no modificaron ese spec. Este changeset corrige M06.01 con las rutas reales `c00-03-m04-05-accessibility.spec.ts` y `c00-03-m04-06-density.spec.ts`; se preserva el SHA anterior y la BITACORA histórica, sin atribuir un PASS retrospectivo a la cita errónea. A5 independiente sigue pendiente.

## Reproducción sin falsas certificaciones

1. Verificar `GET https://api.github.com/repos/solqaryn/Solqaryn/commits/<SHA>` para cada línea. Comprobar `commit.tree.sha`, `files[].filename`, `files[].sha` con esta tabla; si el HEAD cambió, repetir lectura.
2. Consultar `GET /repos/solqaryn/Solqaryn/commits/<SHA>/check-runs?per_page=100`; solo `head_sha` exacto y `status=completed/conclusion=success` acredita un job requerido. `CANCELLED`, `SKIPPED`, `IN_PROGRESS` nunca son PASS. En M05.03, security/auth SUCCESS procede del rerun selectivo exact-SHA, no de los originales cancelados.
3. Con checkout/runner autorizado, `git show <SHA>:<RUTA>` reproduce el blob; `dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj` o `npx playwright test` solo cuando se necesite reejecución real y desde el directorio/config correcta. **Comandos de referencia, no ejecutados en esta auditoría**.
4. Releer `PLAN_MAESTRO!H/I`, `COLA!E/G/O` y `BITACORA!A:Z`; conservar autor por microtarea, claves EVENT_ID/IDEMPOTENCY_KEY únicas y readback postcommit. El manifiesto no crea dispatcher, lease, tasks, PR ni ramas.

## Clasificación, seguridad, A0–A5 y rollback

Tipo 1 documental: `requiere_dotnet=NO_APLICA`, `requiere_node_angular=NO_APLICA`, `requiere_mysql=NO_APLICA`, `requiere_docker=NO_APLICA`, `requiere_e2e=NO_APLICA` para el **diff nuevo**; `requiere_security=SI` (riesgo de sobrecertificar tenant/JWT/RBAC), `requiere_scope_lock=SI` (origen y gobernanza). A0 verifica diff y blobs/corrección sin secretos; A1 compara positivo/negativos y rutas reales; A2 conserva ausencia de garantías institucionales live; A3 verifica consumidores y seis fuentes; A4 requiere commit fast-forward, Scope Lock/V5 Changelog SUCCESS exact-SHA y readback PLAN/COLA/BITACORA. A5_PENDIENTE a Task Javier distinta. Quedan NO_CERTIFICADOS CAS institucional Sheets, JWT/RBAC real, MySQL durable, aislamiento entre empresas persistido, QA/main/PROD. No libera M06.06 ni saltos de DAG.

Rollback normal por `git revert` del commit documental, sin force-push ni borrar BITACORA o citas históricas; cualquier corrección posterior requiere nuevo SHA y auditoría independiente.
