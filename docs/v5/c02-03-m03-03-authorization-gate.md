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

## V22 DEV05 — reparación P2 de identificadores positivos SQL de un dígito (2026-11-20)

A0: hallazgo reproducible en `C0203TenantLifecycleAuthorizationGate.SafeId`: la validación de longitud mínima 2 rechazaba `Empresa.Id` y `Usuario.Id` positivos válidos de un dígito (1..9), incluso con evidencia de servidor, JWT firmado, grant de plataforma y tenant correctos. La corrección admite un único carácter únicamente cuando es un dígito ASCII 1..9, conserva rechazo de `0`, letras solitarias y caracteres de control, y no modifica las prohibiciones de mutación.

A1: xUnit positivo para (tenant, actor) (1,1), (7,9) y (9,3), denegación de `0` y `a`, regresiones de 129 caracteres, falsificación de actor/JWT/tenant, revocación, comandos cruzados, excepciones y cancelación. La aprobación real depende de GitHub Actions exact-SHA; pruebas requeridas en curso no son PASS.

A2/A3: este incremento corrige una validación de Application que afecta al lector real DEV-only; **no acredita** el JWT+HTTP+Oracle EF/MySQL 8.4 con el lector de producción completo, revisión transaccional duradera, ni permisos de mutación. Mantener VALIDANDO y `A5_PENDIENTE` hasta prueba integrada causal y revisión independiente. La configuración sigue desactivada por defecto; sin QA, main o PROD.

A4/rollback: verificar SHA, Scope Lock, .NET Release/xUnit, seguridad y V5 CHANGELOG en GitHub-hosted. Rollback mediante `git revert` normal del commit tras revalidar HEAD, preservando BITACORA.

## V22 DEV05 — reparacion causal del CI MySQL 8.4 compartido (2026-11-20)

El job `Migraciones EF Oracle, variantes y cargas masivas en MySQL 8.4` del workflow `dev-ci.yml`, exact SHA `05d61c770371676cfa62801bc9ad52672eeae6af`, ejecuto `Category=Integration` en GitHub-hosted MySQL 8.4 y obtuvo 109 PASS, 2 FAIL (c0404 M03.04/M03.05): ambas pruebas lanzaban `EPHEMERAL_MYSQL84_REQUIRED` porque las variables `C0404_M0304_MYSQL` y `C0404_M0305_MYSQL` no estaban presentes en el entorno del job. No se acepta ese FAIL como PASS. Se incorporan exclusivamente esos dos connection strings al servicio MySQL 8.4 efimero de loopback (`127.0.0.1`, `root`, `c0404_bootstrap`), sin apuntar a Aiven, sin secretos persistentes ni activar proveedores. Los tests derivan nombres de DB unicos y comprueban MySQL 8.4, aislamiento, replay, conflicto y rollback. Hipotesis comprobable: el job deja de lanzar `EPHEMERAL_MYSQL84_REQUIRED` y ejecuta el conjunto real con cero FAIL en el SHA reparado.

A0: reparacion CI transversal relacionada con gate A4 de C02-03.M03.03; no cambia API/EF/DB ni habilita autorizacion de mutaciones. A1/A3: SOLO PASS si la ejecucion GitHub Actions exact-SHA de `Category=Integration` tiene 0 FAIL y no omite pruebas requeridas; otros gates de runtime C02-03 siguen pendientes. A2: no se modifica JWT/RBAC/tenant ni la proteccion del backend. A4: verify-solqaryn-project-scope, provenance V5, backend y seguridad deben completarse SUCCESS; si quedan queued/in_progress/failed, conservar VALIDANDO. A5: revisor Javier diferente de autor y reparador. Rollback: `git revert` normal tras verificar HEAD; no QA/main/PROD.


## V22 DEV05 — integración real del lector global JWT/RBAC/Oracle EF/MySQL 8.4

