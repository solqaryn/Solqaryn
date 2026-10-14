## 2026-10-14 — P4 admin reports tenant isolation (PR #3614, candidato DEV)

- ReportesAdministrativos: exige tenant RBAC verificado en resumen, usuarios, roles, auditoría y exportaciones CSV/XLSX/PDF. No acepta como autorización marcas de tienda pública ni token PDF.
- Roles efectivos y recuentos proceden de UsuarioEmpresa.RolId del tenant (no del Usuario.RolId global legacy). Las consultas de auditoría y sus archivos filtran EmpresaId explícitamente; filas sin dueño permanecen en cuarentena.
- Tests negativos: sin RBAC, capability pública, tenant A/B con roles globales divergentes, CSV y auditoría sin contaminación cruzada; se preserva la prueba PDF con RBAC legítimo.
- No DDL ni backfill; cuarentena HTTP de 2+ empresas preservada. **No certificar P4 integral** hasta CI causal exact-head y demás vías privadas, workers, archivos y aceptación MySQL A/B.

## 2026-10-14 — P4: defensa de propietario en lotes Outbox (DEV candidato)

- `OutboxRetryProcessor.ProcesarLoteAsync` comprueba que **todos** los mensajes reclamados tienen `MensajeOutbox.EmpresaId` exactamente igual al `empresaId` del procesamiento antes de ejecutar el primer `IOutboxEffectDispatcher`. Ante lote mixto A/B lanza error fail-closed sin despachar, confirmar ni reprogramar filas.
- Test negativo `N72DOutboxRetryProcessorTests.Lote_Con_Mensaje_De_Otra_Empresa_No_Despacha_Ni_Confirma_Nada`; preserva las pruebas existentes de retry/stale/dead-letter y contratos de repositorio que ya utilizan `EmpresaId`.
- Incremento independiente de los PR #3608–#3611 y del trabajo de startup #3612: sin DDL, sin backfill, sin cambios QA/PROD, sin habilitar segundas empresas. Requiere CI exact-head, review y merge para considerarlo integrado. **No certifica por sí solo el punto #4 completo.**

## 2026-10-14 — P4: seed de roles no modifica roles específicos de A/B (candidato DEV)

- `SeedPermisoService` sólo encuentra/repara plantillas compartidas con `EmpresaId=NULL` y `Ambito=Empresa`. Durante startup nunca reactiva el rol `ADMINISTRADOR` propio de B, ni concede automáticamente todos los permisos a un rol administrador personalizado, ni modifica un rol de Plataforma.
- Prueba `SeedPermisoServiceTests.StartupMultiEmpresa_NoRehabilitaRolPropioNiConcedeGrantsAOtraEmpresa`: crea roles de B desactivados y administrador B personalizado; valida que seed global no los altere y que sólo la plantilla recibe grants.
- Candidato aún sujeto a CI exact-head, revisión P0/P1 y MySQL efímero antes de fusionarse; sin retirar la cuarentena HTTP ni habilitar empresas adicionales.

## 2026-10-14 — P4: tareas legacy de arranque bajo 2+ empresas (candidato DEV)

- `ProductionDataRepairService.RepairAsync`: cuenta empresas **antes** del UPDATE SQL global de `Usuarios`. Con 2+ empresas (activas o inactivas) devuelve resultado sin ejecutar normalización ni atribuir membresías; conserva reparación monoempresa/legacy. Corrige un write fuera de tenant que no cubría middleware HTTP.
- `SeedFiscalService.SeedDefaultsAsync`: no crea impuestos ni operaciones fiscales globales sin `EmpresaId` cuando existen 2+ empresas. La inicialización fiscal de nuevas empresas requiere onboarding explícito verificado; esta protección no la implementa ni permite activar empresas todavía.
- `StartupLegacyJobsMultiEmpresaIsolationIntegrationTests`: prueba MySQL descartable con dos empresas A/B, B inactiva y un usuario histórico marcado eliminado, certifica que ningún seed global lo reactivó y que no se insertaron impuestos sin dueño.
- DEV exclusivamente. Ninguna migración ni dato histórico real modificado; QA/PROD y dominios aplazados intocados.
- **Incremento de contención, NO certificación P4 integral.** La cuarentena HTTP con 2+ empresas sigue vigente; aún hay que certificar jobs tenant-aware, reportes/exportaciones y habilitación segura end-to-end sin bypass. Requiere CI exact-head y REVIEW_FIRST.

## 2026-10-13 — P4: auditoría pública atribuida a empresa verificada (DEV candidato)

- `AuditoriaService.RegistrarCoreAsync` conserva precedencia `VerifiedCommercialTenant` → `VerifiedPublicInvoiceTenant` → `VerifiedPublicStoreTenant` al asignar `RegistroAuditoria.EmpresaId`. Enlaces PDF y checkout no usan el `X-Empresa-Id` bruto ni requieren inventar una identidad RBAC.
- `AuditoriaTenantOwnershipTests` cubre factura pública, tienda, prioridad RBAC y marcas fuera de ruta. La consulta posterior de auditoría se mantiene filtrada por propiedad empresarial EF.
- Aplica al PR #3611; pendiente CI exact-head y REVIEW_FIRST. Sin cambios QA/PROD; cuarentena general de 2+ no se elimina.

## 2026-10-13 — P4: acceso exclusivo a PDF público mediante capability empresarial (candidato DEV)

- `UnscopedCommercialTenantGuardMiddleware` permite atravesar la cuarentena con dos empresas únicamente a `GET /facturas/publico/{token}/pdf` cuando la ruta coincide exactamente y el token tiene formato URL-safe (32-160 caracteres). El middleware NO verifica ni confiere la capability: `FacturaCompartirRepository` comprueba hash, vigencia, límite, EmpresaId persistido y FK empresarial antes de marcar el contexto HTTP; `FacturaRepository` solo lee el FacturaId autorizado y EF filtra por empresa.
- Ni `X-Empresa-Id` ni el host constituyen autoridad; POST, prefijos engañosos, tokens cortos, rutas adicionales, facturas privadas, exportaciones y demás operaciones comerciales permanecen bloqueadas con 2+ empresas.
- Pruebas negativas y positivas en `UnscopedCommercialTenantGuardTests`: la ruta solo atraviesa el middleware pero no emite `VerifiedPublicInvoiceTenant` sin validar el token en el servicio.
- **No se declara certificación P4 integral:** falta CI exact-head, REVIEW_FIRST y cobertura de operaciones privadas/jobs multiempresa. Sin cambios de dominios, datos históricos, QA ni PROD.

## 2026-10-13 — P4: resolver Host exacto para múltiples tiendas (candidato DEV)

