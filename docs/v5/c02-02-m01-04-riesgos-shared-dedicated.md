# SOLQARYN V5 FINAL — C02-02.M01.04: riesgos y decisiones de almacenamiento shared/dedicated

**TIPO 1: preparación documental provisional, sin cambios ejecutables.** Fecha 2026-11-13; repositorio solqaryn/Solqaryn@dev, Scope Lock STRICT. Autor: PRIMARY DEV 01 JAVIER, TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a. Fuente única: Standard V5 FINAL Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA, Sheet V5 1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU.

## Límite atómico y dependencia

El resultado global de C02-02 es **definir almacenamiento compartido, dedicado, router server-side y ciclo de migración**. Su evidencia futura mínima incluye placement, migración y rollback sin DSN elegido por el cliente. Esta microtarea M01.04 **sólo registra riesgos, hipótesis y decisiones pendientes**; no implementa router, migración, separación física o proveedor, no habilita M01.06/M02, ni certifica independencia real con MySQL.

El DAG_TECNICO permite C02-02.M01.01–05 en preparación documental mientras se satisfagan predecesores, sin liberar fases ejecutables. Antes de claim se comprobó C02-01.M01.01 y C02-01.M01.02 LISTO con owner y evidencia; seis C00-01.M02 y cinco C00-03.M01, más C01-01.M01.01–05, preceden el contrato. **Revalidar ante cambios de sus premisas** y antes de la siguiente fase.

**No alcance:** no crear bases, DSN, conexiones, credenciales, cuentas, endpoints, tenants, archivos, servidores o pipelines; no migrar MySQL, EF, Docker, datos reales, Cloudinary, R2, Hostinger, DNS ni producción. No cambiar permisos/JWT, QA/main/PROD, ramas, PR, Tasks ni horarios. Inventario cero de imágenes históricas: migración histórica NO_APLICA. No hay certificación live en esta entrega.

**Pruebas pertinentes:** requiere_dotnet=NO; requiere_node_angular=NO; requiere_mysql=NO; requiere_docker=NO; requiere_e2e=NO (ningún runtime modificado). requiere_security=SI (aislamiento, credenciales, tenancy, rollback y PII documentados); requiere_scope_lock=SI (integridad de commit documental exact-SHA). Para la implementación posterior sí se exigirán .NET/EF/MySQL 8.4, migraciones reversibles y pruebas con tenants A/B si se tocan esas superficies.

## Evidencias de código inspeccionadas (no certificación de comportamiento integral)

| Archivo del HEAD dev | Blob Git SHA | Observación limitada |
| --- | --- | --- |
| backend/src/Application/Interfaces/StorageTenantContext.cs | 716e8e205502d6300f555947c79edc2852fd63ba | Scope de almacenamiento desde contexto tenant server-side, prefijo empresas/{EmpresaId} y validación de propietario. |
| backend/src/Domain/Security/ContextoTenantActual.cs | 7377d2d752985cc9baf31d08280f38ad94a6a114 | Contexto creado desde membresía válida y guardia ExigirEmpresa, no desde claims del cliente. |
| backend/src/Infrastructure/Persistence/AppDbContext.cs | c4b32f6637f1b4426655bb0cd12af8dd9d229866 | Contextos de lectura por marcas verificadas, restricciones de soporte read-only y coexistencia de vías públicas. |
| backend/src/API/Middleware/UnscopedCommercialTenantGuardMiddleware.cs | 558dda1eb5be729b2bd9a6d9fefbccd5db94addc | Cuarentena con 2+ empresas para operaciones privadas no habilitadas y excepciones acotadas. |
| backend/src/Infrastructure.Migrations/Migrations/20261013220000_CommercialTenantOwnershipGraphExpand.cs | 9170f552351d122ef7f74628e59f7c61aacdc28d | Migración histórica de expansión aditiva nullable; no implica partición o DSN dedicado. |
| backend/src/Domain/Entities/UsuarioEmpresa.cs | f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c | Rol y membresía por empresa, sujeto a verificar estado de usuario/rol/empresa en consumidores. |

La revisión de un archivo y de sus blobs identifica contratos existentes; **no certifica que un router shared/dedicated esté implementado o que existan bases dedicadas**. Cualquier nombre de entorno futuro es diseño, no infraestructura real. Los documentos previos C02-01 deben revalidarse al promover esta preparación.

## Matriz de riesgos, negativos y controles futuros

