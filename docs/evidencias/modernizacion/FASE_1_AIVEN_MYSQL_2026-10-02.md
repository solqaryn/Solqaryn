# Modernización — Fase 1 Aiven / MySQL DEV — 2026-10-02

Estado: **PASS / CERRADA SIN CAMBIO DE VERSIÓN**

## Alcance

- Repositorio: `solqaryn/Solqaryn`
- Rama: `dev`
- Aiven project: `solqaryn`
- Aiven service: `solqaryn-mysql`
- Base DEV: `solqaryn_dev`
- Usuario DEV: `solqaryn_dev_user`
- Objetivo evaluado: MySQL `8.4.11`
- QA/main/PROD: fuera de alcance y no tocados

## Corrección de topología y seguridad

SOLQARYN no tiene tres servicios MySQL Aiven. La topología canónica vigente es **un solo servicio** `solqaryn-mysql` que aloja tres bases y tres usuarios aislados: `solqaryn_dev`, `solqaryn_qa` y `solqaryn_prod`.

Por tanto, Maintenance es una propiedad del **servicio compartido**. Un `maintenance-start` no puede considerarse una actualización “DEV primero” a nivel de motor: puede reciclar/actualizar los nodos que sirven también QA y PROD. El gate queda endurecido para **no iniciar mantenimiento automáticamente**. Si `8.4.11` aparece disponible, el workflow falla cerrado y exige autorización productiva explícita vigente o una topología con servicios separados antes de ejecutar el cambio.

## Evidencia causal

- Workflow: `Modernización - Fase 1 Aiven MySQL 8.4.11 DEV`
- Run final exact-head tras el hardening del servicio compartido: [37067448903](https://github.com/solqaryn/Solqaryn/actions/runs/37067448903)
- HEAD evaluado: `ffe3ac936cbcdaa65e513042b1efa9242805b28a`
- Resultado del job: `SUCCESS`
- Backup lógico cifrado previo: `modernization-phase1-aiven-prebackup-37067448903`
- En el mismo HEAD, el baseline de Fase 0 [37067448864](https://github.com/solqaryn/Solqaryn/actions/runs/37067448864) terminó `success` en los siete jobs.

La certificación previa [37066644421](https://github.com/solqaryn/Solqaryn/actions/runs/37066644421), sobre `d9b7af721dde5decf0411f055a6524637b9f46b9`, también pasó el gate de disponibilidad/no-cambio. El run `37067448903` es la última revalidación histórica antes de cerrar Fase 1: su único job pasó y conservó el comportamiento fail-closed de no ejecutar maintenance.

## Disponibilidad Aiven observada

El control-plane del servicio DEV devolvió:

- service type: `mysql`
- service state: `RUNNING`
- versión 8.4.x expuesta en metadata: `8.4.8`
- target `8.4.11` en metadata de mantenimiento: `false`
- target `8.4.11` en cualquier metadata del servicio: `false`

Por la regla fail-closed de esta fase, no se ejecutó `maintenance-start`.

## Readback MySQL antes/después

- Antes: `8.4.8`
- Después: `8.4.8`
- Acción: `not_available_no_change`
- Tablas base: `137`
- Historial EF: `109`
- Productos: `8`
- Categorías: `2`
- Conteos críticos preservados: **PASS**
- Secretos expuestos: `0`
- Producción tocada: `false`

## Recovery del backup pre-cambio

La primera ejecución, run `37066495962`, falló de forma segura antes de consultar/aplicar mantenimiento porque `mysqldump` intentó `FLUSH TABLES` con el usuario DEV de mínimo privilegio.

Se corrigió causalmente sin ampliar grants, reutilizando el contrato ya probado de mínimo privilegio:

`--single-transaction --skip-lock-tables --no-tablespaces --set-gtid-purged=OFF`

La segunda ejecución completó el backup cifrado y todos los gates.

## Dictamen

`FASE_1_AIVEN_MYSQL=PASS_NO_CHANGE_TARGET_NOT_AVAILABLE`

A la fecha de esta certificación, el servicio Aiven DEV de SOLQARYN no ofrece MySQL `8.4.11` como mantenimiento aplicable. Por tanto, conforme a la decisión del propietario, **se conserva MySQL 8.4.8 y no se fuerza ningún cambio**.

Esta fase no autoriza ni implica cambios en QA o PROD.
