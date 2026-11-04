# P1-009.A — Plantillas, propietario y unicidad de roles (DEV)

## Autoridad y scope
- Plan rector: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, documento institucional `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`.
- Registro: `PLAN_MAESTRO!42` / `COLA!42`, prioridad P1, orden 41; dependencia `P0-012.B=LISTO`, `COLA!G42=SI`.
- Task propietaria: `JAVIER_CHATGPT`, ID `6aeacb7820ac8191b7d768a6f33dfa09`, cuenta GitHub propia `jmejia31`.
- Rama de trabajo exclusiva: `work/solqaryn-p1-009-a`, desde `dev@30bf43749a8eb7b164a065f5aabebfa024aa0faa`. PR hacia `dev`.
- Decisión: REUTILIZAR el runtime ya fusionado por PR [#3669](https://github.com/solqaryn/Solqaryn/pull/3669), sin duplicar servicios ni migraciones.

## Evidencia existente y frontera
1. `Rol.EsAsignableAEmpresa` concede roles empresariales privados solo al `EmpresaId` propietario, o plantillas de sistema sin propietario y ámbito empresarial. Rechaza B, global/Plataforma, huérfano no plantilla, inactivo o eliminado.
2. `RolesController` requiere autorización y pasa la identidad empresarial verificada por `TenantPermissionContext`; `RolService` rechaza crear/duplicar sin contexto o desde un rol ajeno. `UsuarioService` y `UsuarioAdministracionEmpresaService` impiden asignación cruzada.
3. `RolConfiguration` declara el índice único `IX_Roles_NombreNormalizado` (modelo: `HasIndex(...).IsUnique()`) y restricciones `CK_Roles_Ambito`, `CK_Roles_Ambito_Empresa`, `CK_Roles_EmpresaId_Positivo`. Esta **unicidad es global por nombre normalizado**, no por empresa; mantenerla sin cambio de diseño o DDL en P1-009.A. No asumir que dos tenants pueden crear roles privados del mismo nombre.
4. Migración histórica `20261018200000_TenantRoleOwnershipDeterministicBackfill`: con varias empresas, roles privados históricos sin propietario quedan en cuarentena en vez de asignarse a partir de datos ambiguos. No repetir migraciones ni alterar datos.

## Incremento dirigido y negativos
- Nuevo xUnit `P1009ARoleTemplateUniquenessTests`: plantilla válida compartida A/B, rol privado de A no asignable a B, rechazo de rol B/global/huérfano para A, metadatos EF de unicidad, y duplicado normalizado con bloqueo anterior a `AddAsync`/`SaveChangesAsync`.
- Suite reutilizada sin modificar: `RolServiceTenantOwnershipTests`, `UsuarioTenantRoleOwnershipTests`, `P1001BRoleAssignmentCertificationTests`, `TenantRoleOwnershipBackfillMySqlTests` y `P1005BRolesEmpresarialesCertificacionTests`.
- Prueba dirigida en CI: `dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --configuration Release --filter FullyQualifiedName~P1009A`; el workflow comercial dirigido exige al menos un caso PASS en el SHA físico del PR.
- Los tests en memoria y metadatos EF **no certifican el índice físico de Aiven ni respuestas HTTP 4xx reales**. La conversión a 4xx depende de la capa de excepciones/HTTP y requiere verificación independiente cuando aplique. Ninguna de estas pruebas sustituye aceptación MySQL/JWT live de DEV.

## Impacto, seguridad y cierre
- Diff limitado a xUnit, esta acta y `CHANGELOG_AI.md`. Backend runtime, frontend, API, JWT, RBAC, migraciones, Aiven, Render, Vercel, secretos, QA/main/PROD sin cambios.
- Riesgo de regresión reducido por reutilizar invariantes actuales; no ampliar permisos ni inferir owner. No tocar plan calculado `COLA`.
- **Estado: CANDIDATO/EN_PROGRESO.** CI exact-HEAD y Scope Lock, test dirigido, revisión de seguridad y readback de PR requeridos; no `LISTO` ni merge hasta todos los gates. Certificación de P1-009.B permanece separada.
- Rollback: nuevo PR protegido que revierta solo estos tests/acta/changelog; no hay datos ni banderas activadas.
