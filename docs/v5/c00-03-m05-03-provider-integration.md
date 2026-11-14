# C00-03.M05.03 — Integración del proveedor V5 de cobertura

## Clasificación causal
El código de cobertura en `backend/src/Application/AutomationV5` ofrece `V5CoverageMemoryAdapter`, un adaptador estrictamente **en memoria** (sin DB, Google Sheets, lease durable o sincronización entre procesos). La proyección autorizada es `V5CoverageTenantProjectionService`. No existe en esta superficie un proveedor de persistencia MySQL/EF ni un endpoint de escritura institucional; no se simulan ni se activan.

## Prueba dirigida
`backend/tests/Solqaryn.Tests/Application/V5C0003M0503ProviderIntegrationTests.cs` integra ambas superficies reales:
1. Guardado, lectura con revisión y proyección autorizada; el estado LISTO continúa señalando `EVIDENCE_NOT_VERIFIED`.
2. Separación de namespaces del proveedor y rechazo de work item de scope ajeno por la proyección con mapas fiables.
3. Rechazo de escritura con revisión obsoleta, sin alterar lo observado por el consumidor.

## Límites y gates
**requiere_dotnet=SI**, porque la prueba es xUnit C# sobre servicios Application reales. **requiere_security=SI** por separación de scopes; Scope Lock y procedencia del CHANGELOG son obligatorios. **requiere_mysql=NO_APLICA**, **requiere_docker=NO_APLICA**, **requiere_node_angular=NO_APLICA** y **requiere_e2e=NO_APLICA** porque el diff no toca proveedores SQL, UI ni HTTP. CI .NET y gates nativos deben aprobar en el SHA publicado; no se declarará A0-A4 PASS ni LISTO antes del readback. El adaptador no constituye un CAS institucional ni una prueba de tenant HTTP/DB real. A5 de otra Task Javier será posterior.

## Reversión
`git revert` ordinario del commit exacto, sin force-push y sin borrar historial; únicamente se incorporan tests/documentación, sin cambios de runtime, migraciones, datos ni grants.
