# SOLQARYN V5 — C01-03.M03.01: reglas Domain de configuración dinámica segura

**Ámbito exclusivo:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Autor:** PRIMARY DEV 06 Javier, Task `6af16bbad26881918c283abe06e1f32a`. **Riesgo V15:** R2, inyección/configuración/tenant/RBAC.

## Entrega real

Se implementa `V5DynamicConfigurationDomainRules` como prevalidación .NET 10 Domain pura de un catálogo previamente allowlisted, estrictamente tipado y sin efectos. Verifica autoridad que debe ser obtenida desde backend (identidad, membresía, RBAC, plan/entitlement), tenant A/B, scope plataforma separado, clases de valor Boolean/BoundedInteger/SafeString/ColorHex, claves reservadas e intentos de código/SQL/JS/CSS, valor y versión optimista. La validación usa un allowlist suministrado por una fuente autorizada: **nunca confiar en uno construido desde un DTO**, y el resultado favorable jamás autoriza escribir.

El veredicto `TRUSTED_BACKEND_AUTHORIZATION_CAS_REQUIRED` exige servicios de Application, implementación efectiva de autorización, CAS transaccional, auditoría y rollback **futuros**. `GrantsWrite`, `GrantsPlatformScope`, `OverridesRbac`, `CertifiesDurableCas` son siempre false. Ninguna tabla, EF/MySQL, endpoint, worker, feature flag operativo, secreto, Angular, JWT o sesión cambia.

## A0–A5 por riesgo

- A0: diff solamente reglas Domain + diez métodos xUnit + documento + CHANGELOG; predecesores M02 con gates institucionales reales, no autorización general de migrar.
- A1: xUnit positivos, negativos y fronteras: allowlist, tipos, boolean, límites enteros, color, string/inyección, versionado, ámbito, null, seguridad, permisos y revocación; tests capaces de fallar por defecto de código.
- A2: tenant A/B y scope PLATFORM separado; una membresía/rol/plan revocado o flag cliente jamás otorgan privilegio. No se presenta este guard puro como JWT/RBAC runtime real.
- A3: .NET Release/build y xUnit ejecutados por GitHub Actions, seguridad pertinente. MySQL 8.4 Integration y Playwright NO_APLICA al diff sin EF/DB ni UI.
- A4: Scope Lock, V5 CHANGELOG, backend/security/auth checks requeridos completed/success por SHA exacto o equivalencia verificable; readback PLAN/COLA/BITACORA.
- A5: revisión independiente posterior por Task Javier distinta, puede rechazar requisitos/cobertura y encontrar defectos.

**Rollback:** revert del commit en dev, conservando HEAD y BITACORA. Sin QA/main/PROD, secretos, migraciones o cuentas externas.

## Corrección de defecto A5 V17 (revisión independiente DEV03)
Se identificó un defecto de canonicalidad en el preflight de Domain, severidad P2: el método `approvedCatalogKeys.Contains(change.Key!)` respetaba el comparador interno de un `HashSet<string>` posiblemente `OrdinalIgnoreCase` y podía aceptar una clave con casing distinto al catálogo nominal. El resultado aún **no otorgaba autorización de escritura**, pero infringía la intersección exacta fail-closed.

Corrección propuesta y publicada por DEV03 como asistencia técnica a la entrega de DEV06 recuperada por DEV01: comparar cada llave del catálogo con `StringComparison.Ordinal`, independientemente del comparador de la colección. Añadir xUnit con conjunto case-insensitive y variante `UI.compactMode` rechazada, más control positivo con llave exacta. Exigir .NET 10 Release/xUnit, seguridad tenant, autorización, Scope Lock, procedencia V5 del nuevo SHA y **A5 nuevamente por una Task Javier distinta de DEV03 y de DEV06**. Historial A5_FAIL se conserva; no se certifica PASS por este cambio ni se ejecuta un CAS real.