Incremento R2 de C02-03.M03.03: la nueva prueba de Integration ejecuta el lector de API C0203VerifiedPlatformOperatorEvidenceReader y el servicio real PlataformaScopeService con JWT Bearer firmado dentro de ASP.NET Core TestServer, consultas Oracle EF Core 10 y una base MySQL 8.4 aislada por prueba, creada y destruida en GitHub-hosted. Consulta las tablas reales por nombre/modelo UsuarioRolesPlataforma, Usuarios, Roles, RolPermisos, Permisos y Empresas (esquema minimo exclusivo de prueba). No usa un lector sustituto de SQL de flags para el positivo. Compara caso positivo A y B del operador global, inexistencia/inactivacion de tenant, firma falsificada, subject inconsistente, ausencia de JWT, grant revocado, rollback de grant, rol global revocado y usuario bloqueado. La cabecera de tenant o rol falsificada no puede crear autorizacion.

El workflow dirigido incluye este nuevo test y la prueba anterior de SQL fixture; ambos requieren ejecución GitHub-hosted .NET 10 y MySQL 8.4 con 0 FAIL y 0 SKIPPED, además de Scope Lock, backend/seguridad y procedencia V5 del SHA. Un archivo de prueba o una suite en cola NO acredita A3 hasta completed/success exact-SHA.

LIMITACION: el resultado positivo solo autoriza preflight de LECTURA en Development con flags servidor desactivados por defecto; no existe controlador comercial de mutacion ni versionado/CAS transaccional para el ciclo de vida, ni certificacion de todo el flujo de Empresa o de produccion. El fixture usa esquema minimo descartable, NO es migracion real ni datos Aiven. Por eso conservar VALIDANDO si quedan A0-A4 del alcance D376 pendientes y exigir A5 de otra Task Javier no autora/reparadora. Rollback: git revert normal tras validar HEAD y conservar BITACORA, sin QA/main/PROD.


## V22 DEV04 - reparacion de cuatro fallos de integracion por variables CI ausentes (2026-11-20)

Se identifico una causa reproducida en el job GitHub-hosted `Migraciones EF Oracle, variantes y cargas masivas en MySQL 8.4`, SHA `7c251789768a4b0de3c2d6a6d016e54b44280998`, job `136595913146`: 118 pruebas aprobadas y cuatro fallidas con `InvalidOperationException: EPHEMERAL_MYSQL84_REQUIRED`. Las cuatro pruebas `C0404M0503PaymentPersistenceConcurrencyTests`, `C0404M0504PositiveReservationAcceptanceTests`, `C0404M0505NegativeSecurityMySqlTests` y `C0404M0604RollbackAndDeactivationMySqlTests` requieren respectivamente `C0404_M0503_MYSQL`, `C0404_M0504_MYSQL`, `C0404_M0505_MYSQL` y `C0404_M0604_MYSQL`, no suministradas por `dev-ci.yml`. La instancia efimera MySQL 8.4.13 si existia y los tres contratos M03.04-M03.06 ya estaban configurados.

La reparacion agrega solo esas cuatro claves al job existente, con loopback 127.0.0.1, usuario root temporal de GitHub-hosted y esquema bootstrap exclusivo. Cada xUnit genera su propio esquema aleatorio y comprueba version MySQL 8.4, aislamiento tenant A/B, replay, fronteras de entrada y rollback sin efectuar cobros; NO se desactivan ni omiten tests. No cambia runtime, API, EF, autenticacion, secretos, datos Aiven, proveedores de pago o QA/main/PROD.

A0: reparacion de infraestructura CI compartida, asociada a la evidencia de `C02-03.M03.03`, preservando owner historico DEV05 y ejecutor reparador DEV04. A1/A2/A3: los cuatro tests adversariales fisicos ya existian, pero solo hay nueva evidencia al completarse `Category=Integration` con cuatro PASS, cero FAIL y cero SKIPPED en el nuevo SHA; hasta entonces NO se declara PASS. A4: requieren SUCCESS exact-SHA de Scope Lock, V5, compilacion y seguridad pertinentes y readback del Sheet; no sustituir el gate de ciclo de vida durable pendiente de C02-03.M03.03. A5 por otra Task Javier diferente de DEV05 autor y DEV04 reparador. Rollback: revert normal fast-forward despues de comprobar HEAD; preservar BITACORA.

