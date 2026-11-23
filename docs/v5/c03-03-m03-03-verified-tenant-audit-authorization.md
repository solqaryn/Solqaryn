# C03-03.M03.03 — autorización audit-read por JWT/RBAC y tenant (V25)

Alcance: PLAN_MAESTRO D520, `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. Task autora DEV03; riesgo R2. PLAN E520 contiene objetivo acumulativo M03; esta entrega NO completa el subsistema de auditoría ni M03.04-.06.

Cambio verificable: se implementa `C0303VerifiedAuditTenantReaders` para los contratos ya existentes de Application. OFF por defecto (`SecurityAudit:EnableReadOnlyAuthorizationProbe=false`), fuente obligatoria de empresa server-side (`VerifiedCommercialTenant`), token firmado por el verdadero esquema JWT Bearer y coincidencia estricta de `id/nombreUsuario/sub` con el usuario autenticado. La pertenencia, estado de usuario, tenant, rol empresarial administrador y concesión vigente `Auditoria.Ver` se leen en EF SQL, se refrescan antes de retornar y nunca se derivan del cliente o del rol JWT heredado. La fuente de recurso solamente acredita la raíz Empresa ACTIVA para la acción de SOLO LECTURA AuditRead: no afirma propiedad de facturas ni demás recursos, no concede permisos ni ejecuta cambios, no instala endpoint.

Pruebas xUnit: JWT firmado vs firma ajena/actor falsificado, ausencia de feature/tenant verificado, route spoof, membresía tenant B, rol no administrador, falta y revocación de RolPermiso, bloqueo de usuario, cancelación, denegación de acciones críticas y resource id cruzado. Auth bearer real + EF InMemory en pruebas unitarias; NO confundir EF InMemory con integración física MySQL 8.4 ni con HTTP de API en ejecución. **Falta integración dirigida GitHub-hosted MySQL 8.4 efímero / HTTP** y validación posterior de A0-A4 antes de LISTO. En ausencia de evidencias CI completas permanece VALIDANDO con owner. A5 solo independiente de Task distinta.

Topología: API Security -> Application contract -> Domain rules; lectura EF SQL de Infrastructure, ningún cambio de esquema, runtime permitido solo bajo opt-in explícito DEV. Pendiente: fuente de eventos desde call-site autenticado y sink durable (M03.04-.06); pruebas adversariales de cambios críticos atómicos antes de cualquier habilitación. Rollback: git revert ordinario exact commit, flag OFF; sin QA/main/PROD, secretos ni migración real.

## V25 — DEV06 rescate cruzado: cuatro regresiones ante revocación relacional

Hipótesis nueva y causal: un JWT firmado y el scope `IUsuarioScopeService` previamente aceptados no deben permitir evidencia posterior cuando cambie una autorización en la base relacional. Cuatro casos xUnit parametrizados registran lectura positiva, desactivan independientemente `UsuarioEmpresa.Activa`, `Empresa.Activa`, `Rol.Activo` o `Permiso.Activo`, dejan intacto el snapshot de alcance y comprueban nuevas lecturas de actor y recurso rechazadas.

Riesgo R2; A0 diff de pruebas y documentación. A1/A2 positivos y negativos sujetos a GitHub-hosted. A3 permanece LIMITACIÓN: estas pruebas emplean EF InMemory, **NO** Oracle EF/MySQL 8.4 físico ni HTTP comercial; la integración requerida sigue pendiente y ninguna certificación global se infiere. A4 exigir SUCCESS exact-SHA de Scope Lock, V5, Backend Release/xUnit, seguridad/autorización pertinentes. La opción DEV de lectura sigue OFF por defecto, sin permisos nuevos, sin API comercial habilitada ni cambios de esquema. Autor histórico DEV03 intacto, reparador DEV06; A5 futura por Task Javier diferente de ambos. H=VALIDANDO, no LISTO, QA/main/PROD. Rollback mediante git revert ordinario con HEAD revalidado.

## V25 — comprobación física Oracle EF/MySQL 8.4, DEV05 reparación independiente

Nueva hipótesis causal: la evaluación relacional anterior estaba respaldada por EF InMemory, que no prueba traducción SQL ni transacciones en el proveedor Oracle. La regresión `C0303M0303VerifiedAuditMysql84IntegrationTests` se ejecuta sólo con `Category=Integration` sobre MySQL `8.4` efímero del job global, conexión limitada a loopback `127.0.0.1:3306`, esquema aleatorio con bootstrap, cleanup y `SELECT VERSION()`; utiliza `UsuarioScopeService` auténtico, lector API preexistente, JwtBearer firmado, membership A/B, grant real, revocación en SQL, lectura denegada y rollback. El JWT se autentica dentro de `DefaultHttpContext` de ensayo: NO prueba HTTP transportado por servidor ni un endpoint publicado, ni call-site de auditoría durable. `TransactionalSinkVerified` sigue `false`. A3 física sólo acreditable después de TRX con `passed=1,failed=0,notExecuted=0` y checks exact-SHA; antes de ese resultado H permanece VALIDANDO y A5 independiente, jamás PASS inferido. No se modifica runtime, migración ni despliegue. Rollback git revert normal, PROJECT_SCOPE_LOCK STRICT, no QA/main/PROD.


## V25 - HTTP TestServer y JWT real sobre MySQL 8.4 efimero (incremento DEV03)

El test C0303M0303VerifiedAuditMysql84IntegrationTests amplia la prueba fisica
Oracle EF/MySQL 8.4 con un servidor HTTP ASP.NET Core en proceso que
ejecuta routing, UseAuthentication, UseAuthorization, JwtBearer firmado y
el adaptador relacional C0303VerifiedAuditTenantReaders. La ruta
/_ci/audit/{empresaId} existe unicamente dentro del TestServer de xUnit.
El marcador del tenant A se coloca exclusivamente en el middleware del
servidor de ensayo; una URL de tenant B no puede cambiarlo.

Criterios adversariales con Category=Integration en GitHub-hosted:
JWT valido/tenant A devuelve 204, JWT valido/ruta de tenant B devuelve 403,
firma ajena devuelve 401 y JWT expirado devuelve 401, todos con MySQL 8.4
efimero, sin secretos ni datos de produccion. A0/A1/A2/A3 permanecen
PENDIENTES de prueba fisica exact-SHA hasta observar CI; no convertir
codigo publicado en PASS. La prueba HTTP es un contrato de transporte
de adaptador de CI, no demuestra endpoint comercial publicado, call-site
de cambios criticos, escritor durable, retencion ni auditoria transaccional.
TransactionalSinkVerified=false permanece obligatorio y el opt-in de
produccion sigue OFF. Esa brecha requiere trabajo posterior y A5 independiente,
sin marcar LISTO antes de cumplir alcance y calidad A0-A4. Rollback: git revert
normal del SHA causal con HEAD revalidado; no QA/main/PROD.


## V25 - DEV05: correccion P2 de registro DI fuera de Development

Hallazgo nuevo: Program.cs registraba las cuatro dependencias de preflight (lector, ambas interfaces y servicio Application) fuera del bloque Development, contradiciendo DEV-only aun con opt-in OFF. El riesgo era un grafo de servicios disponible en staging/production si codigo futuro lo consumia. El nuevo metodo AddC0303VerifiedAuditDevelopmentOnly aplica IsDevelopment a la composicion real; fuera de Development no registra ninguno, y en Development conserva lifetime scoped y flag SecurityAudit:EnableReadOnlyAuthorizationProbe desactivado por defecto. No crea HTTP comercial, grant, sink durable, migracion, secretos ni cambia ambientes externos.

Regresiones xUnit verifican 4/4 registros scoped solo en Development, denegacion en Staging y Production y fallo cerrado ante environment nulo. CI exact SHA (backend Release, seguridad, Scope Lock, V5) y prueba relacional MySQL 8.4 de la version causal siguen siendo exigibles; no se atribuye PASS a tests queued/in_progress/skipped. El alcance sigue VALIDANDO con autor historico DEV03; A5 por revisor distinto de autor y reparadores; no QA/main/PROD. Revertir mediante git revert normal y HEAD/CAS.

## V25 — Endpoint read-only Development por mapeo real y reparación de hospedaje .NET 10

La ruta `/__dev/audit-authorization/{empresaId:int}` se registra desde `Program.cs` **únicamente** en `Development` y con `SecurityAudit:EnableReadOnlyAuthorizationProbe=true` (valor predeterminado falso). No existe en Staging/Production. La solicitud debe poseer JWT válido, selector coincidente, membresía empresarial vigente y el permiso relacional `Auditoria.Ver`; se relee la autoridad antes de emitir únicamente HTTP 204 sin payload. Selección de tenant por URL/cabecera no concede RBAC. Cualquier 401/403 es denegación segura y no hay mutaciones ni permisos nuevos. El middleware P0 global continúa y puede rechazar la ruta en multiempresa: el test host de este scope acredita el mapeo real pero no una excepción comercial P0.

El caso `C0303M0303VerifiedAuditMysql84IntegrationTests` usa **el mismo mapeo** que la aplicación, sin middleware que coloque manualmente un marcador privilegiado; autentica JWT firmado A/B, firma falsa, expiración y selector cruzado con MySQL 8.4 efímero. El laboratorio migra del constructor obsoleto `WebHostBuilder/TestServer(IWebHostBuilder)` a `HostBuilder.ConfigureWebHost(...UseTestServer()).StartAsync()` para evitar ASPDEPR004/ASPDEPR008 con `-warnaserror` en .NET 10. El pipeline HTTP de pruebas no sustituye el API comercial integral ni certifica writer o sink persistente, retención, logger productor, despliegue o A5. Scope continúa VALIDANDO hasta verificación de CI exact-SHA; rollback con git revert sin tocar QA/main/PROD.

## V26 — regresión de aislamiento JwtBearer por solicitud en MySQL 8.4 (DEV03, 2026-11-23)

Fallo real en CI SHA `fccd449e266ebdea4628b70d82e915ed010fcb2e`, job `137572782704`: `AssertUntrustedBearerRejectedAsync` esperaba null, pero el token de firma ajena reutilizaba un resultado autenticado. Nueva hipótesis causal: el laboratorio creaba varios `DefaultHttpContext` con el mismo **provider raíz**; el proveedor de handlers de autenticación es scoped y puede conservar el handler del contexto positivo. Esto no demuestra una omisión criptográfica en el pipeline HTTP comercial.

La corrección usa `CreateAsyncScope()` nuevo para el request válido y uno distinto para cada intento falso/expirado; afirma `AuthenticateAsync(...).Succeeded == false` antes de consultar el adaptador. Preserva `JwtBearer` real, claves diferentes, expiración, pertenencias A/B, autorización SQL Oracle/MySQL 8.4, revocación, rollback y endpoint DEV-only. No se habilita sink durable, logger de producto ni permisos de producción. Prueba real Category=Integration queda PENDIENTE de GitHub-hosted exact-SHA; no marcar LISTO/A5 mientras no exista evidencia. Rollback mediante `git revert` fast-forward del commit, sin QA/main/PROD.

## Incremento DEV-only: registro persistente de lectura autenticada (23-11-2026)

La misma autorización relacional, tenant A/B, firma JWT real y denegación por defecto que protege GET se reutiliza en el nuevo POST opt-in \`/__dev/audit-authorization/{empresaId}/record\`. Ambas rutas se registran **únicamente** si el entorno es Development y el flag \`SecurityAudit:EnableReadOnlyAuthorizationProbe\` está habilitado explícitamente. GET continúa sin escribir. POST sólo registra un acceso \`Auditoria.Ver\` en la bitácora real \`RegistroAuditoria\` de MySQL mediante el servicio ya existente \`IAuditoriaService.RegistrarEstrictoAsync\`. Usuario y tenant proceden del contexto autenticado y marcador server-side; una cabecera/URL nunca conceden autoridad. Error de persistencia: HTTP 503 sin datos SQL ni JWT. Sin escritura a entidades de negocio.

La regresión GitHub-hosted de este scope usa MySQL 8.4 efímero con aislamiento de esquema y verifica GET sin registro, POST positivo con actor/tenant/módulo/acción persistidos, y negativas cross-tenant, JWT falsificado/expirado y selector ajeno sin filas nuevas. El esquema se elimina al concluir la prueba. Este incremento **no certifica** integración con un call-site de negocio productivo, retención durable a largo plazo, transacciones compartidas con mutaciones comerciales, autorización de plataforma ni operación real en producción: son brechas de alcance que mantienen \`PLAN_MAESTRO H520=VALIDANDO\` y requieren evaluación A0–A4 causal y A5 independiente, nunca un PASS anticipado. Rollback permitido: git revert normal; el flag continúa deshabilitado fuera de la prueba.

