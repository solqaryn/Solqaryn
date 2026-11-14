# SOLQARYN V5 FINAL — C01-03.M02.03: persistencia, índices, migraciones y datos de prueba de configuración dinámica

**Autor:** PRIMARY DEV 04, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Clase:** diseño de persistencia exclusivamente DOCUMENTAL M02.03. **Rama:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fuente única:** Standard de Implementación de Automatizaciones V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, y Sheet institucional PLAN/COLA/DAG. Ninguna instrucción de este documento crea, altera o ejecuta una migración.

## 1. Alcance y esquema actual comprobable

El objetivo del bloque C01-03 es la configuración **tipada, permitida y aislada por ámbito**, con valores editables separados de código. Esta microtarea especifica invariantes de modelo, índices previstos, protocolo de migración futura y fixtures **simbólicos**. No crea tablas, constraints, migraciones EF, conexiones, grants ni seeds reales. Un archivo Markdown **no prueba** que MySQL o EF estén operativos.

| Código verificado en dev | Blob SHA | Hecho observable |
| --- | --- | --- |
| `backend/src/Infrastructure/Persistence/Configurations/ConfigEmpresaConfiguration.cs` | `c26a46d4381618c960c93bb71806f802969ddcb8` | Mapea `ConfigEmpresas`, unicidad de `EmpresaId` con `UX_ConfigEmpresas_EmpresaId`, `Version` como concurrency token, CHECK de versión positiva y moneda de longitud tres; `PlantillasCorreoEmpresa` con índice único por (`EmpresaId`, `TipoPlantilla`) |
| `backend/src/Infrastructure/Persistence/Configurations/EmpresaConfiguracionConfiguration.cs` | `f238687672981621506175db2cc9d6ec6d02d323` | `EmpresaConfiguraciones` legacy global, `UX_EmpresaConfiguraciones_Activa` sobre columna computada `ActivaUnica`, **no** por tenant |
| `backend/src/Infrastructure/Migrations/20260912121400_N67CConfiguracionEmpresaPersistence.cs` | `8b28efe971561d98fa7a3e54f703d2913023fd63` | Migración existente crea `ConfigEmpresas` con `EmpresaId`, `Version`, JSON operativos, FKs/índices; no es una migración nueva de catálogo universal |
| `backend/src/Domain/Entities/ConfigEmpresa.cs` | `664547d28b513d3187622b02c57a7db1f812f7ec` | Objeto operativo tenant-scoped con `ImpuestosJson` y `EmisionJson` longtext, referencia SMTP opaca y versión |
| `docs/v5/c01-03-m02-01-entidades-ownership-invariantes.md` | `8820c3b28b56a9134c6edc6fe8139c27a21ca340` | Modelo conceptual de clave/owner/scope/revisión y 12 invariantes |
| `docs/v5/c01-03-m02-02-contratos-api-dto-errores-compatibilidad.md` | `04de8c729ed45769d39f8edaff9af5940e02588b` | API futura de clave tipada, idempotencia, errores y compatibilidad legacy |

**Hallazgo de gobierno:** `ConfigEmpresa` representa configuración operativa por empresa y `EmpresaConfiguracion` mantiene un registro legacy activo global. No son sinónimos; ningún script futuro debe copiar automáticamente el registro global al tenant B ni convertir `ActivaUnica` en tenant-first por inferencia. Primero se requiere decisión explícita de propiedad, migración reversible y pruebas de aislamiento. `IsConcurrencyToken` mapea el token EF pero **no demuestra por sí solo** que toda ruta use CAS y auditoría transaccional correctos.

## 2. Modelo físico futuro condicional (NO creado)

La decisión física está pendiente de M03.04 y de las comprobaciones IAM C01-01. Si se aprueba un catálogo por clave, considerar separación lógica del **catálogo inmutable controlado por código** y de valores con owner verificado, sin sustituir `ConfigEmpresas` existente ni editar `EmpresaConfiguraciones` legacy prematuramente.

