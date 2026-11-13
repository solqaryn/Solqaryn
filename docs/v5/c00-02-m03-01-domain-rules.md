# SOLQARYN V5 — C00-02.M03.01: reglas de dominio ejecutables

Se incorpora la máquina de **validación pura** de la microtarea V5 dentro del proyecto Domain de .NET 10. No se crea un dispatcher, endpoint, persistencia, evento entre Tasks ni nueva autorización. La única fuente de identidad/estados sigue siendo PLAN_MAESTRO H/I; COLA G/O y DAG siguen siendo puertas externas.

**Alcance real:** `backend/src/Domain/AutomationV5/V5WorkItemRules.cs` contiene decisiones sin efectos laterales para `CanClaim`, `CanMarkValidating`, `CanCloseDev` y `CanReviewA5`. La aplicación consumidora deberá proporcionar el catálogo y allowlist vivos y garantizar un CAS efectivo en su mecanismo de escritura: `CanClaim` no es un lock ni evita por sí sola carreras concurrentes.

**Pruebas dirigidas:** `backend/tests/Solqaryn.Tests/Domain/V5WorkItemRulesTests.cs` cubre aceptación de item PENDIENTE y todas las puertas, ID inexistente, caller no autorizado, owner ajeno, estado improcedente, bloqueos independientes G/O/DAG, fingerprint obligatorio, A0–A4 y A5_PENDIENTE, SHA exacto y A5 independiente. `dotnet test` y controles de seguridad de CI verifican compilación y regresión reales sobre el SHA publicado. Estas pruebas son unitarias del dominio; no simulan que Sheets tenga CAS ni certifican MySQL, UI, HTTP o integraciones externas.

**Matriz de herramientas:** requiere_dotnet=SI; requiere_node_angular=NO; requiere_mysql=NO (ninguna entidad o consulta DB tocada); requiere_docker=NO; requiere_e2e=NO; requiere_security=SI (identidades, owner, A5, pero sin modificación JWT/RBAC/tenant); requiere_scope_lock=SI.

**Límites:** no se aceptan TASK_ID de fuera del conjunto inyectado, ni se permite revisión A5 propia. Ninguna regla de dominio concede acceso a empresas A/B, cambia roles o interactúa con credenciales. El cierre institucional necesita aparte A0–A4, commit fast-forward, checks del mismo SHA, readback y A5_PENDIENTE. Rollback: `git revert` normal del commit, sin reescribir BITACORA o historial de otras Tasks.
