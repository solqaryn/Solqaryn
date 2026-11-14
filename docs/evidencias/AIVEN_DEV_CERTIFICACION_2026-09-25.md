# Certificación Aiven DEV — 2026-09-25

Estado: **PASS / CERRADO**

## Evidencia

- GitHub Actions run: `36175275439`
- Workflow: `DEV - Certificación canónica Aiven`
- Artifact: `aiven-dev-certification-36175275439`
- Commit del gate: `041e0fbb2e3042b38ffd00c20b0115f73bd58406`

## Resultado no sensible

- Project: `solqaryn`
- Service: `solqaryn-mysql`
- Service type: `mysql`
- Service state: `RUNNING`
- Cuenta/organización contiene: `solqaryn.platform@outlook.com`
- Endpoint DEV coincide con el servicio canónico: sí
- Database efectiva: `solqaryn_dev`
- Usuario MySQL efectivo: `solqaryn_dev_user`
- MySQL: `8.4.8`
- Tablas base: `137`
- Migraciones EF: `107`
- Productos: `8`
- Categorías: `2`
- Secretos expuestos: no
- Producción tocada: no

## Decisión operativa

Aiven DEV queda certificado como recurso canónico de SOLQARYN. No debe crearse otro recurso DEV en Aiven. La base/servicio legacy de la cuenta personal solo puede eliminarse después de auditar por separado la dependencia de PROD legacy.
