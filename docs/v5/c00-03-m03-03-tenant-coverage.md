# SOLQARYN V5 — C00-03.M03.03: proyeccion de cobertura con autorizacion por tenant

**TIPO 2 (.NET 10), cambio ejecutable y testable:** `V5CoverageTenantProjectionService` aplica autenticacion de actor contra allowlist autorizada, verifica scope habilitado por actor, binding tenant-scope de servidor, scope de cada work item y de cada requisito de cobertura antes de llamar a `V5RequirementCoverageProjectionService`. El resultado esta tipado, respeta los 2340 IDs V5 y no deja escapar filas de otro tenant.

**Seguridad:** los mapas `trustedTaskScopes`, `trustedScopeTenants`, `trustedWorkItemScopes` y `trustedRequirementScopes` deben venir de origen institucional/servidor ya autorizado y no de un request. Esto NO implementa JWT/roles/tenancy HTTP global, no escribe en PLAN/COLA/BITACORA, no habilita QA/PROD, no certifica pruebas ni A5 y no reemplaza CAS. Incluso LISTO mantiene `EVIDENCE_NOT_VERIFIED` hasta confirmar CI y auditoria.

**Pruebas dirigidas:** positivos de proyeccion limitada con evidencias pendientes; negativos de actor externo, scope ajeno, tenant cruzado, item de otro scope, requisito de otro scope, binding faltante, SHA movido, duplicados y lote vacio.

**Clasificacion y cierre:** requiere_dotnet=SI, requiere_security=SI, requiere_scope_lock=SI; requiere_node_angular=NO, requiere_mysql=NO, requiere_docker=NO, requiere_e2e=NO. A0 revision de diff/candidata; A1 xUnit positivos/negativos; A2 tenancy fail-closed; A3 build/test .NET exact SHA; A4 check-runs exact SHA y readback PLAN/COLA/BITACORA. A5_PENDIENTE por una Task Javier distinta del autor antes de promocion. Rollback por revert no destructivo.
