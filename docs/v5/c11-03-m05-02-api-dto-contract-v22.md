# SOLQARYN V22 — C11-03.M05.02: API/DTO y errores seguros

**Alcance PLAN D1683:** ejecutar contrato API/DTO y validar errores seguros. Riesgo R2 test-only, rama dev, Scope Lock STRICT.

Se ejecuta el controlador real \`MensajesOutboxController\` por ASP.NET Core 10 MVC TestServer con JwtBearer firmado, filtro \`RequierePermiso\`, selector X-Empresa-Id, idempotency key y DTO existente. Nueve pruebas adversariales: 401 anónimo/firma forjada; 202 DTO tenant A y clave normalizada; 403 ruta B versus permiso A; 403 permiso revocado; 400 falta clave; 409 conflicto; 400 argumento; 409 colisión de índice. Cada negativa confirma error seguro o ausencia de efecto según corresponda.

**Límite explícito:** IPermisoService e IMensajeOutboxService son dobles controlados; EF es InMemory. El handler de excepción del filtro se traduce solo en middleware de test. Esto acredita el pipeline HTTP MVC/JWT y la respuesta del controlador, **NO** acredita Oracle EF/MySQL 8.4, RBAC relacional persistido, PSP, negocio, workers, transacción real ni producción. NO_APLICA MySQL/PSP por diff test-only y PLAN D; la integración de persistencia tiene microtarea distinta M05.03.

A0: diff aditivo sin runtime, schema, DI ni API/DTO productivos cambiados. A1: positivos, negativos, límite y error seguros reales por HTTP. A2: anónimo, firma falsa, revocación simulada y selector tenant A/B; distinguir permisos simulados de seguridad de base real. A3: TestServer real y MVC filtros. A4: GitHub-hosted exact-SHA Release xUnit >=9/9 PASS, 0 FAIL/SKIP, Scope Lock y V5 SUCCESS, Sheets y BITACORA readback; CI queued/in_progress no es PASS. A5: otra Task Javier distinta de DEV05 revisa genuinamente. Rollback revert normal, sin qa/main/PROD, secretos ni pagos.
