## 2026-10-14 — P4: piloto privado de lecturas A/B (candidato, apagado por defecto)

- El middleware P0 mantiene la cuarentena `503` para 2+ empresas; sólo añade la bandera **`MultiEmpresa:PrivateReadPilotEnabled=false` por defecto**. Bajo opt-in controlado, únicamente `GET /productos`, `GET /clientes` y `GET /proveedores` pueden avanzar tras JWT autenticado, membresía/permisos RBAC efectivos de la empresa seleccionada y Empresa activa verificada en MySQL. `X-Empresa-Id` nunca autoriza por sí solo.
- La marca `VerifiedCommercialTenant` se establece antes de las consultas EF sólo tras la autorización server-side; el action filter `RequierePermiso` vuelve a verificar. Las rutas de detalle, mutaciones, archivos, exportaciones, jobs y alta de empresas siguen en `503` con 2+ empresas. No se modifica el esquema ni datos Aiven ni se habilita la bandera en Render/QA/PROD.
- Prueba negativa y positiva A/B en `UnscopedCommercialTenantGuardTests`: propietario A visible, B rechazado, JWT ausente, ruta similar y POST rechazados. Candidato sujeto a revisión exact-head y CI; **no constituye autorización para retirar la cuarentena ni certificación P4 integral**. Rollback: revert del PR; estado por defecto idéntico al anterior.

## 2026-10-14 — P4: lecturas auxiliares de métodos de pago con membresía tenant

- `GET /metodos-pago/activos` y `GET /metodos-pago/bancos-activos` tenían `[Authorize]` pero no emitían marca `VerifiedCommercialTenant` antes de consultar catálogos/bancos. Se agrega `RequiereMembresiaEmpresa`: autenticación validada, selección empresarial con cabecera **no autoritativa**, `IUsuarioScopeService.ObtenerActualAsync(empresaId)` con membresía/rol/empresa activos y marca empresarial solo tras comprobación.
- Se conserva la disponibilidad a empleados miembros de la empresa sin exigir el permiso administrativo `MetodosPago:Ver` para desplegables usados en cobros/ventas; las demás operaciones siguen con `RequierePermiso`. Pruebas de membresía A válida, B ajena, sin empresa seleccionada, JWT ausente, y contrato de atributos de ambos endpoints.
- Corrección sólo DEV, sin habilitar ruta alguna bajo cuarentena de 2+ empresas, sin migraciones, alteraciones Aiven, Cloudinary, QA ni PROD. Candidato a CI exact-head y REVIEW_FIRST; no representa certificación P4 integral.

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

## 2026-10-13 — Candidato DEV: lecturas empresariales por marca RBAC (punto #4)

- Se registra un filtro EF generalizado que combina el owner `EmpresaId` con los filtros previos cuando `VerifiedCommercialTenant` está presente. No hay atribución de registros históricos y los roles/permiso de plantilla siguen compartidos por diseño.
- Esto constituye contención adicional, no la certificación integral de #4: rutas sin marca, tienda anónima, inventarios/jobs/SQL directo, exports, archivos y filtros omitidos aún requieren auditoría A/B. La cuarentena de segunda empresa permanece hasta cierre integral; QA/PROD intocados.

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

# PROJECT_CONTEXT — SOLQARYN

### Actualización P0 #3 — PR #3599 (13 octubre 2026; candidato, NO fusionado)

Se añadieron unicidades comerciales por EmpresaId e índices compuestos de cinco raíces. Variantes, imágenes, detalles de compra y venta y facturas reciben EmpresaId nullable derivado/validado, además de doce claves foráneas compuestas en MySQL. Se incorporó migración Oracle EF10 y contratos físicos de bootstrap. Esto es un tramo de la solución, NO su certificación: quedan entidades comerciales descendientes por clasificar/proteger, filtros de lectura y validación MySQL exact-head. No levantar cuarentena de segunda empresa, no tocar QA ni PROD y no reasignar los 19 registros descartables.


> Contexto técnico canónico de estado actual. Este archivo describe únicamente la realidad vigente necesaria para trabajar sobre SOLQARYN.