- `ConfiguredPublicStoreHostTenantResolver` implementa `IPublicStoreHostTenantResolver` a partir de `PublicStore:HostBindings:{N}:Host` y `EmpresaId`, con opt-in `PublicStore:EnableMultiEmpresaBinding=true` que permanece **apagado por defecto**. Hosts deben ser ASCII exactos y distintos, sin wildcard, IP, URL ni doble asignación; cualquier configuración ambigua falla cerrada.
- `UnscopedCommercialTenantGuardMiddleware` permite `/tienda/*` con 2+ empresas SOLO cuando `Request.Host` coincide con allowlist server-side y la Empresa persiste activa; las cabeceras `X-Empresa-Id` y `X-Forwarded-Host` no autorizan tenant. Emite `VerifiedPublicStoreTenant` por solicitud y `PublicStoreTenantKeyProvider` produce caché `empresa:{id}` independiente. Hosts no registrados/suspendidos se rechazan. `/productos`, `/facturas`, `/reportes` y demás rutas privadas continúan en cuarentena con 2+.
- Pruebas EF/InMemory y MySQL descartable A/B: host A solo ve A y host B solo ve B; host malicioso se rechaza; ninguna ruta privada se desbloquea. DNS/certificado real y operación pública multinavegador aún pendientes; sin activar flag en DEV/QA/PROD ni mover dominio `solqaryn.com`.
- Este incremento NO representa el cierre general de P4: persisten tareas de workers sin HTTP, exportación, PDFs públicos, Cloudinary, permisos y E2E públicos completos. La habilitación productiva requiere certificación integral y autorización posterior.

## 2026-10-13 — P4 DEV: cache pública con tenant vinculado (candidato)

- `PublicStoreTenantKeyProvider` utiliza `VerifiedPublicStoreTenant` emitido server-side para solicitudes `/tienda/*`; claves canónicas `empresa:{EmpresaId}` y rechazo estricto de rutas públicas sin vinculación. No acepta como autoridad cabecera, DTO, query ni nombre comercial.
- Se mantiene la resolución legacy cacheada solo para llamadas sin ruta pública y únicamente si existe UNA empresa activa; jamás usa el resultado legacy para sustituir la marca faltante de un request público.
- Pruebas: dos solicitudes A/B producen identidades y entradas de caché separadas; una ruta pública con `X-Empresa-Id` arbitrario sin marca validada falla cerrada. Integración MySQL, CDN/Cloudinary y host->empresa siguen requiriendo certificación independiente.
- **No habilitar 2+ empresas ni retirar el middleware de cuarentena en este cambio.** Rama y pruebas DEV; no modificar QA ni PROD.

## 2026-10-13 — P4: identidad empresarial y cuenta de tienda por tenant (candidato DEV)

- `EmpresasController` usa `TenantPermissionContext` y no enumera Empresas B al pedir la lista bajo RBAC de A; una búsqueda puntual por la empresa autorizada sustituye la carga global. Lectura por ID y edición/activación/desactivación exigen id de empresa igual al autorizado.
- Las validaciones de venta en `AppDbContext` aplican `TenantQueryBypassGuard` al consultar incluso Productos eliminados y evitan mensajes de error con nombres de otra empresa.
- Cuenta pública `TiendaCuentaCliente`: índice único de correo normalizado ahora `(EmpresaId, CorreoNormalizado)`. Migración `20261014112000_PublicStoreAccountEmailPerEmpresa` crea índice compuesto ANTES de borrar el global; rollback intenta restaurar primero índice global auxiliar y falla cerrado si existen correos repetidos A/B. Snapshot Oracle EF actualizado. Prueba de integración MySQL A/B incluye rollback negativo.
- **Falta certificación exact-head, REVIEW_FIRST y respuesta segura a procesos sin contexto HTTP, tiendas por host/slug y autorización de plataforma para crear nuevas empresas.** Mantener cuarentena de 2+ empresas; sin QA/PROD ni backfill de datos históricos.

## 2026-10-13 — P4: correo de cuenta de tienda único por Empresa (candidato DEV)

- Nuevo delta de tienda pública sobre el candidato de lectura P4: `TiendaCuentaClienteConfiguration` cambia la unicidad del correo normalizado de GLOBAL a `(EmpresaId, CorreoNormalizado)`. La migración aditiva `20261014112000_PublicStoreAccountEmailPerEmpresa` reemplaza únicamente el índice comercial `UX_TiendaCuentasCliente_Correo`; no repite migraciones P3 ni reescribe datos/contraseñas/sesiones históricas.
- Pruebas MySQL efímeras: mismas credenciales/email sintético en empresas A y B con cuentas/clientes independientes, sin lectura cruzada; mismo email en dos clientes de A debe ser rechazado. `EmpresaId=NULL` histórico continúa en cuarentena, sin atribución forzada.
- **No se habilita la tienda con 2+ empresas por esta mejora aislada:** faltan host/slug público verificable, resolución/caché/identidad por empresa, jobs y certificación end-to-end A/B. Mantener bloqueo P0, no tocar QA/PROD y no declarar cierre integral hasta CI verde exact-head + REVIEW_FIRST.

## 2026-10-13 — P4 seguridad de usuarios, membresías y regresión fiscal (candidato DEV)

- Corregida la regresión E2E: tras crear un `Usuario` sin membresías, su creador puede consultar una lista vacía y asignarlo a la empresa RBAC vigente; otros actores y usuarios pertenecientes exclusivamente a B continúan inaccesibles desde A.
- Reactivación: el filtro de `Usuario` ya considera una membresía de la empresa incluso si está inactiva; `UsuarioRepository.GetEmpresasAsync` lista esa membresía solo en el tenant autorizado y `GetEmpresaAsync` puede localizarla para reactivación sin abrir membresías ajenas. Pruebas de servicio, EF y MySQL A/B.
- Se corrigió el fixture **exclusivamente efímero** de la aceptación `catalogos-aceptacion`: los impuestos fiscales de prueba y sus filas `ImpuestoOperaciones` reciben `EmpresaId` coherente; sin backfill ni atribución real de datos legacy.
- Seguridad previa: `TenantQueryBypassGuard`, permisos de Finanzas por tenant y endpoints `UsuariosController` mantienen filtros de propietario. Los cambios no alteran migraciones P3 ni permiten crear una segunda empresa; la cuarentena 503 sigue vigente hasta cobertura de tienda/archivos/jobs.
- **Validación:** CI exact-head, Playwright, integración MySQL y revisión P0/P1 son requisitos pendientes hasta resultado comprobado; prohibido promocionar QA/PROD o marcar P4 completo solo por compilación.

## 2026-10-13 — P4 DEV: revisión de bypass y recuperación segura de onboarding

- En el PR #3608, `UsuarioRepository.GetPendientePrimeraMembresiaAsync` permite finalizar SOLO la primera membresía de una identidad sin empresas creada por el mismo actor; no abre listados de usuarios ajenos. Los endpoints `UsuariosController` comprueban que empresa del body/ruta coincide con la marca RBAC verificada, censuran otras membresías y sustituyen autorizaciones legacy por permisos tenant-aware.
- `ReportesInventarioValorizacionController` comprueba `Finanzas:Ver` en la empresa autorizada antes de revelar costos.
- `TenantQueryBypassGuard` vuelve a imponer owner sobre consultas de maestros, variantes, ubicación, comprobación de tipos de cliente, cargas masivas, auditoría de costo de envío y bloqueos de inventario que utilizan `IgnoreQueryFilters`. Conserva el caso monoempresa antiguo sin marca y la cuarentena HTTP 2+; no es una vía de excepción para habilitar multiempresa.
- Nuevas pruebas cubren creación inicial por actor, bloqueo de modificaciones de membresías A→B, censura de lista de empresas y filtro A/B aun con `IgnoreQueryFilters`.
- **Candidato en rama; NO es certificación integral ni autorización de nueva empresa:** quedan revisión de workers/no HTTP y consultas SQL que ignoran EF, binding público multiempresa, cobertura integral A/B con MySQL real, CI exact-head y cierre REVIEW_FIRST/P0-P1. QA/PROD sin cambios.

