# C02-01.M05.01 — Regla unitaria de resolución única de empresa

Owner: SOLQARYN PRIMARY DEV04 JAVIER (6aeacb7820ac8191b7d768a6f33dfa09). Único repo solqaryn/Solqaryn, dev, PROJECT_SCOPE_LOCK=STRICT, V17 lock 2000000314. PLAN D314 exige **ejecutar una prueba unitaria o de regla reproducible**; PLAN E314 es objetivo de fase y no certificación completa de host/JWT/tenant.

## Alcance
Regresión xUnit real .NET 10 del método de producción UsuarioScopeService.ObtenerUnicoActualAsync, además de ObtenerActualAsync y ListarEmpresasSeleccionablesAsync. Test-only: no se altera runtime, JWT, DI, API, EF model, MySQL, rutas ni frontend. EF InMemory es un sustituto intencional **solo para la regla unitaria**, no integración SQL. La autoridad del usuario permanece en UsuarioEmpresa y rol/estado reales de los objetos del servicio, nunca una preferencia del cliente.

## A0–A5
- A0: diff xUnit, workflow independiente, documentación y CHANGELOG, sin modificación productiva; regla comercial reproducible y rollback por revert normal.
- A1: positivos/negativos: dos membresías activas impiden autoselección; revocar B permite solo A; rol perteneciente a B no eleva A; usuario bloqueado no selecciona empresa; anónimo no obtiene empresa. Aserciones fallan ante retornos permisivos o datos de otro tenant.
- A2: casos adversariales de empresa A/B, rol cruzado, membresía revocada y usuario bloqueado; **solo prueba la regla del servicio**. No confundir mock de identidad con JWT firmado, revocación en HTTP/DB.
- A3: NO_APLICA MySQL 8.4, HTTP real, Chromium y proveedores **para este cambio test-only** conforme a D314. EF InMemory y Mock ICurrentUserService no demuestran seguridad del runtime; pruebas de integración deben pertenecer a M05.02–06/otros scopes.
- A4: exigir xUnit TRX unívoco 1/1 PASS 0 FAIL 0 NOT_EXECUTED en GitHub-hosted Release, Scope Lock/V5/security y checks pertinentes SUCCESS SHA exacto; PLAN/COLA/BITACORA readback.
- A5: revisión auténtica de otra Task Javier distinta del autor, con veto posible.

Este incremento no habilita QA/main/PROD ni desbloquea todos los requisitos de la fase M05. El ciclo real de login, host, HTTP JWT y persistencia quedan por evaluar en microtareas siguientes.
