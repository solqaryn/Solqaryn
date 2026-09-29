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
- GitHub Environments canónicos: `DEV` y `PROD`.
- Identidad corporativa operativa: `solqaryn.platform@outlook.com`.
- VariStoreHN es una empresa cliente alojada en SOLQARYN; no define la identidad de la plataforma.

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

- Frontend: Angular 20 standalone, Signals y Angular Material.
- Backend: ASP.NET Core 8 Web API.
- Capas: Domain <- Application <- Infrastructure; API compone y expone.
- Persistencia: MySQL con EF Core 8/Pomelo.
- Seguridad: JWT, BCrypt, RBAC relacional, auditoría, CORS explícito, rate limiting y security headers.
- Integraciones vigentes: Cloudinary, QuestPDF y SMTP; DEV y PROD usan Outlook.com con OAuth2/Modern Auth para `solqaryn.platform@outlook.com`.
- E2E/browser: Playwright/Chromium.
- Baseline de rendimiento DEV first-party: duración API, cantidad/tiempo de queries MySQL, TTFB/requests/bytes por pantalla, LCP/INP/CLS y tamaños de bundles; no requiere un servicio de observabilidad pagado.
- Storefront público de productos: read path dedicado con proyecciones ligeras, paginación/filtros server-side y contexto de carrito por IDs; `GET /tienda/productos` y destacados usan `TiendaProductoResumenDto` con imagen principal y variantes mínimas, mientras el DTO rico con galería queda reservado al detalle/contexto acotado; las lecturas públicas ya no materializan el catálogo administrativo completo.
- La autorización del backend es la autoridad; la UI nunca sustituye controles de seguridad.
- Tenancy, integridad transaccional y trazabilidad deben preservarse en cambios de negocio.

Consultar `ARCHITECTURE.md` para cambios estructurales y `PROJECT_INDEX.md` para navegación dirigida.

## 4. Infraestructura canónica

### GitHub
- Repositorio: `solqaryn/Solqaryn`.
- Trabajo: `dev`.
- Environments: `DEV` y `PROD`.

### Render
- Servicio DEV: `solqaryn-api-dev`.
- Servicio PROD: `solqaryn-api-prod`.
- Health de plataforma Render: `/health` (liveness rápida). `/health/ready` se conserva para readiness/diagnóstico de dependencias como MySQL, pero no como probe de despliegue.
- SMTP DEV y PROD: `smtp-mail.outlook.com:587` + STARTTLS + OAuth2/Modern Auth con identidad `solqaryn.platform@outlook.com`.
- DEV y PROD no provisionan contraseña SMTP ni client secret OAuth2; cada entorno mantiene su propio refresh token en Render.
- Contrato Render canónico: 28 claves idénticas por nombre en DEV y PROD; sólo cambian valores dependientes del entorno. Inventario y justificación: `docs/RENDER_ENVIRONMENT_CONTRACT.md`.

### Vercel
- Proyecto DEV activo: `solqaryn-dev`.
- Proyecto PROD corporativo activo: `solqaryn-prod`.
- PROD fue certificado sobre la rama `main` y permanece operativo mediante el alias administrado `https://solqaryn-prod.vercel.app`.
- No se reutiliza ningún proyecto personal o legacy.

### Aiven
- Proyecto: `solqaryn`.
- Servicio MySQL: `solqaryn-mysql`.
- Bases: `solqaryn_dev` y `solqaryn_prod`.
- Usuarios de aplicación separados por entorno.

### Cloudflare
- La cuenta/DNS corporativos pertenecen a SOLQARYN.
- `solqaryn.com` está delegado correctamente a Cloudflare.
- DEV y PROD continúan usando las URLs administradas actuales mientras el cutover del dominio personalizado permanezca aplazado.
- El cutover DNS hacia PROD es una decisión deliberadamente diferida y no bloquea el estado productivo certificado.

## 5. Estado de legado y migración histórica

- La migración histórica de VariStoreHN hacia PROD ya fue ejecutada y certificada; el respaldo verificado se utilizó como fuente controlada de migración.
- Ningún deployment, proyecto, servicio, cuenta personal, repositorio, dominio o variable legacy se considera dependencia de SOLQARYN.
- La infraestructura legacy retirada no se consulta ni se reactiva como fallback.
- VariStoreHN permanece únicamente como **primer tenant/empresa cliente** dentro de SOLQARYN.
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

El plan maestro que ejecutan las diez automatizaciones se define únicamente por objetivos, prioridades y dependencias actuales de SOLQARYN.

No hereda restricciones, numeraciones, fases, filas, gates ni prioridades que no hayan sido incorporadas expresamente a la versión vigente del MAESTRO.

La implementación existente puede ser modificada, reemplazada o retirada cuando el objetivo vigente lo requiera, siempre bajo controles de seguridad, integridad, trazabilidad, revisión y rollback proporcionales.