## 2026-10-13 — P4 identidad compartida (candidato con CI pendiente)

- La entidad global `Usuario` carece de `EmpresaId` por diseño; su filtro de lectura administrativa por `UsuarioEmpresa` restringe listados y consultas de identidad a miembros de la empresa post-RBAC. Sin marca, login/renovación aún pueden buscar identidades globalmente sin revelar listados HTTP comerciales.
- Prueba: `CommercialTenantReadFilterTests.UsuariosCompartidos_SeListanSoloConMembresiaEnEmpresaActual`. Debe aprobar exact-head junto al resto de la matriz. No habilitar segundo tenant ni declarar cierre completo solo por esta protección.

## 2026-10-13 — P4 DEV: ampliación del candidato (sin cierre total)

- Lecturas EF: filtro por `EmpresaId` en entidades empresariales, conservando soft-delete y roles plantilla. Las rutas con `IgnoreQueryFilters` en conteos y reversión de existencias reintroducen explícitamente el owner.
- Tienda anónima: bajo la cuarentena de una sola empresa activa, `UnscopedCommercialTenantGuardMiddleware` fija una marca de empresa server-side restringida a `/tienda`. Los filtros y las altas de cuenta consumen esa marca, nunca `X-Empresa-Id` del cliente. Si no existe empresa pública única, `PublicStoreTenantKeyProvider` falla cerrado; no infiere por nombres o `public-config`.
- PDF público: un token hasheado solo puede establecer el scope de lectura si el enlace persistido no ha expirado, tiene `EmpresaId>0` y no contradice RBAC. Enlaces de prueba sin dueño no reciben autorización; la lectura de factura posterior se filtra por tenant.
- Evidencia requerida antes de fusionar: revisión y CI exact-head del PR #3608, pruebas por módulo y MySQL real. **No se ha eliminado la cuarentena de 2+ empresas y NO se declara el punto #4 cerrado**. Faltan tareas/jobs, SQL directo, todas las variantes de archivo/export y dominio->empresa pública para convivencia simultánea. QA y PROD intactos.

## 2026-10-13 — Punto #4: filtros EF tenant-aware (candidato)

- Código: `backend/src/Infrastructure/Persistence/CommercialTenantReadFilterModel.cs`, activado por `AppDbContext.OnModelCreating`; prueba: `backend/tests/Solqaryn.Tests/CommercialTenantReadFilterTests.cs`.
- Alcance: lecturas EF con marca verificada post-RBAC, manteniendo borrado lógico y roles plantilla; los flujos anónimos/no EF siguen en cuarentena 2+ empresas y NO están certificados. No confundir esta protección con cierre P0 completo.

## 2026-10-13 — Ajuste de compatibilidad tenant: roles plantilla y fixture E2E

- Los grants de roles empresariales plantilla (Rol.EmpresaId nulo, Ambito Empresa) conservan propietario nulo y no reciben EmpresaId arbitrario. Los grants de roles empresariales personalizados mantienen validación A/B; los roles de plataforma no se modifican bajo una marca tenant.
- El fixture MySQL descartable E2E asigna el tipo inicial SIN_CLASIFICAR al único tenant de prueba antes de insertar clientes; no se atribuyen datos históricos reales ni se relajan FKs de clientes/tipos.
- Pruebas dirigidas para roles plantilla, roles ajenos y autoridad de plataforma. QA y PROD sin cambios.

## 2026-10-13 — Candidato final P0 #3: completar `EmpresaId` en modelo comercial

- El PR #3603 está integrado. Esta ampliación incorpora ownership en 42 tablas adicionales, incluidas `RolPermisos`, catálogos, finanzas, almacenes, contabilidad, caja, tienda y configuración comercial, mediante columnas nullable para legacy descartable.
- Se añaden FKs a `Empresas`, índices de identidad por empresa, 15 índices únicos complementarios en padres y 86 FKs compuestas sobre referencias entre tablas con propiedad empresarial; 25 unicidades de negocio dejan de ser globales.
- El catálogo exclusivamente técnico sigue compartido (`Empresas`, `Usuarios`, `Permisos`, `Planes`, `PlanModulos`, `UsuarioRolesPlataforma`, `Bancos`, `MetodosPago`). La unicidad del hash de token público permanece global para evitar colisiones de URLs.
- Oracle EF10/MySQL 8.4: expansiones `20261013230000` y `20261013231000`, bootstrap fresh, fixture legacy y tests A/B. No eliminar ni reatribuir información de prueba, ni abrir multiempresa antes de cerrar aislamiento de lecturas/servicios.
- Estado de este changeset: candidato; sólo pasa a certificado tras CI verde exact-head, revisión y merge a `dev`. QA/PROD intocados.

## 2026-10-13 — Candidato P0: esquema tenant para cotizaciones y pedidos

- EmpresaId nullable y FKs empresariales en Cotizaciones, CotizacionDetalles, PedidosVenta y PedidoVentaDetalles; 9 FKs compuestas vinculadas a Clientes, Productos, Variantes y documentos.
- Idempotencia y origen de pedido únicos por (EmpresaId, IdempotencyKey / CotizacionId). Migración Oracle 20261013193000, snapshot, fresh/adopt bootstrap, guard RBAC en escrituras.
- La cuarentena multiempresa continúa; no se atribuyen fixtures de prueba, no hay cambios QA/PROD ni certificación de todo el grafo## 2026-10-13 — Candidato de expansión integral del grafo comercial P0 #3

- Derivadas 46 entidades comerciales EF adicionales con EmpresaId nullable desde las cinco raíces, sin reconstruir/atribuir registros históricos descartables.
- Incorporados índices por empresa de identidad y relaciones, 46 FKs a Empresas y 12 índices únicos adicionales en padres existentes; migración Oracle EF Core 10 y pruebas dirigidas.
- La expansión de esquema es estructural, no certifica todavía FKs compuestas de todas las referencias, lecturas/servicios/jobs ni autorización de segunda empresa. Mantener cuarentena, QA/PROD intactos.

## 2026-10-13 — Candidato tributación/descuentos por EmpresaId

- `VentaImpuestos`, `CompraImpuestos`, `VentaDescuentos`, `HistorialUsoDescuentos`: propiedad empresarial nullable para legacy, índices owner-padre, 5 FKs compuestas con Ventas/Compras/Clientes y validaciones de nuevas escrituras.
- Migración Oracle EF10 aditiva, bootstrap MySQL fresh/legacy, pruebas A/B; no altera datos, QA ni PROD. Quedan catálogos/configuraciones y otros descendientes por aislar.
- Mantener cuarentena multiempresa y las protecciones de lecturas/escrituras existentes.

## 2026-10-13 — Candidato adicional P0 #3: factura y adjuntos de compra por empresa

- `FacturaDetalles`, `FacturaPagos` y `CompraDocumentos` incorporan `EmpresaId`, indices por empresa, tres FKs compuestas documentadas/validadas en MySQL y guard de escrituras RBAC. `Facturas` incorpora indice unico `(EmpresaId, Id)`.
- Migracion Oracle expand-only, snapshot, bootstrap fresh/adopt, simulacion legacy, pruebas A/B y verificacion fisica de FKs; no modifica ni atribuye los fixtures historicos.
- No levantar cuarentena ni habilitar nueva empresa hasta completar todo el modelo y los read paths; QA/PROD no tocados.

