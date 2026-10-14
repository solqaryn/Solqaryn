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

## 2026-10-13 — Punto #4 (candidato): filtros de lectura empresarial EF

- Se incorpora `CommercialTenantReadFilterModel` al final de `AppDbContext.OnModelCreating` para combinar, no sustituir, los filtros existentes de borrado lógico con `EmpresaId` cuando la petición trae una marca RBAC verificada. La identidad del tenant proviene de `VerifiedCommercialTenant`, nunca de un valor directo de cliente. Incluye tipos con propiedad shadow y excepciones de visibilidad para roles/grants de plantilla con `EmpresaId=NULL`.
- Sin marca RBAC, esta defensa adicional no particiona lecturas: debe mantenerse la cuarentena HTTP de 2+ empresas. Tienda pública, acceso por token a factura, jobs, SQL directo, `IgnoreQueryFilters`, caché y archivos requieren resolución y certificación separadas antes de habilitar múltiples empresas. No se altera esquema ni QA/PROD.

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

# ARCHITECTURE — Solqaryn

### Actualización P0 #3 — PR #3599 (13 octubre 2026; candidato, NO fusionado)

Se añadieron unicidades comerciales por EmpresaId e índices compuestos de cinco raíces. Variantes, imágenes, detalles de compra y venta y facturas reciben EmpresaId nullable derivado/validado, además de doce claves foráneas compuestas en MySQL. Se incorporó migración Oracle EF10 y contratos físicos de bootstrap. Esto es un tramo de la solución, NO su certificación: quedan entidades comerciales descendientes por clasificar/proteger, filtros de lectura y validación MySQL exact-head. No levantar cuarentena de segunda empresa, no tocar QA ni PROD y no reasignar los 19 registros descartables.


## 1. Estilo arquitectónico

Solqaryn usa una arquitectura backend por capas, cercana a Clean Architecture pragmática:

`Domain <- Application <- Infrastructure`

`API` compone las dependencias y expone la aplicación por HTTP. Infrastructure implementa contratos definidos hacia Application/Domain. La dirección conceptual debe mantener las reglas de negocio independientes de detalles externos siempre que sea razonable.

El frontend Angular se organiza por funcionalidades con servicios compartidos y controles transversales en `core`.

### Contención adicional de escrituras comerciales (candidato PR #3596)

El `AppDbContext` recibe opcionalmente `IHttpContextAccessor` por DI y recupera exclusivamente la marca de empresa validada tras RBAC en `RequierePermiso`. Antes de `SaveChangesAsync`, `CommercialCrossTenantWriteGuard` rechaza operaciones `Added/Modified/Deleted` sobre raíces comerciales con dueño positivo ajeno al tenant verificado, incluso si `EmpresaId` permanece intacto. Para referencias nuevas del mismo `SaveChangesAsync`, el dueño proviene de entidades EF `Added`; las referencias ya persistidas se contrastan contra MySQL. No constituye autorización de lecturas, uso público, jobs ni backfill; el P0 permanece en cuarentena hasta certificación integral.

## 2. Componentes principales

### Frontend Angular

Versión de framework vigente: **Angular 22.2.1** (Core/CLI/Material/CDK 22.2.1), migrada desde Angular 21 mediante `ng update` oficial major-a-major. Node 24.21.0, npm 11.19.0, TypeScript 6.0.3, RxJS 7.8.2, tslib 2.8.1 y Zone.js 0.16.3 completan el toolchain frontend. SOLQARYN conserva `provideZoneChangeDetection` y `zone.js` como polyfill; la migración a zoneless queda fuera de este cambio arquitectónico.

Responsabilidades:

- navegación y UX;
- formularios y validación de presentación;
- guards de sesión/permisos;
- consumo de API;
- representación de módulos ERP;
- lazy loading de componentes.

### ASP.NET Core API

Responsabilidades:

- endpoints REST;
- autenticación/autorización;
- validación;
- coordinación de casos de uso;
- middleware de errores/seguridad;
- rate limiting;
- health/readiness;
- composición DI.

### Application

Responsabilidades:

- casos de uso;
- servicios de aplicación;
- DTO y contratos;
- validadores;
- coordinación de reglas funcionales.

### Domain

