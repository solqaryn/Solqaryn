# C10-04.M06.04 — Ensayo reversible de rollback sin pérdida de datos

**Acción atómica PLAN D:** “Ensayar rollback o desactivación sin borrar datos legítimos”. **PLAN E/M06** describe una meta acumulativa de fase, no una licencia para afirmar recuperación integral o datos productivos. El alcance es **R2 de prueba de persistencia**, exclusivamente MySQL **8.4 efímero de GitHub-hosted**; sin Aiven, Cloudflare R2, API productiva, pagos, JWT real, workers desplegados, datos comerciales o QA/main/PROD.

## A0: impacto y arquitectura

- Archivo de ensayo: `scripts/quality/verify-c10-04-m06-04-rollback.sh`. Workflow: `.github/workflows/v21-c10-04-m06-04-rollback.yml`. No cambia Domain/Application/Infrastructure/API/Angular, contratos, migraciones, permisos, secretos reales ni datos persistentes.
- Fuente única: STANDARD V5 Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; `PLAN_MAESTRO` fila 1583, H/I/COLA/DAG bajo candado V17 `SQV17_C10_04_M06_04` (`2000001583`). Una EN_PROGRESO propia; preservar owner histórico.
- Prueba diseñable para fallar: no se considera PASS un job queued/in_progress/skipped/cancelled o una simulación en memoria. Se verifica el resultado real de un servidor InnoDB en contenedor MySQL 8.4 temporal.

## A1 y A2: asserts positivos, negativos y fronteras

1. Insertar **4 filas** legítimas, dos por tenant 101/202, con versión 7; tomar digest SHA-256 ordenado de los datos.
2. En una transacción real, modificar estado y desactivar una fila del tenant A y borrar *tentativamente* una fila de B; constatar **3 filas/2 habilitadas antes del rollback** y luego exigir **4 filas** y **digest original** después de `ROLLBACK`.
3. Intentar la mutación cruzada de `doc-c` (B) con filtro de tenant A: `ROW_COUNT()=0`; hash original intacto.
4. Desactivar con CAS una funcionalidad en una fila A: exactamente una actualización, 4 filas preservadas, B intacto; un replay obsoleto con versión 7 debe dar 0; reactivar condicionalmente con versión 8 y restituir el hash íntegro original.
5. Aserciones `set -Eeuo pipefail` fallan si cualquier cuenta, versión, hash o contrato cambia. `trap` elimina únicamente la base efímera creada por esta ejecución.

## A3: integración real acotada

`ubuntu-latest` en GitHub Actions inicia un **servicio MySQL 8.4 real**; Docker `exec` ejecuta SQL de InnoDB con `START TRANSACTION / ROLLBACK`, UPDATE, DELETE controlado, reactivación y prueba adversarial A/B. No presenta un mock como servidor real. No hay integración EF Core ni restauración de datos reales: **NO_CERTIFICADO** en esos niveles hasta otra microtarea R2 con evidencia propia.

## A4 y A5

- Validar `PROJECT_SCOPE_LOCK=STRICT`, procedencia `CHANGELOG_AI.md`, el check dirigido y los checks requeridos exact-SHA en estado `completed/success`; conservar `VALIDANDO` mientras corra CI externa.
- Mantener BITACORA idempotente, estado/owner y readback PLAN/COLA/DAG; no forzar fórmulas. **A5 por otra Task Javier** distinta de DEV01, con revisión genuina del código, logs y tests.
- Rollback de código si falla: `git revert` normal sobre HEAD vigente, sin force-push ni borrar BITACORA. Cualquier aprobación R3 o certificación productiva queda fuera de este ensayo.