## 1. Identidad y repositorio

- PROJECT_ID: `SOLQARYN`
- PROJECT_SCOPE_LOCK: `STRICT`
- `PROJECT_SCOPE_LOCK=STRICT`
- Plataforma: SOLQARYN.
- Repositorio: `solqaryn/Solqaryn`.
- Rama ordinaria de trabajo: `dev`.
- Rama productiva: `main`; cualquier cambio requiere autorización explícita vigente.
- GitHub Environments canónicos: `DEV`, `QA` y `PROD`.
- Identidad corporativa operativa: `solqaryn.platform@outlook.com`.
- Las empresas cliente/tenants no definen la identidad técnica ni operativa de SOLQARYN.
- El árbol versionado vigente aplica un gate repository-wide que rechaza cualquier identificador de plataforma retirado y la terminología inglesa heredada del dominio de inventario; no existe una identidad paralela a SOLQARYN.
- `Inventario` permanece como módulo funcional ERP legítimo en español. Sus rutas administrativas canónicas viven bajo `/inventario/...`; no existe un wrapper `/app/inventario/...` ni un store/plataforma legacy separado.

## 2. Regla de estado vivo

Antes de actuar:

1. leer `docs/VAEP_AUTHORITY.md`;
2. releer HEAD vivo de `dev`;
3. consultar sólo el estado operativo necesario para la tarea;
4. validar dependencias técnicas reales del scope;
5. usar CI/tests/readbacks causales cuando corresponda.

Ningún plan, fila, gate, fase o secuencia que no esté incorporado al MAESTRO vigente puede condicionar trabajo nuevo.

## 3. Arquitectura vigente

- **Protección comercial PR #3596 integrada en dev (d851ae19):** `AppDbContext` obtiene `VerifiedCommercialTenant` desde `IHttpContextAccessor` cuando existe marca post-RBAC y bloquea escrituras comerciales a owners explícitos distintos; asegura nuevos grafos de venta/compra en la misma transacción. No aísla lecturas ni datos históricos `EmpresaId=NULL`; no retirar cuarentena.

- Bitácora nueva tenant-aware: `AuditoriaService` firma `RegistroAuditoria.EmpresaId` sólo desde marca autorizada de RBAC. Las filas históricas y jobs sin contexto siguen sin atribución; no equivale a filtro empresarial de auditoría.


- Análisis P0 del snapshot Oracle EF10: 132 entidades mapeadas, 20 con `EmpresaId` y 66 entidades adicionales conectadas directa/indirectamente con cinco raíces comerciales. Inventario en `docs/tenancy/INVENTARIO_GRAFO_COMERCIAL_P0_DEV.md`. No clasificar las restantes como globales por omisión ni retirar la cuarentena.


- Validación parcial de escritura `CommercialCrossTenantWriteGuard`: si una venta/compra/detalle y su referencia ya tienen owners positivos diferentes, SaveChanges rechaza la asociación. Los NULL históricos permanecen sin atribuir y no se considera aislamiento por fila completo.


SOLQARYN es una plataforma empresarial multiempresa.

