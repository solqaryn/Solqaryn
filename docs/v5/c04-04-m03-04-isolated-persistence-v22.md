# C04-04.M03.04 — adaptador aislado de persistencia V22

Riesgo R2 y alcance PLAN D701: "Implementar persistencia, migración reversible o adaptador aislado". Se adopta expresamente la alternativa de adaptador aislado, sin certificar toda M03 ni M03.05/06. PROJECT_SCOPE_LOCK=STRICT, rama dev solamente.

Se incorpora C0404PaymentReservationStore en Infrastructure/AutomationV5, con un DbContext **independiente y no registrado** en la API, AppDbContext, DI ni migraciones de SOLQARYN. La reserva tiene índice UNIQUE físico por EmpresaId, Provider y KeyHash, digest SHA256, política de replay vs conflicto de factura, moneda, importe o digest, y errores fail-closed. No persiste tokens, claves en claro, secretos ni tarjetas; jamás invoca PSP, cobra, reembolsa, actualiza ledger ni habilita permisos.

La prueba dirigida C0404M0304IsolatedPaymentReservationTests usa EF Core Oracle y MySQL 8.4 físico en el servicio efímero de GitHub-hosted, base GUID de un solo uso, tenant A/B, replay, colisión/conflicto SQL UNIQUE, entradas inválidas, cancelación y rollback real. La DB temporal se elimina al finalizar. Workflow dedicado impone SHA exacto, .NET 10 Release, Scope Lock y TRX 1/1 PASS sin FAIL/SKIP.

A0: diff y alcance D701; A1: asserts adversariales positivos, negativos, frontera y error; A2: tenant A/B SOLO en repositorio efímero, sin JWT/RBAC runtime (NO_APLICA a este adaptador desconectado); A3: integración física MySQL/Oracle EF cuando CI termine; A4: Scope Lock, V5, backend, seguridad, artefactos y readback exact-SHA. LIMITACION_FASE: la conexión verdadera con JWT, UsuarioEmpresa/RolPermiso, HTTP y proveedor de pagos, migración comercial y webhook quedan sin certificar en M03.05/M03.06 y etapas posteriores. A5 pendiente de Task Javier distinta de autor y reparador tras A0-A4 completos.

Rollback: revert normal fast-forward en dev. No se modifica Aiven, datos reales, QA/main/PROD, PSP ni secretos. SKIPPED/CANCELLED/FAIL/IN_PROGRESS no constituyen PASS.
