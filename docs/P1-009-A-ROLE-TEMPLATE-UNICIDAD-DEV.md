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
3. `RolConfiguration` inicialmente configura `NombreNormalizado` como único global, **pero NO es el modelo EF final**: `CommercialTenantNaturalKeysModel.Apply` reemplaza esa regla por `UX_Roles_Empresa_NombreNormalizado` **único por `(EmpresaId, NombreNormalizado)`**, un índice simple `IX_Roles_NombreNormalizado` **NO único** y una columna calculada `NombreNormalizadoGlobalUnico` **única solo para roles con `EmpresaId=NULL`** (plantillas y globales). Dos empresas pueden compartir el nombre de sus roles personalizados sin choque de índice compuesto. `CK_Roles_Ambito`, `CK_Roles_Ambito_Empresa` y `CK_Roles_EmpresaId_Positivo` siguen vigentes. No modificar DDL ni inferir un cambio de autorización. El diagnóstico Aiven del 2026-10-29 documentaba discrepancia física del índice por empresa en ese corte; el readback **actual** de ese índice NO se ha efectuado aquí.
4. Migración histórica `20261018200000_TenantRoleOwnershipDeterministicBackfill`: con varias empresas, roles privados históricos sin propietario quedan en cuarentena en vez de asignarse a partir de datos ambiguos. No repetir migraciones ni alterar datos.

## Incremento dirigido y negativos
- Nuevo xUnit `P1009ARoleTemplateUniquenessTests`: plantilla válida compartida A/B, rol privado de A no asignable a B, rechazo de rol B/global/huérfano para A, **tres aserciones del modelo EF final** (índice compuesto único de tenant, columna calculada única de roles sin dueño e índice simple no único) y duplicado normalizado del ámbito consultado bloqueado antes de `AddAsync`/`SaveChangesAsync`.
- Suite reutilizada sin modificar: `RolServiceTenantOwnershipTests`, `UsuarioTenantRoleOwnershipTests`, `P1001BRoleAssignmentCertificationTests`, `TenantRoleOwnershipBackfillMySqlTests` y `P1005BRolesEmpresarialesCertificacionTests`.
- Prueba dirigida en CI: `dotnet test backend/tests/Solqaryn.Tests/Solqaryn.Tests.csproj --configuration Release --filter FullyQualifiedName~P1009A`; el workflow comercial dirigido exige al menos un caso PASS en el SHA físico del PR.
- Primera ejecución dirigida sobre PR #3841, SHA `e89d57189c261a48378599d3a5a7ca343867978c`: 5 PASS / 1 FAIL (test de modelo que confundió unicidad intermedia con final). Este commit corrige la **aserción**, no el esquema, conservando evidencia del fallo anterior. El nuevo SHA requiere ejecución dirigida independiente; no extrapolar PASS.
- Los tests en memoria y metadatos EF **no certifican el índice físico de Aiven ni respuestas HTTP 4xx reales**. La conversión a 4xx depende de la capa de excepciones/HTTP y requiere verificación independiente cuando aplique. Ninguna de estas pruebas sustituye aceptación MySQL/JWT live de DEV.

## Readback de CI dirigido y concurrencia de checks
- Primer SHA `e89d57189c261a48378599d3a5a7ca343867978c`: 5/6 PASS y 1 FAIL debido a la aserción equivocada sobre unicidad global intermedia de `RolConfiguration`.
- Segundo SHA `9f21a469b00affcf6d7ba038d22e3fe14c85d4de`: workflow dirigido `SOLQARYN - Directed commercial scope` ejecutó **6/6 PASS**, `DIRECTED_SHA` exacto y `DIRECTED_SCOPE_PASS total=6 passed=6` en [job 126453449865](https://github.com/solqaryn/Solqaryn/actions/runs/41793791918/job/126453449865).
- Editar el cuerpo del PR en ese mismo SHA lanzó un segundo evento GitHub `pull_request.edited`; existe además un check dirigido anterior `cancelled`. El Scope Lock correspondiente había finalizado en `failure` durante checks activos; **no es un 3/3 certificado**. El verificador confiable agrupa por nombre y necesita evidencias consistentes del mismo HEAD.
- Se incorpora esta evidencia mediante commit **solo documental** en la misma rama (sin reescribir historia ni tocar gobierno), creando un HEAD nuevo y una única ejecución `pull_request.synchronize` para evitar reutilizar el check cancelado del SHA anterior. No volver a editar metadata PR después del nuevo push. Confirmar de cero CI dirigido, seguridad, backend y Scope Lock para ese nuevo HEAD.
- Los tests dirigidos no sustituyen validación MySQL física ni negativa HTTP/JWT A/B real; ninguna aprobación independiente se atribuye a la propia cuenta de GitHub.

## Impacto, seguridad y cierre
- Diff limitado a xUnit, esta acta y `CHANGELOG_AI.md`. Backend runtime, frontend, API, JWT, RBAC, migraciones, Aiven, Render, Vercel, secretos, QA/main/PROD sin cambios.
- Riesgo de regresión reducido por reutilizar invariantes actuales; no ampliar permisos ni inferir owner. No tocar plan calculado `COLA`.
- **Estado: CANDIDATO/EN_PROGRESO.** CI exact-HEAD y Scope Lock, test dirigido, revisión de seguridad y readback de PR requeridos; no `LISTO` ni merge hasta todos los gates. Certificación de P1-009.B permanece separada.
- Rollback: nuevo PR protegido que revierta solo estos tests/acta/changelog; no hay datos ni banderas activadas.
