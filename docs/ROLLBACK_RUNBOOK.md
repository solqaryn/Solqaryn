# ROLLBACK RUNBOOK — SOLQARYN

## DEV

Ámbito permitido:

- GitHub `dev`;
- Render `solqaryn-api-dev`;
- Vercel `solqaryn-dev`;
- Aiven `solqaryn_dev`;
- Cloudinary prefijo `solqaryn_dev`.

Secuencia:

1. identificar el último commit/deploy DEV certificado;
2. verificar que el rollback no toca `main` ni recursos PROD;
3. restaurar código/configuración DEV al estado certificado;
4. comprobar `/health` y `/health/ready`;
5. ejecutar pruebas dirigidas del cambio;
6. registrar causa, SHA y resultado en `CHANGELOG_AI.md`.

No usar infraestructura personal o legacy como rollback.

## PROD

Un rollback productivo requiere autorización explícita vigente del propietario. Debe conservar datos, tenancy y secretos; no se sustituye PROD por recursos legacy.

## Base histórica

El respaldo histórico de VariStoreHN es fuente futura de migración, no mecanismo de rollback de infraestructura.
