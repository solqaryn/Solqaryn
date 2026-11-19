# C03-04.M05.02 — contrato de DTO y errores seguros (.NET 10)

**Microtarea V5 M05.02; R2 por incidentes/JWT/RBAC/tenant.** El código usa realmente `V5ContinuityIncidentApplicationService.Request` y `Result` con `System.Text.Json` en `backend/tests/Solqaryn.Tests/Application/C0304M0502IncidentDtoContractTests.cs`. Cubre serialización camelCase, efectos siempre falsos, valores inyectados, SHA causal, evidencia/URLs sensibles, A/B, pertenencia/revocación, errores de estado, restore y errores JSON. La ejecución real de estas pruebas y TRX >=18 con cero FAIL/SKIPPED corresponde al workflow GitHub-hosted dedicado, exclusivamente cuando CI acredite su SHA exacto.

No modifica API, aplicación productiva, controladores, rutas, DI, MySQL/EF, migraciones, JWT firmado, proveedores ni datos. Ni un DTO deserializado ni las banderas de la fixture demuestran autoridad JWT/RBAC real. El servicio `Prepare` siempre exige adaptador autenticado antes de cualquier operación; `AllowsIncidentMutation`, `AllowsBackupRestore`, `AllowsEvidenceDisclosure` permanecen falsos.

**A0**: diff/alcance de test de contrato con fuente ya existente. **A1**: pruebas positivas/negativas de serialización y rechazo, pendientes de GitHub Actions. **A2**: fixtures de tenant A/B, permisos y revocación, nunca sustituto de JWT firmado/HTTP. **A3**: .NET Release/xUnit real en CI; MySQL de incidentes, proveedor de restauración y endpoints no probados. **A4**: Scope Lock, V5, SHA exacto y readback PLAN/COLA/BITACORA. Mantener VALIDANDO hasta verificar todos los gates R2 pertinentes; no fabricar A0–A4 completos ni A5. Revisión A5 independiente por otra Task Javier autorizada, después de certificación suficiente.

Rollback normal `git revert` si se confirma defecto, sin `force-push`, sin QA/main/PROD ni secretos.
