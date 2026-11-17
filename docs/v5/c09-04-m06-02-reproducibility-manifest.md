# C09-04.M06.02 — Registro reproducible V19 (versión, diffs, artefactos y resultados)

Fecha: 17-11-2026. Autor SOLQARYN DEV06, TASK_ID `6af16bbad26881918c283abe06e1f32a`. Repositorio `solqaryn/Solqaryn`, rama `dev`, PROJECT_SCOPE_LOCK=STRICT. Fuente de ejecución: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES y PLAN_MAESTRO fila 1437. Lock V17 `SQV17_C09_04_M06_02` (`2000001437`). Riesgo **R0 documental**; esta microtarea no modifica aplicación, base de datos, credenciales, proveedores ni infraestructura.

## A0 — versión y alcance del changeset
El baseline GitHub leído al reclamar fue `434de297aef583f46011b87de469c63bd84c7b8b` en `dev`, tras el cierre documental de `C09-04.M06.01`. El changeset M06.02 consiste exclusivamente en **este documento nuevo y una sección indivisible de CHANGELOG_AI.md**. No debe aceptarse si el diff final contiene runtime, workflows, secretos o infraestructura. Stack de referencia del proyecto: Angular 22.2.1, ASP.NET Core net10.0, EF Core 10.0.12, Oracle MySql.EntityFrameworkCore 10.0.9, MySQL 8.4 en CI efímera. Las versiones del runtime no se actualizan ni se infieren de imágenes históricas.

## Matriz de procedencia de los seis artefactos M05
La fase M05.01–06 figura LISTO con A5 de revisor diferente. La auditoría anterior `C09-04.M06.01` preserva comparación de pruebas reales frente a mocks: `docs/v5/c09-04-m06-01-m05-evidence-vs-mocks.md` del baseline citado. Este registro aporta comandos, manifiesto de versiones y resultados replicables, **sin recertificar** las microtareas de sus autores.

