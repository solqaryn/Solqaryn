# SOLQARYN V5 FINAL — C00-01.M06.02: versiones, diffs y artefactos reproducibles

**Tipo:** 1 — trazabilidad documental, sin modificar servicios, Angular, APIs, DB, MySQL, Docker, JWT, RBAC, infraestructura o secretos.  
**Ejecutor:** PRIMARY DEV 06 — JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`, cuenta GitHub `jmejia31`. Registro atribuible a la cuenta y al commit, **no** firma criptográfica.  
**Fuente canónica V5:** [Google Doc](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) (ID `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y [Sheet institucional](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit) (ID `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`). Snapshot de 2026-11-13; revalidar siempre HEAD y estados vigentes.

## Gate y clasificación

- `C00-01.M05.01–M05.06` estaban LISTO y `C00-01.M06.01` figuraba LISTO_DEV antes del claim; `PLAN_MAESTRO!H` es autoridad y `COLA!G/O=SI` para M06.02. Sin responsable anterior. No se encontró fila M06.02 en DAG_TECNICO: aplicación conservadora del gate de lote M05, sin abrir fases siguientes.
- **requiere_dotnet=NO, requiere_node_angular=NO, requiere_mysql=NO, requiere_docker=NO, requiere_e2e=NO** para este nuevo cambio documental; versiones y ejecuciones existentes se comprueban por evidencia de sus propios SHA, sin pruebas nuevas de software.
- **requiere_security=SI (inspección documental)** para no mezclar datos, secretos, permisos, falsas coberturas y QA. **requiere_scope_lock=SI** para el nuevo commit documental.
- A5 es independiente. El resultado de este registro es inventariar y poder reproducir la consulta de evidencias, no aprobar QA/main/PROD ni M06.06.

## Manifiesto reproducible de versiones y artefactos

