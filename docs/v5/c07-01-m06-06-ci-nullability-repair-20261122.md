# C07-01.M06.06 — reparación causal CI (2026-11-22)

## Alcance y causa
Scope R2, branch dev, owner histórico DEV01. El job GitHub-hosted 137372857419 del SHA d1f46c3b1dcca327fa836b704520b9e2c244ee7c falló por seis diagnósticos warnings-as-errors: CS8602 en ProductoVarianteService.cs:95 y cinco CS8601 en C0701M0606ProductosRealHttpMysql84Tests.cs al usar CategoriaId nullable.

## Incremento
La variante post-transacción debe existir antes de recuperar DTO desde repositorio. Los productos de tenant A y B deben poseer CategoriaId persistida antes de emitir solicitudes HTTP válidas; la prueba IDOR conserva categoría legítima de B y la autorización del backend sigue siendo la frontera efectiva. Sin cambios de JWT, RBAC, migraciones ni ambientes. Sin mocks certificados como integración.

## Comprobación y continuidad
A0: diff localizado y diagnóstico exacto. A1–A3: reejecutar en GitHub-hosted .NET 10, JWT firmado tenant A/B, MySQL 8.4 efímero, casos positivos/negativos. A4: exigir Scope Lock, V5 y checks causales completed/success del nuevo SHA. No contar skipped/failed/in_progress como PASS. Mantener PLAN H1045 EN_PROGRESO, owner DEV01 y candado V17 hasta la verificación de toda la fase; A5 independiente posterior. No QA/main/PROD.

CHANGELOG_AI.md no modificado porque su contenido histórico extenso no pudo recuperarse íntegro con el conector; no sobrescribir ni truncar ese archivo.
