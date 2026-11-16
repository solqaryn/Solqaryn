# SOLQARYN V17/V15 — C15-01.M03.06 auditoría estructurada de cobertura

**Microtarea:** C15-01.M03.06, fila PLAN 2179. Autor DEV01 JAVIER, TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a. Rama solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT y candado V17 2000002179. Riesgo R2 por tenancy, trazabilidad institucional y manejo de errores.

## A0 — entrega y límites

Se implementa Infrastructure .NET 10 C1501RequirementCoverageSafeAuditSink, conectado por tipo al resultado C1501RequirementCoverageBoundedReviewWorker de M03.05. Revalida tenant verificado/recurso, sesión y membresía actuales (representadas sólo como evidencia sintética), rol vigente, revocación, SHA canónico y correlación GUID, códigos de resultado exactos, 1–16 filas, claves seguras y unicidad ordinal. Registra un evento ILogger fijo 150106 con código fijo, correlación opaca y conteo seguro, o denegación con REDACTED y cero. Si el logger falla devuelve código seguro. No registra requisito, work ID, tenant, usuario, JWT, SHA, PII, clave o excepción.

No crea endpoint, identidad JWT viva, lector real Drive/Sheets/GitHub, EF/MySQL, worker alojado ni persistencia durable. No certifica la cobertura global de 2340 IDs, A5, promociones QA/main/PROD, precios, proveedores o permisos. Los flags en memoria no sustituyen autenticación real.

## A1/A2 — pruebas adversariales reales

14 xUnit genuinas con aserciones sobre positivo no autorizante; tenant A/B, revocación/membresía/rol, origen no verificado, SHA distinto o no canónico, correlación maliciosa, código de revisión falsificado, item inyectado, duplicados de requisito/work item, lote vacío y frontera 17, identidad insegura, null, logger deshabilitado, excepción opaca, cancelación y dependencia nula. Los escenarios negativos fallarán si el sink registra secretos o concede grants. Exigir build .NET 10 Release y ejecución xUnit real, Seguridad multiempresa y Authorization GitHub-hosted por SHA causal.

## A3/A4/A5 — controles y reversibilidad

A3 es integración in-process Infrastructure con resultado M03.05 e ILogger. MySQL 8.4, EF/migraciones, API/HTTP, JWT firmado, Playwright y proveedores reales NO_APLICA para este diff aislado, no para certificación global. Publicar cuatro archivos en un commit de dev fast-forward tras candado V17; pasar a VALIDANDO con CI real que siga QUEUED o IN_PROGRESS. Sólo cerrar LISTO con A0–A4, checks exact-SHA Backend Release, backend build, Seguridad multiempresa, Authorization, Scope Lock y V5 completed/success, readback PLAN/COLA/DAG/BITACORA. A5 independiente por otra Task Javier. Rollback normal git revert preservando avances y auditoría histórica. No QA/main/PROD.
