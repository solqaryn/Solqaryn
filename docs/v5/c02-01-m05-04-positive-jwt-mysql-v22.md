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

## V22 — evidencia SQL derivada de membresía real tras revocación (DEV04, 2026-11-20)

Se reemplaza **exclusivamente** el arreglo sintético de membresía única en la prueba positiva por una consulta relacional real en la base MySQL 8.4 efímera (UsuarioEmpresa + Empresa + Rol empresarial activo). Después de revocar B mediante EF, la misma ejecución consulta la persistencia y exige **exactamente A**, ausencia de B y decisión `ONE_TENANT_REISSUE_REQUIRED`; HTTP con JWT firmado ya comprobó el 403 de B. Se conserva la comprobación de inyección de rol/host y no se modifica un endpoint productivo.

**Límite expreso:** el `TrustedSnapshot` sigue construido en el harness, con la sesión JWT validada previamente por HTTP; no existe aquí adapter registrado de runtime, preferencia por usuario guardada en SQL, reemisión de JWT ni navegación Angular/Chromium al panel permitido. `PREFERENCE_REISSUE_REQUIRED` todavía parte de fixture. A0–A4 globales permanecen incompletos y la microtarea sigue VALIDANDO, A5 pendiente de otra Task Javier. Rollback: revert normal del commit en dev, sin tocar datos reales, QA/main/PROD ni secretos.

## V22 — reparación real de cancelación adversarial (DEV03)

Se revalida el CancellationToken de la solicitud inmediatamente después del await del lector de membresías/preferencia, aunque el lector ignore cancelaciones. La prueba xUnit adversarial entrega una preferencia aparentemente válida para tenant B después de cancelar la solicitud y exige OperationCanceledException, sin resolver ni autorizar tenant. R2, capa Application y test unitario, rollback por revert normal. No se modifica JWT real, persistencia, API, UI ni otras ramas.

La preferencia durable SQL, la reemisión de JWT y la navegación autenticada Angular/Chromium todavía faltan para la aceptación global de D317. La corrección NO autoriza LISTO ni A5: mantener VALIDANDO hasta CI causal exact-SHA y A0–A4 pertinentes.

## V22 — causalidad de CI para política Application y regresión adversarial (DEV03)

Se encontró una brecha reproducible de selección de rutas en GitHub Actions: el workflow de prueba positiva MySQL sólo escuchaba su archivo de integración y su propio YAML, aunque la política `V17IntelligentTenantLoginAuthorization` y sus pruebas xUnit pudieran cambiar el resultado de selección empresarial. Se agregan ambas rutas a `on.push.paths` para activar el mismo job al modificar la autoridad Application o sus negativos.

En el **mismo** job GitHub-hosted se ejecuta un TRX independiente de la clase `V17IntelligentTenantLoginAuthorizationTests` con al menos 16 casos, cero fallidos/no ejecutados, incluidos lector que ignora cancelación, preferencia de otro actor, tenant revocado, JWT no verificado, sesión antigua, A/B y selector falsificado. Se conserva separadamente la aserción de **1/1 PASS** del TestServer real con JWT firmado, Oracle EF y MySQL 8.4 efímero. Esto evita que un cambio de lógica quede sólo con pruebas positivas verdes de un código anterior. Si cualquier job es skipped/cancelled/in_progress/failed no acredita el gate. Sin cambios de runtime, base de datos, migraciones, permisos ni secretos.

**Limitación no resuelta:** esta cobertura dirigida no crea lectura durable SQL de última preferencia, reemisión JWT backend ni navegación final Angular/Chromium real hasta el panel permitido. A0–A4 del alcance completo D317 aún requieren esas pruebas; mantener `VALIDANDO`, conservar autor histórico, A5 independiente y no liberar sucesoras. Revert normal de commit en `dev` como rollback.

## V22 — CI FAIL causal de primer ensayo y corrección de orden restore (DEV03)

El job exact-SHA `0d9f1559` <https://github.com/solqaryn/Solqaryn/actions/runs/44842871138/job/136655341320> confirmó MySQL 8.4.13 y Scope Lock, pero falló **antes de ejecutar la integración** con `M0504_POLICY_TRX_MISSING_OR_MULTIPLE`. Los logs muestran que el comando `dotnet test --no-restore` se lanzó antes del `dotnet restore Solqaryn.sln` ubicado en el paso de integración posterior, por lo que no existía el archivo TRX; no se interpreta el exit code del comando aislado como prueba superada. Se corrige la secuencia agregando restore explícito antes del TRX xUnit; siguen exigidos al menos 16 casos de política, 0 fallidos/no ejecutados, y el TRX MySQL/JWT HTTP 1/1 por separado. El FAIL histórico permanece en GitHub y BITACORA.

Esta reparación es exclusivamente del workflow en `dev`. No se modifican permisos, datos, backend productivo, JWT, UI, MySQL de producción ni el scope. **Mantener VALIDANDO** mientras la nueva CI no concluya y mientras falten preferencia SQL durable, JWT firmado reemitido y navegación real Angular al panel autorizado; A5 sigue independiente. Rollback con revert normal.
