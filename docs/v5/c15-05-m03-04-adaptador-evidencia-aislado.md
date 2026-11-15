# SOLQARYN V5 C15-05.M03.04 — adaptador aislado .NET 10

Autor: PRIMARY DEV 04 Javier, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. Proyecto solqaryn/Solqaryn, solo dev, PROJECT_SCOPE_LOCK=STRICT. Fuente: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL y PLAN/COLA/DAG por ID.

Se implementa en Infrastructure un adaptador aislado, en memoria y de solo lectura, V5OperationalAuditIsolatedSnapshotAdapter. Captura defensivamente las colecciones de IDs, Task IDs y filas del snapshot. Compone Application/Domain ya existentes y valida SHA y revisión de fuente. Su resultado positivo exige AUTHENTICATED_EVIDENCE_REQUIRED: no acredita autenticación de GitHub, Google Sheets, CI, revisor ni estado institucional.

Pruebas xUnit reales en Infrastructure: preflight positivo sin permiso de escritura, SHA y fuente incorrectos, estado PENDIENTE, owner desconocido, auto-A5, revisor externo, ausencia de evidencia A5/CI, ID desconocido, catálogo incompleto, mutación de colecciones y entrada nula. La autorización de un tenant nunca se obtiene de los datos caller-supplied del snapshot.

No hay persistencia SQL, EF/MySQL, migraciones reversibles, acceso HTTP, cambios de permisos, acceso a archivos, nuevas colas, Tasks, webhooks, ni certificación de las 2.340 filas. El alcance autorizado permite adaptador aislado en lugar de migración real; una integración externa posterior requiere orden, aislamiento tenant y pruebas propias.

A0: revisión Domain/Application/Infrastructure y diff. A1: xUnit positivos/negativos en CI. A2: denegación de autoaprobación y mutaciones. A3: build/test .NET10 reales. A4: Scope Lock, provenance V5 exact SHA, fast-forward CAS y readbacks. A5: otra Task Javier distinta del autor. CI queued/in_progress/cancelled/skipped/failed no equivale a PASS. Rollback: git revert normal. Nunca QA/main/PROD o secretos.
