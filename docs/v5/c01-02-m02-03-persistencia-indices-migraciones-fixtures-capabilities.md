# C01-02.M02.03 — Contrato de persistencia, índices, migraciones y fixtures de capabilities

**Tipo:** diseño V5 FINAL de persistencia; NO hay DDL ejecutado ni entidades o endpoints nuevos. **Responsable:** SOLQARYN PRIMARY DEV 06 — JAVIER (`6af16bbad26881918c283abe06e1f32a`), sólo `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Autoridad:** [Standard V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit).

## 1. Baseline verificado en código, no en MySQL desplegado

| Contrato del árbol Git dev | Blob exacto | Efecto comprobable de la configuración actual |
|---|---|---|
| `backend/src/Infrastructure/Persistence/Configurations/UsuarioEmpresaConfiguration.cs` | `adb6f7db929ecafcfb101031b4da23595477388b` | Índice único `UX_UsuarioEmpresas_UsuarioId_EmpresaId`; `RolId` puede cambiar; FKs restrictivas a Usuario, Empresa y Rol; índices `IX_UsuarioEmpresas_EmpresaId_Activa` y `IX_UsuarioEmpresas_RolId`. |
| `backend/src/Infrastructure/Persistence/Configurations/RolConfiguration.cs` | `b130f832365794d3f54eba16b38716badcc9914b` | Roles con `Ambito`, `EmpresaId`, checks de ámbito/empresa, índice `IX_Roles_EmpresaId`; nombre normalizado único. |
| `backend/src/Infrastructure/Persistence/Configurations/PermisoConfiguration.cs` | `1dd3911ce8590762aeea2b38bb6f3b44e12c48c9` | `Codigo` único y par `(Modulo,Accion)` único; `Ambito` y `Activo`. |
| `backend/src/Infrastructure/Persistence/Configurations/RolPermisoConfiguration.cs` | `4c66d3969e594b3ea87a42ba899ac7f41916bbb0` | `RolId,PermisoId` único; borrado en cascada desde Rol y FK restrictiva a Permiso. |
| `backend/src/Infrastructure/Persistence/Configurations/EmpresaConfiguration.cs` | `7000321f5e4914e92c7a08fb526c83c622e4c82f` | Empresa activa y `IX_Empresas_Activa_Nombre`; `UX_Empresas_RTN`; la unicidad no prueba que cada fila real esté reconciliada. |
| `backend/src/Infrastructure/Persistence/AppDbContext.cs` | `c4b32f6637f1b4426655bb0cd12af8dd9d229866` | DbSets de membresía y grants; lecturas verificadas de tenant en borde HTTP. No demuestra por sí solo enforcement de todas las escrituras o trabajos fuera de HTTP. |
| `docs/v5/c01-02-m02-01-modelo-capabilities-ownership.md` | `85344880fe45907bd9777dfdeb29dce1f07db3fb` | Entidades **conceptuales** DefinicionCapacidad, ReglaCapacidadPlan, AsignacionPlanEmpresa y RestriccionCapacidadRecurso; no existen por esta documentación como tablas reales. |
| `docs/v5/c01-02-m02-02-contratos-api-dto-capabilities.md` | `126802a61e74816785242c0fc815e36a70398139` | DTO y rutas propuestos, **sin** endpoint creado. Deben conservarse permisos/matriz existentes y el control de autorización backend. |

La observación es de código versionado: **no** se inspeccionó estado de migraciones aplicadas, índices físicos, datos de Aiven ni MySQL del proveedor. Cualquier discrepancia entre esquema físico y EF necesita un diagnóstico posterior autorizado; jamás un `ALTER TABLE` impulsado por M02.03.

## 2. Propuesta relacional condicional (NO IMPLEMENTADA)

| Relación futura | Clave de negocio / índice candidato | Propiedad, integridad y límite |
|---|---|---|
| `DefinicionesCapacidad` | `UNIQUE(CodigoNormalizado, VersionCatalogo)` | Código con allowlist, versión publicada y estado; no concede grant. Normalizar case/espacios antes de crear unicidad, rechazar colisiones. |
| `ReglasCapacidadPlan` | `UNIQUE(PlanVersionId, CodigoCapacidad, RubroId, VigenciaDesdeUTC)`; índice `(PlanVersionId,VigenciaHastaUTC)` a evaluar | Plan versionado y vigencias no solapadas **por regla del dominio**; un índice simple no impide por sí solo intervalos solapados. |
| `AsignacionesPlanEmpresa` | `INDEX(EmpresaId, VigenciaDesdeUTC, VigenciaHastaUTC)`; FK de empresa `RESTRICT` | Único plan efectivo por política futura aprobada y transacción; no elegir el primero de dos planes concurrentes. |
| `RestriccionesCapacidadRecurso` | `INDEX(EmpresaId, CodigoCapacidad, TipoRecurso, RecursoId, SucursalId)` condicionado al patrón de consulta | Validar `EmpresaId` y pertenencia del recurso en servidor; `SucursalId` nulo **no** significa permiso global. Revisar semántica de `UNIQUE` con NULL en MySQL antes de usarla. |
| `VersionesPoliticaCapacidad` (opcional) | `UNIQUE(EmpresaId, Version)`, control de versión monotónica | Serializar activación/revocación; registrar hashes no sensibles de política y auditoría. Nunca persistir decisión ALLOW sin revalidación de membership/RBAC. |

**Sin DDL preparado para ejecución:** los nombres son opciones de diseño y no son migraciones EF presentes. No crear tabla de roles paralela ni duplicar `RolPermisos`; la decisión permitida debe ser intersección de membresía vigente + grant RBAC actual + restricción rubro/plan + recurso propio + sucursal propia. El frontend no tiene autoridad de otorgamiento; el `EmpresaId` enviado por cliente tampoco.

### Invariantes antes de cualquier migración futura

1. `UsuarioEmpresas(UsuarioId,EmpresaId)` sigue única; nunca agregar `RolId` a la identidad de membresía.
2. `Rol.EsAdministrador` sin grants no es bypass. `RolPermiso` existente conserva unicidad y ámbito empresarial; un plan comercial no crea un permiso.
3. Toda tabla de capacidad con dueño debe rechazar recurso o plan de otra empresa, empresa inexistente, sucursal cruzada, rol revocado y propietario no comprobado.
4. Definiciones globales de catálogo no incluyen PII; asignaciones, límites y decisiones privadas se filtran por empresa/sucursal. Ninguna consulta carga el catálogo de políticas ajenas para escoger un fallback.
5. Evitar solapes de vigencia con transacción, índice adecuado y token de concurrencia de negocio definido en M02.04; `UNIQUE(..., NULL)` no resuelve automáticamente duplicados lógicos.
6. Rechazar rollback de versiones que deje capacidades huérfanas o elimine facturación histórica; `ON DELETE CASCADE` indiscriminado sobre relaciones comerciales es NO_GO.
7. Revocación o caída de MySQL/servicio de política = DENY para acciones restringidas; prohibido un cache ALLOW indefinido o estado de UI como último criterio.
8. Auditabilidad sin JWT, PII, hash secreto, cadena de conexión o valores del otro tenant en logs/fixtures.

## 3. Migración futura: procedimiento condicionado, no ejecutado

**Preflight:** comparar modelo EF + migraciones versionadas con `information_schema` de un ambiente autorizado y sin PII; determinar cardinalidades, duplicados, NULL, colisiones de código y solapes de vigencia; capturar baseline y plan de reversión. Los índices existentes son **declaraciones EF inspeccionadas**, no índices físicos certificados.

**Simulación:** en MySQL 8.4 efímero mediante GitHub Actions, aplicar migración aditiva con EF 10/Oracle `MySql.EntityFrameworkCore` compatible, en esquema aislado; sembrar solo fixtures sintéticos A/B. Comprobar DDL generado, límites de unicidad con NULL, FK, on-delete, índices EXPLAIN medidos, rollback bajo falla parcial, concurrencia de actualización de plan y timeout/cancelación. No asumir que `dotnet ef` o CI pasó aquí.

**Promoción futura:** nueva microtarea M03 con permiso, diff de código, tests .NET/HTTP/DB, aislamiento A/B y check-runs `completed/success` **del SHA exacto**, con revisión de impacto sobre billing/ERP. No correr DDL ni tocar Aiven/Hostinger/DNS/QA/main/PROD desde M02.03.

**Rollback:** para este documento, `git revert` normal en dev (sin reescribir Git ni BITACORA). Para una migración futura: estrategia expand/contract, backup y restauración probada en entorno efímero; no prometer reversibilidad automática de DROP/backfill.

## 4. Fixtures sintéticos y oráculos futuros (NO EJECUTADOS)

| ID | Signo | Entrada de prueba futura | Resultado exigido |
|---|---|---|---|
| P01 | + | Usuario miembro activo de A, grant A, plan/rubro vigente, recurso A | ALLOW sólo operación/grant propios |
| P02 | + | Usuario con memberships A y B y roles distintos | Evaluación bajo A no lee ni muta B; cambio explícito revalida rol |
| P03 | + | Cambio de versión de plan A conservando RolPermiso | Sólo cambia restricción comercial; grants idénticos |
| P04 | + | Reintento idempotente de asignación con versión esperada | Una sola asignación lógica o conflicto 409, sin doble cobro |
| N01 | - | Recurso de B bajo sesión A, incluso ID adivinado | DENY y cero filtración o mutación B |
| N02 | - | Plan habilitado pero sin grant de acción | DENY |
| N03 | - | Grant existe pero plan vencido, sin rubro o empresa suspendida | DENY |
| N04 | - | `EsAdministrador` / rol legacy sin grant empresarial | DENY |
| N05 | - | Dos asignaciones de vigencia solapada para A | Rechazo transaccional, sin elegir primera fila |
| N06 | - | Fila de capacidad con `EmpresaId` o `SucursalId` ajeno/null ambiguo | DENY; prohibido ámbito global inferido |
| N07 | - | Código de capacidad duplicado por case/espacios o versión inválida | 400/409, no grants implícitos |
| N08 | - | Política desactualizada por revocación o cache ALLOW persistente | DENY/revalidación, nunca autorización stale |
| N09 | - | Dependencia EF/MySQL caída o timeout | Fail-closed; cero mutación parcial |
| N10 | - | Error/log exporta secreto, token, PII o plan B | Redactar, no enumerar ni persistir |
| N11 | - | Migración intenta `DROP` columnas actuales o `CASCADE` comercial | NO_GO hasta aprobación/pruebas/rollback independientes |
| N12 | - | Se intenta habilitar workers, websockets, endpoints, DDL o QA mediante este documento | NO_GO: M02 es contrato, no implementación |

Los 16 escenarios precedentes sólo son una **matriz de diseño verificable por lectura**. No representan xUnit, SQL, HTTP, Docker, Playwright, seguridad runtime ni CI de integración ejecutados durante esta unidad.

## 5. Impacto, evaluación A0–A5 y reversión

**Rutas de esta entrega:** sólo este Markdown y `CHANGELOG_AI.md`. **Capas de runtime:** Domain/Application/Infrastructure/API, frontend, datos y migraciones sin cambios. **Consumidores:** M02.04 tipado/vigencias, M02.05 correlación, M02.06 salida, M03 código, pruebas de seguridad y CI. **Interfaces afectadas:** ninguna clase o ruta ejecutable. **Riesgos:** tenant A/B, BOLA/IDOR, flags y grants, versionado, secretos y transacciones de ventas/suscripciones.

**Gate individual:** C01-02.M01.01–06 LISTO + A5 previa; PLAN H190=PENDIENTE/I vacío, COLA G190=SI/O190=SI y DAG F204=VALIDADO/G204=SI al preflight. Se reclamó exclusivamente con transición condicional H190 y readback del owner; no se autorizan M03, QA/main/PROD ni activos externos por este hecho.

- **A0** revisar 8 fuentes/SHAs, diff sólo documental, semántica, secretos, ausencia de headings vacíos y procedencia del CHANGELOG.
- **A1** contrastar 4 positivos/12 negativos como criterios escritos; **NO** atribuirlos como pruebas ejecutadas.
- **A2** inspección documental de propiedad, JWT/RBAC, tenant A/B, revocación, versiones, negative paths y borrado seguro; no afirmar aislamiento real nuevo.
- **A3** consistencia de `UsuarioEmpresa`, `RolPermiso`, `PermisoService`, M02.01/M02.02 y arquitectura actual; integración .NET/MySQL no aplica al diff solo Markdown.
- **A4** commit fast-forward esperado sobre HEAD revalidado, Scope Lock y check V5 `completed/success` del SHA publicado y readback institucional H/I, COLA y BITACORA.
- **A5** revisor de otra de las cinco Tasks Javier, distinto del autor; `A5_PENDIENTE` no es aprobación automática.

Si un check documental obligatorio no termina SUCCESS, mantener este ID en VALIDANDO. Backend/Angular/MySQL/Playwright para este changeset documental = NO_APLICA **por causa**, nunca PASS inventado.
