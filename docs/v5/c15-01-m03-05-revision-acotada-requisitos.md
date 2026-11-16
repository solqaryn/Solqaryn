# SOLQARYN V17/V15 — C15-01.M03.05 revisión acotada de cobertura de requisitos

**Microtarea:** C15-01.M03.05, PLAN_MAESTRO fila 2178. Autor DEV06 JAVIER, TASK_ID 6af16bbad26881918c283abe06e1f32a. PROJECT_SCOPE_LOCK=STRICT, rama solqaryn/Solqaryn@dev, riesgo R2 por cobertura institucional/concurrencia y control tenant.

## A0 — alcance y arquitectura
Se implementa un runner secuencial y acotado en Infrastructure, C1501RequirementCoverageBoundedReviewWorker (máximo 16 entradas), que reutiliza por puerto explícito el adaptador de snapshot C1501RequirementCoverageSnapshotAdapter de M03.04. Valida todo el lote antes de invocar fuente confiable: clave y ID seguros, SHA completo, mismo SHA para todos, duplicidad ordinal de requisito/ítem y veto de catalogo controlado por cliente o intención de escritura/A5. Cada entrada exige resultado de proveedor "EXTERNAL_REQUIREMENT_EVIDENCE_AND_A5_REQUIRED" y exactamente una fila, sin aceptar estados inventados. Un rechazo intermedio descarta toda salida parcial aunque haya lecturas previas.

No usa Google Sheets real, Drive, GitHub, SQL, API HTTP, workers hosted, servicios de pago ni credenciales. No hace escritura institucional, y jamás declara cobertura global 2.340, A5, aptitud QA ni permisos tenant. No se registran servicios DI ni operaciones automáticas. La integración auténtica con fuentes y autorización exigirá scope específico y causal posterior. Rollback: git revert normal de dev, no force/reset.

## A1 — pruebas
Se añaden once xUnit con resultado positivo no certificante y escenarios negativos: lote nulo/vacío/sobrecarga, validación previa de todas las entradas, claves no seguras, catalogo malicioso, SHA mezclado, duplicidad de requisito/ítem, comparador ordinal, denegación upstream, excepción opaca, cancelación y dependencia nula. CI debe compilar .NET 10 Release y ejecutar realmente la suite con 0 FAIL.

## A2 — seguridad y tenancy
La fuente de cada mapping se valida mediante el adaptador existente (M03.04), no mediante claims del cliente. La estructura de salida solo contiene keys e IDs validados, códigos fijos y no otorga permisos. El lote nunca autoriza A5 ni acceso tenant. Sin fuente institucional autenticada, no hay afirmación de cobertura real. Tests de regresión y jobs de Seguridad multiempresa/Authorization deben pasar sobre SHA exacto o equivalencia auditada.

## A3 — integración
El contrato compone Infrastructure→Infrastructure con el adaptador y los contratos Application/Domain ya existentes, comprobable por compilación y xUnit con puerto fake. No invoca ni modifica datos MySQL 8.4, EF, migraciones, frontend, browser, HTTP, JWT firmado o proveedores externos: NO_APLICA motivado solo para este changeset, no para certificación institucional end-to-end.

## A4/A5
Publicar en dev con un único fast-forward/CAS y sección CHANGELOG autocontenida. Cambiar EN_PROGRESO→VALIDANDO antes de certificar A1-A3; exigir GitHub checks completed/success de backend Release/build, seguridad multiempresa, autorización, Scope Lock y V5. Leer HEAD, PLAN/COLA/DAG y BITACORA para cada transición y liberar candado V17 tras readback. Si una CI requerida sigue QUEUED/IN_PROGRESS, conservar VALIDANDO y owner. A5 solo revisión independiente de otra de las cinco Tasks Javier y sin permiso QA/main/PROD.
