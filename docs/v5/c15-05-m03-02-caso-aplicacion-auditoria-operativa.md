# SOLQARYN V5 — C15-05.M03.02 servicio de preflight de auditoría operacional (Application)

Autor: PRIMARY DEV 01, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. Exclusivamente `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`, 14 de noviembre de 2026.

## Preflight institucional y objetivo real

Fuente única: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL; PLAN_MAESTRO fila 2319, COLA G/O=SI y DAG F=VALIDADO/G=SI antes del CAS PENDIENTE→EN_PROGRESO. C15-05.M02.01–06 y C15-05.M03.01 constan LISTO con A5 independiente; revisión J00 de M03.01 acreditada sobre commit `027c693dc2e32ca97c48753604270d777ef8780c`, blobs Domain `a7919d2beacbb9048f3112c82bdb743b2e80f3d2` y xUnit `a43404b762a135e3452b3fe3d39f31fa52dd4888`.

La entrega **sí implementa código de aplicación .NET 10 y pruebas xUnit**, exclusivamente `V5OperationalAuditApplicationService` y tests, sin endpoints nuevos, consultas GitHub/Drive, cambio de entidad EF, MySQL, eventos, webhooks, schedulers, cambios de permisos, workers, nuevas Tasks ni migración. No se habilita QA/main/PROD.

## Contrato funcional

El método `Prepare(Request)` toma un snapshot que debe proceder de un adaptador externo autenticado. Compara HEAD SHA esperado y observado, revisión esperada y observada, catálogo de Tasks esperado y declarado, límites de 1–2340 microtareas, y delega a `V5OperationalAuditDomainRules.Evaluate` la validación estructural de estados, owners, duplicados, SHAs y evidencias. Nunca lee la identidad de un tenant desde una entrada web ni confía en una referencia textual como prueba de GitHub/Sheets.

La salida positiva se llama **AUTHENTICATED_EVIDENCE_REQUIRED**, NO PASS, NO LISTO, NO autorización CAS, NO aprobación A5. `AllowsInstitutionalWrite` permanece `false` incluso con el snapshot completo. Un adaptador FUTURO deberá comprobar con credenciales reales la revisión fuente, SHA exacto de CI, autoría independiente, permisos, aislamiento y compare-and-swap atómico con readback; **esta microtarea no implementa el adaptador**.

## Casos y seguridad

Pruebas dirigidas xUnit: snapshot completo sólo solicita verificación externa, SHA alterado o corto se rechaza; catálogo/revisión desalineados se rechazan; PENDIENTE/VALIDANDO o A5 incompleta jamás liberan; autoaprobación falla; catálogo excesivo, IDs repetidos/desconocidos y actores fuera de allowlist fallan. No se serializan credenciales, JWT, PII, empresas ni información de terceros. Cambios confinados a Application y tests; Domain, API, EF, JWT/RBAC y guards tenant existentes intactos.

## Gates y reversión

A0: revisar 2 archivos C# y este documento además de CHANGELOG. A1: xUnit positivo/negativo real dirigido en GitHub Actions exact SHA. A2: seguridad multiempresa, auth/secret gate y ausencia de exposición de datos con checks pertinentes. A3: build/test de backend net10.0 más integración de capas, sin fingir llamada HTTP/MySQL. A4: commit fast-forward/CAS `dev`, Scope Lock y V5 CHANGELOG SUCCESS SHA exacto, diff/blobs y readback PLAN/COLA/DAG/BITACORA. A5: otra Task JAVIER distinta de J00, después de LISTO_DEV; sin autopromoción ni acceso productivo.

Si la CI causal está queued/in_progress/cancelled/failed, conservar VALIDANDO y owner, sin declarar PASS. Rollback: git revert normal del changeset y compensación append-only, sin force-push ni reescritura histórica.
