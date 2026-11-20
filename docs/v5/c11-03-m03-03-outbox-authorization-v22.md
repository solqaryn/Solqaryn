# SOLQARYN V22 — C11-03.M03.03: autorización, tenancy y denegación por defecto

Repositorio único solqaryn/Solqaryn, rama dev. PROJECT_SCOPE_LOCK=STRICT; riesgo R2. PLAN_MAESTRO D1672 exige una barrera de autorización real de código, no un pase documental. Esta entrega crea C1103OutboxAuthorizationGate en Application; NO configura DI/API/worker, ejecuta persistencia, conecta proveedores ni autoriza producción.

## Impacto de arquitectura y flujo

Application consulta IC1103OutboxCurrentMembershipReader con usuario JWT ya autenticado por middleware de servidor, tenant solicitado e idempotencia. Comprueba un único user ID, membresía fresca, rol/grant vigente, revocación, ownership tenant, origen confiable y hora UTC; luego integra reglas Domain C11-03.M03.01. Un tenant B nunca hereda acceso de A y la lista de eventos permitidos solo proviene del lector servidor, no del cliente. Error, replay y estado ambiguo rechazan. Resultado positivo OUTBOX_AUTH_PREFLIGHT_ONLY, sin autoridad para escribir/despachar, ACK o hacer transacciones.

Riesgo explícito: este guard NO verifica por sí solo la firma de JWT ni recibe solicitudes HTTP reales. El adaptador backend futuro debe validar criptográficamente JWT en middleware y consultar membresía server-side persistida antes de invocarlo; jamás pasar ClaimsPrincipal falso ni claims de headers cliente.

## A1–A3 y límite de integración

La suite xUnit incluye tokens JWT efímeros realmente firmados y verificados con issuer/audience/exp, rechazos por firma distinta y expiración, usuario duplicado/malformado, tenant A/B ajeno, rol/permiso revocado, evento fuera de allowlist, replay, excepción y cancelación. GitHub Actions .NET 10 Release debe ejecutar TRX dirigido con 17+ PASS, 0 FAIL, 0 NOT_EXECUTED. Estos tests no prueban por sí solos HTTP backend con autenticación real, MySQL 8.4 EF, ni worker/ACK durable: siguen SIN_CERTIFICAR para cualquier cierre R2 que los exija. No confundir mock de lector con consulta a base de datos.

A0: código Application, adversariales xUnit, workflow GitHub-hosted, CHANGELOG aditivo y este documento. A4: Scope Lock, procedencia V5, seguridad, build/test exact-SHA y PLAN/COLA/BITACORA readback. Solo transición a VALIDANDO si la entrega está publicada y trazable; LISTO condicionado a toda evidencia requerida. A5 por Task Javier distinta de DEV06 autora. Reversión: git revert normal en dev sin force-push y sin tocar QA/main/PROD. Aún falta integración efectiva con JWT/HTTP/MySQL 8.4 aislado, verificación de roles reales y casos de revocación en runtime si el requisito lo exige; M03.04–06 resolverán persistencia, relay y ACK.

## Reparación de seguridad V22 — frescura de evidencia (2026-11-19)

La barrera Application ahora impide aceptar snapshots de membresía o grants con sello UTC de verificación anterior a 30 segundos o futuro por más de 5 segundos respecto del reloj del servidor (TimeProvider inyectable). Un resultado antiguo de caché que antes conservaba permiso después de una revocación deja de producir OUTBOX_AUTH_PREFLIGHT_ONLY. Nuevos casos adversariales verifican límites y denegaciones; la autorización sigue siendo exclusivamente de preflight y no autoriza persistencia/dispatch. El reloj de servidor se usa para esta comprobación, no una fecha provista por el cliente. Un adaptador real debe leer estado de autorización actual en MySQL por solicitud: este control no certifica integración HTTP/JWT middleware/MySQL 8.4 ni ACK, y A5 independiente sigue pendiente.

## Reparacion de CI MySQL 8.4 compartida V22 — DEV03 (2026-11-19)