| Objeto conceptual | Columnas mínimas potenciales | Unicidad / integridad obligatoria | Nota |
| --- | --- | --- | --- |
| `ConfigKeyCatalog` | `KeyCanonical`, `ValueKind`, `AllowedScopes`, `DefaultVersion`, `MaxLength`, `Sensitivity`, `IsEditable` | Clave canonizada única; tipo y capacidad sólo desde allowlist versionada | Se puede implementar estáticamente en código, sin tabla, si resulta más seguro |
| `ScopedConfigValue` | `Id`, `KeyCanonical`, `ScopeKind`, `OwnerId`, `ValueTyped`, `Revision`, `UpdatedAtUtc` | Unique (`KeyCanonical`, `ScopeKind`, `OwnerId`) **y FK/constraint que demuestre owner real** | Un `OwnerId` sin vínculo de empresa no garantiza tenancy; verificar contra Empresa/Sucursal/Recurso |
| `ConfigChangeAudit` | `EventId`, `ScopeKind`, `OwnerId`, `KeyCanonical`, `RevisionBefore`, `RevisionAfter`, `ActorId`, `Outcome`, `CorrelationId` | EventId único + scope/actor de servidor; append-only bajo modelo autorizado | No guardar credenciales, JWT, secretos ni valor privado en claro |
| `ConfigIdempotency` | `ActorId`, `TenantId`, `OperationCode`, `KeyHash`, `RequestFingerprint`, `ResultRevision` | Unique por actor+tenant+operación+idempotency key; TTL/limpieza controlada | Evita replay cross-tenant, pero no crea workers ni eventos entre Tasks |

**Diseño fail-closed:** un esquema de `ScopeKind` polimórfico sin FK verificable requiere una política de resolución y pruebas de propietario para cada tipo. No asumir que un índice compuesto por `OwnerId` resuelve la autorización. Los valores globales son claves separadas, inmutables salvo rol plataforma explícitamente autorizado; ninguna membresía de empresa los edita. Toda lectura filtra en servidor y no acepta `EmpresaId`, slug, cookie, header o parámetro de cliente como prueba de ownership.

## 3. Plan de migración reversible (futuro, no ejecutado)

1. **Preflight:** revisar HEAD exacto, plan+DAG, autoría y consumidores; resolver duplicados y nulos actuales, cardinalidad de pertenencia, compatibilidad de EF Core 10.0.12 y proveedor Oracle `MySql.EntityFrameworkCore` 10.0.9 con MySQL 8.4; probar primero sobre servicio efímero en CI, nunca un servidor productivo.
2. **Expand:** agregar nuevas estructuras **vacías** y constraints/índices validados en migración versionada nueva, sin eliminar `ConfigEmpresas` ni `EmpresaConfiguraciones`; defaults/flags apagados por defecto. Validar backfill opt-in, jamás copiar secretos.
3. **Migración por tenant:** para cada empresa con membership/ownership verificables, mapear únicamente claves allowlisted no sensibles; registrar conteos/hashes no sensibles, skips/rechazos y pivotes del legacy global. No habilitar valores de empresa distinta al origen.
4. **Dual read controlado:** backend autenticado usa catálogo sólo en ámbitos aprobados; caché por empresa/recurso, con invalidación tras cambio; respuestas inconsistentes, empresa inactiva o ambigüedad → DENY/default seguro, nunca fallback a B.
5. **Cutover reversible:** pruebas xUnit y API positivas/negativas, integración MySQL 8.4 efímero, drift EF, autorizaciones JWT/RBAC y transacciones reales, validación de índices y `Revision` CAS frente a concurrencia; QA/PROD requieren autorización independiente.
6. **Rollback:** desactivar lectura del nuevo modelo, mantener legado intacto y snapshots/backup verificados, revertir sólo **cambios aditivos** cuando no haya datos no reconciliados; jamás `DROP` destructivo de valores de clientes. Para esta microtarea únicamente git revert normal del documento.

**Decisión pendiente:** un rollback SQL de índices/tablas por DDL MySQL puede no ser transaccional. No prometer atomicidad total del DDL; planificar checkpoints y backups reales antes de ejecutar futuras migraciones. No se han creado scripts de backfill ni autorizaciones live.