Responsabilidades:

- entidades y conceptos del negocio;
- enums aún vigentes/transitorios;
- invariantes que no dependen de infraestructura.

### Infrastructure

Responsabilidades:

- EF Core/MySQL;
- repositorios;
- configuraciones de entidades;
- migraciones;
- Cloudinary;
- SMTP;
- QuestPDF;
- integraciones concretas.

## 3. Flujos

### Petición autenticada

`Browser -> Angular Route -> authGuard/permisoGuard -> Component -> Service HTTP -> Controller -> Application Service -> Repository -> MySQL`

La ocultación de botones/rutas en frontend es UX, no frontera de seguridad. El backend debe rechazar operaciones no autorizadas.

### Persistencia

`Application Service -> Repository/UnitOfWork -> AppDbContext -> MySQL`

Cuando una operación modifica inventario/finanzas/documentos relacionados, debe preservarse consistencia transaccional y trazabilidad.

### Archivos y documentos

- imágenes/documentos: adaptadores Cloudinary; `ImagenUploadSecurity` valida firma/MIME/extensión, dimensiones y recodifica JPG/PNG/WebP con SkiaSharp 4.152.1 antes del almacenamiento externo;
- factura PDF: QuestPDF; el logo se toma de la configuración tenant de empresa y, si falta/no es descargable, se deriva un monograma del nombre empresarial; no existe logo global de cliente ni fallback `AppSettings__LogoPublicUrl` en runtime Render;
- correo: SMTP; PROD autentica Outlook.com mediante OAuth2/Modern Auth y mantiene secretos/tokens fuera del repositorio;
- enlaces públicos de factura: token seguro, expiración/revocación según implementación vigente.

### Storefront público: read models ligeros

Las lecturas públicas de productos usan una vía de consulta específica, separada del CRUD administrativo:

`TiendaController -> ICatalogoPublicoService -> IProductoCatalogoPublicoRepository -> proyecciones EF/MySQL`

Reglas:

- listado público pagina y filtra server-side; no descarga todas las páginas al navegador;
- listado y destacados responden con `TiendaProductoResumenDto`: descripción acotada, una imagen principal y variantes mínimas sin galerías/color/talla ni grafos administrativos;
- detalle público mantiene el contrato rico y carga galería/variantes sólo para el producto solicitado;
- carrito/checkout/cuenta rehidratan únicamente IDs persistidos mediante `POST /tienda/productos/contexto`;
- el repositorio público proyecta únicamente campos comerciales necesarios y evita `ConIncludes()` del repositorio administrativo;
- inventario y promociones siguen resolviéndose desde sus autoridades existentes; no se crea una segunda fuente de verdad;
- `GET /tienda/bootstrap` consolida la carga inicial del storefront en un único request scope: identidad pública mínima + WhatsApp público resuelto + tema visual + hasta 6 categorías de navegación + hasta 4 destacados ligeros;
- `ITiendaBootstrapService` compone autoridades existentes de forma secuencial dentro del mismo scope HTTP; no paraleliza repositorios EF que comparten `DbContext`;
- Angular comparte la respuesta bootstrap con `shareReplay`, de modo que shell, identidad y portada no compiten por lecturas públicas duplicadas; `StorefrontIdentidadService` conserva además el observable en vuelo y `StorefrontService` comparte la lista de categorías entre rutas;
- el backend usa `IMemoryCache` in-process mediante `IPublicStoreCache`/`PublicStoreMemoryCache`; cada key incorpora tenant, segmento, generación y hash de parámetros;
- TTL públicos: identidad/tema/categorías 5 minutos, destacados 30 segundos y listados 15 segundos; detalle, contexto de carrito y checkout permanecen sin cache;
- la capa HTTP comprime respuestas JSON/text con Brotli/Gzip sobre HTTPS mediante Response Compression de ASP.NET Core;
- el backend aplica `private, no-store, max-age=0` como política por defecto y sólo permite cache HTTP público mediante `PublicHttpCacheAttribute` en GET anónimos explícitamente clasificados;
- perfiles HTTP públicos: identidad/tema/WhatsApp/categorías `max-age=120, s-maxage=300, stale-while-revalidate=600`; bootstrap `15/30/60` por incluir destacados; productos/listados/detalle `max-age=5, s-maxage=15, must-revalidate`;
- las respuestas públicas cacheables emiten ETag débil SHA-256 sobre el payload JSON y resuelven `If-None-Match` con `304 Not Modified`; `Vary: Accept-Encoding` preserva corrección con Brotli/Gzip;
- contexto de carrito, checkout, sesiones, endpoints autenticados, administración, documentos y errores permanecen fuera de cache público;
- Vercel conserva el SPA/CDN: bundles Angular hashados reciben `Cache-Control: public, max-age=31536000, immutable`; `/api/*` entra a un proxy server-side que preserva `Cache-Control`/ETag/Vary emitidos por el backend;
- la cache tiene lock por key contra stampede y generaciones para invalidación sin enumerar entradas;
- `AppDbContext.SaveChangesAsync` invalida generaciones tras escrituras de producto/variante/imágenes/stock, categoría, identidad/WhatsApp, tema, marca/modelo y descuentos relacionados;
- la partición tenant se resuelve a `empresa:{EmpresaId}` cuando la identidad pública puede vincularse inequívocamente a una empresa; ante ambigüedad se usa un namespace `public-config:{Id}` fail-safe, evitando mezclar particiones;
- esta cache es válida para la topología actual de una instancia por servicio; si el API escala horizontalmente a múltiples instancias, el contrato `IPublicStoreCache` debe migrarse a almacenamiento distribuido con invalidación compartida antes de confiar en coherencia cross-instance;
- los endpoints anteriores quedan disponibles como recovery/rutas específicas, no como camino feliz inicial;
- no hay migración ni duplicación de datos ni servicio externo/pagado.

