# C11-04.M06.02 — registro reproducible de versión, diff, artefactos y resultado

**Identidad y gobierno:** `SOLQARYN`, `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`; Task autora DEV04 `6aeacb7820ac8191b7d768a6f33dfa09`; PLAN fila 1725; V17 lock determinista 2000001725. **Clase R0, documental sin runtime**. Fuente de partida `c30283c16fca5639ddb52864aa455bc2a868a5c0`. No cambia API, frontend, backend, migraciones, secretos, permisos ni infraestructura. Normal rollback con git revert únicamente de los documentos del changeset, nunca editar BITACORA histórica.

## Registro de artefactos y pruebas por SHA

Cada fila apunta a una versión **causal** diferente; el valor de SHA no certifica otras versiones ni implica A5. Los tests UI con sesión simulada son browser real y HTTP fixture, **no** seguridad end-to-end.

| ID | SOURCE_SHA | Artefacto principal / evidencia pertinente | Runner y prueba | No certifica |
| --- | --- | --- | --- | --- |
| C11-04.M05.01 | `29f4e4b1605d291566db73a840683d2be0c2a157` | Matriz de unit tests de observabilidad e interacciones browser; run `43866547232` | Angular/Vitest 22 y Chromium 14 según readback A0–A4 en PLAN | JWT, MySQL o exporter real |
| C11-04.M05.02 | `a31cb7bae9a1acf7151f37e69711d342fd89d05b` | Contrato DTO y errores xUnit; run `43907004048` | .NET 10 Release, tests DTO en proceso, resultados en PLAN | API HTTP con middleware, DB real |
| C11-04.M05.03 | `ee1169b09f9638cf58cf48df1879f80de8abce84` | Test `LoggerFactory`/ `ILoggerProvider` .NET; run `43960859810` | 14/14 xUnit real in-process | Exporter externo y sistema de logs durable |
| C11-04.M05.04 | `4a6a4c4d78929a5b025cc368f00f21e6ac531269` | Test positivo, 200 sintético, 320/1280; runs `43868812535` y aceptación `43868812747` | 22 Vitest + 15 Chromium | Sesión real, provider, SLO |
| C11-04.M05.05 | `0243c612b57c8df312acc996df5b6086f435d278` | Negativos 429/revocación/no secretos; runs `43869540690` y aceptación `43869540646` | 22 Vitest + 16 Chromium | Rate limit/roles backend efectivos |
| C11-04.M05.06 | `e8a2663ccec7964a005ca7b73bdebebc0158ddb0` | `.github/workflows/v18-c11-04-m05-06-regression.yml`, `frontend/e2e/c11-04-m05-06-regression.spec.ts`, `docs/v5/c11-04-m05-06-regression-performance-a11y.md`; run `43971041616`; job IDs `133662951322` y `133662952008` | **14/14 xUnit** de proveedor real en proceso + **4/4 Chromium** con Tab y 403/503 sintéticos, logs leídos | JWT, MySQL, reporter externo, SLO/CWV/coste |

Los workflows pueden informar trabajos auxiliares SKIPPED: **no se contabilizan como PASS**. El artefacto de auditoría `C11-04.M06.01` es `docs/v5/c11-04-m06-01-auditoria-alcance-pruebas-vs-mocks.md`, SHA de commit `c30283c16fca5639ddb52864aa455bc2a868a5c0`, blob Git `94f835998189b03f00938283407818e8936e8a40`. Su Scope Lock/V5 se evalúa en ese SHA, sin reutilizar comprobaciones anteriores.

## Procedimiento de reproducción por tercero (no sustituye ejecución)

Con sesión GitHub autorizada en el repositorio original y **sin escribir en ramas**, repetir para cada `SOURCE_SHA` de la tabla:

```sh
git fetch origin dev
git show --stat --no-renames <SOURCE_SHA>
gh api "repos/solqaryn/Solqaryn/commits/<SOURCE_SHA>/check-runs?per_page=100" --jq '.check_runs[] | [.name, .head_sha, .status, .conclusion] | @tsv'
gh run view <RUN_ID> --repo solqaryn/Solqaryn --log
```

El comando es guía **para ejecución posterior**: no afirma que `git` o `gh` se hayan ejecutado en este documento. Comprobar que `head_sha` corresponde al commit causal y que cada check **requerido** está `completed/success`, no `queued`, `skipped`, `cancelled` o `failure`. Para probar equivalencia con HEAD descendiente, comparar SHA Git de los archivos de prueba, código, workflow, dependencias compartidas y configuración de tests; un solo build verde no prueba equivalente funcional. Verificar bitácora institucional y reviewer ID distinto del author.

### Registro de cambio propio y límites

**Incremento de M06.02:** exclusivamente este documento de inventario trazable y un encabezado autocontenido con viñetas en `CHANGELOG_AI.md`. Ningún nuevo evento del producto, nuevo API, tenant, columna DB o permiso; no hace falta MySQL, Playwright, xUnit o navegador nuevos **para verificar este documento R0**. A1 documental: seis IDs exactos, SHA y run emparejados, errores/negativos expresos, límites indicados; A2 y A3 runtime NO_APLICA motivado. A4: GitHub Scope Lock y procedencia V5 en SHA publicado, readback en PLAN/COLA/DAG/BITACORA. A5: revisión independiente por otra Task Javier, no autor.

**Limitaciones abiertas:** Core Web Vitals/SLO de producción, métricas de red, RTO/RPO, coste, exporter, flujo de JWT/RBAC efectivo entre empresas y persistencia externa permanecen `NO_MEDIDO` o `NO_CERTIFICADO` (sin numeradores/denominadores inventados). No se habilitan QA/main/PROD ni sucesoras por declaración textual; rige el DAG.

**SHA y checks posteriores a esta entrega:** se registran exclusivamente en el evento causal de BITACORA y en PLAN_MAESTRO al publicar; este archivo no los anticipa.
