# SOLQARYN V5 FINAL — C02-04.M03.02: aplicación de preflight de consola global

**Autor:** PRIMARY DEV 06, Task `6af16bbad26881918c283abe06e1f32a`. **Rama única:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Ámbito:** caso de uso puro en Application .NET 10 para consola global auditada; sin permisos operativos nuevos, infraestructura, datos, secretos ni QA/main/PROD. **Fecha:** 2026-11-15.

## Implementación real

`V5GlobalControlPlaneApplicationPreflight` transforma una intención de lectura acotada en una evaluación *no autorizante* de `V5GlobalControlPlaneDomainRules` (C02-04.M03.01). Los únicos tipos de operación son lectura metadatos del propio tenant, directorio de plataforma y metadatos de acceso excepcional sujeto a lease. La entrada `ReadIntent` **no contiene flags de JWT, RBAC, grants, MFA, pertenencia ni auditoría que el cliente pueda usar como autoridad**; esos campos pertenecen a un `TrustedSnapshot` que necesitará ser construido y reválidado por un adaptador backend seguro en otra microtarea. La coincidencia del tenant solicitado se utiliza **sólo para negar discrepancias**, nunca para elegir tenant.

El servicio impone hora UTC del servidor, ID de tenant seguro, rechazo de motivos inesperados para lecturas ordinarias, y delega a Domain las comprobaciones de rol, membresía, MFA, reautenticación, permiso de plataforma distinto del ERP, motivo acotado, duración de lease excepcional, revocación, aprobador independiente, tenant A/B, auditoría e intentos de exportación/mutación/lectura sensible. Una evaluación favorable produce `TRUSTED_RUNTIME_ENFORCEMENT_REQUIRED`, conserva `RequiresLiveSecurityChecks=true` y **`GrantsAccess=false` siempre**. No emite token/grant, no consulta contenido ni escribe auditoría.

## Nueve tests xUnit implementados

Las nueve pruebas verifican: (1) preflight válido sin permiso; (2) tenant B falsificado/ausencia evidencia; (3) reloj no UTC e intención nula; (4) client authority, mutación/exportación y datos sensibles; (5) MFA, reauth y rol; (6) plataforma separada de ERP; (7) excepción con razón, expiry y aprobador independiente; (8) revocación y auditoría ausente; (9) motivo inesperado y operación desconocida. Son pruebas de clase pura y resultado de CI **no se declara PASS antes de conocer el SHA y GitHub Actions finalizados**.

## Alcance de seguridad y límites

No se registra DI, controller, endpoint HTTP, policy JWT/RBAC, proveedor MFA, query SQL, grant persistido, webhook, worker, auditoría crítica en vivo, ni integración de acceso excepcional real. Es un paso Application aditivo y deliberadamente sin ruta ejecutable. Nunca convertir el resultado de preflight en permiso HTTP: requerirá adaptador servidor confiable, lectura actual de grants, MFA real, auditoría persistida y pruebas A/B HTTP/EF adicionales antes de cualquier activación.

**Dependencias:** C02-04.M02.01–06 LISTO+A5 y C02-04.M03.01 LISTO+A5, DAG F=VALIDADO/G=SI y COLA G/O=SI al claim. **A0:** preflight identidad/archivos/autorización. **A1:** nueve tests xUnit positivos/negativos cuando se ejecuten. **A2:** aislamiento/denegación y no concesión, incluidos roles ERP vs plataforma. **A3:** compilar Domain+Application y pruebas xUnit pertinentes en GitHub Actions; MySQL/Angular/Playwright NO_APLICA a este diff sin EF/UI. **A4:** publicar fast-forward/CAS, Scope Lock, V5 CHANGELOG, backend y seguridad pertinente `completed/success` sobre SHA causal, readback PLAN/COLA/BITACORA; si CI pending mantener VALIDANDO. **A5:** otra Task Javier distinta del autor.

**Rollback:** git revert aditivo sobre commit, test dirigido y readback posterior. Ninguna promoción QA/main/PROD, instalación de secretos, cuenta ni cambio de protección de ramas.