# PROJECT_INDEX — Solqaryn

### Actualización P0 #3 — PR #3599 (13 octubre 2026; candidato, NO fusionado)

Se añadieron unicidades comerciales por EmpresaId e índices compuestos de cinco raíces. Variantes, imágenes, detalles de compra y venta y facturas reciben EmpresaId nullable derivado/validado, además de doce claves foráneas compuestas en MySQL. Se incorporó migración Oracle EF10 y contratos físicos de bootstrap. Esto es un tramo de la solución, NO su certificación: quedan entidades comerciales descendientes por clasificar/proteger, filtros de lectura y validación MySQL exact-head. No levantar cuarentena de segunda empresa, no tocar QA ni PROD y no reasignar los 19 registros descartables.


Índice operativo del repositorio. Su objetivo es llevar al equipo directamente al área correcta sin reindexar todo el proyecto.

## Lectura inicial mínima

Regla: navegar por estado y arquitectura vigentes. No abrir planes, fases o documentos no vigentes como requisito de trabajo salvo que el MAESTRO actual los cite expresamente.


1. `AGENTS.md` — reglas obligatorias y gate de identidad.
2. `PROJECT_CONTEXT.md` — contexto técnico base e identidad `SOLQARYN`.
3. `TASKS.md` — trabajo pendiente/vigente.
4. última entrada relevante de `CHANGELOG_AI.md` — continuidad entre agentes.
5. `PROJECT_INDEX.md` — localizar archivos.
6. `ARCHITECTURE.md` — solo cuando la tarea implique arquitectura, dependencias transversales o diseño.

No leer todos los documentos administrativos en cada tarea. Consultarlos solo cuando sean relevantes.

## Raíz

- `AGENTS.md`: reglas obligatorias para cualquier agente.
- `PROJECT_CONTEXT.md`: memoria técnica canónica e identidad inequívoca del proyecto.
- `PROJECT_INDEX.md`: este índice.
- `ARCHITECTURE.md`: arquitectura y patrones.
- `ARCHITECTURE_CHANGELOG.md`: registro breve de cambios que alteran el mapa técnico.
- `TASKS.md`: pendientes operativos.
- `CHANGELOG_AI.md`: bitácora/evidencia de cada changeset.
- `README.md`: introducción, stack y operación básica.
- `CONTRIBUTING.md`: flujo Git y criterios de contribución.
- `render.yaml`: configuración versionada relacionada con Render; tratar con cautela por separación de entornos.
- `docs/ENTORNOS_DEV_PROD.md`: fuente canónica para GitHub Environments y topología Aiven DEV/QA/PROD (`DEV`, `QA`, `PROD`, `solqaryn-mysql`, bases/usuarios aislados).
- `docs/RENDER_ENVIRONMENT_CONTRACT.md`: contrato exacto de 28 claves Render y diferencias permitidas entre DEV/QA/PROD.
- `docs/runbooks/TENANCY_DATOS_COMERCIALES_P0.md`: aislamiento comercial incompleto y requisitos de partición futura (los 19 fixtures DEV no requieren backfill).
- `docs/tenancy/LIMPIEZA_DATOS_PRUEBA_DEV.md`: decisión de descarte sin tocar usuarios, y controles para posterior limpieza segura de datos de prueba DEV.
- `docs/runbooks/API_UPSTREAM_DISPONIBILIDAD_DEV.md`: diagnóstico de cold start, HTTP 503 y runbook de smoke del login, sin keep-alive y con QA/PROD fuera de scope.
- `docs/GITHUB_ENVIRONMENT_CONTRACT.md`: contrato de cardinalidad, ramas, variables/secrets y aislamiento Aiven para los tres GitHub Environments.
- `docs/DEV_CIERRE_TECNICO_PROMOCION.md`: evidencia del cierre DEV, rollback y precondiciones fail-closed para una futura promoción autorizada `dev -> main/PROD`; no es roadmap ni autorización productiva.

## Backend

- **Integrado en dev PR #3596:** `backend/src/Infrastructure/Persistence/AppDbContext.cs` y `CommercialCrossTenantWriteGuard.cs` exigen que las escrituras sobre datos comerciales con owner explícito concuerden con la marca RBAC de la empresa de la sesión; `backend/tests/Solqaryn.Tests/CommercialCrossTenantWriteGuardTests.cs` cubre A/B, grafos recién creados y bypasses. Aislamiento parcial de escrituras; seguir `docs/runbooks/TENANCY_DATOS_COMERCIALES_P0.md`.

- Bitácora de nuevos eventos comerciales: `backend/src/Application/Services/AuditoriaService.cs`, `VerifiedCommercialTenant.Resolve(http)` y tests `AuditoriaTenantOwnershipTests.cs`. No permite leer auditorías ajenas ni atribuir historial.

- Inventario verificable del grafo comercial EF (`132` entidades y `66` descendientes relacionados): `docs/tenancy/INVENTARIO_GRAFO_COMERCIAL_P0_DEV.md`. Clasificación preliminar; no significa aislamiento certificado.

- Gate de integridad de referencias empresariales explícitas: `backend/src/Infrastructure/Persistence/CommercialCrossTenantWriteGuard.cs`, llamado por `AppDbContext.SaveChangesAsync` con pruebas en `CommercialCrossTenantWriteGuardTests.cs`. No reemplaza los filtros de consulta ni la cuarentena P0.

- Propagación preliminar de propiedad comercial a nuevas altas: `Application/Security/VerifiedCommercialTenant.cs` (marca server-side RBAC), consumido por cinco servicios. No equivale a aislamiento completo.

- Candidato expand comercial: `backend/src/Infrastructure.Migrations/Migrations/20261013121500_CommercialTenantRootExpand.cs` y `backend/tests/Solqaryn.Tests/CommercialTenantRootOwnershipModelTests.cs`. La cuarentena de seguridad sigue siendo obligatoria hasta el aislamiento por fila.

`backend/Solqaryn.sln` — solución .NET.

### `backend/src/Domain`

Entidades, enums y elementos puros de dominio. No debe depender de Infrastructure ni API.

### `backend/src/Application`

Casos de uso, DTO, interfaces, servicios, validadores y contratos. Puede depender de Domain, no de detalles concretos de infraestructura.

### `backend/src/Infrastructure`

EF Core, `AppDbContext`, repositorios, configuraciones, migraciones, Cloudinary, SMTP, QuestPDF y demás adaptadores concretos.

### `backend/src/API`

Controladores, middleware, filtros, configuración HTTP, autenticación/autorización, DI y arranque. `Program.cs` es el composition root.

Performance DEV: `Observability/RequestPerformanceContext.cs`, `Observability/DbQueryTimingInterceptor.cs` y `Middleware/RequestObservabilityMiddleware.cs`; contrato y procedimiento en `docs/PERFORMANCE_BASELINE_DEV.md`.

Aislamiento Render/DB: `API/Configuration/EnvironmentDatabaseGuard.cs` valida en startup que Development/QA Staging/Production usen exclusivamente su base y usuario canónicos sobre el endpoint Aiven corporativo `:14402` con TLS requerido. `API/Configuration/RenderEnvironmentContractGuard.cs` exige exactamente las 28 claves Render administradas y valida el contrato compartido sin exponer secretos. Pruebas: `backend/tests/Solqaryn.Tests/API/EnvironmentDatabaseGuardTests.cs` y `RenderEnvironmentContractGuardTests.cs`.

