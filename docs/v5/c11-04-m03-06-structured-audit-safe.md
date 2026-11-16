# SOLQARYN V17 — C11-04.M03.06: auditoria estructurada de observabilidad

Autor: PRIMARY DEV 01 JAVIER, TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a. Fuente: PLAN_MAESTRO C11-04.M03.06, COLA G/O=SI, DAG F=VALIDADO/G=SI. Rama dev; Scope Lock STRICT; candado V17 SQV17_C11_04_M03_06 / 2000001711. Riesgo R2 por aislamiento tenant, RBAC, logging y confidencialidad.

## A0: implementacion y limites

- Nuevo C1104ObservabilityStructuredAuditSink en Infrastructure. Compone el resultado seguro de C1104ObservabilityBoundedReadWorker (M03.05), consulta un puerto de evidencia backend confiable por invocacion y emite un evento ILogger con EventId 110406, codigo fijo admitido y conteo 0..16.
- Rechaza resultado desconocido, conteo positivo invalido, SHA distinto o no canonico, identidad no autenticada, membresia o RBAC revocados, tenant A/B distinto y ausencia de barrera de auditoria. Retorna codigos opacos ante fallos de fuente/logger; cancelacion se propaga.
- No registra actor, tenant, SHA, correlacion, credenciales, excepciones ni payload. Un log aceptado NO certifica auditoria durable, SLO, metricas, acceso privado ni permiso de despliegue.
- Solo integracion real en proceso usando Fake Authority + ILogger de prueba; NO registra servicios en DI, proveedor de telemetria, HTTP, JWT live, EF/MySQL, exporter, worker alojado, red, cola, scheduler ni produccion. No hay cambio de interfaz publica, esquema de datos, secretos ni migraciones.

## A1–A3: verificaciones

- 14 casos xUnit dirigidos: positivo estructurado, warning de negacion, error opaco, resultado desconocido, cero/17/frontera, conteo erroneo, A/B, membresia/permiso/audit revocado, SHA incorrecto, nulo, excepcion de fuente, logger indisponible, cancelacion y constructor fail-closed. Ejecutar con GitHub Actions del SHA real (no atribuir PASS hasta finalizar).
- A2 es verificacion de barreras de un contrato aislado y no una prueba de JWT HTTP o de autenticacion real. A3 MySQL/EF, Chromium, API/provider: NO_APLICA al alcance; las dependencias externas no se tocan.
- A4: exigir Scope Lock/V5, build Release/xUnit y controles de seguridad/autorizacion pertinentes del SHA origen o equivalencia probada. CANCELLED/SKIPPED/QUEUED/IN_PROGRESS no son PASS.
- A5: revision independiente por otra Task Javier, nunca el autor.

Rollback: revert Git normal de los archivos del alcance y del CHANGELOG, conservando BITACORA. No QA/main/PROD.
