# C02-02.M06.02 — Manifiesto de versión, diffs, artefactos y reproducción

Fecha: 2026-11-18. Proyecto: `solqaryn/Solqaryn` / `dev`. `PROJECT_SCOPE_LOCK=STRICT`. Tipo de entrega: **R0 documental**. Autor de este manifiesto: DEV03. Este registro reúne fuentes y procedimientos auditables sin atribuir nuevas pruebas, conexiones, gates ni permisos a SOLQARYN.

## A0 — Alcance y control de versiones

Requisito de `PLAN_MAESTRO!A357:E357`: «Registrar versión, diff, artefactos y resultado reproducible», dentro de la evidencia y liberación M06 shared/dedicated placement. Dependencias: M05 del mismo bloque y sus A5 realmente aplicables según el DAG. Un resultado `LISTO` documental de este ID no certifica el router, DNS/DSN, secretos, migración, rollback operativo, cambios sobre tenants reales, QA ni producción.

Este commit agrega solamente el manifiesto y una sección autocontenida a `CHANGELOG_AI.md`. No cambia `.NET`, Angular, EF Core, MySQL, DTO, API, permisos JWT/RBAC, rutas, contenedores, schemas, scripts ni parámetros comerciales. Rollback: `git revert <SHA_DE_ESTE_COMMIT>` en un HEAD autorizado y con serialización de escritura, sin force push ni retroceder progreso ajeno.

**Baseline de consulta técnica:** `c21fc9eb67a4490252ff9df131e0ecd819c81305` (dev antes de que empezara M06.03); M06.03 publicó `5628543352af66fc01c5c998d0bc3d5ddddc6fe6`. La base real de ESTE commit debe comprobarse en el objeto Git publicado, no deducirse de estos enlaces. El alcance de evidencia M05 se ancla a los commits históricos que figuran en el registro institucional; esos SHA no son el HEAD actual.

## Artefactos M05 y trazabilidad de prueba

| ID | Commit causante o correctivo relevante | Fuente reproducible y frontera |
| --- | --- | --- |
| C02-02.M05.01 | `a3f3d0bb88863c9cd1697ed2f81cace70e89447f` | `docs/C02-02.M05.01-PLACEMENT-DOMAIN-BOUNDARY-REGRESSIONS.md`: reglas puras/tenant A-B y límites 128/129; no MySQL/JWT |
| C02-02.M05.02 | `f6c945a86d255fd7c473ac10c149951b2b2a064a` | `docs/C02-02.M05.02-PLACEMENT-REQUEST-CONTRACT-ERROR-SAFETY.md`: DTO Application y fallos sanitizados; no endpoint registrado |
| C02-02.M05.03 | `e3688574566e84e4def5d98fda8ec852d3158a1e` (reparación correctiva tras A5_FAIL) | `docs/v5/c02-02-m05-03-oracle-mysql-placement-integration.md`, `backend/tests/Solqaryn.Tests/C0202M0503MySqlPlacementEvidenceIntegrationTests.cs`, job MySQL 8.4 real `44404923031/135157483625`; no aislamiento SQL automático, no JWT HTTP |
| C02-02.M05.04 | `c37158e030e7af47b3da975d4e66bd054a61a907` | `docs/v5/c02-02-m05-04-positive-placement-acceptance.md`, suite Application positiva no autorizante |
| C02-02.M05.05 | `488aaa40001d0015d8268ae3de47b9212426c6f0` | `docs/v5/c02-02-m05-05-negative-security-placement.md`, tenant cruzado, abuso DSN/DDL, revocación y error seguro |
| C02-02.M05.06 | `2abd96b9f243eb2e499d5eb11b01b72a9cd38bf6` | `docs/v5/c02-02-m05-06-regression-budget.md`, xUnit del presupuesto de reglas puras, job `44407530043/135166621371`; no latencia productiva |

**Evidencia institucional obligatoria:** volver a leer para cada ID `PLAN_MAESTRO!H/I/K/L`, `BITACORA!A:Z` y A5 en el SHA correcto. El estado `LISTO` o un A5_PASS escrito en la hoja no sustituye resultados de CI o un A5 causal. `SKIPPED`, `CANCELLED` e `IN_PROGRESS` no equivalen a `SUCCESS`.

## Procedimiento técnico de readback (reproducible; NO ejecutado por este documento)

En checkout de `solqaryn/Solqaryn` autenticado en `dev` y con HEAD comprobado:

```bash
git rev-parse HEAD
git show --format=fuller --stat <SOURCE_SHA>
git diff --name-status <SOURCE_SHA>^ <SOURCE_SHA>
git ls-tree -r <SOURCE_SHA> -- docs/v5/
node scripts/verify-project-scope.mjs
node scripts/quality/verify-v5-changelog-provenance.mjs
```

Los marcadores `<SOURCE_SHA>` deben sustituirse por SHA observados de la tabla; no ejecutar literalmente ese placeholder. El resultado de ejecutar esos comandos no está afirmado como prueba nueva por esta entrega R0.

Para comprobar realmente tests dirigidos posteriores, inspeccionar en GitHub Actions el **workflow/job y SHA exactos**; leer logs, filtro de xUnit y número de pruebas ejecutadas. No considerar `Category!=Integration` como prueba EF/MySQL; para M05.03 exigir el filtro **Category=Integration** y MySQL efímero real. Fuentes de referencia:

- `https://github.com/solqaryn/Solqaryn/actions/runs/44404923031/job/135157483625` — EF/MySQL 8.4 de M05.03 corregida.
- `https://github.com/solqaryn/Solqaryn/actions/runs/44407530043/job/135166621371` — suite pura de regresión M05.06.
- `https://github.com/solqaryn/Solqaryn/commit/e3688574566e84e4def5d98fda8ec852d3158a1e` — corrección tras defecto A5, sin borrar FAIL histórico.

En una comprobación futura documentar explícitamente: `SOURCE_SHA`, `TESTED_SHA`, nombre del check, `status`, `conclusion`, tests invocados y tests fallidos/omitidos, límites de tenant y enlaces causales. Para descendiente equivalente comparar blobs del scope y dependencias; no admitir PASS de otro SHA sólo porque build general esté verde.

## A1 — invariantes comprobables del manifiesto

Se enumeran exactamente los seis IDs M05.01–M05.06, sus artefactos y SHA, la corrección P2 de MySQL, los límites de integración y dos URL de jobs dirigidos. Cada hash de commit debe resolverse en el repo original. El manifiesto no inventa `PASS` adicional ni acredita el estado físico de las Tasks. Si cambian HEAD/artefactos, añadir registro posterior sin sobrescribir BITACORA.

## A2/A3 — seguridad e integración

`NO_APLICA` al **diff documental**: no se ejecuta runtime, JWT, RBAC, tenant real, migración, MySQL, Chromium ni proveedor externo. Esta exclusión NO certifica su implementación: el plan de placement/migración exige esas verificaciones en las unidades pertinentes. No se exponen credenciales, DSN ni datos de terceros.

## A4 — publicación controlada

Publicación por fast-forward sobre `dev` con HEAD CAS y CHANGELOG de sección autocontenida. Exigir `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` **sobre el SHA de este commit**, más readback del estado y evento idempotente `BITACORA`. No usar `SKIPPED` como PASS.

## A5 — independencia y continuación

A5 pendiente de otra Task Javier diferente del DEV03 autor, con lectura genuina del diff, hashes/fuentes, correspondencia de pruebas y ausencia de sobrecertificación. M06.01, M06.04, M06.05, M06.06, API JWT, router de storage y producción mantienen gates propios, no se desbloquean por este manifiesto.