### Baseline de rendimiento DEV

La observabilidad de rendimiento DEV es first-party y no requiere un proveedor pagado:

- `RequestObservabilityMiddleware` mide duración HTTP y emite el baseline estructurado;
- `DbQueryTimingInterceptor` agrega cantidad y duración de comandos EF/MySQL por request sin registrar SQL ni parámetros;
- `PerformanceBaselineService` mide TTFB, requests/bytes por pantalla y Web Vitals en el navegador DEV;
- `frontend/scripts/performance-bundle-baseline.mjs` mide bundles raw/gzip/Brotli;
- la instrumentación está desactivada por defecto fuera de DEV y no modifica autoridad de negocio, RBAC, tenancy ni datos.

### Validación HTTP / FluentValidation

Desde Fase 8, la API usa FluentValidation 12.1.1 con registro DI oficial y un filtro MVC asíncrono propio (`FluentValidationActionFilter`). `FluentValidation.AspNetCore` está retirado. El filtro ejecuta `IValidator<T>` con `RequestAborted` y reutiliza `InvalidModelStateResponseFactory`, preservando el contrato HTTP existente; las excepciones de negocio/servicio continúan bajo `ExceptionHandlingMiddleware`.

La familia IdentityModel queda alineada en 8.23.0 mientras JwtBearer permanece 10.0.12. Swashbuckle.AspNetCore 10.2.3 conserva Swagger/OpenAPI y Bearer. La sanitización de uploads usa SkiaSharp 4.152.1 + native assets Linux; SixLabors.ImageSharp 3.1.12 quedó retirado después de que los scanners NuGet comenzaron a reportar advisories high/moderate sobre esa versión.

## 4. Patrones vigentes

- Dependency Injection.
- Repository.
- Unit of Work donde aplica.
- DTO/Service layer.
- FluentValidation.
- Soft-delete.
- Auditoría transversal.
- RBAC relacional.
- Lazy loading de rutas Angular.
- Expand-and-contract para migraciones legacy delicadas.

## 5. Seguridad

