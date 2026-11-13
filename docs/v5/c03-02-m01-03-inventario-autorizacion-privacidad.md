# SOLQARYN V5 FINAL — C03-02.M01.03: inventario de autorizacion y privacidad

**Fecha:** 2026-11-13. **Tipo:** preparacion documental (M01.03), sin modificaciones ejecutables. **Autor:** PRIMARY DEV 06 — JAVIER, TASK_ID 6af16bbad26881918c283abe06e1f32a. **Repositorio unico:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT.

**Autoridad:** [STANDARD DE IMPLEMENTACION DE AUTOMATIZACIONES](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO / COLA / DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). El resultado exigido de la fase C03-02 es RBAC/ABAC server-side para tenant, sucursal, propiedad, archivos y operaciones, con evidencia negativa BOLA/IDOR, claims falsos, sucursal cruzada y exportacion ajena. **Este entregable solo lee contratos, codigo y evidencias**; no certifica la implementacion futura ni libera M01.06/M02.

## Clasificacion proporcional antes del claim

| Control | Requerido en esta M01.03 | Motivo y evidencia |
| --- | --- | --- |
| dotnet build/test | NO_APLICA | No se modifica C# ni test de runtime; los tests listados son evidencia de codigo existente, no nuevas ejecuciones |
| Node / Angular | NO_APLICA | Ninguna UI, ruta, script npm ni navegacion se altera |
| MySQL 8.4 / migraciones | NO_APLICA | No cambia esquema ni repositorios; pruebas DB futuras deberan usar runner efimero |
| Docker | NO_APLICA | Ninguna imagen ni despliegue se altera |
| Playwright E2E | NO_APLICA | No hay cambio de UX o navegacion |
| Seguridad JWT/RBAC/tenant | SI: lectura y revision de fronteras | Comprobar contratos server-side, permisos por empresa, pruebas negativas y brechas pendientes; no atribuir PASS live |
| Scope Lock exact-SHA | SI | Verificar el check real verify-solqaryn-project-scope del commit documental publicado |

## Contratos y fuentes leidos directamente de dev

Los SHA siguientes son **blobs Git de contenido**, no SHA de una prueba ejecutada. Se deben releer si el codigo cambia.

| Ref | Archivo | Blob SHA | Lo que realmente acredita la lectura |
| --- | --- | --- | --- |
| F01 | backend/src/API/Filters/TenantPermissionContext.cs | df7d49f3e185c538f3b3dafabb36c3a971a7257d | La cabecera X-Empresa-Id es un selector unico/positivo, no una prueba de autoridad; el scope falla cerrado sin contexto valido |
| F02 | backend/src/API/Filters/RequierePermisoAttribute.cs | b25a75b33d9cf809e2bbbf92b8e7f35463d3eea4 | Verifica permiso empresa/modulo/accion con IPermisoService; marca empresa solo tras autorizacion; soporte es excepcion GET restringida |
| F03 | backend/src/API/Filters/RequiereMembresiaEmpresaAttribute.cs | 17c7d191e3e1b34f56c3d27ba4992b512526542b | Exige identidad autenticada y membresia vigente para la empresa seleccionada |
| F04 | backend/src/Application/Interfaces/IUsuarioScopeService.cs | 5345ee2de1b1e942bff8eded658f5dc27c0a6745 | Distingue scope legacy de UsuarioTenantScopeActual; no concede autoridad por rol global |
| F05 | backend/src/Domain/Entities/UsuarioEmpresa.cs | f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c | Membresia usuario/empresa/rol con estado activo; rol efectivo no es necesariamente Usuario.RolId legacy |
| F06 | backend/src/Application/Security/VerifiedCommercialTenant.cs | 2da140ce9d7f47329d39d292d3271596467910cc | Marca autorizada server-side en HttpContext.Items; no se deduce de query, body o header |
| F07 | backend/src/Application/Common/CatalogoPermisosBase.cs | 6060b4c54c0428902603f2157984b693a324e8f3 | Catalogo de modulo/accion; por si solo NO otorga permisos (grant efectivo relacional RolPermiso) |
| F08 | backend/src/Application/Security/PlatformSupportPolicy.cs | b103f0b0ec7b9169c060661f6a4456f8863152cf | Soporte restringido a allowlist de lecturas GET; no autorizacion universal de plataforma |
| F09 | backend/src/Application/Common/RbacN04Authority.cs | 4aec0cd7165b7357d4ba0bd2e964917faf7b3a63 | Contrato relacional legacy Usuario.RolId -> Rol -> RolPermiso -> Permiso: no prueba ABAC integral |

## Pruebas existentes: diferencia entre codigo de prueba y ejecucion causal

