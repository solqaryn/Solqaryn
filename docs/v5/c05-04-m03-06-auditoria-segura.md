# SOLQARYN V17 — C05-04.M03.06 auditoria segura de cola de ligereza UI

Autor: PRIMARY DEV 01, TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a. Plan Comercial Inicial, PLAN C05-04.M03.06, COLA G/O=SI, DAG F=VALIDADO G=SI. Rama dev, PROJECT_SCOPE_LOCK=STRICT, V17 namedRange SQV17_C05_04_M03_06 id 2000000847. Riesgo R2: telemetria, tenancy, autorización, error y privacidad.

## A0: alcance y dependencias

- Se agrega V5UiLightnessQueueAuditSink en Infrastructure .NET 10, que consume exclusivamente los codigos fijos de V5UiLightnessBoundedReviewQueue (M03.05) y una fuente de autoridad backend que verifica identidad, membresia, rol, permiso, tenant A/B, SHA exacto y audit fence.
- Un evento ILogger de esquema fijo (EventId=504306) emite solo codigo permitido, no request, actor, tenant, SHA, JWT, payload, PII, URL ni excepcion. Unknown codes nunca llegan a log. Se preservan denegacion y cancelacion; se devuelve error opaco si falla la fuente o logger.
- No instala proveedor, registrador de DI, hospedaje, persistencia, cola durable, cache, HTTP, Angular, navegador, MySQL, EF, credenciales ni auditoria transaccional. Una entrega/logger exitosa NO habilita acceso, no mide Core Web Vitals y NO acredita despliegue ni informe SLO/costos.

## A1-A3: evidencia exigida

- 14 xUnit especificas: positivo, negativo, vacio, codigo no permitido, tenant B, membresia/rol/RBAC revocados, source SHA stale, autoridad ausente o fallando, sink inactivo o fallando, cancelacion, constructor. Incluyen limites y error seguros; pruebas reales solo al aprobar la CI GitHub Actions por SHA.
- A2 frontera contractualmente aislada. JWT HTTP, proveedor externo, metricas browser, Angular, MySQL/EF y pruebas con datos productivos NO_APLICA al diff. No se atribuye a los mocks una certificacion de sesiones, tenants reales o persistencia durable.
- A4 requiere exact SHA SUCCESS de Scope Lock/V5, .NET Release/test, seguridad multiempresa y autorización relevantes, ademas del diff/blobs y readback PLAN/BITACORA.
- A5 independiente por distinta Task Javier; no autoprobación. Rollback git revert normal, jamás borrar BITACORA ni tocar QA/main/PROD.
