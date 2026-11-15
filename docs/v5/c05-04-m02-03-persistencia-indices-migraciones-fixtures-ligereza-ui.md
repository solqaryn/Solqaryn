# SOLQARYN V5 — C05-04.M02.03: persistencia, índices, migraciones y datos de prueba de ligereza UI

**Autor:** PRIMARY DEV 02 — JAVIER_CHATGPT (TASK_ID 6af16bb9492c8191b8e8c7d606f11507). **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA. **Entorno:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. **Estado del artefacto:** solo diseño documental, sin migración, DDL, transacción ni base real modificados.

## Contexto y motivo de almacenamiento

PLAN_MAESTRO fila 838, COLA G/O=SI y DAG VALIDADO/SI autorizan M02.03 como diseño independiente dentro de C05-04. Los precedentes documentales M01 y M02.01 tratan carga, bundles, HTTP duplicado, Core Web Vitals, foco/ARIA y cambio de contexto multiempresa. No se requiere una tabla persistente por defecto: la opción preferida es evidencia de CI versionada y despersonalizada en un artefacto efímero. Una nueva tabla solo se justificaría por requisito explícito de retención/consulta, sin duplicar el histórico institucional PLAN/COLA/BITACORA.

Arquitectura real: ASP.NET Core 10, Domain/Application/Infrastructure/API, MySQL con EF Core 10.0.12 y proveedor MySql.EntityFrameworkCore 10.0.9; frontend Angular 22.2.1. Este documento no modifica el proveedor ni crea una segunda fuente de autoridad sobre tenant, permisos o estados V5.

## Opciones de persistencia y decisión pendiente

| Opción | Destino conceptual | Ventajas | Condiciones / riesgos |
| --- | --- | --- | --- |
| S01 | Artefactos de CI por SHA, sin datos de usuarios | sin migraciones ni retención transaccional | conservar procedencia, acceso y caducidad |
| S02 | Resumen agregado de pruebas sintéticas | comparación de tendencias reproducible | métricas y entorno comparables, sin tokens/PII |
| S03 | Tabla versionada MySQL, solo si se autoriza más adelante | consultas de historial validadas por API | exige aislamiento tenant, índice, TTL, migración reversible y pruebas EF/MySQL |
| S04 | Cache efímera client-side | navegación fluida sin escribir base | debe invalidarse por sesión/tenant/permiso, nunca fuente de autoridad |

La decisión **PENDIENTE** es elegir S01 por defecto o habilitar S03 tras justificar finalidad, retención, seguridad y coste. No afirmar que el destino o la retención ya se aprobaron.

## Modelo relacional conceptual, no entidad EF creada

| Contrato futuro | Campos conceptuales | Clave / aislamiento |
| --- | --- | --- |
| UiPerfRun | runKey, sourceSha, routeTemplate, viewport, networkProfile, cacheMode, capturedAtUtc | unique(runKey, sourceSha), datos sintéticos |
| UiPerfMetric | runKey, metricName, value?, unit, sampleCount, evidenceRef | PK(runKey, metricName); nulo si no medido |
| UiRequestAggregate | runKey, requestGroup, method, count, duplicateCount | PK(runKey, requestGroup, method), sin URL sensible |
| UiTenantContextProof | runKey, opaqueTenantRef, policyVersion, authorizedContext, proofRef | relacionar sólo tenant autorizado y prueba redactada |
| UiAcceptanceLink | runKey, criterionId, result, proofRef, reviewerTaskId | no confiar en texto PASS sin artefacto y autoría |

Los nombres son **propuestas no implementadas**, no tablas existentes. No persistir tokens, cookies, email, nombres, payload HTTP, ruta con query sensible, secretos, capturas con PII ni identificadores crudos de persona/tenant; usar claves opacas y controlar finalidad y plazo.

## Índices, consistencia y rollback futuros

Si posteriormente se aprueba S03: revisar (tenantScope, sourceSha, capturedAtUtc) para consultas autorizadas, (runKey, metricName) para idempotencia, (criterionId, result) para evidencia, y (capturedAtUtc) para purga controlada. No aprobar índice sin EXPLAIN y cardinalidad real; no inferir que estos índices ya existen. Los writes deben derivar tenant de contexto autenticado server-side; una transacción atómica insertaría run, métricas y referencias de prueba, rechazando duplicados por clave estable. Lecturas entre A/B deben fallar cerradas.

Migración futura MIG01–MIG07: captura de versión y backup autorizado; migration EF additive/nullable; servicio MySQL 8.4 efímero en CI; fixture A/B; prueba de restricciones y rollback; revalidación de autorizaciones de lectura y escritura; decisión humana antes de habilitar persistencia externa. Nada se ejecuta en M02.03.

## Fixtures de regresión futuros, NO_EJECUTADOS

| Caso | Diseño sintético | Oráculo |
| --- | --- | --- |
| P01 | Run dev con SHA, entorno y métrica medida | queda vinculado al artefacto sin secretos |
| P02 | Dos runs mismo método, ruta y filtros sin PII | se comparan solo si dispositivos/red coinciden |
| P03 | Tenant A y B autorizados separadamente | queries devuelven solo su propio scope |
| P04 | Reintento con misma runKey | evita duplicar datos sin perder evidencia |
| P05 | Purga autorizada de run expirado | respeta retención y conserva trazabilidad auditada |
| N01 | Cambio de tenant con cache anterior | no muestra dato ni evidencia de A en B |
| N02 | Run duplicado con payload diferente | conflicto y rechazo, no sobrescritura silenciosa |
| N03 | Input negativo/NaN o muestra insuficiente | rechaza o deja NO_MEDIDO, nunca PASS |
| N04 | Payload con cookie/JWT/PII | rechaza y depura antes de cualquier persistencia |
| N05 | Borrado sin transacción o sin política | bloquea, no deja registros huérfanos |
| N06 | Fallo MySQL/CI requerido | VALIDANDO y rollback, sin LISTO falso |
| N07 | Query sin autorización server-side | fail-closed, incluso si UI oculta recurso |

Las pruebas mencionadas son contrato, **no resultados EF, MySQL, API o backup**.

## Impacto y gates

No se cambian Domain/Application/Infrastructure/API, EF migrations, entidades, datos, endpoints, ficheros frontend ni programas de medición. Auditoría A0 documento y CHANGELOG; A1 persistencia/índices/fixtures positivos y negativos documentados; A2 aislamiento A/B, mínimos de privacidad y transacción futura; A3 correspondencia con arquitectura y contratos M01/M02.01; A4 requiere publicación fast-forward/CAS, Scope Lock y verificador V5 exact-SHA, readback PLAN/COLA/BITACORA. CI MySQL/E2E no es causal a un diseño Markdown y queda NO_APLICA, sin falsificar PASS. A5 por Task Javier distinta. Revert normal de estos documentos, sin QA/main/PROD.
