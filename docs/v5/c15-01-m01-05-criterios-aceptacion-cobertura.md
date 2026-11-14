# SOLQARYN V5 FINAL — C15-01.M01.05: criterios observables de aceptación para auditoría de cobertura

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 05 JAVIER (`6adc2e55f47c81919f6374e561687076`). **Ámbito:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Naturaleza:** M01 documental de oráculos de aceptación, NO censo ni auditoría de 2340 IDs ejecutada.

**Autoridad:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), más Skill canónica, AGENTS, Scope Lock y contratos [M01.01](./c15-01-m01-01-alcance-auditoria-cobertura-trazabilidad.md), [M01.02](./c15-01-m01-02-actores-interfaces-auditoria-cobertura.md), [M01.03](./c15-01-m01-03-inventario-fuentes-evidencia-cobertura.md), [M01.04](./c15-01-m01-04-riesgos-supuestos-decisiones-cobertura.md). Sus A5 siguen requiriendo identidad distinta del autor.

## 1. Objetivo, alcance y definición de aceptación

La futura auditoría de cobertura exigirá una relación reproducible `requisito V5 → ID exacto → dependencias PLAN/COLA/DAG → commit/blobs → CI causal → BITACORA → A5 independiente`. La meta contractual es **cero requisitos huérfanos y duplicados críticos** después de realizar el inventario completo y resolver excepciones; **NO CERTIFICADA** por esta M01.05. La matriz vigente de PLAN declara 2340 IDs; no se extrapola de una muestra ni de la cantidad variable de filas DAG. Los gates derivados G/O y F/G sólo dan elegibilidad de trabajo con preflight, no pruebas ni autorizaciones extras.

Se aprovechan los contratos documentales existentes M01.01 (invariantes NR01–16), M01.02 (14 actores, 12 entradas, 12 salidas, 10 fronteras), M01.03 (18 blobs leídos, 10 brechas NO_ACREDITADAS), M01.04 (14 riesgos potenciales, 10 supuestos y 10 decisiones abiertas). Estos números describen **preparación**, no ejecución total o tests de seguridad.

## 2. Dieciséis pares de aceptación positiva / negativa — TODOS FUTUROS_NO_EJECUTADOS

| Caso | Resultado positivo verificable futuro | Contraejemplo negativo que bloquea | Evidencia requerida |
| --- | --- | --- | --- |
| AC01 | Versiones y hash del Doc V5, Sheet y HEAD fijados en mismo corte lógico | Se mezclan revisiones sin señal de desfase | Snapshot con revisionId, fecha UTC y sha |
| AC02 | Se extrae el universo 2340 IDs exactos y se informa denominador reproducible | Se declara 2340 sin haber leído filas o se omiten claves | Listado/CSV verificado y hash |
| AC03 | Cada requisito rector se une con cero o más IDs y excepciones explícitas | Un requisito huérfano se declara cubierto | Matriz bidireccional y excepciones |
| AC04 | Se detectan duplicados por ID exacto y duplicación semántica justificada | Una duplicidad crítica se invisibiliza por coincidencia parcial | Reporte de colisiones revisable |
| AC05 | PLAN H/I y COLA E/I/G/O coinciden o muestran diferencia explicada | Un owner de COLA contradice PLAN sin alerta | Join por ID y readback |
| AC06 | DAG valida cada predecesor con estado causal real | Gate SI en sucesora con predecesor NO se acepta | Lista aristas, evaluación y motivo |
| AC07 | Se detectan ciclos/autorreferencias/ID no existente | Se permite un ciclo o prerequisito inventado | Resultado de grafo con caso negativo |
| AC08 | Un CLAIM modifica estado por CAS y preserva único owner | Dos Tasks reclaman mismo ID simultáneamente | Evento y readback de ambos intentos |
| AC09 | BITACORA añade 26 campos, EVENT_ID e IDEMPOTENCY_KEY únicos | Se duplica evento o reescribe historial | Comparación A:Z y key |
| AC10 | Commit directo es descendiente fast-forward del HEAD con sólo diff autorizado | Un SHA de otra rama/diff sustituye el scope | Parent/tree/files/sha comprobados |
| AC11 | CI relevante es completed/success y head_sha exacto | Cancelado, skipped, failed o queued cuenta como PASS | GET check-runs y logs dirigidos |
| AC12 | A5 independiente usa TASK_ID distinto y allowlist Javier6 | A5 de autor o Task retirada libera fase | Autor/revisor y blob original |
| AC13 | Pruebas JWT/RBAC niegan recurso tenant B a actor A cuando runtime aplique | Guard UI sustituye la denegación backend | HTTP/DB efímero positivo/negativo |
| AC14 | DB/migración sólo si código causal, con rollback probado y tenant preservado | Se declara MySQL real probado leyendo csproj | CI efímera y plan reversible |
| AC15 | Bloqueo A5_FAIL y condición M01.06/M06 se conservan hasta gate real | Un documental M01 libera migración o producción | Evidencia causal y autorización |
| AC16 | Informe final distingue NO_ACREDITADO, FALLO_VERIFICADO, PASS y riesgos pendientes | Se declara cero huérfanos/P0/P1 a partir de muestra | Acta con denominador, contraejemplos, incidencias, reauditoría |

