# Contrato GitHub Environments — SOLQARYN

Estado canónico para los GitHub Environments `DEV`, `QA` y `PROD`. Este contrato evita copiar secretos o variables entre entornos sin un consumidor real y aplica mínimo privilegio.

## Reglas comunes

- Flujo único de promoción: `dev -> qa -> main`.
- Cada Environment permite despliegues únicamente desde su rama canónica.
- No se comparten contraseñas de base de datos entre entornos.
- Las variables no sensibles de MySQL son exactamente cuatro por entorno: host, port, database y user.
- Un Environment no debe almacenar secretos o variables “por simetría” si ningún workflow activo los consume.
- Required reviewers y wait timer permanecen desactivados salvo decisión posterior explícita; la restricción obligatoria es la rama de despliegue.
- QA y PROD no permiten bypass administrativo de su regla de rama como estado canónico.
- Los valores sensibles nunca se documentan en el repositorio.

## DEV

Rama de deployment permitida: `dev`.

Variables:

- `SOLQARYN_DEV_DB_HOST`
- `SOLQARYN_DEV_DB_PORT`
- `SOLQARYN_DEV_DB_NAME=solqaryn_dev`
- `SOLQARYN_DEV_DB_USER=solqaryn_dev_user`

Secretos consumidos por workflows DEV:

- `SOLQARYN_DEV_DB_PASSWORD`
- `SOLQARYN_AIVEN_TOKEN` cuando el workflow de control-plane Aiven lo requiere.
- `SOLQARYN_DEV_BACKUP_PASSPHRASE` cuando el workflow operativo de backup DEV lo requiere.

## QA

Rama de deployment permitida: `qa`.

Variables canónicas, y sólo estas cuatro:

- `SOLQARYN_QA_DB_HOST`
- `SOLQARYN_QA_DB_PORT`
- `SOLQARYN_QA_DB_NAME=solqaryn_qa`
- `SOLQARYN_QA_DB_USER=solqaryn_qa_user`

Secretos canónicos de QA:

- `SOLQARYN_QA_DB_PASSWORD` — credencial de aplicación para `solqaryn_qa_user`.
- `SOLQARYN_AIVEN_TOKEN` — token de control-plane Aiven reservado a workflows GitHub Actions QA; nunca se expone al runtime Render.
- `SOLQARYN_QA_BACKUP_PASSPHRASE` — passphrase de cifrado reservada a backup/restore QA.

No son variables del Environment QA:

- `SOLQARYN_QA_FRONTEND_URL`
- `SOLQARYN_QA_BACKEND_URL`

Los endpoints QA son identidades canónicas declaradas en los workflows y bindings del repositorio, no configuración mutable del Environment.

Los tres secretos QA permanecen segregados en el Environment `QA`. El token Aiven y la passphrase de backup son secretos de automatización/control-plane y no forman parte de la configuración del backend desplegado. Los workflows sólo deben leerlos cuando una certificación o backup QA aprobado los requiera.

## PROD

Rama de deployment permitida: `main`.

Variables:

- `SOLQARYN_PROD_DB_HOST`
- `SOLQARYN_PROD_DB_PORT`
- `SOLQARYN_PROD_DB_NAME=solqaryn_prod`
- `SOLQARYN_PROD_DB_USER=solqaryn_prod_user`

El password de aplicación es `SOLQARYN_PROD_DB_PASSWORD`. Otros secretos productivos sólo permanecen si un workflow PROD autorizado los consume; no se usan para justificar secretos extra en QA.

## Aiven — contrato de mínimo privilegio

Los usuarios de aplicación deben conservar únicamente:

- `solqaryn_dev_user`: `USAGE ON *.*` + `ALL PRIVILEGES ON solqaryn_dev.*`.
- `solqaryn_qa_user`: `USAGE ON *.*` + `ALL PRIVILEGES ON solqaryn_qa.*`.
- `solqaryn_prod_user`: `USAGE ON *.*` + `ALL PRIVILEGES ON solqaryn_prod.*`.

No se permiten grants de datos cruzados, `WITH GRANT OPTION`, `ROLE_ADMIN` ni `REPLICATION_APPLIER` en usuarios de aplicación.

La aplicación refuerza la misma frontera con `EnvironmentDatabaseGuard`: Development sólo acepta DEV, Staging sólo QA y Production sólo PROD.
