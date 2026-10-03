# ARCHITECTURE — Solqaryn

## 1. Estilo arquitectónico

Solqaryn usa una arquitectura backend por capas, cercana a Clean Architecture pragmática:

`Domain <- Application <- Infrastructure`

`API` compone las dependencias y expone la aplicación por HTTP. Infrastructure implementa contratos definidos hacia Application/Domain. La dirección conceptual debe mantener las reglas de negocio independientes de detalles externos siempre que sea razonable.

El frontend Angular se organiza por funcionalidades con servicios compartidos y controles transversales en `core`.

## 2. Componentes principales

### Frontend Angular

Versión de framework vigente: **Angular 21.2.x** (Core 21.2.25, CLI 21.2.24, Material/CDK 21.2.14), migrada desde Angular 20 mediante `ng update` oficial major-a-major. Node 24.21.0, npm 11.19.0 y TypeScript 5.9.3 completan el toolchain frontend certificado.

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

- imágenes/documentos: adaptadores Cloudinary;
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

Persistencia principal: MySQL mediante EF Core/Pomelo.

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
