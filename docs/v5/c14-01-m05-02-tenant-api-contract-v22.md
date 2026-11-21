# C14-01.M05.02 — contrato API/DTO y errores seguros V22

PLAN D2043 exige ejecutar contrato API/DTO y validar errores seguros. El incremento es estrictamente **test-only en frontend Angular 22.2.1**: `HttpClient` y `HttpTestingController` de Angular corren realmente en GitHub-hosted, pero el transporte HTTP está simulado. No hay API real, JWT firmado, MySQL físico ni operación productiva en esta suite. La meta de fase M05 es acumulativa y no se certifica aquí.

## A0–A4
- A0: nueva suite de seis escenarios más workflow GitHub-hosted por SHA y documentación; ningún cambio runtime, backend, interfaz pública DTO ni migración. Riesgo R2 por privacidad y tenancy.
- A1: GET `/empresa-configuracion/tenant/{id}` y DTO positivo, nombre/logo, sin petición pública; cambio A/B con neutralización inmediata; errores HTTP 401/403/404/503; respuesta 200 con EmpresaId cruzado o marca XSS; carrera asíncrona A/B.
- A2: el **consumidor** `EmpresaIdentidadService` rechaza DTO cruzado y no copia RTN/correo. Los tests no prueban server-side JWT/RBAC/revocación de sesión; no confundir contrato simulado con aislamiento productivo.
- A3: NO_APLICA MySQL/Oracle EF/Chromium y HTTP de red real a esta prueba test-only; la integración con backend firmado queda explícitamente pendiente de aceptación de fase y pruebas causales en scopes correspondientes.
- A4: workflow `.github/workflows/v22-c14-01-m05-02-tenant-api-contract.yml` sobre GitHub-hosted Node 24/npm 11 y `ng test`, lint, build. Exigir exact-SHA `completed/success`, además de Scope Lock, V5 y checks pertinentes; FAIL/SKIPPED/IN_PROGRESS no PASS.
- A5: revisión independiente por Task Javier diferente al autor después de LISTO dev, sin autoaprobación.

Rollback: commit revert fast-forward del test/workflow/documento, preservando trazabilidad histórica y BITACORA. QA/main/PROD, secretos, pagos y datos reales fuera de alcance.
