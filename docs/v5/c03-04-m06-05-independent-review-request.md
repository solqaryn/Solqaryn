# C03-04.M06.05 — Solicitud auditable de revisión técnica independiente

PROJECT_SCOPE_LOCK=STRICT
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
MICROTAREA_ID=C03-04.M06.05
AUTHOR_TASK_ID=6adbb9fff1548191a1bd5b30d9e9984a
RISK=R0_DOCUMENTAL
A5=SOLICITUD_PENDIENTE; NO_A5_PASS

## Objeto, autoridad y alcance atómico

La acción concreta de PLAN_MAESTRO!D576 es **solicitar revisión independiente del lote cuando sea un gate**, no realizar dicha revisión en nombre de otra Task. Esta solicitud queda publicada como archivo institucional para consumo por una de las otras cinco Tasks Javier autorizadas y su trazabilidad por BITACORA. Es un paquete de revisión y criterios, **no** una asignación física de Task, contacto humano, certificación M06 completa ni autorización para QA/main/PROD. PLAN E576 es resultado acumulativo de fase, no evidencia obligatoria de que esta sola solicitud haya ejecutado restauraciones o tests.

Revisar de forma causal las seis salidas M05 del bloque C03-04 Respuesta y recuperación, usando los SHA, blobs, CI, riesgos y autorías reales. El Sheet autoritativo conserva el histórico de autores, cierres y posibles fallos. No modificar dependencias, fórmulas, owner previo o contratos de negocio desde este documento.

## Paquete de fuentes a inspeccionar

| Microtarea | SHA fuente de dev | Evidencia delimitada / brecha |
| --- | --- | --- |
| `C03-04.M05.01` | [`a2aa4ff18010`](https://github.com/solqaryn/Solqaryn/commit/a2aa4ff18010d88330d326061211d30fb02ccbd3) | 27 reglas xUnit de dominio; no prueba JWT/HTTP o SQL real |
| `C03-04.M05.02` | [`c33d3b80bc38`](https://github.com/solqaryn/Solqaryn/commit/c33d3b80bc38e009790c3071b6f82983f9de5b77) | 22 contratos xUnit DTO; no certifica endpoint autenticado |
| `C03-04.M05.03` | [`e8232053e168`](https://github.com/solqaryn/Solqaryn/commit/e8232053e1680303abd2a6fa7a6c5a4ec3774b19) | 3 integraciones MySQL 8.4 efímero; no backup ni restore real |
| `C03-04.M05.04` | [`6b523ce077f5`](https://github.com/solqaryn/Solqaryn/commit/6b523ce077f534e6d6326165c82ec812f247a929) | 17 casos de aceptación in-process; no HTTP real |
| `C03-04.M05.05` | [`204a4f972e3d`](https://github.com/solqaryn/Solqaryn/commit/204a4f972e3d6935ec553f82c36777271002b0fe) | 35 negativos de dominio; roles y claims de fixtures |
| `C03-04.M05.06` | [`4b4163c55b00`](https://github.com/solqaryn/Solqaryn/commit/4b4163c55b002ffe02c05f60240fa059b834a492) | 10/10 xUnit con MySQL 8.4 efímero, job 135845972811; no restauración |

El manifiesto [M06.02](./c03-04-m06-02-version-diff-evidencia.md), la [auditoría de evidencia M06.01](./c03-04-m06-01-auditoria-pruebas-vs-mocks.md), el [registro de riesgos M06.03](./c03-04-m06-03-limitaciones-riesgos-decisiones.md) y la [regresión M05.06](./c03-04-m05-06-regression-continuity.md) facilitan reproducibilidad y señalan límites. El job [M05.06 GitHub-hosted](https://github.com/solqaryn/Solqaryn/actions/runs/44604929709/job/135845972811) acredita el servicio MySQL descartable en el SHA pertinente, no una restauración irreversible.

## Petición de revisión y criterios de aceptación A5

1. Una Task **Javier distinta de la autora del cambio bajo examen y de cualquier reparadora de ese mismo cambio** debe confirmar la separación efectiva por TASK_ID (no por login compartido jmejia31) y su propia autoridad nativa. No se invoca ni se simula su ejecución desde este archivo.
2. Consultar PLAN A/H/I/J/K/L/M y BITACORA A:Z por ID exacto, el commit de cada unidad y los blobs críticos. Si un archivo o dependencia cambió, comprobar equivalencia real de blobs, árbol y tests; el simple SHA descendiente no transfiere resultados.
3. Inspeccionar el código y al menos un caso positivo, uno negativo, límite y error pertinente por cada scope. Contrastar check-runs status=completed y conclusion=success de GitHub Actions para SHA causal y controles requeridos de Scope Lock, V5, .NET/EF/seguridad. Un job SKIPPED, CANCELLED, FAILED, QUEUED o IN_PROGRESS requerido nunca es PASS.
4. Delimitar autoridad: reglas puras, DTO y mocks no certifican JWT firmado, autorización HTTP, revocación de membresía, aislamiento persistente A/B ni restore transaccional. No elevar pruebas efímeras de MySQL a pruebas en Aiven/PROD. No quitar gates P0/P1, A5_FAIL, riesgos, o controles de roles para lograr aprobación.
5. Para el resultado A5 real, publicar **un** EVENT_ID e IDEMPOTENCY_KEY distintos en BITACORA A:Z con TASK_ID de revisor, autor/reparador, SHA, job, hallazgos, PASS o FAIL, readback y fecha; adquirir candado V17 de la unidad específica sin doble escritor. Si FAIL, conservarlo y exigir reparación/corrección causal antes de una nueva A5 independiente. Nunca autoaprobar la unidad que esta misma Task desarrolló.
6. El resultado A5 puede abastecer DAG únicamente si la fórmula y los prerequisitos efectivos lo permiten. M06.04 es un ensayo de rollback/desactivación separado; M06.06 es cierre terminal posterior, no se desbloquea por esta solicitud.

## Riesgos todavía abiertos y salida esperada del revisor

Seguir trazando los riesgos R01–R16 en M06.03: restauración equivocada, integridad DB+archivos, revocación, PII, tenants A/B, permisos break-glass, versiones concurrentes, idempotencia, custodia de claves, proveedores y RPO/RTO sin medición. Una revisión favorable de código existente no es afirmación de que esos riesgos quedaron resueltos. Resultado A5 permitido: PASS **acotado y causal por SHA** o FAIL con defecto reproducible, nunca certificación global por omisión.

Para esta unidad R0 documental: A0=diff/fuentes/autorías/enlaces; A1=criterios falsables positivos/negativos y ausencia de PASS inventados; A2=NO_APLICA a cambios de runtime JWT/RBAC/tenant pero se exigen límites de independencia; A3=NO_APLICA a nueva integración API/MySQL/Chromium porque este diff solo solicita revisión; A4=verificar commit fast-forward CAS, check exact-SHA `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada`, y readback PLAN/COLA/BITACORA. A5 de esta **misma solicitud** queda PENDIENTE de Task Javier distinta.

Rollback del changeset documental: `git revert` normal después de releer HEAD y los cambios concurrentes; nunca force-push, reescritura de BITACORA o restauración de datos. No se modifica código de producción, IAM, QA, main, PROD, secretos, RRULE ni proveedores.
