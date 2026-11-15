# SOLQARYN V5 — C02-04.M03.03: autorización y tenancy de consola global

Autor: PRIMARY DEV 04 JAVIER; TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. Fuente: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL; repositorio solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT.

## Implementación de código real

Se añade V5GlobalControlPlaneAuthorizationGate en Application .NET 10 con IV5GlobalControlPlaneVerifiedEvidenceReader. La evidencia debe proceder exclusivamente de backend y comparar SHA, actor, tenant y permisos actuales; los parámetros cliente no prueban identidad ni grants. Operaciones peligrosas (mutación, exportación, datos sensibles, sugerencias de autoridad cliente) son rechazadas antes de leer proveedor. Se reutilizan el preflight Application M03.02 y Domain M03.01 para imponer MFA, reautenticación, grants de plataforma independientes del ERP, auditoría, revocación y aprobación excepcional independiente con lease acotado.

Respuesta positiva AUTHENTICATED_GLOBAL_CONTROL_PLANE_ADAPTER_REQUIRED es solo una condición preliminar: GrantsAccess, GrantsTenantEnumeration, GrantsCrossTenantRead, GrantsInstitutionalWrite y GrantsExceptionalLease son siempre false. No hay autorización runtime ni acceso a datos. La interfaz de lectura es inerte; NO se implementa JWT/HTTP, EF/MySQL, MFA real, DI, grants, endpoint, auditoría persistida, worker, ni modificación de QA/main/PROD.

## Pruebas y aceptación

Siete métodos xUnit cubren positivos y negativos: lectura propia que nunca concede permisos; solicitudes nulas/operación/SHA/flags inseguros; evidencia caducada, UTC y revocación; tenant A/B, actor y MFA; grants de plataforma; excepción con aprobador; cancelación. Pruebas reales declaradas pendientes hasta confirmar GitHub Actions del SHA.

A0 verifica arquitectura y diff; A1 pruebas xUnit; A2 tenant A/B, revocación, MFA, roles, auditoría; A3 build Release y seguridad pertinente; A4 Scope Lock, procedencia V5, GitHub Actions y readback exact-SHA o equivalencia demostrada. A5 independiente por otra Task Javier; ninguna autoaprobación. Reversión con git revert normal, preservando BITACORA append-only.
