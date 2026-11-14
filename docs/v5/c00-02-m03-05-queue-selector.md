# SOLQARYN V5 - C00-02.M03.05, cola de seleccion aislada

Codigo real Application .NET 10 en backend/src/Application/AutomationV5/V5WorkItemQueueSelector.cs, con xUnit en backend/tests/Solqaryn.Tests/Application/V5WorkItemQueueSelectorTests.cs. Reutiliza Domain V5WorkItemRules y el planner Application existente.

La cola selecciona por fila de PLAN_MAESTRO a partir de snapshots institucionales confiables, catalogo cerrado, allowlist Javier, gates G/O/DAG, HEAD exacto, areas de conflicto y capacidad de lote. Retorna una intencion inmutable y no modifica ownership, estados ni datos. Los filtros rechazan duplicados, SHA obsoleto, areas desconocidas, actor externo, items ocupados y puertas cerradas.

No implementar eventos aun: no existe despacho real de Scheduled Tasks, webhook, worker persistente, envio entre Tasks, acceso a MySQL/Sheets, claims distribuidos, tenant context otorgado ni API publica. El consumidor futuro debe aplicar JWT/RBAC/tenant server-side, permisos actuales, CAS institucional exclusivo, readback y A0-A5; la intencion no prueba que la microtarea haya sido ejecutada.

Tipo 2: requiere_dotnet=SI y requiere_security=SI, ejecutando build/test y casos positivos-negativos en GitHub Actions con SHA causal. requiere_scope_lock=SI. requiere_node_angular=NO_APLICA, requiere_mysql=NO_APLICA, requiere_docker=NO_APLICA, requiere_e2e=NO_APLICA porque no cambia UI, base, contenedor ni rutas. Revert normal en dev como rollback, sin force-push; A5 por otra Task Javier.
