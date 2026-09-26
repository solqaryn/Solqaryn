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
- Integraciones vigentes: Cloudinary, QuestPDF y SMTP; PROD usa Outlook.com con OAuth2/Modern Auth para `solqaryn.platform@outlook.com`.
- E2E/browser: Playwright/Chromium.
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

### Vercel
- Proyecto DEV: `solqaryn-dev`.
- Proyecto PROD: `solqaryn-prod`.

### Aiven
- Proyecto: `solqaryn`.
- Servicio MySQL: `solqaryn-mysql`.
- Bases: `solqaryn_dev` y `solqaryn_prod`.
- Usuarios de aplicación separados por entorno.

### Cloudflare
- La gestión de DNS/domino se trata como infraestructura de plataforma y no se asume requisito de DEV salvo que una tarea vigente lo necesite.

## 5. Dominios funcionales

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

## 6. Invariantes

- No exponer secretos.
- No mezclar datos entre empresas.
- No confiar en permisos visuales como sustituto del backend.
- Migraciones y cambios de datos deben ser explícitos, verificables y recuperables.
- No force-push.
- Revalidar HEAD antes de publicar.
- Cambios en `main`, PROD o infraestructura productiva requieren autorización explícita vigente.

## 7. Plan maestro vigente

El plan maestro que ejecutan las diez automatizaciones se define únicamente por objetivos, prioridades y dependencias actuales de SOLQARYN.

No hereda restricciones, numeraciones, fases, filas, gates ni prioridades que no hayan sido incorporadas expresamente a la versión vigente del MAESTRO.

La implementación existente puede ser modificada, reemplazada o retirada cuando el objetivo vigente lo requiera, siempre bajo controles de seguridad, integridad, trazabilidad, revisión y rollback proporcionales.
