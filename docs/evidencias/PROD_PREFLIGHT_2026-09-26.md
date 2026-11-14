# Preflight de PROD — 2026-09-26

Estado: **NO LISTO PARA MERGE/DEPLOY**.

El backup de la base productiva anterior ya está verificado y no constituye bloqueo. El bloqueo actual es de topología/cutover productivo.

## Backup previo

- Run: `36228394479` — SUCCESS.
- Fuente: `defaultdb`.
- 132 tablas base.
- 104 migraciones EF.
- Restore MySQL 8.4 del mismo artifact: PASS.
- Artifact cifrado: `10901905430`.
- SOURCE_WRITES=0.

## GitHub

- PR de promoción: `#3416`, `dev -> main`.
- Los 6 commits exclusivos de `main` fueron reconciliados dentro de `dev` mediante merge commit `d8888651147d06bac68a3fecfbbdba08023ac1d4` sin cambiar el árbol certificado de `dev`.
- El PR pasó de `mergeable=false` a `mergeable=true`.
- No se ha fusionado todavía.

## Render PROD

Readback vivo:

- workspace corporativo: SOLQARYN.
- servicio: `solqaryn-api-prod`.
- service ID: `srv-dapl2j49v7es73907om0`.
- repo: `solqaryn/Solqaryn`.
- branch: `main`.
- `autoDeploy=yes`.
- `autoDeployTrigger=commit`.
- URL: `https://solqaryn-api-prod.onrender.com`.
- health path actual: `/health`.

Logs vivos anteriores al cutover demuestran:

- runtime conectado a `defaultdb`;
- `Database:ApplyMigrationsOnStartup` efectivo: el arranque consulta `__EFMigrationsHistory` y registra `No migrations were applied. The database is already up to date.`;
- `ProductionDataRepair` se ejecuta en el arranque.

Consecuencia: un merge a `main` es también un deploy automático del backend. No debe ejecutarse hasta preparar el destino PROD canónico.

## Aiven PROD canónico

La arquitectura vigente reserva:

- proyecto: `solqaryn`;
- servicio: `solqaryn-mysql`;
- base: `solqaryn_prod`;
- usuario app: `solqaryn_prod_user`.

Se intentó un probe estrictamente read-only desde `dev` usando Environment `PROD`, run `36228744085`. GitHub rechazó el job antes de iniciar steps/logs, por lo que no hubo conexión ni escritura. La protección del Environment PROD impide consumir esas credenciales desde `dev`.

Estado de datos de `solqaryn_prod`: **no certificado todavía desde esta rama**.

## Vercel PROD

Readback corporativo actual:

- team: `SOLQARYN`;
- proyectos visibles: únicamente `solqaryn-dev`;
- proyecto frontend PROD corporativo: **no existe todavía**.

## Cloudinary PROD

El cloud corporativo `riyrzmob` tiene credenciales separadas:

- `Root`: administración/recuperación;
- `solqaryn_dev`: DEV;
- `solqaryn_prod`: reservada para PROD.

La credencial `solqaryn_prod` está documentada como creada pero **aún no conectada al runtime productivo**.

## Decisión técnica actual

No fusionar PR #3416 hasta que:

1. el destino Aiven `solqaryn_prod` esté certificado y preparado con los datos productivos que deban conservarse;
2. Render `solqaryn-api-prod` deje de apuntar a `defaultdb` y use la conexión corporativa `solqaryn_prod`;
3. Cloudinary PROD use la credencial/prefijo productivos corporativos;
4. exista el frontend PROD corporativo en Vercel o se defina explícitamente su cutover;
5. se defina el cambio de tráfico/DNS y rollback.

El sistema productivo anterior permanece intacto durante este preflight.


## Aiven control-plane read-only — 2026-09-26

Workflow temporal `Aiven - PROD topology read-only`, run `36229036129`: **SUCCESS**.

Readback sin secretos:

- proyecto: `solqaryn`;
- servicio: `solqaryn-mysql`;
- tipo: `mysql`;
- estado: `RUNNING`;
- cloud: `do-sfo`;
- plan: `free-1-1gb`;
- host MySQL: `solqaryn-mysql-solqaryn.h.aivencloud.com`;
- puerto MySQL: `14402`;
- usuario `solqaryn_prod_user`: presente;
- usuario `solqaryn_dev_user`: presente;
- usuario `avnadmin`: presente;
- `includeSecrets=false`;
- operaciones write: 0;
- datos productivos consultados: 0.

Esta evidencia certifica la topología y la existencia del usuario PROD, pero no certifica aún el contenido de la base `solqaryn_prod`.