- JWT Bearer con issuer/audience/secret configurables.
- BCrypt para contraseñas.
- CORS por lista explícita.
- Rate limiting de login.
- Security headers.
- Separación estricta de DEV/QA/PROD.
- En runtime Render, `EnvironmentDatabaseGuard` enlaza fail-closed `ASPNETCORE_ENVIRONMENT` con endpoint Aiven, TLS, base y usuario MySQL canónicos: Development → `solqaryn_dev`/`solqaryn_dev_user`; Staging → `solqaryn_qa`/`solqaryn_qa_user`; Production → `solqaryn_prod`/`solqaryn_prod_user`; todos sobre `solqaryn-mysql-solqaryn.h.aivencloud.com:14402` y `SslMode=Required`.
- `RenderEnvironmentContractGuard` exige el mismo conjunto de 28 claves administradas en los tres servicios Render y valida constantes públicas compartidas sin comparar ni exponer secretos. Cloudinary, conexión, JWT, URLs/prefijos y refresh tokens pueden conservar valores propios del entorno.
- Secretos fuera del repositorio.
- SMTP OAuth2 en PROD usa access tokens efímeros obtenidos desde refresh token; no usa contraseña SMTP básica.
- `main` congelada durante el trabajo en `dev`.

RBAC debe basarse en relaciones persistentes y permisos explícitos, evitando bypasses implícitos por banderas administrativas.

## 6. Datos

Persistencia principal: MySQL mediante **EF Core 10.0.12** y Oracle **`MySql.EntityFrameworkCore` 10.0.9**.

Estado de la migración de versiones (2026-10-06): Fase 7 está integrada y certificada en `dev` sobre **`net10.0` + C# 14 + SDK 10.0.401 + runtime/ASP.NET 10.0.12 + EF Core/Design/CLI 10.0.12 + `MySql.EntityFrameworkCore` 10.0.9**. El runtime productivo de `dev` usa únicamente Oracle Connector/NET EF; Pomelo/MySqlConnector quedan fuera de los proyectos productivos y las 107 migraciones históricas Pomelo se conservan en un assembly de historia no cargado por runtime. El bootstrap de base nueva usa el contrato físico canónico certificado + `OracleBaseline`; no hace replay de la historia Pomelo. Docker build/runtime y GitHub Actions activos están alineados a .NET 10. La certificación post-merge exact-head usa `7013a55cd7491c3c79d6808df5db9ba0b8580f40`; no hubo promoción a `main`, QA o PROD.

Topología operacional vigente en Aiven:

- proyecto `solqaryn`;
- un único servicio MySQL Free `solqaryn-mysql`;
- base `solqaryn_dev` con usuario `solqaryn_dev_user` para DEV;
- base `solqaryn_qa` con usuario `solqaryn_qa_user` para QA;
- base `solqaryn_prod` con usuario `solqaryn_prod_user` para PROD;
- `avnadmin` reservado para administración;
- aislamiento DEV/QA/PROD lógico por base, usuario y GitHub Environment; los tres comparten el mismo servicio físico Aiven y el mismo endpoint/TLS corporativo.

Reglas:

- migraciones versionadas;
- no ejecutar migraciones productivas sin autorización;
- revisar operaciones destructivas;
- preferir migraciones aditivas durante transiciones;
- conservar historial cuando exista impacto contable/comercial;
- evitar eliminar físicamente catálogos referenciados por documentos históricos.

## 7. Fronteras del ERP

Orden rector:

`N0 saneamiento -> N1 inventario -> N2 compras -> N3 ventas -> N4 tesorería/CxC/CxP/contabilidad -> N5 BI -> N6 multiempresa -> N7 integraciones -> N8 production readiness -> N9 go-live/hypercare`

Los transversales T0–T12 aplican durante todo el roadmap.

## 8. Principios de cambio

1. Cambios pequeños antes que refactors globales.
2. No duplicar conceptos existentes.
3. No introducir una segunda vía de autorización, cálculo o persistencia sin razón arquitectónica documentada.
4. Preservar compatibilidad durante migraciones legacy.
5. Toda nueva dependencia transversal debe justificarse.
6. Toda modificación estructural debe actualizar `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md`, este archivo y `ARCHITECTURE_CHANGELOG.md`.

## 9. Qué se considera cambio arquitectónico importante

Requiere renovar el mapa arquitectónico una vez si ocurre, por ejemplo:

- nuevo proyecto/capa principal;
- cambio de framework mayor con impacto real;
- reemplazo de EF Core/MySQL;
- rediseño de RBAC/autenticación;
- multiempresa con cambio de tenancy transversal;
- event bus/mensajería distribuida;
- nuevo gateway/BFF;
- partición en microservicios;
- incorporación de un módulo ERP mayor que introduzca nuevas fronteras de dominio;
- cambio fuerte de despliegue, observabilidad o seguridad transversal.