**Estado del conjunto AC01–AC16: `FUTUROS_NO_EJECUTADOS`.** Ninguna fila anterior es test aprobado, integración validada o chequeo funcional ejecutado. Las fallas hipotéticas no son incidentes reales. Las rutas Git citadas en M01.03 fijan contenidos estáticos pero no satisfacen oráculos HTTP/MySQL/Playwright.

## 3. Doce requisitos no funcionales propuestos — NO_MEDIDOS

| ID | Atributo | Condición medible futura |
| --- | --- | --- |
| NF01 | Reproducibilidad | Mismo snapshot debe producir mismo censo y ordenamiento por ID |
| NF02 | Consistencia | Diferencias entre fuentes se registran, no se concilian inventando valores |
| NF03 | Trazabilidad | Cada hallazgo usa requisito, ID, SHA, autor/revisor y timestamp UTC |
| NF04 | Idempotencia | Repetir lectura no añade eventos; append operativo usa llave única |
| NF05 | Concurrencia | CAS y relectura antes del siguiente CLAIM; jamás dos escritores del mismo ID |
| NF06 | Seguridad | Redactar secretos/PII/JWT y no consultar datos productivos |
| NF07 | Aislamiento | Tenant A/B y RBAC se demuestran en backend si hay runtime causal |
| NF08 | Observabilidad | Anotar estado queued/in_progress/cancelled sin convertirlo en PASS |
| NF09 | Reversibilidad | Revert normal y auditoría histórica append-only preservada |
| NF10 | Cobertura negativa | Casos de duplicados, ID ausente, ciclo, SHA divergente y self-A5 |
| NF11 | Alcance de entornos | Sólo dev; QA/main/PROD requieren permiso distinto |
| NF12 | Accesibilidad del informe | Tabla legible, claves estables y resultado verificable con evidencias |

**NF01–NF12: `NO_MEDIDOS`.** Las métricas, tolerancias y criterios de severidad requieren diseñar fixture sintético, denominador de requisitos, marco de evaluación y evidencia de los 2340 registros. No se establecen SLO ni cuotas artificiales sin autorización.

## 4. Formato mínimo futuro de evidencia por CASE_ID

Cada caso deberá identificar: (1) CASE_ID; (2) requisito original y versión/hash del Doc V5; (3) ID_MICROTAREA exacto y fila; (4) estado PLAN H/owner I y COLA G/O; (5) predecesores y DAG F/G; (6) archivo, blob y `SOURCE_SHA`; (7) test positivo y negativo con esperado/observado; (8) check GitHub name/status/conclusion/head_sha/log; (9) TASK_ID autora y revisora independiente; (10) evento A:Z e idempotency key; (11) timestamps UTC; (12) clasificación PASA, FALLA_CONFIRMADA, NO_ACREDITADO o NO_APLICA justificado; (13) corrección y rollback; (14) readback posterior.

La vista de auditoría no puede inferir que `cancelled/skipped/queued/in_progress` es éxito de prueba requerida. `NO_APLICA` es estado explicativo y jamás PASS; A5 documental no concede QA/main/PROD. Detectar diferencia no confirma por sí mismo P0/P1: exige impacto, reproducción y revisión independiente. Para código M03 se requiere el pipeline causal .NET 10, Angular 22.2.1, EF/MySQL 8.4 y seguridad tenant A/B según alcance, en GitHub Actions del SHA.

## 5. Impacto, auditorías y reversión

**Cambio permitido:** este Markdown y sección aditiva íntegra de `CHANGELOG_AI.md`. Sin modificación de `Domain/Application/Infrastructure/API`, Angular, archivos fuente de pruebas, esquemas EF/MySQL, endpoints, JWT/RBAC, tenant, servicios de pago, proveedores, jobs, webhooks, automatizaciones físicas, QA/main/PROD o secretos. Consumidores futuros: definición de gates M01.06 sólo si realmente satisfechos y auditoría de cobertura causal posterior.

- **A0:** matriz autocontenida de 16 pares AC, 12 NF, fuentes y límites; sin encabezados huérfanos.
- **A1:** oráculos P/N falsables diseñados, no ejecutados; funcionalidad runtime `NO_APLICA` para el diff Markdown.
- **A2:** no hay código de autorización cambiado; pruebas negativas JWT/RBAC/tenant A/B `NO_APLICA` a este artefacto, no PASS.
- **A3:** referencias cruzadas M01 y documento rector, sin test .NET, Angular, MySQL, Playwright o CI de infraestructura causal al Markdown.
- **A4:** publicación fast-forward con CAS HEAD, diff limitado a dos Markdown, check Scope Lock y control V5 `completed/success` para el SHA exacto, readback PLAN/COLA/BITACORA.
- **A5:** `PENDIENTE` de Task Javier distinta de esta autora; no autopromoción ni permisos implícitos.

**Rollback:** `git revert` normal del commit publicado, luego revalidar HEAD/readbacks; no reset destructivo ni reescritura de BITACORA.
