# C00-03.M03.01 — Reglas de dominio de cobertura integrada V5

**Implementación real:** `backend/src/Domain/AutomationV5/V5RequirementCoverageRules.cs`, validación pura de correspondencias requisito → microtarea → versión, contrato, prueba y evidencia. La clase funciona en .NET 10 Domain sin IO, API, tablas, migraciones ni nuevas Tasks.

**Contrato:** cada `Entry` exige `RequirementKey` en un conjunto de referencias V5 aprobado, `WorkItemId` en el catálogo vigente de 2.340 IDs, `SourceVersion`, `ContractReference`, `TestReference` y `EvidenceReference` no vacíos. `ValidateBatch` rechaza duplicados de (requisito, ID, versión) incluso con otra evidencia; admite correspondencias muchos-a-muchos y versiones distintas. Los conjuntos deben obtenerse de fuentes institucionales auténticas y verificadas: este validador no pretende leer ni reservar Google Sheets.

**Prueba real dirigida:** `backend/tests/Solqaryn.Tests/Domain/V5RequirementCoverageRulesTests.cs` incluye caminos positivos, muchos-a-muchos, rechazo de requisito/ID inexistente, ausencia de versión/contrato/test/evidencia, duplicados, versiones distintas y lote vacío. Ejecutar `dotnet build/test` desde GitHub Actions para el SHA publicado; no convertir resultados queued o skipped en PASS.

**Seguridad/fail-closed:** una correspondencia válida NO autoriza un claim, un cambio de PLAN H/I, A5, JWT/RBAC, lectura entre empresas, o promoción QA/main/PROD. Se mantiene el gate de DAG y verificación de owner en el orquestador autorizado y la separación tenant A/B. La creación de un servicio persistente o endpoint se evaluará en sus microtareas correspondientes, no en este contrato de dominio.

**Clasificación:** requiere_dotnet=SI; requiere_node_angular=NO; requiere_mysql=NO; requiere_docker=NO; requiere_e2e=NO; requiere_security=SI (identidad y rechazo de referencias no autorizadas); requiere_scope_lock=SI. Rollback reversible por `git revert` no destructivo del commit directo en dev; BITACORA histórica no se modifica. Estado LISTO solo con A0–A4 causales y A5_PENDIENTE de otra Javier.