## V22 DEV05 — reparacion P2 transversal: Empresa/Usuario de ID positivo de un digito y cancelacion post-I/O (2026-11-20)

A0: El guard real de C02-03.M03.03 ya admitia Empresa.Id y Usuario.Id 1..9, pero el preflight Application M03.02 y la regla Domain M03.01 aun exigian longitud >= 2. Se produce denegacion funcional erronea para IDs SQL validos en el mismo flujo. La correccion introduce validacion separada de IDs de negocio (1..9 permitidos, cero y letras solitarias rechazadas), sin relajar la longitud minima de claves opacas de idempotencia. Se preserva rechazo de tenant ajeno, JWT no firmado, rol/grant revocado y cualquier mutacion no autorizada.

A1/A2: xUnit adversarial positivo de ID (1,1), (7,9), (9,3), negativos "0"/"a"/espacios, clave idempotente de un caracter y ausencia de efectos. Tambien se revalida CancellationToken despues del await del lector Application: si un proveedor ignorase cancelacion y devolviera evidencia positiva obsoleta, el preflight lanza cancelacion y no autorizacion. Prueba con lector asincrono no cooperativo. No se modifican rutas HTTP, DI, tablas, datos, migraciones, secretos, grants, feature flags ni el contrato de escritura. Riesgo R2.

A3/A4: los nuevos tests .NET 10 deben pasar realmente en GitHub-hosted contra SHA exacto junto con Backend Release, seguridad multiempresa, Scope Lock y provenance V5. Sin verificacion COMPLETED SUCCESS no declarar PASS. Sigue pendiente para C02-03.M03.03 un adaptador transaccional duradero de revision de politica, mutacion CAS, revocacion e idempotencia en MySQL con JWT/RBAC reales; mantener VALIDANDO e historico A5_FAIL intactos. A5 solo por Task Javier ajena a autor DEV05 y reparadores previos; no QA/main/PROD.

Rollback: git revert normal del commit de seis archivos con readback, sin tocar historial BITACORA ni formulas de DAG/COLA.

    
## V22 DEV04 - P2: evidencia confiable vinculada a la operacion (2026-11-20)

Hallazgo causal: el preflight Application de C02-03.M03.03 solo entregaba tenant al lector confiable. Un snapshot positivo emitido para Suspend podia reutilizarse para Reactivate, Archive o LogicalDelete: el booleano RequestedCommandEnabledByTrustedConfiguration no indicaba que comando verifico el servidor. Ninguna evidencia de otra operacion puede habilitar preflight.

Reparacion: IC0203LifecycleEvidenceReader.ReadAsync recibe tenant y comando; TrustedReadback.VerifiedOperation debe ser enum definido e identico a la solicitud. La comprobacion ocurre antes del evaluador Domain, tras await y control de cancelacion. Denegacion saneada sin revelar sesion, SQL ni JWT.

A0/A1/A2: xUnit adversarial cubre tres cruces Suspend hacia Reactivate, Archive, LogicalDelete; comando indefinido; propagacion al lector; positivo Reactivate que aun exige CAS durable sin escribir. Riesgo R2, tenancy A/B y politica de autorizacion sin relajar. A3 LIMITACION: sin adaptador runtime de mutacion ni CAS transaccional durable, sin nueva migracion ni endpoint comercial; el preflight no valida por si solo HTTP JWT/MySQL. Exigir integracion real para cierre de D376. A4: GitHub-hosted Backend Release xUnit, seguridad, Scope Lock y V5 del SHA publicado requieren SUCCESS; queued/in_progress no es PASS. Conservar H VALIDANDO, owner historico DEV05, A5 independiente, BITACORA intacta.

Rollback: git revert normal, previa comprobacion del HEAD, sin reescribir historial ni tocar QA/main/PROD.

## V22 DEV05 — canonicalizacion estricta del tenant de ruta JWT/MySQL (2026-11-20)