- La microtarea C11-03.M03.03 sigue VALIDANDO y conserva su autor historico DEV06. El job de migraciones del SHA 7309e540 presento seis fallos C1502M0503Mysql84ProviderTests con MYSQL84_EPHEMERAL_TEST_CONNECTION_REQUIRED: conexion separada ausente en dev-ci, aunque MySQL 8.4.13 estaba operativo.
- dev-ci crea ahora solqaryn_c1502_m0503 en el contenedor MySQL 8.4 efimero GitHub-hosted y declara la conexion C1502_M0503_MYSQL_TEST limitada a loopback, usuario root efimero y esquema dedicado. Se preserva la verificacion estricta DisposableConnection() y se siguen ejecutando todas las pruebas Category=Integration, sin saltarlas ni deshabilitar controles.
- A0-A4 requieren resultado real completed/success del SHA nuevo en el job de MySQL, Scope Lock y V5. Hasta entonces NO_PASS y no cerrar. Esto no implementa adaptador HTTP/JWT de outbox, lector duradero de membresias/roles ni revocacion tenant A/B; A5 independiente continua pendiente.
- Cambio R2 solo CI/documentacion: ningun runtime, tablas productivas, QA/main/PROD, cuenta de pago ni secretos. Rollback con git revert normal sobre dev despues de verificar HEAD.

## V22 — JWT/HTTP y MySQL 8.4 aislado en TestServer (20 noviembre 2026)

Se agregan pruebas reales de middleware JwtBearer firmado, HTTP 401/403/200 y MySQL 8.4.13 efimero con consultas parametrizadas de membresia y permisos por cada solicitud. Casos adversariales: firma falsa, tenant B, actor ajeno, evento fuera del grant, revocacion despues de expedir token, usuario deshabilitado y rollback SQL. GitHub-hosted debe acreditar al menos 2/2 xUnit Category=Integration y SHA exacto; hasta entonces CI pendiente NO PASS. LIMITACION R2: ruta __v22_test, tabla V22C1103AuthFixture y lector son test-only, NO adaptador operativo API/EF UsuarioEmpresa/RolPermiso/Outbox, ni persistencia/dispatch. Mantener VALIDANDO y A5 pendiente aun con test PASS hasta integracion runtime real y A0-A4 completos. Sin QA/main/PROD; rollback git revert normal.

## V22 DEV06 — adaptador real de autorización read-only, OFF por defecto

El nuevo `C1103OutboxVerifiedMembershipReader` utiliza el handler Bearer registrado, exige `id` firmado único y congruente con `ICurrentUserService`, verifica `UsuarioScopeService.ObtenerActualAsync(tenant)` y `IPermisoService.TienePermisoAsync(tenant, Configuracion, Crear)` respaldados por el modelo vigente UsuarioEmpresa/RolPermiso. Deniega sin token firmado, con identidad ambigüa, tenant cruzado, membresía/rol/permisos faltantes, fallo de servicio y cancelación. No acepta autorización por encabezados cliente ni claim admin. El endpoint `/__dev/outbox-preflight/{tenantId}` está solo en Development, requiere JWT válido, está OFF salvo `OutboxPlanning:EnableReadOnlyAuthorizationProbe=true` y devuelve únicamente un resultado de inspección (jamás inserta, despacha o confirma outbox). No activa QA/PROD ni modifica migraciones.

Pruebas negativas xUnit nuevas confirman flag OFF, tenant/ruta/actor inválidos, principal sintético sin handler firmado y cancelación; no se declaran como éxito antes del SHA exacto GitHub-hosted. El fixture previo de MySQL 8.4 y JWT HTTP es real pero utiliza tablas de test separadas. **Limitación de cierre:** falta demostrar en una misma ejecución la ruta real DEV + MySQL 8.4 con UsuarioEmpresa/RolPermiso vigente, revocación A/B y verificación de permisos, y correlacionar las pruebas obligatorias de backend/seguridad/Scope Lock/V5 en el SHA final; no se acredita aún A0–A4 integral ni A5. La salida sigue VALIDANDO, owner histórico DEV06 intacto, sin operación irreversible. Rollback mediante git revert normal, sin force-push.
