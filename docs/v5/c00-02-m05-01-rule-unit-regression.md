# SOLQARYN V5 — C00-02.M05.01: regresión unitaria reproducible

**Resultado exigido:** ejecutar una prueba unitaria o de regla con resultado reproducible, para el ciclo de estados y autorías V5. Se añade `backend/tests/Solqaryn.Tests/Domain/V5C0002M0501LifecycleRegressionTests.cs`, que ejercita las reglas de dominio existentes, sin crear un despachador, servicio, API ni mutación de Sheets.

**Casos positivos y negativos:** las seis Task IDs autorizadas de Javier pueden proponerse como actor de una microtarea PENDIENTE sin dueño solo cuando G/O/DAG están abiertos; una Task retirada no es elegible. Se rechazan duplicados y estados no PENDIENTE, cada puerta cerrada por separado, SHA incompleto y A0–A4/A5_PENDIENTE faltantes. A5 rechaza autorrevisión y Task externa. Los identificadores son fixture aislado, no autorización operativa ni hardcode de un servicio.

**Matriz técnica:** requiere_dotnet=SI (xUnit real en GitHub Actions); requiere_node_angular=NO; requiere_mysql=NO; requiere_docker=NO; requiere_e2e=NO; requiere_security=SI (identidad/autoría/denegaciones de regla); requiere_scope_lock=SI. No hay nuevos DTO, dependencias, migraciones, JWT, RBAC o acceso multitenant. Los tests no sustituyen la prueba de CAS sobre Google Sheets ni prueban tenancy real.

**CI y cierre:** `dotnet test backend/Solqaryn.sln` desde el runner configurado, con check real en SHA de dev; A0 diff, A1 positivos/negativos xUnit, A2 denegaciones sin grant externo, A3 build y regresión .NET, A4 Scope Lock/check runs SHA exacto y readback PLAN/COLA/BITACORA. CI pendiente no es PASS. A5 requiere otra Task Javier. Rollback es `git revert` normal, nunca fuerza ni reescritura de historia.
