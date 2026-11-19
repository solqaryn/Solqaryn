# C15-01.M05.02 - verificación causal de contrato Application DTO

SOLQARYN/dev; PROJECT_SCOPE_LOCK=STRICT; DEV05 Task 6adc2e55f47c81919f6374e561687076; riesgo R2 conservador. Esta entrega añade cuatro xUnit en backend/tests/Solqaryn.Tests/Application/C1501M0502CoverageDtoContractTests.cs, sin modificar producción, controlador HTTP, API, tokens, seguridad, IAM, tenant, EF/MySQL, Angular ni secretos.

A0: prueba el DTO real Result de V5CoverageApplicationService.Prepare(Request), ya implementado en C15-01.M03.02. Los dos requisitos y dos IDs de fixture son sintéticos, NO son cobertura del plan institucional de 2340 IDs ni prueba de acceso a Google Sheets.

A1: corte internamente consistente responde AUTHENTICATED_EVIDENCE_REQUIRED, nunca autorización. SHA falsificado y revisión obsoleta responden error sin reflejar marcador confidencial; mapping duplicado denegado; fronteras MaxMappings=1 y 2341 y requisito no mapeado también denegados. En todos los casos JSON tiene seis campos y tres flags de escritura/certificación/A5 siempre false.

A2: solo tests de preflight Application fail-closed; NO acredita JWT HTTP firmado, RBAC persistente, pertenencia tenant A/B ni permisos de backend. A3: .NET 10 xUnit real con compilación Release y tests en GitHub-hosted; MySQL/Chromium/proveedores NO_APLICA al diff test-only. A4: exigir Backend Release y pruebas dirigidas incluidas, Scope Lock y V5 en SHA exacto y readback PLAN/BITACORA antes de cerrar. Mientras CI pendiente, mantener VALIDANDO; no contar SKIPPED/IN_PROGRESS como PASS. A5 independiente por Task Javier distinta de DEV05. Rollback normal git revert, no force-push. Sin QA/main/PROD/R3.