No requieren reescaneo completo: correcciones de UI, CRUD, validaciones puntuales, nuevos campos localizados, pequeños endpoints o refactors internos sin cambio de fronteras.


### Identidad técnica canónica SOLQARYN

- Assemblies, namespaces, proyectos, solución, artefactos de build y claves técnicas propias usan únicamente la identidad Solqaryn / SOLQARYN.
- El storefront público es un módulo tenant-neutral bajo frontend/src/app/features/storefront; las marcas comerciales y nombres de empresas se resuelven desde datos/configuración, nunca desde nombres de código.
- La ruta pública técnica canónica del storefront es /tienda; dominios y nombres comerciales pertenecen a configuración, no al source code.


### Aislamiento de entornos en Vercel

`/api/*` no selecciona backend por hostname. `environment-binding.js` usa `VERCEL_PROJECT_ID` —variable de sistema inmutable del deployment— como identidad primaria y mantiene una allowlist canónica de los tres proyectos corporativos: `prj_1Anhx5mWyXEBX89lWC24Py6JXe7A` -> DEV/Render DEV, `prj_n5STx5F6VboqXd1oLUMR8AvZZtml` -> QA/Render QA y `prj_si3ORH7lBhM4aSAYfYvXsbJT2lHA` -> PROD/Render PROD. `SOLQARYN_ENV`, `API_UPSTREAM`, `PUBLIC_ORIGIN` y `SEO_INDEXING_ENABLED`, si están definidos, sólo actúan como overrides de coherencia y deben coincidir exactamente con el binding canónico; nunca seleccionan otro entorno. Proyecto desconocido, override incompatible o cualquier cruce falla cerrado. Alias, preview o custom domain no alteran el entorno.


### Contención P0 de aislamiento de datos comerciales (candidato DEV, 2026-10-12)

El contrato actual de membresías y permisos valida UsuarioEmpresa/RBAC, pero Producto, Cliente, Proveedor, Compra y Venta aún carecen de EmpresaId persistente; sus descendientes/compras/facturas/variantes requieren partición consistente. Hasta la migración y reatribución verificada, la API usa `UnscopedCommercialTenantGuardMiddleware`: sólo sirve datos cuando existe una única Empresa (incluidas inactivas en el recuento de seguridad). Con 0 o más de una Empresa bloquea rutas comerciales y públicas con 503 sin revelar datos; también se impide crear una segunda empresa por endpoints y por `EmpresaService`. Auth/health/tenant-context permanecen disponibles con sus controles. Esto no convierte el ERP en multiempresa funcional, no modifica schema ni datos y no reemplaza el contrato de acceso por fila. Detalles y definición estricta de cierre en `docs/runbooks/TENANCY_DATOS_COMERCIALES_P0.md`.

### Disponibilidad transitoria de la API durante el inicio de sesión — DEV (2026-10-12)

