# SOLQARYN V22 — C04-01.M03.06: auditoría estructurada aislada con MySQL 8.4

**Acción PLAN D595:** implementar auditoría, logs estructurados y manejo de errores seguro. Entrega R2: sink C0401IsolatedStructuredAuditSink con tabla propia V22C0401AuditIsolated exclusivamente en MySQL efímero, sin registrar DI productiva ni escribir a Aiven. E «Completa M03» es meta acumulativa: esta .06 no certifica billing, cuotas ni toda M03. Rama dev, PROJECT_SCOPE_LOCK=STRICT, STANDARD V5 FINAL, PLAN/COLA/DAG con candado V17 exclusivo.

## A0 arquitectura e impacto

Infrastructure recibe la fábrica de DbConnection explícitamente y persiste evento estructurado con TenantId, EventId, WorkId, ActorId, EventCode y OccurredUtc dentro de transacción InnoDB. ID de evento canónico ASCII 16–64, código allowlist CLAIM/ACK/DENY/EXPIRE, ids positivos. Índice único por (TenantId,EventId) impide replay/reescritura del histórico, incluido replay con otro actor/código. No se acepta texto arbitrario, JWT, cabeceras, payload, detalles de SQL, PII ni secretos. Error SQL devuelve solo AUDIT_REJECTED sin excepción de datos, cancelación propaga. El sink NO concede autoridad; un caller futuro debe verificar JWT/rol/tenant en servidor y acoplar la auditoría al flujo de la operación en una transacción autorizada. Aun con test PASS, un ACK de cola aislada y evento de auditoría en transacciones separadas no prueban atomicidad conjunta.

## A1–A3: casos reales adversariales

Test C0401M0306IsolatedAuditMysql84Tests en GitHub-hosted, Docker mysql:8.4 efímero, Oracle MySql.Data y SELECT VERSION() 8.4.*. Verifica escritura A/B separada con mismo EventId, campos y UTC en lectura real; replay y replay conflictivo denegados sin sobrescribir; entradas negativas tenant/work/actor cero, ID corto, JWT-like y código fuera de allowlist; simulación de error sin fuga, cancelación, rollback InnoDB y estado posterior. TRX debe acreditar total==passed, cero FAIL/NOT_EXECUTED. No mocks ni CHECK SKIPPED como evidencia A3.

## A4, A5 y reversión

Exigir CI exact-SHA SUCCESS del job MySQL físico, .NET10 Release/backend, seguridad, Scope Lock, V5 y readback PLAN/COLA/BITACORA antes de VALIDANDO→LISTO; A5 de Task Javier distinta de DEV01 con revisión propia y evidencia, no autopase. Limitaciones: NO está integrado en worker/producto real, no garantiza atomicidad entre queue claim/ack y registro, no certifica API/JWT HTTP ni historial contable comercial, PSP, Aiven, migraciones, stock o pagos; para esas áreas una fase posterior deberá implementar transacción y permisos comprobados. Reversión con git revert normal sobre dev; MySQL CI se descarta, sin QA/main/PROD, secrets ni R3.