| ID | Commit GitHub (SHA exacto) | CI dirigida | Job y resultado comprobado | Frontera |
| --- | --- | --- | --- | --- |
| M05.01 | [0263060c4f6042e634f26bd5f6c5119babb5bebf](https://github.com/solqaryn/Solqaryn/commit/0263060c4f6042e634f26bd5f6c5119babb5bebf) | [44121223928](https://github.com/solqaryn/Solqaryn/actions/runs/44121223928) | `134172795531`: 7/7 xUnit Release, SUCCESS | Worker con fake repo/proveedor; no envío real. |
| M05.02 | [9a0dab0695a8212b29222c68db57ade9a11beffa](https://github.com/solqaryn/Solqaryn/commit/9a0dab0695a8212b29222c68db57ade9a11beffa) | [44124315470](https://github.com/solqaryn/Solqaryn/actions/runs/44124315470) | `134183815604`: 8/8 xUnit Release, SUCCESS | Controlador/DTO con servicios sustituidos; no JWT HTTP real. |
| M05.03 | [a44a227f4bc4e5ee6a77762b997afadccdee86e2](https://github.com/solqaryn/Solqaryn/commit/a44a227f4bc4e5ee6a77762b997afadccdee86e2) | [44126557626](https://github.com/solqaryn/Solqaryn/actions/runs/44126557626) | `134191826045`: 3/3 xUnit Category=Integration/TRX MySQL 8.4 EF Oracle, SUCCESS | DB efímera real, no Aiven/SMTP/WhatsApp. |
| M05.04 | [655a28e7633bd5b3d32a56a8e2e7b7cd4d731323](https://github.com/solqaryn/Solqaryn/commit/655a28e7633bd5b3d32a56a8e2e7b7cd4d731323) | [44126350846](https://github.com/solqaryn/Solqaryn/actions/runs/44126350846) | `134191090953`: 3/3 xUnit positivo, SUCCESS | API/aplicación sin transporte de proveedor. |
| M05.05 | [76ddaee9990cb1dece9af8813c62612915255b8c](https://github.com/solqaryn/Solqaryn/commit/76ddaee9990cb1dece9af8813c62612915255b8c) | [44129286222](https://github.com/solqaryn/Solqaryn/actions/runs/44129286222) | `134201568394`: 11/11 xUnit negativos, SUCCESS | SHA corregido de Moq; no utilizar SHA previo `443707d9` como PASS. |
| M05.06 | [3dd2b7be2787c87dcf021eecd628e7238503217d](https://github.com/solqaryn/Solqaryn/commit/3dd2b7be2787c87dcf021eecd628e7238503217d) | [44140941910](https://github.com/solqaryn/Solqaryn/actions/runs/44140941910) | `134243228056`: 4/4 Playwright/Chromium, SUCCESS | Navegador real, sesión HTTP simulada; no JWT firmado. |

Fuente de cada conteo: log real del job, no solo el resumen verde del workflow. En los seis commits se constató `verify-solqaryn-project-scope` completed/success exact-SHA. **No se puede afirmar lo mismo del check V5 en los seis.** La consulta REST `commits/<SHA>/check-runs?per_page=100` confirmó: 

| M05 y SHA | Scope Lock | Check V5 de procedencia | Resultado |
| --- | --- | --- | --- |
| M05.01 `0263060c` | SUCCESS | SUCCESS | Acreditado |
| M05.02 `9a0dab06` | SUCCESS | SUCCESS | Acreditado |
| M05.03 `a44a227f` | SUCCESS | SUCCESS | Acreditado |
| M05.04 `655a28e7` | SUCCESS | SUCCESS | Acreditado |
| M05.05 `76ddaee9` | SUCCESS | **NO_EJECUTADO/NO_ACREDITADO** (0 check V5 de 29) | No interpretar como PASS |
| M05.06 `3dd2b7be` | SUCCESS | SUCCESS | Acreditado |

En M05.05 se preservan como evidencia válida los **11/11 xUnit** y Scope Lock SUCCESS exact-SHA, sin atribuir un check V5 inexistente ni inferir equivalencia con otro SHA. `SKIPPED` en reintentos auxiliares no equivale a PASS. Esta **reparación documental P2** la realiza DEV04 sobre el documento originalmente elaborado por DEV06, con candado V17 y revisión A5 correctiva pendiente por una Task Javier independiente de DEV04 y DEV06; conserva A5_FAIL histórico hasta esa revisión, sin liberar DAG, QA/main/PROD, webhook, SMTP o WhatsApp reales.

## A1 — procedimiento reproducible read-only
Con GitHub CLI autenticado y un checkout **de sólo lectura** del repositorio autorizado (o utilizando exclusivamente la API GitHub), sustituir `SHA`/`RUN` por cada entrada de la matriz:

```sh
git show --format=fuller --stat SHA
git diff-tree --no-commit-id --name-only -r SHA
gh api repos/solqaryn/Solqaryn/commits/SHA/check-runs --paginate --jq '.check_runs[] | [.name,.head_sha,.status,.conclusion] | @tsv'
gh api repos/solqaryn/Solqaryn/actions/runs/RUN/jobs --jq '.jobs[] | [.id,.name,.status,.conclusion] | @tsv'
gh api repos/solqaryn/Solqaryn/actions/jobs/JOB/logs > evidence-job.log
```

`gh api .../logs` descarga logs; si GitHub expide un redirect, el cliente debe resolverlo sin divulgar tokens. La CI original sí ejecutó los tests. Los comandos anteriores sirven para **releer** evidencia, no para afirmar que se repitieron pruebas ni para saltarse CI. Comparar `head_sha` contra el SHA de la fila y el nombre del test/check; exigir `status=completed`, `conclusion=success`, total ejecutado, fallos cero. Para M05.03 comprobar expresamente TRX `Category=Integration`, MySQL 8.4 aislado y 3/3, no un build o un filtro `Category!=Integration`.

## A1 negativos, A2 seguridad y A3 integración
- N1 si un run es `cancelled/skipped/queued/in_progress/failure`, **no** se registra como evidencia aprobada.
- N2 si `head_sha` difiere del source SHA, no se presume equivalencia: cotejar blobs, dependencias, test config y resultados o retener VALIDANDO.
- N3 si MySQL es sustituido por mock en M05.03, falla el criterio de integración causal; las otras suites con mocks no demuestran persistencia real.
- N4 un HTTP 403/503 simulado de Chromium no prueba JWT firmado, revocación real o anti-enumeración del servidor.
- N5 `7+8+3+3+11+4=36` casos aprobados del conjunto M05 no certifican webhook firmado/replay remoto, entrega SMTP/WhatsApp, privacidad PII en proveedores ni exactly-once de la red.
- N6 la lectura de documentación no autoriza migraciones, QA, main, PROD, Aiven ni cambios a secretos.

A2: la revisión de seguridad conserva límites tenant A/B de tests, riesgos de revocación real, consentimiento, PII y webhook; no se ejecuta un test nuevo de autenticación. A3: **NO_APLICA a cambios de runtime para esta unidad R0**: se registran los enlaces de CI real previa sin fingir nueva integración. La evidencia MySQL 8.4 de M05.03 pertenece a su SHA, no al documento M06.02.

## Artefactos entregables, rollback y A4/A5
El resultado auditable es (i) este manifiesto con enlaces y oráculos, (ii) CHANGELOG_AI.md con encabezado y viñetas contiguos, (iii) commit nuevo fast-forward sobre el HEAD vivo, (iv) run GitHub Scope Lock/V5 exact-SHA `completed/success`, (v) readbacks PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA. **No otorgar LISTO** si (iv) falta; `VALIDANDO` mientras CI externa siga legítimamente in_progress. Mantener A5_PENDIENTE para una de las otras cinco Tasks Javier; DEV06 no puede revisar su propia autoría.

Rollback de este changeset documental: `git revert` normal sobre commit validado y HEAD actual, sin fuerza ni restaurar historia; preservar BITACORA apend-only. Seguir con unidades independientes autorizadas y puertas DAG, sin liberar M06.06 ni producción por este manifiesto.