- Frontend: Angular 22.2.1 standalone, Signals y Angular Material/CDK 22.2.1. Toolchain frontend vigente: Node.js 24.21.0 LTS + npm 11.19.0 + TypeScript 6.0.3 + RxJS 7.8.2 + tslib 2.8.1 + Zone.js 0.16.3; `provideZoneChangeDetection` y el polyfill `zone.js` permanecen activos, sin conversión a zoneless. CI usa patches exactos y Vercel se gobierna por `engines.node=24.x`.
- Identidad técnica de código: namespaces/assemblies/proyectos usan Solqaryn.*; el storefront fuente es tenant-neutral y vive bajo features/storefront.
- Backend: ASP.NET Core 10 Web API sobre `net10.0`, C# 14, SDK 10.0.401 y runtime/ASP.NET 10.0.12.
- Capas: Domain <- Application <- Infrastructure; API compone y expone.
- Persistencia: MySQL con EF Core 10.0.12 y Oracle `MySql.EntityFrameworkCore` 10.0.9.
- Migración de versiones: Fase 7 está integrada y certificada en `dev`. El toolchain canónico es SDK 10.0.401, runtime/ASP.NET 10.0.12, `net10.0`, C# 14, EF Core/Design/CLI 10.0.12, JwtBearer 10.0.12 y provider Oracle `MySql.EntityFrameworkCore` 10.0.9. Los proyectos productivos no referencian Pomelo ni MySqlConnector; las 107 migraciones históricas Pomelo se preservan fuera del runtime para auditoría/rollback histórico, mientras las migraciones activas usan el assembly Oracle y `OracleBaseline`. El bootstrap fresh produce 136 tablas, `pending-model=0`, y CI conserva los contratos 1062/1205/1213, tenancy fail-closed y la integración MySQL. HEAD post-merge certificado: `7013a55cd7491c3c79d6808df5db9ba0b8580f40`. No se ha promovido Fase 7 a `main`, QA ni PROD.
- Fase 8 NuGet: FluentValidation 12.1.1 + DI oficial con filtro async propio; FluentValidation.AspNetCore retirado; BCrypt.Net-Next 4.2.0; Swashbuckle.AspNetCore 10.2.3; CloudinaryDotNet 1.29.3; ClosedXML 0.105.1; IdentityModel 8.23.0 alineado; SkiaSharp 4.152.1 como sanitizador de imágenes; ImageSharp 3.1.12 retirado por advisories NuGet; caching/JSON siguen resueltos por .NET 10 sin PackageReference redundante.
- Fase 10 tooling de pruebas .NET 10: `Microsoft.NET.Test.Sdk 18.10.1` y `Moq 4.21.0`; `Microsoft.EntityFrameworkCore.InMemory` y `.Sqlite` conservan `10.0.12` en consonancia con EF Core productivo `10.0.12`; `xunit 2.8.1` y `xunit.runner.visualstudio 2.8.2` permanecen en v2. Gate dedicado `modernization-phase10-testing-net.yml`; la evaluación de xUnit v3 se separa de este cambio. Solo test/CI, no runtime, provider ni esquema productivo.
- Seguridad: JWT, BCrypt, RBAC relacional, auditoría, CORS explícito, rate limiting y security headers.
- Integraciones vigentes: Cloudinary, QuestPDF y SMTP; DEV, QA y PROD usan Outlook.com con OAuth2/Modern Auth para `solqaryn.platform@outlook.com`.
- Facturas PDF: el branding visual se resuelve por empresa/tenant (`EmpresaConfiguracion.LogoUrl`); si no existe logo válido, QuestPDF usa un monograma derivado de `EmpresaNombre`. No existe fallback global `AppSettings__LogoPublicUrl` en Render ni branding fijo de un cliente.
- E2E/browser: Playwright/Chromium.
- Baseline de rendimiento DEV first-party: duración API, cantidad/tiempo de queries MySQL, TTFB/requests/bytes por pantalla, LCP/INP/CLS y tamaños de bundles; no requiere un servicio de observabilidad pagado.
- Storefront público de productos: read path dedicado con proyecciones ligeras, paginación/filtros server-side y contexto de carrito por IDs; `GET /tienda/productos` y destacados usan `TiendaProductoResumenDto` con imagen principal y variantes mínimas, mientras el DTO rico con galería queda reservado al detalle/contexto acotado; las lecturas públicas ya no materializan el catálogo administrativo completo.
- Bootstrap storefront: `GET /tienda/bootstrap` es la carga inicial canónica y pequeña para identidad pública, WhatsApp, tema, hasta 6 categorías de navegación y 4 destacados; Angular deduplica consumidores concurrentes y reserva las lecturas públicas separadas para recovery o pantallas específicas.
- Cache storefront: Angular comparte identidad en vuelo y categorías entre rutas; backend usa `IMemoryCache` tenant-aware con invalidación generacional. TTL vigentes: identidad/tema/categorías 5 min, destacados 30 s y listados 15 s. Detalle/contexto/checkout no se cachean. Si Render/API pasa a múltiples instancias simultáneas, esta implementación local debe sustituirse por una cache distribuida con invalidación compartida antes de asumir coherencia cross-instance.
- Cache HTTP storefront: ASP.NET comprime JSON/text con Brotli/Gzip y usa `no-store` por defecto. Sólo GET públicos allowlisted emiten `Cache-Control` público + ETag/304: identidad/categorías con TTL moderado y SWR, bootstrap corto por contener destacados, productos con TTL corto/must-revalidate. Vercel marca bundles Angular hashados como immutable por un año; el proxy server-side preserva `Cache-Control`/ETag/Vary emitidos por el backend. Sesión, contexto, checkout y administración nunca son cache público.
- La autorización del backend es la autoridad; la UI nunca sustituye controles de seguridad.
- **P0 de tenancy comercial vigente:** Aiven DEV tiene EmpresaId nullable en cinco raíces y protecciones parciales #3596; #3597 completó auditoría de procedencia. Los 19 registros históricos son PRUEBAS descartables por decisión del propietario (2026-10-13): no se hará backfill ni búsqueda adicional de dueño histórico. La futura limpieza preservará usuarios, credenciales, roles, permisos, membresías y relaciones imprescindibles de autenticación; no se ha ejecutado. El P0 real consiste en filtrar y proteger todos los DATOS FUTUROS de cada empresa en SQL, APIs, relaciones, storefront público, archivos, inventarios, finanzas, jobs y caché con pruebas A/B. Hasta ese certificado continúan cuarentena y bloqueo de alta de segunda empresa.
- Tenancy, integridad transaccional y trazabilidad deben preservarse en cambios de negocio.

