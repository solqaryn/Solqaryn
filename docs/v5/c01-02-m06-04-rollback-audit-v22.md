# C01-02.M06.04 — Ensayo de reversión sin destruir datos legítimos (V22)

**Requisito PLAN!D215:** «Ensayar rollback o desactivación sin borrar datos legítimos». Fecha local 2026-11-20. Autor DEV06, SOLQARYN `dev` bajo PROJECT_SCOPE_LOCK STRICT. **Riesgo R2 de CI/reversión**, con ejecución **exclusivamente en runner efímero GitHub-hosted** y pruebas negativas de alcance. La meta `E215` «Completa M06» es acumulativa; aquí no se certifica rollback de despliegue, base de datos productiva ni fase M06. No QA/main/PROD, no secretos/pagos y sin `git push`, `reset` ni reescritura de historia compartida.

## A0: Ámbito y activos preservados

La unidad ensaya una reversión de Git concreta del parche **test-only** de `C01-02.M05.06`, SHA `844d7fead192cf0f661ac8ae6f1b934860eca6ab`. Los únicos tres archivos del commit son `CHANGELOG_AI.md`, `docs/v5/c01-02-m05-06-regression-performance-a11y-v22.md`, `frontend/e2e/c01-02-m05-06-regression-a11y.spec.ts`. No hay migraciones, tablas, datos de clientes, tokens, código de producción o cambios en ruta de red. Un revert a ese SHA en su propio checkout efímero puede contrastarse matemáticamente con los blobs del padre; el branch vivo `dev` permanece inalterado. No se ejecuta transacción SQL alguna, por lo cual los datos legítimos no están expuestos a escrituras en este **ensayo de rollback de Git**. Este resultado **NO acredita** rollback transaccional de MySQL de negocio, datos del proveedor ni reversión de producción.

Archivos nuevos de esta unidad: `scripts/quality/verify-c0102-m0604-rollback.mjs`, suite `.test.mjs`, workflow `.github/workflows/v22-c01-02-m06-04-rollback.yml` y este documento; cabecera aditiva íntegra `CHANGELOG_AI.md`. Sin modificar runtime .NET/Angular, proveedores ni datos. Dependientes: revisores M06 y automatizaciones dev. El workflow `concurrency` está aislado por `github.sha` y `cancel-in-progress:false`.

## A1: Protocolo real, positivos, negativos, frontera, errores

1. GitHub-hosted `ubuntu-latest`, `actions/checkout@v4` `fetch-depth:0` y `persist-credentials:false`; Node24. Se exige `CI=true` y `GITHUB_SHA == git rev-parse HEAD`. El script rechaza árbol original sucio y ausencia de fuente.
2. Crea **worktree detached temporal** con `git worktree add --detach <tmp> 844d7fea...` (no rama), corre **`git revert --no-commit`** del commit desde su propio árbol de origen. No escribe al branch `dev` ni a remoto.
3. Verifica **exactamente tres rutas staging** de la allowlist (mismo conjunto, sin duplicados), ninguna migración, secreto, infraestructura, archivo real de cliente ni datos. Para **cada una** exige `git rev-parse :<ruta>` igual al **blob Git del padre** del SHA revertido. Esto verifica reversión real y recuperabilidad de los tres artefactos a estado previo.
4. Ejecuta negativos mediante `node --test`: rechazar migración añadida, ruta `../secrets`, duplicado, lista vacía, ruta extra, path inexistente. Requiere **todos los tests PASS, 0 FAIL/0 SKIP**.
5. Elimina **solo** el worktree efímero, compara HEAD principal antes/después y exige `git status --porcelain` vacío. El árbol compartido y sus historiales no se alteran; un resultado incorrecto produce exit code distinto de 0.

**Negativos no acreditados:** la suite no ejecuta SQL ni despliegues, no prueba reversibilidad de migraciones productivas, contratos comerciales futuros, rollback en dispositivos ni recuperación ante corrupción de datos. Para esas superficies R2/R3 se requiere un ensayo específico autorizado. No se finge evidencia.

## A2/A3 y límite de seguridad

El test se ejecuta únicamente sobre Git temporal (integración real de Git checkout/revert/index/blobs; no mocks), con comparación de paths que previene que se incluyan SQL, secretos o datos. El workflow no obtiene credenciales de escritura (`contents:read` y `persist-credentials:false`). No hay auth JWT ni endpoint HTTP cambiado; **A2/A3 de tales sistemas son NO_APLICA al diff**. El estado real del negocio, los datos persistidos de MySQL y un rollback de PROD quedan fuera del alcance y NO_MEDIDO.

## A4-A5, readback y plan de recuperación

Workflow dedicado `V22 C01-02.M06.04 - isolated reversible Git rollback` se ejecuta automáticamente tras publicar en `dev` por CAS fast-forward, y **solo** su job `C01-02.M06.04 - ephemeral git revert and safety negatives` en `completed/success` con SHA exacto, más `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` SUCCESS, habilitan A0–A4. `SKIPPED/CANCELLED/IN_PROGRESS/FAIL` requeridos no son PASS. Registrar PLAN/COLA/BITACORA event IDs y readback por transición y preservar autor DEV06. A5 **independiente por otra Task Javier distinta de DEV06**, leyendo el código, los negativos y la limitación de no usar datos reales. Si CI sigue corriendo, dejar VALIDANDO con SHA/RUN en handoff. Revertir solo con un commit `git revert` normal en dev si se requiere revertir el changeset propio; nunca force-push.
