# C14-04.M06.04 — Ensayo real y acotado de reversión Git en entorno efímero

**Fecha:** 2026-11-18. **Entorno:** dev; `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo:** R2, integridad y reversión. El único cambio añade un script y un job de GitHub Actions que operan sobre un **worktree desechable** sin escribir a ramas, bases de datos, secretos, API, DNS ni producción.

## A0. Requisitos, alcance y decisión

Esta microtarea exige "ensayar rollback o desactivación sin borrar datos legítimos". Se ejecuta una **reversión Git real** de la entrega de regresión de readiness [M05.06](https://github.com/solqaryn/Solqaryn/commit/963fa8c4e08300238f1b818fa7b33cb20ab1346c), no una simple simulación de éxito. La reversión se intenta contra el SHA de CI en un worktree aislado; el commit M05.06 sólo añadió workflow, test, documento y sección CHANGELOG. El objetivo reproducible es garantizar que se revierten exclusivamente esos cuatro archivos y no se altera código aplicativo protegido.

**Límite imprescindible:** el ejercicio NO restaura una base MySQL ni datos productivos, no prueba RPO/RTO, no demuestra reversión de migraciones y NO autoriza un go-live R3. Los datos legítimos de usuarios reales permanecen fuera del entorno; un marcador sintético sin datos clientes sirve únicamente como comprobación limitada de no eliminación de contenido no rastreado.

## A1. Controles positivos, negativos y frontera realmente falsables

- Validar que el SHA objetivo es ancestro del HEAD verificado por GitHub y que el diff objetivo contiene **exactamente** cuatro rutas permitidas. Si cambia la lista, detener antes de revertir.
- Crear un `git worktree add --detach` temporal sobre el SHA de la ejecución. Ejecutar `git revert --no-commit --no-edit` del SHA M05.06; exigir ausencia del spec, workflow y documento añadidos por aquel commit. Si la reversión encuentra un conflicto **únicamente en CHANGELOG_AI.md**, restaurar la versión exacta del SHA probado y quitar sólo el bloque literal original de M05.06, validando encabezado, dos viñetas y unicidad; cualquier otro conflicto, duplicación o deriva de ese bloque debe fallar cerrado. Conservar todas las entradas posteriores sin intercalado.
- Verificar con `git diff --name-only HEAD` que sólo se alteraron esos cuatro archivos, y con `git diff --check HEAD` que no aparecen defectos de formato. El script no efectúa `git push`.
- **Control adversarial:** modificar un archivo real rastreado de `backend/src` o `frontend/src` dentro del sandbox; la función fail-closed de protección debe devolver error. Luego restaurar solamente ese archivo de prueba dentro del sandbox y volver a exigir protección. El test debe FALLAR si la mutación indebida pasa inadvertida.
- Guardar un marcador sintético local fuera de Git, verificar SHA-256 antes y después; es sólo una frontera de preservación de un archivo temporal, no un DB rollback. No falsificar datos clientes ni acceder a proveedores pagos.
- El worktree se elimina con `git worktree remove --force` al salir; es limpieza local del sandbox, NO borrado histórico ni reescritura de ramas.

La reparación V20 detectó un conflicto Git real entre la sección M05.06 y entradas posteriores de CHANGELOG. El script recupera únicamente esa sección dentro del worktree efímero y exige `git diff` de las cuatro rutas exactas, sin suavizar la protección sobre archivos de aplicación. La prueba dirigida y los controles por SHA deben acreditarse en GitHub Actions: este documento no anticipa PASS.

## A2. Protección por diseño

La reversión se rechaza si difiere el target o aparecen cambios bajo `backend/src`, `frontend/src`, `backend/scripts` o `scripts/security`. No se toca API, tenant, RBAC, JWT, EF/MySQL ni el árbol vivo de dev. Las pruebas de aislamiento A/B, revocación, restauración y recuperación real **siguen SIN_VERIFICAR** para un eventual despliegue. Un éxito aquí significa exclusivamente `REAL_GIT_REVERT_EPHEMERAL=PASS`.

## A3. Integración real aplicable

La CI usa Ubuntu GitHub-hosted con checkout `fetch-depth:0`, ejecuta Git real y verifica cambios de rutas sobre el repositorio SOLQARYN. Son operaciones reales de Git con contenidos del SHA causal, no una función mock. MySQL 8.4, navegador, JWT HTTP y datos productivos son `NO_APLICA` al diff de script/workflow de reversión Git y **NO_CERTIFICADOS** para la liberación. `CANCELLED/SKIPPED/QUEUED/IN_PROGRESS` jamás se informarán como PASS.

## A4/A5. Evidencia, gates y recuperación

Requerir éxito real del job `C14-04.M06.04 - real git revert isolated and adverse guard` sobre SHA exacto y controles Scope Lock/V5 pertinentes, no un resultado asumido. Registrar SHA, job, logs, cambios, BITACORA A:Z y readback. Si la reversión causa conflicto, conservar VALIDANDO y diagnosticar el punto exacto; no permitir borrado de datos. Después de A0–A4, cerrar sólo el scope DEV de ensayo R2 con CAS V17; A5 la realiza otra Task Javier independiente de DEV06. La transición a QA/main/PROD o rollback productivo permanece **NO_GO R3** sin autorización nueva.

**Rollback de esta entrega técnica:** `git revert` normal del commit que añade este script, workflow y documento, después de verificar HEAD exacto; sin force-push. BITACORA histórica se conserva.