Consultar `ARCHITECTURE.md` para cambios estructurales y `PROJECT_INDEX.md` para navegación dirigida.

## 4. Infraestructura canónica

### GitHub
- Repositorio: `solqaryn/Solqaryn`.
- Trabajo: `dev`.
- Environments: `DEV`, `QA` y `PROD`.

### Render
- Servicio DEV: `solqaryn-api-dev`.
- Servicio QA: `solqaryn-api-qa`.
- Servicio PROD: `solqaryn-api-prod`.
- Estado de compute actual: DEV, QA y PROD están en Render `free`. DEV/QA pueden aceptar cold starts para ahorro; PROD no se considera backend comercial `always-on` mientras permanezca Free. Está prohibido usar keep-alive artificial como sustituto de un plan always-on; cualquier upgrade productivo requiere autorización explícita de gasto.
- Health de plataforma Render: `/health` (liveness rápida). `/health/ready` se conserva para readiness/diagnóstico de dependencias como MySQL, pero no como probe de despliegue.
- SMTP DEV, QA y PROD: `smtp-mail.outlook.com:587` + STARTTLS + OAuth2/Modern Auth con identidad `solqaryn.platform@outlook.com`.
- DEV, QA y PROD no provisionan contraseña SMTP ni client secret OAuth2; cada entorno mantiene su propio refresh token en Render.
- Contrato Render canónico: 28 claves idénticas por nombre en DEV, QA y PROD; sólo cambian valores dependientes del entorno. Inventario y justificación: `docs/RENDER_ENVIRONMENT_CONTRACT.md`.
- En Render, el arranque backend aplica `EnvironmentDatabaseGuard`: `Development` sólo acepta `solqaryn_dev` + `solqaryn_dev_user`, `Staging` sólo `solqaryn_qa` + `solqaryn_qa_user`, y `Production` sólo `solqaryn_prod` + `solqaryn_prod_user`; además exige el endpoint Aiven corporativo `solqaryn-mysql-solqaryn.h.aivencloud.com:14402` con `SslMode=Required`. Entorno, host, puerto, TLS, base o usuario incompatibles fallan cerrados antes de registrar `AppDbContext`.
- `RenderEnvironmentContractGuard` valida en cada arranque Render las 28 claves administradas: mismo conjunto de nombres en DEV/QA/PROD, valores obligatorios no vacíos y constantes públicas compartidas coherentes; los secretos y las identidades propias del entorno pueden diferir. El runtime sólo registra conteo y fingerprints no sensibles.

### Binding Vercel -> API

