# SOLQARYN V17/V15 — C04-03.M01.06: candados de metering, reversa y contrato de salida

**Autor:** PRIMARY DEV 02 JAVIER, TASK_ID 6af16bb9492c8191b8e8c7d606f11507. **Tipo:** R0 preparación DOCUMENTAL, sin runtime. **Repositorio:** solqaryn/Solqaryn, solo dev, PROJECT_SCOPE_LOCK=STRICT. **PLAN_MAESTRO:** fila 655. **Candado V17:** SQV17_C04_03_M01_06, namedRangeId 2000000655. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Google Doc institucional) y PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA institucional.

## A0: alcance real y prerequisitos verificables

M01.06 ordena los requisitos de NO-GO, condiciones para planificar M02 y reglas de rollback del lote de metering C04-03. No crea medición, eventos, facturas, cobros, tablas, jobs, webhooks, servicios, componentes Angular, rutas HTTP, IAM, tokens, proveedores ni configuración activa. La expresión "Habilita M02" del plan significa exclusivamente habilitar documentación M02 que cumpla su DAG dinámico, sin aprobar M03 de código, migraciones, finanzas o QA/PROD.

Fuente secuencial revisada: C04-03.M01.01 objetivo y no-regresión (NR01–14); M01.02 10 actores y 14 I/O; M01.03 11 blobs cotejados y 6 brechas; M01.04 12 riesgos, 8 supuestos y decisiones D01–D08 todavía PENDIENTE; M01.05 AC01–AC14 y N01–N14 futuros. Todas constan LISTO DEV y A5 independiente real de otras Tasks Javier. Prerequisito adicional C01-03.M01.06+A5 figura LISTO y aprobado independientemente. DAG_TECNICO fila 616 F=VALIDADO/G=SI y COLA fila655 G/O=SI fueron leídas antes de reclamar; el lock V17 y CAS exclusivo devolvieron un único cambio. Estas evidencias no autorizan un sistema vivo.

## Barreras de bloqueo B01–B16

| ID | Condición que mantiene NO-GO técnico/comercial | Evidencia positiva futura para liberación |
| --- | --- | --- |
| B01 | Tenant no deriva de contexto backend autenticado | Pruebas HTTP positivas/negativas A/B y propiedad por EmpresaId |
| B02 | Falta JWT vigente, membresía o RBAC granular | Validaciones y rechazos de role/revocation reales en CI |
| B03 | Catálogo de unidades y métricas no aprobado | Claves versionadas de usuario, operación, byte y archivo |
| B04 | No existe regla de ventana/periodo UTC confiable | Tests frontera UTC, cambio de periodo y relojes adulterados |
| B05 | Fuente de cantidad puede llegar exclusivamente del cliente | Adaptador verificado server-side y negativas de cifras forjadas |
| B06 | Replay/doble ejecución sin exclusión transaccional | Pruebas concurrentes con un solo efecto persistente |
| B07 | Persistencia de uso y ownership tenant sin esquema validado | EF/MySQL 8.4 efímero con migración/reversa real y 0 FAIL |
| B08 | Suscripción suspendida/cancelada sin política aprobada | Decisión explícita D01–D08 aplicable y test de denegación |
| B09 | Idempotencia de onboarding se confunde con metering | Ledger de consumo separado y correlación sin permisos implícitos |
| B10 | Lectura/exportación cruza empresas o filtra PII | Pruebas A/B, minimización y autorización específica |
| B11 | Precios, moneda, impuestos o liquidación no están aprobados | Aprobación comercial separada antes de facturación |
| B12 | Cuotas y límites se calculan desde UI o valor NULL ambiguo | Semántica tipada y contraejemplos nulos/ilimitados |
| B13 | Conciliador altera historia o ignora errores de almacenamiento | Reproceso auditable, compensación, rollback e inyección de fallo |
| B14 | Faltan políticas de retención, opt-out o finalidad | Decisiones legítimas por empresa y privacidad verificadas |
| B15 | CI necesaria está queued/in_progress/cancelled/skipped/failed | completed/success exact SHA de pruebas requeridas y Scope Lock |
| B16 | A5 independiente, autor o claim no verificables | Revisor Javier distinto, lock único, BITACORA idempotente y readback |

