# SOLQARYN V22 - C11-03.M03.02: caso de uso Application Outbox

**Contrato atomico:** PLAN_MAESTRO D1671, implementar caso de uso, servicio o aplicacion con limites claros. Riesgo R2 por tenant, roles e idempotencia. Solo solqaryn/Solqaryn@dev. PROJECT_SCOPE_LOCK=STRICT.

## Cambio real y limites
- Se implementa una clase compilable .NET 10 en Application con contrato inyectable IC1103OutboxEvidenceReader y PrepareEnqueueAsync. Consume reglas Domain C11-03.M03.01, sin crear un dispatcher ni persistencia.
- Rechaza datos invalidos antes de tocar el lector. Cruza tenant del request, tenant autenticado del contexto, membresia, grant, token actual declarado por lector confiable, auditoria y estado existente. Fallos de adaptador, replay, diferencias de payload, DeadLetter y cancelacion son fail-closed.
- El resultado positivo es solamente ATOMIC_RESERVATION_AND_RUNTIME_AUTH_REQUIRED. AuthorizesDispatch y AuthorizesPersistence son siempre FALSE. No se hace commit de evento, encolado, cobro ni ACK.
- NO conectado a controlador/DI, JWT real, EF/MySQL 8.4, worker o proveedor. M03.03 tiene que demostrar JWT/RBAC real; M03.04, MySQL unico y CAS; M03.05-06, relay, ACK, logging y errores. Un booleano de evidencia inyectada NO certifica JWT real.

## Gates A0-A5
- A0: diff de clase Application, xUnit, workflow GitHub-hosted, este expediente y CHANGELOG autocontenido. Sin cambios de autorizacion runtime, migraciones, productos ni produccion.
- A1: xUnit real .NET 10 positivo, adversarial, frontera y errores con lector de evidencia aislado: tenants A/B, membresia y grant revocados, evento fuera de allowlist, replay, llave/payload conflictivo, DeadLetter, fallo de lector y cancelacion. Exigir un TRX con al menos 17 PASS, cero FAIL, cero NOT_EXECUTED en SHA exacto.
- A2: ningun plan concede persistir o despachar. La prueba usa flags de evidencia ficticia y NO JWT firmado; el adaptador real debera revalidar autoridad.
- A3: integracion Application-Domain real en .NET compilado. HTTP/JWT/MySQL/Chromium/proveedor NO_APLICA exclusivamente a este incremento sin consumidores ni infraestructura, NO al cierre M03.
- A4: check-run de la suite dirigida, Scope Lock, V5 y CI pertinentes completed/success por SHA causal. SKIPPED/CANCELLED/FAIL/IN_PROGRESS requerido NO PASS. V17, BITACORA y PLAN/COLA readback.
- A5: revision genuina por otra Task Javier diferente de DEV05, incluyendo codigo, pruebas, limites y regresiones.

Rollback solo git revert normal tras HEAD y writer check, sin reset/force-push. No QA, main, PROD, secretos o pagos.