- El backend de un deployment no se decide por hostname ni alias.
- `frontend/server/environment-binding.js` usa `VERCEL_PROJECT_ID` como identidad primaria del deployment y sólo reconoce los tres proyectos corporativos autorizados.
- Proyecto `prj_1Anhx5mWyXEBX89lWC24Py6JXe7A` resuelve exclusivamente DEV -> `solqaryn-api-dev-fxx8.onrender.com`; `prj_n5STx5F6VboqXd1oLUMR8AvZZtml` resuelve exclusivamente QA -> `solqaryn-api-qa.onrender.com`; `prj_si3ORH7lBhM4aSAYfYvXsbJT2lHA` resuelve exclusivamente PROD -> `solqaryn-api-prod.onrender.com`.
- `SOLQARYN_ENV`, `API_UPSTREAM`, `PUBLIC_ORIGIN` y `SEO_INDEXING_ENABLED` son controles opcionales de coherencia: si existen, deben coincidir con el proyecto canónico o el runtime falla cerrado.
- `frontend/api/backend-proxy.js` es la frontera server-side de `/api/*`; alias, previews y custom domains no cambian el upstream.
- Mitigación de arranques Render Free (candidato DEV 2026-10-12): el proxy clasifica únicamente errores de socket como `503 API_UPSTREAM_UNAVAILABLE` con `Retry-After`, sin datos sensibles en los logs. El login comprueba `GET /health/ready` por `HttpBackend` sin credenciales ni JWT (máximo 3 GET transitorios); solo después hace un único POST login. No hay keep-alive, cambios a QA/PROD ni garantía de always-on; requiere despliegue exact-head para validación real. Ver `docs/runbooks/API_UPSTREAM_DISPONIBILIDAD_DEV.md`.
- Un `VERCEL_PROJECT_ID` desconocido jamás obtiene fallback DEV/PROD.

### Vercel
- Proyecto DEV activo: `solqaryn-dev`.
- Proyecto QA activo: `solqaryn-qa`.
- Proyecto PROD corporativo activo: `solqaryn-prod`.
- PROD fue certificado sobre la rama `main` y permanece operativo mediante el alias administrado `https://solqaryn-prod.vercel.app`.
- No se reutiliza ningún proyecto personal o legacy.

### Aiven
- Proyecto: `solqaryn`.
- Servicio MySQL: `solqaryn-mysql`.
- **MySQL Aiven: 8.4.8 aprobada por el propietario el 2026-10-10; congelada sin upgrade ni maintenance-start**. La Fase 1 prueba la versión real vía `SELECT VERSION()` y falla si difiere; DEV/QA/PROD comparten ese servidor, con bases/usuarios segregados. MySQL 8.4 de Docker en CI es efímero e independiente del servicio Aiven.
- Bases: `solqaryn_dev`, `solqaryn_qa` y `solqaryn_prod`.
- Usuarios de aplicación separados por entorno: `solqaryn_dev_user`, `solqaryn_qa_user`, `solqaryn_prod_user`.

### Cloudflare
- La cuenta/DNS corporativos pertenecen a SOLQARYN.
- `solqaryn.com` está delegado correctamente a Cloudflare.
- DEV y PROD continúan usando las URLs administradas actuales mientras el cutover del dominio personalizado permanezca aplazado.
- El cutover DNS hacia PROD es una decisión deliberadamente diferida y no bloquea el estado productivo certificado.

### Cierre técnico DEV / futura promoción

- El contrato de evidencia, rollback y precondiciones para una futura promoción vive en `docs/DEV_CIERRE_TECNICO_PROMOCION.md`.
- Ese runbook no autoriza `main`/PROD ni modifica el Plan Maestro; cualquier promoción requiere autorización explícita nueva y certificación exact-head.
- La promoción nunca puede resolver PROD mediante infraestructura DEV ni reintroducir fallback por hostname/alias.

## 5. Estado de legado y migración histórica