## 4. Fixtures simbólicos y oráculos futuros (sin seeds)

| Fixture | Contexto/acción propuesta | Resultado esperado de prueba futura |
| --- | --- | --- |
| F01 (+) | Tenant A, key `ui.locale` allowlisted, versión 1 | Crear y leer sólo el valor de A |
| F02 (+) | Tenant B, misma key, valor distinto | Unique compuesto admite scope B, no filtra A |
| F03 (+) | A actualiza revisión 1→2 con actor miembro vigente | CAS, un solo commit, auditoría A |
| F04 (+) | A lee key sin override autorizado | Default seguro, nunca valor de B |
| F05 (-) | Token A con `EmpresaId=B` en body | Denegar, 0 consultas de valor privado B |
| F06 (-) | Miembro revocado de A intenta lectura/escritura | Denegar, sin mutación ni cache legacy |
| F07 (-) | Dos actores con `expectedRevision=1` concurren en A | Un ganador, conflicto seguro al otro |
| F08 (-) | Key de secreto SMTP o conexión | Rechazar sin registrar ni exportar referencia secreta |
| F09 (-) | `<script>`, SQL literal, JSON grande o scope no permitido | Validación/rechazo, sin SQL/JS arbitrario |
| F10 (-) | Error de auditoría o de DB tras validación | Rollback de cambio, sin dato parcial |
| F11 (-) | `OwnerId` de sucursal B en scope A | FK/ownership DENY aun cuando el ID exista |
| F12 (-) | DDL/backfill falla a mitad de la migración efímera | Restauración sin pérdida de datos existentes, no cutover |

Estos son **datos y criterios documentales propuestos**, no registros insertados ni pruebas en una base MySQL. Los índices propuestos no se acreditan como creados; el índice existente `UX_ConfigEmpresas_EmpresaId` sí consta en código y migración.

## 5. A0–A5, impacto, compatibilidad y cierre

- **Gate:** `C01-03.M01.01–06` LISTO con A5 por Tasks Javier autorizadas; `DAG_TECNICO!A234:J234` F=VALIDADO/G=SI; `PLAN_MAESTRO!H226/I226` y `COLA!G226/O226` verificadas antes del claim. M02.01/02 ya LISTO pero A5 propio queda independiente; no usar esos LISTO para liberar M02.06 o implementación.
- **A0:** evaluar seis fuentes/blob reales, diff limitado a Markdown, sección CHANGELOG cohesionada y sin encabezados vacíos; sin secretos ni scripts SQL.
- **A1:** fixtures F01–F12, cuatro positivos y ocho negativos como criterios escritos, nunca como resultados de pruebas ejecutadas.
- **A2:** tenant A/B, revocación, privilegio plataforma, clave secreta, SQL/JS inyectable, CAS e idempotencia fail-closed.
- **A3:** coherencia con EF `ConfigEmpresa`, migración versionada 20260912, `EmpresaConfiguracion` legacy global, proveedor MySQL y contratos C01-03.M02.01/02, con riesgos de DDL no transaccional.
- **A4:** publicar fast-forward CAS exclusivo en `dev`, comprobar exact SHA `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` con `completed/success`, readback PLAN/COLA/BITACORA. CI dotnet/MySQL/Playwright `NO_APLICA` exclusivamente si diff contiene Markdown, no por skip arbitrario.
- **A5:** revisor de otra Task Javier (TASK_ID distinto) evalúa fuentes y limitaciones del SHA; ninguna certificación propia.
- **Impacto:** no cambia Domain, API, Infrastructure, EF, MySQL, frontend, datos, JWT, RBAC, tenant, worker, evento, secreto, dependencia o entorno.
- **Rollback de esta entrega:** revert normal del commit del documento en HEAD actualizado, preservando trabajo ajeno; nunca force-push, reset, BITACORA histórica o QA/main/PROD.

**Cierre condicionado:** esquema, índices, migración y 12 fixtures quedan **diseñados**, no ejecutados. M03–M05 deben construir y probar lo necesario con CI real; ningún cambio productivo está autorizado.
