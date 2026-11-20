# C15-02.M05.02 — contrato cliente API/DTO y errores seguros

PROJECT_SCOPE_LOCK=STRICT. Alcance atomico PLAN D2223: ejecutar contrato API/DTO y validar errores seguros para el guard de la pantalla privada de arquitectura C15-02.

## Analisis de impacto y riesgo
- R1 TEST_ONLY: solo prueba Angular 22.2.1, workflow dirigido, contrato documental y CHANGELOG. No se modifica runtime, endpoint, base de datos, JWT/RBAC, frontend publico ni infraestructura.
- Consumidor real: `PlataformaSesionService.obtener()` utilizado por `plataformaGuard` antes de la ruta `/plataforma/revision-arquitectura-v20`. El backend sigue siendo autoridad real.
- Contrato GET `/plataforma/sesion`: positivo 200 con DTO global, negativos 403, suplantacion de rol/ambito, DTO incompleto, invalidacion por revocacion y carreras de respuestas HTTP.
- A0: diff ligado a D2223 y rollback normal con git revert, no force-push.
- A1: seis pruebas con asserts positivos, negativos, frontera, errores y carreras de sesion.
- A2: denegacion en cliente por 403/rol Empresa/administrador falso y no reutilizacion de privilegios obsoletos. NO se atribuye seguridad JWT/RBAC backend sin prueba de firma/servidor.
- A3: `HttpTestingController` comprueba contrato real de `HttpClient` del servicio existente con backend virtual. **No es una peticion HTTP real**, ni prueba MySQL 8.4, JWT firmado, proveedor, A/B, restauracion o integracion end-to-end. Estas evidencias pertenecen a pruebas de integracion/securidad posteriores que asi lo exijan.
- A4: exigir workflow Angular GitHub-hosted exact-SHA con seis tests, lint, build, verify-solqaryn-project-scope y V5 CHANGELOG completos SUCCESS. SKIPPED/CANCELLED/FAIL/IN_PROGRESS no son PASS. Readback Sheets/Bitacora V17/CAS.
- A5: revision independiente por otra Task Javier distinta de DEV04; nunca autoaprobar.

PLAN E2223 es meta de fase acumulativa, NO certificacion de toda M05 ni de Hostinger, Cloudflare, R2, dominios o produccion. Sin cambios de QA/main/PROD, DNS, secretos o pagos. No se alteran gates DAG y COLA.