- La migración histórica del tenant inicial hacia PROD ya fue ejecutada y certificada; el respaldo verificado se utilizó como fuente controlada de migración.
- Ningún deployment, proyecto, servicio, cuenta personal, repositorio, dominio o variable legacy se considera dependencia de SOLQARYN.
- La infraestructura legacy retirada no se consulta ni se reactiva como fallback.
- Los tenants/empresas cliente permanecen como datos de negocio dentro de SOLQARYN y nunca como identidad del código, rutas, proyectos o infraestructura.
- El estado productivo vigente se sostiene exclusivamente sobre la infraestructura corporativa certificada de SOLQARYN.

## 6. Dominios funcionales

Áreas principales:

- empresas y configuración empresarial;
- autenticación, usuarios, roles y permisos;
- productos, variantes, catálogos e imágenes;
- inventario, almacenes, ubicaciones, reservas y movimientos;
- compras, proveedores y cuentas por pagar;
- ventas, clientes, cotizaciones, pedidos y facturación;
- finanzas;
- descuentos e impuestos;
- auditoría;
- reportes;
- tienda pública de empresas cliente;
- automatizaciones de operación y control.

## 7. Invariantes

- No exponer secretos.
- No mezclar datos entre empresas.
- No confiar en permisos visuales como sustituto del backend.
- Migraciones y cambios de datos deben ser explícitos, verificables y recuperables.
- No force-push.
- Revalidar HEAD antes de publicar.
- Cambios en `main`, PROD o infraestructura productiva requieren autorización explícita vigente.

## 8. Plan maestro vigente

Existe **un solo Plan Maestro vivo** para roadmap y arquitectura objetivo:

- Google Doc nativo: `PLAN MAESTRO SOLQARYN`.
- ID: `1YdQlNJ312HuziyKb9E-GEt55dcgSmuFGgfsHxyzPUaw`.
- No existen V1/V2/V3/V5 ni roadmaps paralelos con autoridad ejecutable.
- Cualquier cambio aprobado de alcance, arquitectura objetivo, programa u objetivo SQ modifica ese mismo documento.
- `Notas SOLQARYN_DEV.docx` (ID `1WlqyRz09P7jARAg948Plg2UDKbKLSndT`) es exclusivamente una bandeja de observaciones; no es ejecutable hasta que el propietario apruebe su incorporación al Plan Maestro.

Contrato operativo y superficie administrativa:

- `docs/VAEP_AUTHORITY.md` es el contrato operativo de VAEP; no crea roadmap.
- Google Doc operativo auxiliar `SOLQARYN - AUTORIDAD OPERATIVA VAEP`: ID `1frrmekon0pBTrLcXk0yUZSJa4uISjEzrwrni3ja5y38`.
- Google Sheet `SOLQARYN - PLAN MAESTRO DE AUTOMATIZACIONES`: ID `1gcVyCoyhLU0jFMwRtf0s5_x8FSnfBs38ojml1QF7Xwk`.
- El Sheet fue rebasado al Plan Maestro actual: `SQ-000..SQ-353`; COLA, PLAN_MAESTRO y BITACORA históricos fueron retirados de la superficie viva.
- `PLAN_MAESTRO`, `COLA`, `BITACORA`, `DASHBOARD`, `CONFIG`, `LEYENDA`, `TAREAS_PROGRAMADAS`, `CONTROL_TOWER`, `AUTOMATIZACIONES` y `TAREAS_DE_SUPERVISION` son superficies administrativas derivadas.
- Fuentes técnicas ocultas: `_MASTER_SOURCE`, `_RUNTIME_SOURCE`, `_AUTOMATION_SOURCE`, `_EVENTS_SOURCE`.
- CURRENT_STATE_ONLY: planes, fases, filas, parents, queues, gates y receipts históricos no condicionan ejecución nueva.
- Estados únicos: `PENDIENTE`, `EN_PROGRESO`, `VALIDANDO`, `LISTO`, `BLOQUEADO`, `CANCELADO`.
- Las diez automatizaciones canónicas usan slots `:00,:05,:12,:17,:24,:29,:36,:41,:48,:53` y permanecen **PAUSADAS (0/10 habilitadas)** hasta autorización explícita del propietario.
- Responsabilidad operativa: Javier Mejía controla las cinco Primary `:00/:12/:24/:36/:48`; Alex Morales controla las cinco Supervisor `:05/:17/:29/:41/:53`.
- IDs Primary/Javier: `:00=6aa15346f5408191bdd9043fd26ff7aa`, `:12=6aa1534deee481918280def1343adcfa`, `:24=6abd70e42cb4819190b3b28916dc9dbb`, `:36=6aa1535a51508191a610e6cdb90a2a4d`, `:48=6aa1535f8cd48191b73e10f17843372a`.
- IDs Supervisor/Alex: `:05=6abd62255ae88191a2dba6e1b00d3b4d`, `:17=6abd6235e100819195785fc76a44a8e0`, `:29=6abd6241c3e48191a8856ed7b0f42c5c`, `:41=6abd624e5e108191bef6bb879559328c`, `:53=6abd625b397c8191a49904227946a20f`.
- Las cinco Supervisor antiguas que existían en la cuenta de Javier quedaron retiradas e inactivas; no deben reactivarse.
- Las automatizaciones escriben estado únicamente en fuentes técnicas autorizadas y ejecutan readback; las vistas visibles no son superficies de escritura de runtime.
- Los backups de Drive conservan historia únicamente como respaldo; no poseen autoridad operativa.
- Queda prohibido usar cuentas personales, repositorios retirados, ramas legacy o infraestructura legacy como fallback.