Factura PDF multiempresa: `backend/src/Infrastructure/Services/QuestPdfFacturaPerfilesService.cs` consume `FacturaDto.EmpresaLogoUrl` (derivado de `EmpresaConfiguracion.LogoUrl`) y usa monograma del nombre empresarial si el logo falta/falla; Render no necesita `AppSettings__LogoPublicUrl`.

Storefront de productos: `TiendaController.cs` expone `bootstrap`, listado/detalle/contexto. La carga inicial vive en `Application/Services/TiendaBootstrapService.cs` + `DTOs/TiendaBootstrapDto.cs`; listados/destacados usan `TiendaProductoResumenDto` + `ProductoCatalogoResumenReadModel.cs`, el detalle/contexto conservan `ProductoCatalogoPublicoDto`; la resolución pública de WhatsApp vive en `Infrastructure/Services/WhatsAppPublicoService.cs`; las proyecciones de catálogo EF viven en `Infrastructure/Repositories/ProductoCatalogoPublicoRepository.cs`. Cache pública: contratos `Application/Interfaces/IPublicStoreCache.cs` + `IPublicStoreTenantKeyProvider.cs`, implementación `Infrastructure/Services/PublicStoreMemoryCache.cs` + `PublicStoreTenantKeyProvider.cs`, invalidación transversal en `AppDbContext.SaveChangesAsync`. Transporte/cache HTTP: `API/Filters/PublicHttpCacheAttribute.cs` clasifica únicamente GET públicos, emite Cache-Control/ETag/304 y `Program.cs` configura Brotli/Gzip + no-store global; `frontend/vercel.json` gobierna immutable de bundles hashados y enruta `/api/*` al proxy local; `frontend/server/environment-binding.js` resuelve DEV/QA/PROD por `VERCEL_PROJECT_ID` canónico y valida cualquier override de entorno/upstream; `frontend/api/backend-proxy.js` preserva las políticas HTTP del backend.

### `backend/tests`

Pruebas backend. Ejecutar pruebas dirigidas para cambios localizados y suite completa en cierres/cambios transversales.

## Frontend

Stack vigente: Angular 22.2.1 + Angular Material/CDK 22.2.1 sobre Node 24.21.0, npm 11.19.0, TypeScript 6.0.3, RxJS 7.8.2, tslib 2.8.1 y Zone.js 0.16.3; la aplicación conserva change detection basada en Zone.js.

### `frontend/src/app/core`

Autenticación, guards, interceptores, modelos y utilidades transversales. El baseline browser DEV vive en `core/performance/performance-baseline.service.ts`.

### `frontend/src/app/features`

Pantallas/módulos funcionales: productos, variantes, catálogos, compras, ventas, facturas, inventario, finanzas, usuarios, roles, permisos, auditoría, etc.

El storefront Storefront usa `storefront.service.ts`: `obtenerBootstrap()` comparte/deduplica la carga inicial, `obtenerCategorias()` comparte la lista entre rutas y `storefront-identidad.service.ts` conserva el observable de identidad en vuelo; el catálogo continúa paginado y `obtenerProductosContexto` rehidrata únicamente referencias del carrito/cuenta, evitando descargas completas.

### `frontend/src/app/services`

Clientes HTTP y servicios compartidos.

### `frontend/src/app/app.routes.ts`

Mapa principal de rutas y permisos. Revisarlo únicamente cuando una tarea afecte navegación, lazy loading o autorización por ruta.

### `frontend/e2e`

Pruebas Playwright para flujos críticos.

## Documentación

### `docs/`

Documentación funcional, técnica y guías complementarias de estado vigente.

Administración colaborativa central:

- `docs/COLABORACION_IA.md`
- `docs/COLABORATIVO.md`
- `docs/ENTORNOS_DEV_PROD.md`

No cargar toda la carpeta `docs` por defecto. Abrir únicamente el documento asociado al punto en ejecución.

## Automatización local de colaboración

- `scripts/iniciar-sesion-ia.ps1`: gate read-only de identidad, rama, HEAD, divergencia y estado del checkout. Ejecutarlo al inicio de cada conversación/sesión local.
- `scripts/configurar-colaboracion.ps1`: configuración inicial/sincronización del flujo local y hooks.
- `.githooks/pre-commit`: bloquea commits locales si repo/rama son incorrectos o falta `CHANGELOG_AI.md` en el changeset.
- `.githooks/post-commit`: publica commits de `dev` solo si `origin` pertenece realmente a Solqaryn.

## CI

- `.github/workflows/`: CI y automatizaciones versionadas.
- Para cambios puramente administrativos/locales puede utilizarse `[skip ci]` bajo las condiciones estrictas de `AGENTS.md`.
- Los workflows y triggers solo deben modificarse con inspección dirigida; no desactivar validaciones funcionales para ahorrar tiempo.

## Regla de navegación dirigida

Para una tarea normal:

1. confirmar PROJECT_ID/repo/rama;
2. identificar módulo;
3. abrir archivo de entrada;
4. seguir únicamente imports/interfaces/repositorios/DTO directamente relacionados;
5. buscar símbolos concretos;
6. detener expansión cuando exista evidencia suficiente.

No listar recursivamente todo `backend`, `frontend` o `docs` salvo cambio estructural justificado.

## Índice de decisión para cambios frecuentes

| Petición | Abrir primero | Seguir solo si aplica |
| --- | --- | --- |
| Endpoint o contrato HTTP | `backend/src/API/Controllers/<Area>Controller.cs` | DTO, interfaz y servicio homónimos en `Application`; cliente en `frontend/src/app/services` |
| Regla de negocio | `backend/src/Application/Services/<Area>Service.cs` | interfaz/DTO/validator; entidad o enum en `Domain` |
| Tabla, relación o índice | entidad en `backend/src/Domain/Entities` + `backend/src/Infrastructure/Persistence/AppDbContext.cs` | `Persistence/Configurations`, `Migrations` y scripts SQL de `backend/scripts` |
| Consulta o persistencia | `backend/src/Infrastructure/Repositories/<Area>Repository.cs` | interfaz en `Application/Interfaces` y configuración EF correspondiente |
| Pantalla o formulario | `frontend/src/app/features/<area>` | servicio HTTP en `frontend/src/app/services` y modelos compartidos |
| Ruta, menú o permiso visual | `frontend/src/app/app.routes.ts` o `features/**/**.routes.ts` | `core/guards`, `core/navigation` y permiso del endpoint backend |
| Login, JWT o permisos | `AuthController.cs`, `Program.cs` y servicios Auth/RBAC | `core/auth`, `core/guards`, interceptor y pruebas de acceso |
| Imagen, PDF, correo o exportación | interfaz en `Application/Interfaces` | implementación en `Infrastructure/Services` y registro DI en `Program.cs` |
| Variable o entorno | `backend/src/API/appsettings*.json`, `frontend/src/environments`, `frontend/vercel.json`, `render.yaml` | `docs/ENTORNOS_DEV_PROD.md`; nunca copiar secretos |
| Prueba localizada | `backend/tests/Solqaryn.Tests` o `frontend/e2e` | workflow específico en `.github/workflows` |

## Puntos de entrada, API y datos

