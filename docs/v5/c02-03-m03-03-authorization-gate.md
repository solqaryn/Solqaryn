# C02-03.M03.03 — autorización tenant y denegación por defecto (DEV)

**Fecha:** 2026-11-18. **Rama:** dev. **Scope Lock:** STRICT. **Riesgo:** R2.

## A0 — Alcance y arquitectura

Se agrega una puerta Application de lectura `C0203TenantLifecycleAuthorizationGate` dependiente de `IC0203VerifiedOperatorEvidenceReader`. El consumidor entrega únicamente tenant objetivo y operación, sin JWT, identidad de actor, grants ni DSN de procedencia cliente. El lector de confianza es una interfaz para un futuro proveedor **servidor** que deberá verificar firma JWT en middleware, rol global actual, grant explícito, revocación, sesión/membresía fresca, tenant dueño y política/fence actuales. Deniega con un código uniforme si cualquiera falta. Una validación positiva habilita únicamente continuar al preflight Application; jamás se ejecuta mutación, autorización de grant, CAS, restauración, borrado o despliegue.

**Límite crítico:** NO se registra implementación viva de `IC0203VerifiedOperatorEvidenceReader` en DI. Los flags del snapshot son afirmaciones que un adaptador real deberá obtener de fuentes verificadas, no JWT válidos por sí mismos. La clase no expone endpoint HTTP ni altera EF/MySQL, migraciones o datos. No confundir pruebas sintéticas con aislamiento real A/B en API o con autorización productiva. Este incremento NO completa aún toda la aceptación de C02-03.M03.03.

## A1/A2 — Pruebas negativas

xUnit en GitHub-hosted comprueba el preflight positivo no autorizante, cliente que intenta autodeclarar permisos/actor/JWT/DSN/cutover, tenant B, grant revocado, JWT no verificado, rol faltante, política obsoleta, ownership no verificado, lectura sin fence, fuente nula o fallida, error saneado, cancelación, caracteres Unicode invisibles e IDs al límite. No se emite tenant, JWT o datos privados en respuestas de error.

## A3/A4 — Integración, CI y revisión

La integración HTTP JWT firmado con RBAC persistente, revocación real y consultas EF/MySQL multi-tenant A/B no está implementada en este diff y por tanto **NO_APLICA a las pruebas del módulo inerte**; sigue **PENDIENTE para la aceptación integral** de la microtarea. El workflow dirigido compila `.NET 10` Release y ejecuta suite xUnit exacta; Scope Lock, V5, backend y seguridad deben constar SUCCESS en el SHA causal. QUEUED/IN_PROGRESS/FAILED/SKIPPED/CANCELLED nunca PASS. Mantener VALIDANDO mientras falten pruebas reales de integración y A5 independiente de Task Javier distinta.

## V22 DEV05 — reparación causal R2: vinculación obligatoria del comando

Hallazgo: el lector de evidencia recibía solo `TargetTenantId` y el booleano `CommandEnabledByTrustedConfig` no demostraba a qué operación se vinculó la autorización. Una lectura positiva para `Suspend` podía reutilizarse para `Reactivate`, `Archive` o `LogicalDelete` en el preflight sintético. El contrato ahora exige que el lector reciba el comando y devuelva `VerifiedCommand`, con comparación estricta y validación de enumeración dentro del guard. Las pruebas adversariales comprueban el cruce de operaciones y comandos desconocidos; la CI GitHub-hosted del SHA nuevo deberá certificarlas.

**Limitación explícita:** el cambio protege una frontera Application sin endpoint ni escritura. No se registra todavía adaptador runtime verificado contra JWT firmado + UsuarioEmpresa/RolPermiso + MySQL 8.4 ni revocación por HTTP; no equivale a aceptación A2/A3 integral ni habilita `LISTO`/A5. Se conserva denegación por defecto, cero autorizaciones de mutación y rollback con `git revert` normal.

## Rollback

`git revert` normal del commit exclusivo de este scope tras validar HEAD, con readback y sin borrar BITACORA o modificar DAG/cola. Nunca QA/main/PROD.


