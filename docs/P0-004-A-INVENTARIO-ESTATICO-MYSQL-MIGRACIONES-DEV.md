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

## Cadena de migraciones seleccionada por el runtime (evidencia estática adicional)

**Corte de fuentes:** `dev` @ `785911f1095f13c0b6c0e859c35e0a7b733fcb3c` (26-10-2026 UTC). Este contraste amplía el inventario de código; **no existe lectura de MySQL DEV** y no valida el historial aplicado.

| Evidencia de código | Comportamiento verificable en el repositorio | Límite de la conclusión |
| --- | --- | --- |
| `backend/src/API/Program.cs` | Registra `AppDbContext` con `UseMySQL(..., mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))`. | Selección de ensamblado en el código API; no prueba configuración ni conexión del despliegue vivo. |
| `backend/src/API/OracleAppDbContextDesignTimeFactory.cs` | La fábrica API para tooling selecciona el mismo ensamblado de migraciones. | El fallback de diseño no representa la base DEV física. |
| `backend/tools/Solqaryn.DatabaseBootstrap/Program.cs` | Bootstrap también usa ese ensamblado y consulta `GetMigrations()`; contiene `EnsureCreatedAsync`, `MigrateAsync` y SQL mutador. | **NO ejecutar** como diagnóstico de solo lectura, incluso en modo `adopt`, sin revisión/autorización separada. |
| `backend/tests/Solqaryn.Tests/N33MigrationDiscoveryTests.cs` | La prueba estática de descubrimiento exige `20261006111818_OracleBaseline` y excluye `20260824120000_N3_3_C_PedidoVentaReservaInventario` de la cadena activa, preservando el tipo histórico como auditoría. | La presencia en `IMigrationsAssembly` no acredita filas en `__EFMigrationsHistory`. |
| `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` | Excluye de compilación `Migrations/**/*.cs`, `Persistence/Migrations/**/*.cs` y la fábrica de diseño de Infrastructure. | Los recuentos de esos directorios no deben sumarse como migraciones activas. |
| `backend/src/Infrastructure.Migrations/Solqaryn.Infrastructure.Migrations.csproj` | Declara `AssemblyName=Solqaryn.Infrastructure.Migrations` y `MySql.EntityFrameworkCore=10.0.9`. | El nombre `OracleBaseline` no implica que se use Oracle Database. |

**Control negativo de inventario:** la hipótesis «todos los `.cs` de los tres directorios de migraciones están activos/aplicados» queda **rechazada** por las exclusiones de compilación y la prueba de descubrimiento. La hipótesis «la cadena activa está aplicada en MySQL DEV» queda **NO VERIFICADA** hasta lectura auténtica de `INFORMATION_SCHEMA` y `__EFMigrationsHistory`. En ausencia de permisos efectivos de lectura MySQL o evidencia de respaldos, mantener `NO_GO_LISTO`.

**Pruebas en esta adición:** inspección remota de archivos concretos y checks exitosos de `dev` exact-SHA (cinco checks GitHub); **no** se ejecutaron tests locales, consulta SQL, bootstrap ni migraciones. La rama de inventario deberá obtener CI propio cuando se publique PR y revisión independiente antes de cualquier cierre.

## Evidencia que aún falta para completar P0-004.A

1. Lectura autenticada **solo lectura** del MySQL DEV real: proveedor/versión, `DATABASE()`, base/servidor de referencia sin revelar credenciales ni datos.
2. Comparar `INFORMATION_SCHEMA.TABLES`, `COLUMNS`, `STATISTICS`, `TABLE_CONSTRAINTS` y `TRIGGERS` con el modelo/código, registrando conteos, nombres y hashes sanitizados, no PII.
3. Leer el historial EF físico `__EFMigrationsHistory` o el historial configurado, comparándolo con los ensamblajes válidos. No suponer equivalencia entre archivos `.cs` y migraciones aplicadas.
4. Confirmar **solo metadatos** de backups DEV y retención, sin restaurar ni administrar infraestructura en esta microtarea. La restauración aislada corresponde a `P0-005`.
5. Registrar prueba negativa: una discrepancia de índice, migración ausente o versión real distinta impide la afirmación de paridad. No ejecutar `database update`, `Migrate()`, scripts mutadores ni `CREATE/ALTER/DROP` para inventariar.

## Seguridad, impacto y punto de continuación

Solo se agrega documentación de un inventario estático; no cambia API, DTO, modelos, migraciones, permisos, datos, pipelines, jobs ni despliegues. Riesgos pendientes: drift físico de esquema e índices, diferencias entre ensamblajes de migraciones, aislamiento tenant en constraints y backups no comprobados. No hay certificación de entorno, RBAC, QA/main/PROD ni autorización de infraestructura.

**NO_GO_LISTO:** falta lectura física autorizada del MySQL DEV y conciliación; conservar `EN_PROGRESO`, no liberar `P0-004.B`. Continuar desde este SHA de rama y registrar evidencia nueva; toda edición/merge futuro requiere CI exacto y revisión independiente. Rollback documental: revert protegido del PR, nunca reescritura de historial.

## Instrumento verificable, pendiente de ejecucion contra MySQL DEV

Se agrega `backend/scripts/p0-004-a-readonly-mysql-inventory.sql` con nueve consultas exclusivamente `SELECT`. La secuencia Q01-Q08 inspecciona version/base efectiva y metadatos de tablas, columnas, indices, constraints, claves y triggers sin leer filas comerciales. Q08 consulta si existe `__EFMigrationsHistory`; **Q09 se ejecuta manualmente solo si Q08 lo confirma**. Ante esquema inesperado, error de permisos, historial ausente o datos no coincidentes: **NO_GO**, no corregir ni crear objetos en esta microtarea.

Antes de usarlo, una identidad proveedora autorizada debe confirmar la cuenta MySQL DEV de **solo lectura**, la base seleccionada y la prohibicion de publicar hostnames, usuarios, credenciales o datos sensibles. El SQL NO establece conexion, NO lee backups del proveedor y NO equivale a inventario fisico completado. Los metadatos de backup/retencion requieren readback independiente del proveedor; restauracion aislada pertenece a P0-005.

`scripts/verify-p0-004-a-readonly-mysql-inventory.mjs --self-test` comprueba que el fichero solo tenga nueve instrucciones SELECT, que todas las fuentes sean INFORMATION_SCHEMA o historial EF y que 14 casos negativos de mutacion/exportacion/acceso a tablas comerciales sean rechazados. La comprobacion local del script se ejecuto con Node 22.16.0 y paso 9/9 consultas + 14/14 negativas. El workflow de PR `.github/workflows/p0-004-a-readonly-mysql-inventory.yml` usa Node 24, checkout del SHA exacto y permiso `contents:read`. **No se ha ejecutado SQL real ni confirmado CI GitHub de estos cambios al redactar este checkpoint.**

Este progreso **no modifica** P0-004.A=EN_PROGRESO, no proporciona evidencia fisica de MySQL ni habilita LISTO o dependientes. DRAFT PR a dev puede validar exclusivamente el codigo estatico; cierre exige evidencia fisica, revision ajena y readbacks institucionales.
