# C01-02.M05.02 — contrato API/DTO real y errores seguros — V22

Repositorio: `solqaryn/Solqaryn@dev`. Unidad del PLAN D207: **Ejecutar contrato API/DTO y validar errores seguros**. Actor DEV05 (`6adc2e55f47c81919f6374e561687076`), riesgo **R2** por JWT/autorización HTTP; `PROJECT_SCOPE_LOCK=STRICT`. Esta entrega NO completa la fase M05 ni autoriza QA/main/PROD.

## A0 — alcance y compatibilidad

Se prueba el **controller ya registrado** `PermisosController` con HTTP real en `Microsoft.AspNetCore.TestHost`, en lugar de crear un endpoint imaginario de capabilities. El contrato existente `GET /permisos/mis-permisos` usa `[Authorize]` y `ApiResponse<MisPermisosDto>`; `GET /permisos/mis-permisos/empresa/{empresaId:int}` valida pertenencia, estado activo, devuelve 400/403 y aplica `Cache-Control: private, no-store, max-age=0`. El endpoint de capabilities comerciales `/api/v1/capacidades/evaluar` es solamente un **diseño no registrado** en `docs/v5/c01-02-m02-02-contratos-api-dto-capabilities.md`; el test verifica 404 y no declara que exista ni que conceda permisos.

Archivos afectados: únicamente nueva suite de pruebas API/DTO, este documento y `CHANGELOG_AI.md`. Sin cambios de runtime, EF schema, migraciones, endpoints, JWT productivo, configuración, frontend, usuarios, secretos o pagos. Consumidores: los clientes actuales de `MisPermisosDto` y el formato JSON `ApiResponse<T>` se protegen sin alterarlos.

## A1/A2/A3 — pruebas causales adversariales

Suite `backend/tests/Solqaryn.Tests/API/C0102M0502PermissionsHttpContractTests.cs` ejecutada mediante GitHub-hosted .NET 10: host HTTP real con routing MVC y `JwtBearer` criptográfico de claves efímeras ficticias. Exige respuestas HTTP 401 para ausencia de JWT, token malformado, firma incorrecta y expiración; 200 y JSON del sobre para token válido; 400 sin `data` para ID inválido; 403 sin datos de permisos para tenant ajeno/suspendido; 200 de tenant propio con `Cache-Control`; y 404 de la API futura no registrada. El servicio de permisos se sustituye por un fake Moq **deliberado** y `Empresa` usa EF InMemory, de modo que NO se presenta el fake como membresía SQL de producción ni como certificación de tenant A/B runtime. La prueba de routing/auth del host es real; el almacén de datos en esta unidad **NO es MySQL 8.4**.

Limitación exacta: la microtarea verifica contrato/errores de **API existente**; NO implementa `IC0102VerifiedCapabilityPolicyReader` productivo, catálogo/entitlements persistentes, RBAC relacional auténtico ni endpoint comercial futuro. El ciclo de implementación y aceptación completa M05/M06 requiere pruebas y trabajos propios, no se certifica por este incremento. No afirmar `HTTP/JWT/MySQL` **productivos** mediante TestServer/Moq. Ninguna decisión positiva del fake concede acceso real.

## A4/A5 — publicación, validación y rollback

Exigir check-runs `completed/success` exact SHA de backend Release/xUnit, seguridad pertinente, `verify-solqaryn-project-scope` y V5/CHANGELOG. `SKIPPED/CANCELLED/FAIL/IN_PROGRESS` requeridos no son PASS. Verificar diff y ejecución de casos por GitHub-hosted, readback institucional CAS/PLAN/COLA/BITACORA. Solo con A0–A4 proporcionales cerrar `VALIDANDO->LISTO`; A5 por otra Task Javier distinta de autor y reparador. Rollback: revert normal del commit en `dev` (sin force-push ni borrar eventos BITACORA).

## Reparación causal de CI — 20 de noviembre de 2026

La primera entrega SHA `70773727479bcd689aa05aa84582c1ec63ba2360` ejecutó en GitHub Actions **6179/6180** xUnit en Backend Release (job 136720479368) y **2100/2101** en el job de seguridad (136720479712). Una prueba propia falló porque comparaba literalmente la serialización de `Cache-Control` en orden de directivas: esperaba `private, no-store, max-age=0`; el cliente HTTP real normalizó `no-store, max-age=0, private`, semánticamente igual. Fallo P2 de aserción textual, no degradación de autorizaciones ni ausencia de cabeceras.

Se cambia **sólo** la prueba para analizar `Headers.CacheControl` y exigir las tres propiedades `Private=true`, `NoStore=true` y `MaxAge=TimeSpan.Zero`, preservando negativos JWT, 403/400, JSON y 404 intactos. CI nuevo SHA debe demostrar ambos jobs COMPLETED SUCCESS, 0 FAIL y Scope Lock/V5; un estado QUEUED no permite LISTO. Se conserva el fallo histórico de SHA 70773727, owner DEV05 y límites explícitos de proveedor Moq/EF InMemory vs MySQL real. Sin cambios runtime ni QA/PROD.
