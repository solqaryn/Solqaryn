# C10-04.M05.05 — Casos negativos de seguridad, aislamiento y abuso

Solqaryn dev, PROJECT_SCOPE_LOCK=STRICT. Autor DEV01, TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a. Riesgo R2 por tenant, RBAC, permisos, versión y cargas hostiles. Alcance exclusivamente M05.05: casos negativos y ausencia de efectos, no restauración ni certificación de toda M05.

## A0 — diff y rollback
Reglas actuales de C1004CacheJobsAuthorizationGate en Application (sin modificaciones) y su fuente IC1004VerifiedWorkEvidenceSource. Solo se agregan nueva suite xUnit, workflow GitHub-hosted, documento y sección CHANGELOG autocontenida. No se cambia API, Domain, Infrastructure, MySQL, JWT, proveedores, workers, frontend, Aiven/Render/Vercel o producción. Reversión normal git revert del SHA exacto.

## A1 — oráculos adversariales
Nueve combinaciones de política para denegación: membresía revocada/inactiva, permiso de operación faltante, service actor sin grant, actor/tenant persistido diferentes, versión obsoleta, deadline vencido y reintento agotado. Entrada hostil inválida se rechaza ANTES de consultar la fuente; excepción del proveedor con marcador de secreto se redacta; tenant hint B no suplanta A; backup.restore/delete y export general no allowlisted; snapshot ausente deniega; cancelación no consulta fuente. Todos los resultados imponen seis propiedades de NO ejecución/NO grant. Se reejecutan los doce Facts de regresión previos de la barrera.

## A2 — límites de autenticación/tenancy
Se prueban las clases reales net10.0, pero la fuente es simulada: nunca se ha demostrado JWT firmado, claims A/B reales, MySQL aislado ni acceso a Cloudflare R2. Se verifica que un DTO de preflight no puede adquirir lease, encolar, escribir, leer artefactos privados, declarar seguridad runtime ni otorgar provider grant. No es autorización R3 ni sustituto de controles del backend/HTTP.

## A3 — pruebas pertinentes
xUnit + build Release en runner GitHub-hosted .NET 10, controles backend, seguridad, Scope Lock y V5 sobre SHA. MySQL 8.4, EF, Playwright, restore durable y proveedor no aplican a este DIFF test-only, pero sí siguen pendientes de aceptación de fase M05 completa.

## A4/A5 — cierre
CHECK RUN exact-SHA completed/success, pruebas nuevas + previas y validación readback PLAN/COLA/DAG/BITACORA. QUEUED/IN_PROGRESS/FAILED/SKIPPED nunca PASS. A5 por Task Javier distinta del autor, no por DEV01; sin QA/main/PROD, secretos ni fórmulas alteradas.
