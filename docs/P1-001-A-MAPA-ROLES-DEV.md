# P1-001.A — Mapa verificable de roles y membresías (DEV)

## Corte de solo lectura — 2026-10-29

La conexión confirmada en DBeaver fue `solqaryn_dev` (Aiven MySQL 8.4.11); no se consultaron ni modificaron otras bases. Se observaron 3 empresas activas, 7 membresías empresariales activas, 3 roles, 239 permisos activos, 263 vínculos RolPermisos y 1 asignación de rol de plataforma. Los conteos agregados de membresías activas incompatibles y asignaciones de plataforma incompatibles fueron ambos **0**; los grupos duplicados de roles personalizados por empresa/nombre normalizado fueron **0**.

Las migraciones `20261014130000_CommercialTenantNaturalKeysPostStorefront` y `20261018200000_TenantRoleOwnershipDeterministicBackfill` aparecen en `__EFMigrationsHistory` con EF `10.0.12`. Sin embargo, `INFORMATION_SCHEMA.STATISTICS` muestra `UX_Roles_Empresa_NombreNormalizado` con `NON_UNIQUE=1` sobre `(EmpresaId,NombreNormalizado)`, aunque el modelo y la migración declaran unicidad. `UX_Roles_NombreNormalizadoGlobalUnico` sí aparece único (`NON_UNIQUE=0`) y `NombreNormalizadoGlobalUnico` existe como columna virtual. Esto es una discrepancia física real; que actualmente no haya duplicados no sustituye la restricción de unicidad.

Se confirmó que `GET /roles` en la API Render DEV responde `401` sin JWT. Esa respuesta acredita el gate de autenticación del endpoint, pero no las pruebas HTTP/JWT A/B con dos membresías válidas. La prueba existente `P1001ARolesHttpJwtIsolationTests` sigue usando JWT de prueba, TestServer y EF InMemory, no la API ni MySQL DEV reales.

La migración correctiva nueva reemplaza el índice no único por el único mediante un solo `ALTER TABLE`; no se ha aplicado a la base. Antes de cerrar P1-001.A aún se requiere CI y revisión independiente del PR, deployment de esa migración en DEV y readback de `NON_UNIQUE=0`, más la prueba autenticada A/B contra el servicio DEV real. El estado permanece **EN_PROGRESO / NO_LISTO**. No se leyeron filas de usuarios, nombres, correos, contraseñas ni datos comerciales.

