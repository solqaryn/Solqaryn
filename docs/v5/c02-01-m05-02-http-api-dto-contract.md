# C02-01.M05.02 — contrato API/DTO y errores seguros

SOLQARYN V22 / DEV05 JAVIER / PROJECT_SCOPE_LOCK=STRICT / riesgo R2 / dev.

## A0: alcance atomico D315
Ejecutar el contrato API/DTO de TenantContextController sin cambiar su implementacion: GET /tenant-context/empresas enumera solo empresas devueltas por IUsuarioScopeService; GET /tenant-context/{empresaId} falla cerrado en identificador no positivo y en ausencia de membresia. Ningun dato de cliente es autoridad de empresa o rol.

Impacto: solo nueva suite xUnit, este documento y CHANGELOG_AI.md. No hay nuevos endpoints, DI productiva, cambios de datos, migraciones, JWT runtime, Angular ni proveedores. Rollback: git revert no destructivo en dev.

## A1/A2: aserciones reales
C0201M0502TenantContextHttpApiContractTests ejecuta HTTP real en TestServer ASP.NET Core 10 con middleware JWT Bearer firmado y validacion de firma, issuer, audience y expiracion. Comprueba 401 anonimo y firma manipulada; 400 para 0, -1, int minimo sin consultar membresias; 200 listado limitado a A y DTO A con rol server-side pese a header X-Empresa-Id, X-Forwarded-Host y claim rolId falsos; 403 opaco para B no autorizada. Moq estricto demuestra que los errores de autenticacion y rango no consultan autoridad.

## A3: limitacion causal transparente
La autoridad de membresias IUsuarioScopeService es un doble estricto de prueba. HTTP/JWT firmados si se prueban realmente; este diff TEST-ONLY no verifica UsuarioEmpresa/RolPermiso/MySQL 8.4, revocacion concurrente, proveedor remoto ni persistencia. MySQL 8.4 y Chromium son NO_APLICA para esta unidad de contrato D315 porque no cambia runtime, no para toda M05. M05.03 debe probar DB/proveedor; M05.04/.05 hacen aceptacion y negativos.

## A4/A5
No marcar LISTO hasta comprobar GitHub-hosted xUnit .NET10, backend Release, seguridad multiempresa, Scope Lock STRICT y check V5 procedencia SUCCESS pertinentes en SHA causal, ademas de PLAN/COLA/BITACORA readback y lock V17. SKIPPED/CANCELLED/IN_PROGRESS/FAIL requeridos NO PASS. A5 requiere otra Task Javier distinta del autor/reparador, sin autoprobacion ni QA/main/PROD.