| Ref | Archivo de test / blob | Cobertura que el codigo pretende comprobar | Limitacion |
| --- | --- | --- | --- |
| T01 | backend/tests/Solqaryn.Tests/N65FTenantPermissionIsolationTests.cs — 06c183ec49cce364959d969dbfa8562c21eef53a | Falta de empresa, selector tenant, marcado de autoridad; casos positivos/negativos del filtro | Test unitario con servicios de prueba, no MySQL real ni cobertura de todos los controllers |
| T02 | backend/tests/Solqaryn.Tests/P1001ARolesHttpJwtIsolationTests.cs — d0f29269117f7c0d6a08eed1cb31114073717c19 | TestServer HTTP/JWT para rutas de roles y denegaciones entre empresas | El propio test declara grants simulados; no certifica membresias fisicas MySQL |
| T03 | backend/tests/Solqaryn.Tests/P1007ATenantSwitchAuthorizationTests.cs — d519a4ff57da685ada79b17350d5503eede0054b | Cambiar empresa A→B sin membresia y revocacion de A | Mocks del servicio de scope, no runtime live |
| T04 | backend/tests/Solqaryn.Tests/N58ExportSecurityTests.cs — b37385e6362ae53f480c84675b4b6d44b5a0ce54 | Neutralizacion de formulas CSV/XLSX y contrato de nombres de archivo | No demuestra bloqueo de exportacion ajena BOLA |
| T05 | backend/tests/Solqaryn.Tests/CommercialTenantMySqlIsolationIntegrationTests.cs — e1a41bf39e3a7161d921cdfc50b7c76c0d173a42 | Dos empresas en MySQL efimero y separacion relacional de datos | Es codigo de test de integracion: aqui solo se leyo, no se ejecuto |

## Matriz de escenarios exigidos para C03-02.M02-M06

| Riesgo / escenario | Contrato existente | Evidencia que todavia falta antes de certificar la capacidad V5 |
| --- | --- | --- |
| Positivo: usuario A con grant valido sobre recurso A | RequierePermiso + IPermisoService + marca autorizada | API real con identidad, membresia y recurso de empresa A, resultado permitido y auditado |
| Negativo: JWT valido de A intenta id de recurso B (BOLA/IDOR) | Scope por empresa y guardas disponibles | HTTP/API y persistencia real rechazan lectura, escritura, archivo, metadatos y export de B; no filtrar existencia |
| Negativo: claim, cabecera o DTO falsean empresa/rol | TenantPermissionContext y VerifiedCommercialTenant separan selector de autorizacion | Tests de cabeceras multiples, token invalido, rol desactualizado, body/query falsos, sin fallback privilegiado |
| Negativo: usuario con membresia A consulta sucursal B | UsuarioTenantScopeActual provee EmpresaId | Filtros por sucursal + ownership fisico con DB; falta prueba end-to-end dirigida |
| Negativo: export CSV/PDF/XLSX intenta datos de B | N58ExportSecurityTests cubre formula injection | Probar delimitacion del dataset por tenant/rol/sucursal/recurso antes del serializador |
| Negativo: grants revocados o membresia inactiva | IUsuarioScopeService y P1007A especifican falla cerrada | Revalidar el grant por solicitud y sesion, incluidas revocacion y concurrencia |
| Excepcion de soporte | PlatformSupportPolicy limita GET modulos | Sesion acotada, temporal, auditada, sin escritura ni escalado o leakage cross-tenant |

**Brechas inventariadas:** no se acredita en esta microtarea un motor ABAC transversal para propiedad/sucursal/archivo, ni cobertura completa de todos los endpoints, ni pruebas nuevas BOLA/IDOR/exports en HTTP+MySQL. La cuarentena multiempresa en rutas sin alcance comercial no debe levantarse con este inventario.

## Prerrequisitos y no-regresion

DAG_TECNICO valida PREPARACION_DOC_CONDICIONAL para C03-02.M01.03: C01-02.M01.01–.05 y C02-01.M01.01–.05 estaban 10/10 LISTO al preflight; PLAN estado unico H, responsable I y COLA G/O fueron contrastados antes del claim. Esto habilita **solo** preparar la documentacion M01.03; no implica liberar fases, desplegar o habilitar acceso privilegiado.

**Criterios negativos de esta lectura:** (N1) rechazar usar el header como grant; (N2) rechazar asumir que el catalogo de permisos es autorizacion efectiva; (N3) rechazar que tests con mocks prueben MySQL real; (N4) rechazar interpretar la lectura del codigo como A5_PASS; (N5) rechazar concluir que ABAC/BOLA/IDOR ya estan resueltos.

**Rollback documental:** revert normal del commit de esta unidad en dev, sin force-push ni modificar autores, migraciones, datos de usuarios, QA/main/PROD o configuraciones de Tasks. Cualquier implementacion posterior exige A0-A4 sobre su propio SHA y A5 independiente de otra Task Javier autorizada.
