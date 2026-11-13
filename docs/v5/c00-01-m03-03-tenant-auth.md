# C00-01.M03.03 — Tenant header fail-closed y membresía autorizada

**Plan V5 FINAL:** `C00-01.M03.03`; ámbito `solqaryn/Solqaryn@dev`; dueño PRIMARY DEV 03 — JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. Este documento describe solo el cambio de autorización de la selección tenant y no declara un cierre de M03 ni autorización para QA/main/PROD.

## Riesgo, frontera y cambio

`TenantPermissionContext.RequireEmpresaIdAsync` admitía un selector explícito inválido/duplicado como ausencia de cabecera, pudiendo caer a `ObtenerUnicoActualAsync` y seleccionar implícitamente la empresa A. El selector sigue sin ser autoridad: `RequiereMembresiaEmpresaAttribute` verifica identidad JWT y membresía vigente, y `RequierePermisoAttribute` mantiene la comprobación RBAC. El cambio de seguridad impide que una cabecera explícita malformada o ambigua active fallback, sin alterar un selector positivo válido ni el fallback cuando la cabecera está realmente ausente.

**Archivos:** `backend/src/API/Filters/TenantPermissionContext.cs`, `backend/tests/Solqaryn.Tests/API/V5C0001M0303TenantHeaderFailClosedTests.cs`, este contrato y `CHANGELOG_AI.md`. Sin migraciones, SQL, entidades, DTO, Angular, configuraciones productivas, secretos ni cambios de proveedor.

## Pruebas dirigidas y evidencia exigida

- Positivo: selector único 17 + membresía activa A autoriza; sin cabecera, el único contexto verificado mantiene compatibilidad.
- Negativos: vacío, blanco, no numérico, cero, negativo, valor sobre rango, valor combinado `17,18` o dos valores `17`/`18` se deniegan antes de invocar fallback. Empresa B (18) sin membresía no autoriza aunque exista sesión A.
- Integración: compilación .NET 10 y xUnit real por GitHub Actions `solqaryn-core-ci.yml` y `dev-ci.yml`; seguridad tenant por core y suite MySQL 8.4 por el impacto de `backend/src/API/Filters/` si el clasificador realmente la activa. Inspeccionar jobs, logs y SHA exacto. Un job en curso/SKIPPED no es PASS.
- A0: diff y contenido dirigido, revisión de secretos y dependencias; A1 funcional positivo/negativo; A2 aislamiento JWT/RBAC/tenancy y rechazo B; A3 compilación y tests; A4 SHA publicado, checks y readback. Ningún PASS se reclama antes de existir evidencia. A5 corresponde a otro TASK_ID.
- Rollback: `git revert <SHA-publicado>` tras reconciliar HEAD compartido, preservando historia institucional y work concurrente. Sin force-push, PR, ramas, QA/main/PROD ni cambios reales a Aiven.
