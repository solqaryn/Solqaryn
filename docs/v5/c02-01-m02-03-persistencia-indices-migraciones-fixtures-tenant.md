# SOLQARYN V5 FINAL — C02-01.M02.03: persistencia, índices, migración y fixtures del contexto tenant

**Entrega documental, sin DDL ni código ejecutable.** Autor PRIMARY DEV 05 — JAVIER_CHATGPT, Task `6adc2e55f47c81919f6374e561687076`. Repositorio `solqaryn/Solqaryn@dev` y Scope Lock STRICT. Autoridad: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`); fuente de estados PLAN_MAESTRO/COLA, DAG_TECNICO y BITACORA del Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.

## 1. Realidad de persistencia inspeccionada en dev

| Código actual | Git blob inspeccionado | Hecho contrastado |
| --- | --- | --- |
| `backend/src/Infrastructure/Persistence/Configurations/UsuarioEmpresaConfiguration.cs` | `adb6f7db929ecafcfb101031b4da23595477388b` | Tabla `UsuarioEmpresas`, FK restrictivas a Usuario, Empresa y Rol; `UX_UsuarioEmpresas_UsuarioId_EmpresaId` único; `IX_UsuarioEmpresas_EmpresaId_Activa`, `IX_UsuarioEmpresas_RolId`. RolId mutable no parte de identidad de membresía |
| `backend/src/Infrastructure/Persistence/Configurations/EmpresaConfiguration.cs` | `7000321f5e4914e92c7a08fb526c83c622e4c82f` | Tabla `Empresas`, default `Activa=true`, índices `IX_Empresas_Activa_Nombre` y `UX_Empresas_RTN`; no equivale a registro de host universal |
| `backend/src/Infrastructure/Persistence/AppDbContext.cs` | `c4b32f6637f1b4426655bb0cd12af8dd9d229866` | `DbSet<UsuarioEmpresa>`, `AuthorizedReadEmpresaId` de marcas HTTP verificadas y marca comercial separada de soporte público/tienda/factura. No demuestra por sí solo que todos los repositorios hayan aplicado filtro positivo |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | `(UsuarioId, EmpresaId, RolId, Activa)` y control de rol efectivo; ninguna autorización de negocio se deriva de un simple FK |
| `docs/v5/c02-01-m01-05-aceptacion-contexto-tenant.md` | `ab7d97eeafaf15f7e2ce5659ff1ebe6e98bb5492` | Matriz AC01–AC18 de expectativas HTTP/DB futuras; no presenta ejecuciones reales |

**No se creó ni modificó ninguna migración.** La definición EF actual se observa desde archivos, no desde la base desplegada; la existencia física de índices y constricciones requiere inspección de migraciones/CI y DB real con permisos adecuados, que este documento no realizó.

## 2. Diseño físico condicional: esquema y límites de ownership

- **Membresía existente:** conservar identidad compuesta única `UsuarioId,EmpresaId` y FK restrictivas a ambos extremos y Rol; preservar `Activa`. La consulta de autorización debe verificar simultáneamente usuario, empresa y membresía activos, rol efectivo y ámbito del recurso; un índice no es un permiso.
- **Lookup de tenant por host (futuro, NO creado):** evaluar una relación `TenantHostBinding` tenant-scoped con `EmpresaId`, nombre DNS normalizado único por entorno y propósito, estado, y fuente confiable; no permitir wildcard ni duplicados ambiguos. Host forwarding sólo de proxy permitido. No introducir tabla sin contrato de despliegue, pruebas de spoofing y revisión de DNS/infraestructuras.
- **Recurso tenant-scoped (futuro o existente según entidad):** cada repositorio sensible debe imponer `EmpresaId` positivo validado server-side + filtro por propietario del recurso; índices compuestos para consultas habituales se diseñan tras medir, sin inferir column names o crear SQL especulativo.
- **Sucursal y roles:** el `RolId` de membresía no confiere privilegios plataforma ni entre empresas; validar grants actuales, recurso y sucursal cada vez. No usar `Usuario.RolId` legacy para saltar control A/B.
- **Auditoría/versión:** conservar actor, tenant autorizado, operación y motivo/correlación opacos en tabla/sistema permitido; evitar tokens, secretos y PII. Cualquier patrón CAS, token de concurrencia, tabla de hosts o evento será evaluado en su microtarea específica.

Los índices presentes son los del archivo EF, no nuevas migraciones; no elevar `AuthorizedReadEmpresaId` como autorización para escritura sin RBAC y ownership. Un host correcto no reemplaza una membresía vigente para rutas privadas.

## 3. Plan de migración FUTURO y rollback, sin ejecuciones

1. **Inventario:** obtener esquema físico y migraciones EF aplicadas con cuenta de auditoría apropiada; contrastar FK, índices y duplicados de `UsuarioEmpresas`, sin mutar datos. Detectar filas orphan, tenants inactivos, claves sin owner y usuarios con múltiples empresas.
2. **Preflight y seguridad:** preparar plan expand/contract separado, respaldo/restauración demostrable, clasificación de PII, secuencia de índice concurrente si aplicable y locks/ventana transaccional; no asumir soporte exacto del proveedor MySQL sin CI.
3. **Cambio aditivo eventual:** solo en microtarea autorizada y validada con .NET/EF y MySQL 8.4 efímero; comparar `dotnet ef migrations script` revisado, efectos de FK/índices, backfill verificado e idempotencia. No hacer DROP de columnas o cambiar unicidad en M02.03.
4. **Pruebas y cutover:** test unitarios EF, integración descartable MySQL y HTTP/JWT A/B con membresías revocadas, host ambiguo, roles cruzados, acceso de sucursal y fallas parciales; CI exact-SHA required PASS para scripts ejecutables. Incluir aislamiento tenant en lectura y mutación, rollback ante conflicto y auditoría sin secretos.
5. **Rollback:** posibilidad de volver a ruta autorizada previa sin reescribir datos históricos; snapshots verificados, reversión aditiva y ensayo de restauración antes de QA/PROD con permiso nuevo. Si falla un gate, no hacer deploy ni ocultar divergencia.

Ningún paso arriba se ejecuta en esta entrega; son controles previos para una implementación futura. No operar Aiven productivo, redes, host, Cloudflare/Hostinger, credenciales, CI reruns innecesarios o migraciones reales aquí.

## 4. Matriz de 12 fixtures simbólicos (4 positivos y 8 negativos)

| Fixture | Escenario de prueba futura | Oráculo esperado |
| --- | --- | --- |
| F01 (+) | Usuario U miembro activo de A, empresa A vigente, rol A y recurso A | Lectura/escritura autorizada exclusivamente A, log seguro |
| F02 (+) | U miembro activo de A y B con roles diferenciados | Cambiar A→B revalida rol y no devuelve datos A en B |
| F03 (+) | Dos usuarios diferentes comparten empresa A con permisos independientes | Unicidad de vínculo usuario/empresa preservada y permisos propios |
| F04 (+) | Empresa A con múltiples sucursales autorizadas y recurso A | Acceso sólo a sucursal y recurso permitidos, sin cruzar B |
| F05 (-) | Token A y `EmpresaId=B` o header `X-Empresa-Id=B` | Rechazo, cero consultas o mutaciones privadas B |
| F06 (-) | Membresía A revocada con JWT vigente | Rechazo, no caché antigua ni escritura |
| F07 (-) | Host falso, parecido a autorizado, wildcard o mapping duplicado | Ningún tenant elegido por default |
| F08 (-) | `UsuarioEmpresa` duplicado para `(UsuarioId,EmpresaId)` | Única relación, conflicto seguro e idempotencia |
| F09 (-) | Recurso sin dueño o con `EmpresaId` de B en operación A | Denegar lectura/escritura sin filtrar B |
| F10 (-) | `Usuario.RolId` legacy indica admin pero membresía A no permite | Rechazo, sin elevación a plataforma o B |
| F11 (-) | Empresa suspendida o dependencia de autorización caída | Denegar, sin fallback a ámbito anterior |
| F12 (-) | Un log/error intenta volcar JWT, secreto, PII o identificar B | Sanitizar, no enumerar ni revelar datos |

**Todos los fixtures son datos/oráculos simbólicos redactados**, no seeds, pruebas xUnit, consultas a Aiven, transacciones MySQL ni ejecutables. Las pruebas futuras deberán verificar estos casos con runners reales y comparar respuesta HTTP + DB descartable.

## 5. Impacto, gates A0–A5 y reversibilidad

**Impacto de este changeset:** exclusivamente `docs/v5/` y `CHANGELOG_AI.md`. No nuevos archivos .NET/Angular, capas, DTO, migraciones, tablas, índices ejecutados, credenciales, RBAC, tenants, workers, Tasks, eventos o servicios. Consumidores futuros: Application tenant, filtros HTTP, Infrastructure EF, pruebas y QA; el código actual no cambia.

Dependencias de `C02-01.M02.03`: seis M01 de C02-01 LISTO y A5 independiente autorizada, `PLAN_MAESTRO!H298=PENDIENTE/I298` vacío antes del claim, `COLA!G298/O298=SI`, `DAG_TECNICO F=VALIDADO/G=SI` para el ID exacto, CAS de propietario y BITACORA A:Z. Un diseño de migración no habilita M02.06, M03/DB, operación JWT, QA/main/PROD, ni borra A5_FAIL previo.

**A0:** inspección documental de blobs/diff, Scope Lock y sección CHANGELOG sin encabezado vacío ni atribución cruzada, sin secretos. **A1:** F01–F12 presentes con 4 positivos y 8 negativos verificables como especificación; pruebas runtime no aplican. **A2:** revisar host spoofing, membresía revocada, tenant A/B, RBAC, FK y filtración de datos; no PASS de DB. **A3:** cruce de cuatro blobs de código existentes, M01.05, DAG, PLAN/COLA y 26 columnas en BITACORA; no integración ejecutable. **A4:** commit directo fast-forward con CAS HEAD en dev, dos blobs, check-runs Scope Lock y V5 provenance exact-SHA `completed/success`, estados/leases/readbacks; jobs .NET/MySQL/Angular NO_APLICA para Markdown-only. **A5:** revisor independiente entre otras cinco Tasks Javier antes de QA.

**Rollback de esta documentación:** `git revert` normal con lectura HEAD y registro auditable; no force-push y nunca modificar BITACORA histórica. Si un check pertinente no está completado SUCCESS, mantener el scope en VALIDANDO sin contarlo LISTO.
