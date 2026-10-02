# Entornos DEV / QA / PROD — SOLQARYN

Estado operativo vigente. Este documento reemplaza referencias históricas de infraestructura personal o legacy.

## Identidad

- Plataforma: `SOLQARYN`.
- Repositorio: `solqaryn/Solqaryn`.
- Rama DEV: `dev`.
- Rama QA: `qa`.
- Rama PROD: `main`.
- Cuenta corporativa operativa: `solqaryn.platform@outlook.com`.
- Los tenants/clientes no forman parte de la identidad técnica de la plataforma.

## GitHub

- Repositorio corporativo único: `solqaryn/Solqaryn`.
- Environments esperados: `DEV`, `QA` y `PROD`.
- Trabajo de implementación ocurre en `dev`.
- QA persistente se publica desde `qa` para pruebas internas y de clientes.
- Flujo canónico de promoción: `dev -> qa -> main`; `main` sólo recibe cambios certificados en QA y autorizados.
- Contrato canónico de ramas permitidas, variables y secretos de GitHub Environments: `docs/GITHUB_ENVIRONMENT_CONTRACT.md`.

## Render

| Entorno | Servicio | Rama | URL | Health de plataforma |
|---|---|---|---|---|
| DEV | `solqaryn-api-dev` | `dev` | `https://solqaryn-api-dev-fxx8.onrender.com` | `/health` |
| QA | `solqaryn-api-qa` | `qa` | `https://solqaryn-api-qa.onrender.com` | `/health` |
| PROD | `solqaryn-api-prod` | `main` | `https://solqaryn-api-prod.onrender.com` | `/health` |

`/health/ready` se conserva para diagnosticar dependencias como MySQL, no como probe de despliegue.

El contrato de variables vive en `docs/RENDER_ENVIRONMENT_CONTRACT.md` y exige paridad de nombres DEV/QA/PROD.

## Aiven

- Proyecto: `solqaryn`.
- Servicio MySQL: `solqaryn-mysql`.
- Base DEV: `solqaryn_dev`.
- Base QA: `solqaryn_qa`.
- Base PROD: `solqaryn_prod`.
- Usuarios de aplicación separados por entorno: `solqaryn_dev_user`, `solqaryn_qa_user`, `solqaryn_prod_user`.
- DEV puede aplicar migraciones de la app según política explícita.
- QA y PROD no aplican migraciones automáticamente; las migraciones llegan mediante promoción controlada.

La migración histórica del tenant inicial hacia `solqaryn_prod` ya fue ejecutada y certificada. Una futura promoción de código no repite esa migración ni usa DEV como fuente de datos; cualquier nueva operación de datos productivos requiere autorización explícita.

## Cloudinary

- Cloud corporativo certificado para DEV: `riyrzmob`.
- Prefijo DEV: `solqaryn_dev`.
- Prefijo QA: `solqaryn_qa`.
- Prefijo PROD: `solqaryn_prod`.
- Las credenciales son secretos por entorno.
- Activos históricos externos no son dependencia de runtime.

## Vercel

- Proyecto DEV activo: `solqaryn-dev`.
- Dominio DEV: `https://solqaryn-dev.vercel.app`.
- Proyecto QA activo: `solqaryn-qa` (`prj_n5STx5F6VboqXd1oLUMR8AvZZtml`).
- Dominio QA: `https://solqaryn-qa.vercel.app`.
- Proyecto PROD corporativo activo: `solqaryn-prod`.
- Dominio administrado PROD: `https://solqaryn-prod.vercel.app`.
- El proyecto PROD permanece separado del proyecto DEV; una futura promoción de código/configuración se ejecuta únicamente con autorización explícita.
- No se reutiliza ningún proyecto personal o legacy para PROD.

## Binding Vercel -> API

- `/api/*` usa binding explícito por `VERCEL_PROJECT_ID`; no selecciona backend por hostname ni alias.
- `prj_1Anhx5mWyXEBX89lWC24Py6JXe7A` sólo acepta DEV -> `solqaryn-api-dev-fxx8.onrender.com`; `prj_n5STx5F6VboqXd1oLUMR8AvZZtml` sólo acepta QA -> `solqaryn-api-qa.onrender.com`; `prj_si3ORH7lBhM4aSAYfYvXsbJT2lHA` sólo acepta PROD -> `solqaryn-api-prod.onrender.com`.
- Overrides opcionales `SOLQARYN_ENV`/`API_UPSTREAM`/`PUBLIC_ORIGIN`/`SEO_INDEXING_ENABLED` deben coincidir con el proyecto canónico; proyecto desconocido o cruce falla cerrado.
- Render valida además la pareja entorno/base/usuario: Development -> `solqaryn_dev`/`solqaryn_dev_user`; Staging -> `solqaryn_qa`/`solqaryn_qa_user`; Production -> `solqaryn_prod`/`solqaryn_prod_user`.
- Contrato de cierre y promoción: `docs/DEV_CIERRE_TECNICO_PROMOCION.md`.

## Cloudflare

- DEV no depende de dominio custom mientras use el dominio administrado por Vercel.
- El frontend PROD corporativo ya existe. El cutover del dominio personalizado permanece deliberadamente aplazado; no forma parte de esta fase ni de una futura promoción salvo autorización específica.
- Infraestructura personal/legacy no se usa como fallback.

## Correo

DEV, QA y PROD usan el mismo contrato de claves; cambian únicamente valores propios del entorno:

- host `smtp-mail.outlook.com`;
- puerto `587`;
- STARTTLS;
- identidad `solqaryn.platform@outlook.com`;
- OAuth2/Modern Auth;
- Client ID público compartido;
- refresh token secreto e independiente por entorno;
- sin contraseña SMTP;
- sin client secret OAuth2.

El transporte SMTP real permanece aplazado/no bloqueante mientras la conectividad del plan Render Free no lo permita de forma fiable. No se compra ni activa un servicio para forzarlo. Si correo SMTP vuelve a entrar explícitamente en alcance, se exigirá diagnóstico OAuth2 y envío controlado antes de declararlo certificado.

## Legado

El único origen heredado autorizado fue el **respaldo verificado de la base histórica del tenant inicial**, utilizado durante la migración ya cerrada. No son dependencias de SOLQARYN los despliegues, proyectos, cuentas, dominios, repositorios, variables o servicios personales antiguos, y no se reactivan como fallback.

## Promoción futura

El flujo permanente es `dev -> qa -> main`. La promoción `dev -> qa` exige CI exact-head y despliegue/certificación QA; la promoción `qa -> main/PROD` exige aprobación humana explícita, gates exact-head, snapshot/rollback de configuración, revisión de migraciones y smoke productivo. `dev -> main` queda fuera del flujo normal y debe ser bloqueado por el guard de promoción.
