# SOLQARYN V22 — C04-01.M03.03: JWT/RBAC y tenant de catalogo SaaS (DEV)

**Alcance:** PLAN_MAESTRO!D592 = "Implementar autorización, tenancy y denegación por defecto"; E es objetivo acumulativo de M03. Autor DEV03, V17 2000000592, PROJECT_SCOPE_LOCK=STRICT, riesgo R2. No hay autorización R3/pagos ni modificación QA/main/PROD.

## A0 e impacto

Añade un adaptador de solo lectura en la capa API Security, registrado en DI sin controlador público. Usa JwtBearer AuthenticateAsync efectivo, claim `id` único, identidad de `ICurrentUserService`, tenant del selector, `IUsuarioScopeService` vigente, rol empresarial activo en MySQL y `RolPermiso` + `Permiso` activos para Configuracion/Ver (sin grants por claims de rol). Repite membresía, rol y permiso para limitar revocaciones durante la lectura. Devuelve solo un `AuthenticatedActor` verificable para el contrato Application M03.02, no una decisión de uso, reserva, factura, cobro o escritura. La marca de tenant no se sobreescribe ni se infiere de `X-Empresa-Id`.

## A1/A2/A3 — adversariales y fronteras

TestServer HTTP real + JWT HMAC firmado y validado por JwtBearer + Oracle EF/MySQL 8.4 efímero de GitHub-hosted. Tests: anónimo 401, firma alterada 401, flag OFF 403, tenant A válido 200 bajo lectura aislada, B ajeno 403 aun con X-Empresa-Id falsificado, revocación RolPermiso 403. El rol debe pertenecer a empresa y seguir activo; la fuente de MySQL se reconsulta en cada llamada y falla cerrada ante error/cancelación. El controller de prueba SOLO existe dentro del assembly de tests. La configuración `PlanCatalogAuthorization:EnableReadOnlyProbe` es OFF si ausente; no se abre ninguna ruta SaaS de producción y P0 de cuarentena permanece.

## A4/A5 y LIMITACION

Validar GitHub Actions exact-SHA: `verify-solqaryn-project-scope`, procedencia V5, .NET Release+xUnit, seguridad/autorización y workflow dirigido `C04-01.M03.03 - real signed JWT HTTP MySQL 8.4` con al menos 1 test Integration efectivamente ejecutado, 0 fallos y 0 omitidos. Solo si todos los checks pertinentes concluyen SUCCESS y readback institucional es correcto cabe VALIDANDO->LISTO; otros QUEUED/IN_PROGRESS/FAIL no son PASS. A5 posterior independiente de DEV03 y cualquier reparador.

**LIMITACION:** aún no hay lector MySQL de catálogo de planes ni enforcement de cuota transaccional integrado en endpoints de uso; M03.04+ debe resolverlo. El resultado del adaptador no permite activar suscripciones, cobrar ni afirmar que toda M03 está certificada. Rollback mediante git revert normal del commit + bitácora idempotente, nunca force-push ni borrar registros.
