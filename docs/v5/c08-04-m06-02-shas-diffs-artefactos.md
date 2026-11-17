# C08-04.M06.02 — Versiones, diffs y artefactos reproducibles del lote M05

**Autor:** PRIMARY DEV 03, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Repositorio:** `solqaryn/Solqaryn`; **rama:** `dev`; **PROJECT_SCOPE_LOCK:** `STRICT`. **Clasificación:** R0, exclusivamente documental. No escribir sobre implementación ajena o reejecutar tests sintéticos como si fueran integración productiva.

## A0 — Fuente institucional y versiones

Consulta autenticada de `PLAN_MAESTRO!A1286:M1291`, seis unidades M05 en LISTO, con responsables reales preservados. Para cada SHA origen se abrió el commit GitHub (mensaje, diff y lista de rutas) y `GET /repos/solqaryn/Solqaryn/commits/<SHA>/check-runs?per_page=100` y se contrastaron **todos** los checks reportados; `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` constan `completed/success` en cada SHA. Fecha de consulta: 16 de noviembre de 2026, America/Tegucigalpa.

| ID | SHA Git fuente | Diff / artefactos verificables | Checks GitHub Actions en SHA |
| --- | --- | --- | --- |
| `C08-04.M05.01` | [`9aeadac6fc`](https://github.com/solqaryn/Solqaryn/commit/9aeadac6fc9dc43f21f6b5fb695f537abe26aba0) | test TS reglas de capacidad + workflow + doc + CHANGELOG; `.github/workflows/v18-c08-04-m05-01-capabilities.yml` | 22/24 check-runs `completed/success`; 2 `skipped` (NO PASS) |
| `C08-04.M05.02` | [`e084a06a8f`](https://github.com/solqaryn/Solqaryn/commit/e084a06a8f66bc7d528fe7d4834a6991c085ac96) | gate Application .NET, xUnit DTO/errores + workflow + doc + CHANGELOG; `.github/workflows/v18-c08-04-m05-02-contract.yml` | 22/27 check-runs `completed/success`; 5 `skipped` (NO PASS) |
| `C08-04.M05.03` | [`7bcdf84a8a`](https://github.com/solqaryn/Solqaryn/commit/7bcdf84a8ae60a2c274c0ccbb37828439ddef629) | xUnit adaptador y fronteras + workflow + doc + CHANGELOG; `.github/workflows/v18-c08-04-m05-03-cross-layer.yml` | 19/20 check-runs `completed/success`; 1 `skipped` (NO PASS) |
| `C08-04.M05.04` | [`979b0229ea`](https://github.com/solqaryn/Solqaryn/commit/979b0229ea475c3c98f8b5ac4969b17318ab5755) | Playwright aceptación positiva + workflow + doc + CHANGELOG; `.github/workflows/v18-c08-04-m05-04-positive.yml` | 15/16 check-runs `completed/success`; 1 `skipped` (NO PASS) |
| `C08-04.M05.05` | [`3a5aa69c38`](https://github.com/solqaryn/Solqaryn/commit/3a5aa69c38b716336da150650fdf881ebb5320f8) | Playwright rechazo sectorial + workflow + doc + CHANGELOG; `.github/workflows/v18-c08-04-m05-05-negative.yml` | 15/16 check-runs `completed/success`; 1 `skipped` (NO PASS) |
| `C08-04.M05.06` | [`2239d9a85e`](https://github.com/solqaryn/Solqaryn/commit/2239d9a85e9846c8ba5f95f3eb2c955c91e07efb) | Playwright accesibilidad/regresión + workflow + doc + CHANGELOG; `.github/workflows/v18-c08-04-m05-06-regression.yml` | 15/17 check-runs `completed/success`; 2 `skipped` (NO PASS) |

Los nombres y SHA son identificadores de **entregas individuales**: no representan el estado de una rama remota mutable. Las cantidades se refieren a check-runs detectados en cada SHA; las omisiones auxiliares no equivalen a pruebas ejecutadas. Las seis entregas constaban como LISTO en la fuente institucional; sus A5 históricas deben consultarse **por evento y autor** antes de liberar sucesoras. Este documento no emite A5 sobre ninguna de ellas.

## Procedimiento reproductible de verificación

Para cada uno de los seis SHAs anteriores:

```bash
# Usar un entorno autorizado con acceso de lectura a solqaryn/Solqaryn.
git show --format=fuller --stat <SHA>
gh api "repos/solqaryn/Solqaryn/commits/<SHA>/check-runs?per_page=100" \
  --jq '.check_runs[] | [.name, .head_sha, .status, .conclusion, .html_url] | @tsv'
```

Solo acreditar el job **requerido y pertinente** cuando su `head_sha` coincida con la versión objeto de revisión y `status=completed`, `conclusion=success`. Un workflow verde de otro SHA no es transferible sin equivalencia probada de blobs, dependencias y configuración de test. Para analizar un resultado, abrir el job en GitHub Actions y el archivo `.github/workflows/...` señalado en la tabla; los artefactos de CI pueden caducar y **no se descargaron ni certificaron aquí**. Para comprobar estado institucional actual, releer PLAN_MAESTRO, COLA y DAG_TECNICO, nunca confiar en una fotografía temporal.

## A1–A3 — Qué se puede y qué no se puede concluir

- **A1 — NO_APLICA al diff documental:** no se ejecutan nuevas pruebas de runtime. El lote M05 contiene suites reales xUnit/Node/Chromium según cada diff; este documento no agrega ni suma pasadas de pruebas entre SHAs.
- **A2 — NO_APLICA al diff documental:** los tests de denegación y tenant context sintético no certifican JWT/RBAC/IDOR ni aislamiento HTTP real, roles, MFA o revocación en servidor. Cualquier modificación a IAM/tenant exige gate R2 nuevo.
- **A3 — NO_APLICA al diff documental:** ninguna ruta API/DTO, EF/MySQL, Angular ejecutable, proveedor, secreto o persistencia se modifica. No atribuir certificación MySQL por un test con EF InMemory ni afirmar tráfico HTTP real con Playwright `page.route`.
- **Rendimiento:** sin distribución de muestras de SLO/p95/p99/Core Web Vitals/latencia/costos, registrar **NO_MEDIDO**, no estimar valores de producción.
- **Integridad:** las rutas, workflows y documentos son fuentes Git históricas; en HEAD descendientes la equivalencia exige comparar blobs exactos. No convertir `skipped`, `cancelled` o `in_progress` en `PASS`.

## A4/A5 y cierre causal

Esta publicación solo aporta un **manifest reproducible de fuentes** (R0) en dos archivos: documento y sección autocontenida en `CHANGELOG_AI.md`. A4 exige `verify-solqaryn-project-scope`, procedencia V5, diff y SHA publicado, readback `PLAN_MAESTRO`/`BITACORA`, candado V17 en transiciones. Los checks genéricos de compilación ajenos al diff R0 se reportan por su estado real sin tomar `in_progress` como aprobación, declarando `NO_APLICA` cuando no sean causales.

**A5:** revisión independiente del documento solo por una de las otras cinco Tasks Javier, no por DEV03. El resultado de A5 no certifica producción ni libera dependencias mediante edición manual de COLA/DAG. **Rollback:** git revert normal de este commit, nunca reset/force-push ni borrar BITACORA. No QA, main, PROD, secretos ni datos productivos.
