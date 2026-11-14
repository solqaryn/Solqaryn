# SOLQARYN V5 FINAL — C01-01.M02.03: diseño de persistencia, índices, migraciones y datos de prueba de identidad universal

**Microtarea:** `C01-01.M02.03`. **Autor:** PRIMARY DEV 04 — JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Clase:** contrato documental de persistencia, **SIN** migración ni SQL ejecutados. **Alcance:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

**Resultado exigido:** definir persistencia, índices, migraciones y datos de prueba para la identidad universal de empresa, usuario, membresía, rol, sucursal, ownership y contexto. Se distingue lo que **ya está en código fuente** de lo que debe verificarse en MySQL 8.4 efímero antes de cualquier cambio ejecutable. Esta unidad no crea tabla, columna, constraint, migración, archivo SQL, fixture persistido o conexión a Aiven; no garantiza que una migración esté aplicada en un proveedor real.

**Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` y hoja `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU` (PLAN_MAESTRO, COLA, DAG_TECNICO, BITACORA). Contratos anteriores M02.01 `cbb112f24e162ae914b6a56221c1cbc58bb3f8c0` (blob `6545f81125d0115b9bfdd0e24daee6d0edbe490a`) y M02.02 `a076e8901d10d04dcc8e47997d4bbe28a56e67d3` (blob `54f7570b606829b41bfb9363157f31a3fc0ba8c0`) son documentales y conservaron A5_PENDIENTE. Un A5 pendiente no concede QA/main/PROD ni sustituye pruebas DB.

## 1. Preflight y limitación de la autoría

Para evaluar M02.03 se comprobaron `C01-01.M01.01–06`, `C01-01.M02.01–02`, `C00-01.M06.06`, `C00-03.M01.06` y `C00-03.M06.06` como LISTO: once predecesores en DAG individual. `DAG_TECNICO` F140=VALIDADO/G140=SI, `PLAN_MAESTRO` H154=PENDIENTE, I154 sin titular y `COLA` G154/O154=SI antes de reclamar. El claim compare-and-swap reserva SOLO este ID; no afecta M02.04–06 ni rutas sensibles. Las pruebas de integración necesarias para futuros cambios SQL/EF siguen siendo obligatorias sobre su SHA y MySQL efímero real; un documento no cuenta como esa prueba.

## 2. Inventario EF/MySQL efectivo en fuentes dev

| Archivo GitHub y SHA blob | Contrato observable en fuente | No queda acreditado únicamente por texto |
| --- | --- | --- |
| `backend/src/Infrastructure/Persistence/Configurations/UsuarioEmpresaConfiguration.cs` — `adb6f7db929ecafcfb101031b4da23595477388b` | Tabla `UsuarioEmpresas`; campos `UsuarioId`, `EmpresaId`, `RolId`, `Activa` requeridos; índice único `UX_UsuarioEmpresas_UsuarioId_EmpresaId`; índices `IX_UsuarioEmpresas_EmpresaId_Activa` y `IX_UsuarioEmpresas_RolId`; FK restrictivas a Usuario, Empresa y Rol | Que la tabla exista en Aiven o que usuarios legacy tengan membresía; FK de Rol no comprueba sola que el rol pertenezca a la empresa |
| `backend/src/Infrastructure/Migrations/20260911164800_N64CUsuarioEmpresaPersistence.cs` — `5140494e99621ad5805c98a2913fa70c8786b894` | Migración `N64CUsuarioEmpresaPersistence` crea tabla, FKs restrictivas e índices; no inventa membresías legacy durante backfill; `Down` elimina tabla | Que `Up` fue aplicado a algún proveedor. `Down` destruye datos si ya existen: NO usar como rollback automático de producción |
| `backend/src/Infrastructure/Persistence/Configurations/SucursalConfiguration.cs` — `2fbf1b75954bc308be078e8b119fb50d53d8c103` | `Sucursal.EmpresaId` FK nullable legacy, DELETE Restrict, `HasQueryFilter(!Eliminado)`; columna calculada almacenada `CodigoActivoUnico` y índice único `UX_Sucursales_Codigo_Activo`; índices de empresa y estado | Que una sucursal sin owner sea pública; la nulabilidad de compatibilidad NO autoriza leerla/escribirla sin tenant |
| `backend/src/Infrastructure/Persistence/AppDbContext.cs` — `c4b32f6637f1b4426655bb0cd12af8dd9d229866` | Contexto EF con `DbSet<UsuarioEmpresa>` y marcas de empresa autorizada/consulta; comportamiento dependiente del proveedor | Que todas las consultas actuales pasen el filtro correcto ni que el esquema real esté sincronizado |
| `backend/tests/Solqaryn.Tests/UsuarioTenantRoleOwnershipMySqlIntegrationTests.cs` — `2cb1693ababec245df01957093115f07cbaf8a64` | Prueba de integración declara necesidad de MySQL efímero, rechaza URL remota/Aiven y verifica que rol de B no autoriza contexto A | El mero archivo de test no es evidencia de ejecución ni cubre todos los recursos/historial de datos |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` — `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Rol empresarial es por par (UsuarioId,EmpresaId); inactivo/mismatch se rechaza | Invariantes de entidad por sí solos no fuerzan FK/constraint física |
| `backend/src/Domain/Entities/Sucursal.cs` — `8746b11fa30cdac0b97241b6221100bedd813b35` | `ObtenerEmpresaIdTenant()` exige positivo; reasignar owner es operación distinta de asignar | Autorización de la reasignación y transacción se comprueban después en aplicación |
| `backend/src/Domain/Entities/Empresa.cs` — `c44940df7c30983a4c23b93338e0cb9367962069` | Raíz empresarial con estado activa/inactiva | No prueba aislamiento end-to-end ni eliminación segura de datos |

**No se modificó ninguno de estos archivos.** El inventario es fuente estática con hashes causalmente verificados. La base real en Aiven, servicios externos y datos privados están fuera del alcance y no se consultaron.

## 3. Modelo físico e índices existentes vs decisiones futuras

| Objeto | Regla actual de fuente | Comprobación futura exigida en MySQL 8.4 |
| --- | --- | --- |
| `UsuarioEmpresas` | PK `Id`; `UsuarioId`, `EmpresaId`, `RolId` requeridos; `Activa=true` por defecto | Crear esquema limpio efímero vía migraciones reales, inspeccionar columns/FKs y rechazar nulidad |
| Unicidad de membresía | `UX_UsuarioEmpresas_UsuarioId_EmpresaId` único. RolId es mutable, no integra unicidad | Duplicar par U/A debe fallar; U puede pertenecer A y B con roles distintos; cambiar rol no duplica fila |
| Índices de consulta | `IX_UsuarioEmpresas_EmpresaId_Activa` y `IX_UsuarioEmpresas_RolId` | Test plan de consulta y performance sólo con distribución representativa sintética; no inferir latencia de producción |
| Integridad referencial | FKs `UsuarioId`, `EmpresaId`, `RolId` con `Restrict` | FK huérfana rechazada, no borrar Empresa/Usuario/Rol con membresías sin transición aprobada |
| Sucursales | `EmpresaId` nullable solo para legacy, FK restrictiva | Nuevos writes requieren owner positivo y válido; legacy nulo aislado/quarantined, nunca atribuido a A/B por inferencia |
| Código de sucursal | `CodigoActivoUnico` normaliza código dentro de empresa o namespace `LEGACY`; elimina unicidad para soft-deleted | Dos empresas pueden compartir código normalizado; duplicado activo misma empresa falla; legacy no se mezcla accidentalmente |
| Filtro de estado | `HasQueryFilter(!Eliminado)` para Sucursal y estado Activa en otras capas | Verificar consultas con `IgnoreQueryFilters` no permiten exposición de filas borradas/tenant ajeno |
| Transacción / rollback | `N64C...Down` contiene `DropTable` | No ejecutar Down con datos reales como rollback inocuo. Diseñar compensación/backups verificados y testear reversibilidad en DB desechable |

## 4. Esquema sintético de fixtures propuesto (NO datos insertados)

Los identificadores son **roles de prueba abstractos**, no IDs de empresas reales, secretos ni registros existentes:

| Fixture simbólico | Composición | Resultado esperado |
| --- | --- | --- |
| FX-A1 | Empresa A activa, U1 activo, Rol RA perteneciente A, membresía U1–A activa | Consulta de U1 a recursos A permitida SOLO con grant de acción, empresa y owner verificados |
| FX-B1 | Empresa B activa, Rol RB de B, membresía U1–B distinta activa | U1 puede seleccionar B con rol RB, nunca reutilizar RA en B |
| FX-A2 | Empresa A, U2 sin membresía A | Debe rechazar contexto y operaciones A |
| FX-X1 | Membresía U1–A marcada inactiva | Denegar pese a sesión JWT válida y rol conocido |
| FX-X2 | Membresía U1–A asociada artificialmente a Rol RB de empresa B | FK podría aceptar RB existente, PERO scope de aplicación debe denegar por owner: test indispensable |
| FX-S1 | Sucursal A con EmpresaId positivo y código normalizado, otra B con igual código | Unicidad por empresa, nunca exposición B a actor A |
| FX-S2 | Sucursal LEGACY con EmpresaId=null | Quarantined/fail-closed; no backfill ni permiso de escritura automático |
| FX-S3 | Sucursal A activa duplicada por código normalizado en A | Índice único debe rechazar duplicado; datos B independientes |

**Prohibido:** usar credenciales reales, PII, hashes/tokens, backups productivos, tenants ajenos o fixtures persistidos en proveedores reales. Crear datos efímeros únicamente en el job MySQL 8.4 autorizado de la futura microtarea ejecutable y eliminarlos al finalizar.

## 5. Casos verificables de aceptación para una futura implementación

| Caso | Positivo requerido | Negativo / defensa obligatoria |
| --- | --- | --- |
| DB01 | `UsuarioEmpresas` migra en MySQL 8.4 efímero, FK/índices se verifican | Migración fallida, tabla ausente o constraint divergente → FAIL |
| DB02 | U1–A y U1–B distintos, rol efectivo por tenant | Duplicado U1–A o rol RB aplicado a A → DENY/constraint |
| DB03 | UsuarioEmpresa inactiva no resuelve contexto válido | U1 con token aún vigente + membresía revocada → DENY |
| DB04 | FK restrictivas impiden miembros huérfanos | Empresa/Usuario/Rol inexistente o eliminado por cascada silenciosa → FAIL |
| DB05 | Sucursal A y B mismo código lógico permanecen aisladas | Dos sucursales activas de A con código normalizado igual → constraint fail |
| DB06 | Sucursal LEGACY nula solo se observa en inventario controlado de mantenimiento | Resolver tenant/operar recursos sin owner → DENY |
| DB07 | Cambio de rol de U1–A es idempotente y no modifica U1–B | Carrera de actualización o reuso de rol tras revocación → FAIL |
| DB08 | Migración de esquema nuevo tiene estrategia de rollback compatible con datos | Ejecutar `DropTable` contra miembros activos sin backup → PROHIBIDO |
| DB09 | Auditoría no registra PII/tokens, hash o IDs privados de B | Log de error SQL o token de actor ajeno → FAIL |
| DB10 | Build/tests de aplicación + MySQL efímero y Scope Lock exact-SHA pasan para modificación futura | Fixture mock/no MySQL real, check SKIPPED/CANCELLED/IN_PROGRESS → NO PASS |

Estas filas son **criterios de aceptación pendientes de ejecución futura**, no certificaciones MySQL/EF realizadas en M02.03. La fuente de test MySQL citada muestra intención y controles de entorno, no una corrida nueva.

## 6. Contrato de migración, compatibilidad y reversión

- **Preparar:** comparar `__EFMigrationsHistory` y esquema real solo en MySQL efímero; revisar riesgo de datos legacy `Sucursal.EmpresaId=null`, membership sin fila y FK de rol de otra empresa, sin inferir valores.
- **Aplicar (futuro):** cualquier cambio de mappings/índices/constraints/SQL requiere nueva microtarea autorizada con CI del SHA publicado: `dotnet restore/build/test`, integración MySQL 8.4/Docker GitHub-hosted y pruebas negativas de tenancy y rollback; no usar Aiven/PROD.
- **Concurrencia:** una revisión de membresía y scope debe ser transaccional o revisada de nuevo por operación; evitar autoridad cacheada, escrituras simultáneas sin CAS y backfill automático U→Empresa.
- **Compatibilidad:** mantener temporalmente nulabilidad legacy solo donde exista, pero negar accesos a recursos sin owner; no añadir roles globales ni grants de soporte/empresa por migración.
- **Rollback:** revertir este Markdown/CHANGELOG con `git revert` normal. Para futuras migraciones con datos, distinguir reversión de código de reversión de esquema: `Down` destructivo no es plan aceptable sin respaldo/ensayo causal. No force-push, reset, scripts de producción ni borrado de BITACORA.

## 7. A0–A5 proporcionadas y condiciones de liberación

**Archivos afectados por M02.03:** solamente `docs/v5/c01-01-m02-03-persistencia-indices-migraciones-fixtures.md` y una sección agrupada de `CHANGELOG_AI.md`. Sin modificaciones ejecutables, no se requiere ejecutar .NET/Angular/MySQL/Docker/E2E en esta unidad documental: `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`, `requiere_security=SI_DOCUMENTAL`, `requiere_scope_lock=SI`.

- **A0:** verificar diff solo Markdown+CHANGELOG, ocho blobs y hashes, secretos/PII, duplicación y procedencia de encabezados.
- **A1:** verificar documentalmente DB01–DB10 con positivos/negativos, límites de la migración y su no ejecución.
- **A2:** revisar tenant A/B, rol de B en A, revocación, sucursal LEGACY, tokens/PII y prevención de escritura a proveedor real.
- **A3:** 11 predecesores PLAN LISTO, DAG F140 VALIDADO/G140 SI, contratos M02.01/M02.02, constraints/migración y compatibilidad; `NO_APLICA` ejecución DB mientras solo se diseñe.
- **A4:** commit FF en `dev`, SHA exacto, blobs y diff, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success`, readback PLAN/COLA/BITACORA.
- **A5:** otra Task Javier distinta verifica SHA, fuentes positivas/negativas y rollback. `A5_PENDIENTE` no bloquea LISTO_DEV A0–A4, pero sí bloquea toda promoción sin revisión y autorización adicional.

**No habilita:** M02.04–06, M03 ni despliegues, cambios IAM/JWT/RBAC, migraciones DB, QA/main/PROD. Cada ID requiere autorización y pruebas propias.
