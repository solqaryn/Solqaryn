# SOLQARYN V5 — C14-04.M03.02: caso de uso Application de readiness DEV

Autor: PRIMARY DEV 04 JAVIER, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. Proyecto solqaryn/Solqaryn rama dev, Scope Lock STRICT, fuente única V5 FINAL y PLAN/COLA/DAG.

Se implementa código .NET 10 real en backend/src/Application/AutomationV5/V5ReleaseReadinessApplicationService.cs, con frontera IV5ReleaseReadinessVerifiedEvidenceSource para un adaptador futuro de autoridad, CI, tenant y candidato obtenidos desde backend confiable. El servicio valida SHA esperado/observado y referencia de release antes de acceso a fuentes, confirma SHA de autoridad, identidad del candidato y compone las reglas Domain C14-04.M03.01 ya publicadas. Un preflight exitoso sólo emite INDEPENDENT_AUTHENTICATED_RELEASE_REVIEW_REQUIRED, nunca autorización de release, despliegue QA/PROD ni permiso de escritura.

Pruebas xUnit en backend/tests/Solqaryn.Tests/Application/V5ReleaseReadinessApplicationServiceTests.cs cubren positivo sin autorización final, SHA spoofing, URL insegura, autoridad ausente, candidato ajeno, tenant B, entorno PROD, CI cancelada o en otro SHA, auto-A5 y aceptación no ejecutada. Un adaptador real y autorizado deberá comprobar procedencia de GitHub, evidencia de ejecución real y revisión independiente antes de cualquier cambio institucional; ningún dato de navegador constituye autoridad.

No se instala DI, endpoint, persistencia, migración, worker, DNS, secrets, deployment ni integración de proveedor. A0 diff por capas, A1 xUnit, A2 tenant/autoría/SHA/CI negativos, A3 backend CI, A4 Scope Lock/provenance exact-SHA más CAS/Sheets/BITACORA readback, A5 otra Task Javier. Rollback git revert sin force-push, sin QA/main/PROD.
