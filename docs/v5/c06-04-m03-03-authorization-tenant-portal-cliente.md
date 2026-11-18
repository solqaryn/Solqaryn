# SOLQARYN V5 — C06-04.M03.03: gate de autorización/tenancy de portal cliente

**Autor:** PRIMARY DEV 06 Javier, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Scope:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo V15:** R2 por identidad cliente, JWT, recursos privados y tenant. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL.

## Entrega real

Se añade `C0604PortalClienteAuthorizationGate` en Application .NET 10, frontera `IC0604PortalAuthorizationEvidenceReader`. Pide evidencia confiable desde backend para SHA de código, actor verificado, empresa, cliente, cuenta, token y JWT validados, membership y permiso actuales, ausencia de revocación y auditoría disponible. Evita que hints/roles cliente modifiquen autoridad, y comprueba consistencia de snapshot con las identidades autenticadas antes de delegar al servicio Application `C0604PortalClienteApplicationService` y reglas Domain `C0604PortalClienteDomainRules` existentes. Reutiliza las denegaciones de propiedad A/B, cliente A1/A2, sesión caducada y revisión obsoleta.

El resultado positivo `AUTHENTICATED_CUSTOMER_RUNTIME_ADAPTER_REQUIRED` significa **solo prerequisito**, nunca autorización. `GrantsPrivateRead`, `GrantsCrossTenantRead`, `GrantsWrite`, `IssuesJwt`, `AuthorizesOrder` y `CertifiesLiveAuthentication` son siempre false. Se evita integrar endpoints, DI, persistencia, entrega de recursos privados, pagos, cuentas, workers o credenciales; el test no demuestra autenticación real de un JWT, hash ni aislamiento HTTP.

## Criterios A0–A5

- A0: scope individual M03.03 autorizado por M02.01–06+A5 y M03.01–02+A5, sin otro scope incluido; diff solo Application/xUnit/documento/CHANGELOG.
- A1: nueve xUnit ejecutables con caso positivo no autorizante, entradas inválidas, actor/SHA/tenant spoofing, permisos revocados, propiedad de recurso, versionado y errores/cancelación.
- A2: puerta fail-closed para tenant A/B, permisos CUSTOMER, grants actuales, token y revocación, PII en telemetría y override; evidencia de pruebas *sintéticas* no certifica JWT HTTP real.
- A3: ejecutar en GitHub Actions .NET Release, xUnit y seguridad/autorización pertinentes. EF/MySQL y Playwright **NO_APLICA al diff sin persistencia ni UI**; no atribuir PASS al proveedor externo.
- A4: `verify-solqaryn-project-scope`, procedencia V5, backend/security/auth `completed/success` sobre SHA exacto o equivalencia auditada de blobs con readback PLAN/COLA/BITACORA. Mientras CI en progreso, queda VALIDANDO.
- A5: una Task Javier distinta revisa independientemente requerimientos, código, pruebas y regresiones; puede emitir A5_FAIL.

**Rollback:** commit `git revert` en dev preservando HEAD concurrente, sin force-push ni tocar QA/main/PROD, secretos, datos productivos o migraciones.

## Reparacion correctiva P2: identidad de actor sin whitespace ni formatos invisibles

La revision A5 independiente de DEV02 encontro que SafeActorId rechazaba solo controles
y longitud invalida, pero permitia blancos y separadores Unicode en VerifiedActorId.
Se endurece el preflight: rechaza valor nulo, vacio, solo espacios, cualquier
whitespace/control y caracteres Unicode de categoria Format (p. ej., zero-width).
Conserva identificadores no vacios de hasta 128 caracteres, caso positivo y
los gates de pertenencia tenant A/B, revocacion y JWT/permiso de backend.

Pruebas dirigidas xUnit: ocho variantes adversariales (espacios, tab,
EM SPACE, separador de linea, zero-width space/joiner y actor con espacio)
mas fronteras de 128/129 caracteres; las nueve pruebas originales siguen
sin relajacion. Esto protege un prerequisito de Application R2, **no certifica**
autenticacion JWT HTTP real, MySQL ni autorizacion de lectura privada.
A0--A4 requieren GitHub Actions por SHA causal y A5 nueva de otra Task Javier.
Conservar A5_FAIL historico y propiedad de DEV06. Rollback por git revert
normal tras comprobar HEAD; no QA/main/PROD, secretos ni migraciones.

