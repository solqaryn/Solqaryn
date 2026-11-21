# C07-01.M05.01 — aceptación unitaria reproducible de catálogo V22

PLAN D1034: Ejecutar prueba unitaria o de regla con resultado reproducible. La meta E Completa M05 es acumulativa, no equivale a certificar M05.02–M05.06.

A0: riesgo R1 test-only; tres archivos aditivos xUnit, workflow y este documento, sin modificar runtime, API, DTO, migración, JWT, RBAC o UI. Depende de las reglas de dominio C0701CatalogDomainRules actuales.
A1: Xunit .NET 10 real con positivo, negativos y límites exactos SKU 3/64 y nombre 2/150, decimales 2 posiciones y 999999999.99, impuesto 0–10000, versiones, tipo de producto y servicio, variantes y categorías.
A2: aserciones de tenant A/B, legacy sin EmpresaId, sucursal B, membresía revocada, rol inactivo y propiedad de categoría/variante; ninguna decisión de dominio habilita escritura real. Los scopes son fixtures, no JWT firmado ni middleware verificado.
A3: pruebas de unidad verdaderas en GitHub-hosted dotnet 10 Release; al no tocar integraciones o API, HTTP firmado, MySQL 8.4, Angular/Chromium no aplican al diff, NO son PASS.
A4: GitHub Actions dirigido, versión de stack, ScopeLock exact-SHA, Skill self-test, TRX >=16 tests ejecutados y todos PASS, cero failed/notExecuted, V5/procedencia y readback. Si QUEUED, IN_PROGRESS, SKIPPED o FAIL mantener VALIDANDO.
A5: revisión independiente por otra Task Javier distinta del autor DEV01, con diff y pruebas genuinas. Handoffs no certifican toda M05 ni QA/main/PROD. Rollback git revert normal.
La fuente CHANGELOG_AI.md supera el límite de lectura del conector; conservar el blob íntegro sin edición/truncamiento y comprobar el gate V5 del SHA real. No sustituir su historial por extractos.