| Elemento | Huella o referencia | Verificación concreta |
| --- | --- | --- |
| Plan V5 FINAL | Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; 2.340 IDs `C00-01.M01.01` a `C15-05.M06.06` | Leer versión vigente del documento; no usar V3 ni PR históricos |
| PLAN / COLA | Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`; `PLAN_MAESTRO!H` y `COLA!E` | Estados coherentes por ID; leer G y O con responsable y DAG al reclamar |
| BITACORA | 26 columnas `A:Z`, encabezado físico vigente | `EVENT_ID` (A) e `IDEMPOTENCY_KEY` (R) únicos; RUN_ID (H) vacío sin ejecución nativa acreditada |
| Governance | Skill blob `304e400d5c0f89fcfdcc65caece86c34276734b5`; arquitectura blob `1931e1229752028229b6ad1bb3368038e2c6769b` | Coincidencia de Skill y Source Lock; no suponer que copia instalada cambia automáticamente |
| M06.01 auditoría causal | [Commit `44065130c13be001370130bfe11c683636bd4227`](https://github.com/solqaryn/Solqaryn/commit/44065130c13be001370130bfe11c683636bd4227) | Parent `18bb123608f2a04dbc6d76e442b48d2a801e28ff`, tree `fc0a37255e13d29081b139783b34668895fe27da` |
| M06.01 documento | `docs/v5/c00-01-m06-01-pruebas-reales-vs-mocks.md` blob `e8622341597558c84310a91d132f8b75367e91e7` | Nuevo archivo documental de 53 líneas, incluye matriz positiva/negativa y límites de mocks |
| M06.01 CHANGELOG | Commit anterior añadió 3 líneas a `CHANGELOG_AI.md` | Contrastar diff real; no hay modificación de runtime |
| M06.03 riesgos corregidos | `docs/v5/c00-01-m06-03-riesgos-limitaciones-decisiones.md` blob `2af0c3abe67d0ac0853fedff099d5eafebadb352` | BITACORA A:Z/26 explícito; A5_PASS del SHA corregido registrado por otro TASK_ID |
| M10 / MySQL / Playwright | [Run `43463854111`](https://github.com/solqaryn/Solqaryn/actions/runs/43463854111), job `132017609415`, SHA `14ea2e238cffa33bb93af38412983918de306cc5` | Job `completed/success` y log `12 passed`; API, Angular y MySQL 8.4 efímero |
| Seguridad xUnit | [Run `43463853912`](https://github.com/solqaryn/Solqaryn/actions/runs/43463853912), job `132017600479`, mismo SHA de pruebas | Log `Passed 1109, Failed 0`; filtro excluye `Category=Integration`; no garantiza todas las combinaciones tenant A/B |
| Scope Lock M06.01 | [Run `43468121775`](https://github.com/solqaryn/Solqaryn/actions/runs/43468121775), job `132032616311`, SHA `44065130c13be001370130bfe11c683636bd4227` | `status=completed, conclusion=success, head_sha=SHA`; pasos de frescura Skill/negativos exitosos |
| Catálogo Angular M06.01 | Mismo run, job `132032614466` | `completed/success` exact-SHA, complementario; no asumir que un job separado omitido es PASS |

## Diff verificable y comandos de readback

El commit `44065130c13be001370130bfe11c683636bd4227` cambió únicamente dos rutas: `CHANGELOG_AI.md` (**+3/-0**) y `docs/v5/c00-01-m06-01-pruebas-reales-vs-mocks.md` (**+53/-0**). Tree igual al candidato `fc0a37255e13d29081b139783b34668895fe27da`. La evidencia indica **documentación nueva**, no pruebas de comportamiento runtime nuevas.

Consultas reproducibles, sólo lectura, usando cuenta GitHub autorizada:

```text
GET https://api.github.com/repos/solqaryn/Solqaryn/commits/44065130c13be001370130bfe11c683636bd4227
GET https://api.github.com/repos/solqaryn/Solqaryn/commits/44065130c13be001370130bfe11c683636bd4227/check-runs?per_page=100
GET https://api.github.com/repos/solqaryn/Solqaryn/branches/dev
GET https://api.github.com/repos/solqaryn/Solqaryn/actions/runs/43463854111/jobs
```

Comparar siempre `head_sha` del check con el SHA de la prueba, `status=completed`, `conclusion=success`; el retry auxiliar SKIPPED no es PASS de una prueba. Para Sheets, leer `PLAN_MAESTRO!A1:O2341`, `COLA!A1:Q2341`, `DAG_TECNICO!A:G` y `BITACORA!A1:Z1` antes de modificar; readback después.

## Contradicciones y condiciones negativas

1. **Baseline congelado vs HEAD móvil:** `f4b8528f...` es antecesor histórico, no SHA permanente de cierre; la evidencia de cada cambio debe usar su commit exacto.
2. **Scope Lock positivo no es CI global verde:** `verify-solqaryn-project-scope` SUCCESS certifica su propia ruta. Job backend, frontend o seguridad ajeno / en progreso no se marca PASS por extrapolación. Fallos obligatorios nunca se omiten.
3. **Mock/UI contra red/DB:** `route.fulfill` del dashboard en M10 simula respuesta, pero el login usa `route.continue` y Kardex verifica HTTP 200/400 reales en runner MySQL efímero. No combinar pruebas en una garantía que no tienen.
4. **A5_FAIL histórico:** una reparación posterior no reescribe el resultado original. Para el SHA nuevo, la revisión A5 corresponde a Task distinta y evidencia nueva.
5. **Integridad de eventos:** un evento de 21–25 columnas o EVENT_ID/IDEMPOTENCY_KEY duplicado es inválido; no crear filas auxiliares artificiales ni inventar RUN_ID.
6. **No promoción:** LISTO_DEV con A5_PENDIENTE no autoriza QA/main/PROD; M06.02 no libera C00-01.M06.06, que requiere comprobación independiente de estado y dependencias.

## A0–A5 y rollback

- **A0:** revisar diff documental exacto, huellas y ausencia de secretos/sorpresas.
- **A1:** verificar positivos (commit, árbol, dos rutas, checks reales) y negativos (SHA anterior/SKIPPED/mock no se elevan).
- **A2:** separar seguridad, identificación de actor y controles JWT/RBAC/tenant sin prometer cobertura de producción.
- **A3:** integrar referencias de Docs/Sheets/GitHub reproducibles; `dotnet`/npm/MySQL/Docker/E2E nuevos: NO_APLICA, justificado por alcance sin ejecutable.
- **A4:** tras publicar este documento en `dev` por fast-forward, confirmar SHA y Scope Lock exactos, diff, readback PLAN/COLA/BITACORA 26 campos y `git revert` no destructivo.
- **A5:** PENDIENTE de otro TASK_ID; registro único por SHA, revisión positiva+negativa real antes de QA/main/PROD.

**Rollback:** `git revert <SHA-de-este-documento>`, sin force-push, preservando historial ajeno y estados institucionales coherentes.
