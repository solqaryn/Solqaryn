# C11-04.M06.04 — ensayo real de reversión Git en un worktree aislado

**Identidad:** SOLQARYN `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`, Task autora DEV04 ID `6aeacb7820ac8191b7d768a6f33dfa09`; PLAN_MAESTRO fila 1727 y candado V17 2000001727. **R2** porque agrega script y workflow CI (sin cambiar runtime).

## A0: alcance y reversibilidad
Se ensaya una reversión **real de Git en runner efímero**, nunca de la base de datos o de un entorno desplegado. Fuente de regresión C11-04.M05.06 `e8a2663ccec7964a005ca7b73bdebebc0158ddb0`; padre comprobado `91a0485a696feb72d825ccb111afa2703e535fbf`. El diff original contiene exactamente 4 archivos de test/workflow/documentación/CHANGELOG; el script rechaza otro archivo. La entrega nueva agrega solo script, workflow, este documento y entrada autocontenida de CHANGELOG. Revertirla con git revert normal bajo V17 sin reescribir historia.

## A1/A3: experimento reproducible y falsable
GitHub Actions checkout con fetch-depth 0, runner ubuntu y permisos `contents: read`; el script verifica parent exacto, working tree principal limpio y lista explícita de los cuatro paths originales. Crea un `git worktree --detach` temporal con el SHA original, aplica `git revert --no-commit` exclusivamente allí, calcula `git write-tree` del índice revertido y exige que sea idéntico al árbol del parent; exige además que HEAD y working tree primarios permanezcan intactos. Usa trap para eliminar solamente worktree temporal.

Positivo: árbol revertido idéntico al padre y encabezado `ROLLBACK_ISOLATED_PASS=1` en log real. Negativos: parent distinto, ruta extra, conflicto o working tree sucio causan fallo sin alterar Git remoto. Frontera: exactamente 4 archivos. Error: se informa salida != 0 sin convertirla en PASS.

## A2: límite de datos y seguridad
NO se ejecuta rollback MySQL, Aiven, JWT, datos de clientes, proveedor de telemetría, DNS ni despliegue. `MYSQL_RESTORE=NOT_TESTED`, `PROD=NOT_TOUCHED`, SLO/CWV/RTO/RPO/costes `NO_MEDIDO`; el resultado no certifica recuperación de datos legítimos ni continuidad de negocio.

## A4/A5
Solo certificar con job GitHub-hosted `completed/success` del SHA de entrega, log de árbol reverso comprobado, `verify-solqaryn-project-scope` y procedencia V5 exact-SHA SUCCESS, readback PLAN/COLA/DAG/BITACORA. CI externa QUEUED/IN_PROGRESS mantiene VALIDANDO; CANCELLED/SKIPPED/FAIL no son PASS. Revisor A5 **otra Task Javier distinta de DEV04**, no autoaprobación. Sin cambios QA/main/PROD, sin servicios de pago, force-push o borrado de historial.
