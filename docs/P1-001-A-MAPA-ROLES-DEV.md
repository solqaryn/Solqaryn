# P1-001.A — Mapa verificable de roles y membresías (DEV)

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

- `Roles`: PK `Id`; CHECK `Ambito IN (1,2)`; si `Ambito=2`, `EmpresaId IS NULL`; `EmpresaId` positivo o NULL; `NombreNormalizado` es **único globalmente** en la configuración EF (observar restricción de nombres homónimos entre empresas). FK opcional `EmpresaId`.
- `UsuarioEmpresas`: PK `Id`; FK `UsuarioId`, `EmpresaId`, `RolId`; `Activa` booleano; índice único `(UsuarioId,EmpresaId)`, por tanto el `RolId` empresarial es mutable y no identifica la fila. Las consultas verifican actividad del usuario, empresa y rol, no únicamente `Activa`.
- `UsuarioRolesPlataforma`: PK `Id`; FK de usuario y rol; `Activa`; índice único `(UsuarioId,RolId)`. **No** es una membresía de empresa.
- `RolPermisos`: clave `Id`; FK `RolId`, `PermisoId`; índice único `(RolId,PermisoId)`. La migración de backfill explica `EmpresaId` shadow para grants tenant; verificar su materialización real antes de atestar MySQL.
- `Permisos`: claves únicas `Codigo` y `(Modulo,Accion)`; ámbito y estado activos se verifican además en consumidores.
- `RolService`: crea roles personalizados solo con `TenantPermissionContext.GetAuthorizedEmpresaId`, impide mutar roles de sistema, plataforma o sin propietario. `RolesController` usa `[Authorize]` y `[RequierePermiso]`; `UsuarioScopeService` y `PlataformaScopeService` aplican reglas concretas en backend.

**Fuentes de código:** `backend/src/Domain/Entities/{Rol,Usuario,UsuarioEmpresa,UsuarioRolPlataforma,RolPermiso,Permiso}.cs`, `backend/src/Domain/Enums/{AmbitoAutorizacion,RolUsuario}.cs`, `backend/src/Infrastructure/Persistence/Configurations/{Rol,UsuarioEmpresa,UsuarioRolPlataforma,RolPermiso,Permiso}Configuration.cs`, `backend/src/Infrastructure/Services/{UsuarioScopeService,PlataformaScopeService}.cs`, `backend/src/Infrastructure/Repositories/RolRepository.cs`, `backend/src/Application/Services/{AuthService,RolService}.cs`, `backend/src/API/Controllers/RolesController.cs`, `backend/src/Infrastructure.Migrations/Migrations/20261018200000_TenantRoleOwnershipDeterministicBackfill.cs`.

## 3. Hallazgos para seguimiento, sin atribuir explotación

1. **Lectura multiempresa: filtro EF comprobado estáticamente, no HTTP.** `RequierePermisoAttribute` resuelve y verifica la empresa antes de marcar `TenantPermissionContext.MarkAuthorizedEmpresaId`, y `AppDbContext.OnModelCreating` aplica `CommercialTenantReadFilterModel`. La consulta EF `Roles` usa `AuthorizedReadEmpresaId` de contexto verificado; cuando está presente, permite únicamente el rol propio o la plantilla de sistema de ámbito Empresa sin dueño. `RolRepository.GetAllAsync/GetByIdAsync` no agrega un predicado explícito y depende de ese filtro EF; `scopeAbsent` admite el catálogo completo en ejecuciones sin contexto RBAC. **No equivale a una prueba HTTP/MySQL DEV.** Probar lectura A/B, `GET /roles/{id}` ajeno, contexto ausente y `IgnoreQueryFilters` en consumidores relevantes antes de certificar aislamiento.
2. **Índice de nombre global:** `RolConfiguration` declara unicidad global de `NombreNormalizado`, incluso para personalizados de empresas diferentes; confirmar intención comercial y estado real de la constraint.
3. **Backfill y legado:** roles `EsSistema=false,EmpresaId=NULL` quedan sin titular hasta evidencia determinista; no regularizarlos por inferencia ni darles permisos automáticamente.
4. **Evidencia MySQL faltante:** no se inspeccionaron credenciales, filas vivas, `INFORMATION_SCHEMA`, constraints desplegadas ni pruebas E2E con usuarios reales. Cualquier cantidad de roles, membresías o anomalías reales se deja **NO_VERIFICADO**. No declarar P1-001.A LISTO por esta acta.

### 3.1. Escrituras de roles: alcance de la defensa a verificar

`RolesController` aplica `RequierePermiso` a PUT/PATCH/DELETE, pero las rutas Update/Activar/Desactivar/Eliminar pasan solo `id` al `RolService`; `ExigirRolMutable` verifica tipo y dueño positivo, **no compara allí** `Rol.EmpresaId` contra el tenant autenticado. Las consultas normales pueden quedar limitadas por el filtro EF, y los guards de persistencia pueden agregar una segunda defensa; no se atribuye una vulnerabilidad explotable sin ejecutar negativas A/B. Exigir prueba de rechazo de IDs de B en GET, PUT, PATCH y DELETE desde A y prueba del contexto de lectura/escritura sin marca. No ampliar permisos ni editar datos reales para probarlo.

**CI vs evidencia viva:** el workflow de alcance, frontend, backend y seguridad pueden aprobar el SHA del PR en GitHub Actions; eso acredita pruebas de repositorio y **no** la instancia MySQL DEV ni autenticación HTTP A/B. Cualquier hallazgo de aislamiento debe corregirse en un changeset acotado, con test rojo previo, revisión independiente y CI exact SHA antes de declarar aceptación.

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

No interpretar cero filas anómalas como prueba de autorización HTTP o de esquema: falta verificar índices físicos, scopes RBAC, denegación A/B y estado de despliegue.

## 5. Aceptación y rollback

**Verificable ahora:** inventario trazado a fuente GitHub del SHA exacto y comprobador Node read-only con pruebas negativas sintéticas; CI GitHub Actions del PR cuando se ejecute.  
**Pendiente para certificación funcional:** readback MySQL DEV/constraints, prueba HTTP autenticada tenant A/B, autorización/denegación efectiva para roles custom y plataforma, revisión independiente y CI del SHA del PR. Se admite acta de reutilización únicamente si esas pruebas demuestran que el comportamiento actual cumple.  
**Rollback:** revert protegido únicamente de documento/script/workflow; ningún DDL, cuenta, rol, grant, configuración productiva o permiso real se altera por este PR.