- Backend: `backend/src/API/Program.cs`; controladores bajo `backend/src/API/Controllers`. La mayoría declara una base con `[Route("...")]`; salud se expone directamente como `/health` y `/health/ready`.
- Frontend: `frontend/src/main.ts` -> `frontend/src/app/app.config.ts` -> `frontend/src/app/app.routes.ts`. Algunas áreas agregan rutas en archivos `*.routes.ts` dentro de su feature.
- Datos: `backend/src/Infrastructure/Persistence/AppDbContext.cs` y `Persistence/Configurations`. Existen migraciones en `backend/src/Infrastructure/Migrations` y `backend/src/Infrastructure/Persistence/Migrations`; inspeccionar ambas ubicaciones y no moverlas ni consolidarlas desde un cambio local.
- Dependencias: proyectos `backend/src/*/*.csproj`, solución `backend/Solqaryn.sln`, `frontend/package.json` y `frontend/angular.json`.

## Mapa operativo por capas

```text
Angular route/component
  -> frontend/src/app/services o core/services
  -> API Controller ([Route]/[Http*])
  -> Application Service + DTO/validator/interface
  -> Domain entity/enum/invariant
  -> Infrastructure Repository/Service
  -> AppDbContext + Configuration + MySQL
```

- Angular: `frontend/src/app/features`, `core`, `services` y `app.routes.ts`.
- API: `backend/src/API/Controllers`; composición, middleware y DI en `backend/src/API/Program.cs`.
- Application: `backend/src/Application/{Services,Interfaces,DTOs,Validators}`.
- Validación HTTP Fase 8: `backend/src/API/Filters/FluentValidationActionFilter.cs`; gate NuGet permanente: `.github/workflows/modernization-phase8-nuget.yml`.
- Domain: `backend/src/Domain/{Entities,Enums,Common}`.
- Infrastructure/DB: `backend/src/Infrastructure/{Repositories,Services,Persistence}` y las dos carpetas de migraciones indicadas arriba.
- Migración de versiones DB/.NET: `dev` ejecuta Fase 7 con `net10.0`, C# 14, SDK 10.0.401, runtime/ASP.NET 10.0.12, EF Core/Design/CLI 10.0.12, JwtBearer 10.0.12 y Oracle `MySql.EntityFrameworkCore` 10.0.9. El runtime usa `backend/src/Infrastructure.Migrations` como assembly de migraciones Oracle; `backend/src/Infrastructure.Migrations.History` conserva las 107 migraciones Pomelo únicamente como historia auditable. Bootstrap fresh: `backend/tools/Solqaryn.DatabaseBootstrap`; gate permanente: `.github/workflows/modernization-phase7-dotnet-ef.yml`; HEAD post-merge certificado: `7013a55cd7491c3c79d6808df5db9ba0b8580f40`. No hay promoción a `main`/QA/PROD.
- Integraciones: contratos en `Application/Interfaces`, adaptadores concretos en `Infrastructure/Services` y registro DI en `Program.cs` (Cloudinary, QuestPDF, SMTP y exportaciones).

## Mapa por dominio

Las rutas mostradas son bases de navegación/API. Para una operación concreta, abrir el archivo citado y localizar su `[Http*]`, DTO o método; no cargar el dominio completo.

| Dominio | Angular / ruta | API y Application | Domain, Infrastructure y migraciones ancla |
| --- | --- | --- | --- |
| Autenticación | `features/login`; `/login`; `core/auth`, `core/guards`, `core/interceptors` | `AuthController` (`auth`); `AuthService`; `IJwtService` | `Usuario`; `UsuarioRepository`; `Infrastructure/Services/JwtService.cs`; `20260720101639_Fases18SesionConfigPermisosCalculos` |
| Usuarios | `features/usuarios`; `/usuarios` | `UsuariosController` (`usuarios`); `UsuarioService` | `Usuario`; `UsuarioRepository`; migraciones `Fase1UsuariosCategoriasImagenes` y `Fases11_17TemaFacturasUsuarios` |
| Roles y permisos | `features/roles`, `features/permisos`; `/roles`, `/permisos` | `RolesController`, `PermisosController`; `RolService`, `PermisoService`, `PermisoCatalogoService` | `Rol`, `Permiso`, `RolPermiso`; repositorios homónimos; `AddRolPermisos`, `Fases1_10RolesDescuentosImpuestos`, `N0_4_ConsolidarRbacRelacional` |
| Productos, categorías e imágenes | `features/productos`, `categorias`, `catalogos-producto`; `/productos`, `/categorias`, `/marcas`, `/modelos`, `/tallas`, `/colores` | `ProductosController`, `ProductoVariantesController`, `CategoriasController` y controladores de catálogo; servicios `Producto*`, `CategoriaService`, `CatalogoProductoService` | `Producto`, `ProductoVariante`, `ProductoImagen`, `Categoria`; repositorios homónimos; adaptadores Cloudinary; migraciones `CatalogosProductoYCategoriaSoftDelete`, `M1NormalizarMaestrosProducto`, `M2VariantesMultidimensionales`, `M2ImagenesPorVariante`, `N0_2_*`, `N0_3_*` |
| Proveedores | `features/proveedores`; `/proveedores` | `ProveedoresController` (`proveedores`); `ProveedorService` | `Proveedor`; `ProveedorRepository`; `20260711022435_AddProveedores` y migraciones N2 de evaluación/documentos cuando aplique |
| Clientes | `features/clientes`, `tipo-clientes`; `/clientes`, `/tipo-clientes` | `ClientesController`, `TipoClientesController`; `ClienteService`, `TipoClienteService` | `Cliente`, `TipoCliente`; repositorios homónimos; `AddClientes`, `AddTipoCliente`, `AddTipoClientePredeterminadoUnico` |
| Compras | `features/compras`, `solicitudes-compra`, `ordenes-compra`, `recepciones-compra`; rutas `/compras`, `/solicitudes-compra`, `/ordenes-compra`, `/recepciones-compra` | controladores `Compras`, `SolicitudesCompra`, `OrdenesCompra`, `RecepcionesCompra`, `FacturasProveedor`, `DevolucionesProveedor`, `NotasCreditoProveedor`; servicios homónimos y `ThreeWayMatchService` | entidades/repositorios `Compra*`, `SolicitudCompra*`, `OrdenCompra*`, `RecepcionCompra*`, `FacturaProveedor*`; migraciones `Fase2MovimientosCompras`, `N2_1_*` a `N2_9_*` repartidas entre ambas carpetas históricas |
| Ventas y facturas | `features/ventas`, `facturas`, `cotizaciones`, `pedidos-venta`; `/ventas`, `/facturas/:id`, rutas feature de cotizaciones/pedidos | `VentasController` (`ventas`), `FacturasController` (`facturas`), `CotizacionesController`, `PedidosVentaController`; `VentaService`, `FacturaService`, `CotizacionService`, `PedidoVentaService` | entidades/repositorios homónimos; `QuestPdfFacturaService`; migraciones `Fase3Fase4VentasFacturacionFinanzas`, `Fase8FacturacionPagosCostosEnvioVariantes`, `N3_1_CotizacionPersistencia`, `N3_2_PedidoVentaPersistencia`, `N3_3_C_PedidoVentaReservaInventario` |
| Inventario | `features/inventario`, `almacenes`, `sucursales`, `ubicaciones-almacen`; `/inventario/*` y rutas feature | controladores `AjustesInventario`, `MovimientosInventario`, `ExistenciasVariante`, `ReservasInventario`, `TransferenciasInventario`, `ConteosInventario`, `CosteoInventario`, `Almacenes`, `Sucursales`, `UbicacionesAlmacen`; servicios homónimos | entidades/repositorios de existencia, movimiento, ajuste, reserva, transferencia, conteo, costo y ubicación; migraciones `N0_6_*`, `N0_7_*`, `N1_1_*` a `N1_10_*` |
| Finanzas | `features/finanzas`; `/finanzas` y ruta feature `/cuentas-por-pagar` | `FinanzasController` (`finanzas`), `CuentasPorPagarController` (`cuentas-por-pagar`); `FinanzasService`, `CuentaPorPagarService` | `MovimientoFinanciero`, `RevisionFinanciera`, `CuentaPorPagar`; repositorios correspondientes; `Fase3Fase4VentasFacturacionFinanzas`, `N2_8_CuentasPorPagarPersistencia` |
| Descuentos e impuestos | `features/descuentos`, `impuestos`; `/descuentos`, `/impuestos` | `DescuentosController`, `ImpuestosController`; `DescuentoService`, `ImpuestoService` | `Descuento`, `Impuesto`; repositorios homónimos; `Fases1_10RolesDescuentosImpuestos` |
| Auditoría | `features/auditoria`; `/auditoria` | `AuditoriaController` (`auditoria`); `AuditoriaService` | `RegistroAuditoria`; `AuditoriaRepository`; `Fase9AuditoriaCentralizada` |
| Configuración | `features/configuracion`; `/configuracion` | `EmpresaConfiguracionController` (`empresa-configuracion`); `EmpresaConfiguracionService` | `EmpresaConfiguracion`; repositorio homónimo; migraciones transversales `Fases18SesionConfigPermisosCalculos` y `M12AutomatizacionTransversal` |
| Tema visual | configuración y servicios `tema-visual.service.ts`, `theme-applier.service.ts`; entrada desde `/configuracion` | `TemaVisualController` (`tema-visual`); `TemaVisualService` | `TemaVisual`; `TemaVisualRepository`; `Fases11_17TemaFacturasUsuarios` |
| Perfil | `features/perfil`; `/perfil` | `PerfilController` (`perfil`); `PerfilService` | usa `Usuario` y almacenamiento de imagen mediante `IPerfilImagenStorageService`/`CloudinaryPerfilImagenStorageService`; `Fase6SeguridadFacturacionPerfil` |

