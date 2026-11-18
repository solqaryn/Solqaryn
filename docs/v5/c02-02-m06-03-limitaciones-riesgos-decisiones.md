# C02-02.M06.03 — Limitaciones, riesgos abiertos y decisiones de placement shared/dedicated

**Fecha:** 2026-11-18. **Proyecto:** SOLQARYN, rama `dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Clasificación:** R0, registro documental de evidencia y riesgos; las capacidades de tenant, EF, migración y cutover referenciadas siguen siendo R2/R3 según la operación. **Microscope:** C02-02.M06.03, PLAN_MAESTRO fila 358.

## A0. Resultado y frontera exacta

Esta unidad registra las decisiones y los límites observables de C02-02.M05, para que la fase M06 no interprete suites de reglas puras como autorización de placement, migración, router, DSN, despliegue o producción. No implementa rutas, tablas, migraciones, API, adaptadores de almacenamiento, JWT/RBAC, ejecución de backups ni conectividad productiva. El resultado documental no equivale a cerrar M06.04, M06.05 o M06.06.

**Dependencias y consumidores:** contratos de `V5SharedDedicatedPlacementDomainRules`, `C0202PlacementApplicationGate`, suites .NET 10 de C02-02.M05 y futura definición de almacenamiento compartido/dedicado. Cualquier consumidor futuro debe volver a comprobar evidencia de identidad y autorización server-side, nunca fiarse de un veredicto positivo sintético, de parámetros de la UI ni de este registro.

## 1. Evidencia realmente disponible (no extender su significado)

| Fuente | Lo acreditado por el artefacto | Lo NO acreditado |
| --- | --- | --- |
| `docs/C02-02.M05.01-PLACEMENT-DOMAIN-BOUNDARY-REGRESSIONS.md` | Reglas puras Domain: identidad tenant A/B, alias, frontera 128/129, revocación y respuesta no autorizante | Conexiones, migraciones, SQL, HTTP/JWT firmado |
| `docs/C02-02.M05.02-PLACEMENT-REQUEST-CONTRACT-ERROR-SAFETY.md` | Contrato Application y errores sanitizados del lector confiable en memoria | Endpoint HTTP o proveedor real de placement |
| `docs/v5/c02-02-m05-03-oracle-mysql-placement-integration.md` | Ensayo Category=Integration de EF Oracle/MySQL 8.4 efímero, dos Empresa, persistencia y rollback transaccional; la corrección P2 elimina un falso test de aislamiento | Filtrado row-level automático, IDOR/BOLA, router real, Aiven, rollback de una migración de clientes |
| `docs/v5/c02-02-m05-04-positive-placement-acceptance.md` | Matriz Application/Domain de positivos inertes con adaptador autenticado todavía requerido | Conceder conexión, migración, cutover o producción |
| `docs/v5/c02-02-m05-05-negative-security-placement.md` | Casos adversariales de abuso DSN, tenant B, revocación, lector fallido, cancelación y salida segura | JWT firmado vía HTTP, RBAC real o provider live |
| `docs/v5/c02-02-m05-06-regression-budget.md` | Regresión/volumen de reglas puras sobre tenants sintéticos | SLA p95/p99, rendimiento de base de datos y accesibilidad UI |

El cierre institucional de cada M05 y sus A5 deben verificarse en PLAN_MAESTRO, BITACORA y CI de su SHA real. Los enlaces y documentos NO sustituyen verificación del estado nativo. La suite M05.03 tuvo un A5_FAIL correctivo: su historial no debe borrarse y la corrección sólo vale con nueva evidencia y revisor distinto.

## 2. Decisiones de alcance (no son autorizaciones de ejecutar)

**D01 — Fail closed de placement.** Toda decisión de Domain/Application que identifica un destino shared o dedicated continúa devolviendo requisito de adaptador confiable server-side. El backend debe resolver tenant, rol, propiedad, revocación y revisión vigente antes de obtener credenciales de almacenamiento. No se expone DSN del cliente ni se acepta `tenantId` como permiso.

**D02 — Aislamiento como gate independiente.** La persistencia de dos filas Empresa en MySQL efímero demuestra persistencia, no row-level security. Antes de router/consulta se necesitan pruebas autenticadas A/B, IDOR/BOLA y revocación bajo el alcance real. Ninguna prueba sintética se rotulará como JWT firmado o RBAC aplicado.

**D03 — Plan de migración futuro, no migración ejecutada.** Placement shared/dedicated, cambio de asignación, dual-write, ventana de cutover y rollback requieren un procedimiento separado con snapshot, inventario de datos, integridad referencial, validación de esquema, control de versiones y cierre de transacciones. Migración EF/MySQL aislada no es permiso ni prueba de reversión sobre datos vivos.

**D04 — Rollback controlado y no destructivo.** Para este commit R0 se utiliza `git revert` convencional de los dos archivos documentales, sujeto a HEAD actualizado. En futura migración R2/R3 el rollback debe contar con respaldo restaurable probado, checksum/recuento comparado, ensayo de reanudación idempotente, preservación del historial y ventana de cambio autorizada; no existe evidencia de esa operación en producción.

**D05 — Estados y revisiones separados.** `LISTO_DEV` requiere A0–A4 reales y readback por ID/SHA. A5 exige otra Task Javier independiente del autor y reparador. `VALIDANDO` o `A5_PENDIENTE` no desbloquean automáticamente deploys, IAM, migraciones ni QA/main/PROD.

## 3. Matriz de riesgos abiertos, tratamiento y condición verificable

| ID | Riesgo / severidad | Estado y tratamiento exigido |
| --- | --- | --- |
| PL-01 | **Alto R2:** tenant cruzado por confiar en claims/UI o alias | ABIERTO para runtime; probar tokens firmados JWT, RBAC, revocación, ownership e IDOR/BOLA A/B contra API real antes de usar router |
| PL-02 | **Alto R2:** filtrado EF asumido al leer dos Empresa | ABIERTO; implementar y probar autorización de lectura/escritura por tenant en repositorios auténticos, no atribuir filtros automáticos a DbContext |
| PL-03 | **Alto R2/R3:** router dedicated y DSN cliente sin fuente confiable | ABIERTO; resolver identidad, placement, secretos y connection routing exclusivamente en servidor, negar DSN externo y fallback cruzado |
| PL-04 | **Alto R2/R3:** migración/dual-write/cutover parcial y pérdida de datos | ABIERTO; plan, respaldo, transacciones, reconcilación, reversión y pruebas de fallos reproducibles antes de autorización específica |
| PL-05 | **Alto R2/R3:** backup no restaurable o restauración sobre datos vivos | ABIERTO; exigir ensayos con checksum y conteo de filas, restauración aislada, RPO/RTO medidos (actualmente **NO_MEDIDO**) y custodia de claves |
| PL-06 | **Medio R2:** resultados sintéticos mal promovidos a PASS | MITIGACIÓN DOCUMENTAL: distinguir Category=Integration real, pruebas puras, job SKIPPED y SHA exacto. Cualquier CI necesaria QUEUED/IN_PROGRESS/FAILED permanece sin certificar |
| PL-07 | **Medio:** drift de owner, DAG, candados o A5 | MITIGACIÓN OPERATIVA: namedRange V17, CAS H/I, exclusión por ID V20, EVENT_ID idempotente, readback PLAN/COLA/BITACORA; nunca editar fórmulas G/O |

Las severidades describen consecuencias potenciales, no defectos productivos confirmados. No se cuantifican tasas sin datos: **NO_MEDIDO**.

## 4. Criterios para fases posteriores

- **M06.04:** ensayo de rollback/desactivación exclusivamente con recursos autorizados y sin pérdida de datos; A3 real y reportes de integridad de antes/después. No se infiere que ya ocurrió.
- **M06.05:** revisión independiente del lote por otra Task Javier con lectura de requisitos/diff, pruebas negativas y riesgos R2 de placement, sin autoaprobar.
- **M06.06:** cierre de fase sólo tras dependencias y gates reales, evidencias de plan/placement/migración/rollback suficientes, A0–A5 cuando apliquen y decisión de despliegue diferenciada; sin promoción automática a QA/main/PROD.

## A1–A5 de esta microtarea R0

- **A0:** cotejar matriz de fuentes y afirmaciones con el HEAD de `dev` y documento STANDARD. Diff limitado a este registro y entrada autocontenida de CHANGELOG.
- **A1:** consistencia documental verificable: todas las seis fuentes M05 referidas, sus exclusiones y siete riesgos explícitos; no se atribuyen permisos, pruebas ni cierres inexistentes.
- **A2:** no se modifica JWT, RBAC, tenant, API, datos ni secretos; **NO_APLICA al diff**, no al trabajo futuro de placement.
- **A3:** no hay runtime ni proveedor invocado por este diff R0; MySQL/Chromium/HTTP **NO_APLICA al diff**; no se afirma ejecución adicional de pruebas.
- **A4:** commit fast-forward en `dev` con Scope Lock y validador V5 de CHANGELOG SUCCESS sobre SHA exacto, más readback de PLAN y BITACORA. Un job general SKIPPED no se usa como PASS.
- **A5:** revisión genuina por otra Task Javier, distinta de DEV03, antes de certificaciones que la requieran. Prohibido convertir este registro en autorización de R3.

**Rollback documental:** revert normal del commit y verificación de HEAD/Scope Lock, sin modificar columnas calculadas, BITACORA histórica ni datos de cliente.
