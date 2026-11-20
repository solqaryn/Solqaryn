# C11-03.M03.04 — Adaptador aislado de evidencia outbox / MySQL 8.4

Identidad: SOLQARYN, solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. Plan D1673: "Implementar persistencia, migración reversible o adaptador aislado". Riesgo R2 por outbox/tenant/EF/MySQL/concurrencia. Autor DEV04, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09, lock V17 2000001673.

## Salida atómica y arquitectura
Se incorpora C1103OutboxTenantEvidenceAdapter sobre el AppDbContext **existente**, sin migrar ni duplicar MensajeOutboxRepository, esquema o índice. Es un adaptador de lectura por empresa + EventoId que proyecta solo estado/intentos; nunca devuelve payload, clave de idempotencia ni PII, no altera registros. La comparación empresa/evento ocurre en el SQL, no después de leer datos de otro tenant. Valor de prueba: inspección acotada del estado durable real para futuras etapas M03.05-.06.

No se registra en DI, no añade endpoint, no concede permisos y no es prueba del flujo de despacho, delivery, ACK, retry o proveedor. La autorización JWT/RBAC probada en M03.03 es independiente: este adaptador **no puede convertirse en API pública sin su control server-side**, y M03.04 no realiza ni certifica ese wiring. No se toca QA/main/PROD ni proveedores reales.

## A0–A4, pruebas y reversibilidad
- A0: diff de adaptador de Infrastructure aislado, xUnit Category=Integration, CI dirigida, documento y CHANGELOG; interfaces y migraciones productivas intactas.
- A1: afirmaciones reales que fallan si falta fila A, si B lee evento de A, si se devuelve evento de otro tenant, ID inválido, cancelación; se prueba caso positivo A/B.
- A2: filtros tenant A/B por SQL, no acepta tenant 0 ni GUID nulo; solo metadatos seguros y ninguna capacidad de escritura/dispatch.
- A3: CI GitHub-hosted MySQL **8.4 efímero** con AppDbContext/EF Oracle real, schema por Phase7MySqlTestDatabase, dos tenants con misma clave idempotente, índice UNIQUE rechazando duplicado A y transacción rollback que deja sin fila. TRX debe demostrar **1/1 PASS 0 FAIL 0 SKIPPED**; sin eso no certificar A3. No sustituir con SQLite/mock ni aseverar haber probado proveedor.
- A4: exigir source SHA exacto, Scope Lock, V5/procedencia y jobs pertinentes completed/success, Sheets PLAN/COLA/BITACORA readback. SKIPPED/CANCELLED/IN_PROGRESS obligatorio no es PASS.
- A5: revisor Javier Task ID distinto del autor y con revisión genuina; no autoaprobación.

Rollback: revert normal del commit en dev después de comprobar HEAD, sin force push. No hay esquema, datos ni endpoints productivos que revertir. PLAN E es meta acumulativa: este incremento no declara M03 completa, relay, delivery o ACK implementados.