**Estado:** inventario de código y contrato EF/MySQL; **NO** es un censo de filas de la base DEV.  
**Microtarea:** P1-001.A · Fuente comercial única: `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` (Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`).  
**Base inspeccionada:** `dev@785911f1095f13c0b6c0e859c35e0a7b733fcb3c` (2026-10-26 UTC).  
**Seguridad:** `PROJECT_SCOPE_LOCK=STRICT`. Lectura de repositorio y esquema declarado únicamente; cero credenciales, consultas a datos productivos, seeds nuevos o escrituras MySQL.

## 1. Modelo de autoridad hallado en el código

| Categoría | Contrato/criterio | Persistencia declarada | Fuente y regla efectiva |
| --- | --- | --- | --- |
| Plataforma global | `Rol.Ambito=Plataforma (2)`, `Rol.EmpresaId=NULL`, `Rol.EsAdministrador=true`; la asignación debe estar activa y ser inequívoca | `Roles` + `UsuarioRolesPlataforma` + `Usuarios` | `PlataformaScopeService.ObtenerActualAsync` exige sesión válida, usuario activo/no bloqueado/no eliminado, rol activo/no eliminado, ámbito Plataforma, sin EmpresaId y **exactamente una** coincidencia. Los claims JWT no confieren este privilegio. |
| Empresa — sistema/plantilla | `Rol.Ambito=Empresa (1)`, `Rol.EmpresaId=NULL`, `EsSistema=true` y activo/no eliminado | `Roles` + `UsuarioEmpresas` | `Rol.EsAsignableAEmpresa` y `UsuarioScopeService.ContextosTenantValidos`: rol plantilla asignable dentro de una membresía empresarial válida, no propietario global de datos. |
| Empresa — personalizado | `Rol.Ambito=Empresa (1)`, `EmpresaId>0`, `EsSistema=false` | `Roles` + `UsuarioEmpresas` | `RolService.CreateAsync` obtiene `EmpresaId` autorizado desde el gate. El scope efectivo exige `Rol.EmpresaId == membresia.EmpresaId` y membresía activa. No cruzar tenant A/B. |
| Empresa — huérfano/ambiguo heredado | `Ambito=Empresa`, `EmpresaId=NULL`, `EsSistema=false` | `Roles` | **NO ASIGNABLE**: un rol sin dueño no se convierte en plantilla. El backfill fechado 20261018200000 no infiere dueño desde membresía; conserva NULL si hay ambigüedad/colisión. |
| Compatibilidad histórica, NO autoridad tenant | `Usuario.RolId` y enum `RolUsuario { Administrador=1, Vendedor=2 }` | `Usuarios` | `Rol.Id` es referencia relacional; la autorización tenant exige `UsuarioEmpresa`. `AuthService` valida rol activo, pero JWT o campo histórico por sí solos no prueban membresía vigente. |

`Permisos` contiene `Codigo`, `Modulo`, `Accion`, `Ambito`, `Activo` y `EsSistema`. `RolPermisos` vincula permisos con roles. Un permiso de plataforma no equivale a un permiso empresarial aunque compartan módulo/acción; el backend es autoridad final.

## 2. Relaciones y reglas EF declaradas

- `Roles`: PK `Id`; CHECK `Ambito IN (1,2)`; si `Ambito=2`, `EmpresaId IS NULL`; `EmpresaId` positivo o NULL y FK opcional. **Modelo EF final:** aunque `RolConfiguration` inicialmente declara `NombreNormalizado` único global, `AppDbContext.OnModelCreating` aplica posteriormente `CommercialTenantNaturalKeysModel.Apply`. Esta última configuración lo sustituye por un índice simple **no único**, una unicidad compuesta `(EmpresaId, NombreNormalizado)` para roles con dueño y la columna calculada `NombreNormalizadoGlobalUnico` única para los roles sin empresa (plantillas/plataforma). Dos empresas pueden usar el mismo nombre normalizado en sus roles personalizados; dos roles sin dueño no pueden repetirlo. **El esquema físico de MySQL DEV sigue NO_VERIFICADO.**
- `UsuarioEmpresas`: PK `Id`; FK `UsuarioId`, `EmpresaId`, `RolId`; `Activa` booleano; índice único `(UsuarioId,EmpresaId)`, por tanto el `RolId` empresarial es mutable y no identifica la fila. Las consultas verifican actividad del usuario, empresa y rol, no únicamente `Activa`.
- `UsuarioRolesPlataforma`: PK `Id`; FK de usuario y rol; `Activa`; índice único `(UsuarioId,RolId)`. **No** es una membresía de empresa.
- `RolPermisos`: clave `Id`; FK `RolId`, `PermisoId`; índice único `(RolId,PermisoId)`. La migración de backfill explica `EmpresaId` shadow para grants tenant; verificar su materialización real antes de atestar MySQL.
- `Permisos`: claves únicas `Codigo` y `(Modulo,Accion)`; ámbito y estado activos se verifican además en consumidores.
- `RolService`: crea roles personalizados solo con `TenantPermissionContext.GetAuthorizedEmpresaId`, impide mutar roles de sistema, plataforma o sin propietario. `RolesController` usa `[Authorize]` y `[RequierePermiso]`; `UsuarioScopeService` y `PlataformaScopeService` aplican reglas concretas en backend.

**Fuentes de código:** `backend/src/Domain/Entities/{Rol,Usuario,UsuarioEmpresa,UsuarioRolPlataforma,RolPermiso,Permiso}.cs`, `backend/src/Domain/Enums/{AmbitoAutorizacion,RolUsuario}.cs`, `backend/src/Infrastructure/Persistence/Configurations/{Rol,UsuarioEmpresa,UsuarioRolPlataforma,RolPermiso,Permiso}Configuration.cs`, `backend/src/Infrastructure/Persistence/{AppDbContext,CommercialTenantNaturalKeysModel,CommercialTenantNaturalKeysPhysicalContract}.cs`, `backend/tests/Solqaryn.Tests/CommercialTenantNaturalKeysTests.cs`, `backend/src/Infrastructure/Services/{UsuarioScopeService,PlataformaScopeService}.cs`, `backend/src/Infrastructure/Repositories/RolRepository.cs`, `backend/src/Application/Services/{AuthService,RolService}.cs`, `backend/src/API/Controllers/RolesController.cs`, `backend/src/Infrastructure.Migrations/Migrations/20261018200000_TenantRoleOwnershipDeterministicBackfill.cs`.

## 3. Hallazgos para seguimiento, sin atribuir explotación

1. **Lectura multiempresa: filtro EF comprobado estáticamente, no HTTP.** `RequierePermisoAttribute` resuelve y verifica la empresa antes de marcar `TenantPermissionContext.MarkAuthorizedEmpresaId`, y `AppDbContext.OnModelCreating` aplica `CommercialTenantReadFilterModel`. La consulta EF `Roles` usa `AuthorizedReadEmpresaId` de contexto verificado; cuando está presente, permite únicamente el rol propio o la plantilla de sistema de ámbito Empresa sin dueño. `RolRepository.GetAllAsync/GetByIdAsync` no agrega un predicado explícito y depende de ese filtro EF; `scopeAbsent` admite el catálogo completo en ejecuciones sin contexto RBAC. **No equivale a una prueba HTTP/MySQL DEV.** Probar lectura A/B, `GET /roles/{id}` ajeno, contexto ausente y `IgnoreQueryFilters` en consumidores relevantes antes de certificar aislamiento.
2. **Unicidad efectiva por tenant (corrección de lectura de código):** la declaración global inicial de `RolConfiguration` **no es el modelo final**: `CommercialTenantNaturalKeysModel.Apply`, llamado después por `AppDbContext`, establece `UX_Roles_Empresa_NombreNormalizado` único por empresa y desactiva la unicidad del índice simple; conserva `UX_Roles_NombreNormalizadoGlobalUnico` único para roles sin dueño. `CommercialTenantNaturalKeysTests` y el contrato de índice físico verifican este diseño en el repositorio. Confirmar en `INFORMATION_SCHEMA` que esas tres reglas están realmente desplegadas en MySQL DEV; un modelo EF o una migración versionada **no certifican** el esquema vivo.
3. **Backfill y legado:** roles `EsSistema=false,EmpresaId=NULL` quedan sin titular hasta evidencia determinista; no regularizarlos por inferencia ni darles permisos automáticamente.
4. **Evidencia MySQL faltante:** no se inspeccionaron credenciales, filas vivas, `INFORMATION_SCHEMA`, constraints desplegadas ni pruebas E2E con usuarios reales. Cualquier cantidad de roles, membresías o anomalías reales se deja **NO_VERIFICADO**. No declarar P1-001.A LISTO por esta acta.

### 3.1. Escrituras de roles: alcance de la defensa a verificar

`RolesController` aplica `RequierePermiso` a PUT/PATCH/DELETE, pero las rutas Update/Activar/Desactivar/Eliminar pasan solo `id` al `RolService`; `ExigirRolMutable` verifica tipo y dueño positivo, **no compara allí** `Rol.EmpresaId` contra el tenant autenticado. Las consultas normales pueden quedar limitadas por el filtro EF, y los guards de persistencia pueden agregar una segunda defensa; no se atribuye una vulnerabilidad explotable sin ejecutar negativas A/B. Exigir prueba de rechazo de IDs de B en GET, PUT, PATCH y DELETE desde A y prueba del contexto de lectura/escritura sin marca. No ampliar permisos ni editar datos reales para probarlo.

**CI vs evidencia viva:** el workflow de alcance, frontend, backend y seguridad pueden aprobar el SHA del PR en GitHub Actions; eso acredita pruebas de repositorio y **no** la instancia MySQL DEV ni autenticación HTTP A/B. Cualquier hallazgo de aislamiento debe corregirse en un changeset acotado, con test rojo previo, revisión independiente y CI exact SHA antes de declarar aceptación.

### 3.2. Regresión adicional en memoria para lecturas y mutaciones A/B

La prueba de xUnit `backend/tests/Solqaryn.Tests/P1001ATenantRoleIsolationTests.cs` ejercita el `RolRepository` real y el `RolService` sobre EF Core InMemory con dos empresas ficticias. Verifica lectura propia y ocultamiento del rol B por lista/ID al estar marcada la empresa A, rechazo sin auditoría de seis operaciones de aplicación por ID ajeno (editar, activar, desactivar, eliminar lógico, eliminar permanente y duplicar), ausencia de cambios pendientes y control positivo al seleccionar B.

**Límite de la evidencia:** el contexto de empresa se marca deliberadamente en la prueba y no proviene de JWT ni de una petición HTTP real; tampoco ejecuta SQL en MySQL DEV. La prueba es una regresión de filtro EF y rutas de servicio con datos sintéticos, **no certificación funcional multiempresa**. Su resultado CI debe acreditarse sobre el SHA final de este PR. La aceptación sigue pendiente del MySQL DEV físico y negativas HTTP autenticadas A/B autorizadas.

## 4. Verificación MySQL DEV de solo lectura pendiente

Ejecutar únicamente por operador autorizado en la instancia DEV identificada y con cuenta read-only, sin exportar PII ni compartir usuarios. Comparar migraciones aplicadas con el esquema declarado y registrar SHA de código, timestamp UTC y resultados agregados:

```sql
-- Distribución de roles por ámbito, dueño y origen; solo conteos.
SELECT Ambito, EsSistema, (EmpresaId IS NOT NULL) AS TieneEmpresa,
       Activo, Eliminado, COUNT(*) AS Total
FROM Roles
GROUP BY Ambito, EsSistema, (EmpresaId IS NOT NULL), Activo, Eliminado;

-- Asignaciones empresariales activas con rol de plataforma,
-- rol personalizado de otra empresa o huérfano no plantilla.
SELECT COUNT(*) AS MembresiasInconsistentes
FROM UsuarioEmpresas ue
INNER JOIN Roles r ON r.Id = ue.RolId
WHERE ue.Activa = 1
  AND (r.Ambito <> 1
       OR (r.EmpresaId IS NOT NULL AND r.EmpresaId <> ue.EmpresaId)
       OR (r.EmpresaId IS NULL AND r.EsSistema = 0));

-- Autoridades globales con rol incompatible o titular de empresa.
SELECT COUNT(*) AS AsignacionesPlataformaInconsistentes
FROM UsuarioRolesPlataforma urp
INNER JOIN Roles r ON r.Id = urp.RolId
WHERE urp.Activa = 1
  AND (r.Ambito <> 2 OR r.EmpresaId IS NOT NULL OR r.EsAdministrador = 0);
```

```sql
-- Contrato físico esperado de nombres de roles (solo metadatos, sin PII).
SELECT INDEX_NAME, NON_UNIQUE,
       GROUP_CONCAT(COLUMN_NAME ORDER BY SEQ_IN_INDEX SEPARATOR ',') AS Columnas
FROM INFORMATION_SCHEMA.STATISTICS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Roles'
  AND INDEX_NAME IN ('UX_Roles_Empresa_NombreNormalizado',
                     'IX_Roles_NombreNormalizado',
                     'UX_Roles_NombreNormalizadoGlobalUnico')
GROUP BY INDEX_NAME, NON_UNIQUE;

-- Esperado: (EmpresaId,NombreNormalizado) único (NON_UNIQUE=0),
-- NombreNormalizado simple no único (NON_UNIQUE=1),
-- NombreNormalizadoGlobalUnico único (NON_UNIQUE=0).
-- Verificar asimismo que la columna calculada está desplegada:
SELECT COLUMN_NAME, GENERATION_EXPRESSION, EXTRA
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Roles'
  AND COLUMN_NAME = 'NombreNormalizadoGlobalUnico';
```

No interpretar cero filas anómalas como prueba de autorización HTTP o de esquema: falta verificar índices físicos, scopes RBAC, denegación A/B y estado de despliegue.

## 5. Aceptación y rollback

**Verificable ahora:** inventario trazado a fuente GitHub del SHA exacto y comprobador Node read-only con pruebas negativas sintéticas; CI GitHub Actions del PR cuando se ejecute.  
**Pendiente para certificación funcional:** readback MySQL DEV/constraints, prueba HTTP autenticada tenant A/B, autorización/denegación efectiva para roles custom y plataforma, revisión independiente y CI del SHA del PR. Se admite acta de reutilización únicamente si esas pruebas demuestran que el comportamiento actual cumple.  
**Rollback:** revert protegido únicamente de documento/script/workflow; ningún DDL, cuenta, rol, grant, configuración productiva o permiso real se altera por este PR.

## RECOVER — ensayo HTTP/JWT efimero de roles A/B (2026-10-28)

Se incorpora `backend/tests/Solqaryn.Tests/P1001ARolesHttpJwtIsolationTests.cs` con TestServer real: JWT firmado unicamente para la prueba, `RolesController` y `RolService/RolRepository` reales, filtro `RequierePermisoAttribute` real, filtro EF InMemory por marca de tenant verificada y stub de `IPermisoService` que relaciona la cabecera con el claim de prueba. Casos: A consulta ID privado B = 404 tras entrar al controlador, lecturas A/B propias = 200, listado A sin rol B y ausencia de JWT = 401. Se agrega `Microsoft.AspNetCore.TestHost` 10.0.12 solo al proyecto de pruebas.

**Evidencia todavía pendiente:** compilacion y resultado CI del SHA nuevo, permiso real de membresias de MySQL DEV, estructura e indices fisicos `INFORMATION_SCHEMA`, HTTP/JWT autenticado contra DEV real y revision independiente. Este ensayo NO certifica el permission service real ni MySQL DEV, NO modifica codigo runtime y NO autoriza VALIDANDO/LISTO. No se emplean credenciales ni entornos externos.

### CI del primer ensayo y eliminación de API de hosting obsoleta

En el primer SHA `5a48f574bc1aa124c61abff9cb385911c882682d`, CI ERP #40705289614 ejecutó el test dentro de 2906 tests backend PASS y 947 de seguridad PASS, además de frontend SUCCESS; Scope Lock #40705289693 y RBAC #40705290092 SUCCESS. Checkout nativo `refs/pull/3764/merge` SHA `cf89a28`, por tanto CI del merge sintético, no checkout literal del SHA aislado. El compilador emitió advertencias ASPDEPR004 y ASPDEPR008 por `WebHostBuilder` y `TestServer(IWebHostBuilder)`. El cambio siguiente actualiza únicamente el harness efímero a `HostBuilder` + `UseTestServer` + `StartAsync`, sin cambiar tests de seguridad ni runtime. CI del SHA posterior PENDIENTE; evidencia MySQL DEV / JWT contra DEV real y revisión independiente continúan NO_VERIFICADAS.
