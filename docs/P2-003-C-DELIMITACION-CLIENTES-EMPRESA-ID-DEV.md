# P2-003.C — Delimitar clientes por EmpresaId (DEV)

- **Scope**: P2-003.C, Plan Comercial Inicial vigente, dependencia P1-016.B LISTO.
- **Acción causal única**: prueba xUnit filtrable `P2003CClientesEmpresaIdTests`, archivo `backend/tests/Solqaryn.Tests/P2003CClientesEmpresaIdTests.cs`.
- **Límite verificado**: con la marca RBAC `VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey` de A solo aparece cliente A, aunque el header `X-Empresa-Id` afirme B; al cambiar la marca a B, solo aparece B en la misma instancia de DbContext.
- **Riesgos/controles**: EF Core InMemory no acredita MySQL real, endpoint HTTP, RBAC completo ni comportamiento de rutas públicas sin marca. El modo `AuthorizedReadEmpresaId == null` del filtro actual no se modifica ni se presenta como barrera HTTP.
- **Impacto**: una sola prueba nueva; sin modificaciones a APIs/DTO, datos reales, migraciones, JWT, permisos, secretos, Angular, Vercel, Render o producción.
- **Validación requerida**: CI dirigida exact-HEAD con al menos una prueba y cero errores; tres checks requeridos GitHub; revisión independiente ajena; merge protegido y certificación institucional posterior. No declarar PASS antes de la ejecución remota.
- **Rollback**: revertir la prueba y evidencia mediante PR protegido, conservando reglas productivas.