## V22 DEV05 — integración incremental con JWT firmado, HTTP y MySQL 8.4

Prueba Integration del gate Application real detrás de ASP.NET Core TestServer/JwtBearer (firma, issuer, audience, lifetime), con SQL de MySQL 8.4 EFÍMERO en loopback por solicitud. Casos adversariales: ausencia/falsificación JWT (401), tenant B, comando Reactivate en vez de Suspend, revocación SQL de grant, cabecera X-Empresa-Id no autoritativa, aislamiento positivo de B y rollback de transacción (403). La respuesta positiva sigue siendo exclusivamente READONLY. Workflow dirigido exact-SHA con MySQL 8.4, .NET 10, Scope Lock y 0 fallos obligatorios.

LIMITACION R2: V22C0203AuthFixture NO representa las tablas reales UsuarioEmpresa/RolPermiso. El endpoint existe solo en TestServer, sin servicio de aplicación registrado ni mutaciones. Esto NO certifica la integración comercial completa ni A2/A3 global; falta implementar lector real desde DB persistente y JWT/RBAC vigente, completar CI y recibir A5 independiente. Mantener VALIDANDO y owner histórico; sin QA/main/PROD.

## V22 DEV05 — carrera de cancelación posterior a lectura confiable

Una implementación I/O no cooperativa puede devolver `TrustedOperatorSnapshot` positivo despues de que se cancele la solicitud. El guard ahora vuelve a comprobar el CancellationToken inmediatamente despues del await del lector; propaga OperationCanceledException y nunca devuelve VERIFIED_READONLY_PREFLIGHT_ONLY para solicitudes canceladas. La prueba xUnit adversarial usa un proveedor asincrono que cancela la solicitud y devuelve evidencia positiva obsoleta. No modifica API, JWT, EF, MySQL, tablas ni autorización real; los adapters de UsuarioEmpresa/RolPermiso siguen pendientes. Riesgo R2, CI exact-SHA requerida, mantener VALIDANDO, A5 independiente. Rollback git revert normal.

## V22 DEV05 — lector real DEV de identidad global y grant de plataforma

Incremento R2: se incorpora C0203VerifiedPlatformOperatorEvidenceReader en API/Security y su registro DI solo en Development. Comprueba JWT Bearer firmado por el middleware registrado (id, nombreUsuario y sub coherentes), identidad ICurrentUserService, autoridad IPlataformaScopeService actual desde UsuarioRolPlataforma/Usuario/Rol, grant relacional explícito RolPermiso/Permiso de ámbito Plataforma para Configuracion.CambiarEstado, y Empresa activa consultada por ID de tenant en AppDbContext. Nunca acepta X-Empresa-Id ni un ClaimsPrincipal sintético como prueba de firma.

**Fail closed por defecto:** requiere TenantLifecycle:EnableReadOnlyAuthorizationProbe=true, TenantLifecycle:AllowSuspendReadOnly=true, ReadOnlyPolicyRevision positivo y ruta tenantId que coincida. Solo permite preflight informativo de Suspend, no reactivación, archive, delete, grant, CAS, escritura ni cutover. La lógica no expone ruta HTTP ni actualiza migraciones o datos. Las pruebas xUnit adversariales validan config apagada, comando no permitido, ruta forjada, actor anónimo, principal sintético y cancelación; ejecutar CI exact-SHA antes de usar estos resultados.

**LIMITACIÓN R2/A2-A3:** el nuevo lector todavía no tiene prueba HTTP JWT+EF de sus propias consultas contra tablas UsuarioEmpresa/RolPermiso y MySQL efímero, y no está conectado a un endpoint de ciclo de vida real. Una revisión de policy version desde configuración NO equivale a revisión transaccional durable ni certifica CAS y revocación. El flujo de autorización para mutaciones sigue bloqueado y la microtarea permanece VALIDANDO hasta pruebas de integración completas y revisor A5 independiente. Rollback: git revert normal del commit exclusivo tras verificar HEAD y CAS; conservar BITACORA histórica.
