# P0-004.A — Inventario parcial MySQL y migraciones (solo código DEV)

**Estado técnico:** EN_PROGRESO — evidencia estática; **NO** es certificación de esquema MySQL vivo, historial aplicado, índices físicos ni respaldo restaurable.

- Microtarea: `P0-004.A` del documento comercial vigente, fila 8 de `PLAN_MAESTRO`.
- Dependencia: `P0-012.B=LISTO`, comprobada en `COLA`.
- Repo/rama base: `solqaryn/Solqaryn`, `dev` @ `a0db2cd9ccced57bf5268bf583712bf93778672b` (25-10-2026).
- Rama aislada de trabajo: `work/solqaryn-p0-004-a`.
- Task: `6adcc98a80a48191b71a5156624c21df` / ALEX_CHATGPT / SECONDARY_REVIEW_03.
- Operación exclusivamente de lectura de repositorio para inventario, sin conectarse a bases de datos, alterar infraestructura ni manipular valores de secretos.

## Evidencia estática reproducible del árbol

| Ubicación en el SHA base | Archivos encontrados | Interpretación precisa |
| --- | ---: | --- |
| `backend/src/Infrastructure/Migrations/` | 165 entradas; 164 `.cs` | Serie histórica de migraciones con pares `.Designer.cs`, snapshots y documento; **no** equivale a cantidad aplicada. |
| `backend/src/Infrastructure/Persistence/Migrations/` | 20 archivos `.cs` | Migraciones de persistencia adicionales, algunas con diseños; verificar inclusión/ensamblaje antes de calcular conjunto efectivo. |
| `backend/src/Infrastructure.Migrations/Migrations/` | 17 archivos `.cs` | Ensamblaje separado, incluye `OracleBaseline`, migraciones comerciales y snapshot; el nombre no acredita conexión con Oracle Database. |
| `backend/src/Infrastructure/Persistence/Configurations/` | 109 archivos `.cs` | Configuraciones de entidades EF; cada archivo **no** demuestra la presencia física de tabla o índice en DEV. |
| `backend/scripts/` | Scripts de `preflight`, `postcheck` y `postdeploy` de diferentes módulos | Evidencia para pruebas controladas; **no fueron ejecutados** ni se aplicaron migraciones. |

### Framework y ruta de compilación

- `backend/src/API/Solqaryn.API.csproj`: `net10.0`, referencia al proyecto `Infrastructure.Migrations`.
- `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj`: EF Core 10.0.12 y `MySql.EntityFrameworkCore` 10.0.9; excluye de compilación sus directorios `Migrations/**` y `Persistence/Migrations/**` en ese ensamblaje. Verificar las rutas compiladas realmente antes de deducir qué migraciones se despliegan.
- `backend/src/Infrastructure.Migrations/Solqaryn.Infrastructure.Migrations.csproj`: ensamblaje dedicado `net10.0` con proveedor MySQL y referencia a Infrastructure.
- `backend/src/Infrastructure/Persistence/AppDbContextFactory.cs`: ruta de creación *design time* de `AppDbContext`; su valor por defecto existe para construir un modelo sin conexión. **No** usar un fallback de diseño como evidencia del servidor real o sus credenciales.
- `backend/src/Infrastructure/Persistence/CanonicalMySqlPhysicalContract.cs`: `VerifyAsync` consulta `INFORMATION_SCHEMA.TRIGGERS`, `INFORMATION_SCHEMA.TABLE_CONSTRAINTS` y datos canónicos mediante EF. Este inventario **no lo ejecuta**, ni presupone que sus invariantes físicos existan.

## Evidencia que aún falta para completar P0-004.A

1. Lectura autenticada **solo lectura** del MySQL DEV real: proveedor/versión, `DATABASE()`, base/servidor de referencia sin revelar credenciales ni datos.
2. Comparar `INFORMATION_SCHEMA.TABLES`, `COLUMNS`, `STATISTICS`, `TABLE_CONSTRAINTS` y `TRIGGERS` con el modelo/código, registrando conteos, nombres y hashes sanitizados, no PII.
3. Leer el historial EF físico `__EFMigrationsHistory` o el historial configurado, comparándolo con los ensamblajes válidos. No suponer equivalencia entre archivos `.cs` y migraciones aplicadas.
4. Confirmar **solo metadatos** de backups DEV y retención, sin restaurar ni administrar infraestructura en esta microtarea. La restauración aislada corresponde a `P0-005`.
5. Registrar prueba negativa: una discrepancia de índice, migración ausente o versión real distinta impide la afirmación de paridad. No ejecutar `database update`, `Migrate()`, scripts mutadores ni `CREATE/ALTER/DROP` para inventariar.

## Seguridad, impacto y punto de continuación

Solo se agrega documentación de un inventario estático; no cambia API, DTO, modelos, migraciones, permisos, datos, pipelines, jobs ni despliegues. Riesgos pendientes: drift físico de esquema e índices, diferencias entre ensamblajes de migraciones, aislamiento tenant en constraints y backups no comprobados. No hay certificación de entorno, RBAC, QA/main/PROD ni autorización de infraestructura.

**NO_GO_LISTO:** falta lectura física autorizada del MySQL DEV y conciliación; conservar `EN_PROGRESO`, no liberar `P0-004.B`. Continuar desde este SHA de rama y registrar evidencia nueva; toda edición/merge futuro requiere CI exacto y revisión independiente. Rollback documental: revert protegido del PR, nunca reescritura de historial.
