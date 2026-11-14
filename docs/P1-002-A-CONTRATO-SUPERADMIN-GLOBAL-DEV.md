# P1-002.A — Contrato de superadministracion global con limite tenant (DEV)

**Origen:** STANDARD DE IMPLEMENTACION DE AUTOMATIZACIONES, P1-002.A, PLAN_MAESTRO fila 28.  
**Rama:** `work/solqaryn-p1-002-a` (base `dev@0cec1a3b06793c95dceb3f316c1ae4cb22951f76`).  
**Estado del entregable:** propuesta sometida a revision; NO certificada como `LISTO`.  
**Alcance:** contrato de autoridad y una negativa dirigida; no se modifica la politica de autorizacion ni se conceden privilegios nuevos.

## Fronteras efectivamente inspeccionadas

| Superficie | Evidencia del codigo DEV | Frontera |
| --- | --- | --- |
| `PlataformaScopeService.ObtenerActualAsync` | Revalida en MySQL identidad JWT vigente, usuario activo y exactamente una asignacion activa de rol de Plataforma con `EmpresaId=null` | Una sesion global no obtiene automaticamente una membresia empresarial |
| `GET /plataforma/sesion` | `[Authorize]`, `PlataformaSesionController`, respuesta sin `EmpresaId` y auditoria con `CorrelationId`; no almacena seleccion de tenant | Solo confirma acceso a la consola SaaS |
| `PlataformaGestionMvpController` | Operaciones SaaS de usuarios, empresas y membresias con scope Plataforma y auditoria | Las rutas empresariales explicitan su `empresaId`; no equivalen a acceso irrestricto al ERP de los tenants |
| `UsuarioScopeService.ObtenerActualAsync(empresaId)` | Exige membresia activa de usuario, empresa y rol empresarial valido; descarta rol de Plataforma como rol empresarial | Una asignacion global no sustituye permiso comercial |
| `RequierePermisoAttribute` + `TenantPermissionContext` | Resuelve candidato de tenant, exige permiso de servidor, y **solo despues** marca el `EmpresaId` autorizado | La cabecera `X-Empresa-Id` no es prueba de autoridad |

**Nota de seguridad:** `IgnoreQueryFilters` en `PlataformaScopeService` se usa exclusivamente para verificar entidades de identidad y su estado, no para consultar registros comerciales. El acceso de Plataforma se revalida en cada solicitud.

## Contrato requerido para expansion a modulos empresariales

1. **Ambitos separados.** El superadministrador puede operar la consola SaaS con grant global vigente. Ese rol no da por defecto consultas, exportaciones ni mutaciones del ERP perteneciente a todas las empresas.
2. **Contexto empresarial explicito y verificado.** Una accion administrativa dentro del ERP de una empresa exige un `EmpresaId` positivo y existente, seleccion inequívoca y un grant empresarial/delegacion especifica autorizada y revalidada en servidor. JWT, `Usuario.RolId`, slug, Host y `X-Empresa-Id` no son grants. No usar fallback a "primera empresa".
3. **Motivo y auditoria obligatorios para delegacion futura.** Cualquier futura entrada global excepcional al tenant requiere motivo valido, identificador de actor, objetivo, `EmpresaId`, accion, instante y `CorrelationId` sin PII ni secretos en el registro. Si el registro de auditoria falla, la operacion privilegiada debe fallar cerrado.
4. **Denegaciones.** Sin contexto, motivo o autorizacion vigente, tenant suspendido, rol ambiguo/revocado o solicitud A->B: denegar. El administrador empresarial de A nunca recibe autoridad SaaS ni privilegios sobre B por accion de interfaz.
5. **No bypass.** No incorporar un rol universal, no desactivar filtros EF, no relajar JWT/RBAC, ni insertar una membresia ficticia para facilitar pruebas. El contexto global y el contexto comercial se prueban por separado.

Este documento **define el contrato**, no habilita todavia un mecanismo nuevo de delegacion sobre todos los modulos empresariales. La implementacion de un acceso delegado requerira un changeset acotado con contrato API/DTO explicito, razon auditada, control de vigencia, consultas tenant-aware, revision de impacto y pruebas E2E A/B; no puede inferirse de este inventario.

## Prueba dirigida y negativa incorporada

`backend/tests/Solqaryn.Tests/API/Controllers/P1002ASuperadminGlobalTenantBoundaryTests.cs` construye dos empresas ficticias y un usuario con rol global activo. Ejercita `PlataformaScopeService` y `UsuarioScopeService` reales sobre EF Core InMemory. El control positivo requiere que el grant de Plataforma sea reconocido. Las negativas comprueban que no hay contexto comercial para A ni B, que no puede inferirse un tenant unico y que una membresia posterior en A no autoriza B.

**Limite explicito:** este test no ejecuta HTTP/JWT E2E real, no acredita MySQL fisico, no valida una delegacion excepcional con motivo, no accede a Aiven ni despliega proveedores. P1-002.B debe certificar los endpoints y rechazos de borde sobre un ambiente autorizado; ese trabajo no se sustituye por estas pruebas InMemory.

## Impacto, comprobaciones y rollback

- **Componente:** autorizacion Plataforma/tenant en backend. En este changeset solo se agrega prueba a `backend/tests` y acta `docs`; no se cambia runtime, frontend, DTO, entidad, migracion ni contrato API vigente.
- **Consumidores:** consola Plataforma, servicios de usuarios y endpoints comerciales con `RequierePermiso`; comportamiento actual preservado.
- **Datos y seguridad:** escenarios sinteticos en EF InMemory, ninguna escritura a MySQL/Aiven; no se copian credenciales, PII o valores de secretos; aislamiento JWT/RBAC/tenant permanece inalterado.
- **Validacion pendiente:** compilar y ejecutar prueba dirigida en CI del PR, tres checks obligatorios exact-SHA, review GitHub independiente y merge protegido a `dev`. No declarar PASS antes de esos readbacks.
- **Rollback:** revert del changeset mediante PR protegido sin tocar base de datos ni despliegues. Si falla la prueba, corregir en la misma rama y repetir CI, nunca rebajar el control negativo.

**Fuentes de codigo:** `backend/src/Infrastructure/Services/{PlataformaScopeService,UsuarioScopeService}.cs`, `backend/src/API/Controllers/{PlataformaSesionController,PlataformaGestionMvpController}.cs`, `backend/src/API/Filters/{RequierePermisoAttribute,TenantPermissionContext}.cs` y `backend/tests/Solqaryn.Tests/API/Controllers/{PlataformaSesionSecurityTests,PlataformaGestionMvpSecurityTests}.cs`. Estado sujeto a revalidacion del HEAD antes del merge.
