# C02-04.M06.04 — ensayo de reversión aislada sin pérdida de datos

**Acción PLAN D431:** «Ensayar rollback o desactivación sin borrar datos legítimos». Proyecto SOLQARYN, únicamente `dev`, `PROJECT_SCOPE_LOCK=STRICT`, riesgo R2 porque afecta validación CI y reversión de evidencia QA. El ensayo de esta unidad se limita al changeset del laboratorio `C02-04.M05.06` y **no** revierte ni modifica una rama compartida.

## Alcance y método reproducible

- Commit bajo ensayo: [`d64b19a4`](https://github.com/solqaryn/Solqaryn/commit/d64b19a40983ea3e59d21c5de00652a8635e5daf), que incorpora laboratorio GitHub-hosted JWT firmado + MySQL 8.4 efímero y preserva ocho pruebas Chromium. Cuatro archivos exactos: workflow, `CHANGELOG_AI.md`, test C0204M0502 y documentación M05.06. **No** contiene migraciones, tablas comerciales, datos de clientes ni cambios de código productivo.
- Script `scripts/quality/verify-c0204-m0604-git-revert.sh`: rechaza repositorio/rama no autorizados, argumento no admitido, SHA no ancestro, diff fuera de los cuatro archivos y reversión que cambie un archivo distinto. Abre un `git worktree` temporal **detached** en el SHA fijo, ejecuta `git revert --no-commit` allí y exige que el árbol del índice resultante sea idéntico al árbol del padre del commit original; comprueba que el HEAD del worktree no se mueve.
- Workflow `.github/workflows/v24-c02-04-m06-04-rollback.yml`: permisos `contents: read`, `fetch-depth: 0`, runner GitHub-hosted, `cancel-in-progress: false`, grupo por `github.sha`, validación Scope Lock antes de ejecutar, prueba de guardas negativas y ensayo positivo. No requiere tokens, conexiones externas ni secretos. No crea PR o rama remota, no fuerza pushes, no hace `git reset`, no toca Aiven/Render/Vercel, QA/main/PROD ni tablas SQL.
- Resultado verificable exigido: job `C02-04.M06.04 - detached no-data-loss rollback proof` `completed/success` en el **SHA exacto** y línea `C0204_M0604_ROLLBACK_REHEARSAL=PASS` con `CHANGED_FILES=4`, `BUSINESS_DATA_WRITES=0`, `REMOTE_BRANCH_WRITES=0`; los controles CI, Scope Lock y procedencia V5 también deben completar SUCCESS cuando resulten requeridos. `QUEUED`, `IN_PROGRESS`, `SKIPPED` o `CANCELLED` no son PASS.

## Matriz A0–A5 y fronteras

- **A0:** cambio de 3 archivos: script fail-closed, job y este documento. El diff no altera frontend Angular, backend ASP.NET Core, infraestructura de producción, datos ni permisos. La ruta de retorno es borrar/revertir estos tres archivos con `git revert` normal del nuevo commit, después de revisar la historia.
- **A1:** positivos de Git reales en runner: tree del revert idéntico al padre y exactamente cuatro paths. Negativos de scope/argumentos, SHA fuera de descendencia, path inesperado, HEAD alterado, escritura no indexada o árbol diferente hacen fallar el script (no se simula PASS).
- **A2:** la comprobación de aislamiento es **del código que se revierte**, no una revalidación JWT/RBAC/tenant A/B. No cambiar endpoints ni grants significa que A2 de ejecución autenticada es `NO_APLICA` al diff; las pruebas históricas signed JWT/MySQL de M05.06 mantienen su propia evidencia separada.
- **A3:** `git revert --no-commit` sobre un worktree efímero es integración real de Git, con restauración exacta de árbol. **No prueba** rollback de migraciones Oracle EF/MySQL, restauración de backup real ni rollback en producción; estos efectos son `NO_APLICA` a un changeset exclusivamente de prueba y documentación. No ocultar esas limitaciones.
- **A4:** requerir SHA exacto, V17/CAS en `PLAN_MAESTRO!H/I`, `BITACORA` append-only, CI requerida SUCCESS y blob equivalencia de los tres archivos. No certificar mientras CI esté pendiente.
- **A5:** revisión independiente por una de las otras cinco Tasks Javier, distinta de autor DEV05, revisando script, logs, árbol probado y límites. `LISTO` no significa A5_PASS ni autorización QA/main/PROD.

`SKREF-06` Humanizer se consultó en origen `blader/humanizer`, pin `9862685f575c65a8247f90369951df1b3416e3d6`, ruta `SKILL.md`, para mejorar claridad sin inventar evidencias. `SKREF-03` Impeccable y `SKREF-10` Angular son `NO_APLICA`: no se modifica interfaz. `SKREF-11` threat model y `SKREF-12` .NET performance son `NO_APLICA`: no se altera frontera autenticada ni se realizan mediciones de rendimiento.

**Rollback de esta unidad:** revertir el commit que añade script/workflow/documento con commit adicional normal en `dev` sólo bajo V17 y nueva autorización de scope; nunca ejecutar el ensayo contra ramas reales, borrar historia o datos legítimos. A5 pendiente hasta revisión externa genuina.
