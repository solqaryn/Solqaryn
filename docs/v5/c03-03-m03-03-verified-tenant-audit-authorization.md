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