Los nombres de migración son anclas de búsqueda, no una lista exhaustiva ni autorización para aplicarlas. Confirmar siempre `AppDbContextModelSnapshot` y referencias al símbolo afectado.

## Flujos transversales

- Sesión y permiso: route guard/interceptor -> `AuthController` o controlador funcional -> autorización/servicio -> auditoría.
- Escritura transaccional: componente -> servicio HTTP -> controlador -> servicio Application -> repositorio/`IUnitOfWork` -> `AppDbContext`.
- Inventario comercial: compra/recepción o venta/pedido -> servicio funcional -> existencia/reserva/kardex -> movimiento y trazabilidad; revisar ambos dominios solo si el cambio cruza esa frontera.
- Facturación: venta/documento -> `FacturaService` -> repositorios -> QuestPDF; correo y enlaces públicos pasan por sus interfaces/adaptadores.
- Multimedia: UI multipart -> controlador/servicio -> interfaz de almacenamiento -> Cloudinary; las reglas de entorno permanecen en `docs/ENTORNOS_DEV_PROD.md`.
- Errores y observabilidad: `Program.cs` -> `CorrelationIdMiddleware` -> `ExceptionHandlingMiddleware`; filtros y health checks viven en API.

## Alcance: qué no volver a inspeccionar

Ante un cambio local no volver a leer toda la solución, todos los controladores, todas las migraciones, todo `docs` ni todos los tests. Abrir solo la fila del dominio, el punto de entrada y sus dependencias directas. Ampliar el alcance únicamente si:

- cambia un contrato compartido entre Angular y API;
- cambia entidad, relación, transacción o migración;
- afecta autenticación, permisos, auditoría, configuración o integración transversal;
- el símbolo aparece en varios dominios o la documentación contradice el código;
- el cambio es arquitectónico según `ARCHITECTURE.md`.

Para CSS, texto, validación de formulario o CRUD localizado, no inspeccionar migraciones ni dominios vecinos sin evidencia de dependencia. Para un endpoint localizado, no recorrer todos los controladores: seguir Controller -> DTO/Service -> Interface/Repository -> Entity/Configuration -> prueba dirigida.

## Comandos verificados

Desde `backend`:

```powershell
dotnet restore Solqaryn.sln
dotnet build Solqaryn.sln --configuration Release
dotnet test Solqaryn.sln --configuration Release
dotnet run --project src/API/Solqaryn.API.csproj
```

Desde `frontend`:

```powershell
npm ci
npm start
npm run lint
npm run build:prod
npm test
npx playwright test
```

Los scripts `start`, `lint`, `build:prod` y `test` existen en `frontend/package.json`; Playwright está declarado y `frontend/playwright.config.ts` existe.

## Convención de mantenimiento

Cuando cambie una capa, módulo, integración, ruta/API, modelo de datos o comando:

1. actualizar la fila o sección afectada de este mapa;
2. actualizar `PROJECT_CONTEXT.md` o `ARCHITECTURE.md` solo si cambia la realidad transversal;
3. agregar una entrada fechada a `ARCHITECTURE_CHANGELOG.md` con alcance, rutas y verificación;
4. registrar el changeset normal en `CHANGELOG_AI.md`.

No registrar en el changelog arquitectónico correcciones internas que no cambien este mapa.

Sanitización de imágenes: `backend/src/Infrastructure/Services/ImagenUploadSecurity.cs` usa SkiaSharp 4.152.1 para validar y recodificar JPG/PNG/WebP antes de Cloudinary; conserva límites de tamaño/dimensiones y no confía en MIME/extensión del cliente.


## Operación de normalización Cloudinary DEV

