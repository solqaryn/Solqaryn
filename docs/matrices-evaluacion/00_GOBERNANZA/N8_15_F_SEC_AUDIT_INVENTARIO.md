# N8.15.F — SEC_AUDIT inventario

Estado: `LISTO_REAL`

Baseline de entrada: `d92bffb39d83a5ac6034d18fda269e0515034d68`.

## Authn / authz

`Program.cs` configura JWT Bearer con validación de issuer, audience, lifetime y signing key, `ClockSkew=0`, y rechaza un `Jwt:Secret` placeholder o menor de 32 bytes. `Jwt:Issuer` y `Jwt:Audience` son obligatorios. La autenticación/authorization middleware se aplica antes del mapping normal de controllers.

Login tiene rate-limit fixed-window por IP mediante policy `AuthLogin`; CORS exige lista explícita no vacía de origins. Swagger queda habilitado sólo en Development o por flag explícito.

## RBAC / tenant

- Frontend protegido usa `authGuard` + `permisoGuard` y metadata de módulo/acción.
- `permisoGuard` revalida contexto tenant/empresa de forma asíncrona y falla cerrado hacia login cuando no puede verificarlo.
- El guard es sólo UX/navigation control; backend sigue siendo frontera de autorización.
- Backend registra `ICurrentUserService`, `IUsuarioScopeService`, repositorios de `RolPermiso`, `Rol`, `Permiso` y servicios de permiso/RBAC.
- `AppDbContext` y servicios de dominio contienen invariantes adicionales; por tanto tenant/business rules no deben depender del cliente.

## Auditoría / logging / observabilidad

- Middleware inventariado: `CorrelationIdMiddleware`, `RequestObservabilityMiddleware`, `ExceptionHandlingMiddleware`.
- `Program.cs` registra `RequestObservability` y `IAuditoriaService`/`IAuditoriaRepository`.
- Fallo de autenticación por token expirado intenta registrar auditoría sin exponer el token.
- Health endpoints: `/health` y `/health/ready` forman parte del mapa operativo.
- Security headers explícitos: `X-Content-Type-Options=nosniff`, `X-Frame-Options=DENY`, `Referrer-Policy=no-referrer`, `Permissions-Policy` restrictiva; HSTS fuera de Development y HTTPS redirection.

## PII / secretos

- Config versionada se inventaría por claves, no por copiar valores secretos.
- Secretos/credenciales reales permanecen fuera del repositorio según autoridad.
- PII debe clasificarse por DTO/campo/matriz en N8.17; N8.15 no inventa sensibilidad sin inspección campo-a-campo.
- Logs/observability no deben serializar auth headers, tokens, passwords ni secretos; cualquier sink/formatter se mantiene `UNKNOWN` hasta inspección específica si no está materialmente visible en el baseline.

## Reglas críticas confiadas sólo al cliente

No se detectó una regla de negocio **certificada** como client-only a partir de las rutas/guards inspeccionadas: el propio contrato arquitectónico exige backend authority y `AppDbContext`/Application implementan invariantes. Sin embargo, toda regla visible sólo en formulario/guard que no tenga correlato backend debe permanecer candidato de hallazgo en N8.17/N8.19; no se declara segura por presencia del guard.

Storefront `SOLQARYN/**` es navegación pública por diseño de rutas; seguridad se decide en sus APIs. No se clasifica como bypass sin evidencia de un endpoint sensible sin enforcement.

## Clasificación

- JWT/Auth middleware: KEEP.
- RBAC/tenant backend services: KEEP.
- frontend guards: KEEP como UX, nunca como autoridad exclusiva.
- audit/correlation/observability/health: KEEP.
- config con placeholders/secret keys: KEEP estructura; valores secretos fuera de repo.
- reglas client-only no corroboradas: UNKNOWN hasta trace completo.
- `REMOVE_SAFE=0` en F.

## REVIEW_FIRST

P0=0, P1=0. La revisión no encontró un bypass demostrable en los elementos inspeccionados ni introdujo cambios funcionales. Ningún control frontend se promocionó a frontera de seguridad.

## Resultado

`N8.15.F = LISTO_REAL`.

Siguiente dependency-valid: `N8.15.G — TEST_CI`.
