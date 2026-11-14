# SOLQARYN V5 FINAL — C02-02.M01.03: inventario de fuentes para placement y almacenamiento

**TIPO 1: auditoría documental de fuentes sin implementación.** Fecha 2026-11-13; propietario PRIMARY DEV 01 JAVIER, TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a; repositorio solqaryn/Solqaryn@dev; PROJECT_SCOPE_LOCK=STRICT. Autoridad de trabajo: Standard V5 FINAL (Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA), Sheet 1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU.

## Objetivo atómico y límites

La fase C02-02 busca definir shared/dedicated storage, placement, router server-side y migración/rollback sin DSN proporcionado por el cliente. Esta unidad M01.03 identifica **contratos, rutas y evidencia existentes con huellas Git**. No implementa router ni DB dedicada, no certifica MySQL/EF en vivo y no habilita M01.06/M02 ni promoción. Antes de claim PLAN estaba PENDIENTE sin dueño, COLA G/O=SI, DAG_TECNICO F=VALIDADO/G=SI; sólo se autoriza preparación documental M01.01–05 y revalidación de prerequisitos tras finalizar las fases anteriores.

Clasificación de pruebas de **esta** entrega: requiere_dotnet=NO, requiere_node_angular=NO, requiere_mysql=NO, requiere_docker=NO, requiere_e2e=NO; requiere_security=SI (contratos tenant/RBAC/ownership y secretos analizados), requiere_scope_lock=SI para el commit documental exact-SHA. Un test source leído NO equivale a PASS de su ejecución. Cambios ejecutables futuros sí requieren sus jobs .NET/MySQL 8.4/Docker respectivos.

## Inventario de fuentes reales examinadas (rutas y Git blob SHA)

| ID | Fuente | SHA Git | Contrato observado y límite |
| --- | --- | --- | --- |
| F01 | backend/src/Application/Interfaces/StorageTenantContext.cs | 716e8e205502d6300f555947c79edc2852fd63ba | Contexto derivado de tenant server-side, ExigirEmpresa y prefijo empresas/{EmpresaId}; no garantiza almacenamiento físico dedicado. |
| F02 | backend/src/Domain/Security/ContextoTenantActual.cs | 7377d2d752985cc9baf31d08280f38ad94a6a114 | Contexto no creado por cliente, requiere membresía activa; rol empresarial separado. |
| F03 | backend/src/Infrastructure/Persistence/AppDbContext.cs | c4b32f6637f1b4426655bb0cd12af8dd9d229866 | Alcance de consultas por marca de autorizacion y vías públicas; debe auditarse por consumidor. |
| F04 | backend/src/API/Middleware/UnscopedCommercialTenantGuardMiddleware.cs | 558dda1eb5be729b2bd9a6d9fefbccd5db94addc | Cuarentena comercial con 2+ empresas y excepciones limitadas; no abre el DB router. |
| F05 | backend/src/Infrastructure.Migrations/Migrations/20261013220000_CommercialTenantOwnershipGraphExpand.cs | 9170f552351d122ef7f74628e59f7c61aacdc28d | Migración histórica aditiva de columnas EmpresaId nullable, sin cortar conexiones o hacer backfill. |
| F06 | backend/src/Domain/Entities/UsuarioEmpresa.cs | f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c | Vínculo Usuario/Empresa/Rol activo; identidad no otorga acceso cross-tenant. |
| F07 | backend/src/Application/Interfaces/IPublicStoreHostTenantResolver.cs | d181269e771dd4f4a833d4ea131732efdc87058a | Contrato del resolvedor de host público, no autoridad de storage dedicado. |
| F08 | backend/src/Application/Security/VerifiedCommercialTenant.cs | 2da140ce9d7f47329d39d292d3271596467910cc | Marca empresarial verificada por backend; header/DTO no es autorización. |
| F09 | backend/src/Application/Interfaces/IUsuarioScopeService.cs | 5345ee2de1b1e942bff8eded658f5dc27c0a6745 | Contrato de scope tenant-aware y defaults de rechazo seguro. |
| F10 | backend/src/Infrastructure/Services/UsuarioScopeService.cs | 14f4acbc5e1dc6a3ed1cf3485d54c7f1121b0e6f | Servicio desde EF: membership/roles; no afirma router compartido/dedicado. |

