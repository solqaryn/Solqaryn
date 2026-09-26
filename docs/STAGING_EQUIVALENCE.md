# Equivalencia DEV / PROD — SOLQARYN

## Regla

DEV y PROD deben compartir arquitectura y nombres de configuración. Sólo cambian valores propios del entorno.

| Superficie | DEV | PROD | Estado |
|---|---|---|---|
| Git | `dev` | `main` | esperado |
| Render | `solqaryn-api-dev` | `solqaryn-api-prod` | equivalente |
| Health plataforma | `/health` | `/health` | equivalente en configuración versionada |
| Aiven DB | `solqaryn_dev` | `solqaryn_prod` | aislado por entorno |
| Cloudinary prefijo | `solqaryn_dev` | `solqaryn_prod` | aislado por entorno |
| SMTP | Outlook OAuth2 | Outlook OAuth2 | mismo contrato; token distinto |
| Variables Render | 28 claves canónicas | 28 claves canónicas | paridad requerida |
| Vercel | `solqaryn-dev` | pendiente `solqaryn-prod` | **gap PROD** |
| Cloudflare custom DNS | N/A para DEV | pendiente tras Vercel PROD | **gap PROD** |

## Criterio de cierre

No se declara equivalencia total hasta:

- DEV certificado funcionalmente;
- frontend PROD corporativo creado;
- variables/URLs PROD alineadas;
- SMTP DEV y PROD probado realmente;
- Cloudinary PROD validado;
- DNS PROD activado si corresponde;
- smoke PROD aprobado;
- migración histórica todavía fuera de alcance hasta ese punto.
