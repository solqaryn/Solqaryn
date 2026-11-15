# SOLQARYN V5 C05-04.M03.03: autorización y tenancy de preflight de ligereza

Autor: PRIMARY DEV 02, Task 6af16bb9492c8191b8e8c7d606f11507. Repo solqaryn/Solqaryn@dev; PROJECT_SCOPE_LOCK=STRICT; fuente Standard de Implementación de Automatizaciones V5 FINAL.

## Implementación real

Se añade V5UiLightnessAuthorizationGate en Application .NET 10, con IV5UiLightnessAuthorizationEvidenceReader para hechos obtenidos exclusivamente por backend. Combina identidad ContextoTenantActual verificada, pertenencia y RBAC actual, SHA fuente y ownership persistido del recurso, segregación de caché por tenant e invalidación tras revocación. Reutiliza V5UiLightnessApplicationService y las reglas V5UiLightnessDomainRules existentes; no duplica sus presupuestos ni relaja los límites del frontend.

Se rechaza antes de I/O: tenant B solicitado por actor A, SHA/ID inválido, bypass de caché, precarga privada, supresión de 401/403, captura de telemetría sensible y tenant elegido por cliente. Se rechazan luego evidencia ausente/obsoleta, permisos revocados, actor/propietario discordantes, caché entre tenants y falta de invalidación. El resultado nominal AUTHENTICATED_LIGHTNESS_ADAPTER_REQUIRED solo indica un requisito pendiente: GrantsAccess, GrantsCrossTenantCacheRead, GrantsPrivatePreload y CertifiesCoreWebVitals siguen false.

## A0-A5 y pruebas

A0: diff Application + 10 pruebas xUnit + documento + CHANGELOG autocontenido; preservar arquitectura y Scope Lock. A1: pruebas positivas y negativas de autorización y error, en GitHub Actions del SHA publicado. A2: rechazo tenant A/B, RBAC/membresía revocados, propiedad persistida, claves de caché y privacidad; no inventar certificación HTTP/JWT. A3: build .NET 10 Release, xUnit y security/auth gates pertinentes; MySQL, Playwright, Angular y CWV no aplicables al diff sin frontend/DB, pero continúan pendientes para etapas que los requieran. A4: publicación dev fast-forward/CAS, Scope Lock/V5, backend/auth/security SUCCESS del SHA exacto o equivalencia auditada, readback PLAN/COLA/BITACORA. Mantener VALIDANDO si CI requerida queued/in_progress/cancelled/skipped/failed. A5: revisión independiente por Task Javier distinta del autor antes de liberar sucesoras.

Sin DI HTTP, endpoint, proveedor real, EF/MySQL, caché runtime, Core Web Vitals medidos, migraciones ni cambio a QA/main/PROD. Rollback git revert normal en dev; conservar BITACORA e historia.