La implementación existente puede ser reutilizada, extendida, refactorizada o reemplazada cuando el objetivo vigente lo requiera, siempre preservando seguridad, RBAC, tenancy, integridad de datos, trazabilidad, revisión, rollback y las autorizaciones explícitas requeridas para `main`/PROD.


## Cloudinary DEV: normalización física y descargas LISTO (2026-10-12)

`DevCloudinaryLegacyNormalizationHostedService` (Infrastructure, compuesto en API) completó la normalización de **11/11 imágenes** de `ProductoImagenes` a locators DEV canónicos con comparación SHA-256 y actualización condicional (`originals_deleted=0`). Respaldo/restauración verificados: [run 38319634960](https://github.com/solqaryn/Solqaryn/actions/runs/38319634960). Auditoría MySQL read-only: [run 38323224517](https://github.com/solqaryn/Solqaryn/actions/runs/38323224517) PASS con `remaining=0`. El flag `Cloudinary:NormalizeDevLegacyImagesOnStartup=false` volvió a estar versionado y está LIVE en Render DEV SHA `f6f79b9d`. El propietario verificó descargas individual y ZIP autenticadas tras la normalización, **aceptación manual PASS** (no CI ZIP automatizado). Evidencia: `docs/CLOUDINARY_AISLAMIENTO.md`. QA/PROD y dominio fuera de alcance; promoción DEV → QA → PROD aplazada.

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

## 2026-10-14 — P4 SQL pesimista transversal: barrera empresarial antes de FOR UPDATE (candidato DEV)

- Nuevo `CommercialTenantSqlLockScope.RequireAsync`: exige marca comercial interna RBAC para accesos SQL directos si existen dos o más empresas, activas o no. Una marca de tienda pública/PDF jamás autoriza locks privados. Solo 0–1 empresas conservan el comportamiento histórico sin marca.
- Predicados parametrizados `EmpresaId` dentro del propio `SELECT ... FOR UPDATE` en 24 archivos de repositorios y servicios: cajas/sesiones, cotizaciones/pedidos/preparación, compras/suministros, inventario/variantes/existencias/almacén/trazabilidad, devoluciones, cuentas por pagar/facturas de proveedor, políticas de costeo y cargas masivas. Se comprueba antes de realizar el bloqueo, sin depender de `IgnoreQueryFilters` o composición EF posterior.
- `CargaMasivaService`: páginas, detalle y confirmación usan rol de membresía empresarial vigente con autorización privada; ya no aceptan `ICurrentUserService.EsAdministrador` de otra empresa como autoridad. Los locks de sus documentos/productos/variantes incluyen EmpresaId en SQL; para 0–1 empresas mantiene contrato antiguo.
- Prueba de alcance directo: dos empresas, una desactivada, sin RBAC o con marca pública = denegado; marca comercial A o B = alcance propio; empresa única = compatibilidad. La prueba integrada MySQL A/B sobre locks de productos permanece en la suite consolidada de P4.
- **No certificado, no habilitar multiempresa todavía**: CI exact-head/MySQL reales efímeros, regresiones de cargas, revisión de seguridad y readback de DEV obligatorios. No hay DDL/backfill ni cambios en QA/PROD; continúa la cuarentena HTTP y el dominio PROD aplazado.

## 2026-10-14 — P4 CI causal: el conteo previo a FOR UPDATE no debe abrir snapshot MySQL

- En el HEAD consolidado de PR #3623, el gate `CommercialTenantSqlLockScope.RequireAsync` hacía `SELECT COUNT` de empresas antes del lock `FOR UPDATE`. Bajo REPEATABLE READ, ese SELECT consistente abre el read view y puede permitir que dos transacciones concurrentes intenten insertar la misma variante técnica. El test de integración `ProductoVarianteTecnicaLifecycleIntegrationTests.AsegurarTecnica_Concurrente_CreaExactamenteUnaVariante` detectó duplicado en `IX_ProductoVariantes_ProductoTecnicoUnico`.
- Corrección limitada: en provider MySQL, el gate de 0–1/2+ empresas utiliza `SELECT e.Id AS Value FROM Empresas e ORDER BY e.Id LIMIT 2 FOR SHARE` (current read compartida) y evita crear snapshot MVCC antes de bloquear el documento; InMemory conserva el conteo para tests de contrato.
- Sin alterar DDL, integridad, propietarios, aislamiento A/B, credenciales ni QA/PROD; no se retira cuarentena P0. Cierre condicionado al nuevo CI exact-head, incluida la regresión de concurrencia MySQL. No equivale a certificación P4 integral.

## 2026-10-14 — P4 archivos: scope Cloudinary no acepta header de otra empresa (candidato DEV)

- El resolutor común `StorageTenantContextResolver` ahora exige que `X-Empresa-Id` coincida con `VerifiedCommercialTenant` cuando la acción ya tiene empresa verificada por RBAC. Una segunda membresía legítima B nunca puede reutilizar una autorización HTTP concedida bajo A para subir, leer o eliminar archivos B.
- Se comprueba además que `IUsuarioScopeService.ObtenerActualAsync(empresaId)` devuelva exactamente `scope.EmpresaId == empresaId`; contextos inconsistentes fallan cerrados. La selección y verificación de membresía existente se conserva para solicitudes monoempresa sin marca, mientras permanece la cuarentena 503 para 2+.
- Nuevas pruebas: RBAC A+header B con membresías múltiples (rechazo antes de consultar B), scope B devuelto para header A (rechazo), y marca/membresía A legítimas (permitido). Sin cambios de DDL, backups, Cloudinary real, QA/PROD o dominios.
- **No constituye certificado integral P4**: exige CI exact-head MySQL/seguridad, A/B downloads y readback de despliegue DEV.

## 2026-10-14 — P4: las secuencias fiscales no aceptan selector de empresa diferente al RBAC

- `SecuenciaDocumentoService.ValidarScopeAsync` exige coincidencia del `EmpresaId` del request con `AuthorizedCommercialEmpresaId` si la acción ya fue autorizada, antes de reservar o consultar números. Un usuario perteneciente a empresas A y B no puede utilizar la autorización A para reservar una secuencia fiscal B.
- Con 2+ empresas sin marca RBAC privada falla cerrado también ante invocaciones no HTTP; el contrato histórico de instalaciones con 0–1 empresa permanece. Comprueba igualmente que la membresía devuelta por el servicio coincide con la empresa solicitada.
- Tests A/B: autorización A con solicitud B y membresía B vigente rechazada sin invocar la membresía B; sin marca RBAC con dos empresas rechazada; no se reserva ni se incrementa numeración de B. Se conserva compare-and-swap MySQL y evidencia de transacciones anterior. No hay DDL, backfill ni cambios QA/PROD, cuarentena 503 sigue activa.
