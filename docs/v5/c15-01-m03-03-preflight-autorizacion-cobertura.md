# SOLQARYN V5 — C15-01.M03.03 preflight de autorizacion y tenancy de cobertura

**Autor:** PRIMARY DEV 04 JAVIER; TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. Repositorio unico solqaryn/Solqaryn dev, PROJECT_SCOPE_LOCK=STRICT. Autoridad: STANDARD V5 FINAL, PLAN_MAESTRO/COLA/DAG por ID exacto. Predecesor C15-01.M03.02 LISTO y A5_PASS independiente del autor; gate DAG VALIDADO/SI y COLA G/O=SI antes de reclamo.

## Alcance implementado, sin derechos nuevos

Se agrega codigo .NET 10 real a `backend/src/Application/AutomationV5/V5CoverageAuthorizedReviewPreflight.cs`. Este orquestador de solo lectura requiere `ClaimsPrincipal` autenticado y subject/tenant backend coherentes, membresia verificada, permiso efectivo `coverage.review`, Scope Lock de DEV y SHA completo exacto. Comprueba que el request no sustituye subject, tenant o SHA. Despues reutiliza `V5CoverageApplicationService.Prepare` para examinar requisitos y mapeos. Una evaluacion coherente retorna `INDEPENDENT_VERIFIED_SOURCE_REVIEW_REQUIRED`, no PASS institucional ni permiso para operaciones.

No valida la firma JWT ni autentica por si mismo el principal proporcionado: eso requiere un futuro adaptador backend con revision de membresia vigente, autorizacion real y lectura autenticada del Standard/PLAN/COLA/DAG/GitHub/CI/BITACORA. Los booleans del resultado que podrian autorizar escritura, cobertura completa, A5, promocion o cross-tenant son siempre `false`. No se introduce endpoint, service DI activo, acceso a base de datos, queries EF, secretos, job, webhook ni feature flag.

## Pruebas y rechazo de autoridad ausente

Los siete escenarios xUnit incluyen: corte internamente coherente sin certificacion, identidad ausente o principal no autenticado, usuario/tenant B falsificados, membresia revocada y rol sin permiso, Scope Lock falso/SHA obsoleto, request con SHA incoherente, requisito no mapeado. El positivo confirma todos los indicadores de concesion en falso; no hay falsa afirmacion de cobertura institucional. Requiere tests backend .NET10, seguridad pertinente, Scope Lock y V5 exact-SHA en GitHub Actions. Si estan en curso, la microtarea permanece VALIDANDO con su owner.

A0: diff y arquitectura Application previa, sin acceso externo. A1: xUnit dirigidos positivos/negativos reales cuando CI los ejecute. A2: fail-closed de subject, tenant, permiso, membresia y SHA; no concede JWT/RBAC/tenant. A3: composicion de validador Domain y servicio Application existente; pruebas de build causal, no test real MySQL/HTTP. A4: fast-forward con CAS GitHub, diff/sha/blobs y controles exact-SHA + readback Sheets. A5: revisor de las otras cinco Tasks Javier distinto del autor; sin autoaprobacion.

**Rollback:** git revert normal sobre dev y revalidacion CI; registrar evento compensatorio sin editar BITACORA historica. QA/main/PROD, DNS, servicios de pago, archivos y datos reales quedan fuera.
