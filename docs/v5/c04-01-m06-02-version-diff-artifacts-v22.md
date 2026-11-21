# SOLQARYN V22 — C04-01.M06.02: versión, diff, artefactos y reproducción

**Documento de evidencia técnica DEV; no acta de producción.** `PROJECT_SCOPE_LOCK=STRICT`; `solqaryn/Solqaryn@dev`; riesgo **R0 documental**. Criterio exclusivo de `PLAN_MAESTRO!D609`: **«Registrar versión, diff, artefactos y resultado reproducible»**. El objetivo `E609` de «Completa M06» es acumulativo, no impone que esta unidad certifique todo el catálogo comercial/financiero. No cambia contrato API, persistencia, JWT, Angular runtime, entidades ni proveedor.

## Identificadores y baseline

- Repositorio: [solqaryn/Solqaryn](https://github.com/solqaryn/Solqaryn); rama `dev`; corte de fuentes `fa84c419d34c33772dba7aea9fd250a4a2e4e6f9` de 2026-11-21.
- Versión de **plataforma**, no un lanzamiento comercial: Angular **22.2.1**, ASP.NET Core **10**, EF Core **10.0.12** con MySql.EntityFrameworkCore Oracle **10.0.9**, MySQL **8.4.x efímero** en laboratorio. Se verifican contra código y jobs al ejecutar, no se modifica SemVer ni se declara producción.
- Plan y datos: `PLAN_MAESTRO!A602:M613`, `COLA!A602:O613` y `DAG_TECNICO!A:F` filtrado por ID. Fuente de requisitos: STANDARD V5 FINAL. El propietario/autor histórico de cada microtarea se conserva.
- M06.01 ya tiene revisión de pruebas frente a mocks: [matriz de alcance V22](https://github.com/solqaryn/Solqaryn/blob/fa84c419d34c33772dba7aea9fd250a4a2e4e6f9/docs/v5/c04-01-m06-01-test-scope-audit-v22.md). Se preservan sus advertencias y su propia CI, sin asumir que esté `LISTO` antes del readback.

## Registro reproducible de artefactos de C04-01 M05

| Microtarea | SHA de código/artefacto publicado | Evidencia dirigida | Naturaleza y límite |
| --- | --- | --- | --- |
| M05.01 | `e1a5ee640d4a7d25a2120e0cfff7a1da6c2d7062` | [GitHub commit](https://github.com/solqaryn/Solqaryn/commit/e1a5ee640d4a7d25a2120e0cfff7a1da6c2d7062); Angular/Vitest **6/6** según PLAN K/L602 | Invariantes test-only; fixture sin JWT productivo |
| M05.02 | `655d5001af0b7c90ded883356fbb33e1a6d9ad9f` | [Job HTTP/DTO .NET xUnit 6/6](https://github.com/solqaryn/Solqaryn/actions/runs/44890825950/job/136806032102) | TestServer/middleware real, actor y plan sintéticos |
| M05.03 | `a09612bb1fb33f27fff1bb839ffb02e816a52a15` | [Job Oracle EF/MySQL 8.4.13 1/1](https://github.com/solqaryn/Solqaryn/actions/runs/44890557494/job/136805225899) | SQL aislado: rollback, revocación y tenant A/B; no PSP |
| M05.04 | `75836d241b9a6a7481bfa61e0f56109a643c7455` | [Job Chromium 4/4](https://github.com/solqaryn/Solqaryn/actions/runs/44889532480/job/136802106742) | Navegación real, sesión de fixture; no JWT/MySQL live |
| M05.05 | `9b54102b7aa40b59e27565211fc0c1bf9ea590cd` | [Job JWT firmado/MySQL 8.4.13, TRX 1/1](https://github.com/solqaryn/Solqaryn/actions/runs/44890020052/job/136803604770) | Negativos HTTP tenant/RBAC de laboratorio; no cobro |
| M05.06 | `3a6d757d997bc6616d9787f93b01bfdf8fadb994` | [Job Playwright/Chromium 5/5](https://github.com/solqaryn/Solqaryn/actions/runs/44892159766/job/136810149216) | Regresión, foco real Tab/Enter, cuatro tamaños, presupuesto CI; no CWV de campo |

**Comprobación de integridad documental:** los seis IDs corresponden a las filas 602–607 y tienen responsables históricos distintos. La evidencia independiente A5 se consulta en `PLAN_MAESTRO!K602:K607` y eventos `BITACORA`; nunca se presupone por el estado de la fase. El cambio M05.06 altera únicamente su spec E2E, workflow y documento; no cambia `CHANGELOG_AI.md`. Sus blobs (GitHub): spec `e6c6a16f9c4c7dbb6f7f395f5f70bf96d10c4439`, workflow `d75cfe5a08ca96329e5c049c0e397c251fe49364`, documento `f3e0589f957a9c6df4e5c052dbb0c62e3e511fc0`. Procedencia V5 previamente verde contra `655d5001...` con `CHANGELOG_AI.md` blob `d5dff8fdc30d067391ad1ab7ad01be878834977b` y verificador blob `00e0b1414695b433f9c48cf7ad1f099a0e4b010b` inalterados al corte M05.06. Una prueba previa verde sin equivalencia de blobs no es PASS de un nuevo runtime.

## Reproducción mediante GitHub-hosted, no CI simulada

1. Consultar HEAD actual: `GET /repos/solqaryn/Solqaryn/branches/dev`; anotar el SHA y confirmar que el commit causal es ancestro.
2. Para **cada** SHA causal, consultar `GET /repos/solqaryn/Solqaryn/commits/<SHA>/check-runs?per_page=100`. Exigir `head_sha` exacto, `status=completed`, `conclusion=success` **solo en jobs obligatorios pertinentes**. Un job `skipped`, `cancelled`, `failure` o `in_progress` no acredita su contrato. Descargar/logs de los jobs citados; exigir el conteo ejecutado y cero fallos/omisiones.
3. Para frontend, usar los jobs existentes de Angular TypeScript/build y Playwright/Chromium, sin elevar mocks de sesión. Para .NET usar el xUnit GitHub-hosted, no afirmar que no hubo ejecución porque el entorno del chat carezca de SDK. Para SQL, la instancia MySQL 8.4.x es efímera, no Aiven productivo.
4. Verificar `verify-solqaryn-project-scope` y procedencia V5 del SHA documental final; si un workflow queda en curso, conservar `VALIDANDO` con su SHA, sin simular PASS. No relanzar trabajos ajenos mientras otra Task está escribiendo el scope.
5. Confirmar después de cualquier transición `PLAN H/I/J/K/L/M`, `COLA E/G/O`, `DAG F/G` y la `BITACORA` append-only por EVENT_ID único. No cambiar manualmente G/O, fórmulas ni propietarios anteriores.

## Diff específico de esta microtarea

Únicamente se crea **`docs/v5/c04-01-m06-02-version-diff-artifacts-v22.md`**. Los enlaces, hashes, pruebas y limitaciones ya publicadas se consignan sin reescribir el gran `CHANGELOG_AI.md`, modificar tests ni activar lógica de facturación. La revisión compara lo que los tests realmente demuestran y lo que no se puede concluir. Una discrepancia de fuente/hash/CI produce **NO_GO** para la afirmación correspondiente, no un PASS documental inventado.

**A0**: mapa de componente, artefactos y alcance D609. **A1**: positivos reproducibles (6 fuentes/6 IDs) y negativos (SHA divergente, test omitido, CI in_progress, mock elevado a JWT y entorno efímero elevado a producción). **A2**: no cambia seguridad; clasificación expresa de JWT/RBAC/tenant real frente a fixture. **A3**: NO_APLICA nueva integración a este diff documental; se citan pruebas reales existentes. **A4**: commit FF/CAS, Scope Lock exact-SHA, procedencia aplicable, readback e idempotencia. **A5** requiere otra Task Javier distinta de DEV01 y revisión propia.

## Riesgos y deshacer

Aún no hay por estas seis pruebas certificación global de precios server-side, cuotas y periodicidad contractuales, PSP, renovaciones/cobros, CWV físicos ni QA/main/PROD. Los gates de `M06.06` requieren resultados independientes genuinos de `M06.01–05+A5` y prerequisitos DAG. Sin orden R3 no se habilitan pagos, producción, secretos o migraciones reales.

Rollback seguro del presente documento: commit Git ordinario con `git revert` y nueva verificación de SHA/CAS; nunca `force-push`, reset destructivo, borrado de BITACORA ni modificación de COLA/DAG.

**Salida D609:** registro versionado y verificable de fuentes, diffs, CI, límites y reproducción; **no** certificado de fase completa.