- `backend/src/Infrastructure/Services/DevCloudinaryLegacyNormalizationHostedService.cs`: worker opcional de migración idempotente de imágenes DEV, validación tenant y hash binario, desactivado por defecto.
- `backend/src/API/Program.cs` y `backend/src/API/appsettings.Development.json`: DI restringido a Render Development y flag `Cloudinary:NormalizeDevLegacyImagesOnStartup=false`.
- `backend/tests/Solqaryn.Tests/DevCloudinaryLegacyLocatorTests.cs`: casos positivos y negativos de Cloudinary, tenant, origen, y firmas de imagen.
- `.github/workflows/cloudinary-dev-backup-restore.yml` (PR #3575): respaldo real cifrado y restore efímero requerido antes de activar el worker.
- `scripts/audit_cloudinary_dev_locations.py`: lectura SQL posterior; exige cero referencias incorrectas para certificación.
- `docs/CLOUDINARY_AISLAMIENTO.md`: contrato de operación, integridad/rollback y cierre DEV 11/11 real; auditoría `remaining=0`, flag `false` y aceptación manual individual/ZIP posterior.

## 2026-10-14 — P4 ventas: reportes con membresía de empresa verificada (PR #3615 candidato)

- `ReporteVentasService` deja de utilizar `UsuarioScopeActual` global; resuelve `UsuarioTenantScopeActual` comprobando membresía de empresa desde el contexto server-side y añade `Venta.EmpresaId` explícito al conjunto de filas consultables.
- Sin tenant verificado, los resúmenes/detalles retornan cero filas; `VendedorId` suministrado por el cliente no expande el alcance. Pruebas A/B simultáneas y fixtures con sucursales de un mismo propietario.
- Sólo DEV; sin migraciones ni backfill, sin retirar cuarentena de segundas empresas, sin QA/PROD. Candidato sujeto a REVIEW_FIRST, CI exact-head y pruebas MySQL A/B.

## 2026-10-14 — P4 compras: reportes con ownership de documentos A/B (PR #3616 candidato)

- `ReporteComprasService` exige una empresa seleccionada por servidor y membresía vigente mediante `IUsuarioScopeService.ObtenerActualAsync(empresaId)`; en ausencia, rechaza incluso invocaciones fuera de middleware HTTP.
- El detalle de orden y sus anexos de facturas, recepciones, devoluciones y evaluaciones preservan `EmpresaId` explícito (EF.Property en propiedad shadow) en consultas y joins.
- La aceptación unitaria usa owner empresa A y B, demuestra separación y rechaza solicitudes sin scope. Sin DDL/backfill ni escritura a datos reales. Cuarentena general de 2+ permanece; sujeto a exact-head MySQL, CI y REVIEW_FIRST.

## 2026-10-14 — P4: locks SQL comerciales y conteos por EmpresaId (candidato DEV)

- `VentaRepository.GetByIdForUpdateAsync` y `CompraRepository.GetByIdForUpdateAsync` verifican el tenant RBAC privado y la membresía activa antes de realizar `SELECT ... FOR UPDATE`. En ambas ramas (administrador y no administrador) el predicado parametrizado `EmpresaId` está dentro del SQL de bloqueo; no depende de la composición de filtros EF.
- `CompraRepository.ContarTodasAsync` conserva la numeración histórica solo cuando existen 0-1 empresas; con 2+ deniega contextos sin RBAC o con capability pública, y aun al usar `IgnoreQueryFilters` cuenta solo filas del tenant validado. Los locks SQL conservan el modo monoempresa bajo conteo de empresas y exigen propietario en SQL con 2+.
- Prueba MySQL descartable A/B: documentos con números coincidentes, lecturas propias, cruces negados incluso mediante SQL FOR UPDATE, conteos A/B y rechazo de capabilities públicas. No hay migración, backfill, ni alteración de datos reales.
- **No certificar todavía el punto 4 integral ni quitar la cuarentena HTTP**: quedan repositorios tenant-aware de consultas ordinarias, inventario/Kardex, archivos/exports, workers y E2E de dos empresas. Gates obligatorios CI exact-head y revisión de seguridad previa a merge.

## 2026-10-14 — P4 inventario: filtros físicos requieren administrador de empresa (candidato DEV)

- Los reportes de Kardex, StockHealth y Reconciliación no aceptan `UsuarioScopeActual.EsAdministrador` legacy para filtros explícitos de Sucursal/Almacén/Ubicación: deben usar `UsuarioTenantScopeActual` validado contra el `EmpresaId` autorizado por `RequierePermiso`.
- La sobrecarga legacy de `ReporteInventarioScopeGuard` rechaza solicitudes con dimensión física; la nueva comprueba simultáneamente EmpresaId y permiso administrador de la membresía. En Kardex la empresa se obtiene de la marca RBAC del servidor; sin marca no se eleva acceso.
- Tests de autorización: legacy admin negado, cruce A→B negado y administrador empresarial positivo. Sin DDL, backfill, cambios de API/DTO ni QA/PROD. Aun quedan consultas legacy del repositorio Kardex; NO retirar contención 503 ni afirmar certificación P4 integral.

## 2026-10-14 — P4 Kardex: lectura A/B y orígenes tipados tenant-scoped (candidato DEV)

- `MovimientoInventarioRepository` ya no aplica exclusivamente el rol legacy/global para las consultas Kardex `GetByProductoAsync`, `GetFilteredAsync`, `GetPagedAsync`. Si existe marca RBAC, exige membresía vigente y filtra por `EmpresaId` shadow; los no administradores mantienen restricción por creador.
- En 2+ empresas, las consultas sin marca RBAC privada devuelven 0 resultados aun ante marca pública /tienda. Accesos auxiliares a orígenes tipados, movimientos posteriores y validaciones también usan query empresarial verificada. La compatibilidad legacy se conserva solo cuando existen como máximo una empresa (por evitar regresiones monoempresa mientras permanece cuarentena).
- Test A/B InMemory con productos/movimientos privados, lectura propia, cruce negado y admin global/ticket público sin autoridad. Sin DDL ni backfill ni cambios en QA/PROD. **No es certificado P4 integral**; falta confirmar CI exact-head y otras rutas no migradas.

## 2026-10-14 — P4 facturas privadas y PDF público con tenant verificable (candidato DEV)

- `FacturaRepository` obtiene la membresía `UsuarioTenantScopeActual` de la marca RBAC empresarial, aplica `Factura.EmpresaId` explícito a GetById/GetByVentaId/GetAll y preserva limitaciones por usuario no-administrador; si no existe marca y hay 2+ empresas, deniega incluso al admin global legacy. Compatibilidad monoempresa 0-1 preservada.
- El método `GetByIdParaEnlacePublicoValidadoAsync` requiere **ambas** marcas causales emitidas por verificación de token hash: `PublicInvoiceAccessContext.FacturaIdKey` y `VerifiedPublicInvoiceTenant.Resolve` en la ruta PDF pública; además exige `Factura.EmpresaId` exacto en consulta. La capability pública no habilita listados privados ni facturas de B.
- Test InMemory A/B cubre facturas propias, ID ajeno, vendedor/venta ajenos, enumeración vacía con 2+ sin RBAC, enlace A válido, enlace B denegado y path/capability ausentes. Sin migración/backfill, sin tocar QA/PROD; cuarentena general 503 permanece.
- Pendiente CI exact-head MySQL/E2E + REVIEW_FIRST; no representa certificación completa del punto 4.

## 2026-10-14 — P4 autoridad central IUsuarioScopeService fail-closed (candidato DEV)

- `UsuarioScopeService.ObtenerActualAsync()` legacy deja de conceder `Usuario.RolId/EsAdministrador` global con dos o más empresas, incluso si una está inactiva o se presenta únicamente una marca pública. Sin empresa privada seleccionada retorna null.
- Si `VerifiedCommercialTenant` certifica EmpresaId, el contrato legacy deriva por compatibilidad el rol desde `UsuarioEmpresa` activa y validada (usuario, empresa y rol vigentes), por lo que los consumidores aún no migrados no pueden elevar permisos con el rol global de otra empresa.
- Se conserva comportamiento histórico para 0-1 empresas, solo bajo cuarentena HTTP existente. Prueba A/B: mismo usuario con rol operador A y admin B, null sin marca y marca tienda pública, roles correctos con RBAC A/B. Sin DDL/backfill, sin tocar QA/PROD ni habilitar nuevas empresas.
- Candidato sujeto a CI exact-head + pruebas MySQL/E2E de consumidores; **no representa certificación integral de P4**, siguen necesarios los guards de repositorios/raw SQL/archivos/jobs y su aceptación A/B.
