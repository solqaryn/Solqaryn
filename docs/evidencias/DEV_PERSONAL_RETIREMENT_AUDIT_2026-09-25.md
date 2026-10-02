# Retiro DEV personal — auditoría previa a destrucción — 2026-09-25

## Estado

**PARCIAL.** Se retiró lo que ya tenía evidencia suficiente y se mantienen bloqueados los destructivos que podrían afectar Producción legacy.

## Vercel

- `identidad-retirada-desarrollo`: eliminado previamente de la cuenta personal.
- Team corporativo `SOLQARYN`: sólo proyecto `solqaryn-dev`.
- `SOLQARYN` PROD legacy: fuera de alcance y no tocado.

## Render

Workspace corporativo visible:

- `SOLQARYN`
- email: `solqaryn.platform@outlook.com`

Servicios visibles:

- `solqaryn-api-dev`
- servicio reservado PROD

DEV histórico conocido por evidencia:

- nombre: `solqaryn-api-desarrollo`
- service ID: `srv-d9jblq7avr4c73c74jng`

El ID histórico no es accesible desde el workspace corporativo. Readback 2026-09-26: el workspace corporativo `SOLQARYN` contiene únicamente `solqaryn-api-dev` (`srv-daqvla49v7es738up9vg`) y `solqaryn-api-prod` (`srv-dapl2j49v7es73907om0`). No se ejecutó una eliminación remota del servicio personal antiguo porque el conector corporativo no administra esa cuenta.

## Aiven

Workflow read-only:

- `DEV - Inventario Aiven para retiro legacy`
- run: `36193976802`
- resultado: `SUCCESS`

Inventario visible para el token corporativo:

- proyecto: `solqaryn`
- servicio: `solqaryn-mysql`
- estado: `RUNNING`
- proyectos legacy visibles: ninguno

Conclusión actual: la cuenta corporativa sólo expone el proyecto `solqaryn` y el servicio `solqaryn-mysql`, pero el endpoint de inventario de bases devolvió HTTP 404 y no permitió enumerar las bases. Además, el servicio corporativo actual `solqaryn-api-prod` (`srv-dapl2j49v7es73907om0`) registra consultas a `defaultdb` hasta el 2026-09-25. Por tanto, `defaultdb` tiene dependencia productiva activa demostrada y queda terminantemente bloqueada cualquier destrucción del Aiven personal o de `defaultdb`/`SOLQARYN_desarrollo` hasta identificar el host/servicio exacto que atiende esa conexión.

## Cloudinary

DEV canónico ya utiliza el cloud `riyrzmob` y prefijo `solqaryn_dev`. El Cloudinary personal legacy no se elimina mientras no exista prueba de cero consumidores de PROD legacy.

## Cloudflare / DNS

Cuenta corporativa:

- `Solqaryn.platform@outlook.com's Account`

Zona:

- `solqaryn.com`
- estado: `pending`

Registros actuales:

- dos TXT `_acme-challenge.solqaryn.com`

No existen aliases DEV legacy en esta zona corporativa.

## GitHub

Readback de permisos:

- `jmejia31`: `admin`
- `morales35alex`: `write`

La autoridad vigente ratificada el 2026-09-25 conserva `jmejia31` como Owner secundario/de recuperación. Por tanto no se elimina como “acceso personal residual” sin una nueva decisión explícita del propietario.

## Producción

`PRODUCTION_TOUCHED=FALSE`.


## Readback 2026-09-26

- GitHub: `jmejia31=admin` se conserva intencionalmente como Owner secundario/de recuperación; no es deuda a retirar.
- Render corporativo: sólo `solqaryn-api-dev` y `solqaryn-api-prod`.
- Aiven inventario run `36193976802`: SUCCESS; sólo proyecto `solqaryn` y servicio `solqaryn-mysql`; listado de databases respondió HTTP 404.
- Render PROD corporativo: evidencia de logs confirma consultas a `defaultdb` hasta 2026-09-25.
- Decisión: NO BORRAR Aiven personal, `defaultdb`, credenciales relacionadas ni recursos potencialmente compartidos hasta identificar el servicio/host exacto de la conexión productiva.
- Producción no fue modificada durante esta verificación.