| Riesgo | Severidad | Escenario negativo observable | Control que debe validarse en fase ejecutable |
| --- | --- | --- | --- |
| R01 | CRITICA | Cliente introduce tenantId o DSN de empresa B | El servidor deriva el ámbito de membresía autorizada y placement verificado; rechaza el cambio a B. |
| R02 | CRITICA | Router cambia de shared a dedicated durante solicitud y lee otra empresa | Resolver estable por transacción/request, sin fallback a DB ajena. |
| R03 | CRITICA | Cadena de conexión, password o token queda en logs, DTO o browser | Secretos solo server-side, rotación y redacción verificable; test negativo de filtración. |
| R04 | CRITICA | Migración de A copia filas o adjuntos de B | Export/restore por tenant con checksum, conteos y tests A/B; nunca modificar producción desde este scope. |
| R05 | ALTA | Reintento de migración duplica writes o pierde eventos | Checkpoint idempotente con fencing, rollback y auditoría; prueba de repetición. |
| R06 | ALTA | Dual-write cambia inconsistente origen y destino | Escritura autoritativa definida y corte transaccional; detectar divergencias y bloquear promoción. |
| R07 | ALTA | Capa EF ignora ownership por conexión dedicada | Validar siempre resource EmpresaId, filtros y FK, también dentro de dedicated. |
| R08 | ALTA | Caché, key de objetos o índice compartido filtra metadatos de otro tenant | Prefijos tenant-bound, cifrado/permisos y prueba de lectura cruzada. |
| R09 | ALTA | Error de base de A causa fallback silencioso a shared sin autorización | Fail-closed o recuperación explícita y auditada, nunca selección arbitraria. |
| R10 | ALTA | Se publica plan sin restore verificado/rollback de esquema | Evidencia de restauración reproducible en entorno descartable antes de activar la ruta. |

Todo riesgo crítico sin mitigación/certificación causal bloquea la implementación y nunca es compensado por un CI anterior, una fórmula de Sheet o una promesa.

## Supuestos (no hechos verificados)

- S01: hay o habrá diferentes clases de placement por empresa; falta contrato institucional de ownership y autorización de transición.
- S02: el catálogo de ubicación y conexión sólo es accesible por backend de plataforma; falta diseño del registro y rotación de credenciales.
- S03: tablas legacy sin EmpresaId positivo necesitan cuarentena/backfill verificable; no presumir que se puede migrar sin riesgos.
- S04: infraestructura propuesta shared/dedicated cumple límites de transacciones, backups y latencia; falta observación real y capacidad.
- S05: el ciclo de migración puede reintentarse y deshacerse con periodo de consistencia definido; falta ensayo causal de rollback.

## Decisiones pendientes

- D01: autoridad única para Placement(empresa) y versionado, sin DSN enviado por cliente.
- D02: política de compatibilidad shared/dedicated y cuándo rechazar/failover sin degradar seguridad.
- D03: control de concurrencia, congelamiento, reconciliación de delta y rollback antes de cortar tráfico.
- D04: administración de secretos, claves y backups por empresa sin PII en logs.
- D05: auditoría, trazabilidad de usuario, acción y destino con firma/huellas y retención delimitada.
- D06: condiciones medibles para probar MySQL/EF/migración Docker de una ruta implementada; sin deployments aprobados.

## Casos dirigidos de este contrato y siguientes gates

P01 positivo documental: diez riesgos R01–R10 con gravedad, negativo verificable y control. P02 positivo documental: cinco supuestos S01–S05 y seis decisiones D01–D06 explícitamente abiertos. N01 negativo: ningún TenantId, EmpresaId, DSN, host, cookie, JWT no autorizado o path se eleva a fuente única de autoridad. N02 negativo: no se reclama que haya dedicated storage real, migración ejecutada o datos restaurados. N03 negativo: no se concede PASS MySQL/Docker/E2E por los blobs documentales. N04 negativo: ninguna conclusión habilita M01.06/M02, qa/main/PROD ni A5_PASS ficticio.

Auditorías documentales: **A0** formato/diff, secretos, no cambios de dependencias; **A1** matriz P01/P02/N01–N04; **A2** amenazas de tenancy/credenciales/rollback; **A3** integración de Plan V5, DAG F/G, COLA E/G/O, fuentes Git y BITACORA A:Z (26 campos); **A4** Scope Lock SUCCESS exact-SHA y readback posterior al commit fast-forward. **A5** revisión por TASK_ID distinto, independiente y causal, antes de QA/main/PROD.

Rollback de esta unidad documental: nuevo commit aditivo de reversión sobre dev (sin force-push), preservar trazabilidad histórica del Sheet y sin destruir trabajo concurrente. **Revalidación obligatoria** al completar C02-01 y C00/C01, cuando cambie autoridad de tenant y antes de cualquier implementación o promoción.
