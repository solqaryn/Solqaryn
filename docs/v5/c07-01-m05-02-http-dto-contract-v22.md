# C07-01.M05.02 — test de contrato HTTP/DTO (V22)

PLAN D1035: ejecutar contrato API/DTO y comprobar errores seguros. PLAN E describe resultado acumulativo de M05, no certifica M05.03–06. Riesgo R1 **test-only**, sin endpoint productivo, cambio de runtime, datos o migraciones. Rama dev; PROJECT_SCOPE_LOCK STRICT.

A0: Se agrega un controlador exclusivo del assembly xUnit y un HostBuilder con ASP.NET Core TestServer real. El contrato HTTP usa el ClientIntent existente y mapea el preflight sin permitir lectura ni escritura. El controlador devuelve 400 ante IDs/version inválidos, 403 genérico frente a tenant distinto o autoridad inyectada y 405 para método no permitido. No revela detalles de SQL ni rol.

A1: Cuatro casos reproducibles cubren positivo 200 preparatorio (no write), negativos 400, 403, JSON malformado 400 y GET 405. A2/A3: el contrato usa dobles deterministas de actor/recurso; **no certifica JWT firmado, RBAC/tenancy real, MySQL 8.4 ni endpoint productivo**. Esto es una limitación registrada, no una afirmación de PASS de seguridad/integración. Dichas certificaciones son responsabilidad de M03.03/M03.04/M05.03/M05.05.

A4: GitHub-hosted .NET 10 Release prueba HTTP/TestServer y TRX 4/4, verificando Scope Lock y skill exact-SHA; CI global aplicable y V5 procedencia deben ser SUCCESS antes de LISTO. Al publicar, mantener VALIDANDO mientras CI esté en curso; A5 corresponde a otra Task Javier independiente. Rollback: git revert normal de tres archivos nuevos, sin sobrescribir otra Task. CHANGELOG_AI.md masivo se conserva sin cambios ante riesgo conocido de truncamiento; V5 de encabezados sigue exigido. Sin QA/main/PROD, pagos ni secretos.
