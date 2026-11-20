# C02-01.M05.04 — aceptación positiva JWT/tenant en MySQL 8.4 (incremento R2)

**SOLQARYN V5 FINAL · DEV03 Javier · PROJECT_SCOPE_LOCK=STRICT · rama dev.** PLAN_MAESTRO!D317 requiere prueba positiva de empresa única y última empresa válida tras JWT y pertenencia verificada. M05 es una meta acumulativa; esta microtarea acredita sólo su salida técnica concreta y no certifica la fase completa ni producción.

## A0 · Alcance, capas y rollback

Test **nuevo** `C0201M0504SignedJwtMysqlPositiveTests.cs`, workflow GitHub-hosted con servicio efímero MySQL 8.4, esta evidencia y CHANGELOG. **No cambia** API de producción, DTO, servicios runtime, migraciones, datos reales, flags, QA/main/PROD ni autorizaciones. Se aprovechan `TenantContextController`, `UsuarioScopeService` y `V17IntelligentTenantLoginAuthorization`, con capas existentes API → Application → Infrastructure → EF/MySQL. Rollback: revert normal del commit, sin force-push.

## A1–A3 · Pruebas causales exigidas en CI

- SQL real en MySQL 8.4 aislado crea usuario JWT, empresas A/B, roles empresa y dos membresías activas. TestServer ASP.NET valida JWT **firmado** y GET HTTP: ambas aparecen como autorizadas; roles devueltos proceden de MySQL, no del claim `rolId=999999` ni `X-Empresa-Id`/host falsos.
- Una preferencia **de contrato** de B, para mismo usuario, obtiene `PREFERENCE_REISSUE_REQUIRED`, con `IssuesJwt=false` y `AuthorizesResourceAccess=false`. La preferencia de ese fragmento es **fixture**, no lectura real de preferencia durable. Se documenta esa limitación; no confundirla con aceptación integral de última empresa válida.
- Revocación real de membresía B en MySQL deja sólo A: GET listado único, B denegada HTTP 403; política de un único tenant requiere `ONE_TENANT_REISSUE_REQUIRED`, jamás redirige o instala contexto por sí sola. JWT manipulado y actor extraño son rechazados. Bloqueo de usuario/rol o fallos de la autoridad se tratan conforme a pruebas adicionales existentes.
- El workflow exige verificación de versión MySQL 8.4, Scope Lock y un TRX real con `total=1,passed=1,failed=0,notExecuted=0`; si falla, **VALIDANDO**, no PASS.

## A4 / límites pendientes

No se acredita aquí la preferencia **persistida real**, el flujo completo de login inteligente y reemisión de JWT con pertenencia backend ni navegación autenticada Angular al panel permitido. Esas condiciones sólo pueden certificarse tras **prueba de integración real** que las implemente/verifique. Ninguna captura/mocks o contrato sintético reemplaza la evidencia de un selector/última preferencia auténtica. Mantener `EN_PROGRESO` o `VALIDANDO` si CI o estas pruebas faltan; no marcar LISTO sólo por el verde del test dirigido. A5 independiente por otra de las seis Tasks Javier, no por el autor. Prohibido QA/main/PROD.

## V22 — reparación causal de disparador CI en `dev` (2026-11-20)

El workflow `v22-c02-01-m05-04-positive-mysql.yml` tenía un filtro `on.push.paths` que apuntaba a `v22-c02-01-m05-04-adversarial-mysql.yml` (ruta inexistente para este check). Cambios sólo en el propio workflow no reejecutaban la prueba causal JWT/HTTP/MySQL 8.4. Se corrige a la ruta del workflow real para que la próxima edición del gate vuelva a validarse automáticamente en GitHub-hosted por SHA exacto. Sin cambio runtime/DB/productivo. La decisión `PREFERENCE_REISSUE_REQUIRED` continúa basada en fixture: faltan preferencia durable real y navegación autenticada, por tanto permanece VALIDANDO, no LISTO/A5. Rollback: revert normal, preservando historia y estado.
