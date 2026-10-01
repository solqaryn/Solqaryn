# Entornos DEV / PROD — SOLQARYN

Estado operativo vigente. Este documento reemplaza referencias históricas de infraestructura personal o legacy.

## Identidad

- Plataforma: `SOLQARYN`.
- Repositorio: `solqaryn/Solqaryn`.
- Rama DEV: `dev`.
- Rama PROD: `main`.
- Cuenta corporativa operativa: `solqaryn.platform@outlook.com`.
- VariStoreHN es el primer tenant/cliente; no es la identidad de la plataforma.

## GitHub

- Repositorio corporativo único: `solqaryn/Solqaryn`.
- Environments esperados: `DEV` y `PROD`.
- Trabajo y certificación ocurren primero en `dev`.
- `main` sólo recibe cambios certificados y autorizados.

## Render

| Entorno | Servicio | Rama | URL | Health de plataforma |
|---|---|---|---|---|
| DEV | `solqaryn-api-dev` | `dev` | `https://solqaryn-api-dev-fxx8.onrender.com` | `/health` |
| PROD | `solqaryn-api-prod` | `main` | `https://solqaryn-api-prod.onrender.com` | `/health` |

`/health/ready` se conserva para diagnosticar dependencias como MySQL, no como probe de despliegue.

El contrato de variables vive en `docs/RENDER_ENVIRONMENT_CONTRACT.md` y exige paridad de nombres DEV/PROD.

## Aiven

- Proyecto: `solqaryn`.
- Servicio MySQL: `solqaryn-mysql`.
- Base DEV: `solqaryn_dev`.
- Base PROD: `solqaryn_prod`.
- Usuarios de aplicación separados por entorno.
- DEV puede aplicar migraciones de la app según política explícita.
- PROD no aplica migraciones automáticamente.

La base productiva nueva no recibe la data histórica de VariStoreHN hasta el cierre final de plataforma y autorización de migración.

## Cloudinary

- Cloud corporativo certificado para DEV: `riyrzmob`.
- Prefijo DEV: `solqaryn_dev`.
- Prefijo PROD: `solqaryn_prod`.
- Las credenciales son secretos por entorno.
- Activos históricos externos no son dependencia de runtime.

## Vercel

- Proyecto DEV activo: `solqaryn-dev`.
- Dominio DEV: `https://solqaryn-dev.vercel.app`.
- Proyecto PROD corporativo activo: `solqaryn-prod`.
- Dominio administrado PROD: `https://solqaryn-prod.vercel.app`.
- El proyecto PROD permanece separado del proyecto DEV; una futura promoción de código/configuración se ejecuta únicamente con autorización explícita.
- No se reutiliza ningún proyecto personal o legacy para PROD.

## Binding Vercel -> API

- `/api/*` usa binding explícito por proyecto/entorno; no selecciona backend por hostname ni alias.
- DEV sólo acepta `solqaryn-api-dev-fxx8.onrender.com` y PROD sólo `solqaryn-api-prod.onrender.com`.
- Configuración ausente, ambigua o cruzada falla cerrada.
- Render valida además la pareja entorno/base/usuario: Development -> `solqaryn_dev`/`solqaryn_dev_user`; Production -> `solqaryn_prod`/`solqaryn_prod_user`.
- Contrato de cierre y promoción: `docs/DEV_CIERRE_TECNICO_PROMOCION.md`.

## Cloudflare

- DEV no depende de dominio custom mientras use el dominio administrado por Vercel.
- La activación DNS de PROD se hace únicamente después de crear y certificar el frontend PROD corporativo.
- Infraestructura personal/legacy no se usa como fallback.

## Correo

DEV y PROD usan el mismo contrato:

- host `smtp-mail.outlook.com`;
- puerto `587`;
- STARTTLS;
- identidad `solqaryn.platform@outlook.com`;
- OAuth2/Modern Auth;
- Client ID público compartido;
- refresh token secreto e independiente por entorno;
- sin contraseña SMTP;
- sin client secret OAuth2.

La certificación real exige `SMTP_OK` y un envío controlado en DEV antes de repetir la prueba en PROD.

## Legado

El único artefacto legacy autorizado es el **respaldo verificado de la base histórica de VariStoreHN**. No son dependencias de SOLQARYN los despliegues, proyectos, cuentas, dominios, repositorios, variables o servicios personales antiguos.

La migración final usa el respaldo como fuente y carga los datos en el tenant VariStoreHN dentro de la nueva arquitectura SOLQARYN.

## Promoción futura

El merge `dev -> main` y cualquier actualización productiva constituyen una operación independiente. Requieren autorización nueva, gates exact-head, snapshot/rollback de configuración, revisión de migraciones y certificación de las cuatro URLs definidas en `docs/DEV_CIERRE_TECNICO_PROMOCION.md`. Este documento no autoriza la promoción.
