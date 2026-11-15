# SOLQARYN V5 — C15-01.M03.02: servicio Application de preflight de cobertura trazable

**Autor:** PRIMARY DEV 05 — JAVIER_CHATGPT (`6adc2e55f47c81919f6374e561687076`). **Fecha:** 2026-11-15. **Repositorio autorizado:** `solqaryn/Solqaryn@dev`; Scope Lock STRICT. **Tecnología:** .NET 10, Domain y Application, pruebas xUnit; **sin** Angular, HTTP, EF/MySQL, migración, cola viva, dispatcher, workers, eventos ni infraestructura.

## 1. Límite funcional real implementado

`backend/src/Application/AutomationV5/V5CoverageApplicationService.cs` implementa un servicio **real de aplicación de sólo lectura** que recibe un corte de cobertura **suministrado por un llamador**, coteja SHA esperado/observado, revisión de fuente, conjuntos de requisitos aprobados y IDs reales del plan, límites de cardinalidad, e invoca `V5CoverageDomainValidator.Assess` de C15-01.M03.01. Devuelve código de denegación en la primera inconsistencia o `AUTHENTICATED_EVIDENCE_REQUIRED` cuando el corte sea internamente coherente. Este resultado **no es PASS ni certifica** los 2.340 IDs; la fuente suministrada por un cliente jamás autentica GitHub, el Standard, Google Sheet, CI o A5.

La API pública de este tipo es `Prepare(Request)`, no endpoint HTTP: `Request` incluye `ExpectedHeadSha`, `ObservedHeadSha`, `ExpectedSourceRevision`, `Source`, `ExpectedApprovedRequirementKeys`, `ExpectedPlanItemIds`, `Mappings`, `MaxMappings`. `Result` expone `Code`, `MappingsExamined`, `RequirementsWithoutMapping` y tres indicadores siempre falsos: `AllowsInstitutionalWrite`, `CertifiesFullCoverage`, `GrantsA5OrEnvironmentPromotion`. No se añade DI, credencial, autoridad de JWT ni controller. Los conjuntos “expected” y “source” siguen siendo datos del llamador y requieren un futuro adaptador autenticado.

## 2. Denegaciones y controles causales

| Código | Disparador | Efecto |
| --- | --- | --- |
| CHECK_SHA_MISMATCH | SHA ausente, mal formado, no hexadecimal o versión distinta | No avanzar |
| SOURCE_REVISION_MISMATCH | Revisión esperada vacía o diferente | No avanzar |
| CAPACITY_EXCEEDED | MaxMappings fuera de 1–2340, mapeos o catálogo fuera de límite | No avanzar |
| CATALOG_SNAPSHOT_MISMATCH | Requisitos o IDs esperados difieren del corte recibido | No avanzar |
| COVERAGE_EMPTY | Lista de mappings vacía | No avanzar |
| REQUIREMENTS_UNMAPPED | Falta al menos un requisito aprobado del corte | No avanzar |
| DUPLICATE_COVERAGE_MAPPING | Par de fuente/ID duplicado según Domain | No avanzar |
| UNKNOWN_WORK_ITEM, UNSAFE_REFERENCE | ID no pertenece al catálogo o evidencia contiene controles | No avanzar |
| AUTHENTICATED_EVIDENCE_REQUIRED | Comparación **interna** satisfactoria | Continuar sólo a verificación externa independiente, **sin autorización** |

Las decisiones del servicio son deterministas, sin I/O ni efectos secundarios. No prueba autoridad de fuente, firma de evidencia, propietario, ACL, lease, concurrency CAS, A5, JWT/RBAC, tenant A/B ni resultado de CI: sólo conserva esos límites para un componente externo que cuente con autenticación y autorización efectiva. Un mock o parámetro de contexto no puede conceder estos privilegios.

## 3. Matriz de pruebas unitarias positivas/negativas

`backend/tests/Solqaryn.Tests/Application/V5CoverageApplicationServiceTests.cs` incluye casos xUnit de preflight consistente pero no certificado, SHA inválido/obsoleto, revisión/plan/requisitos divergentes, requisito faltante, duplicado, ID desconocido, referencia con salto de línea, límite y catálogo vacío. Los casos son código real de tests **por ejecutar en GitHub Actions del SHA publicado**, no resultados inventados. La prueba positiva exige además que todos los campos que podrían autorizar escritura, A5 o promoción sigan siempre `false`.

**Predecesores:** `C15-01.M03.01` contiene `V5CoverageDomainValidator` y xUnit con blobs SHA `7c433bc1d832b24388b418602058b0f4951fb08a` / `495f557618aeb198c0b21e03ac1921630ba7ea46`; PLAN consta LISTO y A5 independiente DEV05 sobre autor DEV04. M02.01–06 son contratos de origen, mapeo y gates, no tablas ni endpoints listos para usar. El DAG de M03.02 fue VALIDADO/SI y COLA G/O=SI al reclamar; no se fuerza estado técnico de sucesoras.

## 4. Impacto y A0–A5

- **A0:** revisar cambio real limitado a Application service, tests, este documento y sección autocontenida de CHANGELOG. Confirmar que no altera Domain previo ni backend de identidad, contratos de API, tenant, seguridad o DB.
- **A1:** ejecutar xUnit y compilación .NET 10 reales del SHA; confirmar positivos, negativos, límites y códigos. Si CI sigue pendiente, este ID permanece VALIDANDO.
- **A2:** mantener fail-closed, ausencia de I/O, no conceder JWT/RBAC, revisar aislamiento de datos y secretos; seguridad backend dirigida en CI según check causal.
- **A3:** comprobar invocación del validador Domain existente; sin crear controller, storage, migración o dispatcher. Verificar test suite de integración pertinente; no inferir MySQL real por compilación.
- **A4:** publicación Git fast-forward/CAS sobre HEAD, SHA, cuatro blobs/diff, Scope Lock, verificador V5, compilación/test backend y seguridad pertinente en `completed/success` exact-SHA, PLAN/COLA/BITACORA readback. `queued`/`in_progress`/`cancelled`/`skipped`/`failure` no son PASS requerido.
- **A5:** una Task Javier distinta de DEV05 revisará código/tests/logs y su SHA, sin autoaprobación ni publicación en QA/main/PROD.

**Rollback:** `git revert` normal del commit verificado, conservando historia concurrente y nuevos eventos compensatorios. No editar BITACORA histórica, cuentas, schedules, secretos, producción ni proveedores. Un futuro servicio para leer datos institucionales reales requiere microtarea propia, autenticación de conectores, pruebas tenant A/B, políticas y CAS atómico antes de cualquier escritura.