**Estado real de estas barreras:** contrato para etapas posteriores; ninguna queda liberada por este Markdown. D01–D08 permanecen PENDIENTE. No deducir un PASS de A5 documental ni de una fórmula G/O=SI. Una eventual M02 debe mantener estos bloqueos hasta aportar evidencia causal de su alcance; M01.06 no es licencia para forzar un M03 ni para activar un proveedor.

## A1: criterios positivos futuros P01–P08 y negativos N01–N12 (NO EJECUTADOS)

| ID | Positivo futuro observable | Negativo/fallo a impedir |
| --- | --- | --- |
| P01 / N01 | Tenant A mide exclusivamente operación de A | A intenta contar o leer B |
| P02 / N02 | Evento repetido conserva un solo conteo | Dos workers generan dos consumos |
| P03 / N03 | Uso lleva unidad y origen acreditados | Browser aporta bytes falsos |
| P04 / N04 | Corte UTC y versión producen periodo reproducible | Timestamp cliente mueve el periodo |
| P05 / N05 | Plan y estado exigen permiso backend | UI o JWT revocado abre entitlement |
| P06 / N06 | Lectura/export usa RBAC y minimización | Se filtran registros/PII ajenos |
| P07 / N07 | Fallo transaccional evita confirmar consumo | HTTP success con DB fallida |
| P08 / N08 | Conciliación compensa sin borrar eventos | Reconciliador destruye historia |
| N09 | — | Idempotencia onboarding se trata como metering |
| N10 | — | NULL se interpreta como consumo o cuota ilimitados |
| N11 | — | A5 del autor, CI cancelada o skipped se presenta PASS |
| N12 | — | Este documento crea pagos, worker, DB o despliega QA/main/PROD |

Es una matriz de aceptación FUTURA, no pruebas HTTP/MySQL/Playwright realizadas en esta unidad R0. Ningún criterio positivo/negativo se etiqueta falsamente como ejecutado.

## A2: seguridad, calidad y A3: integración por causa

Conservar autenticación, tenant, JWT/RBAC, idempotencia, reintentos finitos, secretos, presupuesto de uso, privacidad y rollback como barreras. La seguridad del SaaS actual no se modifica. A1/A2 aquí significan cotejo documental de invariantes; no certifican integración real, equipo comercial ni prueba funcional. A3 para esta unidad de dos archivos Markdown es NO_APLICA a .NET, API, EF/MySQL, Angular/Playwright, servicios externos y proveedores; exigirlas cuando exista código auténtico en otra microtarea.

Prerrequisitos e impacto: lectura individual de PLAN M01.01–05 y sus A5, C01-03.M01.06 A5, DAG/COLA, código/blobs históricos de M01.03; no reescritura de fuentes institucionales ni creación de Task/PR/rama. Las futuras unidades M02 deben desarrollar únicamente contratos bajo DAG vigente y jamás sustituir verificación técnica de M03 por este documento.

## A4, A5 y reversibilidad

A4 requiere dos archivos Markdown por commit fast-forward/CAS (este documento y sección autocontenida de CHANGELOG_AI.md), exact-SHA verify-solqaryn-project-scope y control V5 de procedencia completed/success, ausencia de encabezados vacíos y readback PLAN_MAESTRO/COLA/DAG/BITACORA. CI compilación/backend/Angular/MySQL/Playwright que no toca esta entrega es NO_APLICA motivado, nunca PASS inventado. Si un check obligatorio sigue pendiente, conservar VALIDANDO con autor y SHA hasta acreditarlo.

A5 exige reviewer Task Javier distinta de la autora PRIMARY DEV 02; A5_PASS de este documento no aprueba D01–D08 ni QA/main/PROD. Reversión por git revert normal del commit tras releer dev actual, y anotación compensatoria append-only en BITACORA. No borrar registros, no force-push, no restaurar historial ajeno. Cualquier autorización de política/comercial o cambio irreversible requiere aprobación posterior y pruebas específicas.
