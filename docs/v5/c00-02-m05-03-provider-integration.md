# C00-02.M05.03 — Integración de proveedor y selector V5

La única superficie de provider hoy aplicable al ciclo V5 es `V5WorkItemMemoryAdapter`, un adaptador de prueba *en memoria*, no una implementación de Sheets/MySQL o un lease distribuido. Se integra el resultado de `V5WorkItemQueueSelector` con las transiciones del adaptador en `backend/tests/Solqaryn.Tests/Application/V5C0002M0503ProviderIntegrationTests.cs`.

Se prueban de manera ejecutable: preparación de intención sin escritura, claim único y replay idempotente; rechazo de otro Task_ID autorizado, imposible mutación VALIDANDO ajena; bloqueo por área activa, HEAD obsoleto y DAG cerrado con provider intacto. No se autoriza inventar un CAS institucional a partir de estas pruebas. No se modifica ninguna fuente de estados, esquema ni migración.

Clasificación: requiere_dotnet=SI, requiere_security=SI, requiere_scope_lock=SI. NO_APLICA: requiere_mysql/docker/node_angular/e2e (no SQL provider, frontend ni HTTP en el diff). El backend xUnit, seguridad multiempresa, CHANGELOG provenance y Scope Lock del SHA publicado deben aprobar antes del LISTO_DEV. A5 debe ser independiente de la autora; no implica QA/main/PROD. Reversión normal mediante `git revert`, sin afectar datos o credenciales.
