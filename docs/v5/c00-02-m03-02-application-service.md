# SOLQARYN V5 — C00-02.M03.02: caso de uso de transiciones sin efectos externos

**Scope:** `C00-02.M03.02` (TIPO 2, backend Application .NET 10), autor PRIMARY DEV 01 — JAVIER (`6adbb9fff1548191a1bd5b30d9e9984a`). Se implementa código real en `backend/src/Application/AutomationV5/V5WorkItemTransitionPlanner.cs` y pruebas xUnit dirigidas. Reutiliza `V5WorkItemRules` del Domain introducido por M03.01, sin escribir en archivos de otra Task.

## Contrato ejecutable y límites

`PrepareClaim`, `PrepareValidating` y `PrepareCloseDev` reciben un snapshot confirmado de PLAN_MAESTRO H/I, gates de COLA G/O y DAG, catálogo V5 y allowlist real; emiten un `TransitionIntent` inmutable con `ExpectedState`, `ExpectedOwnerTaskId`, `RequestedState`, `RequestedOwnerTaskId` y fingerprint causal. Las denegaciones de Domain o actor no autorizado **nunca** emiten una intención de escritura. `CheckIndependentA5` prohíbe autorrevisión y actores fuera de allowlist.

**NO es un despachador ni CAS de Sheets.** El consumidor que eventualmente ejecute una intención debe comprobar permisos reales, validar que el snapshot no cambió y aplicar comparación/actualización atómica en su propio adaptador; si no demuestra exclusividad, debe rechazarla. Esta unidad no escribe en PLAN/COLA/BITACORA ni cambia su esquema, no envía eventos a Tasks, no crea endpoints, DB, workers, migraciones, roles, JWT o flags y no convierte un check verde en cierre institucional por sí mismo.

## Pruebas y riesgos dirigidos

Pruebas `V5WorkItemTransitionPlannerTests`: caso positivo de claim y expectativas de versión, rechazo de ID/actor/gates/owner ajenos, no mutación del snapshot, validación con fingerprint, SHA de 40 hex y A0–A4/A5_PENDIENTE requeridos, y A5 de revisor Javier independiente. La igualdad de dos propuestas idénticas no reemplaza la idempotencia del almacén, ni acredita CAS, tenancy HTTP o integración MySQL.

**Clasificación:** requiere_dotnet=SI, requiere_node_angular=NO, requiere_mysql=NO, requiere_docker=NO, requiere_e2e=NO, requiere_security=SI (integridad de actor/owner/A5), requiere_scope_lock=SI. A0 diff y control de secretos; A1 positivos/negativos xUnit; A2 invariantes de allowlist e independencia sin alterar JWT/RBAC real; A3 compilación/regresiones .NET en GitHub Actions SHA exacto; A4 readback GitHub/PLAN/COLA/BITACORA y checks requeridos. No declarar PASS para jobs pendientes o skipped.

**Integración futura:** M03.03 seguridad real, M03.04 adaptador/persistencia CAS y M03.05 worker quedan bajo sus propias puertas y revisiones, no se implementan ni certifican aquí. No se concede acceso a tenant A/B. Rollback: `git revert` normal de commit aditivo tras revalidar HEAD, sin force-push ni borrar eventos históricos.
