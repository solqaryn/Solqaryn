# PROJECT_CONTEXT — SOLQARYN

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

SOLQARYN es una plataforma empresarial multiempresa.

- Frontend: Angular 22.2.1 standalone, Signals y Angular Material/CDK 22.2.1. Toolchain frontend vigente: Node.js 24.21.0 LTS + npm 11.19.0 + TypeScript 6.0.3 + RxJS 7.8.2 + tslib 2.8.1 + Zone.js 0.16.3; `provideZoneChangeDetection` y el polyfill `zone.js` permanecen activos, sin conversión a zoneless. CI usa patches exactos y Vercel se gobierna por `engines.node=24.x`.
- Identidad técnica de código: namespaces/assemblies/proyectos usan Solqaryn.*; el storefront fuente es tenant-neutral y vive bajo features/storefront.
- Backend: ASP.NET Core 8 Web API.
- Capas: Domain <- Application <- Infrastructure; API compone y expone.
- Persistencia: MySQL con EF Core 8/Pomelo.
- Migración de versiones: Fase 6/provider gate `PASS` en exact-head `37330696772` sobre `dev` HEAD `425f9cab0ed291401c081d7946289875eb11e798` (`P0=0`, `P1=0`, `PHASE7_EXECUTED=false`); el provider productivo certificado permanece net8.0 + EF Core 8/Pomelo 8. La ruta candidata para la futura Fase 7 es SDK 10.0.401/runtime 10.0.12 + EF Core/Design/CLI 10.0.12 + `MySql.EntityFrameworkCore` 10.0.9, probada en lanes aisladas con baseline/adopción (no replay de las 107 migraciones Pomelo). Fase 7 aún no se ejecuta.
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