No se inspeccionaron **todos** los consumidores del SaaS. Esta lectura delimitada no certifica aislamiento universal, migración segura, despliegue, proveedor ni la existencia de un nuevo storage con DSN segregado.

## Dependencias y consumidores a revisar antes de implementación

| Capa o flujo | Consumidor/relación | Riesgo y prueba futura |
| --- | --- | --- |
| Controller/API de negocio | TenantPermissionContext, filtros RBAC, UsuarioScopeService | Un selector EmpresaId de B enviado por cliente A NO elige DB B; prueba HTTP negativa A/B. |
| Application/storage | StorageTenantContext y ContextoTenantActual | Recurso y prefijo deben pertenecer al scope verificado; tests positivos A y negativos B. |
| Infrastructure/EF | AppDbContext, migraciones versionadas, repositorios | Consultas y writes por tenant y transacciones sin fuga al cambiar placement; MySQL 8.4 real descartable. |
| Tienda y rutas públicas | Resolvedor de Host y VerifiedPublicStoreTenant | Host sólo habilita binding aprobado por backend, no un DSN del cliente ni permiso administrativo. |
| Jobs/batch | Services sin request HTTP | Requieren contexto de servicio server-side verificable y no fallback a empresa por defecto. |
| Backups/restore | Fuera del código revisado, contrato futuro | Checksum, corte, delta, recuperación y negativa contra filas B antes de cualquier migración. |

## Casos positivos/negativos de inventario documental

- **P01 (+):** diez fuentes F01–F10 con ruta y blob SHA real; ninguna ruta de infraestructura dedicada inventada.
- **P02 (+):** se separan API, Application, EF/MySQL, jobs, public host y backups; hay consumidor y riesgo por capa.
- **N01 (-):** una cabecera, JWT, rol RBAC global, host, slug o DSN del navegador no es autoridad de placement.
- **N02 (-):** la existencia de una migración EmpresaId nullable no demuestra aislamiento B, backfill ni separación física.
- **N03 (-):** cualquier mención a MySQL/Docker/tests se limita a verificación futura; NO_RUNTIME_CERTIFICATION para este documento.
- **N04 (-):** ninguna afirmación cierra anticipadamente C02-02 completo, altera permisos/datos, suprime A5 o habilita M01.06/M02.

## Contratos y puertas de auditoría

A0: revisar diff documental, ausencia de secretos nuevos, whitespace y CHANGELOG aditivo. A1: contrastar diez blobs, dos positivos y cuatro negativos documentales. A2: analizar fraude de selector/DSN, RBAC, impersonación, aislamiento A/B, secretos y jobs sin contexto. A3: Standard V5, DAG VALIDADO, PLAN H/I, COLA E/G/O y BITACORA A:Z **26 columnas**, sin atribuir PASS de integración de MySQL. A4: commit directo fast-forward dev, SHA exacto, Scope Lock completed SUCCESS, ascendencia/diff y readback. A5 independiente por otro TASK_ID antes de promoción; no A5_PASS por el autor.

**Fuera de alcance:** cero cambios .NET/Angular/MySQL, Docker, DB, datos, índices, proveedores, secretos, Hostinger, Cloudflare/R2, archivos históricos, DNS, QA/main/PROD, ramas, PR y Tasks. Inventario de imágenes históricas cero, migración NO_APLICA. Revertir sólo documento/CHANGELOG con commit aditivo autorizado si se invalida evidencia, preservando BITACORA sin reset/force-push. Reevaluar cuando C02-01, C00/C01 y contratos de placement estén certificados antes de M01.06/M02.
