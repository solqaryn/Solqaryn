# C03-04.M06.02 — Manifiesto versionado de pruebas y artefactos reproducibles (V22)

PROJECT_SCOPE_LOCK=STRICT
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
MICROTAREA_ID=C03-04.M06.02
TASK_ID=6aeacb7820ac8191b7d768a6f33dfa09
RISK=R0_DOCUMENTAL_READ_ONLY
A5=INDEPENDIENTE_OTRA_TASK_JAVIER

## Acción atómica y fuentes

PLAN_MAESTRO!D573 solicita **registrar versión, diff, artefactos y resultado reproducible** del bloque C03-04 continuidad/respuesta a incidentes. Esta entrega es un manifiesto documental verificable: no agrega código, prueba, migración, restore, JWT, proveedor o permiso; no certifica la fase M06 ni la meta acumulativa de PLAN E.

| Unidad | SHA fuente (GitHub dev) | Evidencia material | Acreditación y límite |
| --- | --- | --- | --- |
| M05.01 | `a2aa4ff18010d88330d326061211d30fb02ccbd3` | xUnit 27/27 y CI según PLAN K566 | Regla/dominio real, autoridad simulada: no integración JWT HTTP ni DB |
| M05.02 | `c33d3b80bc38e009790c3071b6f82983f9de5b77` | xUnit 22/22 y CI según PLAN K567 | DTO/contrato validado, HTTP autenticado real aún no certificado |
| M05.03 | `e8232053e1680303abd2a6fa7a6c5a4ec3774b19` | 3/3 MySQL 8.4 efímero según PLAN K568 | Integración SQL real; no backup/restore ni JWT live |
| M05.04 | `6b523ce077f534e6d6326165c82ec812f247a929` | xUnit 17/17 según PLAN K569 | Aceptación in-process, no end-to-end API real |
| M05.05 | `204a4f972e3d6935ec553f82c36777271002b0fe` | xUnit 35/35 según PLAN K570 | Negativos de dominio; credenciales/autoridad en fixture |
| M05.06 | `4b4163c55b002ffe02c05f60240fa059b834a492` | [run 44604929709/job 135845972811](https://github.com/solqaryn/Solqaryn/actions/runs/44604929709/job/135845972811) | .NET 10 xUnit Release TRX 10 PASS/0 FAIL/0 SKIP y MySQL 8.4.13 real efímero; sin restore real |

El log real de M05.06 confirma `C0304_M0506_REGRESSION_MYSQL84_PASS=TRUE`, diez pruebas efectivamente ejecutadas y un contenedor mysql:8.4; para M05.01–05 las cantidades se toman del readback institucional K/L y de sus eventos, **no** de una nueva ejecución de CI en esta microtarea.

## Blobs de control de integridad del test M05.06

- `backend/tests/Solqaryn.Tests/Domain/C0304M0506IncidentContinuityRegressionTests.cs`: `515a73891ccfa3b1b6dba2a651219d76f20628a2`
- `backend/tests/Solqaryn.Tests/C0304M0503IncidentMySqlPersistenceIntegrationTests.cs`: `0a67e589da725fcba82c30b841a2cb8a23f9c6b0`
- `backend/src/Domain/AutomationV5/V5ContinuityIncidentDomainRules.cs`: `94e685da2887581b94ecb114cbb992ea5e2018b7`
- `.github/workflows/v22-c03-04-m05-06-regression.yml`: `4968ed5e132cb53e73960d283517fbaac4c4a42f`

Estos cuatro blobs eran identicos en SHA fuente M05.06 y HEAD dev verificado el 19-11-2026 tras A5 independiente. El documento de auditoria previo [C03-04.M06.01](./c03-04-m06-01-auditoria-pruebas-vs-mocks.md) se publico en `a0520314aa23079520c93a2ab42e7b5884037b17`, registrando las diferencias entre pruebas verdaderas y mocks. La presente salida registra esos artefactos sin reproducir un test ni actualizar hashes de archivos ajenos.

## Procedimiento de reproduccion autorizado y alcance de certificados

Para reproducir M05.06 en GitHub-hosted usar el workflow declarado en `.github/workflows/v22-c03-04-m05-06-regression.yml` al SHA que incluya blobs identicos: servicio mysql:8.4 aislado, `dotnet test ... -c Release --filter 'FullyQualifiedName~C0304M0506IncidentContinuityRegressionTests|FullyQualifiedName~C0304M0503IncidentMySqlPersistenceIntegrationTests'`, TRX con executed>=10, passed>=10, failed/error/notExecuted=0, Scope Lock/skill y lectura de logs. Esta receta es referencia documentada, NO afirmacion de que se ha ejecutado otra vez en M06.02. No usar conexiones Aiven productivas, QA/main/PROD ni operaciones R3.

A0: versión/diff documental exclusivamente y origen de cada artefacto.
A1: manifiesto de seis fuentes, resultados reproducibles y blobs para detectar desalineación.
A2/A3: NO_APLICA a **cambios** de API/JWT/DB/UI porque este diff no ejecuta ni modifica esos sistemas; brechas de pruebas integradas permanecen explícitas.
A4: certificación limitada a checks exact-SHA `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada`, BITACORA readback y CAS; no admitir statuses skipped/queued como PASS.
A5: revisión independiente posterior por otra Task Javier diferente de DEV04.

Rollback: `git revert` normal del commit de este manifiesto en dev, conservando evidencias institucionales historicas. No reabrir fase, alterar formulas DAG/COLA ni cambiar schedulers.