Hallazgo P2: el lector **real** API/Security usaba int.TryParse para Empresa.Id y para el parametro tenantId, pero aceptaba valores decimales no canonicos como 0811, 000811 y 01 como alias de 811/1. Esto podia romper la vinculacion de identidad de tenant en trazas, claves de idempotencia y recibos durables futuros aunque la base consultara la misma fila. No se acepta un identificador numerico ambiguo en una operacion de ciclo de vida.

Reparacion R2: C0203VerifiedPlatformOperatorEvidenceReader exige representacion decimal invariante exacta del ID SQL y coincidencia literal del parametro de ruta con el ID solicitado. Mantiene admitidos los ID positivos de un solo digito, deniega ceros iniciales y conserva autenticacion JWT real, rol/grant global vigente, comprobacion SQL de Empresa activa, feature flags DEV-only apagados por defecto y la frontera READONLY sin escritura. El control se ejecuta antes del lookup de grants.

Prueba de integracion REAL: ampliar el TestServer con JWT Bearer firmado, PlataformaScopeService real y Oracle EF/MySQL 8.4 efimero (base creada/borrada por prueba). Positivos tenant 811, 812 y 1; negativos 0811, 000811, 0812 y 01, junto con falsificacion JWT, rol/grant revocado, tenant ausente, cancelacion y rollback ya existentes. Requerir job GitHub-hosted exact-SHA con Category=Integration y 0 FAIL/0 OMITIDAS, ScopeLock, seguridad, backend y provenance V5 COMPLETED SUCCESS; un check aun corriendo NO es PASS.

LIMITACION R2: esta reparacion solo mejora la autenticacion previa de lectura; no implementa revision de politica persistente, CAS transaccional, mutacion de tenant, recibo durable, revocacion de sesiones ni rollback comercial integral de PLAN D376. Se conserva H=VALIDANDO, owner DEV05, A5 independiente de reparador y autor, sin QA/main/PROD. Rollback mediante git revert normal comprobando HEAD y sin alterar BITACORA ni DAG/COLA.


## V22 DEV05 — identidad canónica de actor JWT firmado ante alias SQL (21 noviembre 2026)

**Hipótesis R2/P2 nueva y falsable:** `int.TryParse` convierte los claims JWT firmados `id=0911` y `id=000911` al mismo `Usuario.Id=911`, mientras la auditoría y futuras claves de idempotencia pueden distinguir sus representaciones. El lector DEV de autoridad global ahora exige que el texto del claim `id` coincida exactamente con el entero positivo expresado en decimal invariante sin ceros iniciales ni signo. Conserva la comparación contra `ICurrentUserService`, autenticación JwtBearer real, grant/rol frescos desde Oracle EF/MySQL, Empresa activa, ámbito y comando, con denegación fail-closed antes de leer permisos.

**Prueba causal:** en `C0203M0303RealPlatformReaderMysql84IntegrationTests`, el JWT se firma con clave válida y actor `0911`, `000911` y `+911` y se exige `HTTP 403 / LIFECYCLE_ACCESS_DENIED`; actor canónico `911` conserva `HTTP 200 / VERIFIED_READONLY_PREFLIGHT_ONLY` para el preflight read-only. Utiliza TestServer, reader REAL de API y `PlataformaScopeService`, MySQL 8.4 EFÍMERO y cleanup `DROP DATABASE`; pruebas de firma falsa, tenant A/B, grant duplicado/revocado, rollback e ID de tenant canónico permanecen. Sin cambio de migración, runtime de producción, secretos, grants ni habilitación de feature flags.

**A0–A4:** diff acotado a reader/test/doc/CHANGELOG; exigir GitHub-hosted .NET 10, job real JWT HTTP MySQL 8.4 2/2 sin FAIL/SKIP, seguridad, backend, Scope Lock y V5 COMPLETED SUCCESS del nuevo SHA, además de readback PLAN/BITACORA. Cualquier CI en cola no es PASS. **Limitación:** no implementa mutación de ciclo de vida, CAS de revisión durable, idempotencia de contratos ni revocación transaccional; mantener H=VALIDANDO e I=DEV05 histórico hasta aceptación completa, A5 por una Task distinta del autor/reparador DEV05. Rollback por `git revert` normal, sin QA/main/PROD.