La topología Angular → Vercel /api/* → Render y el binding canónico por VERCEL_PROJECT_ID permanecen intactos. Antes de enviar el POST de autenticación, Angular consulta de forma anónima GET /health/ready mediante HttpBackend sin interceptores JWT ni tenant. Sólo esa comprobación puede reintentarse hasta tres veces frente a fallos transitorios, nunca un POST de autenticación. El proxy convierte fallos locales de socket en HTTP 503 con código estable, Retry-After y registro de causa no sensible. Conserva el timeout de 30 segundos y las cabeceras reales de la API.

Es una mitigación de UX y observabilidad, no un keep-alive ni un SLA always-on en Render Free. No cambia QA, PROD, DB, variables ni DNS. La certificación real requiere publicar el HEAD en Vercel DEV y ejecutar smoke; ver docs/runbooks/API_UPSTREAM_DISPONIBILIDAD_DEV.md.

### Normalización transitoria Cloudinary DEV (11/11 ejecutadas; flag desactivado)

El backend registra de forma condicional `DevCloudinaryLegacyNormalizationHostedService` únicamente bajo Render Development y `Cloudinary:NormalizeDevLegacyImagesOnStartup=true`. En la configuración versionada DEV el indicador es `false`; no afecta QA ni PROD. La operación pertenece a Infrastructure y usa el `AppDbContext` ya compuesto por API, sin endpoint ni DTO público nuevos. Revalida cloud, prefijo y tenant desde MySQL DEV, descarga y valida bytes de las 11 imágenes históricas, copia a rutas canónicas con Cloudinary DEV, compara SHA-256 y actualiza `ProductoImagenes` con compare-and-swap; conserva originales. Si alguna garantía falla, se detiene y el respaldo cifrado con restore probado permite recuperación. La ejecución única fue certificada en DEV (respaldo/restore previo, 11 copias SHA-256 verificadas, auditoría independiente `remaining=0`, descargas individual/ZIP aceptadas manualmente por el propietario). El flag está nuevamente en `false` y el runtime Render DEV `live` en `f6f79b9d`. El worker sigue disponible pero inerte; no se traslada ningún secreto ni se autoriza escritura a QA/PROD. Evidencia: `docs/CLOUDINARY_AISLAMIENTO.md`.

## 2026-10-13 — Datos comerciales de DEV descartables, sin backfill

El propietario confirmó que los 19 registros actuales sin EmpresaId son PRUEBAS descartables y no exigen migrar ownership histórico. No se han borrado ni reasignado. La posterior limpieza DEV preservará usuarios, credenciales, roles, permisos, membresías y configuración indispensable, previa restauración ensayada; detalles en docs/tenancy/LIMPIEZA_DATOS_PRUEBA_DEV.md.

La arquitectura de aislamiento futuro NO cambia: cada dato generado en una empresa requiere ownership server-side verificable, filtros en consultas y escrituras, FK y archivos consistentes, además de tienda pública, caché y jobs separados. La cuarentena para 2+ empresas permanece hasta certificación A/B real.

## 2026-10-13 — Expand de ownership comercial (sin cierre de aislamiento)

Las cinco raíces `Producto`, `Cliente`, `Proveedor`, `Compra` y `Venta` incorporan `EmpresaId?` nullable como transición expand. El modelo añade FK restrictiva e índice por raíz. La migración Oracle `20261013121500_CommercialTenantRootExpand` conserva las filas previas sin atribución; la base efímera fresh reconoce el esquema generado por `EnsureCreated` sin duplicar DDL.

La cuarentena de API y el bloqueo de alta de segunda empresa **permanecen**. Este cambio no garantiza multiempresa: pendientes prueba de procedencia legacy, filtros por empresa en todo el grafo y storefront, unicidades por tenant, respaldo/restauración y pruebas A/B reales.

## 2026-10-13 — Owner verificado para nuevas altas (candidato dependiente)

Las altas administrativas de Producto, Cliente, Proveedor, Compra y Venta propagan `EmpresaId` solo desde el identificador firmado por el filtro `RequierePermiso` en `HttpContext.Items`. La cabecera `X-Empresa-Id` no es fuente de autoridad. Los flujos sin contexto RBAC conservan NULL durante la etapa expand; esto NO certifica aislamiento de filas, relaciones, tienda pública, jobs ni datos históricos. La cuarentena P0 permanece.

## 2026-10-13 — Contención adicional de referencias comerciales cruzadas

`CommercialCrossTenantWriteGuard` valida antes de `AppDbContext.SaveChangesAsync` nuevas referencias Venta-Cliente, Compra-Proveedor, VentaDetalle-Producto y CompraDetalle-Producto cuando ambas partes tienen `EmpresaId` explícito. Rechaza mezclas empresariales; conserva NULL legacy sin atribución. No sustituye aislamiento por fila en consultas ni permite retirar la cuarentena P0.

La misma protección rechaza cambios de `EmpresaId` sobre las cinco raíces comerciales ya persistidas, incluida una atribución de `NULL` legacy mediante servicios ordinarios. La atribución histórica exige un procedimiento excepcional de datos respaldado y auditado, nunca actualización normal de entidad.

Las entradas nuevas de `RegistroAuditoria` se marcan con `EmpresaId` solamente cuando el filtro RBAC ha validado el contexto tenant de la solicitud. Un header HTTP no marca auditorías; registros históricos permanecen sin atribuir.
